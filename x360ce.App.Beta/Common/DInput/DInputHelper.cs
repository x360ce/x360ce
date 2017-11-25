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
	public partial class DInputHelper : IDisposable
	{

		public DInputHelper()
		{
			Manager = new DirectInput();
			InitDeviceDetector();
			CombinedXInputStates = new State[4];
			LiveXInputStates = new State[4];
			XiControllers = new Controller[4];
			XiControllerConnected = new bool[4];
			for (int i = 0; i < 4; i++)
			{
				CombinedXInputStates[i] = new State();
				LiveXInputStates[i] = new State();
				XiControllers[i] = new Controller((UserIndex)i);
			}
			watch = new System.Diagnostics.Stopwatch();
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

		public event EventHandler<EventArgs> FrequencyUpdated;
		public event EventHandler<EventArgs> DevicesUpdated;
		public event EventHandler<EventArgs> StatesUpdated;
		public event EventHandler<EventArgs> StatesRetrieved;
		public event EventHandler<EventArgs> UpdateCompleted;

		DirectInput Manager;
		bool IsStopping;

		public void Start()
		{
			watch.Restart();
			IsStopping = false;
			var ts = new System.Threading.ThreadStart(RefreshAll);
			var t = new System.Threading.Thread(ts);
			t.IsBackground = true;
			t.Start();
		}

		/// <summary>
		/// Watch to monitor update frequency.
		/// </summary>
		System.Diagnostics.Stopwatch watch;
		long lastTime;
		long lastTick;
		long currentTick;
		long lastTickTime;
		public long RequiredFrequency = 1000;
		public long UpdateFrequency;

		public void Stop()
		{
			IsStopping = true;
		}

		object DiUpdatesLock = new object();

		void RefreshAll()
		{
			// Loop until stopped is pressed.
			while (!IsStopping)
			{
				lock (DiUpdatesLock)
				{
					// Update information about connected devices.
					UpdateDiDevices();
					// Update JoystickStates from devices.
					UpdateDiStates();
					// Update XInput states from Custom DirectInput states.
					UpdateXiStates();
					// Combine XInput states of controllers.
					CombineXiStates();
					// Update virtual devices from combined states.
					UpdateVirtualDevices();
					// Retrieve XInput states from XInput controllers.
					RetrieveXiStates();
					// Update pool frequency value and sleep if necessary.
					UpdateDelayFrequency();
					// Fire event.
					var ev = UpdateCompleted;
					if (ev != null)
						ev(this, new EventArgs());
				}
			}
		}

		void UpdateDelayFrequency()
		{
			// Calculate update frequency.
			currentTick++;
			var currentTime = watch.ElapsedMilliseconds;
			var timeChange = currentTime - lastTime;
			var tickChange = currentTick - lastTick;
			// Check if completed too soon
			var requiredTimePerTick = 1000 / RequiredFrequency;
			var timePassed = currentTime - lastTickTime;
			lastTickTime = currentTime;
			var waitTime = requiredTimePerTick - timePassed;
			if (waitTime > 0)
				System.Threading.Thread.Sleep((int)waitTime);
			// If one second passed then...
			if (timeChange > 1000)
			{
				UpdateFrequency = tickChange * 1000 / timeChange;
				lastTime = currentTime;
				lastTick = currentTick;
				var ev = FrequencyUpdated;
				if (ev != null)
					ev(this, new EventArgs());
			}
		}

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
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
