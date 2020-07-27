using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
    public class CppX86RuntimeInstallIssue : IssueItem
	{

		public CppX86RuntimeInstallIssue() : base()
		{
			Name = "Software";
			FixName = "Download";
		}

		// Use ignore case modifier.
		string program1 = "(?i)(Visual C\\+\\+).*(Redistributable).*(x86)";

		public override void CheckTask()
		{
            var installed = IssueHelper.IsInstalled(program1, false);
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
			ControlsHelper.OpenUrl("https://www.microsoft.com/en-us/download/details.aspx?id=53587");
        }
    }
}
