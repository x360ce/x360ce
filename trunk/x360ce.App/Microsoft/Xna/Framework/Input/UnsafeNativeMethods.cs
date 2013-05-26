using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
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

		[HandleProcessCorruptedStateExceptions]
		internal static ErrorCodes GetCaps(PlayerIndex playerIndex, uint flags, out XINPUT_CAPABILITIES pCaps)
		{
			try { return GetMethod<_GetCaps>("XInputGetCapabilities")(playerIndex, flags, out pCaps); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		internal static ErrorCodes SetState(PlayerIndex playerIndex, ref XINPUT_VIBRATION pVibration)
		{
			try { return GetMethod<_SetState>("XInputSetState")(playerIndex, out pVibration); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		internal static ErrorCodes GetState(PlayerIndex playerIndex, out XINPUT_STATE pState)
		{
			var name = _IsGetStateExSupported ? "XInputGetStateEx" : "XInputGetState";
			try { return GetMethod<_GetState>(name)(playerIndex, out pState); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		internal static ErrorCodes Enable(bool enable)
		{
			try { return GetMethod<_Enable>("XInputEnable")(enable); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		#region Custom Functions

		/// <summary>Reloads settings from INI file.</summary>
		[HandleProcessCorruptedStateExceptions]
		internal static ErrorCodes Reset()
		{
			try { return GetMethod<_Reset>("reset")(); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		#endregion

		static bool _IsResetSupported;
		internal static bool IsResetSupported { get { return _IsResetSupported; } }

		static bool _IsGetStateExSupported;
		internal static bool IsGetStateExSupported { get { return _IsGetStateExSupported; } }

		static string _LibraryName;
		public static string LibraryName { get { return _LibraryName; } }

		internal static T GetMethod<T>(string methodName)
		{
			IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, methodName);
			if (procAddress == IntPtr.Zero) throw new Win32Exception();
			return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
		}

		public static bool ReLoadLibrary(string fileName)
		{
			_LibraryName = fileName;
			if (IsLoaded) FreeLibrary();
			libHandle = x360ce.App.Win32.NativeMethods.LoadLibrary(fileName);
			var procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetStateEx");
			// TODO: Uncomment this later.
            //_IsGetStateExSupported = procAddress != IntPtr.Zero;
			procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "reset");
			_IsResetSupported = procAddress != IntPtr.Zero;
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

