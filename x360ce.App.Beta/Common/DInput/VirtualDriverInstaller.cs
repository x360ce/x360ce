using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace x360ce.App.DInput
{
	public class VirtualDriverInstaller
	{

		#region Install/Uninstall ViGEmBus

		public static SP_DRVINFO_DATA GetViGemBusDriverInfo()
		{
			var devices = DeviceDetector.GetDevices();
			var device = devices.FirstOrDefault(x => x.HardwareId == "Root\\ViGEmBus");
			var driver = DeviceDetector.GetDrivers(device.DeviceId).FirstOrDefault();
			return driver;
		}

		public static SP_DRVINFO_DATA GetHidGuardianDriverInfo()
		{
			var devices = DeviceDetector.GetDevices();
			var device = devices.FirstOrDefault(x => x.HardwareId == "Root\\HidGuardian");
			var driver = DeviceDetector.GetDrivers(device.DeviceId).FirstOrDefault();
			return driver;
		}

		static string GetViGEmBusPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return System.IO.Path.Combine(baseDirectory, "Program Files", "ViGEm ViGEmBus");
		}

		static void ExtractViGemBusFiles()
		{
			var target = GetViGEmBusPath();
			ExtractViGemBusFiles("ViGEmBus", target);
		}

		public const string ViGEmBusHardwareId = "Root\\ViGEmBus";
		public const string HidGuardianHardwareId = "Root\\HidGuardian";

		/// <summary>
		/// Install Virtual driver.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallViGEmBus()
		{
			// Extract files first.
			ExtractViGemBusFiles();
			var folder = GetViGEmBusPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"install ViGEmBus.inf " + ViGEmBusHardwareId,
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

		/// <summary>
		/// UnInstall Virtual driver here.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallViGEmBus()
		{
			// Extract files first.
			ExtractViGemBusFiles();
			var folder = GetViGEmBusPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"remove " + ViGEmBusHardwareId,
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

		#endregion

		#region Install/Uninstall HidGuardian

		static string GetHidGuardianPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return System.IO.Path.Combine(baseDirectory, "Program Files", "ViGEm HidGuardian");
		}

		static void ExtractHidGuardianFiles()
		{
			var target = GetViGEmBusPath();
			ExtractViGemBusFiles("HidGuardian", target);
		}

		/// <summary>
		/// Install HID Guardian
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallHidGuardian()
		{
			// Extract files first.
			ExtractHidGuardianFiles();
			var folder = GetHidGuardianPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"install HidGuardian.inf " + HidGuardianHardwareId,
				System.Diagnostics.ProcessWindowStyle.Normal, true);
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"classfilter HIDClass upper -HidGuardian",
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

		/// <summary>
		/// Uninstall HID Guardian.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallHidGuardian()
		{
			// Extract files first.
			ExtractHidGuardianFiles();
			var folder = GetHidGuardianPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"remove " + HidGuardianHardwareId,
				System.Diagnostics.ProcessWindowStyle.Normal, true);
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"classfilter HIDClass upper !HidGuardian",
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

		#endregion

		#region Extract Helper

		static void ExtractViGemBusFiles(string source, string target)
		{
			// There must be an easier way to check embedded non managed DLL version.
			var paString = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			// Get list of resources to extract.
			var assembly = Assembly.GetEntryAssembly();
			var resourceFolder = string.Format(".Resources.{0}.{1}.", source, paString);
			var resourceNames = assembly.GetManifestResourceNames().Where(x => x.Contains(resourceFolder)).ToArray();
			foreach (var resourceName in resourceNames)
			{
				var fileName = resourceName.Substring(resourceName.IndexOf(resourceFolder) + resourceFolder.Length);
				SaveAs(assembly, resourceName, target, fileName);
			}
		}

		static void SaveAs(Assembly assembly, string resource, string folderName, string fileName)
		{
			var dir = new DirectoryInfo(folderName);
			if (!dir.Exists)
				dir.Create();
			var sr = assembly.GetManifestResourceStream(resource);
			if (sr == null)
				return;
			var bytes = new byte[sr.Length];
			sr.Read(bytes, 0, bytes.Length);
			var name = System.IO.Path.GetFileName(resource);
			var fullPath = System.IO.Path.Combine(dir.FullName, fileName);
			var file = new FileInfo(fullPath);
			if (!file.Exists)
			{
				var writer = file.OpenWrite();
				writer.Write(bytes, 0, bytes.Count());
				writer.Flush();
				writer.Dispose();
			}
		}

		#endregion

	}
}
