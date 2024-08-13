using System;
using System.Diagnostics;
using System.IO;

namespace x360ce.App.DInput
{
	public class DInputEventArgs : EventArgs
	{
		public DInputEventArgs(Exception error = null)
		{
			Error = error;
		}
		public Exception Error { get; set; }

		public FileInfo XInputFileInfo { get; set; }
		public FileVersionInfo XInputVersionInfo { get; set; }
	}
}
