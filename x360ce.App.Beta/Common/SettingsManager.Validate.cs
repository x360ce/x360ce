using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		/// <summary>
		/// Check game settings against the folder
		/// </summary>
		/// <param name="game"></param>
		/// <param name="fix"></param>
		/// <returns></returns>
		/// <remarks>Implement ability to run this method as Administrator later, because INI/DLL files inside "Program Files" folder will be protected by UAC.</remarks>
		public GameRefreshStatus GetDllAndIniStatus(x360ce.Engine.Data.UserGame game, bool fix = false)
		{
			var status = GameRefreshStatus.OK;
			var fi = new FileInfo(game.FullPath);
			// Check if game file exists.
			if (!fi.Exists)
			{
				return GameRefreshStatus.ExeNotExist;
			}
			// Check if game is not enabled.
			else if (!game.IsEnabled)
			{
				return status;
			}
			else
			{
				var settingsDirectory = game.GetLibraryAndSettingsDirectory();
				var iniFullName = Path.Combine(settingsDirectory.FullName, IniFileName);
				// Create dictionary from XInput type and XInput file name.
				var xiValues = ((XInputMask[])Enum.GetValues(typeof(XInputMask))).Where(x => x != XInputMask.None).ToArray();
				var dic = new Dictionary<XInputMask, string>();
				foreach (var value in xiValues)
				{
					dic.Add(value, Attributes.GetDescription(value));
				}
				// Get file names: xinput9_1_0.dll, xinput1_1.dll, xinput1_2.dll, xinput1_3.dll, xinput1_4.dll
				var xiFileNames = dic.Values.Distinct();
				// If game DLL emulation is not enabled then...
				if (!game.IsLibrary)
				{
					// If INI file exists then...
					status |= CheckUnnecessarySettingFile(fix, iniFullName);
					// Loop through XInput DLL files.
					foreach (var xiFileName in xiFileNames)
					{
						// Get information about XInput DLL file.
						var dllFullPath = Path.Combine(fi.Directory.FullName, xiFileName);
						status |= CheckUnnecessaryLibraryFile(fix, dllFullPath);
					}
				}
				else
				{
					status = CheckSettingFileContent(fix, game);
					// Loop through XInput DLL files.
					foreach (var xiFileName in xiFileNames)
					{
						// Get enumeration linked to 64-bit version of the file.
						var x64Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x64")).Key;
						// Get enumeration linked to 32-bit version of the file.
						var x86Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x86")).Key;
						// Get information about XInput DLL file.
						var xiFullPath = System.IO.Path.Combine(fi.Directory.FullName, xiFileName);
						var xiFileInfo = new System.IO.FileInfo(xiFullPath);
						// Determine required architecture.
						var requiredArchitecture = ProcessorArchitecture.None;
						var x64Enabled = ((XInputMask)game.XInputMask).HasFlag(x64Value);
						var x86Enabled = ((XInputMask)game.XInputMask).HasFlag(x86Value);
						if (x86Enabled && x64Enabled) requiredArchitecture = ProcessorArchitecture.MSIL;
						else if (x86Enabled) requiredArchitecture = ProcessorArchitecture.X86;
						else if (x64Enabled) requiredArchitecture = ProcessorArchitecture.Amd64;
						// Get embedded resource name for current XInput file.
						var resourceName = EngineHelper.GetXInputResoureceName(requiredArchitecture);
						// If both XInput file CheckBoxes are disabled then...
						if (requiredArchitecture == ProcessorArchitecture.None)
						{
							// If XInput file exists then...
							if (xiFileInfo.Exists)
							{
								if (fix)
								{
									// Delete unnecessary XInput file.
									xiFileInfo.Delete();
								}
								else
								{
									status |= GameRefreshStatus.UnnecessaryLibraryFile;
								}
							}
						}
						// If XInput file doesn't exists then...
						else if (!xiFileInfo.Exists)
						{
							// Create XInput file.
							if (fix)
							{
								AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
							}
							else
							{
								status |= GameRefreshStatus.XInputFilesNotExist;
							}
						}
						else
						{
							// Get current architecture of XInput DLL.
							var xiCurrentArchitecture = PEReader.GetProcessorArchitecture(xiFullPath);
							// If processor architectures doesn't match then...
							if (requiredArchitecture != xiCurrentArchitecture)
							{
								// Create XInput file.
								if (fix)
								{
									AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
								}
								else
								{
									status |= GameRefreshStatus.XInputFilesWrongPlatform;
								}
							}
							else
							{
								// Determine if file was created by Microsoft.
								bool byMicrosoft;
								// Get version of XInput DLL on the disk.
								var dllVersion = EngineHelper.GetDllVersion(xiFullPath, out byMicrosoft);
								// Get version of embedded XInput DLL.
								var embededVersion = EngineHelper.GetEmbeddedDllVersion(xiCurrentArchitecture);
								// If file on disk is older then...
								if (dllVersion < embededVersion)
								{
									// Overwrite XInput file.
									if (fix)
									{
										AppHelper.WriteFile(resourceName, xiFileInfo.FullName);
									}
									else
									{
										status |= GameRefreshStatus.XInputFilesOlderVersion;
									}
								}
								else if (dllVersion > embededVersion)
								{
									// Allow new version.
									// return GameRefreshStatus.XInputFileNewerVersion;
								}
							}
						}
					}
				}
			}
			return status;
		}

		Regex BackupFileRx = new Regex("(?<name>.*).(?<hash>[0-9A-F]{8})(?<ext>.[0-9A-Z]+)");

		/// <summary>
		/// Get backup file.
		/// Backup file for "xinput1_3.dll" file name will be "xinput1_3.HHHHHHHH.dll", where HHHHHHHH is CRC32 checksum in hex.
		/// </summary>
		/// <param name="fileName">Non backup file name.</param>
		FileInfo[] GetBackupFiles(string fileName)
		{
			var list = new List<FileInfo>();
			// Create pattern.
			var name = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var pattern = string.Format("{0}.????????{1}", name, ext);
			var di = new FileInfo(fileName).Directory;
			// If directory do not exists then 
			if (!di.Exists)
				return list.ToArray();
			var files = di.GetFiles(pattern);
			foreach (var file in files)
			{
				var match = BackupFileRx.Match(file.Name);
				// If pattern is incorrect then skip.
				if (!match.Success)
					continue;
				var hash = JocysCom.ClassLibrary.Security.CRC32Helper.GetHashFromFileAsString(file.FullName);
				// If file is damaged then skip.
				if (match.Groups["hash"].Value != hash)
					continue;
				var orgIsCustom = EngineHelper.IsCustomLibrarry(file.FullName);
				// If DLL file is made by X360CE then skip.
				if (orgIsCustom.HasValue && orgIsCustom.Value)
					continue;
				// Add backup file to the list.
				list.Add(file);
			}
			return list.ToArray();
		}

		/// <summary>
		/// Backup non X360CE file.
		/// Backup file for "xinput1_3.dll" file name will be "xinput1_3.HHHHHHHH.dll", where HHHHHHHH is CRC32 checksum in hex.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="move">True - delete original file, false - make a copy of backup file.</param>
		void BackupFile(string fileName, bool move = false)
		{
			// Create pattern.
			var name = Path.GetFileNameWithoutExtension(fileName);
			var ext = Path.GetExtension(fileName);
			var fi = new FileInfo(fileName);
			// If file do not exists then 
			if (!fi.Exists)
				return;
			var hash = JocysCom.ClassLibrary.Security.CRC32Helper.GetHashFromFileAsString(fi.FullName);
			var newName = string.Format("{0}.{1}{2}", name, hash, ext);
			var fullName = System.IO.Path.Combine(fi.Directory.FullName, newName);
			// If backup already exists then return.
			if (File.Exists(fullName))
				return;
			if (move)
				fi.MoveTo(fullName);
			else
				fi.CopyTo(fullName);
		}

		/// <summary>
		/// Restore non X360CE file.
		/// Restore file for "xinput1_3.dll" file name will be "xinput1_3.HHHHHHHH.dll", where HHHHHHHH is CRC32 checksum in hex.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="move">True - delete original file, false - make a copy of backup file.</param>
		void RestoreFile(string fileName, bool move = false)
		{
			var fi = new FileInfo(fileName);
			var backupFiles = GetBackupFiles(fileName);
			// Return if no valid backup files found.
			if (backupFiles.Length == 0)
				return;
			var backupFile = backupFiles[0];
			if (move)
				backupFile.MoveTo(fi.FullName);
			else
				backupFile.CopyTo(fi.FullName);
		}

		/// <summary>
		/// Check for unnecessary INI file.
		/// </summary>
		/// <param name="fix">True - fix the issue, false - return status only.</param>
		/// <param name="iniFileName"></param>
		/// <returns></returns>
		GameRefreshStatus CheckUnnecessarySettingFile(bool fix, string iniFileName)
		{
			// If INI file exists then...
			if (File.Exists(iniFileName))
			{
				if (!fix)
					return GameRefreshStatus.UncecessarySettingFile;
				// Remove INI files.
				File.Delete(iniFileName);
			}
			return GameRefreshStatus.OK;
		}

		/// <summary>
		/// Check for unnecessary DLL file.
		/// </summary>
		/// <param name="fix">True - fix the issue, false - return status only.</param>
		/// <param name="dllFileName"></param>
		/// <returns></returns>
		GameRefreshStatus CheckUnnecessaryLibraryFile(bool fix, string dllFileName)
		{
			var fi = new FileInfo(dllFileName);
			var isCustom = EngineHelper.IsCustomLibrarry(fi.FullName);
			// If DLL file was made by X360CE then...
			if (isCustom.HasValue && isCustom.Value)
			{
				if (!fix)
					return GameRefreshStatus.UnnecessaryLibraryFile;
				// Remove DLL file.
				File.Delete(fi.FullName);
				// Delete Debug Information file if found.
				var name = Path.GetFileNameWithoutExtension(fi.Name);
				var pdbFi = new FileInfo(Path.Combine(fi.Directory.FullName, name + ".pdb"));
				if (pdbFi.Exists)
					pdbFi.Delete();
				RestoreFile(fi.FullName);
			}
			return GameRefreshStatus.OK;
		}

		/// <summary>
		/// Write INI setting file content if different.
		/// </summary>
		/// <param name="fix">True - fix the issue, false - return status only.</param>
		/// <param name="game"></param>
		/// <returns></returns>
		/// <remarks>Game INI file location will be determined from game settings.</remarks>
		GameRefreshStatus CheckSettingFileContent(bool fix, UserGame game)
		{
			var settingsDirectory = game.GetLibraryAndSettingsDirectory();
			var fullName = Path.Combine(settingsDirectory.FullName, IniFileName);
			var content = GetIniContent(game);
			var bytes = SettingsHelper.GetFileContentBytes(content, Encoding.Unicode);
			var isDifferent = SettingsHelper.IsDifferent(fullName, bytes);
			if (isDifferent)
			{
				if (fix)
				{
					File.WriteAllBytes(fullName, bytes);
					saveCount++;
					var ev = ConfigSaved;
					if (ev != null)
					{
						ev(this, new SettingEventArgs(typeof(PadSetting).Name, saveCount));
					}
				}
				else
				{
					var iniExists = File.Exists(fullName);
					return iniExists
						? GameRefreshStatus.IniDifferent
						: GameRefreshStatus.IniNotExist;
				}
			}
			return GameRefreshStatus.OK;
		}

	}
}
