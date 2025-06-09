using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Linq;

namespace x360ce.App.Issues
{
	public class CppX86RuntimeInstallIssue : IssueItem
	{

		public CppX86RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Download and Install";
			MoreInfo = new Uri("https://support.microsoft.com/en-gb/help/2977003/the-latest-supported-visual-c-downloads");
		}

		// Use ignore case modifier.
		string program1Rx = "(?i)(Visual C\\+\\+).*(2015|2017|2019).*(Redistributable).*(x86)";
		string program1 = "Microsoft Visual C++ 2015-2019 Redistributable (x86)";

		public override void CheckTask()
		{
			var installed = IssueHelper.IsInstalled(program1Rx, false);
			if (!installed)
			{
				SetSeverity(
					IssueSeverity.Critical, 1,
					string.Format("Install " + program1)
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			// Microsoft Visual C++ 2015, 2017, 2019 Redistributable
			var uri = new Uri("https://aka.ms/vs/16/release/vc_redist.x86.exe");
			var localPath = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", uri.Segments.Last());
			IssueHelper.DownloadAndInstall(uri, localPath, MoreInfo);
		}

	}
}
