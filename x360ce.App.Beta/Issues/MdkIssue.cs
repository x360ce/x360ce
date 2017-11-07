using System;
using Microsoft.Win32;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class MdkIssue : WarningItem
	{
		public MdkIssue() : base()
		{
			Name = "Microsoft SDK";
			FixName = "Download";
		}

		public override void Check()
		{
			var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Library);
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// Get list of debug files.
			var pdbs = EngineHelper.GetFiles(".", "*.pdb");
			if (pdbs.Length > 0)
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows"))
				{
					string versionString = key.GetValue("CurrentVersion") as string;
					Version version;
					if (Version.TryParse(versionString, out version))
					{
						if (version.Major >= 7)
						{
							SetSeverity(IssueSeverity.None);
							return;
						}
					}
				}
				SetSeverity(
					IssueSeverity.Moderate, 0,
					"You are using debug version of XInput Library. Microsoft SDK not found You can click the link below to download Microsoft SDK:\r\n" +
					"https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx"
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void Fix()
		{
			EngineHelper.OpenUrl("https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx");
			RaiseFixApplied();
		}

	}
}
