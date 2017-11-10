using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		void UpdateDiStates()
		{
			// Get all mapped user instances.
			var instanceGuids = SettingsManager.Settings.Items
				.Where(x => x.MapTo > (int)MapTo.None)
				.Select(x => x.InstanceGuid).ToArray();
			// Get all connected devices.
			var userDevices = SettingsManager.UserDevices.Items
				.Where(x => instanceGuids.Contains(x.InstanceGuid) && x.IsOnline)
				.ToArray();
			for (int i = 0; i < userDevices.Count(); i++)
			{
				var ud = userDevices[i];
				JoystickState state = null;
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
				var isOnline = ud != null && ud.IsOnline;
				if (isOnline)
				{
					var device = ud.Device;
					if (device != null)
					{
						try
						{
							device.Acquire();
							state = device.GetCurrentState();
						}
						catch (Exception ex)
						{
							var error = ex;
						}
					}
					// If this is test device then...
					else if (TestDeviceHelper.ProductGuid.Equals(ud.ProductGuid))
					{
						state = TestDeviceHelper.GetCurrentState(ud);
					}
				}
				ud.JoState = state ?? new JoystickState();
				ud.DiState = new CustomDiState(ud.JoState);
			}
		}

	}
}
