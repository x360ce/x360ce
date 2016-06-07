using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace JocysCom.ClassLibrary.Processes
{
	public class BaseHook : IDisposable
	{

		public bool EnableEvents = true;

		/// <summary>Retrieves a module handle for the specified module. The module must have been loaded by the calling process.</summary>
		/// <returns>If the function succeeds, the return value is a handle to the specified module.</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		protected static extern IntPtr GetModuleHandle(string lpModuleName);

		/// <summary>Installs an application-defined hook procedure into a hook chain.</summary>
		/// <returns>If the function succeeds, the return value is the handle to the hook procedure. </returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		protected static extern IntPtr SetWindowsHookEx(uint hookid, HookProc pfnhook, HandleRef hinst, uint threadid);

		/// <summary>Installs an application-defined hook procedure into a hook chain.</summary>
		/// <returns>If the function succeeds, the return value is the handle to the hook procedure. </returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		protected static extern IntPtr SetWindowsHookEx(uint hookid, HookProc pfnhook, IntPtr hinst, uint threadid);

		/// <summary>Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. </summary>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
		protected static extern bool UnhookWindowsHookEx(HandleRef hhook);

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
		protected static extern IntPtr CallNextHookEx(HandleRef hhook, int code, IntPtr wparam, IntPtr lparam);

		// When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
		[DllImport("user32.dll")]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("kernel32.dll")]
		static extern uint GetCurrentThreadId();

		public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
		protected HandleRef hHook;
		protected HookProc HookProcedure;

		public virtual void Start() { throw new NotImplementedException(); }
		public void Stop() { UnInstallHook(); }
		public HookType HookType { get { return _hookType; } }
		private HookType _hookType;

		object HookLock = new object();

		protected void InstallHook(HookType hookType)
		{
			lock (HookLock)
			{
				// If hook is installed already then return.
				_hookType = hookType;
				if (hHook.Handle != IntPtr.Zero) return;
				string lpModuleName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
				var hMod = GetModuleHandle(lpModuleName);
				//var hRef = new HandleRef(this, hMod);
				HookProcedure = new HookProc(HookProcess);
				IntPtr kh;
				if (hookType == HookType.WH_MOUSE)
				{
					uint threadId = GetCurrentThreadId();
					kh = SetWindowsHookEx((uint)hookType, HookProcedure, hMod, threadId);
				}
				else
				{
					kh = SetWindowsHookEx((uint)hookType, HookProcedure, hMod, 0);
				}
				if (kh == IntPtr.Zero)
				{
					var ex = new System.ComponentModel.Win32Exception();
					throw new Exception(ex.Message);
				}
				hHook = new HandleRef(null, kh);
			}
		}

		private void UnInstallHook()
		{
			lock (HookLock)
			{
				// If hook is not installed already then return.
				if (hHook.Handle == IntPtr.Zero) return;
				UnhookWindowsHookEx(hHook);
			}
		}

		protected virtual IntPtr HookProcess(int nCode, IntPtr wParam, IntPtr lParam)
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
