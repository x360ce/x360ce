using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Services.SimpleService
{
	public class SimpleServiceHandler
	{
		/// <summary>
		/// A delegate type to be used as the handler routine for SetConsoleCtrlHandler.
		/// </summary>
		/// <param name="CtrlType">The type of control signal received by the handler.</param>
		/// <returns>If the function handles the control signal, it should return TRUE. If it returns FALSE, the next handler function in the list of handlers for this process is used.</returns>
		public delegate bool PHANDLER_ROUTINE(CtrlTypes CtrlType);

		/// <summary>
		/// Adds or removes an application-defined HandlerRoutine function from the list of handler functions for the calling process.
		/// </summary>
		/// <param name="HandlerRoutine">A pointer to the application-defined HandlerRoutine function to be added or removed. This parameter can be NULL.</param>
		/// <param name="Add">If this parameter is TRUE, the handler is added; if it is FALSE, the handler is removed.</param>
		/// <remarks>
		/// If no handler function is specified, the function sets an inheritable attribute that
		/// determines whether the calling process ignores CTRL+C signals.
		/// </remarks>
		/// <returns>If the function succeeds, the return value is nonzero.</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern bool SetConsoleCtrlHandler(PHANDLER_ROUTINE HandlerRoutine, bool Add);
	}
}
