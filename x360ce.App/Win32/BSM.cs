using System;

namespace x360ce.App.Win32
{
	/// <summary>
	/// Recipients of the message.
	/// </summary>
	public enum BSM: int
	{
		/// <summary>Broadcast to all system components.</summary>
		BSM_ALLCOMPONENTS = 0x00000000,
		/// <summary>Broadcast to all desktops. Requires the SE_TCB_NAME privilege.</summary>
		BSM_ALLDESKTOPS = 0x00000010,
		/// <summary>Broadcast to applications.</summary>
		BSM_APPLICATIONS = 0x00000008,
	}
}
