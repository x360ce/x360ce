namespace SharpDX.XInput
{
    using SharpDX;
    using SharpDX.Win32;
    using System;
    using System.ComponentModel;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;
    using x360ce.Engine.Win32;

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

        internal delegate ErrorCode ResetDelegate();

        /// <summary>Reloads settings from INI file.</summary>
        [HandleProcessCorruptedStateExceptions]
        internal static ErrorCode Reset()
        {
            if (!IsResetSupported) return ErrorCode.NotSupported;
            try { return GetMethod<ResetDelegate>("Reset")(); }
            catch (AccessViolationException ex) { throw new Exception(ex.Message); }
            catch (Exception) { throw; }
        }

        /// <summary>Get XInput thumb value by DINput value</summary>
        /// <remarks>Used to create graphs pictures.</remarks>
        public static short GetThumbValue(short dInputValue, int deadZone, int antiDeadZone, int linear)
        {
            //        [ 32768 steps | 32768 steps ]
            // DInput [ 0     32767 | 32768 65535 ] 
            // XInput [ 32768    -1 | 0     32767 ]
            //
            int xInput = dInputValue;
            //int xInput = *(targetAxis[i]);
            //int deadZone = (int)device.axisdeadzone[i];
            //int antiDeadZone = (int)device.antideadzone[i];
            //int linear = (int)device.axislinear[i];
            int max = 32767;
            // If deadzone value is set then...
            bool invert = xInput < 0;
            // Convert [-32768;-1] -> [32767;0]
            if (invert) xInput = -1 - xInput;
            //if  invert 
            if (deadZone > 0)
            {
                if (xInput > deadZone)
                {
                    // [deadZone;32767] => [0;32767];
                    xInput = (int)((float)(xInput - deadZone) / (float)(max - deadZone) * (float)max);
                }
                else
                {
                    xInput = 0;
                }
            }
            // If anti-deadzone value is set then...
            if (antiDeadZone > 0)
            {
                if (xInput > 0)
                {
                    // [0;32767] => [antiDeadZone;32767];
                    xInput = (int)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
                }
            }
            // If linear value is set then...
            if (linear != 0 && xInput > 0)
            {
                // [antiDeadZone;32767] => [0;32767];
                float xInputF = (float)(xInput - antiDeadZone) / (float)(max - antiDeadZone) * (float)max;
                float linearF = (float)linear / 100f;
                xInputF = ConvertToFloat((short)xInputF);
                float x = -xInputF;
                if (linearF < 0f) x = 1f + x;
                float v = ((float)Math.Sqrt(1f - x * x));
                if (linearF < 0f) v = 1f - v;
                xInputF = xInputF + (2f - v - xInputF - 1f) * Math.Abs(linearF);
                xInput = ConvertToShort(xInputF);
                // [0;32767] => [antiDeadZone;32767];
                xInput = (int)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
            }
            // Convert [32767;0] -> [-32768;-1]
            if (invert) xInput = -1 - xInput;
            //*(targetAxis[i]) = (SHORT)clamp(xInput, min, max);
            return (short)xInput;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">[-1.0;1.0]</param>
        /// <param name="strength">[-1.0;1.0]</param>
        /// <returns>[-1.0;1.0]</returns>
        static float GetValue(float value, float param)
        {
            var x = value;
            if (value > 0f) x = 0f - x;
            if (param < 0f) x = 1f + x;
            var v = ((float)Math.Sqrt(1f - x * x));
            if (param < 0f) v = 1f - v;
            if (value > 0f) v = 2f - v;
            var val = value + (v - value - 1f) * Math.Abs(param);
            return val;
        }

        /// <summary>Convert short [-32768;32767] to float range [-1.0f;1.0f].</summary>
        public static float ConvertToFloat(short v)
        {
            float maxValue = v >= 0 ? (float)short.MaxValue : -((float)short.MinValue);
            return ((float)v) / maxValue;
        }

        /// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
        public static short ConvertToShort(float v)
        {
            float maxValue = v >= 0 ? (float)short.MaxValue : -((float)short.MinValue);
            return (short)(v * maxValue);
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
            Exception error;
            IntPtr procAddress = NativeMethods.GetProcAddress(libHandle, methodName, out error);
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
                libHandle = NativeMethods.LoadLibrary(_LibraryName, out LastLoadException);
                if (libHandle == IntPtr.Zero)
                {
                    var win32ex = new Win32Exception();
                    LastLoadException = new Exception(win32ex.Message);
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

