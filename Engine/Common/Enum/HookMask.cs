using System;

namespace x360ce.Engine
{
	/// <summary>
	/// Important: Names of Enums are linked to checkbox names on x360ce App.
	/// </summary>
	[Flags]
	public enum HookMask : uint
	{
		NONE = 0x00000000,
		/// <summary>Load Library</summary>
		HookLL = 0x00000001,
		HookCOM = 0x00000002,
		HookDI = 0x00000004,
		HookPIDVID = 0x00000008,
		HookNAME = 0x00000010,
		/// <summary>SetupAPI</summary>
		HookSA = 0x00000020,
		/// <summary>WinVerifyTrust</summary>
		HookWT = 0x01000000,
		HookSTOP = 0x02000000,
		HookDISABLE = 0x80000000,
	}
}
