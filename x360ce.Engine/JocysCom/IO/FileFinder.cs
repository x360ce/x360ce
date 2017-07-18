using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JocysCom.ClassLibrary.IO
{
	public class FileFinder
	{

		public event EventHandler<FileFinderEventArgs> FileFound;

		int _DirectoryIndex;
		List<DirectoryInfo> _Directories;

		public List<FileInfo> GetFiles(string searchPattern, bool allDirectories = false, params string[] paths)
		{
			var fis = new List<FileInfo>();
			_Directories = paths.Select(x => new DirectoryInfo(x)).ToList();
			for (int i = 0; i < _Directories.Count; i++)
			{
				_DirectoryIndex = i;
				var di = _Directories[i];
				// Skip folders if don't exists.
				if (!di.Exists) continue;
				AddFiles(di, ref fis, searchPattern, allDirectories);
			}
			return fis;
		}

		public void AddFiles(DirectoryInfo di, ref List<FileInfo> fileList, string searchPattern, bool allDirectories)
		{
			try
			{
				if (allDirectories)
				{
					foreach (DirectoryInfo subDi in di.GetDirectories())
					{
						AddFiles(subDi, ref fileList, searchPattern, allDirectories);
					}
				}
			}
			catch { }
			try
			{
				// Add only different files.
				var files = di.GetFiles(searchPattern);
				for (int i = 0; i < files.Length; i++)
				{
					var fullName = files[i].FullName;
					if (!fileList.Any(x => x.FullName == fullName))
					{
						fileList.Add(files[i]);
						var ev = FileFound;

						if (ev != null)
						{
							var e = new FileFinderEventArgs();
							e.Directories = _Directories;
							e.DirectoryIndex = _DirectoryIndex;
							e.FileIndex = fileList.Count - 1;
							e.Files = fileList;
							ev(this, e);
						}
					}
				}
			}
			catch { }
		}

	}
}
