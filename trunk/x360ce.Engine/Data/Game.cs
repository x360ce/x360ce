using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace x360ce.Engine.Data
{
	public partial class Game
	{

		public static Game FromDisk(string fileName)
		{
            var item = new Game();
			var fi = new FileInfo(fileName);
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			item.Comment = vi.Comments ?? "";
			item.DateCreated = DateTime.Now;
			item.DateUpdated = item.DateCreated;
            item.FileName = fi.Name ?? "";
            item.FileProductName = vi.ProductName ?? "";
            item.CompanyName = vi.CompanyName ?? "";
			item.DiskDriveId = BoardInfo.GetDiskDriveIdGuid();
			item.FileVersion = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart).ToString();
            item.FullPath = fi.FullName ?? "";
			item.GameId = Guid.NewGuid();
			item.HookMask = 0;
			item.IsEnabled = true;
			item.XInputMask = 0;
            item.ProcessorArchitecture = (int)Win32.PEReader.GetProcessorArchitecture(fi.FullName);
            return item;
		}

		// Check game settings against folder.
		public GameRefreshStatus Refresh()
		{
			var fi = new FileInfo(FullPath);
			// Check if game file exists.
			if (!fi.Exists) return GameRefreshStatus.FileNotExist;
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			return GameRefreshStatus.OK;
		}

		GameRefreshStatus _RefreshStatus;
		public GameRefreshStatus RefreshStatus
		{
			get { return _RefreshStatus; }
			set { _RefreshStatus = value; }
		}

		public void LoadDefault(Program program)
		{
			if (program == null) return;
			HookMask = program.HookMask;
			XInputMask = program.XInputMask;
            if (string.IsNullOrEmpty(FileProductName) && !string.IsNullOrEmpty(program.FileProductName))
            {
                FileProductName = program.FileProductName;
            }
		}

	}
}