using System.IO;
using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class IniFileIssue : WarningItem
	{
		public IniFileIssue() : base()
		{
			Name = "INI File";
			FixName = "Fix";
			Description = "";
		}

		bool enableCheck = true;

		public override void Check()
		{
            lock (checkLock)
			{
				// Note: further check will be disabled if no issues were found.
				if (!enableCheck) return;
				var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Library);
				if (!required)
				{
					SetSeverity(IssueSeverity.None);
					return;
				}
				// If temp file exist then.
				var iniFile = new FileInfo(SettingsManager.IniFileName);
				var tmpFile = new FileInfo(SettingsManager.TmpFileName);
				if (tmpFile.Exists)
				{
					// It means that application crashed. Restore INI from temp.
					if (AppHelper.CopyFile(tmpFile.FullName, SettingsManager.IniFileName))
					{
						enableCheck = false;
						SetSeverity(IssueSeverity.None);
					}
					else
					{
						SetSeverity(
							IssueSeverity.Critical, 0,
							string.Format("Failed to restore '{0}' file from '{1}' file.", SettingsManager.IniFileName, tmpFile.Name)
						);
					}
				}
				else if (iniFile.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(SettingsManager.IniFileName, false);
					if (rulesMustBeFixed)
					{
						SetSeverity(
							IssueSeverity.Critical, 1,
							string.Format("Can't write or modify '{0}' file.", SettingsManager.IniFileName)
						);
					}
					// Create temp file to store original settings.
					else if (AppHelper.CopyFile(SettingsManager.IniFileName, SettingsManager.TmpFileName))
					{
						enableCheck = false;
						SetSeverity(IssueSeverity.None);
					}
					else
					{
						SetSeverity(
							IssueSeverity.Critical, 0,
							string.Format("Failed to backup '{0}' file to '{1}' file.", SettingsManager.IniFileName, tmpFile.Name)
						);
					}
				}
				else
				{
					// Create temp file to store original settings.
                    var resourceName = typeof(Program).Namespace + ".Presets." + SettingsManager.IniFileName;
					if (AppHelper.WriteFile(resourceName, SettingsManager.IniFileName))
					{
						enableCheck = false;
						SetSeverity(IssueSeverity.None);
					}
					else
					{
						SetSeverity(
							IssueSeverity.Critical, 0,
							string.Format("Failed create '{0}' file.", SettingsManager.IniFileName)
						);
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
					AppHelper.CheckExplicitAccessRulesAndAllowToModify(SettingsManager.IniFileName, true);
				}
			}
			RaiseFixApplied();
        }

	}
}
