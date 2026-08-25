using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public enum XInputMaskScannerState
	{
		None = 0,
		Started,
		Completed,
		GameFound,
		GameUpdated,
		DirectoryUpdate,
		FileUpdate
	}

}
