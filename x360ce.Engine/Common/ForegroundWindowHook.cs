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

		public static Process GetActiveProcess(IntPtr? hWnd = null)
		{
			if (!hWnd.HasValue)
				hWnd = JocysCom.ClassLibrary.Win32.NativeMethods.GetForegroundWindow();
			var _ = NativeMethods.GetWindowThreadProcessId(hWnd.Value, out var processId);
			var process = Process.GetProcessById((int)processId);
			return process;
		}

		[Flags]
		internal enum ProcessAccessFlags : uint
		{
			QueryLimitedInformation = 0x00001000
		}

		public static string GetProcessFileName(Process p)
		{
			var capacity = 2048;
			var builder = new StringBuilder(capacity);
			var ptr = NativeMethod.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
			if (!NativeMethod.QueryFullProcessImageName(ptr, 0, builder, ref capacity))
				return string.Empty;
			return builder.ToString();
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
