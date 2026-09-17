using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
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
	///
	/// A controller that is gone leaves a record too: Windows keeps one for every device it has ever
	/// built until something removes it. Those take no place, but a machine that has seen many runs end
	/// badly carries dozens, each walked by every read of the machine and each a line in Device Manager
	/// that looks like a fault. They are named here as well, and the same removal takes them away.
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
			var present = pads.Where(x => x.IsPresent).ToArray();
			var records = pads.Where(x => !x.IsPresent).ToArray();
			// Said as what it costs the person rather than as a count of devices, because the number on
			// its own means nothing to somebody who does not know that only four places exist.
			var lines = new List<string>();
			if (present.Length > 0)
				lines.Add(string.Format(
					"{0} left behind by earlier runs {1} still present, taking places this " +
					"program needs, so a controller can look dead or move on its own.",
					Count(present.Length), present.Length == 1 ? "is" : "are"));
			if (records.Length > 0)
				lines.Add(string.Format(
					"Windows still keeps {0} of {1} no longer present, left by runs that ended " +
					"without unplugging {2}; each is walked by every read of the machine and shown " +
					"in Device Manager as if it were a fault.",
					records.Length == 1 ? "a record" : records.Length + " records",
					records.Length == 1 ? "a virtual controller" : "virtual controllers",
					records.Length == 1 ? "it" : "them"));
			// And named, because a count alone cannot be acted on. Where removing them does not work -
			// Windows refuses while something holds one open, or the thing found is not really a leftover -
			// a person is left with a complaint that returns for ever and nothing to look at. The name and
			// the identifier are what let somebody find it in Device Manager, or say what it is when the
			// removal will not take.
			var named = string.Join(Environment.NewLine, pads
				.Select(x => "    " + (string.IsNullOrEmpty(x.Description) ? "Unnamed device" : x.Description)
					+ (x.IsPresent ? "" : " (not present)")
					+ Environment.NewLine + "        " + x.DeviceId)
				.ToArray());
			// A present leftover takes a place and breaks the emulation; a record only clutters.
			SetSeverity(present.Length > 0 ? IssueSeverity.Moderate : IssueSeverity.Low, 0,
				string.Join(Environment.NewLine + Environment.NewLine, lines.ToArray())
				+ Environment.NewLine + Environment.NewLine + named);
		}

		static string Count(int pads)
		{
			return pads == 1 ? "One virtual controller" : pads + " virtual controllers";
		}

		public override void FixTask()
		{
			// Runs on the issue panel's worker, which is the only kind of thread this may run on.
			bool succeeded;
			DInput.VirtualDriverInstaller.RemoveLeftoverPadsElevated(
				DInput.VirtualDriverInstaller.GetLeftoverVirtualPads().Length, out succeeded);
		}

	}
}
