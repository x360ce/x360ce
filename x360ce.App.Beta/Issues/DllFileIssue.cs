using System;
using System.Reflection;
using System.Linq;
using x360ce.Engine;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Win32;

namespace x360ce.App.Issues
{
	public class DllFileIssue : WarningItem
	{
		public DllFileIssue() : base()
		{
			Name = "DLL File";
			FixName = "Create";
		}

		public override void Check()
		{
			var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Library);
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// If XInput file doesn't exists.
			var appArchitecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
			embeddedDllVersion = EngineHelper.GetEmbeddedDllVersion(appArchitecture);
			var file = EngineHelper.GetDefaultDll();
			// If XInput DLL was not found then...
			if (file == null)
			{
				var xFile = Attributes.GetDescription(XInputMask.XInput13_x86);
				SetSeverity(
					IssueSeverity.Critical, 1,
					string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", xFile)
				);
				return;
			}
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
						SetSeverity(
							IssueSeverity.Moderate, 2,
							string.Format("New version of this file is available:\r\n{0}\r\n\r\nOld version: {1}\r\nNew version: {2}\r\n\r\nDo you want to update this file?", file.Name, dllVersion, embeddedDllVersion)
						);
						return;
					}
					var xiCurrentArchitecture = PEReader.GetProcessorArchitecture(file.FullName);
					if (appArchitecture != xiCurrentArchitecture)
					{
						// Offer upgrade.
						var oldDesc = EngineHelper.GetProcessorArchitectureDescription(xiCurrentArchitecture);
						var newDesc = EngineHelper.GetProcessorArchitectureDescription(appArchitecture);
						SetSeverity(
							IssueSeverity.Moderate, 3,
							string.Format("You are running {2} application but {0} on the disk was built for {1} architecture.\r\n\r\nDo you want to replace {0} file with {2} version?", file.Name, oldDesc, newDesc)
						);
						return;
					}
				}
			}
			SetSeverity(IssueSeverity.None);
		}

		Version dllVersion;
		Version embeddedDllVersion;

		public override void Fix()
		{
			if (FixType > 0)
			{
				var resourceName = EngineHelper.GetXInputResoureceName();
				var file = EngineHelper.GetDefaultDll();
				var fileName = file == null
					? Attributes.GetDescription(XInputMask.XInput13_x86)
					: file.Name;
				AppHelper.WriteFile(resourceName, fileName);
			}
			RaiseFixApplied();
		}

	}
}
