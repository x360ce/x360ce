using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace Microsoft.Xna.Framework.Input
{
	public static class UnsafeNativeMethods
	{
		public const int ERROR_SUCCESS = 0;

		internal static IntPtr libHandle;

        //[DllImport("xinput1_3.dll", EntryPoint = "XInputGetCapabilities")]
        //public static extern ErrorCodes GetCaps(PlayerIndex playerIndex, uint flags, out XINPUT_CAPABILITIES pCaps);
        //[DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        //public static extern ErrorCodes GetState(PlayerIndex playerIndex, out XINPUT_STATE pState);
        //[DllImport("xinput1_3.dll", EntryPoint = "XInputSetState")]
        //public static extern ErrorCodes SetState(PlayerIndex playerIndex, ref XINPUT_VIBRATION pVibration);

        internal delegate ErrorCodes _GetCaps(PlayerIndex playerIndex, uint flags, out XINPUT_CAPABILITIES pCaps);
        internal delegate ErrorCodes _GetState(PlayerIndex playerIndex, out XINPUT_STATE pState);
        internal delegate ErrorCodes _SetState(PlayerIndex playerIndex, out XINPUT_VIBRATION pVibration);

		internal static string LastError;

        internal static ErrorCodes GetCaps(PlayerIndex playerIndex, uint flags, out XINPUT_CAPABILITIES pCaps)
        {
            IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetCapabilities");
            if (procAddress == IntPtr.Zero)
            {
                LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception(LastError);
            }
            _GetCaps m = (_GetCaps)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(_GetCaps));
            return m(playerIndex, flags, out pCaps);
        }

        internal static ErrorCodes SetState(PlayerIndex playerIndex, ref XINPUT_VIBRATION pVibration)
		{
            IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputSetState");
			if (procAddress == IntPtr.Zero)
			{
				LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception(LastError);
			}
			_SetState m = (_SetState)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(_SetState));
            return m(playerIndex, out pVibration);
		}

        internal static ErrorCodes GetState(PlayerIndex playerIndex, out XINPUT_STATE pState)
		{
            IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetState");
			if (procAddress == IntPtr.Zero)
			{
				LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
                throw new Exception(LastError);
			}
			_GetState m = (_GetState)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(_GetState));
            return m(playerIndex, out pState);
		}

		static string _LibraryName;
		public static string LibraryName
		{
			get { return _LibraryName; }
		}

		public static void ReLoadLibrary()
		{
			ReLoadLibrary(LibraryName);
		}

		public static void ReLoadLibrary(string fileName)
		{
			_LibraryName = fileName;
			if (libHandle != IntPtr.Zero) FreeLibrary();
            libHandle = x360ce.App.Win32.NativeMethods.LoadLibrary(fileName);
			if (libHandle == IntPtr.Zero)
			{
				MessageBox.Show("Failed to load '{0}'", fileName);
			}
		}

		public static void FreeLibrary()
		{
			if (libHandle == IntPtr.Zero) return;
            x360ce.App.Win32.NativeMethods.FreeLibrary(libHandle);
			libHandle = IntPtr.Zero;
		}

	}
}

