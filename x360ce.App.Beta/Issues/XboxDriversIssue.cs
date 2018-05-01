using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Issues
{
    public class XboxDriversIssue : IssueItem
    {

        public XboxDriversIssue() : base()
        {
            Name = "Drivers";
            FixName = "Download";
        }

        string program1 = "Xbox 360 Controller for Windows";

        public override void CheckTask()
        {
            // Windows 10 drivers will be installed automatically.
            if (Environment.OSVersion.Version.Major >= 10)
            {
                SetSeverity(IssueSeverity.None, 0, program1);
            }
            var installed = IssueHelper.IsInstalled(program1, false);
            if (!installed)
            {
                SetSeverity(
                    IssueSeverity.Critical, 1,
                    string.Format("Please install {0} ({1}) drivers and software",
                    program1, Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")
                );
                return;
            }
            SetSeverity(IssueSeverity.None, 0, program1);
        }

        public override void FixTask()
        {
            // Xbox 360 Controller for Windows
            EngineHelper.OpenUrl("https://www.microsoft.com/accessories/en-au/d/xbox-360-controller-for-windows");
        }
    }
}
