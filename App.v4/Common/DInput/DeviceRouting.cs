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

		readonly Dictionary<Guid, DeviceForce> _forces = new Dictionary<Guid, DeviceForce>();

		/// <summary>The listed devices the person has unticked: on the Devices page, or on every row of the game that maps them to a controller.</summary>
		/// <remarks>A place named in Pass through is passed over while one of these holds it. A device mapped nowhere in the game is not one of them.</remarks>
		readonly UserDevice[] _unticked;

		DeviceRouting(UserSetting[] rows, IList<PadSetting> padSettings, IList<UserDevice> devices, int enableMask, UserDevice[] unticked)
		{
			Rows = rows;
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
				// One tab's settings make the effects, so they are not made again on every pass.
				var settings = forcing.Length > 0
					? forcing[0].Settings
					: tabs.Select(x => x.Settings).FirstOrDefault(x => x != null);
				_forces[device.Key] = new DeviceForce(settings, forcing.Select(x => x.Pad).Distinct().ToArray());
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
			MappedDevices = devices.Where(x => x != null && mapped.Contains(x.InstanceGuid)).ToArray();
			RowDevices = new UserDevice[rows.Length];
			RowMaps = new List<Map>[rows.Length];
			RowDPads = new int[rows.Length];
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
			}
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
			// The game's rows on a controller tab, ticked or not.
			var tabRows = game == null
				? new UserSetting[0]
				: settings
					.Where(x => x != null && string.Compare(x.FileName, game.FileName, true) == 0
						&& x.MapTo >= (int)MapTo.Controller1 && x.MapTo <= (int)MapTo.Controller4)
					.ToArray();
			// A device unticked on the Devices page is left out of every game, as a row unticked in a tab's list is.
			var disabled = new HashSet<Guid>(devices.Where(x => x != null && !x.IsEnabled).Select(x => x.InstanceGuid));
			var rows = tabRows.Where(x => x.IsEnabled && !disabled.Contains(x.InstanceGuid)).ToArray();
			// The devices left out that way. One on no row of the game is not among them, unless unticked on the Devices page.
			var routed = new HashSet<Guid>(rows.Select(x => x.InstanceGuid));
			var onTabs = new HashSet<Guid>(tabRows.Select(x => x.InstanceGuid));
			var unticked = devices
				.Where(x => x != null && !routed.Contains(x.InstanceGuid) && (!x.IsEnabled || onTabs.Contains(x.InstanceGuid)))
				.ToArray();
			return new DeviceRouting(rows, padSettings, devices, game == null ? 0 : game.EnableMask, unticked);
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
				SettingsManager.UserSettings.ItemsToArraySyncronized(),
				SettingsManager.PadSettings.ItemsToArraySyncronized(),
				SettingsManager.UserDevices.ItemsToArraySyncronized());
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
			// the Devices page. Any other change to a listed device does not: its state is read on every pass, and
			// its ids when the places are worked out.
			SettingsManager.UserDevices.Items.ListChanged += (sender, e) =>
			{
				if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted
					|| e.ListChangedType == ListChangedType.Reset
					|| (e.ListChangedType == ListChangedType.ItemChanged && e.PropertyDescriptor != null
						&& e.PropertyDescriptor.Name == nameof(UserDevice.IsEnabled)))
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
		public DeviceForce(PadSetting padSetting, int[] forcePads)
		{
			PadSetting = padSetting;
			ForcePads = forcePads;
		}

		/// <summary>The settings the device's effects are made with, or null when none of its rows' settings is stored.</summary>
		/// <remarks>
		/// Those of the lowest switched-on tab whose Force feedback enabled switch is on, so that switch is on
		/// exactly when <see cref="ForcePads"/> has a tab. With it off on every switched-on tab, those of the
		/// lowest switched-on tab, which still decide the wheel's own centering.
		/// </remarks>
		public readonly PadSetting PadSetting;

		/// <summary>The pad indexes, 0 to 3, whose Force feedback enabled switch is on, lowest first. Empty when none is.</summary>
		/// <remarks>Only tabs switched on for the game, which have a virtual controller for a game to speak through.</remarks>
		public readonly int[] ForcePads;
	}
}
