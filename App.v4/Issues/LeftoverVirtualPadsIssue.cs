using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
using System.Linq;
using JocysCom.ClassLibrary.Controls.UiTree;

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
	class LeftoverVirtualPadsIssue : OnChangeIssue
	{
		public LeftoverVirtualPadsIssue() : base()
		{
			// Windows does not let an ordinary program remove a device, so pressing this raises a
			// prompt. Marked, so the prompt is expected.
			FixNeedsAdmin = true;
		}

		/// <inheritdoc/>
		public override void SetTexts()
		{
			Name = UiLanguage.T("LeftoverVirtualPadsIssue.LeftoverVirtualControllers", "Leftover Virtual Controllers");
			FixName = UiLanguage.T("LeftoverVirtualPadsIssue.Remove", "Remove");
		}

		public override void CheckTask()
		{
			// Not in the round of checks at start-up. On a machine that keeps hundreds of records of old
			// controllers the first reading takes seconds, under the lock the device list read also waits
			// on, while the engine starts and plugs its controllers in. Nothing here can stop the program
			// working, so the next round, a few seconds after the controllers are running, answers it.
			var form = MainForm.Current;
			if (form != null && form.CheckingAtStartup)
				return;
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
			// One whole sentence for one and another for many, so each language can say it its own way.
			if (present.Length == 1)
				lines.Add(UiLanguage.T("LeftoverVirtualPadsIssue.OnePresent", "One virtual controller left behind by earlier runs is still present, taking a place this program needs, so a controller can look dead or move on its own."));
			else if (present.Length > 1)
				lines.Add(string.Format(UiLanguage.T("LeftoverVirtualPadsIssue.ManyPresent", "{0} virtual controllers left behind by earlier runs are still present, taking places this program needs, so a controller can look dead or move on its own."), present.Length));
			if (records.Length == 1)
				lines.Add(UiLanguage.T("LeftoverVirtualPadsIssue.OneRecord", "Windows still keeps a record of a virtual controller no longer present, left by a run that ended without unplugging it; it is walked by every read of the machine and shown in Device Manager as if it were a fault."));
			else if (records.Length > 1)
				lines.Add(string.Format(UiLanguage.T("LeftoverVirtualPadsIssue.ManyRecords", "Windows still keeps {0} records of virtual controllers no longer present, left by runs that ended without unplugging them; each is walked by every read of the machine and shown in Device Manager as if it were a fault."), records.Length));
			// And named, because a count alone cannot be acted on. Where removing them does not work -
			// Windows refuses while something holds one open, or the thing found is not really a leftover -
			// a person is left with a complaint that returns for ever and nothing to look at. The name and
			// the identifier are what let somebody find it in Device Manager, or say what it is when the
			// removal will not take.
			var named = string.Join(Environment.NewLine, pads
				.Select(x => "    " + Named(x)
					+ Environment.NewLine + "        " + x.DeviceId)
				.ToArray());
			// A present leftover takes a place and breaks the emulation; a record only clutters.
			SetSeverity(present.Length > 0 ? IssueSeverity.Moderate : IssueSeverity.Low, 0,
				string.Join(Environment.NewLine + Environment.NewLine, lines.ToArray())
				+ Environment.NewLine + Environment.NewLine + named);
		}

		/// <summary>A leftover's name as Windows gives it, and whether it is still present.</summary>
		static string Named(JocysCom.ClassLibrary.IO.DeviceInfo pad)
		{
			var name = string.IsNullOrEmpty(pad.Description)
				? UiLanguage.T("LeftoverVirtualPadsIssue.UnnamedDevice", "Unnamed device")
				: pad.Description;
			return pad.IsPresent ? name : string.Format(UiLanguage.T("LeftoverVirtualPadsIssue.NotPresent", "{0} (not present)"), name);
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
