using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
