using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using x360ce.Engine;

namespace x360ce.App.Issues
{

    /// <summary>
    /// Required by ViGEm.
    /// </summary>
    public class HotfixIssue : IssueItem
    {
        public HotfixIssue() : base()
        {
            Name = "HotFix";
            FixName = "Download";
        }

        // Availability of SHA-2 Code Signing Support for Windows 7 and Windows Server 2008 R2
        string program = "Microsoft Security Advisory (KB3033929) Update";

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
            //
            // Issue applies to windows 7 only.
            if (Environment.OSVersion.Version.Major != 6 || Environment.OSVersion.Version.Minor != 1)
            {
                SetSeverity(IssueSeverity.None, 0, program);
                return;
            }
            var installed = IssueHelper.IsInstalledHotFix(3033929);
            if (!installed)
            {
                var bits = Environment.Is64BitOperatingSystem
                    ? "64-bit" : "32-bit";
                SetSeverity(
                    IssueSeverity.Critical, 1,
                    string.Format("Install {0} for Windows 7 {1}", program, bits)
                );
                return;
            }
            SetSeverity(IssueSeverity.None);
        }

        public override void FixTask()
        {
            var url = Environment.Is64BitOperatingSystem
                ? "https://www.microsoft.com/en-us/download/details.aspx?id=46148"
                : "https://www.microsoft.com/en-us/download/details.aspx?id=46078";
            EngineHelper.OpenUrl(url);
        }

    }
}
