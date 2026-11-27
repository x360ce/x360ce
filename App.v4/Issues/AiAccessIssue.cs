using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace x360ce.App.Issues
{
	/// <summary>Access is switched on and something keeps it from working: the port is out of range, another program holds it, or the AI settings file the switches read could not be written.</summary>
	public class AiAccessIssue : IssueItem
	{
		public AiAccessIssue() : base()
		{
			// Making the reservation asks Windows for elevation. Marked, so the prompt is expected.
			FixNeedsAdmin = true;
		}

		/// <inheritdoc/>
		public override void SetTexts()
		{
			Name = UiLanguage.T("AiAccessIssue.AIAssistantAccess", "AI assistant access");
			FixName = UiLanguage.T("AiAccessIssue.Fix", "Fix");
		}

		public override void CheckTask()
		{
			// Keyed on a recorded failure, not on "not running yet", so the moment before the first
			// start is not reported as a fault. The sentence carries its own remedy, and the one
			// remedy the program can apply itself gets the Fix button.
			var error = JocysCom.ClassLibrary.Mcp.McpListener.Program.LastError;
			if (SettingsManager.AiOptions.AiAccessEnabled && error != null)
			{
				SetSeverity(IssueSeverity.Important, JocysCom.ClassLibrary.Mcp.McpListener.Program.NeedsUrlReservation ? 1 : 0, string.Format(UiLanguage.T("AiAccessIssue.AccessError", "AI assistant access: {0}"), error));
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			if (FixType != 1)
				return;
			Program.RunElevated(AdminCommand.ReserveAiAccessUrl, SettingsManager.AiOptions.AiAccessPort.ToString());
			// Windows now allows the address, so the door is opened again the same way the option does it.
			JocysCom.ClassLibrary.Controls.ControlsHelper.Invoke(Global.ApplyAiAccess);
		}
	}
}
