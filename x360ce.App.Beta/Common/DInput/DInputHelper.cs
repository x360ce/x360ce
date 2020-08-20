using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Threading;

namespace x360ce.App.DInput
{
	public partial class DInputHelper : IDisposable
	{

		public DInputHelper()
		{
			CombinedXiConencted = new bool[4];
			CombinedXiStates = new State[4];
			LiveXiControllers = new Controller[4];
			LiveXiConnected = new bool[4];
			LiveXiStates = new State[4];
			for (int i = 0; i < 4; i++)
			{
				CombinedXiStates[i] = new State();
				LiveXiControllers[i] = new Controller((UserIndex)i);
				LiveXiStates[i] = new State();
			}
			watch = new System.Diagnostics.Stopwatch();
			_ResetEvent = new ManualResetEvent(false);
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

		public event EventHandler<DInputEventArgs> FrequencyUpdated;
		public event EventHandler<DInputEventArgs> DevicesUpdated;
		public event EventHandler<DInputEventArgs> StatesUpdated;
		public event EventHandler<DInputEventArgs> StatesRetrieved;
		public event EventHandler<DInputEventArgs> UpdateCompleted;
		public event EventHandler<DInputEventArgs> XInputReloaded;

		/// <summary>
		/// Timer which will be used together with ManualResetEvent to limit update refresh frequency.
		/// </summary>
		JocysCom.ClassLibrary.HiResTimer _timer;

		// Control when event can continue.
		ManualResetEvent _ResetEvent;
		ThreadStart _ThreadStart;
		Thread _Thread;
		bool _AllowThreadToRun;
		object timerLock = new object();

		// Suspended is used during re-loading of XInput library.
		public bool Suspended;

		public void Start()
		{
			lock (timerLock)
			{
				if (_timer != null)
					return;
				watch.Restart();
				_timer = new JocysCom.ClassLibrary.HiResTimer();
				_timer.Elapsed += Timer_Elapsed;
				_timer.Interval = (int)Frequency;
				_timer.Start();
				_AllowThreadToRun = true;
				RefreshAllAsync();
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

		object DiUpdatesLock = new object();

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

		void RefreshAll(DirectInput manager, DeviceDetector detector)
		{
			lock (DiUpdatesLock)
			{
				var game = MainForm.Current.CurrentGame;
				var getXInputStates = SettingsManager.Options.GetXInputStates;
				// Best place to unload XInput DLL is at the start, because
				// UpdateDiStates(...) function will try to acquire new devices exclusively for force feedback information and control.
				CheckAndUnloadXInputLibrarry(game, getXInputStates);
				// Update information about connected devices.
				UpdateDiDevices(manager);
				// Update JoystickStates from devices.
				UpdateDiStates(manager, game, detector);
				// Update XInput states from Custom DirectInput states.
				UpdateXiStates();
				// Combine XInput states of controllers.
				CombineXiStates();
				// Update virtual devices from combined states.
				UpdateVirtualDevices(game);
				// Load XInput library before retrieving XInput states.
				CheckAndLoadXInputLibrary(game, getXInputStates);
				// Retrieve XInput states from XInput controllers.
				RetrieveXiStates(game, getXInputStates);
				// Update pool frequency value every second.
				UpdateDelayFrequency();
				// Fire event.
				var ev = UpdateCompleted;
				if (ev != null)
					ev(this, new DInputEventArgs());
			}
		}

		/// <summary>
		/// Watch to monitor update frequency.
		/// </summary>
		System.Diagnostics.Stopwatch watch;
		long lastTime;
		long currentTick;
		public long CurrentUpdateFrequency;

		public UpdateFrequency Frequency
		{
			get { return _Frequency; }
			set
			{
				_Frequency = value;
				var t = _timer;
				if (t != null && t.Interval != (int)value)
					t.Interval = (int)value;
			}
		}
		UpdateFrequency _Frequency = UpdateFrequency.ms1_1000Hz;

		void UpdateDelayFrequency()
		{
			// Calculate update frequency.
			currentTick++;
			var currentTime = watch.ElapsedMilliseconds;
			// If one second elapsed then...
			if ((currentTime - lastTime) > 1000)
			{
				CurrentUpdateFrequency = currentTick;
				currentTick = 0;
				lastTime = currentTime;
				var ev = FrequencyUpdated;
				if (ev != null)
					ev(this, new DInputEventArgs());
			}
		}

		#region IDisposable

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
