using System;

namespace x360ce.App
{
	[Flags]
	public enum HookMask: uint
	{
		NONE = 0x00000000,
		/// <summary>Load Library</summary>
		LL = 0x00000001,
		COM = 0x00000002,
		DI = 0x00000004,
		PIDVID = 0x00000008,
		NAME = 0x00000010,
		/// <summary>SetupAPI</summary>
		SA = 0x00000020,
		/// <summary>WinVerifyTrust</summary>
		WT = 0x01000000,
		STOP = 0x02000000,
		DISABLE = 0x80000000,
	}
}
