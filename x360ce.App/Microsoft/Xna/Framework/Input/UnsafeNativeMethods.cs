using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;
using x360ce.App.Controls;

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
		internal delegate ErrorCodes _Enable(bool enable);
		internal delegate ErrorCodes _Reset();

		internal static string LastError;

		[SecurityCritical, SecuritySafeCritical]
		internal static ErrorCodes GetCaps(PlayerIndex playerIndex, uint flags, out XINPUT_CAPABILITIES pCaps)
		{
			return GetMethod<_GetCaps>("XInputGetCapabilities")(playerIndex, flags, out pCaps);
		}

		internal static ErrorCodes SetState(PlayerIndex playerIndex, ref XINPUT_VIBRATION pVibration)
		{
			return GetMethod<_SetState>("XInputSetState")(playerIndex, out pVibration);
		}

		internal static ErrorCodes GetState(PlayerIndex playerIndex, out XINPUT_STATE pState)
		{
			return GetMethod<_GetState>("XInputGetState")(playerIndex, out pState);
		}

		internal static ErrorCodes Enable(bool enable)
		{
			return GetMethod<_Enable>("XInputEnable")(enable);
		}

		/// <summary>Custom function of x360ce library. Reloads settings from INI file.</summary>
		internal static ErrorCodes Reset()
		{
			return GetMethod<_Reset>("reset")();
		}

		internal static bool IsResetSupported
		{
			get
			{
				IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "reset");
				return procAddress != IntPtr.Zero;
			}
		}

		internal static T GetMethod<T>(string methodName)
		{
			IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, methodName);
			if (procAddress == IntPtr.Zero)
			{
				LastError = new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
				throw new Exception(LastError);
			}
			return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
		}


		static string _LibraryName;
		public static string LibraryName
		{
			get { return _LibraryName; }
		}

		public static bool ReLoadLibrary(string fileName)
		{
			_LibraryName = fileName;
			if (IsLoaded) FreeLibrary();
			libHandle = x360ce.App.Win32.NativeMethods.LoadLibrary(fileName);
			return IsLoaded;
		}

		public static void FreeLibrary()
		{
			if (!IsLoaded) return;
			x360ce.App.Win32.NativeMethods.FreeLibrary(libHandle);
			libHandle = IntPtr.Zero;
		}

		public static bool IsLoaded
		{
			get { return libHandle != IntPtr.Zero; }
		}


	}
}

