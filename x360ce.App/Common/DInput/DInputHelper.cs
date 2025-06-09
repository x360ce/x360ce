//using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
//using JocysCom.ClassLibrary.Win32;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
//using System.Collections.Generic;

//using System.ComponentModel;
using System.Diagnostics;
//using System.Linq;
//using System.Management;
//using System.Runtime.InteropServices;
using System.Threading;
//using x360ce.App.Common.DInput;
//using static JocysCom.ClassLibrary.Processes.MouseHelper;
//using System.Windows.Interop;
//using System.Windows.Input;

namespace x360ce.App.DInput
{
	public partial class DInputHelper : IDisposable
	{

		// --------------------------------------------------------------------------------------------
		// DESCRIPTION
		// --------------------------------------------------------------------------------------------
		// Monitor (WM_DEVICECHANGE) device (HID, Keyboard, Mouse) interface events (DEV_BROADCAST_DEVICEINTERFACE).
		// On detection, set DevicesNeedUpdating = true (also, set 'true' on 'InputLost' error in 'DInputHelper.Step2.UpdateDiStates.cs' > UpdateDiStates()).
		// Build a list of SharpDX.DirectInput.DeviceInstance objects (DeviceClass.GameControl, DeviceClass.Keyboard, DeviceClass.Pointer).
		// The list holds each Win32_PnPEntity.DeviceID prefix, created from SharpDX.DirectInput.DeviceInstance.ProductGuid.
		// For example: 6f1d2b60-d5a0-11cf-bfc7-444553540000 > HID\VID_2B60&PID_6F1D.
		// Win32_PnPEntity.DeviceID prefix'es are used to select Win32_PnPEntity entities existing as SharpDX.DirectInput.DeviceInstance's.

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


		// Constructor
		public DInputHelper()
		{
			CombinedXiConnected = new bool[4];
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
		}

		//===============================================================================================

		#region ■ Device Detector

		// DevicesNeedUpdating can be set (true = update device list as soon as possible) from multiple threads.
		public bool DevicesNeedUpdating = false;
		// DevicesAreUpdating property ensures parameter remains unchanged during RefreshAll(manager, detector) action.
		// CheckAndUnloadXInputLibrary(*) > UpdateDiDevices(*) > CheckAndLoadXInputLibrary(*).
		private bool DevicesAreUpdating = false;

		#endregion

		/// <summary>
		/// _ResetEvent with _Timer is used to limit update refresh frequency.
		/// ms1_1000Hz = 1, ms2_500Hz = 2, ms4_250Hz = 4, ms8_125Hz = 8.
		/// </summary>
		/// 
		ManualResetEvent _ResetEvent = new ManualResetEvent(false);
		JocysCom.ClassLibrary.HiResTimer _Timer;
		UpdateFrequency _Frequency = UpdateFrequency.ms1_1000Hz;

		public UpdateFrequency Frequency
		{
			get => _Frequency;
			set
			{
				_Frequency = value;
				if (_Timer?.Interval != (int)value)
					_Timer.Interval = (int)value;
			}
		}

		/// <summary>
		/// _Stopwatch time is used to calculate the actual update frequency in Hz per second.
		/// </summary>
		private Stopwatch _Stopwatch = new Stopwatch();
		private object timerLock = new object();
		private bool _AllowThreadToRun;

		// Start DInput Service.
		public void StartDInputService()
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
		public void StopDInputService()
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
			_Thread = new Thread(_ThreadStart)
			{
				IsBackground = true
			};
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
			// Loop until allowed to run.
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

		private readonly object DiUpdatesLock = new object();

		private void RefreshAll(DirectInput manager, DeviceDetector detector)
		{
			lock (DiUpdatesLock)
			{
				var game = SettingsManager.CurrentGame;
				// If the game is not selected.
				if (game != null || !Program.IsClosing)
				{
					// Note: Getting XInput states is not required in order to do emulation.
					// Get states only when the form is maximized in order to reduce CPU usage.
					var getXInputStates = SettingsManager.Options.GetXInputStates && Global._MainWindow.FormEventsEnabled;
					// Update hardware.
					if ((DevicesNeedUpdating && !DevicesAreUpdating) || DeviceDetector.DiDevices == null)
					{
						DevicesAreUpdating = true;
						try
						{
							// The best place to unload the XInput DLL is at the start, because UpdateDiStates(...) function
							// will try to acquire new devices exclusively for force feedback information and control.
							CheckAndUnloadXInputLibrary(game, getXInputStates);
							// Update information about connected devices.
							_ = UpdateDiDevices(manager);
							// Load the XInput library before retrieving XInput states.
							CheckAndLoadXInputLibrary(game, getXInputStates);
						}
						finally
						{
							DevicesNeedUpdating = false;
							DevicesAreUpdating = false;
						}
					}
					else
					{
						// Update JoystickStates from devices.
						UpdateDiStates(game, detector);
						// Update XInput states from Custom DirectInput states.
						UpdateXiStates(game);
						// Combine XInput states of controllers.
						CombineXiStates();
						// Update virtual devices from combined states.
						UpdateVirtualDevices(game);
						// Retrieve XInput states from XInput controllers.
						RetrieveXiStates(getXInputStates);
					}
				}
				// Count DInput updates per second to show in the app's status bar as Hz: #.
				UpdateDelayFrequency();
				// Fire update completed event.
				UpdateCompleted?.Invoke(this, new DInputEventArgs());
			}
		}

		// Count DInput updates per second to show in the app's status bar as Hz: #.
		public event EventHandler<DInputEventArgs> FrequencyUpdated;
		private int executionCount = 0;
		private long lastTime = 0;
		public long CurrentUpdateFrequency;
		private void UpdateDelayFrequency()
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

		private bool IsDisposing;
		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
			// PnPDeviceWatcher?.Dispose();
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

				StopDInputService();
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
