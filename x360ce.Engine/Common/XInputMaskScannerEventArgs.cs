using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public class XInputMaskScannerEventArgs : EventArgs
	{
		public XInputMaskScannerState State { get; set; }
		public UserGame Game { get; set; }

		public Program Program { get; set; }

		public FileInfo GameFileInfo { get; set; }

		public List<DirectoryInfo> Directories;

		public List<FileInfo> Files;

		public int CurentIndex { get; set; }

		public int Skipped { get; set; }

		public int Added { get; set; }
		public int Updated { get; set; }

		public string Message { get; set; }

	}
}
