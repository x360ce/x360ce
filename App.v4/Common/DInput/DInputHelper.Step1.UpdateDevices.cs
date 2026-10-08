using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
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

		#region Device list read on a worker

		/// <summary>What one read of the machine found: the DirectInput instances and the device tree.</summary>
		class DeviceListRead
		{
			public List<DeviceInstance> Devices;
			public DeviceInfo[] DevInfos;
			public DeviceInfo[] IntInfos;
			/// <summary>The DirectInput device made for each instance not yet listed, for the device thread to keep.</summary>
			public Dictionary<Guid, DirectInputDevice> Made = new Dictionary<Guid, DirectInputDevice>();
			/// <summary>The Raw Input game controllers the hub read that are not pads of ours, for the device thread to list.</summary>
			public List<RawInputListing> RawInputDevices = new List<RawInputListing>();
			/// <summary>Why the read gave nothing, or null when it succeeded.</summary>
			public Exception Error;
			/// <summary>How long the read took, for the engine log.</summary>
			public long Milliseconds;
			/// <summary>The time split by phase: DirectInput enumeration, device creation, interfaces, devices, Raw Input.</summary>
			public string Phases = "";
		}

		/// <summary>A Raw Input game controller as one read of the machine found it: what the device list shows of it.</summary>
		class RawInputListing
		{
			/// <summary>The device the hub reads: who it is and its controls.</summary>
			public RawInputDevice Device;
			/// <summary>Where its controls go in its state: the layout the hub reads it by, its twin's slots once they are taken.</summary>
			public RawInputLayout Layout;
			/// <summary>The objects that describe the slots <see cref="Layout"/> fills: its twin's, or made from its controls (<see cref="RawInputLayout.GetDeviceObjects"/>).</summary>
			public DeviceObjectItem[] Objects;
			/// <summary>Its HID interface, or null when the read did not find it.</summary>
			public DeviceInfo Interface;
			/// <summary>The same device as DirectInput lists it, or null when DirectInput does not.</summary>
			public DeviceInstance Twin;
		}

		/// <summary>The layout each Raw Input device took from its DirectInput twin since it arrived, so later reads do not take it again.</summary>
		/// <remarks>
		/// Used by the worker that reads the device list, one read at a time. Keyed by the hub's own entry, which a device
		/// that leaves and comes back has anew, so its twin is read for it again. The hub reads it by its last layout
		/// meanwhile (<see cref="RawInputHub.Add"/>), and the same layout taken again changes nothing.
		/// </remarks>
		readonly Dictionary<RawInputHubDevice, RawInputLayout> _rawInputTwinLayouts = new Dictionary<RawInputHubDevice, RawInputLayout>();

		/// <summary>How long the last worker read took, reported once in the engine log and then cleared.</summary>
		long _deviceReadMs;

		/// <summary>The phases of the last worker read, reported with <see cref="_deviceReadMs"/>.</summary>
		volatile string _deviceReadPhases = "";

		/// <summary>A finished read waiting for the device thread to take it in, or null.</summary>
		volatile DeviceListRead _deviceListRead;

		/// <summary>Whether a worker is reading the machine now.</summary>
		volatile bool _deviceListReading;

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
		/// <param name="hub">The Raw Input hub, whose devices the read lists beside DirectInput's.</param>
		/// <param name="twinLayouts">The layout each hub device took from its twin since it arrived; see <see cref="_rawInputTwinLayouts"/>.</param>
		static DeviceListRead ReadDeviceList(Dictionary<Guid, string> knownPaths, RawInputHub hub, Dictionary<RawInputHubDevice, RawInputLayout> twinLayouts)
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
				// Each HID device DirectInput lists, by interface path: a Raw Input device's twin has the same path.
				var twins = new Dictionary<string, DeviceInstance>(StringComparer.OrdinalIgnoreCase);
				foreach (var instance in read.Devices)
				{
					string path;
					if (knownPaths.TryGetValue(instance.InstanceGuid, out path))
					{
						// A device written down from an earlier run has no interface path yet: the path is
						// not saved with it. Asked of DirectInput here, or the device is never matched to
						// its interface and comes up with no vendor, no id, and a wheel that takes no range.
						if (string.IsNullOrEmpty(path) && instance.IsHumanInterfaceDevice)
							path = InterfacePathOf(manager, instance.InstanceGuid);
						if (!string.IsNullOrEmpty(path))
						{
							paths.Add(path);
							twins[path] = instance;
						}
						continue;
					}
					// Not listed yet: the device is made here, and handed over to be kept.
					DirectInputDevice made;
					try { made = new DirectInputDevice(manager, instance.InstanceGuid); }
					catch (Exception) { continue; }
					read.Made[instance.InstanceGuid] = made;
					if (instance.IsHumanInterfaceDevice)
					{
						path = made.InterfacePath;
						paths.Add(path);
						if (path.Length > 0)
							twins[path] = instance;
					}
				}
				// The hub's devices as they are now. Their interfaces and device chains are read with DirectInput's,
				// so each can be judged as DirectInput's are.
				var rawDevices = hub.Devices.Values.ToArray();
				foreach (var entry in rawDevices)
					paths.Add(entry.Device.InterfacePath ?? "");
				read.Phases += ";made:" + (started.ElapsedMilliseconds - phase);
				phase = started.ElapsedMilliseconds;
				read.IntInfos = DeviceDetector.GetInterfaces((deviceId, devicePath) => paths.Contains(devicePath));
				read.Phases += ";int:" + (started.ElapsedMilliseconds - phase);
				phase = started.ElapsedMilliseconds;
				read.DevInfos = DeviceDetector.GetDevices(read.IntInfos.Select(x => x.DeviceId), true);
				read.Phases += ";dev:" + (started.ElapsedMilliseconds - phase);
				phase = started.ElapsedMilliseconds;
				FindRawInputDevices(read, hub, rawDevices, manager, twins, twinLayouts);
				read.Phases += ";raw:" + (started.ElapsedMilliseconds - phase);
			}
			catch (Exception ex)
			{
				read.Error = ex;
				// A failed read hands nothing over, so what it opened is closed here.
				foreach (var made in read.Made.Values)
					made.Dispose();
				read.Made.Clear();
			}
			read.Milliseconds = started.ElapsedMilliseconds;
			return read;
		}

		/// <summary>The HID interface path of a device, read through a joystick made and let go of for the purpose, or empty.</summary>
		static string InterfacePathOf(DirectInput manager, Guid instanceGuid)
		{
			try
			{
				using (var joystick = new Joystick(manager, instanceGuid))
					return joystick.Properties.InterfacePath ?? "";
			}
			catch (Exception)
			{
				return "";
			}
		}

		/// <summary>The interface with <paramref name="path"/>, which Windows reports in either case, or null.</summary>
		static DeviceInfo FindInterface(DeviceInfo[] interfaces, string path)
		{
			return interfaces?.FirstOrDefault(x => string.Equals(x.DevicePath, path, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Lists the hub's devices that are not pads of ours in <see cref="DeviceListRead.RawInputDevices"/>, and gives each the layout of its DirectInput twin once.</summary>
		/// <remarks>
		/// A pad this program feeds is never listed, or the program would read its own output, and its layout is not built.
		/// It is judged as DirectInput's devices are, by its interface's device chain. A device whose layout has not come
		/// from its twin since it arrived takes it now, when DirectInput lists a device with its path; one without a twin
		/// keeps the layout the hub reads it by, its usages' or the one it had before it last left, and is asked again on
		/// the next read. Each is listed with the objects that describe its layout's slots, made here rather than on the
		/// device thread.
		/// </remarks>
		/// <param name="rawDevices">The hub's devices when the read began.</param>
		/// <param name="manager">Opens a twin that the read has not opened already.</param>
		/// <param name="twins">Each DirectInput HID device the read found, by interface path, ignoring case.</param>
		/// <param name="twinLayouts">The layout each hub device took from its twin since it arrived. Devices no longer in <paramref name="rawDevices"/> are forgotten.</param>
		static void FindRawInputDevices(DeviceListRead read, RawInputHub hub, RawInputHubDevice[] rawDevices, DirectInput manager,
			Dictionary<string, DeviceInstance> twins, Dictionary<RawInputHubDevice, RawInputLayout> twinLayouts)
		{
			var byId = VirtualDriverInstaller.IndexById(read.DevInfos);
			foreach (var entry in rawDevices)
			{
				var device = entry.Device;
				var path = device.InterfacePath ?? "";
				var hid = FindInterface(read.IntInfos, path);
				if (VirtualDriverInstaller.IsVirtualPad(hid, byId))
					continue;
				DeviceInstance twin;
				twins.TryGetValue(path, out twin);
				RawInputLayout layout;
				if (!twinLayouts.TryGetValue(entry, out layout))
				{
					var objects = twin == null ? null : ReadTwinObjects(manager, twin.InstanceGuid, read.Made);
					layout = TakeTwinLayout(hub, entry, objects, twinLayouts);
				}
				read.RawInputDevices.Add(new RawInputListing { Device = device, Layout = layout, Objects = layout.GetDeviceObjects(), Interface = hid, Twin = twin });
			}
			foreach (var gone in twinLayouts.Keys.Where(x => Array.IndexOf(rawDevices, x) < 0).ToArray())
				twinLayouts.Remove(gone);
		}

		/// <summary>A DirectInput device's controls, each with the slot DirectInput fills for it, read without acquiring the device; null when DirectInput does not answer.</summary>
		/// <remarks>
		/// The slots are those the engine learns when it first reads a device: <see cref="SourceState.GetJoystickAxisMask"/>
		/// and <see cref="SourceState.GetJoystickSlidersMask"/> set each axis's and slider's <see cref="DeviceObjectItem.DiIndex"/>.
		/// A device this read made for the list is used as it is; any other is opened for the purpose and let go of.
		/// </remarks>
		static DeviceObjectItem[] ReadTwinObjects(DirectInput manager, Guid instanceGuid, Dictionary<Guid, DirectInputDevice> made)
		{
			DirectInputDevice kept;
			var opened = !made.TryGetValue(instanceGuid, out kept);
			var joystick = opened ? null : kept.Joystick;
			try
			{
				if (opened)
					joystick = new Joystick(manager, instanceGuid);
				var objects = AppHelper.GetDeviceObjects(joystick);
				int axisMask, actuatorMask, actuatorCount, relativeMask, relativeSliderMask;
				SourceState.GetJoystickAxisMask(objects, joystick, out axisMask, out actuatorMask, out actuatorCount, out relativeMask);
				SourceState.GetJoystickSlidersMask(objects, joystick, out relativeSliderMask);
				return objects;
			}
			catch (SharpDX.SharpDXException)
			{
				// Gone since it was listed: the device keeps its layout and is asked again on the next read.
				return null;
			}
			finally
			{
				if (opened && joystick != null)
					joystick.Dispose();
			}
		}

		/// <summary>The layout a hub device is read by: its twin's slots when <paramref name="twinObjects"/> is given, which the hub is told and <paramref name="twinLayouts"/> keeps; otherwise the one the hub reads it by already.</summary>
		/// <param name="twinObjects">The twin's controls as <see cref="ReadTwinObjects"/> reads them, or null when there is no twin.</param>
		static RawInputLayout TakeTwinLayout(RawInputHub hub, RawInputHubDevice entry, DeviceObjectItem[] twinObjects,
			Dictionary<RawInputHubDevice, RawInputLayout> twinLayouts)
		{
			if (twinObjects == null)
				return entry.Layout;
			var layout = RawInputLayout.FromTwin(entry.Device.Controls, twinObjects);
			hub.SetLayout(entry.Device.InstanceGuid, layout);
			twinLayouts[entry] = layout;
			return layout;
		}

		#endregion

		/// <summary>Starts a read of the device list on a worker, unless one is under way or waiting.</summary>
		/// <remarks>
		/// The device thread calls this the moment it wants a list. The window calls it too, as soon
		/// as the settings are read and before the controller panels are built: the read takes about
		/// half a second, nearly all of it DirectInput's own enumeration, and the panels take longer,
		/// so the list is waiting when the device thread starts instead of the other way round.
		/// </remarks>
		public void BeginDeviceListRead()
		{
			lock (_deviceListStartLock)
			{
				if (_deviceListReading || _deviceListRead != null)
					return;
				_deviceListReading = true;
			}
			var known = new Dictionary<Guid, string>();
			foreach (var ud in SettingsManager.UserDevices.ItemsToArraySynchronized())
				known[ud.InstanceGuid] = ud.HidDevicePath;
			System.Threading.Tasks.Task.Run(() => { _deviceListRead = ReadDeviceList(known, RawInput, _rawInputTwinLayouts); });
		}

		/// <summary>Guards the start of a read, which the window and the device thread can both ask for.</summary>
		readonly object _deviceListStartLock = new object();

		/// <summary>Takes a finished device list read in when a list is wanted, and asks for one when none is waiting.</summary>
		/// <remarks>
		/// Runs on every pass, so it only decides. The take-in is a method of its own: its lambdas share locals, and the
		/// object that holds them is made on entry to the method that declares them, whether or not a read is waiting.
		/// </remarks>
		void UpdateDiDevices(DirectInput manager)
		{
			if (!UpdateDevicesPending)
				return;
			var read = _deviceListRead;
			if (read == null)
			{
				// The request stays open until a read comes back; the list stays as it is meanwhile. A read under way is
				// not asked for again, so a pass during it takes no lock and makes nothing: asking makes an object on entry.
				if (!_deviceListReading)
					BeginDeviceListRead();
				return;
			}
			TakeInDeviceList(manager, read);
		}

		/// <summary>Takes a finished read of the device list in: lists the devices that came, refreshes those still here, and marks those gone as offline.</summary>
		void TakeInDeviceList(DirectInput manager, DeviceListRead read)
		{
			_deviceListRead = null;
			_deviceListReading = false;
			_deviceReadMs = read.Milliseconds;
			_deviceReadPhases = read.Phases;
			Program.StartupTrace.Mark("device list taken in");
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
			var uds = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var currentInstanceGuids = uds.Select(x => x.InstanceGuid).ToArray();
			// Only DirectInput's own devices are gone when DirectInput does not list them. A Raw Input device is listed
			// from the hub below, and goes offline when the hub no longer has it.
			deleteDevices = uds.Where(x => x.IsDirectInput && !deviceInstanceGuid.Contains(x.InstanceGuid)).ToArray();
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
				DirectInputDevice made;
				if (read.Made.TryGetValue(device.InstanceGuid, out made))
				{
					ud.Device = made;
					ud.IsExclusiveMode = null;
					ud.LoadCapabilities(made.Joystick.Capabilities);
				}
				DeviceInfo hid;
				RefreshDevice(manager, ud, device, devInfos, intInfos, out hid);
				// Pads this program feeds are never taken back in as devices somebody could map, or it
				// would read its own output as an input. The rule lives in one place so that the device
				// list and the clean-up button can never disagree about what a leftover is.
				if (!VirtualDriverInstaller.IsVirtualPad(hid, devInfosById))
					insertDevices.Add(ud);
				// A pad of ours is never listed, so every read finds it new and opens it again. Left open,
				// each one keeps its handles for the life of the program: a few more on every read, and a
				// read follows every device that comes or goes on the machine.
				else if (ud.Device != null)
					ud.Device.Dispose();
				read.Made.Remove(device.InstanceGuid);
			}
			// Any the worker opened that nothing here took, such as a device listed while it was reading.
			foreach (var unclaimed in read.Made.Values)
				unclaimed.Dispose();
			read.Made.Clear();
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
			TakeInRawInputDevices(read, uds, insertDevices);
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
				// Not read while it is gone, so an Auto run under way on it would never end.
				EndSpringRun(deleteDevices[i], SpringStopUnplugged);
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
				//
				// An evicted stale virtual-pad record keeps its joystick open, once per record.
				// Disposing it here would race the engine thread that may still be polling it.
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
						var opened = new DirectInputDevice(manager, device.InstanceGuid);
						ud.Device = opened;
						ud.IsExclusiveMode = null;
						ud.LoadCapabilities(opened.Joystick.Capabilities);
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
			{
				// A device that comes back starts with no failed reads: it is tried again at once, and its
				// first fault is reported.
				ud.DiReadFailures = 0;
				ud.DiReadFaultReported = false;
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.IsOnline = true;
			}
			// The interface is read first, because the device is then found by the identifier the
			// interface supplies. Read the other way round, a controller which had just been plugged in
			// was looked up by an identifier nothing had filled in yet: the lookup found nothing, the
			// device fields were cleared, and the row showed blanks or the plain DirectInput name until
			// some later pass happened to run. Which pass that was decided what the row said, so the
			// same controller could arrive named, unnamed, or named differently in each list.
			if (device.IsHumanInterfaceDevice && ud.Device != null)
			{
				hid = FindInterface(allInterfaces, ud.Device.InterfacePath);
				// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.LoadHidDeviceInfo(hid);
			}
			LoadDevice(ud, hid, allDevices);
		}

		/// <summary>Loads the device node a device's interface belongs to, and the class of what connects it.</summary>
		/// <param name="ud">The device, its interface already loaded.</param>
		/// <param name="hid">Its interface, or null when none was found.</param>
		/// <param name="allDevices">The device nodes the read found.</param>
		static void LoadDevice(UserDevice ud, DeviceInfo hid, DeviceInfo[] allDevices)
		{
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

		#region Raw Input devices

		/// <summary>Lists each Raw Input device a read found as a device of its own, beside its DirectInput twin, and marks those it did not find as offline.</summary>
		/// <remarks>
		/// A device thread step, run when a read is taken in. A pad of ours is not in the read, so it is never listed.
		/// A device that comes back is the row it had, put online again.
		/// </remarks>
		/// <param name="uds">The listed devices when the read was taken in.</param>
		/// <param name="insertDevices">The devices to add to the list; a Raw Input device listed for the first time is added here.</param>
		static void TakeInRawInputDevices(DeviceListRead read, UserDevice[] uds, List<UserDevice> insertDevices)
		{
			var found = read.RawInputDevices;
			var allDevices = read.DevInfos ?? new DeviceInfo[0];
			foreach (var listing in found)
			{
				var ud = uds.FirstOrDefault(x => x.InstanceGuid == listing.Device.InstanceGuid);
				if (ud == null)
				{
					ud = new UserDevice();
					insertDevices.Add(ud);
				}
				LoadRawInputDevice(ud, listing, allDevices);
			}
			foreach (var ud in uds)
			{
				if (ud.InputSourceType != (int)InputSourceType.RawInput || !ud.IsOnline)
					continue;
				if (found.Any(x => x.Device.InstanceGuid == ud.InstanceGuid))
					continue;
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.IsOnline = false;
			}
		}

		/// <summary>Fills a Raw Input device's row from what a read found of it, and puts it online.</summary>
		/// <remarks>
		/// <see cref="UserDevice.Device"/> stays null: the hub reads the device, not DirectInput. Its counts and masks are
		/// those of the slots its layout fills, the slots DirectInput fills for its twin, so the mapping pages offer the
		/// same axes and sliders on both. Its objects describe the same slots, for the Direct Input page and the automatic
		/// preset, and it has no effects: Raw Input carries no force feedback.
		/// </remarks>
		static void LoadRawInputDevice(UserDevice ud, RawInputListing listing, DeviceInfo[] allDevices)
		{
			var device = listing.Device;
			var twin = listing.Twin;
			// The name the device reports, or DirectInput's names for its twin when it reports none.
			var instanceName = EngineHelper.ToXmlText(device.ProductName ?? twin?.InstanceName ?? "");
			var productName = EngineHelper.ToXmlText(device.ProductName ?? twin?.ProductName ?? "");
			// DirectInput's type for its twin, or the one its top-level collection names.
			var type = twin != null ? (int)twin.Type
				: (int)(device.Usage == 0x05 ? SharpDX.DirectInput.DeviceType.Gamepad : SharpDX.DirectInput.DeviceType.Joystick);
			var layout = listing.Layout;
			int axes = 0, sliders = 0, povs = 0, buttons = 0, axisMask = 0, sliderMask = 0;
			for (var i = 0; i < layout.Types.Length; i++)
			{
				switch (layout.Types[i])
				{
					case MapType.Axis:
						axes++;
						axisMask |= 1 << layout.Indexes[i];
						break;
					case MapType.Slider:
						sliders++;
						sliderMask |= 1 << layout.Indexes[i];
						break;
					case MapType.POV:
						povs++;
						break;
					case MapType.Button:
						buttons++;
						break;
				}
			}
			// Lock to avoid Exception: Collection was modified; enumeration operation may not execute.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				// Check if value is same to reduce grid refresh.
				if (ud.InputSourceType != (int)InputSourceType.RawInput)
					ud.InputSourceType = (int)InputSourceType.RawInput;
				if (ud.InstanceGuid != device.InstanceGuid)
					ud.InstanceGuid = device.InstanceGuid;
				if (ud.ProductGuid != device.ProductGuid)
					ud.ProductGuid = device.ProductGuid;
				if (ud.InstanceName != instanceName)
					ud.InstanceName = instanceName;
				if (ud.ProductName != productName)
					ud.ProductName = productName;
				if (ud.CapType != type)
					ud.CapType = type;
				if (!ud.CapIsHumanInterfaceDevice)
					ud.CapIsHumanInterfaceDevice = true;
				// DirectInput counts a slider as an axis.
				if (ud.CapAxeCount != axes + sliders)
					ud.CapAxeCount = axes + sliders;
				if (ud.CapButtonCount != buttons)
					ud.CapButtonCount = buttons;
				if (ud.CapPovCount != povs)
					ud.CapPovCount = povs;
				if (ud.DiAxeMask != axisMask)
					ud.DiAxeMask = axisMask;
				if (ud.DiSliderMask != sliderMask)
					ud.DiSliderMask = sliderMask;
				ud.LoadHidDeviceInfo(listing.Interface);
				// Raw Input's own path and numbers, which it gives whether or not the interface was read.
				if (ud.HidDevicePath != device.InterfacePath)
					ud.HidDevicePath = device.InterfacePath;
				if (ud.HidVendorId != device.VendorId)
					ud.HidVendorId = device.VendorId;
				if (ud.HidProductId != device.ProductId)
					ud.HidProductId = device.ProductId;
				if (ud.HidRevision != device.Version)
					ud.HidRevision = device.Version;
			}
			if (ud.DeviceObjects != listing.Objects)
			{
				ud.DeviceObjects = listing.Objects;
				// The Direct Input tab draws the objects again only when told the device changed.
				ud.DeviceChanged = true;
			}
			if (ud.DeviceEffects == null)
				ud.DeviceEffects = Array.Empty<DeviceEffectItem>();
			LoadDevice(ud, listing.Interface, allDevices);
			if (!ud.IsOnline)
			{
				// Back with no failed reads, as a DirectInput device comes back.
				ud.DiReadFailures = 0;
				ud.DiReadFaultReported = false;
				lock (SettingsManager.UserDevices.SyncRoot)
					ud.IsOnline = true;
			}
		}

		#endregion

	}
}

