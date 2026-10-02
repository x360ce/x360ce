using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.IO;
using System.Linq;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Real controllers this program switched off to put controllers in order and has not switched back on.
	/// </summary>
	/// <remarks>
	/// A controller that is switched off is seen by no game, and nothing on the screen says why: it is
	/// simply not there. This program switches one back on at the next start when it has Administrator,
	/// and says so in the header when it could not; this is the same fact where problems are listed.
	/// Information only, with no button: switching a device on needs Administrator, and the person is the
	/// one who knows whether it should be on.
	///
	/// Not asked while controllers are being put in order, when the controller is off for a moment on purpose.
	/// </remarks>
	class SwitchedOffControllersIssue : IssueItem
	{
		public SwitchedOffControllersIssue() : base()
		{
			Name = "Controllers Switched Off";
			FixName = null;
		}

		public override void CheckTask()
		{
			var helper = Global.DHelper;
			if (helper != null && helper.Suspended)
				return;
			var off = DInput.XInputReorderRunner.StillSwitchedOff();
			if (off.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// Named when Windows still knows the device, and by its identifier when it does not.
			var known = DeviceDetector.GetDevices(off, false).ToDictionary(x => x.DeviceId, x => x.Description, System.StringComparer.OrdinalIgnoreCase);
			var names = off.Select(id =>
			{
				string name;
				return known.TryGetValue(id, out name) && !string.IsNullOrEmpty(name) ? name + " (" + id + ")" : id;
			}).ToArray();
			SetSeverity(IssueSeverity.Low, 0,
				"This program switched off " + (off.Length == 1 ? "a controller" : off.Length + " controllers")
				+ " to put controllers in order and has not switched "
				+ (off.Length == 1 ? "it" : "them") + " back on, so no game sees "
				+ (off.Length == 1 ? "it" : "them") + ". Start this program as Administrator and "
				+ (off.Length == 1 ? "it is" : "they are") + " switched back on, or switch "
				+ (off.Length == 1 ? "it" : "them") + " on in Device Manager."
				+ System.Environment.NewLine + System.Environment.NewLine
				+ string.Join(System.Environment.NewLine, names.Select(x => "    " + x).ToArray()));
		}

	}
}
