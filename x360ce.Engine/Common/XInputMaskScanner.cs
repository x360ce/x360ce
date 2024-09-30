using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public class XInputMaskScanner
    {

        public XInputMaskScanner()
        {
            ff = new FileFinder();
            ff.FileFound += ff_FileFound;
        }

        public event EventHandler<XInputMaskScannerEventArgs> Progress;

		private void ReportProgress(XInputMaskScannerEventArgs e)
        {
            var ev = Progress;
            if (ev != null)
            {
                ev(this, e);
            }
        }

        public static XSettingsData<XInputMaskFileInfo> FileInfoCache = new XSettingsData<XInputMaskFileInfo>("XInputMask.xml", "XInput mask scan cache.");
		private static readonly object FileInfoCacheLock = new object();

        public bool IsStopping { get => ff.IsStopping; set => ff.IsStopping = value; }

		public bool IsPaused { get => ff.IsPaused; set => ff.IsPaused = value; }

		private XInputMask? GetCachedMask(FileInfo fi)
        {
            lock (FileInfoCacheLock)
            {
                var item = FileInfoCache.Items.FirstOrDefault(x => string.Compare(x.FullName, fi.FullName, true) == 0);
                // if Found and changed then...
                if (item != null && (item.Modified != fi.LastWriteTimeUtc || item.Size != fi.Length))
                {
                    FileInfoCache.Remove(item);
                    item = null;
                }
                // Use cached value.
                return (item == null)
                    ? (XInputMask?)null
                    : item.Mask;
            }
        }

		private void SetCachedMask(FileInfo fi, XInputMask mask)
        {
            lock (FileInfoCacheLock)
            {
                var item = FileInfoCache.Items.FirstOrDefault(x => string.Compare(x.FullName, fi.FullName, true) == 0);
                if (item != null)
                    FileInfoCache.Remove(item);
				item = new XInputMaskFileInfo
				{
					FullName = fi.FullName,
					Mask = mask,
					Modified = fi.LastWriteTimeUtc,
					Size = fi.Length
				};
				FileInfoCache.Add(item);
            }
        }

		private readonly FileFinder ff;

        public void ScanGames(string[] paths, IList<UserGame> games, IList<Program> programs, string fileName = null)
        {
            IsStopping = false;
            IsPaused = false;
			// Step 1: Get list of executables inside the folder.
			var e = new XInputMaskScannerEventArgs
			{
				State = XInputMaskScannerState.Started
			};
			ReportProgress(e);
            var skipped = 0;
            var added = 0;
            var updated = 0;
            var winFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var dirs = paths
                .Select(x => x)
                // Except win folders.
                .Where(x => !x.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            // Create list to store file to scan.
            var exes = string.IsNullOrEmpty(fileName)
                // Scan all executables.
                ? ff.GetFiles("*.exe", true, dirs)
                // Scan specific executable.
                : ff.GetFiles(fileName, false, dirs);
            // Step 2: Scan files.
            for (var i = 0; i < exes.Count; i++)
            {
                var exe = exes[i];
                var exeName = exe.Name.ToLower();
                var program = programs.FirstOrDefault(x => x.FileName.ToLower() == exeName);
				// If file doesn't exist in the game list then continue.
				e = new XInputMaskScannerEventArgs
				{
					Message = string.Format("Step 2: Scan file {0} of {1}. Please wait...", i + 1, exes.Count),
					FileIndex = i,
					Files = exes,
					State = XInputMaskScannerState.FileUpdate,
					Added = added,
					Skipped = skipped,
					Updated = updated
				};
				ReportProgress(e);
                // If specific file name was not specified and program not found then... 
                if (string.IsNullOrEmpty(fileName) && program == null)
                {
                    skipped++;
                }
                else
                {
					e = new XInputMaskScannerEventArgs
					{
						Program = program,
						GameFileInfo = exe
					};
					// Get game by executable name.
					var game = games.FirstOrDefault(x => x.FileName.ToLower() == exeName);
                    // If file doesn't exist in the game list then...
                    if (game == null)
                    {
                        game = FromDisk(exe.FullName, SearchOption.AllDirectories);
                        game.LoadDefault(program, true);
                        e.State = XInputMaskScannerState.GameFound;
                        e.Game = game;
                        added++;
                    }
                    else
                    {
                        e.Game = game;
                        e.State = XInputMaskScannerState.GameUpdated;
                        updated++;
                    }
                    ReportProgress(e);
                }
            }
			e = new XInputMaskScannerEventArgs
			{
				State = XInputMaskScannerState.Completed
			};
			ReportProgress(e);
        }

        private void ff_FileFound(object sender, ProgressEventArgs e)
        {
			var e2 = new XInputMaskScannerEventArgs
			{
				DirectoryIndex = e.TopIndex,
				Directories = (List<DirectoryInfo>)e.TopData,
				FileIndex = e.SubIndex,
				Files = (List<FileInfo>)e.SubData,
				State = XInputMaskScannerState.DirectoryUpdate,
				Message = string.Format("Step 1: {0} programs found. Searching path {1} of {2}. Please wait...", e.SubCount, e.TopIndex + 1, e.TopCount)
			};
			ReportProgress(e2);
        }

        /// <summary>
        /// Create UserGame object from path.
        /// </summary>
        /// <param name="fileName">File name to check.</param>
        /// <param name="searchOption">If not specified then check specified file only.</param>
        /// <returns></returns>
        public UserGame FromDisk(string fileName, SearchOption? searchOption = null)
        {
            var item = new UserGame();
            var fi = new FileInfo(fileName);
            var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
            var architecture = JocysCom.ClassLibrary.Win32.PEReader.GetProcessorArchitecture(fi.FullName);
            var is64bit = architecture == System.Reflection.ProcessorArchitecture.Amd64;
            var mask = Engine.XInputMask.None;
            if (searchOption.HasValue)
            {
                // Get XInput files used inside game folder.
                var list = GetMasks(fi.DirectoryName, searchOption.Value, is64bit);
                // Combine masks.
                foreach (var value in list.Values)
                {
                    mask |= value;
                }
                // var mask = GetMask(fi.FullName);
                if (mask == Engine.XInputMask.None)
                {
                    mask = (is64bit)
                    ? mask = Engine.XInputMask.XInput13_x64
                    : mask = Engine.XInputMask.XInput13_x86;
                }
            }
            else
            {
                mask = GetMask(fileName);
            }
            item.Timeout = -1;
            item.Comment = vi.Comments ?? "";
            item.DateCreated = DateTime.Now;
            item.DateUpdated = item.DateCreated;
            item.FileName = fi.Name ?? "";
            item.FileProductName = EngineHelper.FixName(vi.ProductName, item.FileName);
            item.FileVersion = vi.FileVersion ?? "";
            item.CompanyName = vi.CompanyName ?? "";
            item.ComputerId = BoardInfo.GetHashedDiskId(BoardInfo.GetDiskId());
            item.FileVersion = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart).ToString();
            item.FullPath = fi.FullName ?? "";
            item.GameId = Guid.NewGuid();
            item.HookMask = 0;
            item.XInputMask = (int)mask;
			item.XInputPath = "";
            item.DInputMask = 0;
            item.DInputFile = "";
            item.FakeVID = 0;
            item.FakePID = 0;
            item.Timeout = -1;
            item.Weight = 1;
            item.IsEnabled = true;
            item.ProcessorArchitecture = (int)architecture;
            return item;
        }

        /// <summary>
        /// Look inside folder for "XInput..." strings and return XInput mask.
        /// </summary>
        /// <returns>Thanks mrexodia (https://github.com/mrexodia) for suggestion</returns>
        public Dictionary<string, XInputMask> GetMasks(string path, SearchOption searchOption, bool is64bit)
        {
            // Check masks inside *.exe and *.dll files.
            var files = Directory.GetFiles(path, "*.exe", searchOption).ToList();
            var dlls = Directory.GetFiles(path, "*.dll", searchOption).ToList();
            files.AddRange(dlls);
            var mask = Engine.XInputMask.None;
            // Create list to store masks.
            var masks = new Dictionary<string, XInputMask>();
			// If file doesn't exist in the game list then continue.
			var e = new XInputMaskScannerEventArgs
			{
				Level = 1,
				Files = files.Select(x => new FileInfo(x)).ToList(),
				State = XInputMaskScannerState.FileUpdate
			};
			ReportProgress(e);
			for (var i = 0; i < files.Count; i++)
			{
				e.FileIndex = i;
				e.Message = string.Format("Scan file {0} of {1}. Please wait...", i + 1, files.Count);
				ReportProgress(e);
				var file = files[i];
                // Pause or Stop.
                while (IsPaused && !IsStopping)
					// Logical delay without blocking the current thread.
					System.Threading.Tasks.Task.Delay(500).Wait();
				if (IsStopping)
                    return masks;
                // Do tasks.
                // Skip XInput files.
                if (string.Compare(file, "xinput", true) == 00)
                    continue;
                //  Skip X360CE files.
                if (string.Compare(file, "x360ce", true) == 00)
                    continue;
                var fileArchitecture = JocysCom.ClassLibrary.Win32.PEReader.GetProcessorArchitecture(file);
                var fileIs64bit = (fileArchitecture == System.Reflection.ProcessorArchitecture.Amd64);
                // Skip wrong architecture.
                if (is64bit != fileIs64bit)
                    continue;
                // Get XInput mask for the file.
                mask = GetMask(file);
                if (mask != Engine.XInputMask.None)
                {
                    masks.Add(file, mask);
                }
            }
            return masks;
        }

        /// <summary>
        /// Look inside file for "XInput..." strings and return XInput mask.
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="architecture"></param>
        /// <returns></returns>
        public XInputMask GetMask(string fullName)
        {
            var architecture = JocysCom.ClassLibrary.Win32.PEReader.GetProcessorArchitecture(fullName);
            XInputMask[] xiValues;
            if (architecture == System.Reflection.ProcessorArchitecture.Amd64)
            {
                xiValues = Enum.GetValues(typeof(XInputMask))
                    .Cast<XInputMask>()
                    .Where(x => x.ToString().Contains("x64"))
                    .ToArray();
            }
            else
            {
                xiValues = Enum.GetValues(typeof(XInputMask))
                    .Cast<XInputMask>()
                    .Where(x => x.ToString().Contains("x86"))
                    .ToArray();
            }
            var mask = Engine.XInputMask.None;
            var dic = new Dictionary<XInputMask, string>();
            foreach (var value in xiValues)
            {
                dic.Add(value, Attributes.GetDescription(value));
            }
            // Check cache first.
            var fi = new FileInfo(fullName);
            var cachedMask = GetCachedMask(fi);
            if (cachedMask.HasValue)
                return cachedMask.Value;
            var maxLength = 64 * 1024 * 1024;
            // If file is less or equal 64 MB then...
            byte[] fileBytes;
            if (fi.Length <= maxLength)
            {
                // Read all file bytes.
                fileBytes = File.ReadAllBytes(fi.FullName);
			}
			else
			{
                // Maximum buffer 64 MB.
                fileBytes = new byte[maxLength];
                var half = maxLength / 2;
                // Do not lock the file.
                var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                // Read 32 MB from the start.
                stream.Read(fileBytes, 0, half);
                // Read 32 MB from the end.
                stream.Seek(fi.Length - half, SeekOrigin.Begin);
                stream.Read(fileBytes, half, half);
                stream.Dispose();
            }
            // Get "XInput positions inside file bytes.
            var positions = GetPositions(fileBytes, "xinput");
            foreach (var position in positions)
			{
                foreach (var key in dic.Keys)
                {
                    var value = dic[key];
                    if (position + value.Length > fileBytes.Length)
                        continue;
                    var s = Encoding.ASCII.GetString(fileBytes, position, value.Length);
                    if (value.Equals(s, StringComparison.OrdinalIgnoreCase))
                       mask |= key;
                }
            }
            SetCachedMask(fi, mask);
            return mask;
        }

        public List<int> GetPositions(byte[] bytes, string key)
		{
            var positions = new List<int>();
            var lBytes = Encoding.UTF8.GetBytes(key.ToLower());
            var uBytes = Encoding.UTF8.GetBytes(key.ToUpper());
            int j;
            for (var i = 0; i <= (bytes.Length - lBytes.Length); i++)
            {
                // Pause or Stop.
                while (IsPaused && !IsStopping)
                    // Logical delay without blocking the current thread.
                    System.Threading.Tasks.Task.Delay(500).Wait();
                if (IsStopping)
                    return positions;
                // Find first matching bytes.
                if (bytes[i] == lBytes[0] || bytes[i] == uBytes[0])
                {
                    // Look forward for full match.
                    for (j = 1; j < lBytes.Length && (bytes[i + j] == lBytes[j] || bytes[i + j] == uBytes[j]); j++);
                    // If full match found then add position.
                    if (j == lBytes.Length)
                        positions.Add(i);
                }
            }
            return positions;
        }

    }
}
