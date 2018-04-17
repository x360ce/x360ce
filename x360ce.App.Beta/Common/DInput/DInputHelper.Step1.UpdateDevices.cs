using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		#region Device Detector

		/// <summary>
		/// Main job of detector is to fire event on device connection (power on) and removal (power off).
		/// </summary>
		DeviceDetector detector;

		public bool UpdateDevicesEnabled;

		// Detector will be initialized on a separate thread.
		public void InitDeviceDetector()
		{
			UpdateDevicesEnabled = true;
			detector = new DeviceDetector(false);
		}

		public void UnInitDeviceDetector()
		{
			// Can't dispose here due to cross-threading.
			//detector.Dispose();
		}

		#endregion

		object UpdateDevicesLock = new object();
		public int RefreshDevicesCount;

		void UpdateDiDevices()
		{
			if (!UpdateDevicesEnabled)
				return;
			UpdateDevicesEnabled = false;
			// Make sure that interface handle is created, before starting device updates.
			UserDevice[] deleteDevices;
			// Add connected devices.
			var insertDevices = new List<UserDevice>();
			// List of connected devices (can be a very long operation).
			var devices = new List<DeviceInstance>();
			// Controllers.
			var controllerInstances = Manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices).ToList();
			foreach (var item in controllerInstances)
				devices.Add(item);
			// Pointers.
			var pointerInstances = Manager.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AllDevices).ToList();
			foreach (var item in pointerInstances)
				devices.Add(item);
			// Keyboards.
			var keyboardInstances = Manager.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AllDevices).ToList();
			foreach (var item in keyboardInstances)
				devices.Add(item);
			if (Program.IsClosing)
				return;
			// List of connected devices.
			var deviceInstanceGuid = devices.Select(x => x.InstanceGuid).ToList();
			// List of current devices.
			var currentInstanceGuids = SettingsManager.UserDevices.Items.Select(x => x.InstanceGuid).ToArray();
			deleteDevices = SettingsManager.UserDevices.Items.Where(x => !deviceInstanceGuid.Contains(x.InstanceGuid)).ToArray();
			var addedDevices = devices.Where(x => !currentInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			var updatedDevices = devices.Where(x => currentInstanceGuids.Contains(x.InstanceGuid)).ToArray();
            // Must find better way to find Device than by Vendor ID and Product ID.
            DeviceInfo[] devInfos = null;
            DeviceInfo[] intInfos = null;
            if (addedDevices.Length > 0 || updatedDevices.Length > 0)
            {
                devInfos = DeviceDetector.GetDevices();
                // Controllers.
                var controllers = addedDevices
                    .Where(x => x.Type != SharpDX.DirectInput.DeviceType.Mouse && x.Type != SharpDX.DirectInput.DeviceType.Keyboard)
                    .Select(x => new Joystick(Manager, x.InstanceGuid)).ToArray();
                // Get interfaces.
                var interfacePaths = controllers.Select(x => x.Properties.InterfacePath).ToArray();
                intInfos = DeviceDetector.GetInterfaces(interfacePaths);
            }
            if (addedDevices.Length > 0)
			{
				//Joystick    = new Guid("6f1d2b70-d5a0-11cf-bfc7-444553540000");
				//SysMouse    = new Guid("6f1d2b60-d5a0-11cf-bfc7-444553540000");
				//SysKeyboard = new Guid("6f1d2b61-d5a0-11cf-bfc7-444553540000");
				for (int i = 0; i < addedDevices.Length; i++)
				{
					var device = addedDevices[i];
					var ud = new UserDevice();
                    DeviceInfo hid;
					RefreshDevice(ud, device, intInfos, devInfos, out hid);
					var isVirtual = false;
					if (hid != null)
					{
						DeviceInfo p = hid;
						do
						{
							p = DeviceDetector.GetParentDevice(Guid.Empty, JocysCom.ClassLibrary.Win32.DIGCF.DIGCF_ALLCLASSES, p.DeviceId);
							if (p != null && string.Compare(p.HardwareId, VirtualDriverInstaller.ViGEmBusHardwareId, true) == 0)
							{
								isVirtual = true;
								break;
							}
						} while (p != null);
					}
					if (!isVirtual)
						insertDevices.Add(ud);
				}
			}
			//if (insertDevices.Count > 0)
			//{
			//	CloudPanel.Add(CloudAction.Insert, insertDevices.ToArray(), true);
			//}
			for (int i = 0; i < updatedDevices.Length; i++)
			{
				var device = updatedDevices[i];
				var ud = SettingsManager.UserDevices.Items.First(x => x.InstanceGuid.Equals(device.InstanceGuid));
                DeviceInfo hid;
                // Will refresh device and fill more values with new x360ce app if available.
                RefreshDevice(ud, device, intInfos, devInfos, out hid);
            }
			if (Program.IsClosing)
				return;
			// Remove disconnected devices.
			for (int i = 0; i < deleteDevices.Length; i++)
			{
				deleteDevices[i].IsOnline = false;
			}
			for (int i = 0; i < insertDevices.Count; i++)
			{
				var ud = insertDevices[i];
				SettingsManager.UserDevices.Items.Add(ud);
			}
			// Enable Test instances.
			TestDeviceHelper.EnableTestInstances();
			RefreshDevicesCount++;
			var ev = DevicesUpdated;
			if (ev != null)
				ev(this, new DInputEventArgs());
			//	var game = CurrentGame;
			//	if (game != null)
			//	{
			//		// Auto-configure new devices.
			//		AutoConfigure(game);
			//	}
		}

        /// <summary>
        /// Refresh device.
        /// </summary>
		void RefreshDevice(UserDevice ud, DeviceInstance device, DeviceInfo[] intInfos, DeviceInfo[] devInfos, out DeviceInfo hid)
		{
            hid = null;
            if (Program.IsClosing)
				return;
			ud.LoadInstance(device);
			var joystick = new Joystick(Manager, device.InstanceGuid);
			ud.Device = joystick;
			ud.LoadCapabilities(joystick.Capabilities);
			// If device is set as offline then make it online.
			if (!ud.IsOnline)
				ud.IsOnline = true;
            DeviceInfo dev = null;
            if (device.IsHumanInterfaceDevice)
            {
                // Get interface info for added devices.
                hid = intInfos.FirstOrDefault(x => x.DevicePath == ud.Device.Properties.InterfacePath);
                // Get device info for added devices.
                dev = devInfos.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId);
            }
            ud.LoadHidDeviceInfo(hid);
            ud.LoadDevDeviceInfo(dev);
        }

    }
}

