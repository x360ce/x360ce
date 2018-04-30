using System;
using Microsoft.Win32;
using System.Linq;
using x360ce.Engine;
using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
    public class DirectXIssue : IssueItem
    {
        public DirectXIssue() : base()
        {
            Name = "DirectX";
            FixName = "Download";
        }

        public override void CheckTask()
        {
            var severity = IssueSeverity.Critical;
            var xiFi = EngineHelper.GetMsXInputLocation();
            // If required file is missing then error will be critical.
            if (xiFi.Exists)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
                {
                    string versionString = key.GetValue("Version") as string;
                    Version version;
                    // If DirectX 9 was found then...
                    if (Version.TryParse(versionString, out version) && version.Minor == 9)
                    {
                        severity = IssueSeverity.None;
                        return;
                    }
                }
            }
            SetSeverity(
                severity, 0,
                "Microsoft DirectX 9 not found You can click the link below to download Microsoft DirectX 9.0c:\r\n" +
                "http://www.microsoft.com/en-us/download/details.aspx?id=8109"
            );
            Severity = severity;
        }

        public override void FixTask()
        {
            EngineHelper.OpenUrl("http://www.microsoft.com/en-us/download/details.aspx?id=8109");
        }

    }
}
