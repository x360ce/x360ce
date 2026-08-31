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

		static void ExtractViGemBusFiles(bool overwrite)
		{
			var target = GetViGEmBusPath();
			ExtractViGemFiles("ViGEmBus", target, overwrite);
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
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
			var byId = IndexById(all);
			return all
				.Where(x => IsVirtualPad(x, byId))
				// Not the ones this program is using right now. Offering to remove those would break
				// the very thing somebody pressing the button is trying to repair.
				.Where(x => !IsOneOfOurs(x, byId))
				// One entry per controller, not per device. A controller is a small family - the thing
				// itself and a face for each way of reading it - so counting devices reported one left
				// behind as three, and named the same controller three times over. Removing the
				// controller takes its faces with it, so the family is represented by the controller.
				//
				// The faces carry the XInput marker and the controller does not, so a face is gathered by
				// walking up to the first thing without it, and the controller is gathered by itself.
				// Walking up from the controller as well would take it to the bus that made it, which is
				// shared by every controller on it - so each one would be filed under its own maker and
				// counted apart from its own faces.
				.GroupBy(x => VirtualDriverInstaller.CarriesInputGroup(x.DeviceId)
					|| VirtualDriverInstaller.CarriesInputGroup(x.HardwareIds)
						? XInputPlaces.HardwareOf(x, byId)
						: x.DeviceId, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.FirstOrDefault(x => string.Equals(x.DeviceId, g.Key, StringComparison.OrdinalIgnoreCase))
					?? g.First())
				.OrderBy(x => x.DeviceId)
				.ToArray();
		}

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
		/// the controller's name, as "&amp;01" for one and "&amp;02" for two. So the numbers of the
		/// controllers this program is holding are asked for directly and matched against the name.
		/// That is a clear reference to its own, rather than a guess from timing.
		///
		/// If the numbers cannot be read, nothing is claimed. Being wrong that way mentions a
		/// controller that need not be mentioned; being wrong the other way offers to remove the one
		/// in use.
		/// </remarks>
		public static bool IsOneOfOurs(DeviceInfo device, Dictionary<string, DeviceInfo> byId)
		{
			if (device == null || byId == null || string.IsNullOrEmpty(device.DeviceId))
				return false;
			var serials = OurSerials();
			if (serials.Count == 0)
				return false;
			// A controller is not one device but a small family: the one the bus creates and the two
			// beneath it that Windows adds. Only the top one carries the number, so the question is
			// asked of the whole line of ancestors. Matching the name alone catches the top and misses
			// the rest, and the ones missed are then reported as somebody's leftovers.
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var current = device;
			while (current != null)
			{
				if (serials.Contains(TrailingNumber(current.DeviceId)))
					return true;
				var parentId = current.ParentDeviceId;
				if (string.IsNullOrEmpty(parentId) || !seen.Add(parentId))
					return false;
				DeviceInfo parent;
				if (!byId.TryGetValue(parentId, out parent))
					return false;
				current = parent;
			}
			return false;
		}

		/// <summary>The number Windows put at the end of a device's name, or zero.</summary>
		/// <remarks>
		/// Windows writes the bus number in ordinary digits. This once compared it against the same
		/// number written in hexadecimal, which agrees only while the number is below ten. A fresh bus
		/// starts there, so it worked; a few rounds of switching emulation on and off carried it past,
		/// and from then on the program did not recognise its own controllers and offered to remove the
		/// one it was using. Reading the number instead of writing it out leaves nothing to disagree.
		/// </remarks>
		/// <param name="deviceId">Full device name, whose last part after an ampersand is read.</param>
		public static uint TrailingNumber(string deviceId)
		{
			if (string.IsNullOrEmpty(deviceId))
				return 0;
			var at = deviceId.LastIndexOf('&');
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
			var all = DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT);
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

		public static int RemoveLeftoverVirtualPads(out bool rebootNeeded, out Exception error)
		{
			rebootNeeded = false;
			error = null;
			var removed = 0;
			foreach (var pad in GetLeftoverVirtualPads())
			{
				bool restart;
				var failure = DeviceDetector.RemoveDevice(pad.DeviceId, 1, out restart);
				if (failure != null)
				{
					error = failure;
					continue;
				}
				removed++;
				rebootNeeded |= restart;
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
			return DeviceDetector.GetDevices(null, DIGCF.DIGCF_ALLCLASSES | DIGCF.DIGCF_PRESENT)
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
			ExtractViGemBusFiles(true);
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
			ExtractViGemBusFiles(true);
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
			ExtractViGemBusFiles(false);
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

		static void ExtractHidGuardianFiles(bool overwrite)
		{
			var target = GetHidGuardianPath();
			ExtractViGemFiles("HidGuardian", target, overwrite);
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
			ExtractHidGuardianFiles(false);
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
			ExtractHidGuardianFiles(true);
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
			ExtractHidGuardianFiles(false);
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
			ExtractHidGuardianFiles(true);
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
		/// <param name="overwrite">Overwrite files at target.</param>
		static void ExtractViGemFiles(string source, string target, bool overwrite)
		{
			// Get list of resources to extract.
			var assembly = Assembly.GetEntryAssembly();
			var pattern = string.Format(".Resources.{0}.zip", source);
			var resourceName = assembly.GetManifestResourceNames().Where(x => x.Contains(pattern)).First();
			var sr = assembly.GetManifestResourceStream(resourceName);
			if (sr == null)
				return;
			var bytes = new byte[sr.Length];
			sr.Read(bytes, 0, bytes.Length);
			// Open an existing zip file for reading.
			var zip = ZipStorer.Open(sr, FileAccess.Read);
			// Read the central directory collection
			var dir = zip.ReadCentralDir();
			// Look for the desired file.
			// The folders first. A package holds folders as well as files, and a folder is not something
			// to write bytes into: unpacking one as though it were a file failed on any computer where the
			// destination did not already exist, which is every computer installing the driver for the
			// first time.
			Directory.CreateDirectory(target);
			foreach (ZipStorer.ZipFileEntry entry in dir)
			{
				var relative = entry.FilenameInZip.Replace("/", "\\");
				var fileName = System.IO.Path.Combine(target, relative);
				if (relative.EndsWith("\\"))
				{
					Directory.CreateDirectory(fileName.TrimEnd('\\'));
					continue;
				}
				var folder = System.IO.Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(folder))
					Directory.CreateDirectory(folder);
				zip.ExtractFile(entry, fileName);
			}
			zip.Close();
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

		/// <summary>Installed version, or null when HidHide is not installed.</summary>
		public static string GetHidHideVersion()
		{
			foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
			{
				using (var key = root.OpenSubKey(@"SOFTWARE\Nefarius Software Solutions e.U.\HidHide"))
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
		/// <remarks>
		/// The install location is read from the registry where possible, because the setup lets
		/// the user choose it. The usual folder is only a fallback for when that key is missing.
		/// </remarks>
		public static string GetHidHideClientPath()
		{
			foreach (var folder in GetHidHideFolders())
			{
				if (string.IsNullOrEmpty(folder))
					continue;
				// The setup places the programs in an architecture sub folder.
				foreach (var relative in new[] { "HidHideClient.exe", @"x64\HidHideClient.exe" })
				{
					var path = Path.Combine(folder, relative);
					if (File.Exists(path))
						return path;
				}
			}
			return null;
		}

		static string[] GetHidHideFolders()
		{
			string fromRegistry = null;
			using (var key = Registry.LocalMachine.OpenSubKey(
				@"SOFTWARE\Nefarius Software Solutions e.U.\Nefarius Software Solutions e.U. HidHide"))
				fromRegistry = key?.GetValue("Path") as string;
			return new[]
			{
				fromRegistry,
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
					"Nefarius Software Solutions", "HidHide"),
			};
		}

		#endregion

	}
}
