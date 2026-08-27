namespace x360ce.App
{
    /// <summary>
    ///  x360ce.exe command line arguments used when program have to run as an administrator.
    /// </summary>
    public enum AdminCommand
    {
        InstallViGEmBus,
		UninstallViGEmBus,
		/// <summary>Remove the virtual bus and put it back, to recover one that has stopped working.</summary>
		RepairViGEmBus,
        UninstallHidGuardian,
		UninstallDevice,
		/// <summary>Remove virtual pads left behind by runs that did not shut down cleanly.</summary>
		RemoveLeftoverPads,
#if DEBUG
		/// <summary>Development builds only. Install is not offered in a release.</summary>
		InstallHidGuardian,
#endif
    }
}
