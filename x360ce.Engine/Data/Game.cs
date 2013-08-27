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
			item.Comment = vi.Comments;
			item.DateCreated = DateTime.Now;
			item.DateUpdated = item.DateCreated;
			item.FileName = fi.Name;
			item.FileProductName = vi.ProductName;
			item.CompanyName = vi.CompanyName;
			item.DiskDriveId = BoardInfo.GetDiskDriveIdGuid();
			item.FileVersion = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart).ToString();
			item.FullPath = fi.FullName;
			item.GameId = Guid.NewGuid();
			item.HookMask = 0;
			item.IsEnabled = true;
			item.XInputMask = 0;
			return item;
		}

		public void LoadDefault(Program program)
		{
			if (program == null) return;
			HookMask = program.HookMask;
			XInputMask = program.XInputMask;
		}

	}
}