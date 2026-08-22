using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Processes;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace x360ce.Engine
{
	public partial class ForegroundWindowHook : BaseHook
	{

		public ForegroundWindowHook()
		{
		}

		public bool IsEnabled
		{
			get
			{
				lock (isEnabledLock)
					return _IsEnabled;
			}
			set
			{
				lock (isEnabledLock)
				{
					// If already enabled then return.
					if (_IsEnabled && value)
						return;
					// If already disabled then return.
					if (!_IsEnabled && !value)
						return;
					// Start or stop service.
					var success = value
						? Start()
						: Stop();
					// Update value if success.
					if (success)
						_IsEnabled = value;
				}

			}
		}
		private bool _IsEnabled;
		private readonly object isEnabledLock = new object();

		private bool Start()
		{
			InstallHook(HookType.WH_CBT, true);
			return true;
		}

		private new bool Stop()
		{
			base.Stop();
			return true;
		}

		/// <summary>
		/// CBTProc callback function.
		/// </summary>
		/// <param name="nCode">The code that the hook procedure uses to determine how to process the message.</param>
		/// <param name="wParam">Specifies the handle to the window about to be activated.</param>
		/// <param name="lParam">Specifies a long pointer to a CBTACTIVATESTRUCT structure containing the handle to the active window and specifies whether the activation is changing because of a mouse click.</param>
		/// <returns></returns>
		protected override void Hook2Procedure(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hWnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime
		)
		{
			if (eventType == EVENT_SYSTEM_FOREGROUND)
			{
				var process = GetActiveProcess(hWnd);
				OnActivate?.Invoke(this, new EventArgs<Process>(process));
			}
		}

		public event EventHandler<EventArgs<Process>> OnActivate;

		/// <summary>Process which owns the window, or null when it can no longer be resolved.</summary>
		/// <remarks>
		/// The window can be closed between reading it and resolving its process, so
		/// GetProcessById throws for a process id which was valid moments earlier. This is
		/// reached from a system hook callback, where an escaping exception takes the
		/// application down, so failure is reported as null instead.
		/// </remarks>
		public static Process GetActiveProcess(IntPtr? hWnd = null)
		{
			if (!hWnd.HasValue)
				hWnd = JocysCom.ClassLibrary.Win32.NativeMethods.GetForegroundWindow();
			if (hWnd.Value == IntPtr.Zero)
				return null;
			var _ = NativeMethods.GetWindowThreadProcessId(hWnd.Value, out var processId);
			if (processId == 0)
				return null;
			try
			{
				return Process.GetProcessById((int)processId);
			}
			catch (ArgumentException)
			{
				// Process exited between the window lookup and this call.
				return null;
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		[Flags]
		internal enum ProcessAccessFlags : uint
		{
			QueryLimitedInformation = 0x00001000
		}

		/// <summary>Full path of the process image, or an empty string when unavailable.</summary>
		/// <remarks>
		/// The handle from OpenProcess is always released. This runs for every process on the
		/// machine each time the foreground window changes, so leaking one handle per call
		/// exhausts the handle table over a session.
		/// </remarks>
		public static string GetProcessFileName(Process p)
		{
			if (p == null)
				return string.Empty;
			var capacity = 2048;
			var builder = new StringBuilder(capacity);
			var ptr = IntPtr.Zero;
			try
			{
				ptr = NativeMethod.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
				// Access is denied for protected and elevated processes.
				if (ptr == IntPtr.Zero)
					return string.Empty;
				if (!NativeMethod.QueryFullProcessImageName(ptr, 0, builder, ref capacity))
					return string.Empty;
				return builder.ToString();
			}
			catch (InvalidOperationException)
			{
				// Process exited while its identifier was being read.
				return string.Empty;
			}
			finally
			{
				if (ptr != IntPtr.Zero)
					JocysCom.ClassLibrary.Win32.NativeMethods.CloseHandle(ptr);
			}
		}

		internal static class NativeMethod
		{

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern bool QueryFullProcessImageName(
				  [In] IntPtr hProcess,
				  [In] int dwFlags,
				  [Out] StringBuilder lpExeName,
				  ref int lpdwSize);

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern IntPtr OpenProcess(
			 ProcessAccessFlags processAccess,
			 bool bInheritHandle,
			 int processId
				);

		}

	}
}
