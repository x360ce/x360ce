using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
			var oldPort = Properties.Settings.Default.ComputerPort;
			var win = new OptionsWindow();
			var result = win.ShowDialog();
			var newPort = Properties.Settings.Default.ComputerPort;
			if (result == true)
			{
				if (oldPort != newPort)
					StopServer();
				if (Properties.Settings.Default.Connect)
					StartServer();
			}
		}

		JocysCom.ClassLibrary.HiResTimer _timer;
		JocysCom.ClassLibrary.Data.TlvSerializer _Serializer;

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
			var types = new Dictionary<Type, int>();
			types.Add(typeof(State), 0);
			_Serializer = new ClassLibrary.Data.TlvSerializer(types);
			if (Properties.Settings.Default.AutoConnect && Properties.Settings.Default.Connect)
				StartServer();
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

		#region UDP Client

		void AddLog(string format, params object[] args)
		{
			Trace.Write(string.Format(format, args));
		}

		UdpClient server;

		public bool IsRunning { get; private set; }

		public void StartServer()
		{
			AddLog("Starting remote server... ");
			if (IsRunning)
			{
				AddLog(" already started.\r\n");
				return;
			}
			IsRunning = true;
			// Set local endpoint (random port).
			var localEndpoint = new IPEndPoint(IPAddress.Any, 0);
			// Create client.
			server = new UdpClient();
			// Workaround for: UDP SocketException - Only one usage of each socket address is normally permitted.
			server.ExclusiveAddressUse = false;
			server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			server.Client.Bind(localEndpoint);
			AddLog("started.\r\n");
			Receive();
		}

		public void StopServer()
		{
			AddLog("Stopping remote server... ");
			if (!IsRunning)
			{
				AddLog("already stopped.\r\n");
				return;
			}
			IsRunning = false;
			server.Close();
			AddLog("stopped.\r\n");
		}

		void Receive()
		{
			try
			{
				AddLog("Receiving on {0}...\r\n", server.Client.LocalEndPoint);
				server.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
			}
			catch (Exception ex)
			{
				AddLog("Receiving Error {0}.\r\n", ex.Message);
				IsRunning = false;
			}
		}

		void Send(State state)
		{
			var ms = new MemoryStream();
			var status = _Serializer.Serialize(ms, state);
			if (status == ClassLibrary.Data.TlvSerializerError.None)
			{
				var bytes = ms.ToArray();
				IPAddress adddress = IPAddress.Any;
				IPAddress.TryParse(Properties.Settings.Default.ComputerHost, out adddress);
				var remoteEndpoint = new IPEndPoint(adddress, Properties.Settings.Default.ComputerPort);
				server.SendAsync(bytes, bytes.Length);
			}
		}

		void ReceiveCallBack(IAsyncResult res)
		{
			// If disposed then simply return.
			if (server.Client == null)
			{
				IsRunning = false;
				return;
			}
			try
			{
				var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
				var bytes = server.EndReceive(res, ref remoteEndpoint);
				var data = string.Join("", bytes.Select(x => x.ToString("X2")));
				AddLog("Received Data from {0}: {1}\r\n", remoteEndpoint, data);
				if (!IsRunning)
					Receive();
			}
			catch (ObjectDisposedException)
			{
				// If disposed then simply return.
				if (!IsRunning)
					return;
				throw;
			}
		}


		#endregion

	}

}
