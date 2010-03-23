using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using x360ce.App.XnaInput;

namespace x360ce.App
{
	public partial class MainForm
	{

		void CheckFiles(bool createIfNotExist)
		{
			StatusIsAdminLabel.Text = string.Format("Elevated: {0}", Win32.WinAPI.IsElevated);

			InstallFilesX360ceCheckBox.Checked = System.IO.File.Exists(cIniFile);
			InstallFilesXinput13CheckBox.Checked = System.IO.File.Exists(cXinput3File);
			InstallFilesXinput12CheckBox.Checked = System.IO.File.Exists(cXinput2File);
			InstallFilesXinput11CheckBox.Checked = System.IO.File.Exists(cXinput1File);
			InstallFilesXinput910CheckBox.Checked = System.IO.File.Exists(cXinput0File);
			InstallFilesX360ceCheckBox.Enabled = IsFileSame(cIniFile);
			InstallFilesXinput910CheckBox.Enabled = IsFileSame(cXinput0File);
			InstallFilesXinput11CheckBox.Enabled = IsFileSame(cXinput1File);
			InstallFilesXinput12CheckBox.Enabled = IsFileSame(cXinput2File);
			InstallFilesXinput13CheckBox.Enabled = IsFileSame(cXinput3File);
			if (createIfNotExist)
			{
				// If ini file doesn't exists.
				if (!System.IO.File.Exists(cIniFile))
				{
					CreateFile(cIniFile);
				}
				// If xinput file doesn't exists.
				if (!System.IO.File.Exists(cXinput3File))
				{
					CreateFile(cXinput3File);
				}
			}
		}

		bool IsFileSame(string fileName)
		{
			return false;
			if (!System.IO.File.Exists(fileName)) return false;
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			StreamReader sr;
			// Get MD5 of file on the disk.
			sr = new StreamReader(fileName);
			var dMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			// Get MD5 of resource file.
			if (fileName == cXinput0File) fileName = cXinput3File;
			if (fileName == cXinput1File) fileName = cXinput3File;
			if (fileName == cXinput2File) fileName = cXinput3File;
			var assembly = Assembly.GetExecutingAssembly();
			sr = new StreamReader(assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName));
			var rMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			// return result.
			return rMd5.Equals(dMd5);
		}

		void CreateFile(string fileName)
		{

			var answer = MessageBox.Show(
				string.Format("'{0}' file is missing.\r\nDo you want to create default file?", fileName),
				string.Format("Missing '{0}' file!", fileName),
				MessageBoxButtons.YesNo);
			if (answer == DialogResult.Yes)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var sr = assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName);
				var sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
				var buffer = new byte[1024];
				while (true)
				{
					var count = sr.Read(buffer, 0, buffer.Length);
					if (count == 0) break;
					sw.Write(buffer, 0, count);
				}
				sr.Close();
				sw.Close();
			}
		}

	}
}
