using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

		public string message = "";
		Stopwatch stopwatch = new Stopwatch();
		void FormMessage(string m)
		{	
			message = message + stopwatch.ElapsedMilliseconds + " " + m + "\n";
		}

		//public bool devicesUpdating = false;

		void UpdateDiDevices(DirectInput manager)
		{
			// if (devicesUpdating) return;
			// devicesUpdating = true;

			if (!UpdateDevicesPending || Program.IsClosing)
				return;
			try
			{
				// Make sure that interface handle is created, before starting device updates.
				UpdateDevicesPending = false;
				// Get connected devices (can be a very long operation).
				stopwatch.Restart();
				var connectedDevices = GetConnectedDevices(manager); // 1 second.
				FormMessage("connectedDevices");
				var listedDevices = SettingsManager.UserDevices.ItemsToArraySynchronized();
				// Group devices into categories (added, updated, removed) using GUIDs of connected and listed devices.
				CategorizeDevices(connectedDevices, listedDevices, out var addedDevices, out var updatedDevices, out var removedDevices);
				// Process devices (must find better way to find Device than by Vendor ID and Product ID).
				var (devInfos, intInfos) = UpdateDeviceInfoCaches(addedDevices, updatedDevices); // 13 seconds.
				// Update device list.
				InsertNewDevices(manager, addedDevices, devInfos, intInfos);
				UpdateExistingDevices(manager, listedDevices, updatedDevices, devInfos, intInfos);
				HandleRemovedDevices(removedDevices);

				// Enable Test instances.
				TestDeviceHelper.EnableTestInstances();
				Interlocked.Increment(ref RefreshDevicesCount);
				DevicesUpdated?.Invoke(this, new DInputEventArgs());

				//	var game = CurrentGame;
				//	if (game != null)
				//	{
				//		// Auto-configure new devices.
				//		AutoConfigure(game);
				//	}

				System.Diagnostics.Debug.WriteLine("DInputHelper.Step1.UpdateDevices.cs " + message);
				message = message + "\n";
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastException = ex;
			}

			// devicesUpdating = false;
		}

		// Get connected devices (can be a very long operation).
		private List<DeviceInstance> GetConnectedDevices(DirectInput manager)
		{
			var deviceClasses = new[] { DeviceClass.GameControl, DeviceClass.Pointer, DeviceClass.Keyboard };
			var tasks = deviceClasses.Select(deviceClass => Task.Run(() =>
			manager.GetDevices(deviceClass, DeviceEnumerationFlags.AllDevices).ToList())).ToArray();
			Task.WaitAll(tasks);
			var connectedDevices = tasks.SelectMany(t => t.Result).ToList();
			return connectedDevices;
		}

		// Group devices into categories (added, updated, removed) using GUIDs of connected and listed devices.
		private void CategorizeDevices(List<DeviceInstance> connectedDevices, UserDevice[] listedDevices,
		out DeviceInstance[] addedDevices, out DeviceInstance[] updatedDevices, out UserDevice[] removedDevices)
		{
			var listedInstanceGuids = listedDevices.Select(x => x.InstanceGuid).ToArray();
			var connectedInstanceGuids = connectedDevices.Select(x => x.InstanceGuid).ToArray();
			addedDevices = connectedDevices.Where(x => !listedInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			updatedDevices = connectedDevices.Where(x => listedInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			removedDevices = listedDevices.Where(x => !connectedInstanceGuids.Contains(x.InstanceGuid)).ToArray();
		}

		private (DeviceInfo[], DeviceInfo[]) UpdateDeviceInfoCaches(DeviceInstance[] addedDevices, DeviceInstance[] updatedDevices)
		{
			DeviceInfo[] devInfos = null;
			DeviceInfo[] intInfos = null;
			if (addedDevices.Length > 0 || updatedDevices.Length > 0)
			{
				stopwatch.Restart();
				devInfos = DeviceDetector.GetDevices(); // 10 seconds.
				FormMessage("DeviceDetector.GetDevices()");
				stopwatch.Restart();
				intInfos = DeviceDetector.GetInterfaces(); // 3 seconds.
				FormMessage("DeviceDetector.GetInterfaces()");
				//var classes = devInfos.Select(x=>x.ClassDescription).Distinct().ToArray();
				//var intclasses = intInfos.Select(x => x.ClassDescription).Distinct().ToArray();
			}
			return (devInfos, intInfos);
		}

		private void InsertNewDevices(DirectInput manager, DeviceInstance[] addedDevices, DeviceInfo[] devInfos, DeviceInfo[] intInfos)
		{
			var insertDevices = new List<UserDevice>();
			foreach (var device in addedDevices)
			{
				var ud = new UserDevice();
				DeviceInfo hid;
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);

				var isVirtual = CheckIfDeviceIsVirtual(devInfos, hid);
				if (!isVirtual)
					insertDevices.Add(ud);
			}

			lock (SettingsManager.UserDevices.SyncRoot)
			{
				foreach (var device in insertDevices)
				{
					SettingsManager.UserDevices.Items.Add(device);
				}
			}
		}

		private bool CheckIfDeviceIsVirtual(DeviceInfo[] devInfos, DeviceInfo hid)
		{
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
			return isVirtual;
		}

		private void HandleRemovedDevices(UserDevice[] removedDevices)
		{
			foreach (var device in removedDevices)
			{
				device.IsOnline = false;
			}
		}

		private void UpdateExistingDevices(DirectInput manager, UserDevice[] listedDevices, DeviceInstance[] updatedDevices, DeviceInfo[] devInfos, DeviceInfo[] intInfos)
		{
			foreach (var device in updatedDevices)
			{
				var ud = listedDevices.First(x => x.InstanceGuid.Equals(device.InstanceGuid));
				DeviceInfo hid;
				// Will refresh device and Fill more values with new x360ce app if available.
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
			}
		}

		/// <summary>
		/// Refresh device.
		/// </summary>
		void RefreshDevice(DirectInput manager, UserDevice ud, DeviceInstance instance, DeviceInfo[] allDevices, DeviceInfo[] allInterfaces, out DeviceInfo hid)
		{
			hid = null;
			if (Program.IsClosing) return;

			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				InitializeDevice(manager, ud, instance);
				UpdateDeviceState(ud, instance, allDevices);
				LoadHidDeviceInfo(ud, instance, allInterfaces, out hid);
			}
		}

		private void InitializeDevice(DirectInput manager, UserDevice ud, DeviceInstance instance)
		{
			// Joystick    = new Guid("6f1d2b70-d5a0-11cf-bfc7-444553540000");
			// SysMouse    = new Guid("6f1d2b60-d5a0-11cf-bfc7-444553540000");
			// SysKeyboard = new Guid("6f1d2b61-d5a0-11cf-bfc7-444553540000");
			if (ud.Device == null)
			{
				try
				{
					//if (instance.Type == DeviceType.Mouse)
					//	ud.Device = new Mouse(manager);
					//else if (instance.Type == DeviceType.Keyboard)
					//	ud.Device = new Keyboard(manager);
					//else
					ud.Device = new Joystick(manager, instance.InstanceGuid);
					ud.IsExclusiveMode = null;
					ud.LoadCapabilities(ud.Device.Capabilities);
				}
				catch (Exception ex)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					LastException = ex;
				}
			}
		}

		private void UpdateDeviceState(UserDevice ud, DeviceInstance instance, DeviceInfo[] allDevices)
		{
			ud.LoadInstance(instance);
			// If device is set as offline then set it online.
			if (!ud.IsOnline)
				ud.IsOnline = true;
			// Get device info for added devices.
			var dev = allDevices.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId);
			ud.LoadDevDeviceInfo(dev);
			ud.ConnectionClass = dev is null
				? Guid.Empty
				: DeviceDetector.GetConnectionDevice(dev, allDevices)?.ClassGuid ?? Guid.Empty;
		}

		private void LoadHidDeviceInfo(UserDevice ud, DeviceInstance instance, DeviceInfo[] allInterfaces, out DeviceInfo hid)
		{
			hid = null;

			// InterfacePath is available for HID devices.
			if (instance.IsHumanInterfaceDevice && ud.Device != null)
			{
				var interfacePath = ud.Device.Properties.InterfacePath;
				// Get interface info for added devices.
				hid = allInterfaces.FirstOrDefault(x => x.DevicePath == interfacePath);
				ud.LoadHidDeviceInfo(hid);
				ud.ConnectionClass = hid is null
					? Guid.Empty
					: DeviceDetector.GetConnectionDevice(hid, allInterfaces)?.ClassGuid ?? Guid.Empty;
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

