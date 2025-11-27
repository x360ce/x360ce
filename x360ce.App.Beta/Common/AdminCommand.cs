using System;

namespace x360ce.App
{
    /// <summary>
    ///  x360ce.exe command line arguments used when program have to run as an administrator.
    /// </summary>
    public enum AdminCommand
    {
        InstallViGEmBus,
		UninstallViGEmBus,
        [Obsolete("HidGuardian deprecated. Use HidHide instead.")]
        InstallHidGuardian,
        [Obsolete("HidGuardian deprecated. Use HidHide instead.")]
        UninstallHidGuardian,
		UninstallDevice,
    }
}
