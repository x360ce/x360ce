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
			var items = new List<DeviceObjectItem>();
			//var item = new DeviceObjectItem() { Name, OffsetName, Offset, Instance, Usage, Aspect, Flags, GuidValue, GuidName, };
			//items.Add(item);
			return items.ToArray();
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
