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
		public void Refresh()
		{
			var fi = new FileInfo(FullPath);
			// Check if game file exists.
			if (!fi.Exists)
			{
				RefreshStatus = GameRefreshStatus.FileNotExist;
				return;
			}
			else
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
				var values = (XInputMask[])Enum.GetValues(typeof(XInputMask));
				foreach (var value in values)
				{
					// If value is enabled then...
					if (((uint)XInputMask & (uint)value) != 0)
					{
						// Get name of xInput file.
						var dllName = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value);
						var dllFullPath = System.IO.Path.Combine(fi.Directory.FullName, dllName);
						var dllFileInfo = new System.IO.FileInfo(dllFullPath);
						if (!dllFileInfo.Exists)
						{
							RefreshStatus = GameRefreshStatus.XInputFileNotExist;
							return;
						}
						var arch = Win32.PEReader.GetProcessorArchitecture(dllFullPath);
						// If  64-bit then...
						if (value.ToString().Contains("x64"))
						{
							if (arch == System.Reflection.ProcessorArchitecture.X86)
							{
								RefreshStatus = GameRefreshStatus.XInputFileWrongPlatform;
								return;
							}
						}
						// If 32-bit then...
						else if (value.ToString().Contains("x86"))
						{
							if (arch == System.Reflection.ProcessorArchitecture.Amd64)
							{
								RefreshStatus = GameRefreshStatus.XInputFileWrongPlatform;
								return;
							}
						}
					}
				}
			}
			RefreshStatus = GameRefreshStatus.OK;
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