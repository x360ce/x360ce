using System;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class DllFileIssue : WarningItem
	{
		public DllFileIssue()
		{
			Name = "DLL File";
			FixName = "Create";
			Description = "";
		}

		public override void Check()
		{
			Severity = System.IO.File.Exists(SettingsManager.IniFileName)
				? IssueSeverity.None
				: IssueSeverity.Critical;

			// If XInput file doesn't exists.
			var appArchitecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
			embeddedDllVersion = EngineHelper.GetEmbeddedDllVersion(appArchitecture);
			var file = EngineHelper.GetDefaultDll();
			//// If XInput DLL was not found then...
			//if (file == null)
			//{
			//	var xFile = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(XInputMask.XInput13_x86);
			//	Description = string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", xFile);
			//	// Offer extract.
			//	FixType = 1;
			//	Severity = IssueSeverity.Critical;
			//	return;
			//}
			if (file != null)
			{
				bool byMicrosoft;
				dllVersion = EngineHelper.GetDllVersion(file.FullName, out byMicrosoft);
				// If file is not by Microsoft then...
				if (!byMicrosoft)
				{
					// If file on the disk is older then...
					if (dllVersion < embeddedDllVersion)
					{
						Description = string.Format("New version of this file is available:\r\n{0}\r\n\r\nOld version: {1}\r\nNew version: {2}\r\n\r\nDo you want to update this file?", file.Name, dllVersion, embeddedDllVersion);
						FixType = 2;
						Severity = IssueSeverity.Moderate;
						return;
					}
					var xiCurrentArchitecture = Engine.Win32.PEReader.GetProcessorArchitecture(file.FullName);
					if (appArchitecture != xiCurrentArchitecture)
					{
						// Offer upgrade.
						var oldDesc = EngineHelper.GetProcessorArchitectureDescription(xiCurrentArchitecture);
						var newDesc = EngineHelper.GetProcessorArchitectureDescription(appArchitecture);
						Description = string.Format("You are running {2} application but {0} on the disk was built for {1} architecture.\r\n\r\nDo you want to replace {0} file with {2} version?", file.Name, oldDesc, newDesc);
						FixType = 3;
						Severity = IssueSeverity.Moderate;
						return;
					}
				}
			}
			Severity = IssueSeverity.None;
		}

		int FixType = 0;
		Version dllVersion;
		Version embeddedDllVersion;

		public override void Fix()
		{
			if (FixType > 0)
			{
				var resourceName = EngineHelper.GetXInputResoureceName();
				var file = EngineHelper.GetDefaultDll();
				var fileName = file == null
					? JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(XInputMask.XInput13_x86)
					: file.Name;
				AppHelper.WriteFile(resourceName, fileName);
			}
			RaiseFixApplied();
		}

	}
}
