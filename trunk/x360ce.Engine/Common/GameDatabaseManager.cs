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

		public List<Program> GetPrograms(string iniPath)
		{
			var ini = new Ini(iniPath);
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

		public void SetPrograms(string iniPath, List<Program> programs)
		{
			//var ini = new Ini(iniPath);
			//var sections = ini.GetSections();
			//var programs = new List<Program>();
			//foreach (var section in sections)
			//{
			//	var program = new Program();
			//	program.FileName = section;
			//	program.FileProductName = ini.GetValue(section, "Name") ?? "";
			//	var hmString = ini.GetValue(section, "HookMask");
			//	int hookMask;
			//	if (int.TryParse(hmString, out hookMask)) program.HookMask = hookMask;
			//}
			//return programs;
		}


	}
}
