using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

	}


}
