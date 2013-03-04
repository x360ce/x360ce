using System;
namespace x360ce.App
{
	[Flags]
	public enum XInputMask: uint
	{
		None = 0x000,
		Xinput11 = 0x001,
		Xinput12 = 0x002,
		Xinput13 = 0x004,
		Xinput14 = 0x008,
		Xinput91 = 0x100,
	}
}
