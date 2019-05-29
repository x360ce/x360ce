using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JocysCom.x360ce.RemoteController
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Title = new JocysCom.ClassLibrary.Configuration.AssemblyInfo().GetTitle();
		}

		private void OptionsButton_Click(object sender, RoutedEventArgs e)
		{
			var win = new OptionsWindow();
			win.ShowDialog();
		}

		JocysCom.ClassLibrary.HiResTimer _timer;

		// Control when event can continue.
		object timerLock = new object();

		public bool Suspended;

		public void Start()
		{
			lock (timerLock)
			{
				if (_timer != null)
					return;
				_timer = new JocysCom.ClassLibrary.HiResTimer();
				_timer.Elapsed += Timer_Elapsed;
				_timer.Interval = 2;
				_timer.Start();
			}
		}

		public void Stop()
		{
			lock (timerLock)
			{
				if (_timer == null)
					return;
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
			}
		}

		public Exception LastException = null;

		State currentState;
		State oldState;

		void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				// If state changed.
				if (currentState.Gamepad.Buttons != oldState.Gamepad.Buttons)
				{
					// Send state here (work in progress)
					// ...
					oldState = currentState;
				}
			}
			catch (Exception ex)
			{
				LastException = ex;
			}
		}

		Dictionary<Shape, GamepadButtonFlags> list;


		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			list = new Dictionary<Shape, GamepadButtonFlags>();
			list.Add(LeftShoulderRectangle, GamepadButtonFlags.LeftShoulder);
			list.Add(RightShoulderRectangle, GamepadButtonFlags.RightShoulder);
			//list.Add(LeftTriggerRectangle, GamepadButtonFlags.);
			//list.Add(RightTriggerRectangle);
			list.Add(LeftThumbEllipse, GamepadButtonFlags.LeftThumb);
			list.Add(RightThumbEllipse, GamepadButtonFlags.RightThumb);
			list.Add(DPadUpEllipse, GamepadButtonFlags.DPadUp);
			list.Add(DPadRightEllipse, GamepadButtonFlags.DPadRight);
			list.Add(DPadDownEllipse, GamepadButtonFlags.DPadDown);
			list.Add(DPadLeftEllipse, GamepadButtonFlags.DPadLeft);
			list.Add(ButtonBackEllipse, GamepadButtonFlags.Back);
			list.Add(ButtonStartEllipse, GamepadButtonFlags.Start);
			list.Add(ButtonAEllipse, GamepadButtonFlags.A);
			list.Add(ButtonYEllipse, GamepadButtonFlags.Y);
			list.Add(ButtonXEllipse, GamepadButtonFlags.X);
			list.Add(ButtonBEllipse, GamepadButtonFlags.B);
			foreach (var key in list.Keys)
			{
				key.TouchDown += Shape_Down;
				key.MouseDown += Shape_Down;
				key.TouchUp += Shape_Up;
				key.MouseUp += Shape_Up;
			}
		}

		private void Shape_Up(object sender, EventArgs e)
		{
			var s = (Shape)sender;
			s.Fill = (Brush)FindResource("TouchUpBrush");
			// Add button.
			currentState.Gamepad.Buttons |= list[s];
		}

		private void Shape_Down(object sender, EventArgs e)
		{
			var s = (Shape)sender;
			s.Fill = (Brush)FindResource("TouchDownBrush");
			// Remove button.
			currentState.Gamepad.Buttons &= ~list[s];
		}

	}

}
