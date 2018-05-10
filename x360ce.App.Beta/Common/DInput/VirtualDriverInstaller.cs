using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;

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

		static string GetViGEmBusPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return System.IO.Path.Combine(baseDirectory, "Program Files", "ViGEm ViGEmBus");
		}

		static void ExtractViGemBusFiles(bool overwrite)
		{
			var target = GetViGEmBusPath();
			ExtractViGemFiles("ViGEmBus", target, overwrite);
		}

		public const string ViGEmBusHardwareId = "Root\\ViGEmBus";
		public const string HidGuardianHardwareId = "Root\\HidGuardian";

        /// <summary>
        /// Install Virtual driver.
        /// </summary>
        /// <remarks>Must be executed in administrative mode.</remarks>
        public static void InstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractViGemBusFiles(true);
			var folder = GetViGEmBusPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"install ViGEmBus.inf " + ViGEmBusHardwareId,
				style, true);
		}

		/// <summary>
		/// UnInstall Virtual driver here.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallViGEmBus(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractViGemBusFiles(false);
			var folder = GetViGEmBusPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"remove " + ViGEmBusHardwareId,
				style, true);
		}

		#endregion

		#region Install/Uninstall HidGuardian

		static string GetHidGuardianPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return System.IO.Path.Combine(baseDirectory, "Program Files", "ViGEm HidGuardian");
		}

		static void ExtractHidGuardianFiles(bool overwrite)
		{
			var target = GetHidGuardianPath();
			ExtractViGemFiles("HidGuardian", target, overwrite);
		}

		/// <summary>
		/// Install HID Guardian
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractHidGuardianFiles(true);
			var folder = GetHidGuardianPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"install HidGuardian.inf " + HidGuardianHardwareId,
				style, true);
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"classfilter HIDClass upper -HidGuardian",
				style, true);
		}

		/// <summary>
		/// Uninstall HID Guardian.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UninstallHidGuardian(ProcessWindowStyle style = ProcessWindowStyle.Hidden)
		{
			// Extract files first.
			ExtractHidGuardianFiles(false);
			var folder = GetHidGuardianPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"remove " + HidGuardianHardwareId,
				style, true);
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"classfilter HIDClass upper !HidGuardian",
				style, true);
		}

		#endregion

		#region Extract Helper

		static void ExtractViGemFiles(string source, string target, bool overwrite)
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
                var folderName = target;
                // Optimize better later.
                if (fileName.StartsWith("x64."))
                {
                    fileName = fileName.Substring("x64.".Length);
                    folderName += "\\x64";
                }
                if (fileName.StartsWith("x86."))
                {
                    fileName = fileName.Substring("x86.".Length);
                    folderName += "\\x86";
                }
                SaveAs(assembly, resourceName, folderName, fileName, overwrite);
			}
		}

		static void SaveAs(Assembly assembly, string resource, string folderName, string fileName, bool overwrite)
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
            if (file.Exists && overwrite)
            {
                file.Delete();
                file.Refresh();
            }
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
