using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public class TestDeviceHelper
	{
		public static Guid ProductGuid = new Guid("a10ef011-41ab-4460-8c45-c1ad2dbf7ede");

		public static void EnableTestInstances()
		{
			// Add Test Devices (Make them appear connected online).
			var items = SettingsManager.UserDevices
				.Items.Where(x => x.ProductGuid == ProductGuid).ToArray();
			foreach (var item in items)
			{
				if (item.IsOnline)
					return;
				item.IsOnline = true;
			}
		}

		public static DeviceObjectItem[] GetDeviceObjects()
		{
			var list = new List<DeviceObjectItem>();
			list.Add(new DeviceObjectItem(4, ObjectGuid.XAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 0, "X Axis"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.YAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 1, "Y Axis"));
			list.Add(new DeviceObjectItem(16, ObjectGuid.ZAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 2, "Z Axis"));
			list.Add(new DeviceObjectItem(12, ObjectGuid.RxAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 3, "X Rotation"));
			list.Add(new DeviceObjectItem(8, ObjectGuid.RyAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 4, "Y Rotation"));
			list.Add(new DeviceObjectItem(24, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 0, "Button 0"));
			list.Add(new DeviceObjectItem(25, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 1, "Button 1"));
			list.Add(new DeviceObjectItem(26, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 2, "Button 2"));
			list.Add(new DeviceObjectItem(27, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 3, "Button 3"));
			list.Add(new DeviceObjectItem(28, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 4, "Button 4"));
			list.Add(new DeviceObjectItem(29, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 5, "Button 5"));
			list.Add(new DeviceObjectItem(30, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 6, "Button 6"));
			list.Add(new DeviceObjectItem(31, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 7, "Button 7"));
			list.Add(new DeviceObjectItem(32, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 8, "Button 8"));
			list.Add(new DeviceObjectItem(33, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 9, "Button 9"));
			list.Add(new DeviceObjectItem(20, ObjectGuid.PovController, 0, DeviceObjectTypeFlags.PointOfViewController, 0, "Hat Switch"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 0, "Collection 0 - Game Pad"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 1, "Collection 1"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 2, "Collection 2"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 3, "Collection 3"));
			return list.ToArray();
		}

		public static UserDevice NewUserDevice()
		{
			var instanceName = "";
			for (int i = 1; ; i++)
			{
				instanceName = string.Format("Test Device {0}", i);
				// If device with same name found then continue.
				if (SettingsManager.UserDevices.Items.Any(x => x.InstanceName == instanceName))
					continue;
				break;
			}
			var ud = new UserDevice();
			ud.InstanceGuid = Guid.NewGuid();
			ud.InstanceName = instanceName;
			ud.ProductGuid = TestDeviceHelper.ProductGuid;
			ud.ProductName = instanceName;
			ud.CapAxeCount = 5;
			ud.CapButtonCount = 10;
			ud.CapFlags = 5;
			ud.CapPovCount = 1;
			ud.CapSubtype = 258;
			ud.CapType = 21;
			//ud.HidVendorId = 0;
			//ud.HidProductId = 0;
			ud.HidDescription = instanceName;
			ud.HidClassGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");
			ud.IsEnabled = true;
			return ud;
		}

		static Stopwatch watch;

		public static JoystickState GetCurrentState(UserDevice ud)
		{
			if (watch == null)
			{
				watch = new Stopwatch();
				watch.Start();
			}
			var elapsed = watch.Elapsed;
			// Restart timer if out of limits.
			if (elapsed.TotalMilliseconds > int.MaxValue)
			{
				watch.Restart();
				elapsed = watch.Elapsed;
			}
			// Aquire values.
			var ts = (int)elapsed.TotalSeconds;
			var tm = (int)elapsed.TotalMilliseconds;
			var state = new JoystickState();
			// Set Buttons.
			for (int i = 0; i < ud.CapButtonCount; i++)
			{
				var currentLocation = ts % ud.CapButtonCount;
				// Enable button during its index.
				state.Buttons[i] = currentLocation == i;
			}
			var busy = 4000;
			var half = busy / 2;
			var idle = 2000;
			// Do action for 4 seconds, then stay for 2 seconds idle.
			// 6 = 4 + 2.
			var time = tm % (busy + idle);
			var invert = tm % ((busy + idle) * 2) > (busy + idle);
			// Set POVs.
			for (int i = 0; i < state.PointOfViewControllers.Length; i++)
			{
				// Rotate POVs 360 degrees in 4 seconds forward and back.
				var degree = -1;
				if (time < busy)
				{
					// Shift busy value by half so movement starts from the centre.
					var value = (time + busy / 2) % busy;
					if (time <= half)
					{
						// Convert [   0-1999] to [0-35999].
						degree = DInput.DInputHelper.ConvertRange(0, half - 1, 0, 35999, value);
					}
					else
					{
						// Convert [2000-3999] to [35999-0].
						degree = DInput.DInputHelper.ConvertRange(half, busy - 1, 35999, 0, value);
					}
				}
				state.PointOfViewControllers[i] = degree;
			}

			// Set Axis.
			var axis = CustomDiState.GetAxisFromState(state);
			for (int i = 0; i < axis.Length; i++)
			{
				// Default position is in the center.
				var position = ushort.MaxValue - short.MaxValue;
				// Move axis in 4 seconds, then stay for 2 seconds idle.
				if (time < busy)
				{
					// Convert [0-3999] to [0-2Pi].
					var angle = time * 2 * Math.PI / busy;
					var sine = Math.Sin(angle);
					if (invert && i % 2 == 0)
						sine *= -1f;
					var range = DInput.DInputHelper.ConvertToShort((float)sine);
					position = DInput.DInputHelper.ConvertRange(short.MinValue, short.MaxValue, ushort.MinValue, ushort.MaxValue, range);
				}
				axis[i] = position;
			}
			CustomDiState.SetStateFromAxis(state, axis);
			// Set sliders.
			var sliders = CustomDiState.GetSlidersFromState(state);
			for (int i = 0; i < sliders.Length; i++)
			{
				// Default position is in the center.
				var position = ushort.MaxValue - short.MaxValue;
				// Move slider in 4 seconds, then stay for 2 seconds idle.
				if (time < busy)
				{
					// Convert [0-3999] to [0-2Pi].
					var angle = time * 2 * Math.PI / busy;
					var sine = Math.Sin(angle);
					if (invert && i % 2 == 0)
						sine *= -1f;
					var range = DInput.DInputHelper.ConvertToShort((float)sine);
					position = DInput.DInputHelper.ConvertRange(short.MinValue, short.MaxValue, ushort.MinValue, ushort.MaxValue, range);
				}
				sliders[i] = position;
			}
			CustomDiState.SetStateFromSliders(state, sliders);
			// Return state.
			return state;
		}
	}

}
