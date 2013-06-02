namespace SharpDX.XInput
{
    using SharpDX;
    using SharpDX.Win32;
    using System;
    using System.ComponentModel;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Security;

    internal static class XInput
    {

        #region XInput functions

        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputEnable", CallingConvention = CallingConvention.StdCall)]
        //private static extern void XInputEnable_(Bool enable);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetAudioDeviceIds", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetAudioDeviceIds_(int dwUserIndex, IntPtr renderDeviceId, IntPtr renderCount, IntPtr captureDeviceId, IntPtr pCaptureCount);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetBatteryInformation_(int dwUserIndex, int devType, out BatteryInformation pBatteryInformation);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetCapabilities", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetCapabilities_(int dwUserIndex, int dwFlags, out Capabilities pCapabilities);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetDSoundAudioDeviceGuids", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetDSoundAudioDeviceGuids_(int dwUserIndex, out Guid pDSoundRenderGuid, out Guid pDSoundCaptureGuid);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetKeystroke", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetKeystroke_(int dwUserIndex, int dwReserved, out Keystroke pKeystroke);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputGetState_(int dwUserIndex, out State pState);
        //[SuppressUnmanagedCodeSecurity, DllImport("xinput1_4.dll", EntryPoint = "XInputSetState", CallingConvention = CallingConvention.StdCall)]
        //private static extern unsafe int XInputSetState_(int dwUserIndex, ref Vibration pVibration);

        internal delegate void XInputEnableDelegate(Bool enable);
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
            try { GetMethod<XInputEnableDelegate>("XInputEnable")(enable); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputGetBatteryInformation(int userIndex, BatteryDeviceType devType, out BatteryInformation batteryInformation)
        {
            batteryInformation = new BatteryInformation();
            try { return GetMethod<XInputGetBatteryInformationDelegate>("XInputGetBatteryInformation")(userIndex, (int)devType, out batteryInformation); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        public static unsafe ErrorCode XInputGetAudioDeviceIds(int dwUserIndex, IntPtr renderDeviceIdRef, IntPtr renderCountRef, IntPtr captureDeviceIdRef, IntPtr captureCountRef)
        {
            if (!IsGetAudioDeviceIdsSupported) return ErrorCode.NotSupported;
            try { return GetMethod<XInputGetAudioDeviceIdsDelegate>("XInputGetAudioDeviceIds")(dwUserIndex, renderDeviceIdRef, renderCountRef, captureDeviceIdRef, captureCountRef); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities pCapabilities)
        {
            pCapabilities = new Capabilities();
            try { return GetMethod<XInputGetCapabilitiesDelegate>("XInputGetCapabilities")(dwUserIndex, (int)dwFlags, out pCapabilities); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputGetDSoundAudioDeviceGuids(int dwUserIndex, out Guid dSoundRenderGuid, out Guid dSoundCaptureGuid)
        {
            dSoundRenderGuid = new Guid();
            dSoundCaptureGuid = new Guid();
            try { return GetMethod<XInputGetDSoundAudioDeviceGuidsDelegate>("XInputGetDSoundAudioDeviceGuids")(dwUserIndex, out dSoundRenderGuid, out dSoundCaptureGuid); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke pKeystroke)
        {
            pKeystroke = new Keystroke();
            try { return GetMethod<XInputGetKeystrokeDelegate>("XInputGetKeystroke")(dwUserIndex, dwReserved, out pKeystroke); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputGetState(int dwUserIndex, out State pState)
        {
            var functionName = "XInputGetState";
            //if (IsGetStateExSupported) functionName = "XInputGetStateEx";
            pState = new State();
            try { return GetMethod<XInputGetStateDelegate>(functionName)(dwUserIndex, out pState); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        [HandleProcessCorruptedStateExceptions]
        public static unsafe ErrorCode XInputSetState(int dwUserIndex, Vibration pVibration)
        {
            try { return GetMethod<XInputSetStateDelegate>("XInputSetState")(dwUserIndex, ref pVibration); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        #endregion

        #region Custom Functions

        internal delegate ErrorCode _Reset();

        /// <summary>Reloads settings from INI file.</summary>
        [HandleProcessCorruptedStateExceptions]
        internal static ErrorCode Reset()
        {
            if (!IsResetSupported) return ErrorCode.NotSupported;
            try { return GetMethod<_Reset>("reset")(); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        #endregion

        #region Dynamic Methods

        static bool _IsResetSupported;
        internal static bool IsResetSupported { get { return _IsResetSupported; } }

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
            IntPtr procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, methodName);
            if (procAddress == IntPtr.Zero)
            {
                // Don't throw Win32 exception directly or it can terminate app unexcpectedly.
                var ex = new Win32Exception();
                throw new Exception(ex.ToString());
            }
            return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
        }

        public static void ReLoadLibrary(string fileName)
        {
            _LibraryName = fileName;
            if (IsLoaded) FreeLibrary();
            libHandle = x360ce.App.Win32.NativeMethods.LoadLibrary(fileName);
            IntPtr procAddress;
            // Check if XInputGetStateEx function is supported.
            procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetStateEx");
            _IsGetStateExSupported = procAddress != IntPtr.Zero;
            // Check if XInputGetAudioDeviceIds function is supported.
            procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "XInputGetAudioDeviceIds");
            _IsGetAudioDeviceIdsSupported = procAddress != IntPtr.Zero;
            // Check if Reset function is supported.
            procAddress = x360ce.App.Win32.NativeMethods.GetProcAddress(libHandle, "reset");
            _IsResetSupported = procAddress != IntPtr.Zero;
        }

        public static void FreeLibrary()
        {
            if (!IsLoaded) return;
            x360ce.App.Win32.NativeMethods.FreeLibrary(libHandle);
            libHandle = IntPtr.Zero;
        }

        #endregion



    }
}

