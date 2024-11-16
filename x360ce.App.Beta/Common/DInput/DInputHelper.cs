﻿using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Diagnostics;
using System.Linq;

//using System.Linq;
using System.Management;
using System.Threading;
//using static JocysCom.ClassLibrary.Processes.MouseHelper;
//using System.Windows.Input;

namespace x360ce.App.DInput
{
	public partial class DInputHelper : IDisposable
	{
		// Constructor
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

			// Devices are updated on USB connection event and on UpdateDiStates() method device InputLost error in DInputHelper.Step2.UpdateDiStates.cs 
			PnPDeviceWatcher.EventArrived += new EventArrivedEventHandler(PnPDeviceWatcherUSBEvent);
			// Keyboard, Mouse, HID.
			var keys = DeviceDetector.PnPDeviceClassGuids.Keys.ToList();
			PnPDeviceWatcher.Query = new WqlEventQuery(
					"SELECT * FROM __InstanceOperationEvent " +
					"WITHIN 1 " +
					"WHERE TargetInstance ISA 'Win32_PnPEntity' " +
					"AND (__Class = '__InstanceCreationEvent') " +
					"AND (TargetInstance.ClassGuid = '" + keys[0] + "' " +
					"OR TargetInstance.ClassGuid = '" + keys[1] + "' " +
					"OR TargetInstance.ClassGuid = '" + keys[2] + "') " +
  					"AND TargetInstance.DeviceID LIKE 'HID%' " +
					"AND TargetInstance.DeviceID LIKE '%0' ");
			//PnPDeviceWatcher.Query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PnPEntity' AND (__Class = '__InstanceCreationEvent' OR __Class = '__InstanceDeletionEvent')");
			PnPDeviceWatcher.Start();
		}

		#region ■ Device Detector
		// DevicesNeedUpdating property can be set to true (update device list as soon as possible) from multiple threads.
		public bool DevicesNeedUpdating = false;
		// DevicesAreUpdating property ensures parameter remains unchanged during RefreshAll(manager, detector) action.
		private bool DevicesAreUpdating = false; // CheckAndUnloadXInputLibrary(*) > UpdateDiDevices(*) > CheckAndLoadXInputLibrary(*).
		#endregion

		private ManagementEventWatcher PnPDeviceWatcher = new ManagementEventWatcher();
		// Device Manager > Control Panel > Plug and Play devices.
		private async void PnPDeviceWatcherUSBEvent(object sender, EventArrivedEventArgs e)
		{
			if (e.NewEvent["TargetInstance"] is ManagementBaseObject targetInstance)
			{
				Debug.WriteLine($"\nWin32_PnPEntity USB Event: ClassGuid {targetInstance["ClassGuid"]} ({DeviceDetector.PnPDeviceClassGuids[targetInstance["ClassGuid"].ToString()]}). DeviceId {targetInstance["DeviceID"]}.\n");
				await Helper.Delay(OnDevicesChanged);
			}
		}
		private void OnDevicesChanged() => DevicesNeedUpdating = true;

		// Where the current DInput device state is stored:
		//
		//    UserDevice.Device - DirectInput Device (Joystick)
		//    UserDevice.State - DirectInput Device (JoystickState)
		//
		// Process 1 is limited to [125, 250, 500, 1000Hz]
		// Lock
		// {
		//    Acquire:
		//    DiDevices - when a device is detected.
		//	  DiCapabilities - when a device is detected.
		//	  JoStates - from mapped devices.
		//	  DiStates - from converted JoStates.
		//	  XiStates - from converted DiStates
		// }
		//
		// Process 2 is limited to [30Hz] (only when visible).
		// Lock
		// {
		//	  DiDevices, DiCapabilities, DiStates, XiStates
		//	  Update DInput and XInput forms.
		// }

		/// <summary>
		/// _ResetEvent with _Timer is used to limit update refresh frequency.
		/// ms1_1000Hz = 1, ms2_500Hz = 2, ms4_250Hz = 4, ms8_125Hz = 8.
		/// </summary>
		ManualResetEvent _ResetEvent = new ManualResetEvent(false);
		JocysCom.ClassLibrary.HiResTimer _Timer;
		UpdateFrequency _Frequency = UpdateFrequency.ms1_1000Hz;

		public UpdateFrequency Frequency
		{
			get { return _Frequency; }
			set
			{
				_Frequency = value;
				var t = _Timer;
				if (t?.Interval != (int)value)
					t.Interval = (int)value;
			}
		}

		/// <summary>
		/// _Stopwatch time is used to calculate the actual update frequency in Hz per second.
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
				_AllowThreadToRun = false;
				_Timer.Stop();
				_Timer.Elapsed -= Timer_Elapsed;
				_Timer.Dispose();
				_Timer = null;
				_ResetEvent.Set();
				// Wait for the thread to stop.
				_Thread.Join();
			}
		}

		/// <summary>
		/// Method which will create a separate thread for all DInput and XInput updates.
		/// This thread will run a function which will update the BindingList, which will use synchronous Invoke() on the main form running on the main thread.
		/// It can freeze because the main thread is not getting attention to process Invoke() (because attention is on this thread)
		/// and this thread is frozen because it is waiting for Invoke() to finish.
		/// Control when the event can continue.
		/// </summary>
		ThreadStart _ThreadStart;
		Thread _Thread;
		void RefreshAllAsync()
		{
			_ThreadStart = new ThreadStart(ThreadAction);
			_Thread = new Thread(_ThreadStart);
			_Thread.IsBackground = true;
			_Thread.Start();
		}

		public Exception LastException = null;
		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				// Sets the state of the event to signaled, allowing one or more waiting threads to proceed.
				_ResetEvent.Set();
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastException = ex;
			}
		}

		DirectInput directInput = new DirectInput();
		// Suspended is used during re-loading of the XInput library.
		public bool Suspended;
		void ThreadAction()
		{
			Thread.CurrentThread.Name = "RefreshAllThread";
			// DIrect input device querying and force feedback updates will run on a separate thread from MainForm, therefore
			// a separate windows form must be created on the same thread as the process which will access and update the device.
			// detector.DetectorForm will be used to acquire devices.
			// Main job of the detector is to fire an event on device connection (power on) and removal (power off).
			directInput = new DirectInput();
			var detector = new DeviceDetector(false);
			do
			{
				// Sets the state of the event to non-signaled, causing threads to block.
				_ResetEvent.Reset();
				// Perform all updates if not suspended.
				if (!Suspended)
					RefreshAll(directInput, detector);
				// Blocks the current thread until the current WaitHandle receives a signal.
				// The thread will be released by the timer. Do not wait longer than 50ms.
				_ResetEvent.WaitOne(50);
			}
			// Loop until suspended.
			while (_AllowThreadToRun);
			detector.Dispose();
			directInput.Dispose();
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
				// If the game is not selected.
				if (game != null || !Program.IsClosing)
				{
					var getXInputStates = SettingsManager.Options.GetXInputStates && Global._MainWindow.FormEventsEnabled;
					// Note: Getting XInput states is not required in order to do emulation.
					// Get states only when the form is maximized in order to reduce CPU usage.
					// Update hardware.
					if (DevicesNeedUpdating && !DevicesAreUpdating || DeviceDetector.DiDevices == null)
					{
						DevicesAreUpdating = true;
						try
						{

							//Debug.WriteLine("1");
							// The best place to unload the XInput DLL is at the start, because
							// UpdateDiStates(...) function will try to acquire new devices exclusively for force feedback information and control.
							CheckAndUnloadXInputLibrary(game, getXInputStates);
							//Debug.WriteLine("2");
							// Update information about connected devices.
							_ = UpdateDiDevices(manager);
							//Debug.WriteLine("3");
							// Load the XInput library before retrieving XInput states.
							CheckAndLoadXInputLibrary(game, getXInputStates);
							//Debug.WriteLine("4");
						}
						finally
						{
							//Debug.WriteLine("5");
							DevicesNeedUpdating = false;
							DevicesAreUpdating = false;
							//Debug.WriteLine("6");
						}
					}
					else
					{
						// Debug.WriteLine("7.");
						// Update JoystickStates from devices.
						UpdateDiStates(game, detector);
						// Debug.WriteLine("8.");
						// Update XInput states from Custom DirectInput states.
						UpdateXiStates(game);
						 //Debug.WriteLine("9.");
						// Combine XInput states of controllers.
						CombineXiStates();
						 //Debug.WriteLine("10.");
						// Update virtual devices from combined states.
						UpdateVirtualDevices(game);
						// Debug.WriteLine("11.");
						// Retrieve XInput states from XInput controllers.
						RetrieveXiStates(getXInputStates);
						// Debug.WriteLine("12.");
					}
				}
				// Count DInput updates per second to show in the app's status bar as Hz: #.
				UpdateDelayFrequency();
				// Fire event.
				UpdateCompleted?.Invoke(this, new DInputEventArgs());
			}
		}

		// Count DInput updates per second to show in the app's status bar as Hz: #.
		public event EventHandler<DInputEventArgs> FrequencyUpdated;
		int executionCount = 0;
		long lastTime = 0;
		public long CurrentUpdateFrequency;
		void UpdateDelayFrequency()
		{
			var currentTime = _Stopwatch.ElapsedMilliseconds;
			// If one second has elapsed then...
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

		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
			PnPDeviceWatcher?.Dispose();
			directInput?.Dispose();

		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;

			if (disposing)
			{
				// Do not dispose twice.
				if (IsDisposing)
					return;
				IsDisposing = true;

				Stop();
				Nefarius.ViGEm.Client.ViGEmClient.DisposeCurrent();
				_ResetEvent?.Dispose();

				// Nullify managed resources after disposal.
				_Timer = null;
				_Thread = null;
				_ResetEvent = null;

			}

			disposed = true;
		}

		~DInputHelper()
		{
			Dispose(false);
		}

		#endregion

	}
}
