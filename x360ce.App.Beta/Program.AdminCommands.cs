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
				ProcessAdminCommands(args);
				return true;
			}
			else
			{
				// Run copy of x360ce as Administrator.
				JocysCom.ClassLibrary.Windows.UacHelper.RunProcess(
					Application.ExecutablePath,
					argument, isElevated: true		
				);
				return false;
			}
		}

		static bool ProcessAdminCommands(string[] args)
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
			if (ic.Parameters.ContainsKey(AdminCommand.InstallHidGuardian.ToString()))
			{
				DInput.VirtualDriverInstaller.InstallHidGuardian();
				return true;
			}
			if (ic.Parameters.ContainsKey(AdminCommand.UninstallHidGuardian.ToString()))
			{
				DInput.VirtualDriverInstaller.UninstallHidGuardian();
				return true;
			}
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
