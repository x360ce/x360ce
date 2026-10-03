// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using SharpDX.Mathematics.Interop;
using System.Runtime.InteropServices;

namespace SharpDX.XInput
{
    internal partial class XInput14 : IXInput
    {
		public int XInputSetState(int dwUserIndex, Vibration vibrationRef)
        {
            return Native.XInputSetState(dwUserIndex, vibrationRef);
        }

        public int XInputGetState(int dwUserIndex, out State stateRef)
        {
            return Native.XInputGetState(dwUserIndex, out stateRef);
        }

        public int XInputGetAudioDeviceIds(int dwUserIndex, IntPtr renderDeviceIdRef, IntPtr renderCountRef, IntPtr captureDeviceIdRef, IntPtr captureCountRef)
        {
            return Native.XInputGetAudioDeviceIds(dwUserIndex, renderDeviceIdRef, renderCountRef, captureDeviceIdRef, captureCountRef);
        }

        public void XInputEnable(RawBool enable)
        {
			Native.XInputEnable(enable);
        }

        public int XInputGetBatteryInformation(int dwUserIndex, BatteryDeviceType devType, out BatteryInformation batteryInformationRef)
        {
            return Native.XInputGetBatteryInformation(dwUserIndex, devType, out batteryInformationRef);
        }

        public int XInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke keystrokeRef)
        {
            return Native.XInputGetKeystroke(dwUserIndex, dwReserved, out keystrokeRef);
        }

        public int XInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities capabilitiesRef)
        {
            return Native.XInputGetCapabilities(dwUserIndex, dwFlags, out capabilitiesRef);
        }

		private static partial class Native
		{
			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetKeystroke")]
			public static extern int XInputGetKeystroke(int dwUserIndex, int dwReserved, out Keystroke keystrokeRef);

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetBatteryInformation")]
			public static extern int XInputGetBatteryInformation(int dwUserIndex, BatteryDeviceType devType, out BatteryInformation batteryInformationRef);

			public static unsafe int XInputSetState(int dwUserIndex, Vibration vibrationRef)
			{
				return XInputSetState_(dwUserIndex, (void*)(&vibrationRef));
			}

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputSetState")]
			private static extern unsafe int XInputSetState_(int arg0, void* arg1);

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetState")]
			public static extern int XInputGetState(int dwUserIndex, out State stateRef);

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetAudioDeviceIds")]
			public static extern unsafe int XInputGetAudioDeviceIds(int dwUserIndex, IntPtr renderDeviceId, IntPtr renderCount, IntPtr captureDeviceId, IntPtr pCaptureCount);

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputEnable")]
			public static extern void XInputEnable(RawBool arg0);

			[DllImport("xinput1_4.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetCapabilities")]
			public static extern int XInputGetCapabilities(int dwUserIndex, DeviceQueryType dwFlags, out Capabilities capabilitiesRef);
		}

	}
}
