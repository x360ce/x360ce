using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using x360ce.Engine;

namespace x360ce.App.Common.DInput
{
	public partial class DInputHelper
	{

		Dictionary<Guid, DirectInputState> DinputStates;

		Dictionary<Guid, DirectInputState> GetDInputStates()
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
				JoystickState state;
				// Update direct input form and return actions (pressed buttons/dpads, turned axis/sliders).
				//var isOnline = diDevice != null && diDevice.IsOnline;
				//var hasState = isOnline && diDevice.Device != null;
				//var instance = diDevice == null ? "" : " - " + diDevice.InstanceId;
				//var text = "Direct Input" + instance + (isOnline ? hasState ? "" : " - Online" : " - Offline");
				//AppHelper.SetText(DirectInputTabPage, text);
				//DirectInputPanel.UpdateFrom(diDevice, out state);
				DirectInputState diState = null;
				//if (state != null) diState = new DirectInputState(state);
			}




			return null;
		}


		State[] GetXinputStates()
		{

			return null;
		}

	}
}
