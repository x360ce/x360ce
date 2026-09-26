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
		/// <summary>Let every user listen for AI assistants on every network at the given port, which http.sys allows only once an Administrator has said so.</summary>
		ReserveAiAccessUrl,
		/// <summary>Switch devices on and off for the copy that started this one, until it says to stop.</summary>
		/// <remarks>
		/// Windows will not let an ordinary program switch a device off, and putting controllers in a
		/// chosen order needs exactly that, more than once. The parameter is the name of the pipe the
		/// starting copy listens on.
		/// </remarks>
		DeviceHelper,
#if DEBUG
		/// <summary>Development builds only. Install is not offered in a release.</summary>
		InstallHidGuardian,
#endif
    }
}
