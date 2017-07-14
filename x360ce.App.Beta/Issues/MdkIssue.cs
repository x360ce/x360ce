using System;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class MdkIssue : WarningItem
	{
		public MdkIssue()
		{
			Name = "Microsoft SDK";
			FixName = "Download";
			Description = "You are using debug version of XInput Library. Microsoft SDK not found You can click the link below to download Microsoft SDK:\r\n" +
						"https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx";
		}

		public override void Check()
		{
			// Get list of debug files.
			var pdbs = EngineHelper.GetFiles(".", "*.pdb");
			if (pdbs.Length == 0)
			{
				Severity = IssueSeverity.None;
				return;
			}
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows"))
			{
				string versionString = key.GetValue("CurrentVersion") as string;
				Version version;
				if (Version.TryParse(versionString, out version))
				{
					if (version.Major >= 7)
					{
						Severity = IssueSeverity.None;
						return;
					}
				}
			}
			Severity = IssueSeverity.Moderate;
		}

		public override void Fix()
		{
			EngineHelper.OpenUrl("https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx");
			RaiseFixApplied();
		}

	}
}
