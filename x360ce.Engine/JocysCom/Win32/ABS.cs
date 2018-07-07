using System;
namespace JocysCom.ClassLibrary.Win32
{
	[Flags]
	public enum ABS: uint
	{
		None = 0,
		ABS_AUTOHIDE = 1,
		ABS_ALWAYSONTOP = 2,
	}
}
