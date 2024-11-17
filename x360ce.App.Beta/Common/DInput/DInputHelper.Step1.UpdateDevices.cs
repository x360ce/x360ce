//using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Text;
using System.Threading;
using System.Threading.Tasks;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public int RefreshDevicesCount = 0;


		Task UpdateDiDevices(DirectInput directInput)
		{
			try
			{
				// Make sure that interface handle is created, before starting device updates.
				// Get connected devices.
				var connectedDevices = GetConnectedDiDevices(directInput).Item1.Select(devices => devices.Device).ToList();
				var listedDevices = SettingsManager.UserDevices.ItemsToArraySynchronized();
				// Group devices into categories (added, updated, removed) using GUIDs of connected and listed devices.
				CategorizeDevices(connectedDevices, listedDevices, out var addedDevices, out var updatedDevices, out var removedDevices);
				// Process devices (must find better way to find Device than by Vendor ID and Product ID).
				var (devInfos, intInfos) = UpdateDeviceInfoCaches(addedDevices, updatedDevices);
				// Update device list.
				InsertNewDevices(directInput, addedDevices, devInfos, intInfos);
				UpdateExistingDevices(directInput, listedDevices, updatedDevices, devInfos, intInfos);
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
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastException = ex;
			}

			return Task.CompletedTask;
		}

		// For comparison of connected DiDevice.InstanceGuid new and old list. 
		private HashSet<Guid> DiDevicesGuidsOld = new HashSet<Guid>();
		private List<DeviceClass> DiDeviceClassList = new List<DeviceClass> { DeviceClass.GameControl, DeviceClass.Pointer, DeviceClass.Keyboard /*, DeviceClass.Device, DeviceClass.All*/ };

		private (IEnumerable<(DeviceInstance Device, DeviceClass Class, int Usage, string DiDeviceID)>, bool) GetConnectedDiDevices(DirectInput directInput)
		{
			var stopwatchDi = Stopwatch.StartNew();
			var DiDevicesNew = DiDeviceClassList.AsParallel().SelectMany(diDeviceClass =>
					directInput.GetDevices(diDeviceClass, DeviceEnumerationFlags.AttachedOnly)
					.Where(diDevice =>
						diDeviceClass != DeviceClass.Device
						|| (int)diDevice.Usage == 2
						|| (int)diDevice.Usage == 6
						)
					.Select(diDevice => (
						DeviceInstance: (object)diDevice,
						DeviceClass: (object)diDeviceClass,
						Usage: (int)diDevice.Usage,
						DiDeviceID: ConvertProductGuidToDeviceID(diDevice.ProductGuid, diDeviceClass),
						ProductName: diDevice.ProductName,
						InstanceGuid: diDevice.InstanceGuid
					)))
					.ToList().OrderBy(x => x.DiDeviceID);

			var DiDevicesGuidsNew = new HashSet<Guid>(DiDevicesNew.Select(item => item.InstanceGuid));
			var deviceListChanged = !DiDevicesGuidsNew.SetEquals(DiDevicesGuidsOld);
			if (deviceListChanged)
			{
				DeviceDetector.DiDevices = DiDevicesNew;
				DiDevicesGuidsOld = DiDevicesGuidsNew;
				Debug.WriteLine($"\n");
				foreach (var item in DiDevicesNew)
				{
					var device = (DeviceInstance)item.DeviceInstance;
					Debug.WriteLine($"DiDevice:" +
						$" InstanceGuid {device.InstanceGuid}." +
						$" ProductGuid {device.ProductGuid} ({item.DiDeviceID})." +
						$" InstanceName {device.InstanceName}." +
						$" UsagePage {(int)device.UsagePage}." +
						$" Usage {device.Usage}." +
						$" DeviceClass {item.DeviceClass}." +
						$" Type-Subtype {device.Type}-{device.Subtype}.");
				}
			}

			stopwatchDi.Stop();
			Debug.WriteLine($"StopwatchDi: {stopwatchDi.Elapsed.TotalMilliseconds} ms\n");

			var devices = DeviceDetector.DiDevices.Select(x =>
				((DeviceInstance)x.DeviceInstance,
				(DeviceClass)x.DeviceClass,
				(int)x.Usage,
				(string)x.DiDeviceID
			)).ToList();
			return (devices, deviceListChanged);
		}

		private string ConvertProductGuidToDeviceID(Guid DiDeviceProductGuid, DeviceClass DiDeviceClass)
		{
			// Create PnPDeviceID fragment from DiDevice.ProductGuid to find PnP device later.
			var bar = DiDeviceProductGuid.ToByteArray();
			int vid = bar[1] << 8 | bar[0];
			int pid = bar[3] << 8 | bar[2];
			var PnPDeviceID = $"HID\\VID_{vid:X4}&PID_{pid:X4}";
			return PnPDeviceID;
		}

		// Group devices into categories (added, updated, removed) using GUIDs of connected and listed devices.
		private void CategorizeDevices(List<DeviceInstance> connectedDevices, UserDevice[] listedDevices,
		out DeviceInstance[] addedDevices,
		out DeviceInstance[] updatedDevices,
		out UserDevice[] removedDevices)
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
				devInfos = DeviceDetector.GetDevices(DiDevicesOnly: true);
				intInfos = DeviceDetector.GetInterfaces(DiDevicesOnly: true);
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

