using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        void ReportProgress(XInputMaskScannerEventArgs e)
        {
            var ev = Progress;
            if (ev != null)
            {
                ev(this, e);
            }
        }

        public static XSettingsData<XInputMaskFileInfo> FileInfoCache = new XSettingsData<XInputMaskFileInfo>("XInputMask.xml", "XInput mask scan cache.");

        static object FileInfoCacheLock = new object();

        public bool IsStopping { get { return ff.IsStopping; } set { ff.IsStopping = value; } }

        public bool IsPaused { get { return ff.IsPaused; } set { ff.IsPaused = value; } }

        XInputMask? GetCachedMask(FileInfo fi)
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

        void SetCachedMask(FileInfo fi, XInputMask mask)
        {
            lock (FileInfoCacheLock)
            {
                var item = FileInfoCache.Items.FirstOrDefault(x => string.Compare(x.FullName, fi.FullName, true) == 0);
                if (item != null)
                {
                    FileInfoCache.Remove(item);
                }
                item = new XInputMaskFileInfo();
                item.FullName = fi.FullName;
                item.Mask = mask;
                item.Modified = fi.LastWriteTimeUtc;
                item.Size = fi.Length;
                FileInfoCache.Add(item);
            }
        }

        FileFinder ff;

        public void ScanGames(string[] paths, IList<UserGame> games, IList<Program> programs, string fileName = null)
        {
            IsStopping = false;
            IsPaused = false;
            // Step 1: Get list of executables inside the folder.
            var e = new XInputMaskScannerEventArgs();
            e.State = XInputMaskScannerState.Started;
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
            for (int i = 0; i < exes.Count; i++)
            {
                var exe = exes[i];
                var exeName = exe.Name.ToLower();
                var program = programs.FirstOrDefault(x => x.FileName.ToLower() == exeName);
                // If file doesn't exist in the game list then continue.
                e = new XInputMaskScannerEventArgs();
                e.Message = string.Format("Step 2: Scan file {0} of {1}. Please wait...", i + 1, exes.Count);
                e.FileIndex = i;
                e.Files = exes;
                e.State = XInputMaskScannerState.FileUpdate;
                e.Added = added;
                e.Skipped = skipped;
                e.Updated = updated;
                ReportProgress(e);
                // If specific file name was not specifield and program not found then... 
                if (string.IsNullOrEmpty(fileName) && program == null)
                {
                    skipped++;
                }
                else
                {
                    e = new XInputMaskScannerEventArgs();
                    e.Program = program;
                    e.GameFileInfo = exe;
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
            e = new XInputMaskScannerEventArgs();
            e.State = XInputMaskScannerState.Completed;
            ReportProgress(e);
        }

        private void ff_FileFound(object sender, FileFinderEventArgs e)
        {
            var e2 = new XInputMaskScannerEventArgs();
            e2.DirectoryIndex = e.DirectoryIndex;
            e2.Directories = e.Directories;
            e2.FileIndex = e.FileIndex;
            e2.Files = e.Files;
            e2.State = XInputMaskScannerState.DirectoryUpdate;
            e2.Message = string.Format("Step 1: {0} programs found. Searching path {1} of {2}. Please wait...", e.Files.Count, e.DirectoryIndex + 1, e.Directories.Count);
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
            XInputMask mask = Engine.XInputMask.None;
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
            XInputMask mask = Engine.XInputMask.None;
            // Create list to store masks.
            var masks = new Dictionary<string, XInputMask>();
            foreach (var file in files)
            {
                // Pause or Stop.
                while (IsPaused && !IsStopping)
                    System.Threading.Thread.Sleep(500);
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
            XInputMask mask = Engine.XInputMask.None;
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
            // Do scan.
            byte[] fileBytes = File.ReadAllBytes(fullName);
            foreach (var key in dic.Keys)
            {
                var stringLBytes = Encoding.UTF8.GetBytes(dic[key].ToLower());
                var stringUBytes = Encoding.UTF8.GetBytes(dic[key].ToUpper());
                int j;
                for (var i = 0; i <= (fileBytes.Length - stringLBytes.Length); i++)
                {
                    // Pause or Stop.
                    while (IsPaused && !IsStopping)
                        System.Threading.Thread.Sleep(500);
                    if (IsStopping)
                        return mask;
                    // Do tasks.
                    if (fileBytes[i] == stringLBytes[0] || fileBytes[i] == stringUBytes[0])
                    {
                        for (j = 1; j < stringLBytes.Length && (fileBytes[i + j] == stringLBytes[j] || fileBytes[i + j] == stringUBytes[j]); j++) ;
                        if (j == stringLBytes.Length)
                        {
                            SetCachedMask(fi, key);
                            return key;
                        }
                    }
                }
            }
            SetCachedMask(fi, mask);
            return mask;
        }

    }
}
