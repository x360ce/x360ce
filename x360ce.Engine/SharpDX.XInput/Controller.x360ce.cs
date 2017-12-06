using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using x360ce.Engine;
using x360ce.Engine.Win32;

namespace SharpDX.XInput
{
	/// <summary>
	/// A XInput controller.
	/// </summary>
	public partial class Controller
	{


		/// <summary>Reloads settings from INI file.</summary>
		public static Result Reset()
		{
			var result = ErrorCodeHelper.ToResult(xinput.Reset());
			result.CheckError();
			return result;
		}

		/* NOTE:
		   To enabled C++ debbuging from C# project, make sure that you have turned on
		   "[x] Enable Unmanaged/Native Code Debugging" in your C# project properties on [Debug] panel?
		*/

		public static object XInputLock = new object();

		// https://msdn.microsoft.com/en-us/library/windows/desktop/ee417001(v=vs.85).aspx
		public const int XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30; // 11.8% or 7680 on ushort.
		public const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849; // 12.0%
		public const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689; // 13.3%

		#region Dynamic Methods

		static bool _IsResetSupported;
		public static bool IsResetSupported { get { return _IsResetSupported; } }

		static bool _IsGetStateExSupported;
		internal static bool IsGetStateExSupported { get { return _IsGetStateExSupported; } }

		static bool _IsGetAudioDeviceIdsSupported;
		internal static bool IsGetAudioDeviceIdsSupported { get { return _IsGetAudioDeviceIdsSupported; } }

		static string _LibraryName;
		public static string LibraryName { get { return _LibraryName; } }

		internal static IntPtr libHandle;
		public static bool IsLoaded { get { return libHandle != IntPtr.Zero; } }

		private static object loadLock = new object();
		private static ManualResetEvent resetEvent = new ManualResetEvent(false);
		private static Exception LastLoadException;

		public static void ReLoadLibrary(string fileName, out Exception error)
		{
			lock (loadLock)
			{
				if (IsLoaded)
				{
					Exception freeError;
					NativeMethods.FreeLibrary(libHandle, out freeError);
					libHandle = IntPtr.Zero;
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
				_LibraryName = fileName;
				// Wrap into separate thread in order to avoid error:
				// LoaderLock was detected Message: Attempting managed execution inside OS Loader lock.
				// Do not attempt to run managed code inside a DllMain or image initialization function
				// since doing so can cause the application to hang.
				LastLoadException = null;
				resetEvent.Reset();
				var success = System.Threading.ThreadPool.QueueUserWorkItem(LoadLibraryCallBack);
				resetEvent.WaitOne(5000);
				error = LastLoadException;
				IntPtr procAddress;
				Exception procException;
				// Check if XInputGetStateEx function is supported.
				procAddress = NativeMethods.GetProcAddress(libHandle, "XInputGetStateEx", out procException);
				_IsGetStateExSupported = procAddress != IntPtr.Zero;
				// Check if XInputGetAudioDeviceIds function is supported.
				procAddress = NativeMethods.GetProcAddress(libHandle, "XInputGetAudioDeviceIds", out procException);
				_IsGetAudioDeviceIdsSupported = procAddress != IntPtr.Zero;
				// Check if Reset function is supported.
				procAddress = NativeMethods.GetProcAddress(libHandle, "Reset", out procException);
				_IsResetSupported = procAddress != IntPtr.Zero;
			}
		}

		static void LoadLibraryCallBack(object state)
		{
			try
			{
				Exception loadException;
				libHandle = NativeMethods.LoadLibrary(_LibraryName, out loadException);
				if (libHandle == IntPtr.Zero)
				{
					LastLoadException = loadException;
				}
				else
				{
					var values = (XInputMask[])Enum.GetValues(typeof(XInputMask));
					foreach (var value in values)
					{
						var fileName = JocysCom.ClassLibrary.Runtime.Attributes.GetDescription(value);
						if (_LibraryName.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) > -1)
						{
							if (value == XInputMask.XInput91_x64 || value == XInputMask.XInput91_x86)
								xinput = new XInput910();
							if (value == XInputMask.XInput13_x64 || value == XInputMask.XInput13_x86)
								xinput = new XInput13();
							if (value == XInputMask.XInput14_x64 || value == XInputMask.XInput14_x86)
								xinput = new XInput14();
							if (xinput != null)
								xinput.XInputEnable(true);
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				LastLoadException = ex;
			}
			resetEvent.Set();
		}

		public static void FreeLibrary()
		{
			lock (loadLock)
			{
				if (!IsLoaded) return;
				Exception error;
				NativeMethods.FreeLibrary(libHandle, out error);
				libHandle = IntPtr.Zero;
			}
		}

		#endregion

	}
}
