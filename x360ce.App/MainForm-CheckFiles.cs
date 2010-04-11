using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using x360ce.App.XnaInput;
using System.Security.Permissions;
using System.Security;
using x360ce.App.Win32;

namespace x360ce.App
{
	public partial class MainForm
	{

		bool CheckFiles(bool createIfNotExist)
		{
			InstallFilesX360ceCheckBox.Checked = System.IO.File.Exists(iniFile);
			InstallFilesXinput13CheckBox.Checked = System.IO.File.Exists(dllFile3);
			InstallFilesXinput12CheckBox.Checked = System.IO.File.Exists(dllFile2);
			InstallFilesXinput11CheckBox.Checked = System.IO.File.Exists(dllFile1);
			InstallFilesXinput910CheckBox.Checked = System.IO.File.Exists(dllFile0);
			InstallFilesX360ceCheckBox.Enabled = IsFileSame(iniFile);
			InstallFilesXinput910CheckBox.Enabled = IsFileSame(dllFile0);
			InstallFilesXinput11CheckBox.Enabled = IsFileSame(dllFile1);
			InstallFilesXinput12CheckBox.Enabled = IsFileSame(dllFile2);
			InstallFilesXinput13CheckBox.Enabled = IsFileSame(dllFile3);
			if (createIfNotExist)
			{
				// If ini file doesn't exists.
				if (!System.IO.File.Exists(iniFile))
				{
					if (!CreateFile(iniFile)) return false;
				}
				// If xinput file doesn't exists.
				if (!System.IO.File.Exists(dllFile))
				{
					if (!CreateFile(dllFile)) return false;
				}
			}
			// Can't run witout ini.
			if (!File.Exists(iniFile))
			{
				MessageBox.Show(
					string.Format("Configuration file '{0}' is required for application to run!", iniFile),
					"Error", MessageBoxButtons.OK);
				this.Close();
				return false;
			}
			// If temp file exist then.
			FileInfo iniTmp = new FileInfo(iniTmpFile);
			if (iniTmp.Exists)
			{
				// It means that application crashed. Restore ini from temp.
				if (!CopyFile(iniTmp.FullName, iniFile)) return false;
			}
			else
			{
				// Create temp file to store original settings.
				if (!CopyFile(iniFile, iniTmpFile)) return false;
			}
			// Set status labels.
			StatusIsAdminLabel.Text = string.Format("Elevated: {0}", Win32.WinAPI.IsElevated);
			StatusIniLabel.Text = iniFile;
			var dllInfo = new System.IO.FileInfo(dllFile);
			if (dllInfo.Exists)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllInfo.FullName);
				StatusDllLabel.Text = dllFile + " " + vi.FileVersion;
			}
			return true;
		}

		bool IsFileSame(string fileName)
		{
			return false;
			//if (!System.IO.File.Exists(fileName)) return false;
			//var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			//StreamReader sr;
			//// Get MD5 of file on the disk.
			//sr = new StreamReader(fileName);
			//var dMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			//// Get MD5 of resource file.
			//if (fileName == dllFile0) fileName = dllFile;
			//if (fileName == dllFile1) fileName = dllFile;
			//if (fileName == dllFile2) fileName = dllFile;
			//if (fileName == dllFile3) fileName = dllFile;
			//var assembly = Assembly.GetExecutingAssembly();
			//sr = new StreamReader(assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName));
			//var rMd5 = new Guid(md5.ComputeHash(sr.BaseStream));
			//// return result.
			//return rMd5.Equals(dMd5);
		}

		bool CopyFile(string sourceFileName, string destFileName)
		{
			try
			{
				File.Copy(sourceFileName, destFileName, true);
			}
			catch (Exception)
			{
				Elevate();
				return false;
			}
			return true;
		}

		bool CreateFile(string fileName)
		{
			var answer = MessageBox.Show(
				string.Format("'{0}' file is missing.\r\nDo you want to create default file?", fileName),
				string.Format("Missing '{0}' file!", fileName),
				MessageBoxButtons.YesNo);
			if (answer == DialogResult.Yes)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var sr = assembly.GetManifestResourceStream(this.GetType().Namespace + ".Presets." + fileName);
				FileStream sw = null;
				try
				{
					sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
				}
				catch (Exception)
				{
					Elevate();
					return false;
				}
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
			return true;
		}

		void Elevate()
		{
			// If this is Vista/7 and is not elevated then elevate.
			if (WinAPI.IsVista && !WinAPI.IsElevated) WinAPI.RunElevated();
		}
	}
}
