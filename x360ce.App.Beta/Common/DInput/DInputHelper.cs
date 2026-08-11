//using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
//using JocysCom.ClassLibrary.Win32;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;

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
			ControllerHealth = new ControllerPipelineHealth[4];

			for (int i = 0; i < 4; i++)
			{
				CombinedXiStates[i] = new State();
				LiveXiStates[i] = new State();
				LiveXiControllers[i] = new Controller((UserIndex)i);
				ControllerHealth[i] = new ControllerPipelineHealth();
			}
		}

		readonly object controllerHealthLock = new object();
		readonly HashSet<Guid> forceFeedbackDisabledDevices = new HashSet<Guid>();
		readonly Dictionary<Guid, DateTime> deviceFailureLogTimes = new Dictionary<Guid, DateTime>();
		public ControllerPipelineHealth[] ControllerHealth { get; }

		bool ShouldLogDeviceFailure(Guid instanceGuid)
		{
			var now = DateTime.UtcNow;
			if (deviceFailureLogTimes.TryGetValue(instanceGuid, out var previous) &&
				now.Subtract(previous).TotalSeconds < 10)
				return false;
			deviceFailureLogTimes[instanceGuid] = now;
			return true;
		}

		public ControllerPipelineHealth[] GetControllerHealth()
		{
			lock (controllerHealthLock)
			{
				var result = new ControllerPipelineHealth[ControllerHealth.Length];
				for (var i = 0; i < result.Length; i++)
					result[i] = ControllerHealth[i].Clone();
				return result;
			}
		}

		void SetControllerHealth(
			int index,
			bool? physicalInputOk = null,
			bool? mappingOk = null,
			bool? virtualBusOk = null,
			bool? virtualTargetConnected = null,
			bool? stateSubmitOk = null,
			bool setLastError = false,
			string lastError = null)
		{
			lock (controllerHealthLock)
			{
				var after = ControllerHealth[index];
				var previousPhysicalInputOk = after.PhysicalInputOk;
				var previousMappingOk = after.MappingOk;
				var previousVirtualBusOk = after.VirtualBusOk;
				var previousVirtualTargetConnected = after.VirtualTargetConnected;
				var previousStateSubmitOk = after.StateSubmitOk;
				var previousLastError = after.LastError;
				if (physicalInputOk.HasValue) after.PhysicalInputOk = physicalInputOk.Value;
				if (mappingOk.HasValue) after.MappingOk = mappingOk.Value;
				if (virtualBusOk.HasValue) after.VirtualBusOk = virtualBusOk.Value;
				if (virtualTargetConnected.HasValue) after.VirtualTargetConnected = virtualTargetConnected.Value;
				if (stateSubmitOk.HasValue) after.StateSubmitOk = stateSubmitOk.Value;
				if (setLastError) after.LastError = lastError;
				if (previousPhysicalInputOk == after.PhysicalInputOk &&
					previousMappingOk == after.MappingOk &&
					previousVirtualBusOk == after.VirtualBusOk &&
					previousVirtualTargetConnected == after.VirtualTargetConnected &&
					previousStateSubmitOk == after.StateSubmitOk &&
					string.Equals(previousLastError, after.LastError, StringComparison.Ordinal))
					return;
				after.UpdatedUtc = DateTime.UtcNow;
				x360ce.App.Diagnostics.OperationalLog.Current?.Write(
					"controller_pipeline_health_changed", fields:
					new Dictionary<string, object>
					{
						["slot"] = index + 1,
						["physicalInputOk"] = after.PhysicalInputOk,
						["mappingOk"] = after.MappingOk,
						["virtualBusOk"] = after.VirtualBusOk,
						["virtualTargetConnected"] = after.VirtualTargetConnected,
						["stateSubmitOk"] = after.StateSubmitOk,
					});
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
		private volatile bool _AllowThreadToRun;

		// Start DInput Service.
		public void StartDInputService()
		{
			lock (timerLock)
			{
				if (_Timer != null)
					return;
				if (_Thread != null && _Thread.IsAlive)
				{
					x360ce.App.Diagnostics.OperationalLog.Current?.Write(
						"dinput_restart_skipped", "warn");
					return;
				}
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
				// Never let a broken driver make a UI caller wait indefinitely.
				var thread = _Thread;
				if (thread != null && thread != Thread.CurrentThread)
				{
					if (!thread.Join(TimeSpan.FromSeconds(2)))
						x360ce.App.Diagnostics.OperationalLog.Current?.Write(
							"dinput_stop_timeout", "warn");
					else
						_Thread = null;
				}
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

		// DirectInput can block while loading a broken HID stack. Create it only on
		// the dedicated refresh thread, never while constructing startup services.
		DirectInput directInput;
		// Suspended is used during re-loading of the XInput library.
		public volatile bool Suspended;
		void ThreadAction()
		{
			Thread.CurrentThread.Name = "RefreshAllThread";
			DeviceDetector detector = null;
			try
			{
				x360ce.App.Diagnostics.OperationalLog.Current?.Write("dinput_worker_started");
				// Native DirectInput and detector objects are created only on this worker.
				directInput = new DirectInput();
				detector = new DeviceDetector(false);
				do
				{
					_ResetEvent.Reset();
					if (!Suspended)
					{
						try
						{
							RefreshAll(directInput, detector);
						}
						catch (Exception ex)
						{
							LastException = ex;
							JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
							x360ce.App.Diagnostics.OperationalLog.Current?.WriteException(
								"dinput_refresh_failed", ex);
						}
					}
					_ResetEvent.WaitOne(50);
				}
				while (_AllowThreadToRun);
			}
			catch (Exception ex)
			{
				LastException = ex;
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				x360ce.App.Diagnostics.OperationalLog.Current?.WriteException(
					"dinput_worker_failed", ex);
			}
			finally
			{
				try { detector?.Dispose(); } catch (Exception) { }
				try { directInput?.Dispose(); } catch (Exception) { }
				detector = null;
				directInput = null;
				x360ce.App.Diagnostics.OperationalLog.Current?.Write("dinput_worker_stopped");
			}
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
							UpdateDiDevices(manager);
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
		private long lastFrequencyLogTime = 0;
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
				if (currentTime - lastFrequencyLogTime >= 10000)
				{
					var requested = 1000 / Math.Max(1, (int)Frequency);
					var level = CurrentUpdateFrequency < requested / 2 ? "warn" : "info";
					x360ce.App.Diagnostics.OperationalLog.Current?.Write(
						"controller_poll_frequency", level,
						new Dictionary<string, object>
						{
							["requestedHz"] = requested,
							["actualHz"] = CurrentUpdateFrequency,
							["mappedDeviceCount"] = mappedDevices?.Length ?? 0,
						});
					lastFrequencyLogTime = currentTime;
				}
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
