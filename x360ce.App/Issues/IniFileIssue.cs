using System;
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

		public override void Check()
		{
			Description = string.Format("'{0}' was not found.\r\nThis file is required for emulator to function properly.\r\n\r\nDo you want to create this file?", SettingManager.IniFileName);
			Severity = System.IO.File.Exists(SettingManager.IniFileName)
				? IssueSeverity.None
				: IssueSeverity.Critical;
		}

		public override void Fix()
		{
			var resourceName = this.GetType().Namespace + ".Presets." + SettingManager.IniFileName;
			AppHelper.WriteFile(resourceName, SettingManager.IniFileName);
		}

	}
}
