using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Linq;

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
			// Windows does not let an ordinary program remove a device, so pressing this raises a
			// prompt. Marked, so the prompt is expected.
			FixNeedsAdmin = true;
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
			//
			// And named, because a count alone cannot be acted on. Where removing them does not work -
			// Windows refuses while something holds one open, or the thing found is not really a leftover -
			// a person is left with a complaint that returns for ever and nothing to look at. The name and
			// the identifier are what let somebody find it in Device Manager, or say what it is when the
			// removal will not take.
			var named = string.Join(Environment.NewLine, pads
				.Select(x => "    " + (string.IsNullOrEmpty(x.Description) ? "Unnamed device" : x.Description)
					+ Environment.NewLine + "        " + x.DeviceId)
				.ToArray());
			SetSeverity(IssueSeverity.Moderate, 0, string.Format(
				"{0} virtual controllers left behind by earlier runs are still present. They take the " +
				"places this program needs, so a controller can look dead or move on its own." +
				Environment.NewLine + Environment.NewLine + "{1}",
				pads.Length, named));
		}

		public override void FixTask()
		{
			// Let go of the controllers first. Windows refuses to remove a device anything still holds
			// open, and this program holds all four places open while it reads their states; without
			// this the removal is refused and each refusal leaves Windows needing a restart before it
			// will finish building any new controller.
			var helper = Global.DHelper;
			if (helper != null)
				helper.ReleaseForDeviceRemoval();
			try
			{
				// Windows does not let an ordinary program remove a device, so this takes the route every
				// other administrative action here takes: a second copy of this program starts as
				// Administrator with one argument, does the work, and ends.
				Program.RunElevated(AdminCommand.RemoveLeftoverPads);
				// Windows refuses while anything holds the controller open, and its own shell does, so
				// this is a normal answer rather than a fault. Kept, because the only thing that
				// finishes the removal is a restart, and nobody would otherwise know to do one.
				if (Program.LastAdminResult == Program.AdminResult.RestartNeeded)
					DInput.VirtualDriverInstaller.RestartNeededToFinishRemoval = true;
			}
			finally
			{
				// Picked back up whatever happened, or the program is left feeding nothing.
				if (helper != null)
					helper.ResumeAfterDeviceRemoval();
			}
		}

	}
}
