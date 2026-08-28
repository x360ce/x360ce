using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Controllers Windows agreed to remove and can only finish removing when it restarts.
	/// </summary>
	/// <remarks>
	/// Windows will not remove a device while anything still holds it open, and more than this program
	/// holds these: its own compositor and shell open game controllers too, and cannot be asked to let
	/// go. So a refusal is the ordinary outcome rather than a fault, and the removal is finished at the
	/// next restart instead.
	///
	/// Windows says so plainly when asked. The answer used to be printed to a console window that
	/// opens and closes in the same instant, so what a person saw was a button that did nothing.
	/// </remarks>
	class RestartToFinishRemovalIssue : IssueItem
	{
		public RestartToFinishRemovalIssue() : base()
		{
			Name = "Restart Needed To Finish Removing";
			// No button. Restarting is the person's decision, and offering one that cannot work is worse
			// than offering none.
			FixName = null;
		}

		public override void CheckTask()
		{
			if (!DInput.VirtualDriverInstaller.RestartNeededToFinishRemoval)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			SetSeverity(IssueSeverity.Important, 0,
				"Windows could not finish removing the old virtual controllers because they were " +
				"still in use, and will finish at the next restart. Until then new virtual " +
				"controllers may not work. Restart Windows.");
		}

	}
}
