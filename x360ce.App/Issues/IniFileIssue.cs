using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class IniFileIssue : WarningItem
	{
		public IniFileIssue()
		{
			Name = "INI File";
			FixName = "Create";
			Description = "";
		}

		bool enableCheck = true;

		public override void Check()
		{
			lock (checkLock)
			{
				if (!enableCheck) return;
				enableCheck = false;
				// Description = string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", SettingManager.IniFileName);


				// If temp file exist then.
				var iniFile = new FileInfo(SettingManager.IniFileName);
				var tmpFile = new FileInfo(SettingManager.TmpFileName);
				if (tmpFile.Exists)
				{
					// It means that application crashed. Restore INI from temp.
					if (AppHelper.CopyFile(tmpFile.FullName, SettingManager.IniFileName))
					{
						Severity = IssueSeverity.None;
						Description = "";
					}
					else
					{
						Description = string.Format("Failed to restore '{0}' file from '{1}' file.", SettingManager.IniFileName, tmpFile.Name);
						Severity = IssueSeverity.Critical;
					}
				}
				else if (iniFile.Exists)
				{
					// Create temp file to store original settings.
					if (AppHelper.CopyFile(SettingManager.IniFileName, SettingManager.TmpFileName))
					{
						Severity = IssueSeverity.None;
						Description = "";
					}
					else
					{
						Description = string.Format("Failed to backup '{0}' file to '{1}' file.", SettingManager.IniFileName, tmpFile.Name);
						Severity = IssueSeverity.Critical;
					}
				}
				else
				{
					// Create temp file to store original settings.
                    var resourceName = typeof(Program).Namespace + ".Presets." + SettingManager.IniFileName;
					if (AppHelper.WriteFile(resourceName, SettingManager.IniFileName))
					{
						Severity = IssueSeverity.None;
						Description = "";
					}
					else
					{
						Description = string.Format("Failed create '{0}' file.", SettingManager.IniFileName);
						Severity = IssueSeverity.Critical;
					}
				}
			}
		}

		public override void Fix()
		{
		}

	}
}
