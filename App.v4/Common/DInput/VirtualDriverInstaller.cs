using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace x360ce.App.DInput
{
	public class VirtualDriverInstaller
	{

		#region Install/Uninstall ViGEmBus

		static Guid GUID_DEVINTERFACE_BUSENUM_VIGEM = new Guid("96E42B22-F5E9-42F8-B043-ED0F932F014F");
		public static SP_DRVINFO_DATA GetViGemBusDriverInfo()
		{
			var flags = DIGCF.DIGCF_PRESENT | DIGCF.DIGCF_DEVICEINTERFACE;
			var driver = DeviceDetector.GetDrivers(GUID_DEVINTERFACE_BUSENUM_VIGEM, flags).FirstOrDefault();
			return driver;
		}

		public static SP_DRVINFO_DATA GetHidGuardianDriverInfo()
		{
			var driver = DeviceDetector.GetDrivers(DEVCLASS.SYSTEM, DIGCF.DIGCF_PRESENT, SPDIT.SPDIT_COMPATDRIVER, null, HidGuardianHardwareId).FirstOrDefault();
			return driver;
		}

		public static string GetViGEmBusPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return Path.Combine(baseDirectory, "Program Files", "ViGEm ViGEmBus");
		}

		static bool ExtractViGemBusFiles()
		{
			var target = GetViGEmBusPath();
			return ExtractViGemFiles("ViGEmBus", target);
		}

		public static string[] ViGEmBusHardwareIds = { "Root\\ViGEmBus", "Nefarius\\ViGEmBus\\Gen1" };
		public const string HidGuardianHardwareId = "Root\\HidGuardian";

		#region Pads left behind

		/// <summary>
		/// Pads created by the virtual bus and never removed, from a run that ended without shutting
		/// down cleanly.
		/// </summary>
		/// <remarks>
		/// The existing device clean-up looks for devices that are offline, flagged with a problem, or
		/// unknown. A pad left behind is none of those: it is present, healthy, and simply nobody's.
		/// It matters because only four XInput places exist, so a handful of these fill every one and
		/// the pad this program creates is pushed out of reach. What a player sees then is a controller
		/// that moves on its own, because the state on show belongs to somebody else's leftover.
		/// </remarks>
		public static DeviceInfo[] GetLeftoverVirtualPads()
		{
			// Records of controllers long gone count too: they are left behind exactly as the present
			// ones are, and the same removal takes them away.
			var ids = ControllerFamilyIds(true);
			var key = string.Join("|", ids) + "#" + string.Join("|", ControllerFamilyIds(false))
				+ "#" + string.Join(",", OurSerials().OrderBy(x => x).Select(x => x.ToString()).ToArray());
			lock (LeftoverLock)
				if (key == LastLeftoverKey)
					return LastLeftovers;
			var leftovers = LeftoversOf(DeviceDetector.GetDevices(ids, true, false), OurSerials());
			lock (LeftoverLock)
			{
				LastLeftoverKey = key;
				LastLeftovers = leftovers;
			}
			return leftovers;
		}

		/// <summary>The same judgement against a device list already read and a given list of bus numbers, so it can be asked without a machine.</summary>
		/// <param name="all">The controller family, as <see cref="ReadControllerTree"/> reads it.</param>
		/// <param name="serials">The bus numbers of the controllers this program is holding.</param>
		public static DeviceInfo[] LeftoversOf(DeviceInfo[] all, ICollection<uint> serials)
		{
			// The family is named by the controller, or by its highest face when the controller has gone.
			return LeftoverGroups(all, IndexById(all), serials)
				.Select(g => g.FirstOrDefault(x => string.Equals(x.DeviceId, g.Key, StringComparison.OrdinalIgnoreCase))
					?? g.First())
				.OrderBy(x => x.DeviceId)
				.ToArray();
		}

		/// <summary>Every device of each leftover controller, one list per controller, each device before its parent: what removing the leftovers removes.</summary>
		/// <remarks>
		/// Removing a device does not take its children with it. Removing only the device a leftover is named by leaves its
		/// faces behind, and the next look finds them as a leftover of their own. So every device of the family goes, the
		/// deepest first, and the controller counts once, as <see cref="LeftoversOf"/> names it.
		/// </remarks>
		/// <param name="all">The controller family with records, as <see cref="ReadControllerTree"/> reads it.</param>
		/// <param name="serials">The bus numbers of the controllers this program is holding.</param>
		public static DeviceInfo[][] LeftoverFamiliesOf(DeviceInfo[] all, ICollection<uint> serials)
		{
			var byId = IndexById(all);
			return LeftoverGroups(all, byId, serials)
				.Select(g => g.OrderByDescending(x => AncestorCount(x, byId)).ToArray())
				.ToArray();
		}

		/// <summary>The devices left behind, gathered by the controller each belongs to.</summary>
		static IEnumerable<IGrouping<string, DeviceInfo>> LeftoverGroups(DeviceInfo[] all, Dictionary<string, DeviceInfo> byId, ICollection<uint> serials)
		{
			return all
				.Where(x => IsVirtualPad(x, byId))
				// Not the ones this program is using right now. Offering to remove those would break
				// the very thing somebody pressing the button is trying to repair.
				.Where(x => !IsOneOfOurs(x, byId, serials))
				// One entry per controller, not per device. A controller is a small family - the thing
				// itself and a face for each way of reading it - so counting devices reported one left
				// behind as three, and named the same controller three times over.
				//
				// The faces carry the XInput marker and the controller does not, so a face is gathered by
				// walking up to the first thing without it, or to the highest face when the chain breaks
				// before it, and the controller is gathered by itself.
				// Walking up from the controller as well would take it to the bus that made it, which is
				// shared by every controller on it - so each one would be filed under its own maker and
				// counted apart from its own faces.
				.GroupBy(x => VirtualDriverInstaller.CarriesInputGroup(x.DeviceId)
					|| VirtualDriverInstaller.CarriesInputGroup(x.HardwareIds)
						? XInputPlaces.HardwareOf(x, byId)
						: x.DeviceId, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>How many devices above this one are known, walking up until a parent is missing or seen twice.</summary>
		static int AncestorCount(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var count = 0;
			DeviceInfo parent;
			while (!string.IsNullOrEmpty(device.ParentDeviceId) && seen.Add(device.ParentDeviceId)
				&& byId.TryGetValue(device.ParentDeviceId, out parent))
			{
				count++;
				device = parent;
			}
			return count;
		}

		/// <summary>The leftovers at the last look, and what they were judged from.</summary>
		/// <remarks>
		/// Judging reads the description of every controller record Windows keeps, and a machine where
		/// many runs ended badly keeps hundreds: measured at four seconds for 423, on a check that runs
		/// every five, holding the lock the device list read waits on. The ids alone cost milliseconds,
		/// and the answer changes only when they change, when one comes or goes, or when the controllers
		/// this program holds change, so those are the key and the reading is done only when it moves.
		/// </remarks>
		static string LastLeftoverKey;
		static DeviceInfo[] LastLeftovers = new DeviceInfo[0];
		static readonly object LeftoverLock = new object();

		/// <summary>
		/// Whether a controller is one this program currently has plugged in.
		/// </summary>
		/// <remarks>
		/// This program adds and removes controllers while it runs, so which ones are its own changes
		/// from moment to moment. Anything decided once, at start-up, is wrong shortly afterwards: it
		/// would miss one this program abandoned during the run, and would call one it created later
		/// somebody else's.
		///
		/// The bus knows each controller by a number, and Windows puts that same number at the end of
		/// the controller's name, after a backslash on the current bus and an ampersand on older ones.
		/// So the numbers of the controllers this program is holding are asked for directly and
		/// matched against the name.
		/// That is a clear reference to its own, rather than a guess from timing.
		///
		/// Only the device the bus made - the one whose parent is the bus - is matched by its number.
		/// Its faces reach it by walking up. A face whose chain breaks before reaching it, as a pad
		/// left behind by a run that ended badly does, is never ours, whatever its own name ends in.
		///
		/// If the numbers cannot be read, nothing is claimed. Being wrong that way mentions a
		/// controller that need not be mentioned; being wrong the other way offers to remove the one
		/// in use.
		/// </remarks>
		public static bool IsOneOfOurs(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			return IsOneOfOurs(device, byId, OurSerials());
		}

		/// <summary>The same question against a given list of bus numbers, so it can be asked without a bus.</summary>
		public static bool IsOneOfOurs(DeviceInfo device, Dictionary<string, DeviceInfo> byId, ICollection<uint> serials)
		{
			if (device == null || byId == null || string.IsNullOrEmpty(device.DeviceId))
				return false;
			if (serials == null || serials.Count == 0)
				return false;
			// A controller is not one device but a small family: the one the bus creates and the two
			// beneath it that Windows adds. Only the one the bus made carries the number, so a face walks
			// up to it and it alone is read. The name of any other device in the family ends in something
			// that is not a bus number - a pad left behind has a face ending in "&01" - and reading those
			// would claim somebody else's controller whenever this program holds that number. A chain that
			// breaks before it reaches the device the bus made has lost its number, and is never ours.
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var current = device;
			while (true)
			{
				// The bus that makes every controller is not one of them. Its own name ends in a number
				// too, and reading that would claim every controller on the bus for whoever holds that number.
				if (IsViGEmBus(current))
					return false;
				var parentId = current.ParentDeviceId;
				if (string.IsNullOrEmpty(parentId) || !seen.Add(parentId))
					return false;
				DeviceInfo parent;
				if (!byId.TryGetValue(parentId, out parent))
					return false;
				if (IsViGEmBus(parent))
					return serials.Contains(TrailingNumber(current.DeviceId));
				current = parent;
			}
		}

		/// <summary>The number Windows put at the end of a device's name, or zero.</summary>
		/// <remarks>
		/// Windows writes the bus number in ordinary digits, after whichever of a backslash and an
		/// ampersand comes last: the current bus names its first controller
		/// <c>USB\VID_045E&amp;PID_028E\01</c>, older ones end in <c>&amp;01</c>. Read after the ampersand
		/// alone, the current name gives <c>PID_028E\01</c>, which is no number, so the program did not
		/// recognise its own controller and offered to remove it on every start.
		/// </remarks>
		/// <param name="deviceId">Full device name, whose last part is read.</param>
		public static uint TrailingNumber(string deviceId)
		{
			if (string.IsNullOrEmpty(deviceId))
				return 0;
			var at = Math.Max(deviceId.LastIndexOf('&'), deviceId.LastIndexOf('\\'));
			if (at < 0 || at + 1 >= deviceId.Length)
				return 0;
			uint value;
			return uint.TryParse(deviceId.Substring(at + 1), out value) ? value : 0;
		}

		/// <summary>The bus numbers of the controllers this program is holding, or has held.</summary>
		/// <remarks>
		/// Both, because a controller is ours before the bus reports it connected and stays ours while
		/// Windows is still removing it after we let go. Asking only what is connected right now names
		/// our own controller a stranger's leftover for as long as those moments last, which is exactly
		/// when the list is read: switching emulation on or off is what makes the list be read again.
		/// </remarks>
		private static List<uint> OurSerials()
		{
			var serials = new List<uint>(Nefarius.ViGEm.Client.ViGEmClient.UsedSerials);
			try
			{
				var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
				var targets = client == null ? null : client.Targets;
				if (targets == null)
					return serials;
				for (uint i = 1; i <= targets.Length; i++)
				{
					var target = targets[i - 1];
					if (target == null || !client.IsControllerConnected(i))
						continue;
					var serial = target.Serial;
					if (serial != 0)
						serials.Add(serial);
				}
			}
			catch (Exception)
			{
				// The ones already recorded are still ours; only what the bus was asked is unknown.
			}
			return serials;
		}

		/// <summary>Devices arranged for walking upwards, since a walk asks for a parent by name.</summary>
		public static Dictionary<string, DeviceInfo> IndexById(IEnumerable<DeviceInfo> devices)
		{
			var byId = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
			if (devices == null)
				return byId;
			foreach (var device in devices)
				if (!string.IsNullOrEmpty(device.DeviceId))
					byId[device.DeviceId] = device;
			return byId;
		}

		/// <summary>
		/// True when a device is one of the pads this program feeds, rather than something a player holds.
		/// </summary>
		/// <param name="device">Device to judge.</param>
		/// <param name="byId">Every known device, from <see cref="IndexById"/>.</param>
		/// <remarks>
		/// A pad the bus is still holding descends from the bus, and walking up to it says so. A pad left
		/// behind by a run that ended badly does not: the node its chain points at has gone, so the walk
		/// arrives nowhere. That broken chain is itself the answer, because real hardware hangs off a real
		/// bus and always reaches the top of the tree.
		///
		/// A broken chain only counts against a device that carries the XInput marker, which is what the
		/// pads this program creates carry. Keeping the rule that narrow means a wheel or a stick with an
		/// odd chain is still shown to its owner, and only the shape this program itself produces can be
		/// judged missing.
		/// </remarks>
		public static bool IsVirtualPad(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			if (device == null || byId == null)
				return false;
			// Read from the identifier as well as the hardware list. A pad created moments ago has an
			// empty hardware list, and reading only that let three freshly leaked pads through while
			// catching the older ones. The identifier carries the marker from the moment the device
			// exists, so it is the dependable half.
			var couldBeOurs = CarriesInputGroup(device.HardwareIds) || CarriesInputGroup(device.DeviceId);
			// The bus is what makes the pads; it is not one of them. Answering otherwise would put the
			// virtual driver itself on a list whose whole purpose is to be deleted, and taking the bus
			// away removes the ability to emulate anything at all.
			if (IsViGEmBus(device))
				return false;
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var current = device;
			while (true)
			{
				var parentId = current.ParentDeviceId;
				// The top of the tree, reached through devices that all exist: real hardware.
				if (string.IsNullOrEmpty(parentId))
					return false;
				DeviceInfo parent;
				if (!byId.TryGetValue(parentId, out parent))
					// The chain ends at a device that is not there. For one of our pads that means it
					// was left behind; for anything else it means nothing, and it is left alone.
					return couldBeOurs;
				// Descended from the bus, so the bus made it, so it is ours.
				if (IsViGEmBus(parent))
					return true;
				// A chain that returns somewhere it has already been is not a chain. Stop, rather than
				// walk it forever and take the window down with it.
				if (!seen.Add(parentId))
					return couldBeOurs;
				current = parent;
			}
		}

		/// <summary>
		/// The same judgement applied to a device already written down, from what was written down.
		/// </summary>
		/// <remarks>
		/// A scan only looks at devices it happens to enumerate on that pass, so a leftover that is not
		/// enumerated stays in the list for ever, marked offline but never taken out. This reads the
		/// identifiers already stored against the device and puts them through the same rule, so a list
		/// is cleaned up whether or not the device turned up again.
		/// </remarks>
		public static bool IsVirtualPad(x360ce.Engine.Data.UserDevice device, Dictionary<string, DeviceInfo> byId)
		{
			if (device == null)
				return false;
			return IsVirtualPad(Described(device.HidDeviceId, device.HidParentDeviceId, device.HidHardwareIds), byId)
				|| IsVirtualPad(Described(device.DevDeviceId, device.DevParentDeviceId, device.DevHardwareIds), byId);
		}

		private static DeviceInfo Described(string deviceId, string parentId, string hardwareIds)
		{
			return string.IsNullOrEmpty(deviceId)
				? null
				: new DeviceInfo { DeviceId = deviceId, ParentDeviceId = parentId, HardwareIds = hardwareIds };
		}

		/// <summary>True when a device is the virtual bus itself.</summary>
		public static bool IsViGEmBus(DeviceInfo device)
		{
			return device != null
				&& ViGEmBusHardwareIds.Any(x => string.Compare(device.HardwareIds, x, true) == 0);
		}

		/// <summary>
		/// True when an identifier carries the XInput marker, which is how Microsoft documents telling an
		/// XInput device from an ordinary one.
		/// </summary>
		public static bool CarriesInputGroup(string hardwareIds)
		{
			return !string.IsNullOrEmpty(hardwareIds)
				&& hardwareIds.IndexOf("IG_", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		/// <summary>
		/// The controller family on the machine and nothing else: every XInput face, every pad of the
		/// kind the bus makes, every root system device (where the bus lives), and the ancestors of
		/// each up to the root.
		/// </summary>
		/// <remarks>
		/// Everything asked about controllers walks from a face up to the bus, so this is the whole of
		/// what those questions ever look up. It used to be answered by reading every device on the
		/// machine: seven hundred nodes at a millisecond each, on every arrival and removal, and under
		/// a lock the device list read had to wait for. The ids of every device are cheap; the
		/// descriptions of the few that matter are read afterwards.
		/// </remarks>
		/// <param name="includeRecords">
		/// Also the records Windows keeps of controllers no longer present. A run that ends without
		/// unplugging its controllers leaves one record each, for ever; they take no place, but they
		/// fill Device Manager and every read of the machine walks them.
		/// </param>
		public static DeviceInfo[] ReadControllerTree(bool includeRecords = false)
		{
			return DeviceDetector.GetDevices(ControllerFamilyIds(includeRecords), true, !includeRecords);
		}

		/// <summary>The ids of the controller family, sorted: every XInput face, every pad of the bus's kind and every root system device.</summary>
		static string[] ControllerFamilyIds(bool includeRecords)
		{
			return DeviceDetector.GetDeviceIds(!includeRecords).Where(id =>
				CarriesInputGroup(id)
				|| id.StartsWith("USB\\VID_045E&PID_028E", StringComparison.OrdinalIgnoreCase)
				|| id.StartsWith("ROOT\\SYSTEM", StringComparison.OrdinalIgnoreCase))
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		/// <summary>
		/// Removes pads left behind by earlier runs.
		/// </summary>
		/// <param name="rebootNeeded">True when Windows asked for a restart to finish the work.</param>
		/// <returns>How many were removed.</returns>
		/// <remarks>
		/// Windows reports needing a restart as a failure code even though the device has gone. Reading
		/// it as a failure is why a clean-up can look as though it did nothing while emptying the list.
		/// </remarks>
		/// <summary>Controllers this program made that Windows never finished building.</summary>
		/// <remarks>
		/// A working controller is two devices: the one the bus makes, and the part underneath it that
		/// XInput reads, which carries the input-group marker. Windows sometimes builds the first and
		/// never the second. What is left reports no problem, sits in Device Manager looking healthy,
		/// and is useless: absent from Windows' own Game Controllers list, invisible to every game.
		///
		/// Nothing else notices. The bus is asked whether it accepted the controller and says yes, so
		/// this compares what the bus made against what Windows finished, which is the only way to see
		/// the difference.
		/// </remarks>
		/// <summary>True once Windows has said a removal can only finish at the next restart.</summary>
		public static bool RestartNeededToFinishRemoval;

		/// <summary>Controllers held at the last look, so the same question is not asked twice.</summary>
		/// <remarks>
		/// Answering means reading every device on the machine, which takes about a second. The check
		/// behind this runs on a timer, so answering afresh each time would spend a second of the
		/// machine every few seconds for an answer that only changes when a controller is made or let
		/// go of. Which controllers are held is free to ask, so that is asked instead, and the
		/// expensive question only when it has changed.
		/// </remarks>
		static string LastJudgedSerials;
		static DeviceInfo[] LastUnfinished = new DeviceInfo[0];

		public static DeviceInfo[] GetUnfinishedVirtualPads()
		{
			var held = string.Join(",", OurSerials().OrderBy(x => x).Select(x => x.ToString()).ToArray());
			if (held == LastJudgedSerials)
				return LastUnfinished;
			LastJudgedSerials = held;
			LastUnfinished = ReadUnfinishedVirtualPads();
			return LastUnfinished;
		}

		static DeviceInfo[] ReadUnfinishedVirtualPads()
		{
			var all = ReadControllerTree();
			var byId = IndexById(all);
			// Only this program's own. Somebody else's half-built controller is not its business.
			var ours = all
				.Where(x => IsVirtualPad(x, byId) && IsOneOfOurs(x, byId))
				.ToArray();
			if (ours.Length == 0)
				return new DeviceInfo[0];
			// A finished one has a descendant carrying the input-group marker.
			var finished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var device in all)
			{
				if (!CarriesInputGroup(device.DeviceId))
					continue;
				var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var current = device;
				while (current != null && !string.IsNullOrEmpty(current.ParentDeviceId) && seen.Add(current.ParentDeviceId))
				{
					finished.Add(current.ParentDeviceId);
					DeviceInfo parent;
					if (!byId.TryGetValue(current.ParentDeviceId, out parent))
						break;
					current = parent;
				}
			}
			return ours
				.Where(x => !finished.Contains(x.DeviceId) && !CarriesInputGroup(x.DeviceId))
				.OrderBy(x => x.DeviceId)
				.ToArray();
		}

		/// <summary>How long removing or repairing waits for the XInput library to be free before giving up.</summary>
		/// <remarks>
		/// The time XInput has to answer. A read holds the library for microseconds and a load for milliseconds, so a
		/// hold past this is a read that is not coming back.
		/// </remarks>
		static readonly TimeSpan ReleaseLimit = TimeSpan.FromMilliseconds(DInputHelper.XiAnswerMs);

		/// <summary>How long removing or repairing waits for the pass under way, and then a plug under way, to finish.</summary>
		/// <remarks>
		/// A plug that XInput answers ends within about seven seconds: five waiting for its place, and two device tree reads.
		/// One still running after this is waiting for XInput that is not answering. Removing and repairing run on a
		/// worker, never on the interface thread, so nothing waits for this but them.
		/// </remarks>
		static readonly TimeSpan StopLimit = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Removes the leftover controllers, through an Administrator copy of this program when this
		/// one is not, and says what happened in words. Never on the interface thread: Windows asks
		/// this program's window whether each controller may go, and a window whose thread is waiting
		/// here cannot answer, so the removal and the program wait on each other for ever.
		/// </summary>
		/// <param name="expected">How many were there before, for the report.</param>
		/// <param name="succeeded">Whether anything was removed, or nothing was there to remove.</param>
		public static string RemoveLeftoverPadsElevated(int expected, out bool succeeded)
		{
			// Let go of the controllers first. Windows refuses to remove a device anything still holds
			// open, and this program holds all four places open while it reads their states; without
			// this the removal is refused and each refusal leaves Windows needing a restart before it
			// will finish building any new controller.
			var helper = Global.DHelper;
			try
			{
				if (helper != null && !helper.ReleaseForDeviceRemoval(StopLimit, ReleaseLimit))
				{
					succeeded = false;
					return "The controllers could not be let go of in time, so nothing was removed. Try again in a moment.";
				}
				if (Program.RunElevated(AdminCommand.RemoveLeftoverPads))
				{
					// Already running as Administrator, so the work happened in this program and the
					// outcome is known exactly.
					bool rebootNeeded;
					Exception error;
					var removed = RemoveLeftoverVirtualPads(out rebootNeeded, out error);
					succeeded = error == null;
					var result = string.Format("Removed {0} of {1}.", removed, expected);
					if (rebootNeeded)
						result += "\r\n\r\nRestart Windows to finish removing them.";
					if (error != null)
						result += "\r\n\r\nThe last one that could not be removed reported: " + error.Message;
					return result;
				}
				// Windows refuses while anything holds the controller open, and its own shell does, so
				// this is a normal answer rather than a fault. Kept, because the only thing that
				// finishes the removal is a restart, and nobody would otherwise know to do one.
				if (Program.LastAdminResult == Program.AdminResult.RestartNeeded)
					RestartNeededToFinishRemoval = true;
				// The elevated copy runs on its own, so the count is taken again once it has finished.
				var left = GetLeftoverVirtualPads().Length;
				var gone = expected - left;
				succeeded = gone > 0 || left == 0;
				var text = gone > 0
					? string.Format("Removed {0} of {1}.", gone, expected)
					: "Nothing was removed. The request to run as Administrator may have been refused.";
				if (left > 0 && (gone > 0 || Program.LastAdminResult == Program.AdminResult.RestartNeeded))
					text += "\r\n\r\nRestart Windows to finish removing the rest.";
				return text;
			}
			finally
			{
				// Picked back up whatever happened, or the program is left feeding nothing.
				if (helper != null)
					helper.ResumeAfterDeviceRemoval();
				XInputPlaces.Invalidate();
			}
		}

		/// <summary>Removes every device of each leftover controller, each before its parent.</summary>
		/// <returns>How many controllers were removed whole, counted as <see cref="GetLeftoverVirtualPads"/> counts them.</returns>
		public static int RemoveLeftoverVirtualPads(out bool rebootNeeded, out Exception error)
		{
			rebootNeeded = false;
			error = null;
			var removed = 0;
			// Read now, with the records, as the leftovers are read; see LeftoverFamiliesOf for why every device goes.
			foreach (var family in LeftoverFamiliesOf(ReadControllerTree(true), OurSerials()))
			{
				var whole = true;
				foreach (var device in family)
				{
					bool restart;
					var failure = DeviceDetector.RemoveDevice(device.DeviceId, 1, out restart);
					if (failure != null)
					{
						error = failure;
						whole = false;
						continue;
					}
					rebootNeeded |= restart;
				}
				if (whole)
					removed++;
			}
			return removed;
		}

		#endregion

		#region Driver state

		/// <summary>The driver supplied for Windows 10 and later.</summary>
		/// <remarks>
		/// The last one its authors made. The project is finished and archived, so this is where that
		/// driver stops - there will not be a newer one to move to.
		///
		/// It arrives as the signed setup its authors publish, rather than as loose driver files. From
		/// version 1.17 they stopped shipping the files, and the setup is what carries the signature.
		/// </remarks>
		public static readonly Version ModernViGEmBusVersion = new Version(1, 21, 442, 0);

		/// <summary>The driver supplied for Windows before 10.</summary>
		/// <remarks>
		/// Kept because the newer driver was never made for those versions of Windows: from 1.17 onwards
		/// it is signed for Windows 10 and later only, and older Windows refuses it. This one is four
		/// years older and is the last that works there.
		/// </remarks>
		public static readonly Version LegacyViGEmBusVersion = new Version(1, 16, 112, 0);

		/// <summary>Whether this Windows takes the newer driver.</summary>
		public static bool TakesModernViGEmBus
		{
			get
			{
				return JocysCom.ClassLibrary.Controls.IssuesControl.IssueHelper
					.GetRealOSVersion().Major >= 10;
			}
		}

		/// <summary>Version of the ViGEmBus driver package supplied for this computer.</summary>
		public static Version EmbeddedViGEmBusVersion
		{
			get { return TakesModernViGEmBus ? ModernViGEmBusVersion : LegacyViGEmBusVersion; }
		}

		/// <summary>The setup for the newer driver, once unpacked.</summary>
		public static string GetModernSetupPath()
		{
			return System.IO.Path.Combine(GetViGEmBusPath(),
				"Win10Setup", "ViGEmBus_1.21.442_x64_x86_arm64.exe");
		}

		/// <summary>Setup class of HID devices. HID Guardian registers as its upper filter.</summary>
		const string HidClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{745a17a0-74d3-11d0-b6fe-00a0c90f57da}";
		const string HidGuardianServiceName = "HidGuardian";

		/// <summary>Installed ViGEmBus driver version, or null when the bus is not present.</summary>
		public static Version GetInstalledViGEmBusVersion()
		{
			var info = GetViGemBusDriverInfo();
			return info.DriverVersion == 0 ? null : info.GetVersion();
		}

		/// <summary>True when the HID Guardian device is present.</summary>
		public static bool IsHidGuardianDevicePresent()
			=> GetHidGuardianDriverInfo().DriverVersion != 0;

		/// <summary>True when HID Guardian is listed as an upper filter of the HID device class.</summary>
		/// <remarks>
		/// This entry is what makes HID Guardian dangerous. While it names a service whose
		/// driver is not installed, HID devices fail to start, which can leave the machine
		/// without a working keyboard and mouse. Removal must always clear this value
		/// before the driver itself is removed.
		/// </remarks>
		public static bool IsHidGuardianClassFilterPresent()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(HidClassKey))
			{
				var values = key?.GetValue("UpperFilters") as string[];
				return values != null && values.Any(x =>
					string.Equals(x, HidGuardianServiceName, StringComparison.OrdinalIgnoreCase));
			}
		}

		/// <summary>Run a driver command and report whether it succeeded.</summary>
		/// <remarks>
		/// Hardware identifiers are always passed in full. Wildcards are refused, because a
		/// pattern can match devices other than the one being removed.
		/// devcon returns 0 on success and 1 when a reboot is required.
		/// </remarks>
		static bool RunDevCon(string folder, string arguments, ProcessWindowStyle style)
		{
			if (arguments.Contains("*") || arguments.Contains("?"))
				throw new ArgumentException("Wildcards are not allowed in driver commands.", nameof(arguments));
			var exePath = Path.Combine(folder, GetDevConPath());
			if (!File.Exists(exePath))
				return false;
			var exitCode = UacHelper.RunElevated(exePath, arguments, style, true);
			return exitCode == 0 || exitCode == 1;
		}

		#endregion

		/// <summary>
		/// Install Virtual driver.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		/// <summary>
		/// Which driver command adds a bus and which one changes the bus already there.
		/// </summary>
		/// <param name="busCount">How many buses are on the computer now.</param>
		/// <remarks>
		/// Named and separated because the difference is invisible from the outside and costs nothing
		/// until it has been paid many times over. "install" always makes a new bus and never looks at
		/// what exists, so a computer asked to install ten times ends up with ten buses, none of which
		/// looks wrong on its own.
		/// </remarks>
		public static string GetInstallCommand(int busCount)
		{
			return busCount > 0 ? "update" : "install";
		}

		/// <summary>Every virtual bus currently on this computer.</summary>
		/// <remarks>
		/// There is meant to be exactly one. More than one is the result of installing over an
		/// existing bus, which is a thing this program used to do every time it was asked to install.
		/// </remarks>
		public static DeviceInfo[] GetViGEmBusInstances()
		{
			return ReadControllerTree()
				.Where(IsViGEmBus)
				.ToArray();
		}

		/// <summary>
		/// The driver package for the version of Windows this is running on, as a path inside the
		/// folder the files are unpacked into.
		/// </summary>
		/// <remarks>
		/// The two packages hold the same driver and differ in how they are signed, which is what
		/// decides whether Windows will accept them. Windows 10 and later take the one signed for
		/// Windows 10; everything older takes the other.
		/// </remarks>
		static string GetViGEmBusInfPath()
		{
			var forWindows10 = JocysCom.ClassLibrary.Controls.IssuesControl.IssueHelper
				.GetRealOSVersion().Major >= 10;
			return (forWindows10 ? "Win10" : "WinVS") + "\\ViGEmBus.inf";
		}

		/// <summary>
		/// Installs the virtual bus driver, or updates the one already there.
		/// </summary>
		/// <returns>True when a bus is present afterwards.</returns>
		/// <remarks>
		/// Must be executed in administrative mode.
		///
		/// Installing and updating are different commands and picking the wrong one is what left
		/// this computer with more than one bus. "install" makes a new bus every time it is run and
		/// never looks at what is already there, so asking twice leaves two, asking ten times leaves
		/// ten. "update" changes the driver on the bus that exists and makes nothing new.
		///
		/// So a bus is made only when there is none, and from then on it is updated in place.
		/// <summary>Runs the setup its authors publish, and waits for it.</summary>
		/// <remarks>
		/// Waited for, so what is reported afterwards is what actually happened rather than what was
		/// asked for. The setup is the only thing that knows how to put this driver on: it carries the
		/// signature, registers the product so Apps and Features can remove it, and upgrades an older
		/// one in place.
		/// </remarks>
		static bool RunModernSetup()
		{
			// A package that could not be unpacked is treated like a setup that would not start: not run,
			// and the caller reports the driver it then finds.
			if (!ExtractViGemBusFiles())
				return false;
			var setup = GetModernSetupPath();
			if (!System.IO.File.Exists(setup))
				return false;
			var info = new System.Diagnostics.ProcessStartInfo(setup)
			{
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Normal,
			};
			try
			{
				using (var process = System.Diagnostics.Process.Start(info))
				{
					if (process != null)
						process.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
			return true;
		}

		/// </remarks>
		public static bool InstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Windows 10 and later take the newer driver, which its authors publish only as a signed setup.
			// It is run rather than unpacked, because the signature is on the setup: taking the files out of
			// it and installing them by hand throws away the one thing that makes Windows trust the driver.
			//
			// Shown rather than hidden. Its authors document no way to run it silently, and a setup driven
			// with switches nobody has written down is a setup that can quietly do nothing - the failure a
			// person then reports as "the button does not work". Seen, it either finishes or says why.
			if (TakesModernViGEmBus)
				return RunModernSetup() && GetInstalledViGEmBusVersion() != null;
			// Extract files first.
			if (!ExtractViGemBusFiles())
				return false;
			var folder = GetViGEmBusPath();
			var infFile = GetViGEmBusInfPath();
			// Use last ID.
			var hardwareId = ViGEmBusHardwareIds.Last();
			var command = GetInstallCommand(GetViGEmBusInstances().Length);
			RunDevCon(folder, command + " " + infFile + " " + hardwareId, style);
			// Report the state that was actually reached, not the command result.
			return GetViGEmBusInstances().Any();
		}

		/// <summary>
		/// Removes the virtual bus and puts it back, which is what recovers one that has stopped
		/// working.
		/// </summary>
		/// <returns>True when a working bus is present afterwards.</returns>
		/// <remarks>
		/// Must be executed in administrative mode.
		///
		/// A bus can reach a state where it still answers, still reports itself healthy, and still
		/// accepts a controller being plugged in, yet never brings that controller up. Nothing about
		/// it looks wrong from outside, so there is nothing to detect and nothing to repair in place.
		/// Taking it away and putting it back is what clears it.
		/// </remarks>
		public static bool RepairViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// The newer driver is repaired by its own setup, which offers exactly that when it finds one
			// already there. Taking it away and putting it back the old way would leave Apps and Features
			// pointing at something that is gone.
			if (TakesModernViGEmBus)
				return RunModernSetup() && GetInstalledViGEmBusVersion() != null;
			UninstallViGEmBus(style);
			return InstallViGEmBus(style);
		}

		/// <summary>
		/// Repairs the virtual bus through an Administrator copy of this program, letting go of every
		/// controller first and picking them back up afterwards. Never on the interface thread.
		/// </summary>
		/// <remarks>
		/// The bus cannot be taken out from under controllers this program still holds open, so they
		/// are let go of exactly as removing them does, and picked up again whatever happened. What the
		/// repair achieved is not reported here: on Windows 10 and later the driver's own setup decides,
		/// and on older Windows a bus of another version is left for its own installer. The checks that
		/// follow read the bus afresh.
		/// </remarks>
		public static void RepairViGEmBusElevated()
		{
			var helper = Global.DHelper;
			try
			{
				// Not repaired while the controllers cannot be let go of: XInput not answering, which the window has already
				// been told, or the pass under way not finishing.
				if (helper != null && !helper.ReleaseForDeviceRemoval(StopLimit, ReleaseLimit))
					return;
				Program.RunElevated(AdminCommand.RepairViGEmBus);
			}
			finally
			{
				if (helper != null)
					helper.ResumeAfterDeviceRemoval();
			}
		}

		/// <summary>
		/// Uninstall the virtual bus driver installed by this application.
		/// </summary>
		/// <returns>True when the bus is no longer present.</returns>
		/// <remarks>
		/// Must be executed in administrative mode.
		/// Only a bus matching the driver package embedded here is removed. Any other
		/// version was put there by something else, most often the official ViGEmBus
		/// setup, and must be removed through that installer so its own records stay
		/// consistent. ViGEmBus is shared with other applications, so removing one that
		/// this application did not install would break them without warning.
		/// </remarks>
		public static bool UninstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Put on by its own setup, which registered the product with Windows. Removing the device by
			// hand would leave Apps and Features offering to remove a driver that is no longer there, so it
			// is taken off the same way it was put on.
			if (TakesModernViGEmBus)
				return RunModernSetup() && GetInstalledViGEmBusVersion() == null;
			var installed = GetInstalledViGEmBusVersion();
			// Nothing to remove.
			if (installed == null)
				return true;
			if (!Equals(installed, EmbeddedViGEmBusVersion))
				return false;
			// Extract files first.
			if (!ExtractViGemBusFiles())
				return false;
			var folder = GetViGEmBusPath();
			// Remove all old instances.
			foreach (var ViGEmBusHardwareId in ViGEmBusHardwareIds)
				RunDevCon(folder, "remove " + ViGEmBusHardwareId, style);
			// Whatever is still there is removed one at a time. Removing by hardware identifier only
			// reaches a bus that still answers to one, and a computer that has had a bus installed
			// over an existing one can be left holding a node that no longer does. Leaving even one
			// behind means the next install updates that one instead of making a working bus.
			foreach (var bus in GetViGEmBusInstances())
			{
				bool restart;
				DeviceDetector.RemoveDevice(bus.DeviceId, 1, out restart);
			}
			// Report the state that was actually reached, not the command result.
			return !GetViGEmBusInstances().Any();
		}

		#endregion

		#region Install/Uninstall HidGuardian

		public static string GetHidGuardianPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return Path.Combine(baseDirectory, "Program Files", "ViGEm HidGuardian");
		}

		static bool ExtractHidGuardianFiles()
		{
			var target = GetHidGuardianPath();
			return ExtractViGemFiles("HidGuardian", target);
		}

		/// <summary>
		/// Uninstall HID Guardian.
		/// </summary>
		/// <returns>True when neither the class filter nor the device remain.</returns>
		/// <remarks>
		/// Must be executed in administrative mode.
		/// The order is deliberate. The HIDClass upper filter is removed first and the
		/// removal is verified before the driver is touched, because a filter entry that
		/// names a missing driver stops HID devices from starting and can leave the
		/// machine without a working keyboard and mouse. If the filter cannot be removed
		/// the driver is left in place, which keeps the system in a working state.
		/// </remarks>
		public static bool UninstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			if (!ExtractHidGuardianFiles())
				return false;
			var folder = GetHidGuardianPath();
			// Step 1: remove the HID class filter, then confirm it is gone.
			if (IsHidGuardianClassFilterPresent())
			{
				RunDevCon(folder, "classfilter HIDClass upper !" + HidGuardianServiceName, style);
				if (IsHidGuardianClassFilterPresent())
					return false;
			}
			// Step 2: only now remove the device, then confirm it is gone.
			if (IsHidGuardianDevicePresent())
			{
				RunDevCon(folder, "remove " + HidGuardianHardwareId, style);
				if (IsHidGuardianDevicePresent())
					return false;
			}
			return true;
		}

#if DEBUG

		/// <summary>
		/// Install HID Guardian. Available in development builds only.
		/// </summary>
		/// <returns>True when both the device and the class filter are present.</returns>
		/// <remarks>
		/// Must be executed in administrative mode.
		/// Not compiled into release builds: a misconfigured HID filter driver can lock the
		/// user out of keyboard and mouse, and recovery needs safe mode and a registry edit.
		/// It exists so the uninstall path can be exercised during development.
		/// The order mirrors the uninstall. The driver is installed first and verified, so
		/// the class filter never names a service that is not there yet.
		/// </remarks>
		public static bool InstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			if (!ExtractHidGuardianFiles())
				return false;
			var folder = GetHidGuardianPath();
			var paString = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			var infFile = string.Format("{0}\\{1}", paString, "HidGuardian.inf");
			// Step 1: install the driver, then confirm it is present.
			if (!IsHidGuardianDevicePresent())
			{
				RunDevCon(folder, "install " + infFile + " " + HidGuardianHardwareId, style);
				if (!IsHidGuardianDevicePresent())
					return false;
			}
			// Step 2: only now add the class filter.
			if (!IsHidGuardianClassFilterPresent())
			{
				RunDevCon(folder, "classfilter HIDClass upper -" + HidGuardianServiceName, style);
				if (!IsHidGuardianClassFilterPresent())
					return false;
			}
			return true;
		}

#endif

		/// <summary>
		/// Extract the bundled script which removes every HID Guardian registry entry.
		/// </summary>
		/// <returns>Full path of the script, or null when it could not be extracted.</returns>
		/// <remarks>
		/// Recovery path for a machine where the HID class filter still names HID Guardian
		/// after the driver is gone. Run it from a command prompt with administrative
		/// rights, in safe mode when input devices no longer work.
		/// </remarks>
		public static string GetHidGuardianRemoveScript()
		{
			if (!ExtractHidGuardianFiles())
				return null;
			var path = Path.Combine(GetHidGuardianPath(), "HidGuardian_Remove.ps1");
			return File.Exists(path) ? path : null;
		}


		/// <summary>
		/// Must bve used to uninstall device when this app is 32-bit, but runs on 64-bit windows.
		/// This is because SetupDiCallClassInstaller throws ERROR_IN_WOW64 (ex.ErrorCode = 0xE0000235)
		/// when application architecture do not match OS architecture.
		/// </summary>
		/// <param name="deviceId">
		/// Device Hardware ID ("HID\VID_046D&PID_C219") or
		/// Device Instance ID prefixed with '@' (@"HID\VID_046D&PID_C219\7&29C26453&0&0000").
		/// </param>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UnInstallDevice(string deviceId, ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			if (!ExtractHidGuardianFiles())
				return;
			var folder = GetHidGuardianPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			UacHelper.RunElevated(
				exePath,
				"remove \"" + deviceId + "\"",
				style, true);
			// Make sure that device is re-inserted.
			DeviceDetector.ScanForHardwareChanges();
		}

		#endregion

		#region Extract Helper

		/// <summary>
		/// Extract resource files
		/// </summary>
		/// <param name="source">Resource prefix.</param>
		/// <param name="target">Target folder to extract.</param>
		/// <returns>
		/// True when the folder holds the package. False when it could not be unpacked, because a file that
		/// has to change is in use or may not be written; the failure is logged and the caller stops.
		/// </returns>
		static bool ExtractViGemFiles(string source, string target)
		{
			// Get list of resources to extract.
			var assembly = Assembly.GetEntryAssembly();
			var pattern = string.Format(".Resources.{0}.zip", source);
			var resourceName = assembly.GetManifestResourceNames().Where(x => x.Contains(pattern)).First();
			using (var sr = assembly.GetManifestResourceStream(resourceName))
			{
				if (sr == null)
					return false;
				try
				{
					ExtractZip(sr, target);
				}
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
				{
					// A file of the package that has to change is in use, or may not be written.
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					return false;
				}
			}
			return true;
		}

		/// <summary>Unpacks a driver package into a folder, writing only the files that differ from what is there.</summary>
		/// <remarks>
		/// The package goes to the same folder every time, and a file from the last time can still be in
		/// use: the driver's own setup, still open or still closing, or a virus scanner reading what was
		/// just written. Windows refuses to write over a file in use (0x80070020), and the refusal ends the
		/// install, repair or removal it is part of. A file that already holds the packed bytes is left as
		/// it is, so only a file that really has to change can be refused, and it still is.
		/// </remarks>
		/// <param name="package">The zip package. Left open.</param>
		/// <param name="target">The folder to unpack into.</param>
		/// <exception cref="IOException">A file that has to change is in use or cannot be written.</exception>
		/// <exception cref="UnauthorizedAccessException">A file that has to change may not be written.</exception>
		public static void ExtractZip(Stream package, string target)
		{
			using (var zip = ZipStorer.Open(package, FileAccess.Read, true))
			{
				// The folders first. A package holds folders as well as files, and a folder is not something
				// to write bytes into: unpacking one as though it were a file failed on any computer where the
				// destination did not already exist, which is every computer installing the driver for the
				// first time.
				Directory.CreateDirectory(target);
				foreach (var entry in zip.ReadCentralDir())
				{
					var relative = entry.FilenameInZip.Replace("/", "\\");
					var fileName = Path.Combine(target, relative);
					if (relative.EndsWith("\\"))
					{
						Directory.CreateDirectory(fileName.TrimEnd('\\'));
						continue;
					}
					var folder = Path.GetDirectoryName(fileName);
					if (!string.IsNullOrEmpty(folder))
						Directory.CreateDirectory(folder);
					var existing = new FileInfo(fileName);
					byte[] packed;
					if (existing.Exists && existing.Length == entry.FileSize
						&& zip.ExtractFile(entry, out packed) && HasContent(fileName, packed))
						continue;
					zip.ExtractFile(entry, fileName);
				}
			}
		}

		/// <summary>Whether a file is there and holds exactly these bytes. Read beside whatever else has it open.</summary>
		static bool HasContent(string path, byte[] bytes)
		{
			var file = new FileInfo(path);
			if (!file.Exists || file.Length != bytes.Length)
				return false;
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				var existing = new byte[bytes.Length];
				var read = 0;
				while (read < existing.Length)
				{
					var count = stream.Read(existing, read, existing.Length - read);
					if (count == 0)
						return false;
					read += count;
				}
				for (var i = 0; i < bytes.Length; i++)
					if (existing[i] != bytes[i])
						return false;
				return true;
			}
		}

		static string GetDevConPath()
		{
			var paString = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			return string.Format("devcon.{0}.exe", paString);
		}

		#endregion
		#region HidHide

		// HidHide is the maintained successor to HID Guardian, which its author archived in 2023.
		// It ships as its own signed installer and carries its own configuration program, so this
		// application only detects it and opens its tools; it never installs or configures it.

		/// <summary>Root device the HidHide driver installs under.</summary>
		public const string HidHideHardwareId = "Root\\HidHide";

		/// <summary>Where the driver package is published.</summary>
		public const string HidHideDownloadUrl = "https://github.com/nefarius/HidHide/releases/latest";

		/// <summary>True when the HidHide driver is present on this machine.</summary>
		public static bool IsHidHideDevicePresent()
		{
			var driver = DeviceDetector.GetDrivers(DEVCLASS.SYSTEM, DIGCF.DIGCF_PRESENT,
				SPDIT.SPDIT_COMPATDRIVER, null, HidHideHardwareId).FirstOrDefault();
			return driver.DriverVersion != 0;
		}

		/// <summary>Registry key HidHide's setup writes its version and its folder to.</summary>
		const string HidHideRegistryKey = @"SOFTWARE\Nefarius Software Solutions e.U.\HidHide";

		/// <summary>Installed version, or null when HidHide is not installed.</summary>
		public static string GetHidHideVersion()
		{
			foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
			{
				using (var key = root.OpenSubKey(HidHideRegistryKey))
				{
					var value = key?.GetValue("Version") as string;
					if (!string.IsNullOrEmpty(value))
						return value;
				}
			}
			return null;
		}

		/// <summary>
		/// Full path of the HidHide configuration program, or null when it cannot be found.
		/// </summary>
		public static string GetHidHideClientPath()
		{
			return GetHidHideProgramPath("HidHideClient.exe");
		}

		/// <summary>Full path of one of HidHide's programs, or null when it cannot be found.</summary>
		/// <remarks>
		/// The install location is read from the registry where possible, because the setup lets
		/// the user choose it. The usual folder is only a fallback for when that key is missing.
		/// </remarks>
		static string GetHidHideProgramPath(string fileName)
		{
			foreach (var folder in GetHidHideFolders())
			{
				if (string.IsNullOrEmpty(folder))
					continue;
				// The setup places the programs in an architecture sub folder.
				foreach (var relative in new[] { fileName, Path.Combine("x64", fileName) })
				{
					var path = Path.Combine(folder, relative);
					if (File.Exists(path))
						return path;
				}
			}
			return null;
		}

		/// <summary>Folders HidHide may be installed in, most likely first.</summary>
		/// <remarks>
		/// HidHide 1.5 records its folder as "Path" beside its version. The longer key name is read as
		/// well, then the default folder.
		/// </remarks>
		static string[] GetHidHideFolders()
		{
			var folders = new List<string>();
			foreach (var name in new[] { HidHideRegistryKey,
				@"SOFTWARE\Nefarius Software Solutions e.U.\Nefarius Software Solutions e.U. HidHide" })
			{
				using (var key = Registry.LocalMachine.OpenSubKey(name))
					folders.Add(key?.GetValue("Path") as string);
			}
			folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				"Nefarius Software Solutions", "HidHide"));
			return folders.ToArray();
		}

		/// <summary>What HidHide is set to do.</summary>
		public sealed class HidHideState
		{
			/// <summary>The HidHide driver is present.</summary>
			public bool Installed;

			/// <summary>Its command line program answered. When false, nothing below is known.</summary>
			public bool Answered;

			/// <summary>Hiding is switched on.</summary>
			public bool CloakOn;

			/// <summary>The application list names the programs that may not see hidden devices, rather than the ones that may.</summary>
			public bool Inverse;

			/// <summary>Device instance paths of the hidden devices.</summary>
			public string[] HiddenDevices = new string[0];

			/// <summary>Full paths of the programs on the application list.</summary>
			public string[] Applications = new string[0];

			/// <summary>When this was read, in universal time.</summary>
			public DateTime ReadTime;

			/// <summary>Whether a device instance path is on the hidden list.</summary>
			/// <remarks>Windows ignores case in device instance paths, so this does too.</remarks>
			public bool IsHidden(string deviceInstancePath)
			{
				if (string.IsNullOrEmpty(deviceInstancePath))
					return false;
				var path = deviceInstancePath.Trim();
				return HiddenDevices.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
			}

			/// <summary>Whether a program may see the devices HidHide hides.</summary>
			/// <param name="fileName">The program's full path.</param>
			public bool IsAllowed(string fileName)
			{
				var listed = !string.IsNullOrEmpty(fileName)
					&& Applications.Any(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
				return Inverse ? !listed : listed;
			}
		}

		/// <summary>Longest wait for HidHide's command line program to answer, in milliseconds.</summary>
		const int HidHideCliTimeout = 3000;

		/// <summary>How long one reading of HidHide's settings is used before it is read again.</summary>
		/// <remarks>
		/// The issue check runs every five seconds and every reading starts a process. The settings change
		/// only when somebody uses HidHide's own program, so a reading a quarter of a minute old is soon enough.
		/// </remarks>
		static readonly TimeSpan HidHideStateLifetime = TimeSpan.FromSeconds(15);

		/// <summary>The last reading. Replaced whole and never changed, so any thread may read it.</summary>
		static HidHideState LastHidHideState;

		/// <summary>What HidHide is set to do, read through its command line program.</summary>
		/// <remarks>
		/// HidHide keeps its settings where only an administrator can read them, but HidHideCLI.exe answers
		/// anybody. Starting a process and waiting up to <see cref="HidHideCliTimeout"/> for it is no work for
		/// the device thread, so only the issue check calls this, and a reading is reused for
		/// <see cref="HidHideStateLifetime"/>. It only reads; it never changes HidHide's settings.
		/// </remarks>
		public static HidHideState GetHidHideState()
		{
			var state = LastHidHideState;
			if (state != null && DateTime.UtcNow - state.ReadTime < HidHideStateLifetime)
				return state;
			var installed = IsHidHideDevicePresent();
			var cli = installed ? GetHidHideProgramPath("HidHideCLI.exe") : null;
			var output = cli == null ? null : RunHidHideCli(cli, "--cloak-state --inv-state --dev-list --app-list");
			state = ParseHidHideCli(output);
			state.Installed = installed;
			state.ReadTime = DateTime.UtcNow;
			LastHidHideState = state;
			return state;
		}

		/// <summary>Runs HidHide's command line program and returns what it printed; null when it failed or did not end in time.</summary>
		static string RunHidHideCli(string fileName, string arguments)
		{
			var psi = new ProcessStartInfo(fileName, arguments);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardOutput = true;
			Process process;
			try
			{
				process = Process.Start(psi);
			}
			catch (System.ComponentModel.Win32Exception)
			{
				// Removed or blocked since it was found: no answer, the same as a program that never replies.
				return null;
			}
			using (process)
			{
				// Read while waiting, or a full output pipe would stop the program from ending.
				var output = process.StandardOutput.ReadToEndAsync();
				if (!process.WaitForExit(HidHideCliTimeout))
				{
					try
					{
						process.Kill();
					}
					catch (InvalidOperationException)
					{
						// It ended between the wait and the kill.
					}
					catch (System.ComponentModel.Win32Exception)
					{
						// It is already ending.
					}
					return null;
				}
				if (process.ExitCode != 0 || !output.Wait(HidHideCliTimeout))
					return null;
				return output.Result;
			}
		}

		/// <summary>
		/// Reads what HidHideCLI.exe printed for <c>--cloak-state --inv-state --dev-list --app-list</c>.
		/// </summary>
		/// <remarks>
		/// Each fact is printed on its own line as the command that would set it again: <c>--cloak-on</c> or
		/// <c>--cloak-off</c>, <c>--inv-on</c> or <c>--inv-off</c>, <c>--dev-hide "device instance path"</c> for
		/// every hidden device and <c>--app-reg "full path"</c> for every program on the list. A bare device
		/// instance path, a line with a backslash and no spaces, is taken as a hidden device too. Nothing counts
		/// as an answer unless the cloak state is among the lines, so an error message is never read as settings.
		/// </remarks>
		/// <param name="output">Everything the program printed; null or empty when it did not answer.</param>
		public static HidHideState ParseHidHideCli(string output)
		{
			var state = new HidHideState();
			if (string.IsNullOrEmpty(output))
				return state;
			var devices = new List<string>();
			var applications = new List<string>();
			foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var line = raw.Trim();
				if (string.Equals(line, "--cloak-on", StringComparison.OrdinalIgnoreCase))
				{
					state.Answered = true;
					state.CloakOn = true;
				}
				else if (string.Equals(line, "--cloak-off", StringComparison.OrdinalIgnoreCase))
					state.Answered = true;
				else if (string.Equals(line, "--inv-on", StringComparison.OrdinalIgnoreCase))
					state.Inverse = true;
				else if (line.StartsWith("--dev-hide ", StringComparison.OrdinalIgnoreCase))
					AddQuoted(devices, line.Substring("--dev-hide ".Length));
				else if (line.StartsWith("--app-reg ", StringComparison.OrdinalIgnoreCase))
					AddQuoted(applications, line.Substring("--app-reg ".Length));
				else if (!line.StartsWith("-") && line.IndexOf('\\') > 0 && !line.Any(char.IsWhiteSpace))
					devices.Add(line);
			}
			state.HiddenDevices = devices.ToArray();
			state.Applications = applications.ToArray();
			return state;
		}

		static void AddQuoted(List<string> list, string value)
		{
			value = value.Trim().Trim('"');
			if (value.Length > 0)
				list.Add(value);
		}

		#endregion

	}
}
