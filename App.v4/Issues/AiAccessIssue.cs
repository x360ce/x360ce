using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>Access is switched on and something keeps it from working: the port is out of range, another program holds it, or the options file the switches read could not be written.</summary>
	public class AiAccessIssue : IssueItem
	{
		public AiAccessIssue() : base()
		{
			Name = "AI assistant access";
		}

		public override void CheckTask()
		{
			// Keyed on a recorded failure, not on "not running yet", so the moment before the first
			// start is not reported as a fault. The sentence carries its own remedy.
			var error = Mcp.McpListener.LastError;
			if (SettingsManager.Options.AiAccess != AiAccess.Off && error != null)
			{
				SetSeverity(IssueSeverity.Important, 0, "AI assistant access: " + error);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}
	}
}
