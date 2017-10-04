using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper: IDisposable
	{

		public DInputHelper()
		{
			Manager = new DirectInput();
			InitDeviceDetector();
		}

		public void Start()
		{
			watch.Restart();
			IsStopping = false;
			var ts = new System.Threading.ThreadStart(RefreshAll);
			var t = new System.Threading.Thread(ts);
			t.IsBackground = true;
			t.Start();
		}

		bool IsStopping;

		/// <summary>
		/// Watch to monitor update frequency.
		/// </summary>
		System.Diagnostics.Stopwatch watch;
		long lastTime;
		long lastTick;
		long currentTick;
		long UpdateFrequency;

		public void Stop()
		{
			IsStopping = true;
		}

		void RefreshAll()
		{
			// Loop until stopped is pressed.
			while (!IsStopping)
			{
				lock (DiUpdatesLock)
				{
					// Make sure that interface handle is created, before starting device updates.
					if (UpdateDevicesEnabled && UpdateDevicesFinished)
					{
						UpdateDevicesEnabled = false;
						// This property will make sure that only one 'UpdateDevices' is running at the time.
						UpdateDevicesFinished = false;
						UpdateDevices();
					}
					UpdateDiStates();
					// Calculate update frequency.
					currentTick++;
					// Recalculate about every second.
					var currentTime = watch.ElapsedMilliseconds;
					var timeChange = currentTime - lastTime;
					var tickChange = currentTick - lastTick;
					// If one second passed then...
					if (timeChange > 1000)
					{
						UpdateFrequency = tickChange * 1000 / timeChange;
						lastTime = currentTime;
						lastTick = currentTick;
					}
				}
			}
		}

		// Process 1
		// limited to [125, 250, 500, 1000Hz]
		// Lock
		// {
		//    Aquire:
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

		DirectInput Manager;

		Dictionary<Guid, DirectInputState> DinputStates;

		object DiUpdatesLock = new object();

		void UpdateDiStates()
		{
			// Get all mapped user instances.
			var instances = SettingsManager.Settings.Items
				.Where(x => x.MapTo > (int)MapTo.None)
				.Select(x => x.InstanceGuid).ToArray();
			// Get all connected devices.
			var devices = SettingsManager.UserDevices.Items
				.Where(x => instances.Contains(x.InstanceGuid) && x.IsOnline)
				.ToArray();
			for (int i = 0; i < devices.Count(); i++)
			{
				var diDevice = devices[i];
				//JoystickState state;
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
				//var isOnline = diDevice != null && diDevice.IsOnline;
				//var hasState = isOnline && diDevice.Device != null;
				//var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
				//var text = "Direct Input" + instance + (isOnline ? hasState ? "" : " - Online" : " - Offline");
				//AppHelper.SetText(DirectInputTabPage, text);
				//DirectInputPanel.UpdateFrom(diDevice, out state);
				//DirectInputState diState = null;
				//if (state != null) diState = new DirectInputState(state);
			}
		}


		State[] GetXinputStates()
		{

			return null;
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsDisposing = true;
				UnInitDeviceDetector();
				if (Manager != null)
				{
					Manager.Dispose();
					Manager = null;
				}
			}
		}

		#endregion


	}
}
