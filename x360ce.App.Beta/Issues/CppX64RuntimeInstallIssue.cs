using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Linq;

namespace x360ce.App.Issues
{
    public class CppX64RuntimeInstallIssue : IssueItem
	{

		public CppX64RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Download and Install";
			MoreInfo = new Uri("https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist");
		}

		// Use ignore case modifier.
        string program1Rx = "(?i)(Visual C\\+\\+).*(2015|2017|2019|2022|v14).*(Redistributable).*(x64)";
		string program1 = "Microsoft Visual C++ 2015-2022 (v14) Redistributable (x64)";

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
			// Microsoft Visual C++ 2015-2022 (v14) Redistributable
			var uri = new Uri("https://aka.ms/vc14/vc_redist.x64.exe");
			var localPath = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", uri.Segments.Last());
			IssueHelper.DownloadAndInstall(uri, localPath, MoreInfo);
		}
    }
}
