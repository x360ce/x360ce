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
			MoreInfo = new Uri("https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist");
		}

		string program1 = "Microsoft Visual C++ 2015-2022 Redistributable (x64)";

		public override void CheckTask()
		{
            // This issue check applies only for 64-bit OS.
            if (!Environment.Is64BitOperatingSystem)
            {
                SetSeverity(IssueSeverity.None);
                return;
            }
			var installed = CppRuntimeDetector.GetInstalledVersion(true) != null;
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
			// Microsoft Visual C++ 2015-2022 Redistributable
			var uri = new Uri("https://aka.ms/vs/17/release/vc_redist.x64.exe");
			var localPath = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", uri.Segments.Last());
			IssueHelper.DownloadAndInstall(uri, localPath, MoreInfo);
		}
    }
}
