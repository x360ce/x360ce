using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	/// <summary>Where each device goes in the current game: the rows each controller combines, where each device's force feedback comes from, and where each controller's force is passed on to.</summary>
	/// <remarks>
	/// The engine reads this once a pass, through <see cref="Current"/>, and never builds it. The interface
	/// builds a new one whenever a mapping, the settings a mapping points at, the game, one of its tab
	/// switches, or the devices listed change, and replaces the old one whole, so a pass sees the old routing or the new one and never half of each.
	/// Asking the settings lists instead takes the lock the interface holds while it draws a change, and
	/// copies the list, once per device on every pass. The routing also carries what the engine would otherwise
	/// look up in the settings lists on every pass: the devices it reads, and each routed row's device, its
	/// stored settings' mappings, and its D-Pad.
	///
	/// Only the rows the engine converts count: rows of the current game, on a controller tab, switched on
	/// (ticked) in the tab's list, of a device ticked on the Devices page. A row of another game, or one
	/// unticked in either list, reaches nothing: its device is not read, not held, not forced and not passed
	/// force on to. A row on a tab whose switch is off is still combined, for the tab's own page, but gives no
	/// force.
	///
	/// Force passed on to a real controller takes every ticked row of the current game on the tab, whatever
	/// the tab's switch. A tab switched off still passes on the stop that unplugging its controller sends.
	/// </remarks>
	public sealed class DeviceRouting
	{
		/// <summary>The routing the engine reads. Replaced whole, never changed.</summary>
		public static DeviceRouting Current { get { return _current; } }
		static volatile DeviceRouting _current = Build(null, new UserSetting[0], new PadSetting[0]);

		/// <summary>Every routed row, in the order of the settings list.</summary>
		public readonly UserSetting[] Rows;

		/// <summary>The routed rows of each controller, by pad index: <c>PadRows[0]</c> is Controller 1.</summary>
		public readonly UserSetting[][] PadRows = new UserSetting[4][];

		/// <summary>The rows that pass each controller's force on to a real one, by pad index, in the order they are asked. Empty for a controller that passes nothing on.</summary>
		public readonly PassThroughSource[][] PadPassThrough = new PassThroughSource[4][];

		/// <summary>True when a controller passes its force on, so the engine has places to work out.</summary>
		public readonly bool PassesForceThrough;

		/// <summary>The devices on the current game's ticked rows mapped to a controller, in the order of the devices list: the devices the engine reads.</summary>
		/// <remarks>
		/// Also the DirectInput twin of each Raw Input device sent force, which the engine reads and holds to drive the motors
		/// Raw Input cannot reach; it is on no row, so its input reaches no controller.
		///
		/// The engine reads only those online, and checks that itself on every pass. A device that drops out of
		/// this list, unticked in a tab's list or on the Devices page, taken off its tabs or of another game, is
		/// let go of by the engine on the first pass of the routing without it.
		/// </remarks>
		public readonly UserDevice[] MappedDevices;

		/// <summary>For each of <see cref="Rows"/>, the device it reads, or null when that device is not listed.</summary>
		public readonly UserDevice[] RowDevices;

		/// <summary>For each of <see cref="Rows"/>, its settings' mappings as <see cref="PadSetting.Maps"/> built them, or null when the settings it points at are not stored.</summary>
		/// <remarks>An edit builds a new list rather than changing this one, and the edit builds a new routing.</remarks>
		public readonly List<Map>[] RowMaps;

		/// <summary>For each of <see cref="Rows"/>, the POV its D-Pad is mapped to, counted from one, or 0 for none.</summary>
		public readonly int[] RowDPads;

		/// <summary>For each of <see cref="Rows"/>, where its device's X and Y axes press the D-Pad, from <see cref="ConvertHelper.GetAxisToDPadPressPoint"/>, or -1 while its Axis to D-Pad is off.</summary>
		public readonly int[] RowAxisToDPadPressPoints;

		/// <summary>For each of <see cref="Rows"/>, how far its Axis to D-Pad's offset moves the centre, from <see cref="ConvertHelper.GetAxisToDPadShift"/>.</summary>
		public readonly int[] RowAxisToDPadShifts;

		/// <summary>For each of <see cref="Rows"/>, how a mouse moves its left and right stick, or null when its Mouse page is at its defaults.</summary>
		/// <remarks>Made with the routing and kept by it, so a stick's position and speed carry from pass to pass until the settings change.</remarks>
		public readonly MouseStick[][] RowMouse;

		/// <summary>For each of <see cref="Rows"/>, its device's number on its controller, from <see cref="GetDeviceNumbers"/>.</summary>
		public readonly int[] RowNumbers;

		/// <summary>Each controller's routed devices by their number, by pad index: <c>PadDevices[0][2]</c> is device 2 on Controller 1, null for a number no ticked row has.</summary>
		public readonly UserDevice[][] PadDevices = new UserDevice[4][];

		/// <summary>The device a formula names by its controller and number, <c>c1.d2</c>, or null when this routing reads none there.</summary>
		/// <remarks>An array read, so a formula can ask on every pass.</remarks>
		public UserDevice GetDevice(int pad, int number)
		{
			if (pad < 1 || pad > PadDevices.Length)
				return null;
			var devices = PadDevices[pad - 1];
			return number > 0 && number < devices.Length ? devices[number] : null;
		}

		readonly Dictionary<Guid, DeviceForce> _forces = new Dictionary<Guid, DeviceForce>();

		/// <summary>The DirectInput twin each Raw Input device sent force drives its motors through, by the Raw Input device.</summary>
		readonly Dictionary<Guid, UserDevice> _forceDevices = new Dictionary<Guid, UserDevice>();

		/// <summary>The listed devices the person has unticked: on the Devices page, or on every row of the game that maps them to a controller.</summary>
		/// <remarks>A place named in Pass through is passed over while one of these holds it. A device mapped nowhere in the game is not one of them.</remarks>
		readonly UserDevice[] _unticked;

		DeviceRouting(UserSetting[] rows, int[] rowNumbers, IList<PadSetting> padSettings, IList<UserDevice> devices, int enableMask, UserDevice[] unticked)
		{
			Rows = rows;
			RowNumbers = rowNumbers;
			_unticked = unticked;
			for (var pad = 0; pad < PadRows.Length; pad++)
				PadRows[pad] = rows.Where(x => x.MapTo == pad + 1).ToArray();
			foreach (var device in rows.GroupBy(x => x.InstanceGuid))
			{
				// The device's tabs switched on for the game, lowest first, each with the stored settings it
				// points at. A tab switched off has no virtual controller for a game to speak through, so it
				// drives nothing: it is never a force source and never supplies the settings.
				var tabs = device
					.Where(x => (enableMask & (int)AppHelper.GetMapFlag((MapTo)x.MapTo)) != 0)
					.OrderBy(x => x.MapTo)
					.Select(x => new
					{
						Pad = x.MapTo - 1,
						Settings = padSettings.FirstOrDefault(p => p != null && p.PadSettingChecksum == x.PadSettingChecksum),
					})
					.ToArray();
				// On no switched-on tab: no force source, so the engine stops what the device was playing.
				if (tabs.Length == 0)
					continue;
				var forcing = tabs.Where(x => x.Settings != null && x.Settings.ForceEnable == "1").ToArray();
				// One tab's settings make the effects, so they are not made again on every pass: the lowest switched-on
				// tab's, whatever its Enable says, so ticking Enable never moves the settings to another tab.
				var source = tabs.FirstOrDefault(x => x.Settings != null);
				_forces[device.Key] = new DeviceForce(source?.Settings, source == null ? -1 : source.Pad,
					forcing.Select(x => x.Pad).Distinct().ToArray());
			}
			// Raw Input sends nothing back: Windows reads a device through it and offers no way to write to one. So the force
			// of a Raw Input device goes to its DirectInput twin whose driver takes force feedback (UserDevice.ForceTwin),
			// which the engine then reads and holds for that force alone. The twin's own ticks do not decide this: here it is
			// the Raw Input device's way to its motors, not a device of its own, and a person who reads the controller through
			// Raw Input alone unticks it.
			var forceTwins = new List<UserDevice>();
			foreach (var raw in devices)
			{
				DeviceForce force;
				if (raw == null || raw.InputSource != InputSourceType.RawInput
					|| !_forces.TryGetValue(raw.InstanceGuid, out force) || !force.Drives)
					continue;
				var twin = raw.ForceTwin(devices);
				if (twin == null)
					continue;
				// The twin on a switched-on tab of its own: both tabs' force reaches the motors, and the effects are made
				// with the settings of the lower of the two tabs, as for any device.
				DeviceForce own;
				if (_forces.TryGetValue(twin.InstanceGuid, out own))
				{
					var lower = own.SettingsPad >= 0 && own.SettingsPad < force.SettingsPad ? own : force;
					force = new DeviceForce(lower.PadSetting, lower.SettingsPad,
						own.ForcePads.Concat(force.ForcePads).Distinct().OrderBy(x => x).ToArray());
				}
				_forces[twin.InstanceGuid] = force;
				_forceDevices[raw.InstanceGuid] = twin;
				forceTwins.Add(twin);
			}
			for (var pad = 0; pad < PadPassThrough.Length; pad++)
			{
				PadPassThrough[pad] = PassThroughSources(PadRows[pad], padSettings, devices);
				if (PadPassThrough[pad].Length > 0)
					PassesForceThrough = true;
			}
			// What the engine would otherwise look up in the settings lists on every pass, for every device and every
			// row, worked out here once a change. Copying the lists for it would take the lock the interface holds
			// while it draws a change, and make about a kilobyte a row.
			var byInstance = new Dictionary<Guid, UserDevice>();
			foreach (var device in devices)
				if (device != null && !byInstance.ContainsKey(device.InstanceGuid))
					byInstance.Add(device.InstanceGuid, device);
			var mapped = new HashSet<Guid>(rows.Select(x => x.InstanceGuid));
			MappedDevices = devices.Where(x => x != null && (mapped.Contains(x.InstanceGuid) || forceTwins.Contains(x))).ToArray();
			RowDevices = new UserDevice[rows.Length];
			RowMaps = new List<Map>[rows.Length];
			RowDPads = new int[rows.Length];
			RowAxisToDPadPressPoints = Enumerable.Repeat(-1, rows.Length).ToArray();
			RowAxisToDPadShifts = new int[rows.Length];
			RowMouse = new MouseStick[rows.Length][];
			for (var i = 0; i < rows.Length; i++)
			{
				UserDevice device;
				byInstance.TryGetValue(rows[i].InstanceGuid, out device);
				RowDevices[i] = device;
				var checksum = rows[i].PadSettingChecksum;
				var ps = padSettings.FirstOrDefault(x => x != null && x.PadSettingChecksum == checksum);
				if (ps == null)
					continue;
				RowMaps[i] = ps.Maps;
				MapType type;
				int index;
				// The D-Pad names a POV, counted from one. A button, or nothing, maps no POV.
				if (SettingsConverter.TryParseIniValue(ps.DPad, out type, out index, MapCode.DPad) && type == MapType.POV)
					RowDPads[i] = index;
				int value;
				if (int.TryParse(ps.AxisToDPadEnabled, out value) && value != 0)
				{
					// An empty dead zone is the whole of half the travel, as the settings start.
					RowAxisToDPadPressPoints[i] = ConvertHelper.GetAxisToDPadPressPoint(int.TryParse(ps.AxisToDPadDeadZone, out value) ? value : 256);
					RowAxisToDPadShifts[i] = ConvertHelper.GetAxisToDPadShift(int.TryParse(ps.AxisToDPadOffset, out value) ? value : 0);
				}
				var left = MouseStickSettings.From(ps, false);
				var right = MouseStickSettings.From(ps, true);
				if (left != null || right != null)
					RowMouse[i] = new[] { left == null ? null : new MouseStick(left), right == null ? null : new MouseStick(right) };
			}
			for (var pad = 0; pad < PadDevices.Length; pad++)
			{
				var count = 0;
				for (var i = 0; i < rows.Length; i++)
				{
					if (rows[i].MapTo == pad + 1)
						count = Math.Max(count, RowNumbers[i]);
				}
				PadDevices[pad] = new UserDevice[count + 1];
			}
			for (var i = 0; i < rows.Length; i++)
				PadDevices[rows[i].MapTo - 1][RowNumbers[i]] = RowDevices[i];
		}

		/// <summary>The device the engine drives this device's force feedback through: for a Raw Input device sent force, its DirectInput twin; otherwise the device itself.</summary>
		/// <remarks>Where the centering spring's Auto run goes, so the engine finds it on the device whose motors it drives.</remarks>
		public UserDevice ForceDevice(UserDevice device)
		{
			UserDevice twin;
			return device != null && _forceDevices.TryGetValue(device.InstanceGuid, out twin) ? twin : device;
		}

		/// <summary>Where a device's force feedback comes from; false when the device is on no routed row of a switched-on tab.</summary>
		/// <remarks>Called by the engine for every device that can vibrate, on every pass. It allocates nothing and takes no lock.</remarks>
		public bool TryGetForce(Guid instanceGuid, out DeviceForce force)
		{
			return _forces.TryGetValue(instanceGuid, out force);
		}

		/// <summary>Where a controller's force is passed on to, and the settings which said so; <see cref="XInputPlaces.Unknown"/> and null for nowhere.</summary>
		/// <remarks>
		/// The first of the controller's <see cref="PadPassThrough"/> rows that can answer does: one that names a
		/// place, or one whose device holds a place in the table. The settings come back with the place because
		/// the strengths written on them apply to the force passed on.
		///
		/// A named place is passed over while the table has it held by a device the person has unticked, so an
		/// unticked controller is sent no force this way either. A place held by a device mapped nowhere in the
		/// game, or by one the table does not know, is still sent force: that is what naming a place is for.
		///
		/// It allocates nothing and takes no lock.
		/// </remarks>
		/// <param name="pad">The pad index, 0 to 3.</param>
		/// <param name="places">The XInput place of each device, as <see cref="XInputPlaces.Current"/> holds it.</param>
		/// <param name="padSetting">The settings whose strengths apply to the force passed on, or null.</param>
		public int PassThroughPlace(int pad, IReadOnlyDictionary<string, int> places, out PadSetting padSetting)
		{
			PassThroughSource source;
			var place = PassThroughPlace(pad, places, out source);
			padSetting = source == null ? null : source.PadSetting;
			return place;
		}

		/// <summary>The same, with the row that answered, whose strengths were worked out when this routing was built; null for nowhere.</summary>
		/// <remarks>For the engine, which applies the strengths with no lock and makes nothing. The engine asks only when this routing or the table is new.</remarks>
		public int PassThroughPlace(int pad, IReadOnlyDictionary<string, int> places, out PassThroughSource source)
		{
			source = null;
			if (pad < 0 || pad >= PadPassThrough.Length)
				return XInputPlaces.Unknown;
			var sources = PadPassThrough[pad];
			for (var i = 0; i < sources.Length; i++)
			{
				var candidate = sources[i];
				var place = candidate.Device == null
					? candidate.Place
					: XInputPlaces.PlaceOf(places, candidate.Device.HidDeviceId, candidate.Device.DevDeviceId);
				if (place < 0 || (candidate.Device == null && IsHeldByUnticked(place, places)))
					continue;
				source = candidate;
				return place;
			}
			return XInputPlaces.Unknown;
		}

		/// <summary>Whether the table has this place held by a device the person has unticked.</summary>
		/// <remarks>Asked only for a named place, when the places are worked out. It allocates nothing and takes no lock.</remarks>
		bool IsHeldByUnticked(int place, IReadOnlyDictionary<string, int> places)
		{
			for (var i = 0; i < _unticked.Length; i++)
				if (XInputPlaces.PlaceOf(places, _unticked[i].HidDeviceId, _unticked[i].DevDeviceId) == place)
					return true;
			return false;
		}

		/// <summary>The rows of one controller that pass its force on, in the order they are asked.</summary>
		/// <remarks>
		/// Only rows whose stored settings have Pass through on. One naming a place from 1 to 4 answers
		/// outright, so no row after it is kept. Any other value means the place the row's device holds, which
		/// is known only once the places are read, and when it is not known the next row is asked. A row whose
		/// device is not in the list is passed over.
		/// </remarks>
		/// <param name="padRows">The game's ticked rows on the controller's tab, in the order of the settings list.</param>
		/// <param name="padSettings">The stored settings the rows point at by checksum.</param>
		/// <param name="devices">The devices the rows point at.</param>
		static PassThroughSource[] PassThroughSources(IEnumerable<UserSetting> padRows, IList<PadSetting> padSettings, IList<UserDevice> devices)
		{
			var sources = new List<PassThroughSource>();
			foreach (var row in padRows)
			{
				var ps = padSettings.FirstOrDefault(x => x != null && x.PadSettingChecksum == row.PadSettingChecksum);
				if (ps == null || ps.ForcePassThrough != "1")
					continue;
				int wanted;
				// One to four names a place outright. Zero, empty, or anything unreadable means work it out.
				if (int.TryParse(ps.ForcePassThroughIndex, out wanted) && wanted >= 1 && wanted <= 4)
				{
					sources.Add(new PassThroughSource(ps, wanted - 1, null));
					break;
				}
				// The place the device itself holds. Only a controller with an XInput face has one, which is
				// exactly the kind whose motors cannot be reached any other way.
				var device = devices.FirstOrDefault(x => x != null && x.InstanceGuid == row.InstanceGuid);
				if (device != null)
					sources.Add(new PassThroughSource(ps, XInputPlaces.Unknown, device));
			}
			return sources.ToArray();
		}

		/// <summary>Works out the routing for a game from these settings.</summary>
		/// <param name="game">The current game, or null for none, which routes nothing. Only its switched-on tabs give force.</param>
		/// <param name="settings">The mapping rows.</param>
		/// <param name="padSettings">The stored settings the rows point at by checksum.</param>
		/// <param name="devices">The devices the rows point at, or null for none, when no row can pass force on to the place its device holds.</param>
		public static DeviceRouting Build(UserGame game, IList<UserSetting> settings, IList<PadSetting> padSettings, IList<UserDevice> devices = null)
		{
			devices = devices ?? new UserDevice[0];
			var tabRows = GetTabRows(game, settings);
			var numbers = GetDeviceNumbers(tabRows);
			// A device unticked on the Devices page is left out of every game, as a row unticked in a tab's list is.
			var disabled = new HashSet<Guid>(devices.Where(x => x != null && !x.IsEnabled).Select(x => x.InstanceGuid));
			var rows = tabRows.Where(x => x.IsEnabled && !disabled.Contains(x.InstanceGuid)).ToArray();
			// The devices left out that way. One on no row of the game is not among them, unless unticked on the Devices page.
			var routed = new HashSet<Guid>(rows.Select(x => x.InstanceGuid));
			var onTabs = new HashSet<Guid>(tabRows.Select(x => x.InstanceGuid));
			var unticked = devices
				.Where(x => x != null && !routed.Contains(x.InstanceGuid) && (!x.IsEnabled || onTabs.Contains(x.InstanceGuid)))
				.ToArray();
			return new DeviceRouting(rows, rows.Select(x => numbers[x]).ToArray(), padSettings, devices, game == null ? 0 : game.EnableMask, unticked);
		}

		/// <summary>The game's rows on a controller tab, ticked or not, in the order of the settings list.</summary>
		static UserSetting[] GetTabRows(UserGame game, IList<UserSetting> settings)
		{
			return game == null
				? new UserSetting[0]
				: settings
					.Where(x => x != null && string.Compare(x.FileName, game.FileName, true) == 0
						&& x.MapTo >= (int)MapTo.Controller1 && x.MapTo <= (int)MapTo.Controller4)
					.ToArray();
		}

		/// <summary>The number of each device on its controller in a game: 1 for the controller's first row in the settings list, ticked or not.</summary>
		/// <remarks>
		/// An AutoHotkey script names a device by it, <c>c1.d2.a1</c>, and each controller tab shows it beside the device. Unticking a row
		/// or sorting the tab's list changes no number, so a script keeps working.
		/// </remarks>
		public static Dictionary<UserSetting, int> GetDeviceNumbers(UserGame game, IList<UserSetting> settings)
		{
			return GetDeviceNumbers(GetTabRows(game, settings));
		}

		static Dictionary<UserSetting, int> GetDeviceNumbers(UserSetting[] tabRows)
		{
			var numbers = new Dictionary<UserSetting, int>();
			var counts = new int[(int)MapTo.Controller4 + 1];
			foreach (var row in tabRows)
				numbers[row] = ++counts[row.MapTo];
			return numbers;
		}

		/// <summary>Builds the routing from the settings as they are now and hands it to the engine.</summary>
		/// <remarks>
		/// Runs on the interface thread only: the settings lists deliver their change notices there, and the
		/// game and its tab switches are chosen there. The lists are copied here, once a change, instead of by the engine once a
		/// pass.
		///
		/// It takes no lock of its own, and needs none only because every caller is on that one thread, so
		/// the builds are published in the order the changes were made. Two builds running on two threads
		/// could publish out of order, and the engine would go on reading the older one until the next
		/// change. A list changed on another thread still delivers its notice on the interface thread.
		/// </remarks>
		public static void Refresh()
		{
			_current = Build(SettingsManager.CurrentGame,
				SettingsManager.UserSettings.ItemsToArraySynchronized(),
				SettingsManager.PadSettings.ItemsToArraySynchronized(),
				SettingsManager.UserDevices.ItemsToArraySynchronized());
			// A plugin device's MCP server listens on the port of the controller and place it is mapped to.
			Plugins.PluginMcpDoors.Apply();
		}

		static bool _watching;

		/// <summary>Builds the routing again whenever a mapping, the settings it points at, a game's tab switches, the devices listed, or a device's tick on the Devices page change. Called once, when the settings are loaded.</summary>
		public static void Watch()
		{
			if (_watching)
				return;
			_watching = true;
			SettingsManager.UserSettings.Items.ListChanged += (sender, e) => Refresh();
			SettingsManager.PadSettings.Items.ListChanged += (sender, e) => Refresh();
			SettingsManager.UserGames.Items.ListChanged += (sender, e) => Refresh();
			// A device coming into the list or leaving it changes which devices the engine reads and which rows can
			// pass force on to the place it holds, and a reset of the list may have done either. So does its tick on
			// the Devices page, and its interface path, which pairs a Raw Input device with the DirectInput twin its
			// force goes to. Any other change to a listed device does not: its state is read on every pass, and its
			// ids when the places are worked out.
			SettingsManager.UserDevices.Items.ListChanged += (sender, e) =>
			{
				if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted
					|| e.ListChangedType == ListChangedType.Reset
					|| (e.ListChangedType == ListChangedType.ItemChanged && e.PropertyDescriptor != null
						&& (e.PropertyDescriptor.Name == nameof(UserDevice.IsEnabled) || e.PropertyDescriptor.Name == nameof(UserDevice.HidDevicePath))))
					Refresh();
			};
			Refresh();
		}
	}

	/// <summary>One row's say in where a controller's force is passed on to.</summary>
	public sealed class PassThroughSource
	{
		public PassThroughSource(PadSetting padSetting, int place, UserDevice device)
		{
			PadSetting = padSetting;
			Place = place;
			Device = device;
			// Worked out here, with the routing, whenever the settings change. Reading a strength left unset reads
			// its default, which takes the lock the interface reads defaults under and makes a little.
			LargeScale = padSetting.GetForceScale(true);
			SmallScale = padSetting.GetForceScale(false);
		}

		/// <summary>The settings which asked for it, whose strengths apply to the force passed on.</summary>
		public readonly PadSetting PadSetting;

		/// <summary>The strengths the large motor is played at, as <see cref="PadSetting.GetForceScale"/> gives them.</summary>
		public readonly int LargeScale;

		/// <summary>The strengths the small motor is played at, as <see cref="PadSetting.GetForceScale"/> gives them.</summary>
		public readonly int SmallScale;

		/// <summary>The place the settings name, 0 to 3, or <see cref="XInputPlaces.Unknown"/> when they name none and the place <see cref="Device"/> holds is used.</summary>
		public readonly int Place;

		/// <summary>The row's device, whose own place is used; null when the settings name a place.</summary>
		/// <remarks>Its ids are read when the places are worked out, so a device found again under a new id is looked up by that id.</remarks>
		public readonly UserDevice Device;
	}

	/// <summary>Where one device's force feedback comes from.</summary>
	public sealed class DeviceForce
	{
		public DeviceForce(PadSetting padSetting, int settingsPad, int[] forcePads)
		{
			PadSetting = padSetting;
			SettingsPad = settingsPad;
			ForcePads = forcePads;
			Spring = padSetting != null && padSetting.ForceSpringEnable == "1";
		}

		/// <summary>The settings the device's effects are made with, or null when none of its rows' settings is stored.</summary>
		/// <remarks>
		/// Those of the lowest switched-on tab the device is on, whatever its Force feedback enabled switch says: they
		/// shape the force of every tab in <see cref="ForcePads"/>, and decide the centering spring and the wheel's own
		/// centering. The page of every other tab greys them out and names this one.
		/// </remarks>
		public readonly PadSetting PadSetting;

		/// <summary>The pad index, 0 to 3, of the tab whose settings <see cref="PadSetting"/> are, or -1 when none is stored.</summary>
		public readonly int SettingsPad;

		/// <summary>The pad indexes, 0 to 3, whose Force feedback enabled switch is on, lowest first. Empty when none is.</summary>
		/// <remarks>Only tabs switched on for the game, which have a virtual controller for a game to speak through.</remarks>
		public readonly int[] ForcePads;

		/// <summary>Whether the centering spring of <see cref="PadSetting"/> is ticked, as it was when the routing was built.</summary>
		/// <remarks>
		/// The spring holds the wheel by itself and needs no tab to send force, so a wheel is driven for its spring alone,
		/// with no game vibration: that comes only from <see cref="ForcePads"/>.
		/// </remarks>
		public readonly bool Spring;

		/// <summary>Whether the engine drives the device's force: a tab sends force to it, or its centering spring is ticked.</summary>
		public bool Drives { get { return ForcePads.Length > 0 || Spring; } }
	}
}
