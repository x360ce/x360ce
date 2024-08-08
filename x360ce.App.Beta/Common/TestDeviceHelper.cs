using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
				lock (SettingsManager.UserDevices.SyncRoot)
					item.IsOnline = true;
			}
		}

		public static DeviceObjectItem[] GetDeviceObjects()
		{
			var list = new List<DeviceObjectItem>();
			list.Add(new DeviceObjectItem((int)JoystickOffset.X, ObjectGuid.XAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 0, "X Axis"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Y, ObjectGuid.YAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 1, "Y Axis"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Z, ObjectGuid.ZAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 2, "Z Axis"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.RotationY, ObjectGuid.RxAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 3, "X Rotation"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.RotationX, ObjectGuid.RyAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 4, "Y Rotation"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons0, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 0, "Button 0"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons1, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 1, "Button 1"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons2, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 2, "Button 2"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons3, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 3, "Button 3"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons4, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 4, "Button 4"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons5, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 5, "Button 5"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons6, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 6, "Button 6"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons7, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 7, "Button 7"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons8, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 8, "Button 8"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.Buttons9, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 9, "Button 9"));
			list.Add(new DeviceObjectItem((int)JoystickOffset.PointOfViewControllers0, ObjectGuid.PovController, 0, DeviceObjectTypeFlags.PointOfViewController, 0, "Hat Switch"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 0, "Collection 0 - Game Pad"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 1, "Collection 1"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 2, "Collection 2"));
			list.Add(new DeviceObjectItem(0, ObjectGuid.Unknown, 0, DeviceObjectTypeFlags.Collection | DeviceObjectTypeFlags.NoData, 3, "Collection 3"));
			// Update DIndexes.
			for (int i = 0; i < list.Count; i++)
				list[i].DiIndex = list[i].Instance;
			return list.ToArray();
		}

		public static UserDevice NewUserDevice()
		{
			var instanceName = "";
			var productName = "Test Device";
			for (int i = 1; ; i++)
			{
				instanceName = string.Format("{0} {1}", productName, i);
				// If device with same name found then continue.
				if (SettingsManager.UserDevices.Items.Any(x => x.InstanceName == instanceName))
					continue;
				break;
			}
			var ud = new UserDevice();
			ud.HidManufacturer = "X360CE";
			ud.DevManufacturer = "X360CE";
			ud.InstanceGuid = Guid.NewGuid();
			ud.InstanceName = instanceName;
			ud.ProductGuid = ProductGuid;
			ud.ProductName = productName;
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
			// Acquire values.
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
			// Do action for 4 seconds [0-3999] ms.
			var busy = 4000;
			var half = busy / 2;
			// then stay for 2 seconds idle [4000-5999] ms.
			var idle = 2000;
			// 6 = 4 + 2.
			var time = tm % (busy + idle);
			var invert = tm % ((busy + idle) * 2) > (busy + idle);
			// Set POVs.
			for (int i = 0; i < ud.CapPovCount; i++)
			{
				// Rotate POVs 360 degrees in 4 seconds forward and back.
				var degree = -1;
				if (time < busy)
				{
					// Shift busy value by half so movement starts from the centre.
					var value = (time + busy / 2) % busy;
					if (value < half)
					{
						// Convert [   0-1999] to [0-35999].
						degree = ConvertHelper.ConvertRange(value, 0, half - 1, 0, 35999);
					}
					else
					{
						// Convert [2000-3999] to [35999-0].
						degree = ConvertHelper.ConvertRange(value, half, busy - 1, 35999, 0);
					}
				}
				state.PointOfViewControllers[i] = degree;
			}
			// Set Axis.
			var axis = new int[CustomDiState.MaxAxis];
			CustomDiState.CopyAxis(state, axis);
			// Get information about axis.
			var axisObjects = ud.DeviceObjects
				.Where(x => x.Flags.HasFlag(DeviceObjectTypeFlags.AbsoluteAxis) || x.Flags.HasFlag(DeviceObjectTypeFlags.RelativeAxis)).ToArray();
			for (int i = 0; i < axisObjects.Count(); i++)
			{
				var ao = axisObjects[i];
				// If axis index is even.
				var isEven = i % 2 == 0;
				var position = isEven
					// Default position is in the center.
					? ushort.MaxValue - short.MaxValue
					// Default position is at the bottom.
					: 0;
				// Move axis in 4 seconds, then stay for 2 seconds idle.
				if (time < busy)
				{
					if (isEven)
					{
						// Convert [0-3999] to [0-2Pi].
						var angle = time * 2 * Math.PI / busy;
						var sine = Math.Sin(angle);
						if (invert && isEven)
							sine *= -1f;
						var range = ConvertHelper.ConvertToShort((float)sine);
						position = ConvertHelper.ConvertRange(range, short.MinValue, short.MaxValue, ushort.MinValue, ushort.MaxValue);
					}
					else
					{
						position = time < half
							// Move up [0-1999].
							? ConvertHelper.ConvertRange(time, 0, half - 1, ushort.MinValue, ushort.MaxValue)
							// Move down  [2000-3999].
							: ConvertHelper.ConvertRange(time, half, busy - 1, ushort.MaxValue, ushort.MinValue);
					}
				}
				axis[i] = position;
			}
			CustomDiState.CopyAxis(axis, state);
			// Get sliders array.
			var sliders = new int[CustomDiState.MaxSliders];
			CustomDiState.CopySliders(state, sliders);
			// Set sliders.
			for (int i = 0; i < sliders.Length; i++)
			{
				var isEven = i % 2 == 0;
				var position = isEven
					// Default position is in the center.
					? ushort.MaxValue - short.MaxValue
					// Default position is at the bottom.
					: 0;
				// Move slider in 4 seconds, then stay for 2 seconds idle.
				if (time < busy)
				{
					if (isEven)
					{
						// Convert [0-3999] to [0-2Pi].
						var angle = time * 2 * Math.PI / busy;
						var sine = Math.Sin(angle);
						if (invert && isEven)
							sine *= -1f;
						var range = ConvertHelper.ConvertToShort((float)sine);
						position = ConvertHelper.ConvertRange(range, short.MinValue, short.MaxValue, ushort.MinValue, ushort.MaxValue);
					}
					else
					{
						position = time < half
							// Move up.
							? ConvertHelper.ConvertRange(time, 0, half - 1, ushort.MinValue, ushort.MaxValue)
							// Move down.
							: ConvertHelper.ConvertRange(time, half, busy - 1, ushort.MaxValue, ushort.MinValue);
					}
				}
				sliders[i] = position;
			}
			CustomDiState.CopySliders(sliders, state);
			// Return state.
			return state;
		}
	}

}
