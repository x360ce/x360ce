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
			MoreInfo = new Uri(CppRuntimeDetector.LatestUrl);
			// Microsoft's installer changes the whole machine, so Windows asks for Administrator first.
			FixNeedsAdmin = true;
		}

		string program1 = "Microsoft Visual C++ v14 Redistributable (x64)";

		/// <summary>The installer's exit code from the last Download and Install; null until it has run.</summary>
		int? LastExitCode;

		public override void CheckTask()
		{
			// This issue check applies only for 64-bit OS.
			if (!Environment.Is64BitOperatingSystem)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var installed = CppRuntimeDetector.GetInstalledVersion(true) != null;
			string description;
			bool troubleshoot;
			var severity = CppRuntimeDetector.ExplainInstall(LastExitCode, installed, program1, out description, out troubleshoot);
			var page = troubleshoot ? CppRuntimeDetector.TroubleshootUrl : CppRuntimeDetector.LatestUrl;
			if (MoreInfo == null || MoreInfo.AbsoluteUri != page)
				MoreInfo = new Uri(page);
			SetSeverity(severity, 1, description);
		}

		public override void FixTask()
		{
			// Permalink to the latest supported Microsoft Visual C++ v14 Redistributable (Visual Studio 2017-2026).
			var uri = new Uri("https://aka.ms/vc14/vc_redist.x64.exe");
			LastExitCode = CppRuntimeDetector.Install(uri, new Uri(CppRuntimeDetector.LatestUrl));
		}
    }
}
