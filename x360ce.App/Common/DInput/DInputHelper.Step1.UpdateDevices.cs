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
		public int RefreshDevicesCount = 0;
		// Keeps track of previously detected device InstanceGuids.
		private HashSet<Guid> _previousDeviceGuids = new HashSet<Guid>();

		/// <summary>
		/// Asynchronously updates DirectInput devices.
		/// </summary>
		/// <param name="directInput">The DirectInput instance.</param>
		/// <returns>A completed task.</returns>
		Task UpdateDiDevices(DirectInput directInput)
		{
			try
			{
				// Get currently listed devices.
				var listedDevices = SettingsManager.UserDevices.ItemsToArraySynchronized();

				// Retrieve connected devices and check if the list has changed.
				(var connectedDevices, bool listChanged) = GetConnectedDiDevices(directInput);

				// Compare listedDevices with connectedDevices and put...
				// added and updated devices (from connectedDevices) into: addedDevices, updatedDevices
				// removed devices (from listedDevices) into: removedDevices.
				CategorizeDevices(connectedDevices.Select(x => (DeviceInstance)x.DeviceInstance).ToList(), listedDevices,
					out var addedDevices,
					out var updatedDevices,
					out var removedDevices);

				// Update device info caches for added or updated devices.
				var (devInfos, intInfos) = UpdateDeviceInfoCaches(addedDevices, updatedDevices);

				// Process added, updated and removed devices.
				InsertNewDevices(directInput, addedDevices, devInfos, intInfos);
				UpdateExistingDevices(directInput, listedDevices, updatedDevices, devInfos, intInfos);
				MarkDevicesOffline(removedDevices);

				// Enable test instances.
				TestDeviceHelper.EnableTestInstances();

				// Increment the refresh count and fire events.
				Interlocked.Increment(ref RefreshDevicesCount);
				DevicesUpdated?.Invoke(this, new DInputEventArgs());
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				LastException = ex;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Retrieves connected DirectInput devices and detects whether there was any change compared to the previous state.
		/// </summary>
		/// <param name="directInput">The DirectInput instance.</param>
		/// <returns>A tuple containing the list of connected devices and a flag indicating if the list has changed.</returns>
		private (List<(object DeviceInstance, object DeviceClass, int Usage, string DiDeviceID, string ProductName, Guid InstanceGuid)> Devices, bool IsChanged)
		GetConnectedDiDevices(DirectInput directInput)
		{
			var stopwatch = Stopwatch.StartNew();

			// Get and put connected devices (GameControl, Pointer, Keyboard) to list.
			var connectedDevices = new List<(object DeviceInstance, object DeviceClass, int Usage, string DiDeviceID, string ProductName, Guid InstanceGuid)>();
			foreach (var deviceClass in new DeviceClass[]{DeviceClass.GameControl, DeviceClass.Pointer, DeviceClass.Keyboard})
			{
				var devices = directInput.GetDevices(deviceClass, DeviceEnumerationFlags.AttachedOnly)
					//.Where(d => deviceClass != DeviceClass.Device || d.Usage == UsageId.GenericMouse || d.Usage == UsageId.GenericKeyboard)
					.Select(d => (
						DeviceInstance: (object)d,
						DeviceClass: (object)deviceClass,
						Usage: (int)d.Usage,
						DiDeviceID: ConvertProductGuidToDeviceID(d.ProductGuid, deviceClass),
						ProductName: d.InstanceName,
						InstanceGuid: d.InstanceGuid));
				connectedDevices.AddRange(devices);
			}
			connectedDevices = connectedDevices.OrderBy(x => x.DiDeviceID).ToList();

			// Check for changes in the set of device GUIDs.
			var newDeviceGuidHashSet = new HashSet<Guid>(connectedDevices.Select(item => item.InstanceGuid));
			bool listChanged = !newDeviceGuidHashSet.SetEquals(_previousDeviceGuids);
			if (listChanged)
			{
				DeviceDetector.DiDevices = connectedDevices;
				_previousDeviceGuids = newDeviceGuidHashSet;

				// Debug.
				Debug.WriteLine($"\n");
				foreach (var item in connectedDevices)
				{
					// Casting back to the original types.
					var device = (DeviceInstance)item.DeviceInstance;
					var deviceClass = (DeviceClass)item.DeviceClass;
					Debug.WriteLine($"SharpDX.DirectInput.DeviceInstance: " +
						$"InstanceGuid {device.InstanceGuid}, ProductGuid {device.ProductGuid} ({item.DiDeviceID}), " +
						$"InstanceName: {device.InstanceName}, UsagePage {(int)device.UsagePage}, Usage: {device.Usage}, " +
						$"DeviceClass {deviceClass}, Type-Subtype {device.Type}-{device.Subtype}");
				}

				stopwatch.Stop();
				Debug.WriteLine($"SharpDX.DirectInput.DeviceInstance: Stopwatch {stopwatch.Elapsed.TotalMilliseconds} ms");
			}

			return (connectedDevices, listChanged);
		}

		/// <summary>
		/// Converts a product GUID to a device ID string.
		/// </summary>
		private string ConvertProductGuidToDeviceID(Guid productGuid, DeviceClass deviceClass)
		{
			var bytes = productGuid.ToByteArray();
			int vid = bytes[1] << 8 | bytes[0];
			int pid = bytes[3] << 8 | bytes[2];
			return $"HID\\VID_{vid:X4}&PID_{pid:X4}";
		}

		/// <summary>
		/// Groups devices into added, updated, and removed categories.
		/// </summary>
		private void CategorizeDevices(List<DeviceInstance> connectedDevices, UserDevice[] listedDevices,
			out DeviceInstance[] addedDevices,
			out DeviceInstance[] updatedDevices,
			out UserDevice[] removedDevices)
		{
			var listedGuids = new HashSet<Guid>(listedDevices.Select(x => x.InstanceGuid));
			var connectedGuids = new HashSet<Guid>(connectedDevices.Select(x => x.InstanceGuid));

			addedDevices = connectedDevices.Where(x => !listedGuids.Contains(x.InstanceGuid)).ToArray();
			updatedDevices = connectedDevices.Where(x => listedGuids.Contains(x.InstanceGuid)).ToArray();
			removedDevices = listedDevices.Where(x => !connectedGuids.Contains(x.InstanceGuid)).ToArray();
		}

		/// <summary>
		/// Updates the device information caches if there are any changes.
		/// </summary>
		private (DeviceInfo[] devInfos, DeviceInfo[] intInfos) UpdateDeviceInfoCaches(DeviceInstance[] addedDevices, DeviceInstance[] updatedDevices)
		{
			if (addedDevices.Length > 0 || updatedDevices.Length > 0)
			{
				var devInfos = DeviceDetector.GetDevices(DiDevicesOnly: true);
				var intInfos = DeviceDetector.GetInterfaces(DiDevicesOnly: true);
				return (devInfos, intInfos);
			}
			return (null, null);
		}

		/// <summary>
		/// Inserts new devices into the user devices collection.
		/// </summary>
		private void InsertNewDevices(DirectInput manager, DeviceInstance[] addedDevices, DeviceInfo[] devInfos, DeviceInfo[] intInfos)
		{
			var newUserDevices = new List<UserDevice>();

			foreach (var device in addedDevices)
			{
				UserDevice userDevice = new UserDevice();
				DeviceInfo hid;
				RefreshDevice(manager, userDevice, device, devInfos, intInfos, out hid);

				// Only add if the device is not virtual.
				if (!IsDeviceVirtual(devInfos, hid))
					newUserDevices.Add(userDevice);
			}

			lock (SettingsManager.UserDevices.SyncRoot)
			{
				foreach (var device in newUserDevices)
				{
					SettingsManager.UserDevices.Items.Add(device);
				}
			}
		}

		/// <summary>
		/// Checks if the device is virtual.
		/// </summary>
		private bool IsDeviceVirtual(DeviceInfo[] devInfos, DeviceInfo hid)
		{
			if (hid == null)
				return false;

			DeviceInfo current = hid;
			do
			{
				current = devInfos.FirstOrDefault(x => x.DeviceId == current.ParentDeviceId);
				if (current != null && VirtualDriverInstaller.ViGEmBusHardwareIds.Any(
					id => string.Equals(current.HardwareIds, id, StringComparison.OrdinalIgnoreCase)))
				{
					return true;
				}
			} while (current != null);

			return false;
		}

		/// <summary>
		/// Marks removed devices as offline.
		/// </summary>
		private void MarkDevicesOffline(UserDevice[] removedDevices)
		{
			foreach (var device in removedDevices)
			{
				device.IsOnline = false;
			}
		}

		/// <summary>
		/// Refreshes updated devices in the current list.
		/// </summary>
		private void UpdateExistingDevices(DirectInput manager, UserDevice[] listedDevices, DeviceInstance[] updatedDevices, DeviceInfo[] devInfos, DeviceInfo[] intInfos)
		{
			foreach (var device in updatedDevices)
			{
				var userDevice = listedDevices.First(x => x.InstanceGuid.Equals(device.InstanceGuid));
				DeviceInfo hid;
				RefreshDevice(manager, userDevice, device, devInfos, intInfos, out hid);
			}
		}

		/// <summary>
		/// Refreshes device data by initializing, updating state, and loading HID info.
		/// </summary>
		private void RefreshDevice(DirectInput manager, UserDevice userDevice, DeviceInstance instance, DeviceInfo[] allDevices, DeviceInfo[] allInterfaces, out DeviceInfo hid)
		{
			hid = null;
			if (Program.IsClosing)
				return;

			// Lock to avoid modifications during enumeration.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				InitializeDevice(manager, userDevice, instance);
				UpdateDeviceState(userDevice, instance, allDevices);
				LoadHidDeviceData(userDevice, instance, allInterfaces, out hid);
			}
		}



		/// <summary>
		/// Initializes the device if it has not been initialized.
		/// </summary>
		private void InitializeDevice(DirectInput manager, UserDevice userDevice, DeviceInstance instance)
		{
			if (userDevice.Device == null)
			{
				try
				{
					userDevice.Device = new Joystick(manager, instance.InstanceGuid);
					userDevice.IsExclusiveMode = null;
					userDevice.LoadCapabilities(userDevice.Device.Capabilities);
				}
				catch (Exception ex)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					LastException = ex;
				}
			}
		}

		/// <summary>
		/// Updates the state of the user device.
		/// </summary>
		private void UpdateDeviceState(UserDevice userDevice, DeviceInstance instance, DeviceInfo[] allDevices)
		{
			userDevice.LoadInstance(instance);
			if (!userDevice.IsOnline)
			{
				userDevice.IsOnline = true;
			}

			var deviceInfo = allDevices.FirstOrDefault(x => x.DeviceId == userDevice.HidDeviceId);
			userDevice.LoadDevDeviceInfo(deviceInfo);
			userDevice.ConnectionClass = deviceInfo == null
				? Guid.Empty
				: DeviceDetector.GetConnectionDevice(deviceInfo, allDevices)?.ClassGuid ?? Guid.Empty;
		}

		/// <summary>
		/// Loads HID device information.
		/// </summary>
		private void LoadHidDeviceData(UserDevice userDevice, DeviceInstance instance, DeviceInfo[] allInterfaces, out DeviceInfo hid)
		{
			hid = null;
			if (instance.IsHumanInterfaceDevice && userDevice.Device != null)
			{
				string interfacePath = userDevice.Device.Properties.InterfacePath;
				hid = allInterfaces.FirstOrDefault(x => x.DevicePath == interfacePath);
				userDevice.LoadHidDeviceInfo(hid);
				userDevice.ConnectionClass = hid == null
					? Guid.Empty
					: DeviceDetector.GetConnectionDevice(hid, allInterfaces)?.ClassGuid ?? Guid.Empty;

				userDevice.DevManufacturer = userDevice.HidManufacturer;
				userDevice.DevDescription = userDevice.HidDescription;
				userDevice.DevVendorId = userDevice.HidVendorId;
				userDevice.DevProductId = userDevice.HidProductId;
				userDevice.DevRevision = userDevice.HidRevision;
			}
		}
	}
}
