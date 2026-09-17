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

		// True, update device list as soon as possible.
		public bool UpdateDevicesEnabled = true;

		/// <summary>Whether a device change message means the device list has to be read again.</summary>
		/// <remarks>
		/// Windows broadcasts a device change for any device node change on the machine, repeatedly and
		/// for devices which have nothing to do with controllers. Reading every device again costs about
		/// a second on this thread, and controller processing stops for that long, so the rate falls from
		/// a thousand a second to one or two. Only a device arriving or leaving can change the list.
		///
		/// The rule is kept here, next to the work it decides to pay for, because a second copy of it
		/// elsewhere has already let the noisy case back in and taken the rate down with it.
		/// </remarks>
		/// <param name="change">What Windows says happened.</param>
		public static bool IsDeviceListChange(JocysCom.ClassLibrary.Win32.DBT change)
		{
			return change == JocysCom.ClassLibrary.Win32.DBT.DBT_DEVICEARRIVAL
				|| change == JocysCom.ClassLibrary.Win32.DBT.DBT_DEVICEREMOVECOMPLETE;
		}

		#endregion

		object UpdateDevicesLock = new object();
		public int RefreshDevicesCount;

		#region Device list read on a worker

		/// <summary>What one read of the machine found: the DirectInput instances and the device tree.</summary>
		class DeviceListRead
		{
			public List<DeviceInstance> Devices;
			public DeviceInfo[] DevInfos;
			public DeviceInfo[] IntInfos;
			/// <summary>The DirectInput device made for each instance not yet listed, for the device thread to keep.</summary>
			public Dictionary<Guid, Joystick> Made = new Dictionary<Guid, Joystick>();
			/// <summary>Why the read gave nothing, or null when it succeeded.</summary>
			public Exception Error;
			/// <summary>How long the read took, for the engine log.</summary>
			public long Milliseconds;
			/// <summary>The time split by phase: DirectInput enumeration, device creation, interfaces, devices.</summary>
			public string Phases = "";
		}

		/// <summary>How long the last worker read took, reported once in the engine log and then cleared.</summary>
		long _deviceReadMs;

		/// <summary>The phases of the last worker read, reported with <see cref="_deviceReadMs"/>.</summary>
		volatile string _deviceReadPhases = "";

		/// <summary>A finished read waiting for the device thread to take it in, or null.</summary>
		volatile DeviceListRead _deviceListRead;

		/// <summary>Whether a worker is reading the machine now. Touched by the device thread only.</summary>
		bool _deviceListReading;

		/// <summary>The DirectInput the worker enumerates with. Kept for the life of the process, like the device thread's own.</summary>
		static DirectInput _readManager;

		/// <summary>Reads the machine: three DirectInput enumerations, then only what those devices need.</summary>
		/// <remarks>
		/// This used to run on the device thread and read every device on the machine: seven hundred
		/// nodes at a millisecond each, and every HID interface opened for its strings at ten. Two to
		/// five seconds, during which every controller stopped being polled and the force feedback
		/// stayed at whatever it last was, so a wheel under a centering spring ran to its end stop.
		/// And the read is asked for on every arrival or removal of a HID interface, which includes
		/// each virtual controller this program plugs in, so a start or a hotkey toggle cost ten
		/// seconds or more of it. Now it runs on a worker, and asks only for the interfaces whose
		/// paths the DirectInput devices report and for those devices' own chains up to the root,
		/// which is all the device list ever looks up. The device thread takes the result in, which
		/// costs milliseconds.
		/// </remarks>
		/// <param name="knownPaths">Interface path of every listed device, by instance, where one is known.</param>
		static DeviceListRead ReadDeviceList(Dictionary<Guid, string> knownPaths)
		{
			var read = new DeviceListRead { Devices = new List<DeviceInstance>() };
			var started = System.Diagnostics.Stopwatch.StartNew();
			try
			{
				var manager = _readManager ?? (_readManager = new DirectInput());
				read.Devices.AddRange(manager.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly));
				read.Devices.AddRange(manager.GetDevices(DeviceClass.Pointer, DeviceEnumerationFlags.AttachedOnly));
				read.Devices.AddRange(manager.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly));
				if (Program.IsClosing)
					return read;
				var phase = started.ElapsedMilliseconds;
				read.Phases = "di:" + phase;
				var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var instance in read.Devices)
				{
					string path;
					if (knownPaths.TryGetValue(instance.InstanceGuid, out path))
					{
						if (!string.IsNullOrEmpty(path))
							paths.Add(path);
						continue;
					}
					// Not listed yet: the device is made here, and handed over to be kept.
					Joystick made;
					try { made = new Joystick(manager, instance.InstanceGuid); }
					catch (Exception) { continue; }
					read.Made[instance.InstanceGuid] = made;
					if (instance.IsHumanInterfaceDevice)
						paths.Add(made.Properties.InterfacePath ?? "");
				}
				read.Phases += ";made:" + (started.ElapsedMilliseconds - phase);
				phase = started.ElapsedMilliseconds;
				read.IntInfos = DeviceDetector.GetInterfaces((deviceId, devicePath) => paths.Contains(devicePath));
				read.Phases += ";int:" + (started.ElapsedMilliseconds - phase);
				phase = started.ElapsedMilliseconds;
				read.DevInfos = DeviceDetector.GetDevices(read.IntInfos.Select(x => x.DeviceId), true);
				read.Phases += ";dev:" + (started.ElapsedMilliseconds - phase);
			}
			catch (Exception ex)
			{
				read.Error = ex;
			}
			read.Milliseconds = started.ElapsedMilliseconds;
			return read;
		}

		#endregion

		void UpdateDiDevices(DirectInput manager)
		{
			if (!UpdateDevicesPending)
				return;
			var read = _deviceListRead;
			if (read == null)
			{
				// The request stays open until a read comes back; the list stays as it is meanwhile.
				if (!_deviceListReading)
				{
					_deviceListReading = true;
					var known = new Dictionary<Guid, string>();
					foreach (var ud in SettingsManager.UserDevices.ItemsToArraySyncronized())
						known[ud.InstanceGuid] = ud.HidDevicePath;
					System.Threading.Tasks.Task.Run(() => { _deviceListRead = ReadDeviceList(known); });
				}
				return;
			}
			_deviceListRead = null;
			_deviceListReading = false;
			_deviceReadMs = read.Milliseconds;
			_deviceReadPhases = read.Phases;
			UpdateDevicesPending = false;
			if (read.Error != null)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(read.Error);
				return;
			}
			// Make sure that interface handle is created, before starting device updates.
			UserDevice[] deleteDevices;
			// Add connected devices.
			var insertDevices = new List<UserDevice>();
			var devices = read.Devices;
			if (Program.IsClosing)
				return;
			// List of connected devices.
			var deviceInstanceGuid = devices.Select(x => x.InstanceGuid).ToList();
			// List of current devices.
			var uds = SettingsManager.UserDevices.ItemsToArraySyncronized();
			var currentInstanceGuids = uds.Select(x => x.InstanceGuid).ToArray();
			deleteDevices = uds.Where(x => !deviceInstanceGuid.Contains(x.InstanceGuid)).ToArray();
			var addedDevices = devices.Where(x => !currentInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			var updatedDevices = devices.Where(x => currentInstanceGuids.Contains(x.InstanceGuid)).ToArray();
			// Must find better way to find Device than by Vendor ID and Product ID.
			DeviceInfo[] devInfos = null;
			DeviceInfo[] intInfos = null;
			if (addedDevices.Length > 0 || updatedDevices.Length > 0)
			{
				devInfos = read.DevInfos;
				intInfos = read.IntInfos;
				// A device came or went, so the places may have moved; the next pass reads them again.
				XInputPlaces.Invalidate();
			}
			//Joystick    = new Guid("6f1d2b70-d5a0-11cf-bfc7-444553540000");
			//SysMouse    = new Guid("6f1d2b60-d5a0-11cf-bfc7-444553540000");
			//SysKeyboard = new Guid("6f1d2b61-d5a0-11cf-bfc7-444553540000");
			var devInfosById = VirtualDriverInstaller.IndexById(devInfos);
			for (int i = 0; i < addedDevices.Length; i++)
			{
				var device = addedDevices[i];
				var ud = new UserDevice();
				Joystick made;
				if (read.Made.TryGetValue(device.InstanceGuid, out made))
				{
					ud.Device = made;
					ud.IsExclusiveMode = null;
					ud.LoadCapabilities(made.Capabilities);
				}
				DeviceInfo hid;
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
				// Pads this program feeds are never taken back in as devices somebody could map, or it
				// would read its own output as an input. The rule lives in one place so that the device
				// list and the clean-up button can never disagree about what a leftover is.
				if (!VirtualDriverInstaller.IsVirtualPad(hid, devInfosById))
					insertDevices.Add(ud);
			}
			//if (insertDevices.Count > 0)
			//{
			//	CloudPanel.Add(CloudAction.Insert, insertDevices.ToArray(), true);
			//}
			for (int i = 0; i < updatedDevices.Length; i++)
			{
				var device = updatedDevices[i];
				var ud = uds.First(x => x.InstanceGuid.Equals(device.InstanceGuid));
				DeviceInfo hid;
				// Will refresh device and fill more values with new x360ce app if available.
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
			}
			// Pads of ours written down before this was recognised. Every stored device is judged, not
			// only those this pass enumerated, because the scan otherwise only ever marks a device
			// offline and nothing once written down is ever taken out again.
			var evictDevices = uds
				.Where(x => VirtualDriverInstaller.IsVirtualPad(x, devInfosById))
				.ToList();
			if (Program.IsClosing)
				return;
			// Remove disconnected devices.
			for (int i = 0; i < deleteDevices.Length; i++)
			{
				lock (SettingsManager.UserDevices.SyncRoot)
					deleteDevices[i].IsOnline = false;
			}
			if (evictDevices.Count > 0)
			{
				// All of them removed in one step, on the thread that owns the list.
				//
				// Removing them one at a time from here does not work: the list works out a row's
				// position now and hands that number to the window later, so the second removal names
				// a row that the first has already moved. The window is then given a position that is
				// no longer there and the removal fails, out of sight, on a thread nobody is watching.
				//
				// Sent rather than waited for, because waiting would put this loop behind whatever the
				// window happens to be drawing, which is the thing this list was written to avoid.
				var evicted = evictDevices.ToArray();
				JocysCom.ClassLibrary.Controls.ControlsHelper.BeginInvoke(() =>
				{
					lock (SettingsManager.UserDevices.SyncRoot)
						foreach (var ud in evicted)
							SettingsManager.UserDevices.Items.Remove(ud);
				});
			}
			for (int i = 0; i < insertDevices.Count; i++)
			{
				var ud = insertDevices[i];
				lock (SettingsManager.UserDevices.SyncRoot)
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
		void RefreshDevice(DirectInput manager, UserDevice ud, DeviceInstance device, DeviceInfo[] allDevices, DeviceInfo[] allInterfaces, out DeviceInfo hid)
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
						// Getting state can fail.
						var joystick = new Joystick(manager, device.InstanceGuid);
						ud.Device = joystick;
						ud.IsExclusiveMode = null;
						ud.LoadCapabilities(joystick.Capabilities);
					}
				}
				catch (Exception) { }
			}
			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				ud.LoadInstance(device);
			}
			// If device is set as offline then make it online.
			if (!ud.IsOnline)
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.IsOnline = true;
			// The interface is read first, because the device is then found by the identifier the
			// interface supplies. Read the other way round, a controller which had just been plugged in
			// was looked up by an identifier nothing had filled in yet: the lookup found nothing, the
			// device fields were cleared, and the row showed blanks or the plain DirectInput name until
			// some later pass happened to run. Which pass that was decided what the row said, so the
			// same controller could arrive named, unnamed, or named differently in each list.
			if (device.IsHumanInterfaceDevice && ud.Device != null)
			{
				var interfacePath = ud.Device.Properties.InterfacePath;
				hid = allInterfaces.FirstOrDefault(x => x.DevicePath == interfacePath);
				// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.LoadHidDeviceInfo(hid);
			}
			var dev = allDevices.FirstOrDefault(x => x.DeviceId == ud.HidDeviceId);
			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				ud.LoadDevDeviceInfo(dev);
				// The interface describes the device more accurately than the device node does, and is
				// present whenever it is connected, so it wins wherever both have something to say.
				if (hid != null)
				{
					ud.ConnectionClass = DeviceDetector.GetConnectionDevice(hid, allDevices)?.ClassGuid ?? Guid.Empty;
					ud.DevManufacturer = ud.HidManufacturer;
					ud.DevDescription = ud.HidDescription;
					ud.DevVendorId = ud.HidVendorId;
					ud.DevProductId = ud.HidProductId;
					ud.DevRevision = ud.HidRevision;
				}
				else if (dev != null)
				{
					ud.ConnectionClass = DeviceDetector.GetConnectionDevice(dev, allDevices)?.ClassGuid ?? Guid.Empty;
				}
			}
		}

	}
}

