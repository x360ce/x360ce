using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	class VirtualDeviceDriverIssue : IssueItem
	{
		public VirtualDeviceDriverIssue() : base()
		{
			Name = "Virtual Device Driver";
			FixName = "Driver help";
			MoreInfo = new System.Uri(ViGEmBusSupport.DriverHelpUrl);
		}

		public override void CheckTask()
		{
			var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Virtual);
			if (!required)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var health = Nefarius.ViGEm.Client.ViGEmClient.GetBusHealth();
			if (!health.IsUsable)
			{
				var message = health.VersionIncompatible
					? "ViGEmBus is installed, but its client API version is incompatible."
					: health.ServicePresent
						? "ViGEmBus is present but unavailable (service: " + health.ServiceState + ")."
						: "ViGEmBus is not available. Mapping can still be configured without it.";
				SetSeverity(IssueSeverity.Moderate, 0, message);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}
		public override void FixTask()
		{
			ViGEmBusSupport.OpenDriverHelp(out _);
		}

	}
}
