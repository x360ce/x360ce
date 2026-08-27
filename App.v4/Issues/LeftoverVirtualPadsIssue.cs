using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Virtual controllers the bus is still holding for runs that ended without shutting down.
	/// </summary>
	/// <remarks>
	/// The bus keeps a controller plugged in until it is told to unplug it. A run that ends normally
	/// tells it; one that is killed, or that crashes, never gets the chance, and the controller stays
	/// plugged in until Windows is restarted.
	///
	/// Windows offers four places for a controller of this kind and fills them in order, so ones left
	/// behind take the first places and the controller this program creates lands after them. What a
	/// person sees is a picture that never moves, or one that moves on its own, with nothing anywhere
	/// saying why. This is that missing explanation.
	/// </remarks>
	class LeftoverVirtualPadsIssue : IssueItem
	{
		public LeftoverVirtualPadsIssue() : base()
		{
			Name = "Leftover Virtual Controllers";
			FixName = "Remove";
		}

		public override void CheckTask()
		{
			var pads = DInput.VirtualDriverInstaller.GetLeftoverVirtualPads();
			if (pads.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// Said as what it costs the person rather than as a count of devices, because the number on
			// its own means nothing to somebody who does not know that only four places exist.
			SetSeverity(IssueSeverity.Moderate, 0, string.Format(
				"{0} virtual controllers left behind by earlier runs are still present. They take the " +
				"places this program needs, so a controller can look dead or move on its own.",
				pads.Length));
		}

		public override void FixTask()
		{
			// Windows does not let an ordinary program remove a device, so this takes the route every
			// other administrative action here takes: a second copy of this program starts as
			// Administrator with one argument, does the work, and ends.
			Program.RunElevated(AdminCommand.RemoveLeftoverPads);
			// Devices are read again so the list on screen matches what is now there.
			Global.DHelper.UpdateDevicesEnabled = true;
		}

	}
}
