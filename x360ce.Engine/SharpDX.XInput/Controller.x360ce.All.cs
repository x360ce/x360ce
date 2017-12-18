namespace SharpDX.XInput
{
	using SharpDX.Win32;
	using System;
	using System.Runtime.ExceptionServices;
	using System.Runtime.InteropServices;
	using System.Threading;

	public partial class Controller
	{

		/* NOTE:
		   To enabled C++ debbuging from C# project, make sure that you have turned on
		   "[x] Enable Unmanaged/Native Code Debugging" in your C# project properties on [Debug] panel?
		*/

		// https://msdn.microsoft.com/en-us/library/windows/desktop/ee417001(v=vs.85).aspx
		public const int XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30; // 11.8% or 7680 on ushort.
		public const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849; // 12.0%
		public const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689; // 13.3%

		private readonly UserIndex userIndex;
		//private static IXInput xinput;

		/// <summary>
		/// Initializes a new instance of the <see cref="Controller"/> class.
		/// </summary>
		/// <param name="userIndex">Index of the user.</param>
		public Controller(UserIndex userIndex = UserIndex.Any)
		{
			this.userIndex = userIndex;
		}

		public static object XInputLock = new object();

		#region XInput functions

		internal delegate void XInputEnableDelegate(bool enable);
		internal delegate ErrorCode XInputGetAudioDeviceIdsDelegate(int dwUserIndex, IntPtr renderDeviceId, IntPtr renderCount, IntPtr captureDeviceId, IntPtr pCaptureCount);
		internal delegate ErrorCode XInputGetBatteryInformationDelegate(int dwUserIndex, int devType, out BatteryInformation pBatteryInformation);
		internal delegate ErrorCode XInputGetCapabilitiesDelegate(int dwUserIndex, int dwFlags, out Capabilities pCapabilities);
		internal delegate ErrorCode XInputGetDSoundAudioDeviceGuidsDelegate(int dwUserIndex, out Guid pDSoundRenderGuid, out Guid pDSoundCaptureGuid);
		internal delegate ErrorCode XInputGetKeystrokeDelegate(int dwUserIndex, int dwReserved, out Keystroke pKeystroke);
		internal delegate ErrorCode XInputGetStateDelegate(int dwUserIndex, out State pState);
		internal delegate ErrorCode XInputSetStateDelegate(int dwUserIndex, ref Vibration pVibration);

		[HandleProcessCorruptedStateExceptions]
		public static unsafe void XInputEnable(bool enable)
		{
			try
			{
				var method = GetMethod<XInputEnableDelegate>("XInputEnable");
				method(enable);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputGetBatteryInformation(int userIndex, BatteryDeviceType devType, out BatteryInformation batteryInformation)
		{
			batteryInformation = new BatteryInformation();
			try
			{
				var method = GetMethod<XInputGetBatteryInformationDelegate>("XInputGetBatteryInformation");
				return method(userIndex, (int)devType, out batteryInformation);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		public static unsafe ErrorCode XInputGetAudioDeviceIds(int dwUserIndex, IntPtr renderDeviceIdRef, IntPtr renderCountRef, IntPtr captureDeviceIdRef, IntPtr captureCountRef)
		{
			if (!IsGetAudioDeviceIdsSupported) return ErrorCode.NotSupported;
			try
			{
				var method = GetMethod<XInputGetAudioDeviceIdsDelegate>("XInputGetAudioDeviceIds");
				return method(dwUserIndex, renderDeviceIdRef, renderCountRef, captureDeviceIdRef, captureCountRef);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities pCapabilities)
		{
			pCapabilities = new Capabilities();
			try
			{
				var method = GetMethod<XInputGetCapabilitiesDelegate>("XInputGetCapabilities");
				return method(dwUserIndex, (int)dwFlags, out pCapabilities);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputGetDSoundAudioDeviceGuids(int dwUserIndex, out Guid dSoundRenderGuid, out Guid dSoundCaptureGuid)
		{
			dSoundRenderGuid = new Guid();
			dSoundCaptureGuid = new Guid();
			try
			{
				var method = GetMethod<XInputGetDSoundAudioDeviceGuidsDelegate>("XInputGetDSoundAudioDeviceGuids");
				return method(dwUserIndex, out dSoundRenderGuid, out dSoundCaptureGuid);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke pKeystroke)
		{
			pKeystroke = new Keystroke();
			try
			{
				var method = GetMethod<XInputGetKeystrokeDelegate>("XInputGetKeystroke");
				return method(dwUserIndex, dwReserved, out pKeystroke);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		static bool UseMicrosoft = true;

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputGetState(int dwUserIndex, out State pState)
		{
			var functionName = "XInputGetState";
			if (IsGetStateExSupported) functionName = "XInputGetStateEx";
			pState = new State();
			try
			{
				var method = GetMethod<XInputGetStateDelegate>(functionName);
				return method(dwUserIndex, out pState);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		[HandleProcessCorruptedStateExceptions]
		public static unsafe ErrorCode XInputSetState(int dwUserIndex, Vibration pVibration)
		{
			try
			{
				var method = GetMethod<XInputSetStateDelegate>("XInputSetState");
				return method(dwUserIndex, ref pVibration);
			}
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		#endregion

		#region Default Functions

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <returns>The state of this controller.</returns>
		public State GetState()
		{
			State temp;
			var result = ErrorCodeHelper.ToResult(XInputGetState((int)userIndex, out temp));
			result.CheckError();
			return temp;
		}

		/// <summary>
		/// Sets the vibration.
		/// </summary>
		/// <param name="vibration">The vibration.</param>
		/// <returns></returns>
		public Result SetVibration(Vibration vibration)
		{
			var result = ErrorCodeHelper.ToResult(XInputSetState((int)userIndex, vibration));
			result.CheckError();
			return result;
		}

		/// <summary>
		/// Gets a value indicating whether this instance is connected.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is connected; otherwise, <c>false</c>.
		/// </value>
		public bool IsConnected
		{
			get
			{
				State temp;
				return XInputGetState((int)userIndex, out temp) == 0;
			}
		}

		#endregion

		#region Custom Functions

		internal delegate ErrorCode ResetDelegate();

		/// <summary>Reloads settings from INI file.</summary>
		[HandleProcessCorruptedStateExceptions]
		public static ErrorCode Reset()
		{
			if (!IsResetSupported) return ErrorCode.NotSupported;
			try { return GetMethod<ResetDelegate>("Reset")(); }
			catch (AccessViolationException ex) { throw new Exception(ex.Message); }
			catch (Exception) { throw; }
		}

		#endregion

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

		internal static T GetMethod<T>(string methodName)
		{
			Exception error;
			IntPtr procAddress = JocysCom.ClassLibrary.Win32.NativeMethods.GetProcAddress(libHandle, methodName, out error);
			if (error != null)
			{
				// Don't throw Win32Exception directly or it can terminate app unexcpectedly.
				throw error;
			}
			return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
		}

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
					JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out freeError);
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
				procAddress = JocysCom.ClassLibrary.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetStateEx", out procException);
				_IsGetStateExSupported = procAddress != IntPtr.Zero;
				// Check if XInputGetAudioDeviceIds function is supported.
				procAddress = JocysCom.ClassLibrary.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetAudioDeviceIds", out procException);
				_IsGetAudioDeviceIdsSupported = procAddress != IntPtr.Zero;
				// Check if Reset function is supported.
				procAddress = JocysCom.ClassLibrary.Win32.NativeMethods.GetProcAddress(libHandle, "Reset", out procException);
				_IsResetSupported = procAddress != IntPtr.Zero;
			}
		}

		static void LoadLibraryCallBack(object state)
		{
			try
			{
				Exception loadException;
				libHandle = JocysCom.ClassLibrary.Win32.NativeMethods.LoadLibrary(_LibraryName, out loadException);
				if (libHandle == IntPtr.Zero)
				{
					LastLoadException = loadException;
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
				JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out error);
				libHandle = IntPtr.Zero;
			}
		}

		#endregion

	}
}

