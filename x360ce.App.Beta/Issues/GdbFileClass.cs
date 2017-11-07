using System.Linq;
using x360ce.Engine;

namespace x360ce.App.Issues
{
	public class GdbFileIssue : WarningItem
	{
		public GdbFileIssue() : base()
		{
			Name = "GDB File";
			FixName = "Fix";
		}

		bool enableCheck = true;

		public override void Check()
		{
			lock (checkLock)
			{
				if (!enableCheck) return;
				var required = SettingsManager.UserGames.Items.Any(x => x.EmulationType == (int)EmulationType.Library);
				if (!required)
				{
					SetSeverity(IssueSeverity.None);
					return;
				}
				var gdb = GameDatabaseManager.Current.GdbFile;
				var md5 = GameDatabaseManager.Current.Md5File;
				// If file exist then.
				if (gdb.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(gdb.FullName, false);
					if (rulesMustBeFixed)
					{
						SetSeverity(
							IssueSeverity.Critical, 1,
							string.Format("Can't write or modify '{0}' file.", gdb.FullName)
						);
						return;
					}
				}
				if (md5.Exists)
				{
					var rulesMustBeFixed = AppHelper.CheckExplicitAccessRulesAndAllowToModify(md5.FullName, false);
					if (rulesMustBeFixed)
					{
						SetSeverity(
							IssueSeverity.Critical, 2,
							string.Format("Can't write or modify '{0}' file.", md5.FullName)
						);
						return;
					}
				}
				enableCheck = false;
				SetSeverity(IssueSeverity.None);
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
			}
			RaiseFixApplied();
		}

	}
}
