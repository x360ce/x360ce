using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static void RunElevated(AdminCommand command)
        {
            // If program is running as Administrator already.
            if (JocysCom.ClassLibrary.Security.PermissionHelper.IsElevated)
            {
                // Run command directly.
                var args = new string[] { command.ToString() };
                ProcessAdminCommands(args);
            }
            else
            {
                // Run copy of x360ce as Administrator.
                JocysCom.ClassLibrary.Win32.UacHelper.RunElevated(
                    Application.ExecutablePath,
                    command.ToString(),
                    System.Diagnostics.ProcessWindowStyle.Hidden
                );
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
            return false;
        }

    }
}
