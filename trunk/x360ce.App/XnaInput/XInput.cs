using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace x360ce.App.XnaInput
{
	public class XInput
	{
		private static ControllerCollection controllerList = new ControllerCollection();
		public const int ERROR_SUCCESS = 0;

		private XInput()
		{
		}

		internal static IntPtr libHandle;

		//[SuppressUnmanagedCodeSecurity, DllImport("xinput1_3.dll", EntryPoint="XInputGetState", CharSet=CharSet.Auto)]
		//internal static extern int GetState(int dwUserIndex, ref State pState);
		//[SuppressUnmanagedCodeSecurity, DllImport("xinput1_3.dll", EntryPoint="XInputSetState", CharSet=CharSet.Auto)]
		//internal static extern int SetState(int dwUserIndex, ref Vibration pVibration);

		internal delegate int _GetState(int dwUserIndex, ref GamePadState pState);
		internal delegate int _SetState(int dwUserIndex, ref Vibration pVibration);

		internal static string LastError;

		internal static int SetState(int dwUserIndex, ref Vibration pVibration)
		{
			IntPtr procAddress = Win32.NativeMethods.GetProcAddress(libHandle, "XInputSetState");
			if (procAddress == IntPtr.Zero)
			{
				LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
				return -1;
			}
			_SetState m = (_SetState)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(_SetState));
			return m(dwUserIndex, ref pVibration);
		}

		internal static int GetState(int dwUserIndex, ref GamePadState pState)
		{
			IntPtr procAddress = Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetState");
			if (procAddress == IntPtr.Zero)
			{
				LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
				return -1;
			}
			_GetState m = (_GetState)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(_GetState));
			return m(dwUserIndex, ref pState);
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
			libHandle = Win32.NativeMethods.LoadLibrary(fileName);
			if (libHandle == IntPtr.Zero)
			{
				MessageBox.Show("Failed to load '{0}'", fileName);
			}
		}

		public static void FreeLibrary()
		{
			if (libHandle == IntPtr.Zero) return;
			Win32.NativeMethods.FreeLibrary(libHandle);
			libHandle = IntPtr.Zero;
		}


		public static ControllerCollection Controllers
		{
			get
			{
				return controllerList;
			}
		}

	}
}

