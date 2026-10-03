using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class GdbFileIssue : WarningItem
	{
		public GdbFileIssue()
		{
			Name = "GDB File";
			FixName = "Fix";
			Description = "";
		}

		bool enableCheck = true;
		int FixType = 0;

		public override void Check()
		{
			lock (checkLock)
			{
				if (!enableCheck) return;
				var gdb = GameDatabaseManager.Current.GdbFile;
				var md5 = GameDatabaseManager.Current.Md5File;
				//var xml = SettingsFile.Current.XmlFile;
				FixType = 0;
				// If file exist then.
				if (gdb.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(gdb.FullName, false);
					if (rulesMustBeFixed)
					{
						Description = string.Format("Can't write or modify '{0}' file.", gdb.FullName);
						Severity = IssueSeverity.Critical;
						FixType = 1;
					}
				}
				if (FixType == 0 && md5.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(md5.FullName, false);
					if (rulesMustBeFixed)
					{
						Description = string.Format("Can't write or modify '{0}' file.", md5.FullName);
						Severity = IssueSeverity.Critical;
						FixType = 2;
					}
				}
				//if (FixType == 0 && xml.Exists)
				//{
				//	var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(xml.FullName, false);
				//	if (rulesMustBeFixed)
				//	{
				//		Description = string.Format("Can't write or modify '{0}' file.", xml.FullName);
				//		Severity = IssueSeverity.Critical;
				//		FixType = 3;
				//	}
				//}
				if (FixType == 0)
				{
					Severity = IssueSeverity.None;
					Description = "";
					FixType = 0;
					enableCheck = false;
				}
			}
		}

		public override void Fix()
		{
			lock (checkLock)
			{
				if (FixType == 1)
				{
					var file = GameDatabaseManager.Current.GdbFile;
					AppHelper.CheckExplicitAccessRulesAndAllowToModify(file.FullName, true);
				}
				else if (FixType == 2)
				{
					var file = GameDatabaseManager.Current.Md5File;
					AppHelper.CheckExplicitAccessRulesAndAllowToModify(file.FullName, true);
				}
				//else if (FixType == 3)
				//{
				//	var file = SettingsFile.Current.XmlFile;
				//	AppHelper.CheckExplicitAccessRulesAndAllowToModify(file.FullName, true);
				//}
			}
			RaiseFixApplied();
		}

	}
}
