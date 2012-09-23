using System;

namespace x360ce.App.Win32
{

	/// <summary>
	/// The broadcast option.
	/// </summary>
	[Flags]
	public enum BSF: int 
	{
		/// <summary>Enables the recipient to set the foreground window while processing the message.</summary>
		BSF_ALLOWSFW = 0x00000080,
		/// <summary>Flushes the disk after each recipient processes the message.</summary>
		BSF_FLUSHDISK = 0x00000004,
		/// <summary>Continues to broadcast the message, even if the time-out period elapses or one of the recipients is not responding.</summary>
		BSF_FORCEIFHUNG = 0x00000020,
		/// <summary>Does not send the message to windows that belong to the current task. This prevents an application from receiving its own message.</summary>
		BSF_IGNORECURRENTTASK = 0x00000002,
		/// <summary>Forces a nonresponsive application to time out. If one of the recipients times out, do not continue broadcasting the message.</summary>
		BSF_NOHANG = 0x00000008,
		/// <summary>Waits for a response to the message, as long as the recipient is not being unresponsive. Does not time out.</summary>
		BSF_NOTIMEOUTIFNOTHUNG = 0x00000040,
		/// <summary>Posts the message. Do not use in combination with BSF_QUERY.</summary>
		BSF_POSTMESSAGE = 0x00000010,
		/// <summary>Sends the message to one recipient at a time, sending to a subsequent recipient only if the current recipient returns TRUE.</summary>
		BSF_QUERY = 0x00000001,
		/// <summary>Sends the message using SendNotifyMessage function. Do not use in combination with BSF_QUERY.</summary>
		BSF_SENDNOTIFYMESSAGE = 0x00000100,
	}
}
