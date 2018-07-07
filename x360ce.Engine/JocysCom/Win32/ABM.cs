namespace JocysCom.ClassLibrary.Win32
{
	public enum ABM: uint
	{
		/// <summary>Registers a new appbar and specifies the message identifier that the system should use to send notification messages to the appbar.</summary>
		ABM_NEW = 0,
		/// <summary>Unregisters an appbar, removing the bar from the system's internal list.</summary>
		ABM_REMOVE = 1,
		/// <summary>Requests a size and screen position for an appbar.</summary>
		ABM_QUERYPOS = 2,
		/// <summary>Sets the size and screen position of an appbar.</summary>
		ABM_SETPOS = 3,
		/// <summary>Retrieves the autohide and always-on-top states of the Windows taskbar.</summary>
		ABM_GETSTATE = 4,
		/// <summary>Retrieves the bounding rectangle of the Windows taskbar.</summary>
		ABM_GETTASKBARPOS = 5,
		/// <summary>Notifies the system to activate or deactivate an appbar. The lParam member of the APPBARDATA pointed to by pData is set to TRUE to activate or FALSE to deactivate.</summary>
		ABM_ACTIVATE = 6,
		/// <summary>Retrieves the handle to the autohide appbar associated with a particular edge of the screen.</summary>
		ABM_GETAUTOHIDEBAR = 7,
		/// <summary>Registers or unregisters an autohide appbar for an edge of the screen.</summary>
		ABM_SETAUTOHIDEBAR = 8,
		/// <summary>Notifies the system when an appbar's position has changed.</summary>
		ABM_WINDOWPOSCHANGED = 9,
		/// <summary>Windows XP and later: Sets the state of the appbar's autohide and always-on-top attributes.</summary>
		ABM_SETSTATE = 10,
	}
}
