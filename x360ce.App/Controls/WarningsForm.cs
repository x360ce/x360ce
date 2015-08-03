using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public partial class WarningsForm : Form
	{
		public WarningsForm()
		{
			InitializeComponent();
			checkTimer = new System.Timers.Timer();
			checkTimer.Interval = 5000;
			checkTimer.AutoReset = false;
			checkTimer.SynchronizingObject = this;
			checkTimer.Elapsed += CheckTimer_Elapsed;
			WarningsDataGridView.AutoGenerateColumns = false;
			WarningsDataGridView.DataSource = Warnings;
			Text = EngineHelper.GetProductFullName() + " - Warnings";
		}

		static object currentLock = new object();
		static WarningsForm _Current;
		public static WarningsForm Current
		{
			get
			{
				lock (currentLock)
				{
					if (_Current == null)
					{
						_Current = new WarningsForm();
					}
					return _Current;
				}
			}
		}


		public static void CheckAndOpen()
		{
			Current.CheckAll();
			Current.checkTimer.Start();
		}

		private void CheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock (checkTimerLock)
			{
				// If timer is disposed then return;
				if (checkTimer == null) return;
				CheckAll();

			}
			checkTimer.Start();
		}

		BindingList<WarningItem> Warnings = new BindingList<WarningItem>();
		object checkTimerLock = new object();
		System.Timers.Timer checkTimer;
		bool IgnoreAll;


		void CheckAll()
		{
			var result = new WarningItem("Check");
			try
			{
				var result2 = IsDirectXInstalled();
				UpdateWarning(result2);

				var architectures = new Dictionary<string, ProcessorArchitecture>();
				var architecture = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
				var exes = System.IO.Directory.GetFiles(".", "*.exe", System.IO.SearchOption.TopDirectoryOnly);
				foreach (var exe in exes)
				{
					var pa = Engine.Win32.PEReader.GetProcessorArchitecture(exe);
					architectures.Add(exe, pa);
				}
				var fi = new FileInfo(Application.ExecutablePath);
				// Select all architectures of executables.
				var archs = architectures.Where(x => !x.Key.ToLower().Contains("x360ce")).Select(x => x.Value).ToArray();
				var x86Count = archs.Count(x => x == ProcessorArchitecture.X86);
				var x64Count = archs.Count(x => x == ProcessorArchitecture.Amd64);
				// If executables are 32-bit, but this program is 64-bit then warn user.
				var result86 = new WarningItem("32-bit Game");
				if (x86Count > 0 && x64Count == 0 && architecture == ProcessorArchitecture.Amd64)
				{
					result86.Description = "This folder contains 32-bit game. You should use 32-bit X360CE Application:\r\n" +
					"http://www.x360ce.com/Files/x360ce.zip";
					result86.FixAction = delegate () { EngineHelper.OpenUrl("http://www.x360ce.com/Files/x360ce.zip"); };
					result86.FixName = "Download";
				}
				UpdateWarning(result86);
				var result64 = new WarningItem("64-bit Game");
				// If executables are 64-bit, but this program is 32-bit then warn user.
				if (x64Count > 0 && x86Count == 0 && architecture == ProcessorArchitecture.X86)
				{
					result64.Description = "This folder contains 64-bit game. You should use 64-bit X360CE Application:\r\n" +
					"http://www.x360ce.com/Files/x360ce_x64.zip";
					result64.FixAction = delegate () { EngineHelper.OpenUrl("http://www.x360ce.com/Files/x360ce_x64.zip"); };
					result64.FixName = "Download";
				}
				UpdateWarning(result64);
				// Get list of debug files.
				var pdbs = System.IO.Directory.GetFiles(".", "*.pdb", System.IO.SearchOption.TopDirectoryOnly);
				// If debug files were found then...
				if (pdbs.Length > 0)
				{
					var result3 = IsMdkInstalled();
					UpdateWarning(result3);
					var result4 = IsLeakDetectorInstalled();
					UpdateWarning(result4);
				}
			}
			catch (Exception ex)
			{
				result.Description = ex.Message;
				result.FixName = "";
			}
			UpdateWarning(result);
			MainForm.Current.BeginInvoke((MethodInvoker)delegate ()
			{
				if (Warnings.Count > 0 && !Visible && !IgnoreAll)
				{

					StartPosition = FormStartPosition.CenterScreen;
					ShowDialog(MainForm.Current);
				}
				else if (Warnings.Count == 0 && Visible)
				{
					DialogResult = DialogResult.OK;
				}
			});
		}

		void UpdateWarning(WarningItem result)
		{
			var item = Warnings.FirstOrDefault(x => x.Name == result.Name);
			if (result.Description == null && item != null) Warnings.Remove(item);
			else if (result.Description != null && item == null) Warnings.Add(result);
		}

		WarningItem IsDirectXInstalled()
		{
			var result = new WarningItem("DirectX");
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
			{
				string versionString = key.GetValue("Version") as string;
				Version version;
				if (Version.TryParse(versionString, out version))
				{
					if (version.Minor == 9) return result;
				}
			}
			result.Description = "Microsoft DirectX 9 not found You can click the link below to download Microsoft DirectX:\r\n" +
				"http://www.microsoft.com/en-us/download/details.aspx?id=35";
			result.FixAction = delegate () { EngineHelper.OpenUrl("http://www.microsoft.com/en-us/download/details.aspx?id=35"); };
			result.FixName = "Download";
			return result;
		}

		WarningItem IsMdkInstalled()
		{
			var result = new WarningItem("Microsoft SDK");
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows"))
			{
				string versionString = key.GetValue("CurrentVersion") as string;
				Version version;
				if (Version.TryParse(versionString, out version))
				{
					if (version.Major >= 7) return result;
				}
			}
			result.Description = "You are using debug version of XInput Library. Microsoft SDK not found You can click the link below to download Microsoft SDK:\r\n" +
				"https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx";
			result.FixAction = delegate () { EngineHelper.OpenUrl("https://msdn.microsoft.com/en-us/microsoft-sdks-msdn.aspx"); };
			result.FixName = "Download";
			return result;
		}


		WarningItem IsLeakDetectorInstalled()
		{
			var result = new WarningItem("Leak Detector");
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
			{
				foreach (string subkey_name in key.GetSubKeyNames())
				{
					using (RegistryKey subkey = key.OpenSubKey(subkey_name))
					{
						var displayName = (string)subkey.GetValue("DisplayName", "");
						if (displayName.StartsWith("Visual Leak Detector"))
						{
							return result;
						}
					}
				}
			}
			result.Description = "You are using debug version of XInput Library. Visual Leak Detector not found You can click the link below to download Visual Leak Detector:\r\n" +
				"https://vld.codeplex.com";
			result.FixAction = delegate () { EngineHelper.OpenUrl("https://vld.codeplex.com"); };
			result.FixName = "Download";
            return result;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (checkTimerLock)
				{
					if (checkTimer != null)
					{
						checkTimer.Dispose();
						checkTimer = null;
					}
				}
				if (components != null) components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void Closebutton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void WarningsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
			{
				var row = grid.Rows[e.RowIndex];
				var item = (WarningItem)row.DataBoundItem;
				if (item.FixAction != null) item.FixAction();
			}
		}

		private void IgnoreButton_Click(object sender, EventArgs e)
		{
			IgnoreAll = true;
			DialogResult = DialogResult.Cancel;
		}
	}

}
