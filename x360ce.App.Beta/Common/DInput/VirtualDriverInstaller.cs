using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace x360ce.App.DInput
{
	public class VirtualDriverInstaller
	{
		static string GetVboxPath()
		{
			string baseDirectory = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
			return System.IO.Path.Combine(baseDirectory, "Program Files", "ViGEm ViGEmBus");
		}

		/// <summary>
		/// Extract files first.
		/// </summary>
		static void ExtractFiles()
		{
			var folderName = GetVboxPath();
			// There must be an easier way to check embedded non managed DLL version.
			var paString = Environment.Is64BitOperatingSystem ? "x64" : "x86";
			// Get list of resources to extract.
			var assembly = Assembly.GetEntryAssembly();
			var resourceFolder = string.Format(".Resources.ViGEmBus.{0}.", paString);
			var resourceNames = assembly.GetManifestResourceNames().Where(x => x.Contains(resourceFolder)).ToArray();
			foreach (var resourceName in resourceNames)
			{
				var fileName = resourceName.Substring(resourceName.IndexOf(resourceFolder) + resourceFolder.Length);
				SaveAs(assembly, resourceName, folderName, fileName);
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

		/// <summary>
		/// Install Virtual driver.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void InstallVirtualDriver()
		{
			ExtractFiles();
			var folder = GetVboxPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"install ViGEmBus.inf Root\\ViGEmBus",
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

		/// <summary>
		/// UnInstall Virtual driver here.
		/// </summary>
		/// <remarks>Must be executed in administrative mode.</remarks>
		public static void UnInstallVirtualDriver()
		{
			ExtractFiles();
			var folder = GetVboxPath();
			var fullPath = System.IO.Path.Combine(folder, "devcon.exe");
			JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
				fullPath,
				"remove Root\\ViGEmBus",
				System.Diagnostics.ProcessWindowStyle.Normal, true);
		}

	}
}
