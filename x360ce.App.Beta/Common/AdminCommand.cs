using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App
{
    /// <summary>
    ///  x360ce.exe command line arguments used when program have to run as an administrator.
    /// </summary>
    public enum AdminCommand
    {
        InstallViGEmBus,
        UninstallViGEmBus,
        InstallHidGuardian,
        UninstallHidGuardian,
		UninstallDevice,
    }
}
