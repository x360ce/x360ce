using JocysCom.ClassLibrary.Controls.IssuesControl;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Issues
{
	/// <summary>No virtual bus driver is on this computer while a game uses virtual emulation.</summary>
	/// <remarks>
	/// Asked of the driver, not of a bus client. Connecting from here would make a client on the issue
	/// thread and hold the lock the input thread takes on every pass. An installed driver that does
	/// not answer is a different fault with a different fix, and <see cref="VirtualDriverNotWorkingIssue"/>
	/// reports it.
	/// </remarks>
	public class VirtualDeviceDriverIssue : IssueItem
	{
		public VirtualDeviceDriverIssue() : base()
		{
			Name = "Virtual Device Driver";
			FixName = "Install";
		}

		public override void CheckTask()
		{
			if (!IsRequired(SettingsManager.UserGames.Items))
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			if (DInput.VirtualDriverInstaller.GetInstalledViGEmBusVersion() == null)
			{
				SetSeverity(IssueSeverity.Moderate, 0, "You need to install Virtual Driver for emulation to work.");
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		/// <summary>Whether any game uses virtual emulation, tested by bit as the engine tests it.</summary>
		/// <remarks>Uses the same Virtual test as the engine's <see cref="DInput.DInputHelper.WantsVirtual"/>, so the two never disagree.</remarks>
		public static bool IsRequired(IEnumerable<UserGame> games)
		{
			return games.Any(x => (x.EmulationType & (int)EmulationType.Virtual) != 0);
		}

		public override void FixTask()
		{
			DInput.DInputHelper.CheckInstallVirtualDriver();
		}

	}
}
