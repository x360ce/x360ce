using System;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class LeakDetectorIssue : WarningItem
	{
		public LeakDetectorIssue()
		{
			Name = "Leak Detector";
			FixName = "Download";
			Description = "You are using debug version of XInput Library. Visual Leak Detector not found You can click the link below to download Visual Leak Detector:\r\n" +
					"https://github.com/KindDragon/vld/releases";
		}

		public override void Check()
		{
			// Get list of debug files.
			var pdbs = AppHelper.GetFiles(".", "*.pdb");
			if (pdbs.Length == 0)
			{
				Severity = IssueSeverity.None;
				return;
			}
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
			{
				foreach (string subkey_name in key.GetSubKeyNames())
				{
					using (RegistryKey subkey = key.OpenSubKey(subkey_name))
					{
						var displayName = (string)subkey.GetValue("DisplayName", "");
						if (displayName.StartsWith("Visual Leak Detector"))
						{
							Severity = IssueSeverity.None;
							return;
						}
					}
				}
			}
			Severity = IssueSeverity.Moderate;
		}

		public override void Fix()
		{
			EngineHelper.OpenUrl("https://github.com/KindDragon/vld/releases");
			RaiseFixApplied();
		}

	}
}
