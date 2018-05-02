using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.App.Issues
{
    /// <summary>
    /// Required by ViGEm.
    /// </summary>
    public class XboxDriversIssue : IssueItem
    {

        public XboxDriversIssue() : base()
        {
            Name = "Drivers";
            FixName = "Download";
        }

        string program1 = "Microsoft Xbox 360 Accessories";


        public override void CheckTask()
        {
            // +-----------------------------------------------------+
            // |                    |  Platform      | Major | Minor |
            // +-----------------------------------------------------+
            // | Windows 95         |  Win32Windows  |   4   |   0   |
            // | Windows 98         |  Win32Windows  |   4   |  10   |
            // | Windows Me         |  Win32Windows  |   4   |  90   |
            // | Windows NT 4.0     |  Win32NT       |   4   |   0   |
            // | Windows 2000       |  Win32NT       |   5   |   0   |
            // | Windows XP         |  Win32NT       |   5   |   1   |
            // | Windows 2003       |  Win32NT       |   5   |   2   |
            // | Windows Vista      |  Win32NT       |   6   |   0   |
            // | Windows 2008       |  Win32NT       |   6   |   0   |
            // | Windows 7          |  Win32NT       |   6   |   1   |
            // | Windows 2008 R2    |  Win32NT       |   6   |   1   |
            // | Windows 8          |  Win32NT       |   6   |   2   |
            // | Windows 8.1        |  Win32NT       |   6   |   3   |
            // | Windows 10         |  Win32NT       |  10   |   0   |
            // +-----------------------------------------------------+
            //
            // There is no issue with Windows 10 or later, because 
            // Windows 10 will install drivers and software automatically.
            // 
            var version  = IssueHelper.GetRealOSVersion();
            if (version.Major >= 10)
            {
                SetSeverity(IssueSeverity.None, 0, program1);
                return;
            }
            var installed = IssueHelper.IsInstalled(program1, false);
            if (!installed)
            {
                SetSeverity(
                    IssueSeverity.Critical, 1,
                    string.Format("Please install {0} ({1}) drivers and software.",
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
