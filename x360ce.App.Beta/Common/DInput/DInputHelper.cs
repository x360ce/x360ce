using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Threading;

namespace x360ce.App.DInput
{
	public partial class DInputHelper : IDisposable
	{
		// Constructor.
		public DInputHelper()
		{
			CombinedXiConencted = new bool[4];
			LiveXiConnected = new bool[4];

			CombinedXiStates = new State[4];
			LiveXiStates = new State[4];
			LiveXiControllers = new Controller[4];
			for (int i = 0; i < 4; i++)
			{
				CombinedXiStates[i] = new State();
				LiveXiStates[i] = new State();
				LiveXiControllers[i] = new Controller((UserIndex)i);
			}

			// Set and Start Timer counting DInput updates per second to show in app's status bar as Hz: #.
		}

		// Where current DInput device state is stored:
		//
		//    UserDevice.Device - DirectInput Device (Joystick)
		//    UserDevice.State - DirectInput Device (JoystickState)
		//
		// Process 1
		// limited to [125, 250, 500, 1000Hz]
		// Lock
		// {
		//    Acquire:
		//    DiDevices - when device is detected.
		//	  DiCapabilities - when device is detected.
		//	  JoStates - from mapped devices.
		//	  DiStates - from converted JoStates.
		//	  XiStates - from converted DiStates
		// }
		//
		// Process 2
		// limited to [30Hz] (only when visible).
		// Lock
		// {
		//	  DiDevices, DiCapabilities, DiStates, XiStates
		//	  Update DInput and XInput forms.
		// }

		/// <summary>
		/// _Timer (HiResTimer) with _ResetEvent (ManualResetEvent) is used to limit update refresh frequency.
		/// </summary>
		JocysCom.ClassLibrary.HiResTimer _Timer;
		ManualResetEvent _ResetEvent = new ManualResetEvent(false);

		public UpdateFrequency Frequency
		{
			get { return _Frequency; }
			set
			{
				_Frequency = value;
				var t = _Timer;
				if (t != null && t.Interval != (int)value)
					t.Interval = (int)value;
			}
		}
		UpdateFrequency _Frequency = UpdateFrequency.ms1_1000Hz;

		/// <summary>
		/// _Stopwatch to monitor update frequency.
		/// </summary>
		System.Diagnostics.Stopwatch _Stopwatch = new System.Diagnostics.Stopwatch();
		object timerLock = new object();
		bool _AllowThreadToRun;

		// Start DInput Service.
		public void Start()
		{
			lock (timerLock)
			{
				if (_Timer != null)
					return;
				_Stopwatch.Restart();
				_Timer = new JocysCom.ClassLibrary.HiResTimer((int)Frequency, "DInputHelperTimer");
				_Timer.Elapsed += Timer_Elapsed;
				_Timer.Start();
				_AllowThreadToRun = true;
				RefreshAllAsync();
			}
		}

		// Stop DInput Service.
		public void Stop()
		{
			lock (timerLock)
			{
				if (_Timer == null)
					return;
				_Timer.Stop();
				_Timer.Dispose();
				_Timer = null;
				_AllowThreadToRun = false;
				_ResetEvent.Set();
				// Wait for thread to stop.
				_Thread.Join();
			}
		}

		public Exception LastException = null;

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				//Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
				_ResetEvent.Set();
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastException = ex;
			}
		}

		// Control when event can continue.
		ThreadStart _ThreadStart;
		Thread _Thread;

		/// <summary>
		/// Method which will create separate thread which will do all DInput and XInput updates.
		/// </summary>
		void RefreshAllAsync()
		{
			_ThreadStart = new ThreadStart(ThreadAction);
			_Thread = new Thread(_ThreadStart);
			// This thread will run function which will update BindingList, which will use synchronous Invoke() on main form running on main thread.
			// It can freeze, because Main thread is not getting attention to process Invoke() (because attention is on this thread)
			// and this thread is frozen because it is waiting for Invoke() to finish.
			_Thread.IsBackground = true;
			_Thread.Start();
		}

		// Suspended is used during re-loading of XInput library.
		public bool Suspended;
		void ThreadAction()
		{
			// Set name of the thread.
			Thread.CurrentThread.Name = "RefreshAllThread";
			// DIrect input device querying and force feedback updated will run on a separate thread from MainForm therefore
			// separate windows form must be created on the same thread as the process which will access and update device.
			// detector.DetectorForm will be used to acquire devices.
			/// Main job of detector is to fire event on device connection (power on) and removal (power off).
			var manager = new DirectInput();
			var detector = new DeviceDetector(false);
			do
			{
				// Sets the state of the event to non-signaled, causing threads to block.
				_ResetEvent.Reset();
				// Perform all updates if not suspended.
				if (!Suspended)
					RefreshAll(manager, detector);
				// Blocks the current thread until the current WaitHandle receives a signal.
				// Thread will be release by the timer.
				// Do not wait longer than 50ms.
				_ResetEvent.WaitOne(50);
			}
			// Loop until suspended.
			while (_AllowThreadToRun);
			detector.Dispose();
			manager.Dispose();
		}

		// Events.
		public event EventHandler<DInputEventArgs> DevicesUpdated;
		public event EventHandler<DInputEventArgs> StatesUpdated;
		public event EventHandler<DInputEventArgs> StatesRetrieved;
		public event EventHandler<DInputEventArgs> XInputReloaded;

		public event EventHandler<DInputEventArgs> UpdateCompleted;
		object DiUpdatesLock = new object();
		void RefreshAll(DirectInput manager, DeviceDetector detector)
		{
			lock (DiUpdatesLock)
			{
				var game = SettingsManager.CurrentGame;
				// If game is not selected.
				if (game != null)
				{
					// Note: Getting XInput states are not required in order to do emulation.
					// Get states only when form is maximized in order to reduce CPU usage.
					var getXInputStates = SettingsManager.Options.GetXInputStates && Global._MainWindow.FormEventsEnabled;
					// Best place to unload XInput DLL is at the start, because
					// UpdateDiStates(...) function will try to acquire new devices exclusively for force feedback information and control.
					CheckAndUnloadXInputLibrary(game, getXInputStates);
					// Update information about connected devices.
					UpdateDiDevices(manager);
					// Update JoystickStates from devices.
					UpdateDiStates(game, detector);
					// Update XInput states from Custom DirectInput states.
					UpdateXiStates(game);
					// Combine XInput states of controllers.
					CombineXiStates();
					// Update virtual devices from combined states.
					UpdateVirtualDevices(game);
					// Load XInput library before retrieving XInput states.
					CheckAndLoadXInputLibrary(game, getXInputStates);
					// Retrieve XInput states from XInput controllers.
					RetrieveXiStates(getXInputStates);
				}
				// Counts DInput updates per second to show in app's status bar as Hz: #.
				UpdateDelayFrequency();
				// Fire event.
				UpdateCompleted?.Invoke(this, new DInputEventArgs());
			}
		}

		// Count DInput updates per second to show in app's status bar as Hz: #.
		public event EventHandler<DInputEventArgs> FrequencyUpdated;
		int executionCount = 0;
		long lastTime = 0;
		public long CurrentUpdateFrequency;
		void UpdateDelayFrequency()
		{
			var currentTime = _Stopwatch.ElapsedMilliseconds;
			// If one second elapsed then...
			if ((currentTime - lastTime) > 1000)
			{
				CurrentUpdateFrequency = Interlocked.Exchange(ref executionCount, 0);
				FrequencyUpdated?.Invoke(this, new DInputEventArgs());
				lastTime = currentTime;
			}
			Interlocked.Increment(ref executionCount);
		}

		#region ■ IDisposable

		bool IsDisposing;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Do not dispose twice.
				if (IsDisposing)
					return;
				IsDisposing = true;
				Stop();
				Nefarius.ViGEm.Client.ViGEmClient.DisposeCurrent();
				_ResetEvent.Dispose();
			}
		}

		#endregion

	}
}
