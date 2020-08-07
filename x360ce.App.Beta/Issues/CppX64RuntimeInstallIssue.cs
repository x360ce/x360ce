using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;

namespace x360ce.App.Issues
{
    public class CppX64RuntimeInstallIssue : IssueItem
	{

		public CppX64RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Download and Install";
			MoreInfo = new Uri("https://support.microsoft.com/en-gb/help/2977003/the-latest-supported-visual-c-downloads");
		}

		// Use ignore case modifier.
        string program1Rx = "(?i)(Visual C\\+\\+).*(2015|2017|2019).*(Redistributable).*(x64)";
		string program1 = "Microsoft Visual C++ 2015-2019 Redistributable (x64)";

		public override void CheckTask()
		{
            // This issue check applies only for 64-bit OS.
            if (!Environment.Is64BitOperatingSystem)
            {
                SetSeverity(IssueSeverity.None);
                return;
            }
			var installed = IssueHelper.IsInstalled(program1Rx, false);
            if (!installed)
			{
				SetSeverity(
					IssueSeverity.Critical, 1,
					string.Format("Install "+ program1)
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			// Microsoft Visual C++ 2015, 2017, 2019 Redistributable
			var uri = new Uri("https://aka.ms/vs/16/release/vc_redist.x64.exe");
			IssueHelper.DownloadAndInstall(uri, MoreInfo);
		}
    }
}
