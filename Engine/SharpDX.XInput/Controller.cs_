// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
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
using System.Runtime.InteropServices;
using SharpDX.Win32;

namespace SharpDX.XInput
{
	/// <summary>
	/// A XInput controller.
	/// </summary>
	public partial class Controller
	{

		#region SharpDX

		private readonly UserIndex userIndex;
		private static IXInput xinput;

		/// <summary>
		/// Initializes a new instance of the <see cref="Controller"/> class.
		/// </summary>
		/// <param name="userIndex">Index of the user.</param>
		public Controller(UserIndex userIndex = UserIndex.Any)
		{
			this.userIndex = userIndex;
		}

		/// <summary>
		/// Gets the <see cref="UserIndex"/> associated with this controller.
		/// </summary>
		/// <value>The index of the user.</value>
		public UserIndex UserIndex { get { return this.userIndex; } }

		/// <summary>
		/// Gets the battery information.
		/// </summary>
		/// <param name="batteryDeviceType">Type of the battery device.</param>
		/// <returns></returns>
		/// <unmanaged>unsigned int XInputGetBatteryInformation([In] XUSER_INDEX dwUserIndex,[In] BATTERY_DEVTYPE devType,[Out] XINPUT_BATTERY_INFORMATION* pBatteryInformation)</unmanaged>	
		public BatteryInformation GetBatteryInformation(BatteryDeviceType batteryDeviceType)
		{
			BatteryInformation temp;
			var result = ErrorCodeHelper.ToResult(xinput.XInputGetBatteryInformation((int)userIndex, batteryDeviceType, out temp));
			result.CheckError();
			return temp;
		}

		/// <summary>
		/// Gets the capabilities.
		/// </summary>
		/// <param name="deviceQueryType">Type of the device query.</param>
		/// <returns></returns>
		/// <unmanaged>unsigned int XInputGetCapabilities([In] XUSER_INDEX dwUserIndex,[In] XINPUT_DEVQUERYTYPE dwFlags,[Out] XINPUT_CAPABILITIES* pCapabilities)</unmanaged>	
		public Capabilities GetCapabilities(DeviceQueryType deviceQueryType)
		{
			Capabilities temp;
			var result = ErrorCodeHelper.ToResult(xinput.XInputGetCapabilities((int)userIndex, deviceQueryType, out temp));
			result.CheckError();
			return temp;
		}

		/// <summary>
		/// Gets the capabilities.
		/// </summary>
		/// <param name="deviceQueryType">Type of the device query.</param>
		/// <param name="capabilities">The capabilities of this controller.</param>
		/// <returns><c>true</c> if the controller is connected, <c>false</c> otherwise.</returns>
		public bool GetCapabilities(DeviceQueryType deviceQueryType, out Capabilities capabilities)
		{
			return xinput.XInputGetCapabilities((int)userIndex, deviceQueryType, out capabilities) == 0;
		}

		/// <summary>
		/// Gets the keystroke.
		/// </summary>
		/// <param name="deviceQueryType">The flag.</param>
		/// <param name="keystroke">The keystroke.</param>
		/// <returns></returns>
		/// <unmanaged>unsigned int XInputGetKeystroke([In] XUSER_INDEX dwUserIndex,[In] unsigned int dwReserved,[Out] XINPUT_KEYSTROKE* pKeystroke)</unmanaged>	
		public Result GetKeystroke(DeviceQueryType deviceQueryType, out Keystroke keystroke)
		{
			var result = ErrorCodeHelper.ToResult(xinput.XInputGetKeystroke((int)userIndex, (int)deviceQueryType, out keystroke));
			return result;
		}

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <returns>The state of this controller.</returns>
		public State GetState()
		{
			State temp;
			var result = ErrorCodeHelper.ToResult(xinput.XInputGetState((int)userIndex, out temp));
			result.CheckError();
			return temp;
		}

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <param name="state">The state of this controller.</param>
		/// <returns><c>true</c> if the controller is connected, <c>false</c> otherwise.</returns>
		public bool GetState(out State state)
		{
			return xinput.XInputGetState((int)userIndex, out state) == 0;
		}

		/// <summary>
		/// Sets the reporting.
		/// </summary>
		/// <param name="enableReporting">if set to <c>true</c> [enable reporting].</param>
		public static void SetReporting(bool enableReporting)
		{
			if (xinput != null)
			{
				xinput.XInputEnable(enableReporting);
			}
		}

		/// <summary>
		/// Sets the vibration.
		/// </summary>
		/// <param name="vibration">The vibration.</param>
		/// <returns></returns>
		public Result SetVibration(Vibration vibration)
		{
			var result = ErrorCodeHelper.ToResult(xinput.XInputSetState((int)userIndex, vibration));
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
				if (xinput == null)
					return false;
				State temp;
				return xinput.XInputGetState((int)userIndex, out temp) == 0;
			}
		}

		#endregion

	}
}
