using System;
using System.ComponentModel;

namespace x360ce.Engine
{
	/// <summary>
	/// Important: Names of Enums are linked to checkbox names on x360ce App.
	/// </summary>
	[Flags]
	public enum DInputMask : uint
	{
		None = 0x000,
		// 32-bit build.
		[Description("dinput8.dll")]
		DInput8_x86 = 0x01,
		// 64-bit build.
		[Description("dinput8.dll")]
		DInput8_x64 = 0x0100,
	}
}
