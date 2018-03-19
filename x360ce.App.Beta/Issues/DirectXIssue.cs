using System;
using Microsoft.Win32;
using System.Linq;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls.IssueControl;

namespace x360ce.App.Issues
{
	public class DirectXIssue : IssueItem
    {
		public DirectXIssue() : base()
		{
			Name = "DirectX";
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
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
			{
				string versionString = key.GetValue("Version") as string;
				Version version;
				if (Version.TryParse(versionString, out version))
				{
					if (version.Minor == 9)
					{
						SetSeverity(IssueSeverity.None);
						return;
					}
				}
			}
			SetSeverity(
				IssueSeverity.Moderate, 0,
				"Microsoft DirectX 9 not found You can click the link below to download Microsoft DirectX:\r\n" +
				"http://www.microsoft.com/en-us/download/details.aspx?id=8109"
			);
			Severity = IssueSeverity.Moderate;
		}

		public override void Fix()
		{
			EngineHelper.OpenUrl("http://www.microsoft.com/en-us/download/details.aspx?id=8109");
		}

	}
}
