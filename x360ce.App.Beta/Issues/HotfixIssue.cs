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

        // This will be used to stop check on success.
        bool enableCheck = true;

        // Availability of SHA-2 Code Signing Support for Windows 7 and Windows Server 2008 R2
        string program = "Microsoft Security Advisory (KB3033929) Update";

        public override void CheckTask()
        {
            if (!enableCheck)
                return;
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
            var version = IssueHelper.GetRealOSVersion();
            if (version.Major != 6 || version.Minor != 1)
            {
                SetSeverity(IssueSeverity.None, 0, program);
                enableCheck = false;
                return;
            }
            // KB3033929 supplies wintrust.dll 6.1.7601.22948, we need this version or later.
            var fileVersion = IssueHelper.GetFileVersion(Environment.SpecialFolder.System, "wintrust.dll");
            var installed = fileVersion >= new Version(6, 1, 7601, 22948);
            if (!installed)
            {
                var bits = Environment.Is64BitOperatingSystem
                    ? "64-bit" : "32-bit";
                SetSeverity(
                    IssueSeverity.Critical, 1,
                    string.Format("Old WinTrust {0}. Install {1} for Windows 7 {2}", fileVersion, program, bits)
                );
                return;
            }
            SetSeverity(IssueSeverity.None);
            enableCheck = false;
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
