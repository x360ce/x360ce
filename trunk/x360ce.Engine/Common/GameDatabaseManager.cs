using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>
	/// Helper methods for GDB/INI file.
	/// </summary>
	public class GameDatabaseManager
	{

		public GameDatabaseManager()
		{
			var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\X360CE\\x360ce.gdb";
			InitialFile = new FileInfo(path);
		}



		public void CheckSettingsFolder()
		{
			if (!InitialFile.Directory.Exists)
			{
				try { InitialFile.Directory.Create(); }
				catch (Exception) { return; }
			}
			if (!InitialFile.Exists)
			{
				try { System.IO.File.WriteAllText(InitialFile.FullName, ""); }
				catch (Exception) { }
			}
		}

		static object CurrentLock = new object();
		static GameDatabaseManager _current;
		public static GameDatabaseManager Current
		{
			get
			{
				lock (CurrentLock)
				{
					if (_current == null) _current = new GameDatabaseManager();
				}
				return _current;
			}
		}

		public FileInfo InitialFile;

		public List<Program> GetPrograms()
		{
			var ini = new Ini(InitialFile.FullName);
			var sections = ini.GetSections();
			var programs = new List<Program>();
			foreach (var section in sections)
			{
				var program = new Program();
				program.FileName = section;
				program.FileProductName = ini.GetValue(section, "Name") ?? "";
				var hmString = ini.GetValue(section, "HookMask");
				int hookMask;
				if (int.TryParse(hmString, out hookMask)) program.HookMask = hookMask;
			}
			return programs;
		}

		public void SetPrograms(IEnumerable<Program> programs, IEnumerable<Game> games)
		{
			// Clean file. INI files support only UTF16-little endian format.
			System.IO.File.WriteAllText(InitialFile.FullName, "", Encoding.Unicode);
			var ini = new Ini(InitialFile.FullName);
			// Get unique section names.
			var names = programs.Select(x => x.FileName.ToLower()).ToList();
			var gameNames = games.Select(x => x.FileName.ToLower()).ToList();
			names.AddRange(gameNames);
			names = names.Distinct().OrderBy(x => x).ToList();
			// Write INI
			foreach (var name in names)
			{
				string section = "";
				string productName = "";
				string hookMask = "";
				var program = programs.FirstOrDefault(x => x.FileName.ToLower() == name);
				if (program != null)
				{
					section = program.FileName;
					productName  = program.FileProductName;
					hookMask = string.Format("0x{0:X8}", program.HookMask);
				}
				var game = games.FirstOrDefault(x => x.FileName.ToLower() == name);
				if (game != null)
				{
					section = game.FileName;
					productName = game.FileProductName;
					hookMask = string.Format("0x{0:X8}", game.HookMask);
				}
				ini.SetValue(section, "Name", productName);
				ini.SetValue(section, "HookMask", hookMask);
			}
			Refresh();
			var md5name = System.IO.Path.GetFileNameWithoutExtension(InitialFile.Name);
			var md5file = System.IO.Path.Combine(InitialFile.Directory.FullName, md5name + ".md5");
			var hash = string.Join("", Checksum.ToByteArray().Select(x => x.ToString("X2")));
			var md5line = string.Format("{0} *{1}", hash, InitialFile.Name);
			System.IO.File.WriteAllText(md5file, md5line);
		}

		Guid? _Checksum;
		Guid Checksum
		{
			get
			{
				if (!_Checksum.HasValue) Refresh();
				return _Checksum.Value;
			}
		}

		object RefreshLock = new object();

		public void Refresh()
		{
			// Read checksum.
			var bytes = System.IO.File.ReadAllBytes(InitialFile.FullName);
			_Checksum = EngineHelper.ComputeMd5Hash(bytes);
		}

	}
}
