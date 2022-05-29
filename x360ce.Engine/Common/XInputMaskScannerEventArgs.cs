using System;
using System.Collections.Generic;
using System.IO;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public class XInputMaskScannerEventArgs : EventArgs
	{
		public int Level { get; set; }

		public XInputMaskScannerState State { get; set; }
		public UserGame Game { get; set; }

		public Program Program { get; set; }

		public FileInfo GameFileInfo { get; set; }

		public List<DirectoryInfo> Directories;
		public long DirectoryIndex { get; set; }

		public List<FileInfo> Files;
		public long FileIndex { get; set; }

		public int Skipped { get; set; }

		public int Added { get; set; }
		public int Updated { get; set; }

		public string Message { get; set; }

	}
}
