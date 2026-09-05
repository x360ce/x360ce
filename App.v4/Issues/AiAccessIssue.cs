using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>Access is switched on and something keeps it from working: the port is out of range, another program holds it, or the options file the switches read could not be written.</summary>
	public class AiAccessIssue : IssueItem
	{
		public AiAccessIssue() : base()
		{
			Name = "AI assistant access";
			FixName = "Fix";
			// Making the reservation asks Windows for elevation. Marked, so the prompt is expected.
			FixNeedsAdmin = true;
		}

		public override void CheckTask()
		{
			// Keyed on a recorded failure, not on "not running yet", so the moment before the first
			// start is not reported as a fault. The sentence carries its own remedy, and the one
			// remedy the program can apply itself gets the Fix button.
			var error = Mcp.McpListener.LastError;
			if (SettingsManager.Options.AiAccess != AiAccess.Off && error != null)
			{
				SetSeverity(IssueSeverity.Important, Mcp.McpListener.NeedsUrlReservation ? 1 : 0, "AI assistant access: " + error);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void FixTask()
		{
			if (FixType != 1)
				return;
			Program.RunElevated(AdminCommand.ReserveAiAccessUrl, SettingsManager.Options.AiAccessPort.ToString());
			// Windows now allows the address, so the door is opened again the same way the option does it.
			JocysCom.ClassLibrary.Controls.ControlsHelper.Invoke(Global.ApplyAiAccess);
		}
	}
}
