using System;
using System.Collections.Generic;
using System.Globalization;
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
			GdbFile = new FileInfo(path);
			path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\X360CE\\x360ce.md5";
			Md5File = new FileInfo(path);
		}

		public void CheckSettingsFolder()
		{
			if (!GdbFile.Directory.Exists)
			{
				try { GdbFile.Directory.Create(); }
				catch (Exception) { return; }
			}
			if (!GdbFile.Exists)
			{
				try { System.IO.File.WriteAllText(GdbFile.FullName, ""); }
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

		public FileInfo GdbFile;
		public FileInfo Md5File;

		public List<Program> GetPrograms()
		{
			return GetPrograms(GdbFile.FullName);
		}

		public static List<Program> GetPrograms(string iniFileName)
		{
			var ini = new Ini(iniFileName);
			var sections = ini.GetSections();
			var programs = new List<Program>();
			foreach (var section in sections)
			{
				var program = new Program();
				program.FileName = section;
				program.FileProductName = ini.GetValue(section, "Name") ?? "";
				var hmString = ini.GetValue(section, "HookMask");
				int hookMask;
				// If hook mask is hexadecimal then....
				bool success = hmString.StartsWith("0x")
					? int.TryParse(hmString.Substring(2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat, out hookMask)
					: int.TryParse(hmString, out hookMask);
				if (success)
				{
					program.HookMask = hookMask;
					programs.Add(program);
				}
			}
			return programs;
		}

		public void SetPrograms(IEnumerable<Program> programs, IEnumerable<Game> games)
		{
			// Clean file. INI files support only UTF16-little endian format.
			var file = new System.IO.FileInfo(GdbFile.FullName);
			if (!file.Directory.Exists) file.Directory.Create();
			System.IO.File.WriteAllText(GdbFile.FullName, "", Encoding.Unicode);
			var ini = new Ini(GdbFile.FullName);
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
				string fakeVid = null;
				string fakePid = null;
				string dinputMask = null;
				string dinputFile = null;
				string timeout = null;
				var program = programs.FirstOrDefault(x => x.FileName.ToLower() == name);
				var game = games.FirstOrDefault(x => x.FileName.ToLower() == name);
				if (game != null)
				{
					section = game.FileName;
					productName = game.FileProductName;
					hookMask = string.Format("0x{0:X8}", game.HookMask);
					if (game.DInputMask > 0) dinputMask = string.Format("0x{0:X8}", game.DInputMask);
					if (!string.IsNullOrEmpty(game.DInputFile)) dinputFile = game.DInputFile;
					if (game.FakeVID > 0) fakeVid = string.Format("0x{0:X4}", game.FakeVID);
					if (game.FakePID > 0) fakePid = string.Format("0x{0:X4}", game.FakePID);
					if (game.Timeout >= 0) timeout = game.Timeout.ToString();
				}
				// Use default settings as an alternative.
				else if (program != null)
				{
					section = program.FileName;
					productName = program.FileProductName;
					hookMask = string.Format("0x{0:X8}", program.HookMask);
					if (program.DInputMask > 0) dinputMask = string.Format("0x{0:X8}", program.DInputMask);
					if (!string.IsNullOrEmpty(program.DInputFile)) dinputFile = program.DInputFile;
					if (program.FakeVID > 0) fakeVid = string.Format("0x{0:X4}", program.FakeVID);
					if (program.FakePID > 0) fakePid = string.Format("0x{0:X4}", program.FakePID);
					if (program.Timeout >= 0) timeout = program.Timeout.ToString();
				}
				ini.SetValue(section, "Name", productName);
				ini.SetValue(section, "HookMask", hookMask);
				ini.SetValue(section, "DinputMask", dinputMask);
				ini.SetValue(section, "DinputFile", dinputFile);
				ini.SetValue(section, "FakeVID", fakeVid);
				ini.SetValue(section, "FakePID", fakePid);
				ini.SetValue(section, "Timeout", timeout);
			}
			Refresh();
			var hash = string.Join("", Checksum.ToByteArray().Select(x => x.ToString("X2")));
			var md5line = string.Format("{0} *{1}", hash, GdbFile.Name);
			System.IO.File.WriteAllText(Md5File.FullName, md5line);
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
			var bytes = System.IO.File.ReadAllBytes(GdbFile.FullName);
			_Checksum = EngineHelper.ComputeMd5Hash(bytes);
		}

	}
}
