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
	/// <remarks>Make sure to set the Owner property to be disposed properly after closing.</remarks>
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
		JocysCom.ClassLibrary.Runtime.TlvSerializer _Serializer;

		// Control when event can continue.
		object timerLock = new object();

		public bool Suspended;

		public void Start()
		{
			lock (timerLock)
			{
				if (_timer != null)
					return;
				_timer = new JocysCom.ClassLibrary.HiResTimer(2, "MainWindowTimer");
				_timer.Elapsed += Timer_Elapsed;
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
					Send(currentState);
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
			types.Add(typeof(Gamepad), 1);
			_Serializer = new ClassLibrary.Runtime.TlvSerializer(types);
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

		#region UDP Server

		void AddLog(string format, params object[] args)
		{
			Trace.Write(string.Format(format, args));
		}

		/// <summary>Recevier</summary>
		UdpClient udpServer;

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
			Start();
			// Set local endpoint (random port).
			var localPort = FindFreePort();
			var localEndpoint = new IPEndPoint(IPAddress.Any, localPort);
			// Create client.
			udpServer = new UdpClient();
			// Workaround for: UDP SocketException - Only one usage of each socket address is normally permitted.
			udpServer.DontFragment = true;
			udpServer.MulticastLoopback = false;
			udpServer.ExclusiveAddressUse = false;
			udpServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			udpServer.Client.Bind(localEndpoint);
			//InitClientServer();
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
			Stop();
			udpServer.Close();
			AddLog("stopped.\r\n");
		}

		void Receive()
		{
			try
			{
				AddLog("Receiving on {0}...\r\n", udpServer.Client.LocalEndPoint);
				udpServer.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
			}
			catch (Exception ex)
			{
				AddLog("Receiving Error {0}.\r\n", ex.Message);
				IsRunning = false;
			}
		}

		void ReceiveCallBack(IAsyncResult res)
		{
			// If disposed then simply return.
			if (udpServer.Client == null)
			{
				IsRunning = false;
				return;
			}
			try
			{
				// Allow to receive from any source.
				var remoteEP = new IPEndPoint(IPAddress.Any, 0);
				var bytes = udpServer.EndReceive(res, ref remoteEP);
				var data = string.Join("", bytes.Select(x => x.ToString("X2")));
				AddLog("Received Data from {0}: {1}\r\n", remoteEP, data);
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

		void Send(State state)
		{
			var ms = new MemoryStream();
			var status = _Serializer.Serialize(ms, state);
			var bytes = new byte[0];
			if (status == ClassLibrary.Runtime.TlvSerializerError.None)
			{
				bytes = ms.ToArray();
				IPAddress adddress;
				IPAddress.TryParse(Properties.Settings.Default.ComputerHost, out adddress);
				var remoteEndpoint = new IPEndPoint(adddress, Properties.Settings.Default.ComputerPort);
				udpServer.Send(bytes, bytes.Length, remoteEndpoint);
			}
			var data = string.Join("", bytes.Select(x => x.ToString("X2")));
			AddLog("{0:HH:mm:ss.fff} Send status: {1}\r\n", DateTime.Now, data);
		}

		#endregion

		#region Helper Functions

		/// <summary>
		/// Find first free port. IANA Port categories:
		///         0 –  1023 – System or Well Known ports.
		///      1024 – 49151 – User or Registered ports.
		///     49152 - 65535 – Dynamic (Private) or Ephemeral Ports.
		/// </summary>
		/// <returns>Free port number if found; otherwise 0.</returns>
		private ushort FindFreePort(ushort startPort = 49152, ushort endPort = ushort.MaxValue)
		{
			var portArray = new List<int>();
			var properties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
			// Get TCP connection ports.
			var ports = properties.GetActiveTcpConnections()
				.Where(x => x.LocalEndPoint.Port >= startPort)
				.Select(x => x.LocalEndPoint.Port);
			portArray.AddRange(ports);
			// Get TCP listener ports.
			ports = properties.GetActiveTcpListeners()
				.Where(x => x.Port >= startPort)
				.Select(x => x.Port);
			portArray.AddRange(ports);
			// Get UDP listener ports.
			ports = properties.GetActiveUdpListeners()
				.Where(x => x.Port >= startPort)
				.Select(x => x.Port);
			portArray.AddRange(ports);
			// Get first port not in the list.
			for (int i = startPort; i <= endPort; i++)
				if (!portArray.Contains(i))
					return (ushort)i;
			return 0;
		}

		#endregion


	}

}
