using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	class HidGuardianDriverIssue : IssueItem
	{
		public HidGuardianDriverIssue() : base()
		{
			Name = "HidHide (Recommended)";
			FixName = "Install HidHide";
		}

		public override void CheckTask()
		{
			var haveVirtual = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Virtual && x.EnableMask > 0);
			var haveHidden = SettingsManager.UserDevices.Items.Any(x => x.IsHidden);
			// HidHide is recommended if virtual emulation is enabled and some devices must be hidden.
			var required = haveVirtual && haveHidden;
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			// Don’t rely on HidGuardian presence; simply recommend HidHide.
			SetSeverity(IssueSeverity.Moderate, 0, "Optional: Install HidHide to hide DirectInput controllers when using virtual emulation.");
		}
		public override void FixTask()
		{
			ControlsHelper.BeginInvoke(() =>
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(DInput.VirtualDriverInstaller.HidHideDownloadUrl) { UseShellExecute = true });
			});
		}

	}
}
