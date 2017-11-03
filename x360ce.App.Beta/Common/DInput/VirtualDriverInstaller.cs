using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App.DInput
{
	public class VirtualDriverInstaller
	{

		static void ExtractFiles()
		{
			// Extract files first.
			var pf = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			var dest = System.IO.Path.Combine(pf, "Nefarius vBox");
		}


		// This must be executed in administrative mode.
		public static void InstallVirtualDriver()
		{
			ExtractFiles();
			// Install Virtual driver.
			//devcon.exe install ScpVBus.inf Root\ScpVBus
			// c:\Projects\TocaEdit\x360ce.App.Beta\Resources\vBox_x64\
			//JocysCom.ClassLibrary.Win32.NativeMethods.RunElevated("")
		}

		public static void UnInstallVirtualDriver()
		{
			// UnInstall Virtual driver here.
			//devcon.exe remove Root\ScpVBus
		}

	}
}
