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
				var status = GetGameStatus(CurrentGame, false);
				ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
				SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK);
				_DefaultSettings = SettingsFile.Current.Programs.FirstOrDefault(x => x.FileName == CurrentGame.FileName);
				ResetToDefaultButton.Enabled = _DefaultSettings != null;
				if (ApplySettingsToFolderInstantly)
				{

				}
			}
		}

		// Check game settings against folder.
		public GameRefreshStatus GetGameStatus(x360ce.Engine.Data.Game game, bool fix = false)
		{
			var fi = new FileInfo(game.FullPath);
			// Check if game file exists.
			if (!fi.Exists)
			{
				return GameRefreshStatus.ExeNotExist;
			}
			// Check if game is not enabled.
			else if (!game.IsEnabled)
			{
				return GameRefreshStatus.OK;
			}
			else
			{
				var gameVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(fi.FullName);
				var xiValues = ((XInputMask[])Enum.GetValues(typeof(XInputMask))).Where(x => x != XInputMask.None).ToArray();
				// Create dictionary from XInput type and XInput file name.
				var dic = new Dictionary<XInputMask, string>();
				foreach (var value in xiValues)
				{
					dic.Add(value, JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value));
				}
				var xiFileNames = dic.Values.Distinct();
				// Loop through all files.
				foreach (var xiFileName in xiFileNames)
				{
					var x64Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x64")).Key;
					var x86Value = dic.First(x => x.Value == xiFileName && x.ToString().Contains("x86")).Key;
					var xiFullPath = System.IO.Path.Combine(fi.Directory.FullName, xiFileName);
					var xiFileInfo = new System.IO.FileInfo(xiFullPath);
					var xiArchitecture = ProcessorArchitecture.None;
					var x64Enabled = ((uint)game.XInputMask & (uint)x64Value) != 0; ;
					var x86Enabled = ((uint)game.XInputMask & (uint)x86Value) != 0; ;
					if (x86Enabled && x64Enabled) xiArchitecture = ProcessorArchitecture.MSIL;
					else if (x86Enabled) xiArchitecture = ProcessorArchitecture.X86;
					else if (x64Enabled) xiArchitecture = ProcessorArchitecture.Amd64;
					// If x360ce emulator for this game is disabled or both checkboxes are disabled or then...
					if (xiArchitecture == ProcessorArchitecture.None) // !game.IsEnabled || 
					{
						// If XInput file exists then...
						if (xiFileInfo.Exists)
						{
							if (fix)
							{
								// Delete unnecessary XInput file.
								xiFileInfo.Delete();
								continue;
							}
							else
							{
								return GameRefreshStatus.XInputFilesUnnecessary;
							}
						}
					}
					else
					{
						// If XInput file doesn't exists then...
						if (!xiFileInfo.Exists)
						{
							// Create XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							else return GameRefreshStatus.XInputFilesNotExist;
						}
						// Get current arcitecture.
						var xiCurrentArchitecture = Engine.Win32.PEReader.GetProcessorArchitecture(xiFullPath);
						// If processor architectures doesn't match then...
						if (xiArchitecture != xiCurrentArchitecture)
						{
							// Create XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							else return GameRefreshStatus.XInputFilesWrongPlatform;
						}
						bool byMicrosoft;
						var dllVersion = EngineHelper.GetDllVersion(xiFullPath, out byMicrosoft);
						var embededVersion = EngineHelper.GetEmbeddedDllVersion(xiCurrentArchitecture);
						// If file on disk is older then...
						if (dllVersion < embededVersion)
						{
							// Overwrite XInput file.
							if (fix)
							{
								AppHelper.WriteFile(EngineHelper.GetXInputResoureceName(xiArchitecture), xiFileInfo.FullName);
								continue;
							}
							return GameRefreshStatus.XInputFilesOlderVersion;
						}
						else if (dllVersion > embededVersion)
						{
							// Allow new version.
							// return GameRefreshStatus.XInputFileNewerVersion;
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

		/// <summary>
		/// Checkbox events could fire at the same time.
		/// Use lock to make sure that only one file is processed during synchronization.
		/// </summary>
		object CheckBoxLock = new object();

		void CheckBox_Changed(object sender, EventArgs e)
		{
			if (CurrentGame == null) return;
			lock (CheckBoxLock)
			{
				var cbx = (CheckBox)sender;
				var is64bit = cbx.Name.Contains("x64");
				bool applySettings = true;
				// If checked and it is 64-bit checkbox then...
				if (cbx.Checked && is64bit)
				{
					// Make sure that 32-bit is unchecked
					var cbx32 = XInputCheckBoxes.First(x => x.Name == cbx.Name.Replace("x64", "x86"));
					if (cbx32.Checked)
					{
						cbx32.Checked = false;
						applySettings = false;
					}
				}
				// If checked and it is 32-bit checkbox then...
				else if (cbx.Checked && !is64bit)
				{
					// Make sure that 64-bit is unchecked
					var cbx64 = XInputCheckBoxes.First(x => x.Name == cbx.Name.Replace("x86", "x64"));
					if (cbx64.Checked)
					{
						cbx64.Checked = false;
						applySettings = false;
					}
				}
				var xm = (int)GetMask<XInputMask>(XInputCheckBoxes);
				CurrentGame.XInputMask = xm;
				XInputMaskTextBox.Text = xm.ToString("X8");
				var hm = (int)GetMask<HookMask>(HookCheckBoxes);
				CurrentGame.HookMask = hm;
				HookMaskTextBox.Text = hm.ToString("X8");
				SettingsFile.Current.Save();
				if (applySettings && ApplySettingsToFolderInstantly) ApplySettings();
			}
		}

		void SetCheckXinput(XInputMask mask)
		{
			//if (CurrentGame == null) return;
			var name = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(mask);
			var path = System.IO.Path.GetDirectoryName(CurrentGame.FullPath);
			var fullPath = System.IO.Path.Combine(path, name);
			///var box = (CheckBox)sender;
			//var exists = AppHelper.CreateDllFile(, fullPath);
			//if (exists != box.Checked) box.Checked = exists;
		}

		private void SynchronizeSettingsButton_Click(object sender, EventArgs e)
		{
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var status = GetGameStatus(CurrentGame, false);
			var values = Enum.GetValues(typeof(GameRefreshStatus));
			List<string> errors = new List<string>();
			foreach (GameRefreshStatus value in values)
			{
				if (status.HasFlag(value))
				{
					var description = JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(value);
					errors.Add(description);
				}
			}
			var message = "Synchronize current settings to game folder?";
			message += "\r\n\r\n\tIssues:\r\n\r\n\t - " + string.Join("\r\n\t - ", errors);
			var result = form.ShowForm(message, "Synchronize", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (result == DialogResult.OK) ApplySettings();
		}

		void ApplySettings()
		{
			var status = GetGameStatus(CurrentGame, true);
			ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
			SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK) && (status != GameRefreshStatus.OK);
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
