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
		//XInput11FileNotExist = 0x20,
		//XInput11WrongPlatform = 0x40,
		//XInput11OlderVersion = 0x80,
		//XInput12FileNotExist = 0x200,
		//XInput12WrongPlatform = 0x400,
		//XInput12OlderVersion = 0x800,
		//XInput13FileNotExist = 0x2000,
		//XInput13WrongPlatform = 0x4000,
		//XInput13OlderVersion = 0x8000,
		//XInput14FileNotExist = 0x20000,
		//XInput14WrongPlatform = 0x40000,
		//XInput14OlderVersion = 0x80000,
		//XInput91FileNotExist = 0x200000,
		//XInput91WrongPlatform = 0x400000,
		//XInput91OlderVersion = 0x800000,
	}
}
