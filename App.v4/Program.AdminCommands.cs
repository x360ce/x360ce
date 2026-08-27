using System;
using System.Windows.Forms;

namespace x360ce.App
{

	/// <summary>
	/// Some actions require for x360ce.exe to run as Administrator.
	/// In most cases x360ce.exe will run with permissions of normal user.
	/// In order to get around this issue, x360ce.exe will run second copy of itself with 
	/// Administrative permissions.
	/// </summary>
	static partial class Program
	{

		/// <summary>
		/// Returns true if command was executed locally.
		/// </summary>
		public static bool RunElevated(AdminCommand command, string param = null)
		{
			// If program is running as Administrator already.
			var argument = command.ToString();
			if (param != null)
			{
				argument = string.Format("{0}=\"{1}\"", command, param);
			}
			if (JocysCom.ClassLibrary.Security.PermissionHelper.IsElevated)
			{
				// Run command directly.
				var args = new string[] { argument };
				ProcessAdminCommands(true, args);
				return true;
			}
			else
			{
				// Run copy of x360ce as Administrator.
				JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
					Application.ExecutablePath,
					argument,
					System.Diagnostics.ProcessWindowStyle.Hidden
				);
				return false;
			}
		}

		static bool ProcessAdminCommands(bool direct, string[] args)
		{
			// Requires System.Configuration.Installl reference.
			var ic = new System.Configuration.Install.InstallContext(null, args);
			// ------------------------------------------------
			// Virtual Drivers
			// ------------------------------------------------
			if (ic.Parameters.ContainsKey(AdminCommand.InstallViGEmBus.ToString()))
			{
				DInput.VirtualDriverInstaller.InstallViGEmBus();
				return true;
			}
			if (ic.Parameters.ContainsKey(AdminCommand.UninstallViGEmBus.ToString()))
			{
				DInput.VirtualDriverInstaller.UninstallViGEmBus();
				return true;
			}
			if (ic.Parameters.ContainsKey(AdminCommand.RepairViGEmBus.ToString()))
			{
				var repaired = DInput.VirtualDriverInstaller.RepairViGEmBus();
				Console.WriteLine(repaired
					? "Virtual bus removed and put back."
					: "Virtual bus could not be put back. Restart Windows and try again.");
				Environment.ExitCode = repaired ? 0 : 1;
				return true;
			}
			if (ic.Parameters.ContainsKey(AdminCommand.RemoveLeftoverPads.ToString()))
			{
				bool rebootNeeded;
				Exception error;
				var removed = DInput.VirtualDriverInstaller.RemoveLeftoverVirtualPads(out rebootNeeded, out error);
				// Windows reports needing a restart with a failure code even though the device has
				// gone, so the exit code says only whether anything went wrong that was not that.
				Console.WriteLine("Removed {0} leftover virtual pad(s).", removed);
				if (rebootNeeded)
					Console.WriteLine("Restart Windows to finish removing them.");
				if (error != null)
					Console.WriteLine("Last failure: {0}", error.Message);
				Environment.ExitCode = error == null ? 0 : 1;
				return true;
			}
			if (ic.Parameters.ContainsKey(AdminCommand.UninstallHidGuardian.ToString()))
			{
				DInput.VirtualDriverInstaller.UninstallHidGuardian();
				return true;
			}
#if DEBUG
			// Development builds only. The caller verifies the resulting driver state.
			if (ic.Parameters.ContainsKey(AdminCommand.InstallHidGuardian.ToString()))
			{
				DInput.VirtualDriverInstaller.InstallHidGuardian();
				return true;
			}
#endif
			if (ic.Parameters.ContainsKey(AdminCommand.UninstallDevice.ToString()))
			{
				var hwid = ic.Parameters[AdminCommand.UninstallDevice.ToString()];
				DInput.VirtualDriverInstaller.UnInstallDevice(hwid);
				return true;
			}
			return false;
		}

	}
}
