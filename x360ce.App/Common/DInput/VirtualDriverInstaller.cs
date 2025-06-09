using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
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

		#region ■ Install/Uninstall ViGEmBus

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

		static void ExtractViGemBusFiles()
		{
			var target = GetViGEmBusPath();
			ExtractViGemFiles("ViGEmBus", target);
		}

		public static string[] ViGEmBusHardwareIds = { "Root\\ViGEmBus", "Nefarius\\ViGEmBus\\Gen1" };
		public const string HidGuardianHardwareId = "Root\\HidGuardian";

		/// <summary>
		/// Install Virtual driver.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractViGemBusFiles();
			var folder = GetViGEmBusPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			var osString = JocysCom.ClassLibrary.Controls.IssuesControl.IssueHelper.GetRealOSVersion().Major >= 10
				? "Win10" : "WinVS";
			var infFile = string.Format("{0}\\{1}", osString, "ViGEmBus.inf");
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				// Use last ID.
				"install " + infFile + " " + ViGEmBusHardwareIds.Last(),
				isElevated: true);
		}

		/// <summary>
		/// UnInstall Virtual driver here.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractViGemBusFiles();
			var folder = GetViGEmBusPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			// Remove all old instances.
			foreach (var ViGEmBusHardwareId in ViGEmBusHardwareIds)
			{
				JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
					exePath,
					"remove " + ViGEmBusHardwareId,
					isElevated: true);
			}
		}

		#endregion

		#region ■ Install/Uninstall HidGuardian

		public static string GetHidGuardianPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return Path.Combine(baseDirectory, "Program Files", "ViGEm HidGuardian");
		}

		static void ExtractHidGuardianFiles()
		{
			var target = GetHidGuardianPath();
			ExtractViGemFiles("HidGuardian", target);
		}

		/// <summary>
		/// Install HID Guardian
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractHidGuardianFiles();
			var folder = GetHidGuardianPath();
			var paString = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			var infFile = string.Format("{0}\\{1}", paString, "HidGuardian.inf");
			var exePath = Path.Combine(folder, GetDevConPath());
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				"install " + infFile + " " + HidGuardianHardwareId,
				isElevated: true);
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				"classfilter HIDClass upper -HidGuardian",
				isElevated: true);
			// Fix registry permissions. 
			var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
			// Fix missing white list key.
			if (canModify)
				ViGEm.HidGuardianHelper.FixWhiteListRegistryKey();
		}


		/// <summary>
		/// Uninstall HID Guardian.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractHidGuardianFiles();
			var folder = GetHidGuardianPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				"remove " + HidGuardianHardwareId,
				isElevated: true);
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				"classfilter HIDClass upper !HidGuardian",
				isElevated: true);
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
			ExtractHidGuardianFiles();
			var folder = GetHidGuardianPath();
			var exePath = Path.Combine(folder, GetDevConPath());
			JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
				exePath,
				"remove \"" + deviceId + "\"",
				isElevated: true);
			// Make sure that device is re-inserted.
			DeviceDetector.ScanForHardwareChanges();
		}

		#endregion

		#region ■ Extract Helper

		/// <summary>
		/// Extract resource files
		/// </summary>
		/// <param name="source">Resource prefix.</param>
		/// <param name="target">Target folder to extract.</param>
		/// <param name="overwrite">Overwrite files at target.</param>
		static void ExtractViGemFiles(string source, string target)
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
