using System;
using System.Collections.Generic;
using System.IO;

namespace JocysCom.ClassLibrary.IO
{
	public class FileFinderEventArgs : EventArgs
	{
		public List<FileInfo> Files;
		public int FileIndex;
		public List<DirectoryInfo> Directories;
		public int DirectoryIndex;
	}
}
