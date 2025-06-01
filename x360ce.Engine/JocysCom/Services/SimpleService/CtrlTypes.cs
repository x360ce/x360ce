/// <summary>
/// An enumerated type for the control messages sent to the handler routine.
/// </summary>
namespace JocysCom.ClassLibrary.Services.SimpleService
{
	/// <summary>
	/// The type of control signal received by the handler.
	/// </summary>
	public enum CtrlTypes
	{
		/// <summary>A CTRL+C signal was received.</summary>
		CTRL_C_EVENT = 0,

		/// <summary>A CTRL+BREAK signal was received.</summary>
		CTRL_BREAK_EVENT,

		/// <summary>
		/// A signal that the system sends to all processes attached to a console when
		/// the user closes the console (either by clicking Close on the console window's window menu, or
		/// by clicking the End Task button command from Task Manager)
		/// </summary>
		CTRL_CLOSE_EVENT,

		/// <summary>
		/// A signal that the system sends to all console processes when a user is logging off.
		/// This signal does not indicate which user is logging off, so no assumptions can be made.
		/// Note that this signal is received only by services. Interactive applications are terminated at logoff,
		/// so they are not present when the system sends this signal.
		/// </summary>
		CTRL_LOGOFF_EVENT = 5,

		/// <summary>
		/// A signal that the system sends when the system is shutting down.
		/// Interactive applications are not present by the time the system sends this signal,
		/// therefore it can be received only be services in this situation.
		/// Services also have their own notification mechanism for shutdown events. For more information, see Handler.
		/// </summary>
		CTRL_SHUTDOWN_EVENT
	}
}
