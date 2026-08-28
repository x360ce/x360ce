using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Virtual controllers Windows started building and never finished.
	/// </summary>
	/// <remarks>
	/// A working virtual controller is two devices: the one the bus makes, and the part beneath it
	/// that XInput reads. Windows can build the first and never the second. Nothing reports an error
	/// when that happens - the device says it has no problem, the bus says it accepted the controller,
	/// and this program used to show a green light over the top of it. Meanwhile the controller is
	/// missing from Windows' own Game Controllers list and no game can see it.
	///
	/// It has been seen after many controllers were removed in quick succession while something still
	/// held them open, which is what pressing Remove used to do.
	/// </remarks>
	class UnfinishedVirtualPadsIssue : IssueItem
	{
		public UnfinishedVirtualPadsIssue() : base()
		{
			Name = "Virtual Controller Not Finished";
			FixName = "Repair Bus";
			// Removing and putting back the bus is an Administrator action.
			FixNeedsAdmin = true;
		}

		public override void CheckTask()
		{
			var pads = DInput.VirtualDriverInstaller.GetUnfinishedVirtualPads();
			if (pads.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// Said as what the person is seeing, because what they are seeing is a controller that does
			// nothing while everything claims to be working.
			var text = pads.Length == 1
				? "A virtual controller was created but Windows did not finish building it, so it is " +
				  "missing from Windows Game Controllers and no game can read it. Repairing the virtual " +
				  "bus usually rebuilds it; restart Windows if it does not."
				: string.Format(
					"{0} virtual controllers were created but Windows did not finish building them, so " +
					"they are missing from Windows Game Controllers and no game can read them. " +
					"Repairing the virtual bus usually rebuilds them; restart Windows if it does not.",
					pads.Length);
			SetSeverity(IssueSeverity.Critical, 0, text);
		}

		public override void FixTask()
		{
			// Let go first, exactly as removing does: the bus cannot be taken out from under controllers
			// this program is still holding open.
			var helper = Global.DHelper;
			if (helper != null)
				helper.ReleaseForDeviceRemoval();
			try
			{
				Program.RunElevated(AdminCommand.RepairViGEmBus);
			}
			finally
			{
				if (helper != null)
					helper.ResumeAfterDeviceRemoval();
			}
		}

	}
}
