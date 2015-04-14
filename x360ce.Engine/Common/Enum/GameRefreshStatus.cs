using System;
using System.ComponentModel;

namespace x360ce.Engine
{
	[Flags]
	public enum GameRefreshStatus
	{
		[Description("All is OK")]
		OK = 0x1,
		[Description("Executable was not found.")]
		ExeNotExist = 0x2,
		[Description("Game Database file not found.")]
		GdbNotExist = 0x4,
		[Description("Game Database file is different.")]
		GdbDifferent = 0x8,
		[Description("INI settings file not found.")]
		IniNotExist = 0x10,
		[Description("INI settings file is different.")]
		IniDifferent = 0x20,
		[Description("XML settings file not found.")]
		XmlNotExist = 0x40,
		[Description("XML settings file is different.")]
		XmlDifferent = 0x80,
		[Description("XInput files are missing.")]
		XInputFilesNotExist = 0x100,
		[Description("XInput files are for different processor arhitecture.")]
		XInputFilesWrongPlatform = 0x200,
		[Description("Older version of XInput files were found.")]
		XInputFilesOlderVersion = 0x400,
		[Description("Newer version of XInput files were found.")]
		XInputFilesNewerVersion = 0x800,
		[Description("Unecessary XInput files were found.")]
		XInputFilesUnnecessary = 0x1000,
	}
}
