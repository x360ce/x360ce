using System;
using System.ComponentModel;

namespace x360ce.Engine
{
	/// <summary>
	/// Important: Names of Enums are linked to checkbox names on x360ce App.
	/// Important: Put latest version on the top.
	/// </summary>
	[Flags]
	public enum XInputMask: uint
	{
		None = 0x000,
		// 32-bit build.
		[Description("xinput1_4.dll")]
		XInput14_x86 = 0x08,
		[Description("xinput1_3.dll")]
		XInput13_x86 = 0x04,
		[Description("xinput1_2.dll")]
		XInput12_x86 = 0x02,
		[Description("xinput1_1.dll")]
		XInput11_x86 = 0x01,
		[Description("xinput9_1_0.dll")]
		XInput91_x86 = 0x40,
		// 64-bit build.
		[Description("xinput1_4.dll")]
		XInput14_x64 = 0x0800,
		[Description("xinput1_3.dll")]
		XInput13_x64 = 0x0400,
		[Description("xinput1_2.dll")]
		XInput12_x64 = 0x0200,
		[Description("xinput1_1.dll")]
		XInput11_x64 = 0x0100,
		[Description("xinput9_1_0.dll")]
		XInput91_x64 = 0x4000,
	}
}
