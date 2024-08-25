using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		#region ■ Device Detector

		// True, update device list as soon as possible.
		public bool UpdateDevicesEnabled = true;

		#endregion

		public int RefreshDevicesCount = 0;

		void UpdateDiDevices(DirectInput manager)
		{
			if (!UpdateDevicesPending)
				return;
			UpdateDevicesPending = false;
			// Make sure that interface handle is created, before starting device updates.
			// Add connected devices.
			var insertDevices = new List<UserDevice>();

			// List of connected devices (can be a very long operation).
			var connectedDevices = new List<DeviceInstance>();
			foreach (var deviceClass in new[] { DeviceClass.GameControl, DeviceClass.Pointer, DeviceClass.Keyboard })
			{
				var instances = manager.GetDevices(deviceClass, DeviceEnumerationFlags.AllDevices).ToList();
				connectedDevices.AddRange(instances);
			}

			if (Program.IsClosing)
				return;
			// Update listed devices.
			var listedDevices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var listedInstanceGuids = listedDevices.Select(x => x.InstanceGuid).ToArray();
			var connectedInstanceGuids = connectedDevices.Select(x => x.InstanceGuid).ToArray();
			// Group devices into (removed, added, updated) categories.
			var removedDevices = listedDevices.Where(x => !connectedInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			var addedDevices = connectedDevices.Where(x => !listedInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			var updatedDevices = connectedDevices.Where(x => listedInstanceGuids.Contains(x.InstanceGuid)).ToArray();

			// Must find better way to find Device than by Vendor ID and Product ID.
			DeviceInfo[] devInfos = null;
			DeviceInfo[] intInfos = null;
			if (addedDevices.Length > 0 || updatedDevices.Length > 0)
			{
				devInfos = DeviceDetector.GetDevices();
				//var classes = devInfos.Select(x=>x.ClassDescription).Distinct().ToArray();
				intInfos = DeviceDetector.GetInterfaces();
				//var intclasses = intInfos.Select(x => x.ClassDescription).Distinct().ToArray();
			}
			//Joystick    = new Guid("6f1d2b70-d5a0-11cf-bfc7-444553540000");
			//SysMouse    = new Guid("6f1d2b60-d5a0-11cf-bfc7-444553540000");
			//SysKeyboard = new Guid("6f1d2b61-d5a0-11cf-bfc7-444553540000");
			for (int i = 0; i < addedDevices.Length; i++)
			{
				var device = addedDevices[i];
				var ud = new UserDevice();
				DeviceInfo hid;
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
				var isVirtual = false;
				if (hid != null)
				{
					DeviceInfo p = hid;
					do
					{
						p = devInfos.FirstOrDefault(x => x.DeviceId == p.ParentDeviceId);
						// If ViGEm hardware found then...
						if (p != null && VirtualDriverInstaller.ViGEmBusHardwareIds.Any(x => string.Compare(p.HardwareIds, x, true) == 0))
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
				var ud = listedDevices.First(x => x.InstanceGuid.Equals(device.InstanceGuid));
				DeviceInfo hid;
				// Will refresh device and Fill more values with new x360ce app if available.
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
			}
			if (Program.IsClosing)
				return;
			// Remove disconnected devices.
			for (int i = 0; i < removedDevices.Length; i++)
			{
				lock (SettingsManager.UserDevices.SyncRoot)
					removedDevices[i].IsOnline = false;
			}
			for (int i = 0; i < insertDevices.Count; i++)
			{
				var ud = insertDevices[i];
				lock (SettingsManager.UserDevices.SyncRoot)
					SettingsManager.UserDevices.Items.Add(ud);
			}
			// Enable Test instances.
			TestDeviceHelper.EnableTestInstances();
			Interlocked.Increment(ref RefreshDevicesCount);
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
		void RefreshDevice(DirectInput manager, UserDevice ud, DeviceInstance instance, DeviceInfo[] allDevices, DeviceInfo[] allInterfaces, out DeviceInfo hid)
		{
			hid = null;
			if (Program.IsClosing)
				return;
			// If device added then...
			if (ud.Device == null)
			{
				try
				{
					// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
					lock (SettingsManager.UserDevices.SyncRoot)
					{
						Device device;
						//if (instance.Type == DeviceType.Mouse)
						//	device = new Mouse(manager);
						//else if (instance.Type == DeviceType.Keyboard)
						//	device = new Keyboard(manager);
						//else
							device = new Joystick(manager, instance.InstanceGuid);
						ud.Device = device;
						ud.IsExclusiveMode = null;
						ud.LoadCapabilities(device.Capabilities);
					}
				}
				catch (Exception) { }
			}
			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				ud.LoadInstance(instance);
			}
			// If device is set as offline then make it online.
			if (!ud.IsOnline)
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.IsOnline = true;
			// Get device info for added devices.
			var dev = allDevices.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId);
			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				ud.LoadDevDeviceInfo(dev);
				if (dev != null)
					ud.ConnectionClass = DeviceDetector.GetConnectionDevice(dev, allDevices)?.ClassGuid ?? Guid.Empty;
			}
			// InterfacePath is available for HID devices.
			if (instance.IsHumanInterfaceDevice && ud.Device != null)
			{
				var interfacePath = ud.Device.Properties.InterfacePath;
				// Get interface info for added devices.
				hid = allInterfaces.FirstOrDefault(x => x.DevicePath == interfacePath);
				// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
				lock (SettingsManager.UserDevices.SyncRoot)
				{
					ud.LoadHidDeviceInfo(hid);
					if (hid != null)
						ud.ConnectionClass = DeviceDetector.GetConnectionDevice(hid, allDevices)?.ClassGuid ?? Guid.Empty;
					// Workaround: 
					// Override Device values and description from the Interface, 
					// because it is more accurate and present.
					// Note 1: Device fields below, probably, should not be used.
					// Note 2: Available when device is online.
					ud.DevManufacturer = ud.HidManufacturer;
					ud.DevDescription = ud.HidDescription;
					ud.DevVendorId = ud.HidVendorId;
					ud.DevProductId = ud.HidProductId;
					ud.DevRevision = ud.HidRevision;
				}
			}
		}

	}
}

