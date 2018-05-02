using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Issues
{
    public class CppX64RuntimeInstallIssue : IssueItem
	{

		public CppX64RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Download";
		}

        string program1 = "Visual C++ 2015 Redistributable (x64)";
        string program2 = "Visual C++ 2017 Redistributable (x64)";

        public override void CheckTask()
		{
            // This issue check applies only for 64-bit OS.
            if (!Environment.Is64BitOperatingSystem)
            {
                SetSeverity(IssueSeverity.None);
                return;
            }
			var installed1 = IssueHelper.IsInstalled(program1, false);
            var installed = installed1 || IssueHelper.IsInstalled(program2, false);
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
            // Microsoft Visual C++ 2015 Redistributable Update 3
            EngineHelper.OpenUrl("https://www.microsoft.com/en-us/download/details.aspx?id=53587");
        }
    }
}
