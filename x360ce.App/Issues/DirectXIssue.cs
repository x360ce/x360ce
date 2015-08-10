using System;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class DirectXIssue : WarningItem
	{
		public DirectXIssue()
		{
			Name = "DirectX";
			FixName = "Download";
			Description = "Microsoft DirectX 9 not found You can click the link below to download Microsoft DirectX:\r\n" +
				"http://www.microsoft.com/en-us/download/details.aspx?id=35";
		}

		public override void Check()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
			{
				string versionString = key.GetValue("Version") as string;
				Version version;
				if (Version.TryParse(versionString, out version))
				{
					if (version.Minor == 9)
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
			EngineHelper.OpenUrl("http://www.microsoft.com/en-us/download/details.aspx?id=35");
		}

	}
}
