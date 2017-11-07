using System;
using System.Reflection;
using Microsoft.Win32;
using x360ce.Engine;
using System.IO;
using System.Windows.Forms;

namespace x360ce.App.Issues
{
	public class ExeFileIssue : WarningItem
	{
		public ExeFileIssue() : base()
		{
			Name = "EXE File";
			FixName = "Fix";
		}

		public override void Check()
		{
			var fi = new FileInfo(Application.ExecutablePath);
			var winFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);
			var insideWindowsFolder = fi.FullName.StartsWith(winFolder, StringComparison.InvariantCultureIgnoreCase);
			if (insideWindowsFolder)
			{
				SetSeverity(
					IssueSeverity.Critical, 1,
					string.Format("Do not run X360CE Application from Windows folder.")
				);
				return;
			}
			SetSeverity(IssueSeverity.None);
		}

		public override void Fix()
		{
			if (FixType == 1)
			{
				var fi = new FileInfo(Application.ExecutablePath);
				var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				var newPath = Path.Combine(path, Application.CompanyName);
				newPath = Path.Combine(newPath, Application.ProductName);
				newPath = Path.Combine(newPath, fi.Name);
				var newFile = new FileInfo(newPath);
				if (!newFile.Exists)
				{
					if (!newFile.Directory.Exists)
					{
						newFile.Directory.Create();
					}
					fi.CopyTo(newFile.FullName);
				}
				x360ce.Engine.Win32.WinAPI.RunElevatedAsync(newFile.FullName, null);
				//Close this instance because we have an elevated instance
				Application.Exit();
			}
			RaiseFixApplied();
		}

	}
}
