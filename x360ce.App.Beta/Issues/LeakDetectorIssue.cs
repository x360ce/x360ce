using Microsoft.Win32;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class LeakDetectorIssue : WarningItem
	{
		public LeakDetectorIssue() : base()
		{
			Name = "Leak Detector";
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
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
				{
					foreach (string subkey_name in key.GetSubKeyNames())
					{
						using (RegistryKey subkey = key.OpenSubKey(subkey_name))
						{
							var displayName = (string)subkey.GetValue("DisplayName", "");
							if (displayName.StartsWith("Visual Leak Detector"))
							{
								SetSeverity(IssueSeverity.None);
								return;
							}
						}
					}
				}
				SetSeverity(
					IssueSeverity.Moderate, 0,
					"You are using debug version of XInput Library. Visual Leak Detector not found You can click the link below to download Visual Leak Detector:\r\n" +
					"https://vld.codeplex.com"
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void Fix()
		{
			EngineHelper.OpenUrl("https://vld.codeplex.com");
			RaiseFixApplied();
		}

	}
}
