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
			VirtualErrors = new VirtualError[4];
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
		// Written by the interface thread and read by the update thread.
		volatile bool _AllowThreadToRun;
		object timerLock = new object();

		// Suspended is used during re-loading of XInput library.
		public volatile bool Suspended;

		public void Start()
		{
			lock (timerLock)
			{
				if (_timer != null)
					return;
				watch.Restart();
				_timer = new JocysCom.ClassLibrary.HiResTimer((int)Frequency, "DInputHelperTimer");
				_timer.Elapsed += Timer_Elapsed;
				_timer.Start();
				_AllowThreadToRun = true;
				RefreshAllAsync();
			}
		}

		/// <summary>Stops the update thread.</summary>
		/// <returns>False when the thread was still running when this gave up waiting.</returns>
		public bool Stop()
		{
			lock (timerLock)
			{
				if (_timer == null)
					return true;
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
				_AllowThreadToRun = false;
				_ResetEvent.Set();
				// Wait for thread to stop. Use a timeout, because this runs on the interface
				// thread and the worker can be blocked inside a native DirectInput call.
				var thread = _Thread;
				if (thread != null && thread != Thread.CurrentThread && !thread.Join(TimeSpan.FromSeconds(2)))
				{
					// Record it, but not as a fault. The worker is a background thread which
					// stops on its own once the native call returns, and the runtime ends it at
					// exit. Stop() also runs on a settings change and before the error window,
					// so reporting turned an ordinary delay into an error report for the user.
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
						"DirectInput update thread did not stop within 2 seconds.",
						System.Diagnostics.EventLogEntryType.Warning);
					return false;
				}
				return true;
			}
		}

		/// <summary>What the virtual bus last said about each of the four places.</summary>
		/// <remarks>
		/// Kept because it used to be thrown away. A controller that could not be made left the light
		/// showing whatever it showed before, no message anywhere, and nothing to look at: the person
		/// is told the emulator is on and the game receives nothing.
		/// </remarks>
		public VirtualError[] VirtualErrors;

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
			_Thread.Priority = ThreadPriority.Highest;
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
			DirectInput manager = null;
			DeviceDetector detector = null;
			try
			{
				manager = new DirectInput();
				detector = new DeviceDetector(false);
				do
				{
					// Sets the state of the event to non-signaled, causing threads to block.
					_ResetEvent.Reset();
					// Perform all updates if not suspended.
					if (!Suspended)
					{
						try
						{
							RefreshAll(manager, detector);
						}
						catch (Exception ex)
						{
							// One failed update must not end the thread. Losing it stops all
							// device polling and virtual feeding while the window stays alive.
							LastException = ex;
							JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
						}
					}
					// Blocks the current thread until the current WaitHandle receives a signal.
					// Thread will be release by the timer.
					// Do not wait longer than 50ms.
					_ResetEvent.WaitOne(50);
				}
				// Loop until suspended.
				while (_AllowThreadToRun);
			}
			catch (Exception ex)
			{
				LastException = ex;
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
			}
			finally
			{
				// Native objects must be released even when the loop ended with an error.
				if (detector != null)
					detector.Dispose();
				if (manager != null)
					manager.Dispose();
			}
		}

		/// <summary>How often the states shown on screen are read back, in milliseconds.</summary>
		const long DisplayReadIntervalMs = 16;

		long _lastDisplayRead;

		/// <summary>True when enough time has passed to read the states the window shows.</summary>
		/// <remarks>
		/// Only the reading is paced. Whether the XInput library is loaded is decided by the
		/// same answer elsewhere, and pacing that too made the program load and unload the
		/// library many times a second, which the status bar showed as a name flickering in
		/// and out of existence.
		/// </remarks>
		internal bool DueForDisplayRead()
		{
			var now = watch.ElapsedMilliseconds;
			if (now - _lastDisplayRead < DisplayReadIntervalMs)
				return false;
			_lastDisplayRead = now;
			return true;
		}

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
					var getXInputStates = SettingsManager.Options.GetXInputStates && MainForm.Current.FormEventsEnabled;
					// Best place to unload XInput DLL is at the start, because
					// UpdateDiStates(...) function will try to acquire new devices exclusively for force feedback information and control.
					StepWatch(0, () => CheckAndUnloadXInputLibrarry(game, getXInputStates));
					// Update information about connected devices.
					StepWatch(1, () => UpdateDiDevices(manager));
					// Update JoystickStates from devices.
					StepWatch(2, () => UpdateDiStates(manager, game, detector));
					// Update XInput states from Custom DirectInput states.
					StepWatch(3, () => UpdateXiStates(game));
					// Combine XInput states of controllers.
					StepWatch(4, () => CombineXiStates());
					// Update virtual devices from combined states.
					StepWatch(5, () => UpdateVirtualDevices(game));
					// Load XInput library before retrieving XInput states.
					StepWatch(6, () => CheckAndLoadXInputLibrary(game, getXInputStates));
					// Retrieve XInput states from XInput controllers.
					StepWatch(7, () => RetrieveXiStates(game, getXInputStates));
				}
				// Update pool frequency value every second.
				UpdateDelayFrequency();
				// Fire event.
				var ev = UpdateCompleted;
				if (ev != null)
					ev(this, DInputEventArgs.Empty);
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

		/// <summary>Names of the steps of one update, in the order they run.</summary>
		static readonly string[] StepNames = {
			"UnloadXInput", "UpdateDiDevices", "UpdateDiStates", "UpdateXiStates",
			"CombineXiStates", "UpdateVirtualDevices", "LoadXInput", "RetrieveXiStates" };

		/// <summary>Total microseconds spent in each step since the last rate sample.</summary>
		static readonly long[] StepTicks = new long[8];

		/// <summary>Times one step of the update when engine logging is on.</summary>
		/// <remarks>
		/// The rate alone says the loop is slow without saying where. Timing each step says which
		/// one is holding it, which is the difference between reading a number and knowing what to
		/// change. Costs a delegate call per step and nothing else when logging is off.
		/// </remarks>
		static void StepWatch(int index, Action step)
		{
			if (string.IsNullOrEmpty(EngineLogPath))
			{
				step();
				return;
			}
			var started = System.Diagnostics.Stopwatch.GetTimestamp();
			try { step(); }
			finally { StepTicks[index] += System.Diagnostics.Stopwatch.GetTimestamp() - started; }
		}

		/// <summary>File the update rate is written to, one sample per second, or null.</summary>
		/// <remarks>
		/// Set X360CE_ENGINE_LOG to a file path to record how the engine actually runs. Reading
		/// the rate off the window one glance at a time says nothing: the number moves between
		/// full speed and almost nothing from one second to the next, so any single reading can
		/// be made to say whatever the reader hoped. A run of samples can be counted.
		/// Nothing is opened and nothing is written unless the variable is set.
		/// </remarks>
		static readonly string EngineLogPath = Environment.GetEnvironmentVariable("X360CE_ENGINE_LOG");

		static void LogFrequency(long elapsedMilliseconds, long frequency)
		{
			if (string.IsNullOrEmpty(EngineLogPath))
				return;
			try
			{
				var line = new System.Text.StringBuilder();
				line.Append(elapsedMilliseconds).Append(',').Append(frequency);
				for (int i = 0; i < StepTicks.Length; i++)
				{
					// Milliseconds spent in this step during the second just measured.
					var ms = StepTicks[i] * 1000L / System.Diagnostics.Stopwatch.Frequency;
					line.Append(',').Append(StepNames[i]).Append('=').Append(ms);
					StepTicks[i] = 0;
				}
				System.IO.File.AppendAllText(EngineLogPath, line.ToString() + Environment.NewLine);
			}
			catch (System.IO.IOException) { }
			catch (UnauthorizedAccessException) { }
		}

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
				LogFrequency(currentTime, CurrentUpdateFrequency);
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
				var stopped = Stop();
				Nefarius.ViGEm.Client.ViGEmClient.DisposeCurrent();
				// Only once the thread has actually gone. Waiting for it gives up after two
				// seconds, because it can be inside a native call that takes about a second to
				// return - reading every device does. Releasing the handle while it still runs
				// means the next thing it does with it throws, and the program ends on a fault
				// while closing. Left alone the handle costs nothing: the thread runs in the
				// background and both it and the handle go when the process does.
				if (stopped)
					_ResetEvent.Dispose();
			}
		}

		#endregion

	}
}
