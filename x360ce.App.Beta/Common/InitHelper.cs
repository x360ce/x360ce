using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;

namespace x360ce.App
{
	/// <summary>
	/// Helps to output initialize stats when form initialize.
	/// </summary>
	public class InitHelper : IDisposable
	{

		public InitHelper()
		{
			_Timer = new System.Timers.Timer();
			_Timer.AutoReset = false;
			_Timer.Interval = 2000;
			_Timer.Elapsed += _Timer_Elapsed;
		}

		internal UIElement Control;
		internal DateTime StartDate;
		internal DateTime EndDate;
		internal static int _InitEndCount;
		internal int _PropertyChangedCount;
		System.Timers.Timer _Timer;

		private static object TimersLock = new object();
		private static List<InitHelper> Timers = new List<InitHelper>();
		public static void InitTimer(UIElement control, Action InitializeComponent)
		{
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
			ih.Control.IsVisibleChanged += Control_IsVisibleChanged;
			ih._Timer.Start();
		}

		private static void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			var ih = Timers.FirstOrDefault(x => Equals(x.Control, sender));
			if (ih == null)
				return;
			ih._PropertyChangedCount++;
			ih.EndDate = DateTime.Now;
			ih._Timer.Stop();
			ih._Timer.Start();
		}

		private static void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var ih = Timers.FirstOrDefault(x => Equals(x._Timer, sender));
			if (ih == null)
				return;
			_InitEndCount++;
			ih.WriteLine("INIT END  ");
			ih.Dispose();
		}

		public void WriteLine(string prefix)
		{
			var s = string.Format("-4-> {0}. {1} - {2}: {3} changes",
				_InitEndCount, prefix, Control.GetType(), _PropertyChangedCount);
			if (EndDate.Ticks > 0)
				s += string.Format(", {0:# ##0} ms", EndDate.Subtract(StartDate).TotalMilliseconds);
			Debug.WriteLine(s);
		}

		#region ■ IDisposable

		public bool IsDisposing;

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Control.IsVisibleChanged -= Control_IsVisibleChanged;
				_Timer.Dispose();
				lock (TimersLock)
					Timers.Remove(this);
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion

	}
}
