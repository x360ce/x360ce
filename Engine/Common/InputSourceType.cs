namespace x360ce.Engine
{
	/// <summary>
	/// Defines the input method used to read controller data.
	/// Each method has specific capabilities and limitations that must be understood by the user.
	/// </summary>
	[System.Flags]	
	public enum InputSourceType
	{
		Unknown = 0,
		/// <summary>
		/// DirectInput - Legacy Microsoft DirectInput API (SharpDX wrapper)
		/// 
		/// CAPABILITIES:
		/// • All controller types supported (gamepads, joysticks, wheels, etc.)
		/// • Unlimited device count
		/// • Generic controllers work perfectly
		/// 
		/// LIMITATIONS:
		/// • Xbox One controllers CANNOT be accessed in background on Windows 10+
		/// • Xbox 360/One controllers have triggers on same axis (no separate LT/RT)
		/// • No Guide button access for Xbox controllers
		/// • No rumble/force feedback for Xbox controllers via DirectInput
		/// • Windows Store Apps cannot use DirectInput
		/// • Microsoft no longer recommends using DirectInput (deprecated)
		/// </summary>
		DirectInput = 1,

		/// <summary>
		/// XInput - Microsoft XInput API for Xbox controllers
		/// 
		/// CAPABILITIES:
		/// • XInput controllers CAN be accessed in background
		/// • Proper trigger separation (LT/RT as separate axes)
		/// • Guide button access available
		/// • Full rumble support available
		/// • Best performance for Xbox controllers
		/// 
		/// LIMITATIONS:
		/// • Maximum 4 controllers ONLY (hard XInput API limit)
		/// • Only XInput capable devices (Xbox 360/One controllers)
		/// • Cannot activate extra 2 rumble motors in Xbox One controller triggers
		/// • No support for generic gamepads or specialized controllers
		/// </summary>
		XInput = 2,

		/// <summary>
		/// Gaming Input - Windows.Gaming.Input API (Windows 10+)
		/// 
		/// CAPABILITIES:
		/// • Unlimited number of controllers on Windows 10
		/// • Gamepad class: Xbox One certified/Xbox 360 compatible
		/// • RawGameController class: Other gamepads supported
		/// • Full Xbox One controller features (including trigger rumble)
		/// • Modern API with active Microsoft support
		/// 
		/// LIMITATIONS:
		/// • Controllers CANNOT be accessed in background (UWP limitation)
		/// • Only works on UWP devices (Windows 10+, Xbox One, tablets)
		/// • Desktop apps need special WinRT bridging (complex implementation)
		/// • May not work properly in non-UWP desktop applications
		/// </summary>
		GamingInput = 4,

		/// <summary>
		/// Raw Input - Windows Raw Input API for direct HID access
		/// 
		/// CAPABILITIES:
		/// • Controllers CAN be accessed in background
		/// • Unlimited number of controllers
		/// • Works with any HID-compliant device
		/// • No API deprecation concerns
		/// • Direct hardware access
		/// 
		/// LIMITATIONS:
		/// • Xbox 360/One controllers have triggers on same axis (same as DirectInput)
		/// • No Guide button access
		/// • Probably no rumble support
		/// • Requires manual HID report parsing (complex implementation)
		/// • No built-in controller abstraction (custom profiles needed)
		/// • Complex device capability detection required
		/// </summary>
		RawInput = 8
	}
}
