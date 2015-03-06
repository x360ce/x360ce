using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public enum GameRefreshStatus
	{
		None = 0,
		OK = 0x1,
		FileNotExist = 0x2,
		XInputFileNotExist = 0x4,
		XInputFileWrongPlatform = 0x8,
		XInputFileOlderVersion = 0x10,
		XInputFileNewerVersion = 0x20,
		XInputFileUnnecessary = 0x40,
	}
}
