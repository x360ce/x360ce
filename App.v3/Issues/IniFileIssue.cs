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
			FixName = "Fix";
			Description = "";
		}

		bool enableCheck = true;
		int FixType = 0;

		public override void Check()
		{
            lock (checkLock)
			{
				// Note: further check will be disabled if no issues were found.
				if (!enableCheck) return;
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
						FixType = 0;
						enableCheck = false;
					}
					else
					{
						Description = string.Format("Failed to restore '{0}' file from '{1}' file.", SettingManager.IniFileName, tmpFile.Name);
						Severity = IssueSeverity.Critical;
						FixType = 0;
					}
				}
				else if (iniFile.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(SettingManager.IniFileName, false);
					if (rulesMustBeFixed)
					{
						Description = string.Format("Can't write or modify '{0}' file.", SettingManager.IniFileName);
						Severity = IssueSeverity.Critical;
						FixType = 1;
					}
					// Create temp file to store original settings.
					else if (AppHelper.CopyFile(SettingManager.IniFileName, SettingManager.TmpFileName))
					{
						Severity = IssueSeverity.None;
						Description = "";
						FixType = 0;
						enableCheck = false;
					}
					else
					{
						Description = string.Format("Failed to backup '{0}' file to '{1}' file.", SettingManager.IniFileName, tmpFile.Name);
						Severity = IssueSeverity.Critical;
						FixType = 0;
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
						FixType = 0;
						enableCheck = false;
					}
					else
					{
						Description = string.Format("Failed create '{0}' file.", SettingManager.IniFileName);
						Severity = IssueSeverity.Critical;
						FixType = 0;
					}
				}
			}
		}

		public override void Fix()
		{
			lock (checkLock)
			{
				if (FixType == 1)
				{
					AppHelper.CheckExplicitAccessRulesAndAllowToModify(SettingManager.IniFileName, true);
				}
			}
			RaiseFixApplied();
        }

	}
}
