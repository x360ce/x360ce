using System;
using System.IO;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Win32;
using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	public class ExeFileIssue : IssueItem
	{
		public ExeFileIssue() : base()
		{
			Name = "EXE File";
			FixName = "Fix";
		}

		public override void CheckTask()
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

		public override void FixTask()
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
				WinAPI.RunElevatedAsync(newFile.FullName, null);
				//Close this instance because we have an elevated instance
				Application.Exit();
			}
		}

	}
}
