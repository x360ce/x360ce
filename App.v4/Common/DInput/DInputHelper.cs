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

		/// <summary>Set to end the wait between passes at once, when the thread is asked to stop.</summary>
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
				if (_AllowThreadToRun)
					return;
				watch.Restart();
				// The clock starts again, so the count and the time of the last sample start again with it;
				// left over from the previous run, they silenced the rate for as long as that run had lasted.
				lastTime = 0;
				currentTick = 0;
				_ResetEvent.Reset();
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
				if (!_AllowThreadToRun)
					return true;
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

		/// <summary>Whether the wait between passes is timed by a high-resolution timer, for the engine log.</summary>
		bool _pacerHighResolution;

		/// <summary>Waits since the last frequency sample that ended over half an interval late, and the most any ended late by, in timestamp ticks.</summary>
		int _lateWaits;
		long _longestLate;

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
			DirectInput manager = null;
			DeviceDetector detector = null;
			JocysCom.ClassLibrary.HiResPacer pacer = null;
			try
			{
				manager = new DirectInput();
				detector = new DeviceDetector(false);
				// This thread times its own passes rather than being woken through an event by a timer on
				// another thread. Such a timer falls a clock tick behind now and then and fires twice to
				// catch up, and the second lands on an event already set: about one pass in twenty is lost.
				pacer = new JocysCom.ClassLibrary.HiResPacer(_ResetEvent);
				_pacerHighResolution = pacer.UsesHighResolution;
				do
				{
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
					// Until the next pass is due, or the thread is asked to stop.
					var interval = (int)_Frequency;
					var late = pacer.Wait(interval);
					if (late > _longestLate)
						_longestLate = late;
					if (late * 2000L > System.Diagnostics.Stopwatch.Frequency * interval)
						_lateWaits++;
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
				if (pacer != null)
					pacer.Dispose();
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
			var passStarted = StepMark();
			lock (DiUpdatesLock)
			{
				// The places the interface shows are read on a worker, only when something has
				// changed, and never waited for here.
				XInputPlaces.ReadWhenStale();
				var game = SettingsManager.CurrentGame;
				// If game is not selected.
				if (game != null)
				{
					// Note: Getting XInput states are not required in order to do emulation.
					// Get states only when form is maximized in order to reduce CPU usage.
					var getXInputStates = SettingsManager.Options.GetXInputStates && MainForm.Current.FormEventsEnabled;
					var mark = StepMark();
					// Best place to unload XInput DLL is at the start, because
					// UpdateDiStates(...) function will try to acquire new devices exclusively for force feedback information and control.
					CheckAndUnloadXInputLibrarry(game, getXInputStates);
					StepDone(0, ref mark);
					// Update information about connected devices.
					UpdateDiDevices(manager);
					StepDone(1, ref mark);
					// Update JoystickStates from devices.
					UpdateDiStates(manager, game, detector);
					StepDone(2, ref mark);
					// Update XInput states from Custom DirectInput states.
					UpdateXiStates(game);
					StepDone(3, ref mark);
					// Combine XInput states of controllers.
					CombineXiStates();
					StepDone(4, ref mark);
					// Update virtual devices from combined states.
					UpdateVirtualDevices(game);
					StepDone(5, ref mark);
					// Load XInput library before retrieving XInput states.
					CheckAndLoadXInputLibrary(game, getXInputStates);
					StepDone(6, ref mark);
					// Retrieve XInput states from XInput controllers.
					RetrieveXiStates(game, getXInputStates);
					StepDone(7, ref mark);
				}
				// Update pool frequency value every second.
				UpdateDelayFrequency();
				// Fire event.
				var ev = UpdateCompleted;
				if (ev != null)
					ev(this, new DInputEventArgs());
				PassDone(passStarted);
			}
		}

		/// <summary>
		/// Watch to monitor update frequency.
		/// </summary>
		System.Diagnostics.Stopwatch watch;
		long lastTime;
		long currentTick;
		public long CurrentUpdateFrequency;

		/// <summary>How often a pass runs. The update thread reads it before every wait, so a change applies at once.</summary>
		public UpdateFrequency Frequency
		{
			get { return _Frequency; }
			set { _Frequency = value; }
		}
		volatile UpdateFrequency _Frequency = UpdateFrequency.ms1_1000Hz;

		/// <summary>Names of the steps of one update, in the order they run.</summary>
		static readonly string[] StepNames = {
			"UnloadXInput", "UpdateDiDevices", "UpdateDiStates", "UpdateXiStates",
			"CombineXiStates", "UpdateVirtualDevices", "LoadXInput", "RetrieveXiStates" };

		/// <summary>Time spent in each step since the last rate sample, in timestamp ticks.</summary>
		static readonly long[] StepTicks = new long[8];

		/// <summary>Time each step took in the pass being run, in timestamp ticks.</summary>
		static readonly long[] PassStepTicks = new long[8];

		/// <summary>Passes since the last rate sample that took longer than the timer interval.</summary>
		static int _slowPasses;

		/// <summary>The longest pass since the last rate sample, in timestamp ticks, and the step that took most of it.</summary>
		static long _longestPass;
		static int _longestPassStep = -1;

		/// <summary>When the last pass started, and the longest time between two pass starts since the last rate sample.</summary>
		static long _lastPassStart;
		static long _longestGap;

		/// <summary>The time to measure a step from when engine logging is on, or 0 when it is off.</summary>
		/// <remarks>
		/// The rate alone says the loop is slow without saying where. Timing each step says which
		/// one is holding it, which is the difference between reading a number and knowing what to
		/// change. A step is timed from the end of the one before, so the loop allocates nothing
		/// for it and costs one comparison per step when logging is off.
		/// </remarks>
		static long StepMark()
		{
			return string.IsNullOrEmpty(EngineLogPath) ? 0 : System.Diagnostics.Stopwatch.GetTimestamp();
		}

		/// <summary>Adds the time since <paramref name="mark"/> to the step, and starts the next step's time there.</summary>
		static void StepDone(int index, ref long mark)
		{
			if (mark == 0)
				return;
			var now = System.Diagnostics.Stopwatch.GetTimestamp();
			var took = now - mark;
			StepTicks[index] += took;
			PassStepTicks[index] = took;
			mark = now;
		}

		/// <summary>Counts a pass that ran past the interval, and keeps the longest one.</summary>
		/// <remarks>
		/// A pass that runs past the interval delays the next one, and one that runs past two drops
		/// a pass from the second. The count says how many passes did that, and the longest says
		/// whether a few long stalls or many small overruns are behind it. A long gap between two
		/// short passes is the wait ending late, which no change to the passes themselves can fix.
		/// </remarks>
		void PassDone(long started)
		{
			if (started == 0)
				return;
			if (_lastPassStart != 0 && started - _lastPassStart > _longestGap)
				_longestGap = started - _lastPassStart;
			_lastPassStart = started;
			var took = System.Diagnostics.Stopwatch.GetTimestamp() - started;
			if (took * 1000L > System.Diagnostics.Stopwatch.Frequency * (long)_Frequency)
				_slowPasses++;
			if (took > _longestPass)
			{
				_longestPass = took;
				_longestPassStep = -1;
				for (int i = 0; i < PassStepTicks.Length; i++)
					if (PassStepTicks[i] > 0 && (_longestPassStep < 0 || PassStepTicks[i] > PassStepTicks[_longestPassStep]))
						_longestPassStep = i;
			}
			Array.Clear(PassStepTicks, 0, PassStepTicks.Length);
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

		void LogFrequency(long elapsedMilliseconds, long frequency)
		{
			if (string.IsNullOrEmpty(EngineLogPath))
				return;
			try
			{
				var line = new System.Text.StringBuilder();
				line.Append(elapsedMilliseconds).Append(',').Append(frequency);
				// How the wait between passes is timed, and how often it ended late, so a slow loop can be
				// told from a late timer: waits over half an interval late, and the most any was late by in microseconds.
				line.Append(",timer=").Append(_pacerHighResolution ? "hires" : "std");
				line.Append(",interval=").Append((int)_Frequency);
				line.Append(",late=").Append(_lateWaits).Append('/').Append(_longestLate * 1000000L / System.Diagnostics.Stopwatch.Frequency);
				_lateWaits = 0;
				_longestLate = 0;
				line.Append(",res=").Append(JocysCom.ClassLibrary.HiResTimer.CurrentResolutionMs.ToString("0.0"));
				line.Append(",throttle=").Append(JocysCom.ClassLibrary.HiResTimer.PowerThrottlingState);
				// How long the worker took over the last device list read taken in this second, or nothing.
				line.Append(",read=").Append(System.Threading.Interlocked.Exchange(ref _deviceReadMs, 0));
				line.Append('(').Append(_deviceReadPhases).Append(')');
				// What each of the four emulated controllers is doing, so a missing one can be told from a slow loop.
				line.Append(",pads=");
				for (int i = 0; i < 4; i++)
				{
					if (i > 0)
						line.Append('/');
					var plugging = _plugging[i];
					line.Append(plugging != null && !plugging.IsCompleted ? "plugging" : FeedingState[i] == true ? "on" : FeedingState[i] == false ? "off" : "?");
					line.Append(':').Append(VirtualErrors[i]);
				}
				// What the window's XInput view is given: whether the library is loaded and read, and each
				// pad's place and whether that place answers as connected.
				line.Append(",xi=").Append(SharpDX.XInput.Controller.IsLoaded ? "loaded" : "unloaded")
					.Append(XiStatesRead ? "+read" : "+idle");
				for (int i = 0; i < 4; i++)
					line.Append('/').Append(XiPlaceForPad[i]).Append(LiveXiConnected[i] ? "c" : "-");
				// Passes that ran past the timer interval, and the longest pass in microseconds with the step that held it.
				line.Append(",slow=").Append(_slowPasses);
				line.Append(",longest=").Append(_longestPass * 1000000L / System.Diagnostics.Stopwatch.Frequency);
				line.Append('(').Append(_longestPassStep < 0 ? "-" : StepNames[_longestPassStep]).Append(')');
				line.Append(",gap=").Append(_longestGap * 1000000L / System.Diagnostics.Stopwatch.Frequency);
				_slowPasses = 0;
				_longestPass = 0;
				_longestPassStep = -1;
				_longestGap = 0;
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
				// Waited for, so the controller a plug under way makes is taken away with the rest.
				WaitForPlugging(TimeSpan.FromSeconds(8));
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
