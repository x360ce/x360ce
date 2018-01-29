using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	class VirtualDeviceDriverIssue : WarningItem
	{
		public VirtualDeviceDriverIssue() : base()
		{
			Name = "Virtual Device Driver";
			FixName = "Install";
		}

		public override void Check()
		{
			var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Virtual);
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			if (!Nefarius.ViGEm.Client.ViGEmClient.isVBusExists())
			{
				SetSeverity(IssueSeverity.Critical, 0, "You need to install Virtual Driver for emulation to work.");
				return;
			}
			SetSeverity(IssueSeverity.None);
		}
		public override void Fix()
		{
			MainForm.Current.DHelper.CheckInstallVirtualDriver();
			RaiseFixApplied();
		}

	}
}
