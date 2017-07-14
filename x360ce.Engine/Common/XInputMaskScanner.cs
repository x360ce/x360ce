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

		public event EventHandler<XInputMaskScannerEventArgs> Progress;

		void ReportProgress(XInputMaskScannerEventArgs e)
		{
			var ev = Progress;
			if (ev != null)
			{
				ev(this, e);
			}
		}

		public string[] Paths;

		public void ScanGames(string[] paths, IList<UserGame> games, IList<Program> programs)
		{
			// Make copy of paths.
			Paths = paths.ToArray();
			var e = new XInputMaskScannerEventArgs();
			e.State = XInputMaskScannerState.Started;
			ReportProgress(e);
			var skipped = 0;
			var added = 0;
			var updated = 0;
			for (int i = 0; i < Paths.Length; i++)
			{
				var path = Paths[i];
				// Don't allow to scan windows folder.
				var winFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (path.StartsWith(winFolder)) continue;
				var di = new System.IO.DirectoryInfo(path);
				// Skip folders if don't exists.
				if (!di.Exists) continue;
				var exes = new List<FileInfo>();
				EngineHelper.GetFiles(di, ref exes, "*.exe", true);
				for (int f = 0; f < exes.Count; f++)
				{

					var exe = exes[f];
					var exeName = exe.Name.ToLower();
					var program = programs.FirstOrDefault(x => x.FileName.ToLower() == exeName);
					// If file doesn't exist in the game list then continue.
					if (program == null)
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
					e = new XInputMaskScannerEventArgs();
					e.State = XInputMaskScannerState.Update;
					e.Added = added;
					e.Skipped = skipped;
					e.Updated = updated;
					ReportProgress(e);
				}
			}
			e = new XInputMaskScannerEventArgs();
			e.State = XInputMaskScannerState.Completed;
			ReportProgress(e);
		}

		public UserGame FromDisk(string fileName, SearchOption? searchOption = null)
		{
			var item = new UserGame();
			var fi = new FileInfo(fileName);
			var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
			var architecture = Win32.PEReader.GetProcessorArchitecture(fi.FullName);
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
			item.DiskDriveId = BoardInfo.GetHashedDiskId();
			item.FileVersion = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart).ToString();
			item.FullPath = fi.FullName ?? "";
			item.GameId = Guid.NewGuid();
			item.HookMask = 0;
			item.XInputMask = (int)mask;
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
				// Skip XInput files.
				if (string.Compare(file, "xinput", true) == 00)
					continue;
				//  Skip X360CE files.
				if (string.Compare(file, "x360ce", true) == 00)
					continue;
				var fileArchitecture = Win32.PEReader.GetProcessorArchitecture(file);
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
			var architecture = Win32.PEReader.GetProcessorArchitecture(fullName);
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
				dic.Add(value, JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value));
			}
			byte[] fileBytes = File.ReadAllBytes(fullName);
			foreach (var key in dic.Keys)
			{
				var stringLBytes = Encoding.UTF8.GetBytes(dic[key].ToLower());
				var stringUBytes = Encoding.UTF8.GetBytes(dic[key].ToUpper());
				int j;
				for (var i = 0; i <= (fileBytes.Length - stringLBytes.Length); i++)
				{
					if (fileBytes[i] == stringLBytes[0] || fileBytes[i] == stringUBytes[0])
					{
						for (j = 1; j < stringLBytes.Length && (fileBytes[i + j] == stringLBytes[j] || fileBytes[i + j] == stringUBytes[j]); j++) ;
						if (j == stringLBytes.Length)
						{
							return key;
						}
					}
				}
			}
			return mask;
		}

	}
}
