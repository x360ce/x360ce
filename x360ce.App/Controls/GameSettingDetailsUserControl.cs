using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using x360ce.Engine;
using System.IO;

namespace x360ce.App.Controls
{
	public partial class GameSettingDetailsUserControl : BaseUserControl
	{
		public GameSettingDetailsUserControl()
		{
			InitializeComponent();
			var paItems = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
			XInputCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("XInput")).ToArray();
			HookCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("Hook")).ToArray();
			foreach (var item in paItems) ProcessorArchitectureComboBox.Items.Add(item);
			lock (CurrentGameLock)
			{
				EnableEvents();
			}
		}

		object CurrentGameLock = new object();
		bool EnabledEvents = false;
		bool ApplySettingsToFolderInstantly = false;

		CheckBox[] XInputCheckBoxes;
		CheckBox[] HookCheckBoxes;

		x360ce.Engine.Data.Game _CurrentGame;
		x360ce.Engine.Data.Program _DefaultSettings;

		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.Game CurrentGame
		{
			get { return _CurrentGame; }
			set
			{
				_CurrentGame = value;
				UpdateInterface();
			}
		}

		void UpdateInterface()
		{
			var en = (CurrentGame != null);
			var item = CurrentGame ?? new x360ce.Engine.Data.Game();
			var inputMask = (XInputMask)item.XInputMask;
			var hookMask = (HookMask)item.HookMask;
			SetMask(en, hookMask, inputMask, item.FullPath, item.ProcessorArchitecture);
			if (en)
			{
				var status = GetGameStatus(CurrentGame);
				ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
				SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK) && (status != GameRefreshStatus.None);
				_DefaultSettings = SettingsFile.Current.Programs.FirstOrDefault(x => x.FileName == CurrentGame.FileName);
				ResetToDefaultButton.Enabled = _DefaultSettings != null;
				if (ApplySettingsToFolderInstantly)
				{

				}
			}

		}

		// Check game settings against folder.
		public GameRefreshStatus GetGameStatus(x360ce.Engine.Data.Game game)
		{
			var fi = new FileInfo(game.FullPath);
			if (!game.IsEnabled)
			{
				return GameRefreshStatus.None;
			}
			// Check if game file exists.
			if (!fi.Exists)
			{
				return GameRefreshStatus.FileNotExist;
			}
			else
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
				var values = (XInputMask[])Enum.GetValues(typeof(XInputMask));
				foreach (var value in values)
				{
					// If value is enabled then...
					if (((uint)game.XInputMask & (uint)value) != 0)
					{
						// Get name of xInput file.
						var dllName = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value);
						var dllFullPath = System.IO.Path.Combine(fi.Directory.FullName, dllName);
						var dllFileInfo = new System.IO.FileInfo(dllFullPath);
						if (!dllFileInfo.Exists)
						{
							return GameRefreshStatus.XInputFileNotExist;
						}
						var arch = Engine.Win32.PEReader.GetProcessorArchitecture(dllFullPath);
						// If 64-bit selected but file is 32-bit then...
						if (value.ToString().Contains("x64") && arch == System.Reflection.ProcessorArchitecture.X86)
						{
							return GameRefreshStatus.XInputFileWrongPlatform;
						}
						// If 32-bit selected but file is 64-bit then...
						if (value.ToString().Contains("x86") && arch == System.Reflection.ProcessorArchitecture.Amd64)
						{
							return GameRefreshStatus.XInputFileWrongPlatform;
						}
						bool byMicrosoft;
						var dllVersion = EngineHelper.GetDllVersion(dllFullPath, out byMicrosoft);
						var embededVersion = EngineHelper.GetEmbeddedDllVersion(arch);
						if (dllVersion < embededVersion)
						{
							return GameRefreshStatus.XInputFileOlderVersion;
						}
						else if (dllVersion > embededVersion)
						{
							return GameRefreshStatus.XInputFileNewerVersion;
						}
					}
				}
			}
			return GameRefreshStatus.OK;
		}

		public void SetMask(bool en, HookMask hookMask, XInputMask inputMask, string path, int proc)
		{
			lock (CurrentGameLock)
			{
				if (EnabledEvents) DisableEvents();
				SetMask<XInputMask>(XInputCheckBoxes, inputMask);
				SetMask<HookMask>(HookCheckBoxes, hookMask);
				// Processor architecture.
				ProcessorArchitectureComboBox.SelectedItem = Enum.IsDefined(typeof(ProcessorArchitecture), proc)
					? (ProcessorArchitecture)proc
					: ProcessorArchitecture.None;
				SynchronizeSettingsButton.Visible = en;
				ResetToDefaultButton.Visible = en;
				// Enable events.
				EnableEvents();
			}
		}

		T GetMask<T>(CheckBox[] boxes)
		{
			uint mask = 0;
			// Check/Uncheck checkboxes.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get checkbox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null && cb.Checked) mask |= (uint)(object)value;
			}
			return (T)(object)mask;
		}

		void SetMask<T>(CheckBox[] boxes, T mask)
		{
			// Check/Uncheck checkboxes.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get checkbox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null) cb.Checked = (((uint)(object)mask & (uint)(object)value) != 0);
			}
		}

		void EnableEvents()
		{
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			EnabledEvents = true;
		}

		void DisableEvents()
		{
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			EnabledEvents = false;
		}

		void CheckBox_Changed(object sender, EventArgs e)
		{
			if (CurrentGame == null) return;
			var xm = (int)GetMask<XInputMask>(XInputCheckBoxes);
			CurrentGame.XInputMask = xm;
			XInputMaskTextBox.Text = xm.ToString("X8");
			var hm = (int)GetMask<HookMask>(HookCheckBoxes);
			CurrentGame.HookMask = hm;
			HookMaskTextBox.Text = hm.ToString("X8");
			SettingsFile.Current.Save();
		}

		void SetCheckXinput(XInputMask mask)
		{
			//if (CurrentGame == null) return;
			//var name = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(mask);
			//var path = System.IO.Path.GetDirectoryName(CurrentGame.FullPath);
			//var fullPath = System.IO.Path.Combine(path, name);
			//var box = (CheckBox)sender;
			//var exists = Helper.CreateDllFile(box.Checked, fullPath);
			//if (exists != box.Checked) box.Checked = exists;
		}

		private void SynchronizeSettingsButton_Click(object sender, EventArgs e)
		{
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Synchronize current settings to game folder?", "Synchronize", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (result == DialogResult.OK)
			{
			}
		}

		private void ResetToDefaultButton_Click(object sender, EventArgs e)
		{
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Reset current settings to default?", "Reset", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (result == DialogResult.OK)
			{
				_CurrentGame.XInputMask = _DefaultSettings.XInputMask;
				_CurrentGame.HookMask = _DefaultSettings.HookMask;
				UpdateInterface();
			}
		}

	}
}
