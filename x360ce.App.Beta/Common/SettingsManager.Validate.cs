using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Win32;
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
				var content = GetIniContent(game);
				var bytes = SettingsHelper.GetFileConentBytes(content, Encoding.Unicode);
				var iniDirectory = Path.GetDirectoryName(game.FullPath);
				var iniFullName = Path.Combine(iniDirectory, IniFileName);
				var isDifferent = SettingsHelper.IsDifferent(iniFullName, bytes);
				if (isDifferent)
				{
					if (fix)
					{
						File.WriteAllBytes(iniFullName, bytes);
					}
					else
					{
						var iniExists = File.Exists(iniFullName);
						status |= (iniExists ? GameRefreshStatus.IniDifferent : GameRefreshStatus.IniNotExist);
					}
				}
				var xiValues = ((XInputMask[])Enum.GetValues(typeof(XInputMask))).Where(x => x != XInputMask.None).ToArray();
				// Create dictionary from XInput type and XInput file name.
				var dic = new Dictionary<XInputMask, string>();
				foreach (var value in xiValues)
				{
					dic.Add(value, Attributes.GetDescription(value));
				}
				// Get names of files: xinput9_1_0.dll, xinput1_1.dll, xinput1_2.dll, xinput1_3.dll, xinput1_4.dll
				var xiFileNames = dic.Values.Distinct();
				// Loop through XInput DLL files.
				foreach (var xiFileName in xiFileNames)
				{
					// Get enum linked to 64-bit version of the file.
					var x64Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x64")).Key;
					// Get enum linked to 32-bit version of the file.
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
					// Get embeded resource name for current xinput file.
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
								status |= GameRefreshStatus.XInputFilesUnnecessary;
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
							// Get version of embeded XInput DLL.
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
			return status;
		}

	}
}
