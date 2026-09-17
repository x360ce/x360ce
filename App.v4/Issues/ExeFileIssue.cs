using System;
using System.IO;
using System.Windows.Forms;
using JocysCom.ClassLibrary.Win32;
using JocysCom.ClassLibrary.Controls.IssuesControl;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Where the program is running from, and whether it can write beside itself there.
	/// </summary>
	public class ExeFileIssue : IssueItem
	{
		public ExeFileIssue() : base()
		{
			Name = "EXE File";
			FixName = "Fix";
			// Copying into a protected folder raises a prompt. Marked, so it is expected.
			FixNeedsAdmin = true;
		}

		/// <summary>The program folder is protected and the process is not elevated.</summary>
		const int FixRunElevated = 2;

		/// <summary>
		/// Whether Windows protects this folder from an ordinary user's writes. The program keeps
		/// the game's x360ce.ini beside itself, so from such a folder settings cannot be saved
		/// unless the program runs as administrator.
		/// </summary>
		public static bool IsProtectedFolder(string folder)
		{
			if (string.IsNullOrEmpty(folder))
				return false;
			var roots = new[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				Environment.GetFolderPath(Environment.SpecialFolder.Windows),
			};
			foreach (var root in roots)
			{
				if (string.IsNullOrEmpty(root))
					continue;
				var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
				if (folder.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(folder.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		public override void CheckTask()
		{
			var fi = new FileInfo(Application.ExecutablePath);
			var winFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);
			if (fi.FullName.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
			{
				SetSeverity(
					IssueSeverity.Critical, 1,
					string.Format("Do not run X360CE Application from Windows folder.")
				);
				return;
			}
			if (IsProtectedFolder(fi.DirectoryName) && !WinAPI.IsElevated())
			{
				SetSeverity(
					IssueSeverity.Important, FixRunElevated,
					"The program's folder is protected, so settings beside it cannot be saved. " +
					"Run the program as administrator, or copy it somewhere you can write to."
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
			else if (FixType == FixRunElevated)
			{
				WinAPI.RunElevatedAsync(Application.ExecutablePath, null);
				// This instance ends because an elevated one takes over.
				Application.Exit();
			}
		}
	}
}
