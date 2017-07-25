using JocysCom.ClassLibrary.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using x360ce.Engine;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		// Check game settings against folder.
		public GameRefreshStatus GetDllAndIniStatus(x360ce.Engine.Data.UserGame game, bool fix = false)
		{
			var fi = new FileInfo(game.FullPath);
			// Check if game file exists.
			if (!fi.Exists)
			{
				return GameRefreshStatus.ExeNotExist;
			}
			// Check if game is not enabled.
			else if (!game.IsEnabled)
			{
				return GameRefreshStatus.OK;
			}
			else
			{
				var gameVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
				var xiValues = ((XInputMask[])Enum.GetValues(typeof(XInputMask))).Where(x => x != XInputMask.None).ToArray();
				// Create dictionary from XInput type and XInput file name.
				var dic = new Dictionary<XInputMask, string>();
				foreach (var value in xiValues)
				{
					dic.Add(value, JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value));
				}
				var content = GetIniContent(game);
				var bytes = SettingsHelper.GetFileConentBytes(content, Encoding.Unicode);
				var iniDirectory = Path.GetDirectoryName(game.FullPath);
				var initFullName = Path.Combine(iniDirectory, IniFileName);
				var isDifferent = SettingsHelper.IsDifferent(initFullName, bytes);
				if (isDifferent)
				{
					if (fix)
					{
						File.WriteAllBytes(initFullName, bytes);
					}
					else
					{
						return GameRefreshStatus.IniDifferent;
					}
				}
				var xiFileNames = dic.Values.Distinct();
				// Loop through all files.
				foreach (var xiFileName in xiFileNames)
				{
					var x64Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x64")).Key;
					var x86Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x86")).Key;
					var xiFullPath = System.IO.Path.Combine(fi.Directory.FullName, xiFileName);
					var xiFileInfo = new System.IO.FileInfo(xiFullPath);
					var xiArchitecture = ProcessorArchitecture.None;
					var x64Enabled = ((uint)game.XInputMask & (uint)x64Value) != 0; ;
					var x86Enabled = ((uint)game.XInputMask & (uint)x86Value) != 0; ;
					if (x86Enabled && x64Enabled) xiArchitecture = ProcessorArchitecture.MSIL;
					else if (x86Enabled) xiArchitecture = ProcessorArchitecture.X86;
					else if (x64Enabled) xiArchitecture = ProcessorArchitecture.Amd64;
					// If x360ce emulator for this game is disabled or both CheckBoxes are disabled or then...
					if (xiArchitecture == ProcessorArchitecture.None) // !game.IsEnabled || 
					{
						// If XInput file exists then...
						if (xiFileInfo.Exists)
						{
							if (fix)
							{
								// Delete unnecessary XInput file.
								xiFileInfo.Delete();
								continue;
							}
							else
							{
								return GameRefreshStatus.XInputFilesUnnecessary;
							}
						}
					}
					else
					{
						// If XInput file doesn't exists then...
						if (!xiFileInfo.Exists)
						{
							// Create XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							else return GameRefreshStatus.XInputFilesNotExist;
						}
						// Get current architecture.
						var xiCurrentArchitecture = Engine.Win32.PEReader.GetProcessorArchitecture(xiFullPath);
						// If processor architectures doesn't match then...
						if (xiArchitecture != xiCurrentArchitecture)
						{
							// Create XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							else return GameRefreshStatus.XInputFilesWrongPlatform;
						}
						bool byMicrosoft;
						var dllVersion = EngineHelper.GetDllVersion(xiFullPath, out byMicrosoft);
						var embededVersion = EngineHelper.GetEmbeddedDllVersion(xiCurrentArchitecture);
						// If file on disk is older then...
						if (dllVersion < embededVersion)
						{
							// Overwrite XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							return GameRefreshStatus.XInputFilesOlderVersion;
						}
						else if (dllVersion > embededVersion)
						{
							// Allow new version.
							// return GameRefreshStatus.XInputFileNewerVersion;
						}
					}
				}
			}
			return GameRefreshStatus.OK;
		}


	}
}
