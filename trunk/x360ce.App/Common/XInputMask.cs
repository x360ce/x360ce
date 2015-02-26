using System;
using System.ComponentModel;
namespace x360ce.App
{
	[Flags]
	public enum XInputMask: uint
	{
		None = 0x000,
		[Description("xinput1_1.dll")]
		Xinput11 = 0x001,
		[Description("xinput1_2.dll")]
		Xinput12 = 0x002,
		[Description("xinput1_3.dll")]
		Xinput13 = 0x004,
		[Description("xinput1_4.dll")]
		Xinput14 = 0x008,
		[Description("xinput9_1_0.dll")]
		Xinput91 = 0x100,
	}
}
