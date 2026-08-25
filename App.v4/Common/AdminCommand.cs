namespace x360ce.App
{
    /// <summary>
    ///  x360ce.exe command line arguments used when program have to run as an administrator.
    /// </summary>
    public enum AdminCommand
    {
        InstallViGEmBus,
		UninstallViGEmBus,
        UninstallHidGuardian,
		UninstallDevice,
#if DEBUG
		/// <summary>Development builds only. Install is not offered in a release.</summary>
		InstallHidGuardian,
#endif
    }
}
