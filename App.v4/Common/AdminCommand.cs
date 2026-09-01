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
		/// <summary>Switch real controllers off, so the places they hold are given up.</summary>
		/// <remarks>
		/// Windows will not let an ordinary program switch a device off, and putting controllers in a
		/// chosen order needs exactly that. The parameter is the device identifiers, separated by commas.
		/// </remarks>
		DisableDevices,
		/// <summary>Switch real controllers back on, one at a time, in the order given.</summary>
		/// <remarks>
		/// One at a time and in order, because the order they arrive in is the order XInput gives out the
		/// places - it is the only lever there is. The parameter is the device identifiers, in the order
		/// they should come back, separated by commas.
		/// </remarks>
		EnableDevices,
#if DEBUG
		/// <summary>Development builds only. Install is not offered in a release.</summary>
		InstallHidGuardian,
#endif
    }
}
