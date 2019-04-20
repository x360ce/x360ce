using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
    {

        #region Device Detector

		// True, update device list as soon as possible.
		public bool UpdateDevicesEnabled = true;

        #endregion

        object UpdateDevicesLock = new object();
        public int RefreshDevicesCount;

        void UpdateDiDevices(DirectInput manager)
        {
            if (!UpdateDevicesPending)
                return;
            UpdateDevicesPending = false;
            // Make sure that interface handle is created, before starting device updates.
            UserDevice[] deleteDevices;
            // Add connected devices.
            var insertDevices = new List<UserDevice>();
            // List of connected devices (can be a very long operation).
            var devices = new List<DeviceInstance>();
            // Controllers.
            var controllerInstances = manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices).ToList();
            foreach (var item in controllerInstances)
                devices.Add(item);
            // Pointers.
            var pointerInstances = manager.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AllDevices).ToList();
            foreach (var item in pointerInstances)
                devices.Add(item);
            // Keyboards.
            var keyboardInstances = manager.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AllDevices).ToList();
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
                intInfos = DeviceDetector.GetInterfaces();
            }
            //Joystick    = new Guid("6f1d2b70-d5a0-11cf-bfc7-444553540000");
            //SysMouse    = new Guid("6f1d2b60-d5a0-11cf-bfc7-444553540000");
            //SysKeyboard = new Guid("6f1d2b61-d5a0-11cf-bfc7-444553540000");
            for (int i = 0; i < addedDevices.Length; i++)
            {
                var device = addedDevices[i];
                var ud = new UserDevice();
                DeviceInfo hid;
                RefreshDevice(manager, ud, device, intInfos, devInfos, out hid);
                var isVirtual = false;
                if (hid != null)
                {
                    DeviceInfo p = hid;
                    do
                    {
                        p = devInfos.FirstOrDefault(x=>x.DeviceId == p.ParentDeviceId);
						// If ViGEm hardware found then...
						if (p != null && VirtualDriverInstaller.ViGEmBusHardwareIds.Any(x=> string.Compare(p.HardwareIds, x, true) == 0))
                        {
                            isVirtual = true;
                            break;
                        }

                    } while (p != null);
                }
                if (!isVirtual)
                    insertDevices.Add(ud);
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
                RefreshDevice(manager, ud, device, intInfos, devInfos, out hid);
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
		void RefreshDevice(DirectInput manager, UserDevice ud, DeviceInstance device, DeviceInfo[] intInfos, DeviceInfo[] devInfos, out DeviceInfo hid)
        {
            hid = null;
            if (Program.IsClosing)
                return;
            // If device added then...
            if (ud.Device == null)
            {
                var joystick = new Joystick(manager, device.InstanceGuid);
                ud.Device = joystick;
                ud.IsExclusiveMode = null;
                ud.LoadCapabilities(joystick.Capabilities);
            }
            ud.LoadInstance(device);
            // If device is set as offline then make it online.
            if (!ud.IsOnline)
                ud.IsOnline = true;
            // Get device info for added devices.
            var dev = devInfos.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId);
            ud.LoadDevDeviceInfo(dev);
            // InterfacePath is available for HID devices.
            if (device.IsHumanInterfaceDevice)
            {
                var interfacePath = ud.Device.Properties.InterfacePath;
                hid = intInfos.FirstOrDefault(x => x.DevicePath == interfacePath);
                // Get interface info for added devices.
                ud.LoadHidDeviceInfo(hid);
            }
        }

    }
}

