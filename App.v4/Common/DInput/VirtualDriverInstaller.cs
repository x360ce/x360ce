using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using Microsoft.Win32;
using System;
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

		#region Driver state

		/// <summary>Version of the ViGEmBus driver package embedded in this application.</summary>
		public static readonly Version EmbeddedViGEmBusVersion = new Version(1, 16, 112, 0);

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
		public static void InstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractViGemBusFiles(true);
			var folder = GetViGEmBusPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			var osString = JocysCom.ClassLibrary.Controls.IssuesControl.IssueHelper.GetRealOSVersion().Major >= 10
				? "Win10" : "WinVS";
			var infFile = string.Format("{0}\\{1}", osString, "ViGEmBus.inf");
			UacHelper.RunElevated(
				exePath,
				// Use last ID.
				"install " + infFile + " " + ViGEmBusHardwareIds.Last(),
				style, true);
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
			// Report the state that was actually reached, not the command result.
			return GetInstalledViGEmBusVersion() == null;
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
			foreach (ZipStorer.ZipFileEntry entry in dir)
			{
				var fileName = System.IO.Path.Combine(target, entry.FilenameInZip.Replace("/", "\\"));
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

	}
}
