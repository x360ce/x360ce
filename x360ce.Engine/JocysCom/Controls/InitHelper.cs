using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Helps to output initialize stats when form initialize.
	/// </summary>
	public class InitHelper
	{

		public InitHelper()
		{
			_Timer = new System.Timers.Timer();
			_Timer.AutoReset = false;
			_Timer.Interval = 2000;
			_Timer.Elapsed += _Timer_Elapsed;
		}

		internal FrameworkElement Control;
		internal DateTime StartDate;
		internal DateTime EndDate;
		internal int _PropertyChangedCount;
		System.Timers.Timer _Timer;

		public void WriteLine(string prefix)
		{
			var s = string.Format("-4-> {0,4}. {1} - {2}: {3} changes",
				_InitEndCount, prefix, Control.GetType(), _PropertyChangedCount);
			if (EndDate.Ticks > 0)
				s += string.Format(", {0:# ##0} ms", EndDate.Subtract(StartDate).TotalMilliseconds);
			Debug.WriteLine(s);
		}

		#region ■ Static 

		internal static int _InitEndCount;
		private static object TimersLock = new object();
		private static List<InitHelper> Timers = new List<InitHelper>();

		public static List<string> LoadedControlNames = new List<string>();

		private static bool IsDebug
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// In release mode invoke only, in debug mode use times and monitor object.
		/// </summary>
		public static void InitTimer(FrameworkElement control, Action InitializeComponent)
		{
			if (!IsDebug)
			{
				InitializeComponent.Invoke();
				return;
			}
			var ih = new InitHelper();
			ih.Control = control;
			ih.StartDate = DateTime.Now;
			ih.WriteLine("INIT START");
			ih.EndDate = DateTime.Now;
			InitializeComponent.Invoke();
			ih.EndDate = DateTime.Now;
			ih.WriteLine("INIT CON  ");
			lock (TimersLock)
				Timers.Add(ih);
			ih.Control.Loaded += Control_Loaded;
			ih.Control.Unloaded += Control_Unloaded;
			ih.Control.IsVisibleChanged += Control_IsVisibleChanged;
			//ih.Control.PropertyChanged += Control_PropertyChanged;
			ih._Timer.Start();
		}

		private static void Control_Unloaded(object sender, RoutedEventArgs e)
		{
			var control = (FrameworkElement)sender;
			var name = $"{control.GetType()} {control.Name} {control.GetHashCode()}";
			LoadedControlNames.Remove(name);
			// Remove events.
			control.Loaded -= Control_Loaded;
			control.Unloaded -= Control_Unloaded;
			control.IsVisibleChanged -= Control_IsVisibleChanged;
			//control.PropertyChanged -= Control_PropertyChanged;
		}

		private static void Control_Loaded(object sender, RoutedEventArgs e)
		{
			var control = (FrameworkElement)sender;
			var name = $"{control.GetType()} {control.Name} {control.GetHashCode()}";
			LoadedControlNames.Add(name);
		}

		private static void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			RestartTimer(sender);
		}

		/// <summary>
		/// Xamarin.
		/// </summary>
		private static void Control_PropertyChanged(object sender, EventArgs e)
		{
			RestartTimer(sender);
		}


		private static void RestartTimer(object sender)
		{
			InitHelper ih = null;
			lock (TimersLock)
				ih = Timers.FirstOrDefault(x => Equals(x.Control, sender));
			if (ih == null)
				return;
			ih._PropertyChangedCount++;
			ih.EndDate = DateTime.Now;
			ih._Timer.Stop();
			ih._Timer.Start();
		}

		/// <summary>
		/// This function will trigger once, after 2000ms when control stops visible changing.
		/// </summary>
		private static void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			InitHelper ih = null;
			// Find InitHelper by timer.
			lock (TimersLock)
			{
				ih = Timers.FirstOrDefault(x => Equals(x._Timer, sender));
				if (ih == null)
					return;
				Timers.Remove(ih);
				_InitEndCount++;
				ih.WriteLine("INIT END");
				// Disconnect all links.
				ih.Control.IsVisibleChanged -= Control_IsVisibleChanged;
				//ih.Control.PropertyChanged -= Control_PropertyChanged;
				ih.Control = null;
				ih._Timer.Dispose();
			}
		}

		#endregion


	}
}
