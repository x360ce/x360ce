using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JocysCom.ClassLibrary.IO
{
	public class FileFinder
	{

		public event EventHandler<ProgressEventArgs> FileFound;

		int _DirectoryIndex;
		List<DirectoryInfo> _Directories;
		public bool IsPaused { get; set; }

		public bool IsStopping { get; set; }

		public List<FileInfo> GetFiles(string searchPattern, bool allDirectories = false, params string[] paths)
		{
			IsStopping = false;
			IsPaused = false;
			var fis = new List<FileInfo>();
			_Directories = paths.Select(x => new DirectoryInfo(x)).ToList();
			for (int i = 0; i < _Directories.Count; i++)
			{
				// Pause or Stop.
				while (IsPaused && !IsStopping)
				{
					// Logical delay without blocking the current hardware thread.
					var resetEvent = new ManualResetEventSlim(false); _ = Task.Run(async () => await Task.Delay(500)); resetEvent.Wait();
				}
				if (IsStopping)
					return fis;
				// Do tasks.
				_DirectoryIndex = i;
				var di = _Directories[i];
				// Skip folders if don't exists.
				if (!di.Exists)
					continue;
				AddFiles(di.FullName, di, ref fis, searchPattern, allDirectories);
			}
			return fis;
		}

		public Func<string, string, long, bool> IsIgnored;

		public void AddFiles(string rootPath, DirectoryInfo di, ref List<FileInfo> fileList, string searchPattern, bool allDirectories)
		{
			try
			{
				// Skip system folder.
				//if (di.Name == "System Volume Information")
				//	return;
				var patterns = searchPattern.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				if (patterns.Length == 0)
				{
					// Lookup for all files.
					patterns = new[] { "" };
				}
				for (int p = 0; p < patterns.Length; p++)
				{
					var pattern = patterns[p];
					var files = string.IsNullOrEmpty(pattern)
						? di.GetFiles()
						: di.GetFiles(pattern);
					for (int i = 0; i < files.Length; i++)
					{
						// Pause or Stop.
						while (IsPaused && !IsStopping)
						{
							// Logical delay without blocking the current hardware thread.
							var resetEvent = new ManualResetEventSlim(false); _ = Task.Run(async () => await Task.Delay(500)); resetEvent.Wait();
						}
						if (IsStopping)
							return;
						// Do tasks.
						var fullName = files[i].FullName;
						if (IsIgnored?.Invoke(rootPath, fullName, files[i].Length) == true)
							continue;
						if (!fileList.Any(x => x.FullName == fullName))
						{
							fileList.Add(files[i]);
							var ev = FileFound;
							if (ev is null)
								continue;
							// Report progress.
							var e = new ProgressEventArgs();
							e.TopIndex = _DirectoryIndex;
							e.TopCount = _Directories.Count;
							e.TopData = _Directories;
							e.SubIndex = fileList.Count - 1;
							e.SubCount = 0;
							e.SubData = fileList;
							e.State = ProgressStatus.Updated;
							e.TopMessage = $"Scan Folder: {_Directories[(int)e.TopIndex].FullName}";
							var file = fileList[(int)e.SubIndex];
							var name = file.FullName;
							var size = BytesToString(file.Length);
							e.SubMessage = $"File: {name} ({size})";
							ev(this, e);
						}
					}
				}
			}
			catch (Exception ex)
			{
				var _ = ex.ToString();
			}
			try
			{
				// If must search inside subdirectories then...
				if (allDirectories)
				{
					var subDis = di.GetDirectories();
					foreach (DirectoryInfo subDi in subDis)
					{
						// Pause or Stop.
						while (IsPaused && !IsStopping)
						{
							// Logical delay without blocking the current hardware thread.
							var resetEvent = new ManualResetEventSlim(false); _ = Task.Run(async () => await Task.Delay(500)); resetEvent.Wait();
						}
						if (IsStopping)
							return;
						if (IsIgnored?.Invoke(rootPath, subDi.FullName, 0) == true)
							continue;
						// Do tasks.
						AddFiles(rootPath, subDi, ref fileList, searchPattern, allDirectories);
					}
				}
			}
			catch (Exception ex)
			{
				var _ = ex.ToString();
			}

		}

		static string SizeToString(long value, string format = "{0:0.##} {1}", int newBase = 1000)
		{
			// Suffixes: Kilo, Mega, Giga, Tera, Peta, Exa.
			string[] suffix = { "", "K", "M", "G", "T", "P", "E" };
			var absolute = Math.Abs(value);
			if (value == 0)
				return string.Format(format, value, suffix[0]);
			var index = (int)Math.Floor(Math.Log(absolute, newBase));
			var number = Math.Round(absolute / Math.Pow(newBase, index), 1);
			var signed = Math.Sign(value) * number;
			return string.Format(format, signed, suffix[index]);
		}

		public static string BytesToString(long value)
			=> SizeToString(value, "{0:#,##0} {1}B", 1024);

	}
}
