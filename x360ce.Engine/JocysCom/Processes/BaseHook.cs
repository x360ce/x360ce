using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace JocysCom.ClassLibrary.Processes
{
	public class BaseHook : IDisposable
	{

		public bool EnableEvents = true;

		internal static class NativeMethods
		{

			/// <summary>Retrieves a module handle for the specified module. The module must have been loaded by the calling process.</summary>
			/// <returns>If the function succeeds, the return value is a handle to the specified module.</returns>
			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern IntPtr GetModuleHandle(string lpModuleName);

			/// <summary>Installs an application-defined hook procedure into a hook chain.</summary>
			/// <returns>If the function succeeds, the return value is the handle to the hook procedure. </returns>
			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern IntPtr SetWindowsHookEx(uint hookid, HookProcDelegate pfnhook, HandleRef hinst, uint threadid);

			/// <summary>Installs an application-defined hook procedure into a hook chain.</summary>
			/// <returns>If the function succeeds, the return value is the handle to the hook procedure. </returns>
			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern IntPtr SetWindowsHookEx(uint hookid, HookProcDelegate pfnhook, IntPtr hinst, uint threadid);

			/// <summary>Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. </summary>
			/// <returns>If the function succeeds, the return value is non-zero. If the function fails, the return value is zero.</returns>
			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
			internal static extern bool UnhookWindowsHookEx(HandleRef hhook);

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern IntPtr SetWinEventHook(
				uint eventMin,
				uint eventMax,
				IntPtr hmodWinEventProc,
				WinEventProcDelegate lpfnWinEventProc,
				uint idProcess,
				uint idThread,
				uint dwFlags
			);

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern bool UnhookWinEvent(HandleRef hWinEventHook);

			/// <summary>
			/// Passes the hook information to the next hook procedure in the current hook chain.
			/// A hook procedure can call this function either before or after processing the hook information.
			/// </summary>
			/// <returns>
			/// This value is returned by the next hook procedure in the chain.
			/// The current hook procedure must also return this value.
			/// The meaning of the return value depends on the hook type.
			/// </returns>
			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
			internal static extern IntPtr CallNextHookEx(HandleRef hhook, int code, IntPtr wparam, IntPtr lparam);

			// When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
			[DllImport("user32.dll")]
			internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

			[DllImport("kernel32.dll")]
			internal static extern uint GetCurrentThreadId();

		}

		public const uint EVENT_OBJECT_SHOW = 0x8002;
		public const uint EVENT_OBJECT_HIDE = 0x8004;
		public const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
		public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

		public const uint WINEVENT_INCONTEXT = 0x0004;
		public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

		/// <summary>
		/// Used for processing WH_MOUSE and WH_KEYBOARD_LL messages.
		/// </summary>
		internal delegate IntPtr HookProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);

		HookProcDelegate _Hook1Procedure;

		internal delegate void WinEventProcDelegate(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hwnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime
		);

		/// <summary>
		/// Used for processing EVENT_OBJECT_NAMECHANGE messages.
		/// </summary>
		WinEventProcDelegate _Hook2Procedure;

		protected HandleRef hook1handleRef;
		protected HandleRef hook2handleRef;
		protected HandleRef hook3handleRef;

		public virtual void Start(bool global = false) { throw new NotImplementedException(); }
		public void Stop() { UnInstallHook(); }
		public HookType HookType { get { return _hookType; } }
		private HookType _hookType;

		object HookLock = new object();

		protected void InstallHook(HookType hookType, bool global)
		{
			lock (HookLock)
			{
				// If hook is installed already then return.
				_hookType = hookType;
				if (hook1handleRef.Handle != IntPtr.Zero) return;
				string lpModuleName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
				var hMod = NativeMethods.GetModuleHandle(lpModuleName);
				// Assign Virtual function.
				_Hook1Procedure = new HookProcDelegate(Hook1Procedure);
				IntPtr hook1Handle;
				if (hookType == HookType.WH_MOUSE)
				{
					uint threadId = NativeMethods.GetCurrentThreadId();
					// Listen for events.
					hook1Handle = NativeMethods.SetWindowsHookEx(
						(uint)hookType,
						_Hook1Procedure,
						hMod,
						// Associate hook procedure with current application/thread only.
						threadId
					);
					if (hook1Handle == IntPtr.Zero)
					{
						var ex = new Win32Exception();
						throw new Exception(ex.Message);
					}
					_Hook2Procedure = new WinEventProcDelegate(Hook2Procedure);
					// Listen for name change changes across all processes/threads on current desktop...
					var hook2Handle = NativeMethods.SetWinEventHook(
						EVENT_OBJECT_NAMECHANGE,
						EVENT_OBJECT_NAMECHANGE,
						IntPtr.Zero,
						_Hook2Procedure,
						0,
						 // Associate hook procedure with current application/thread only.
						global ? 0 : threadId,
						WINEVENT_OUTOFCONTEXT
					);
					if (hook2Handle == IntPtr.Zero)
					{
						var ex = new Win32Exception();
						throw new Exception(ex.Message);
					}
					hook2handleRef = new HandleRef(null, hook2Handle);

					// Listen for name change changes across all processes/threads on current desktop...
					var hook3Handle = NativeMethods.SetWinEventHook(
						EVENT_OBJECT_SHOW,
						EVENT_OBJECT_SHOW,
						IntPtr.Zero,
						_Hook2Procedure,
						0,
						// Associate hook procedure with current application/thread only.
						global ? 0 : threadId,
						WINEVENT_OUTOFCONTEXT
					);
					if (hook3Handle == IntPtr.Zero)
					{
						var ex = new Win32Exception();
						throw new Exception(ex.Message);
					}
					hook3handleRef = new HandleRef(null, hook3Handle);

				}
				else
				{
					// Listen for events.
					hook1Handle = NativeMethods.SetWindowsHookEx((uint)hookType, _Hook1Procedure, hMod, 0);
					if (hook1Handle == IntPtr.Zero)
					{
						var ex = new Win32Exception();
						throw new Exception(ex.Message);
					}
				}
				hook1handleRef = new HandleRef(null, hook1Handle);
			}
		}

		private void UnInstallHook()
		{
			lock (HookLock)
			{
				// If hook is installed then...
				if (hook1handleRef.Handle != IntPtr.Zero)
				{
					NativeMethods.UnhookWindowsHookEx(hook1handleRef);
				}
				// If hook is installed then...
				if (hook2handleRef.Handle != IntPtr.Zero)
				{
					NativeMethods.UnhookWinEvent(hook2handleRef);
				}
				// If hook is installed then...
				if (hook3handleRef.Handle != IntPtr.Zero)
				{
					NativeMethods.UnhookWinEvent(hook3handleRef);
				}
			}
		}

		protected virtual IntPtr Hook1Procedure(int nCode, IntPtr wParam, IntPtr lParam)
		{
			throw new NotImplementedException();
		}

		protected virtual void Hook2Procedure(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hwnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime
		)
		{
			throw new NotImplementedException();
		}

		#region IDisposable

		// Dispose() calls Dispose(true)
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		//// NOTE: Leave out the finalizer altogether if this class doesn't 
		//// own unmanaged resources itself, but leave the other methods
		//// exactly as they are. 
		//~Encryption()
		//{
		//    // Finalizer calls Dispose(false)
		//    Dispose(false);
		//}

		// The bulk of the clean-up code is implemented in Dispose(bool)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				UnInstallHook();
			}
		}

		#endregion


	}
}
