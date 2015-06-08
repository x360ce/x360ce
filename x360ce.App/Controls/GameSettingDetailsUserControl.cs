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
	public partial class GameSettingDetailsUserControl : UserControl
	{
		public GameSettingDetailsUserControl()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			var paItems = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
			DInputCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("DInput")).ToArray();
			XInputCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("XInput")).ToArray();
			HookCheckBoxes = Controls.OfType<CheckBox>().Where(x => x.Name.StartsWith("Hook")).ToArray();
			foreach (var item in paItems) ProcessorArchitectureComboBox.Items.Add(item);
			lock (CurrentGameLock)
			{
				EnableEvents();
			}
		}

		internal bool IsDesignMode
		{
			get
			{
				if (DesignMode) return true;
				if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return true;
				var pa = this.ParentForm;
				if (pa != null && pa.GetType().FullName.Contains("VisualStudio")) return true;
				return false;
			}
		}

		object CurrentGameLock = new object();
		bool EnabledEvents = false;
		bool ApplySettingsToFolderInstantly = false;

		CheckBox[] XInputCheckBoxes;
		CheckBox[] DInputCheckBoxes;
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
				UpdateFakeVidPidControls();
				UpdateDinputControls();
			}
		}

		void UpdateInterface()
		{
			var en = (CurrentGame != null);
			var item = CurrentGame ?? new x360ce.Engine.Data.Game();
			var dInputMask = (DInputMask)item.DInputMask;
			var xInputMask = (XInputMask)item.XInputMask;
			var hookMask = (HookMask)item.HookMask;
			SetMask(en, hookMask, dInputMask, xInputMask, item.FullPath, item.ProcessorArchitecture);
			HookModeFakeVidNumericUpDown_ValueChanged2(null, null);
			HookModeFakeVidNumericUpDown.Value = item.FakeVID;
			HookModeFakePidNumericUpDown.Value = item.FakePID;
			HookModeFakePidNumericUpDown_ValueChanged2(null, null);
			TimeoutNumericUpDown.Value = item.Timeout;
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

		public void SetMask(bool en, HookMask hookMask, DInputMask dInputMask, XInputMask xInputMask, string path, int proc)
		{
			lock (CurrentGameLock)
			{
				if (EnabledEvents) DisableEvents();
				SetMask<DInputMask>(DInputCheckBoxes, dInputMask);
				SetMask<XInputMask>(XInputCheckBoxes, xInputMask);
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
			foreach (var cb in DInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			HookModeFakeVidNumericUpDown.ValueChanged += HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged += HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged += this.TimeoutNumericUpDown_ValueChanged;
			EnabledEvents = true;
		}

		void DisableEvents()
		{
			foreach (var cb in DInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			HookModeFakeVidNumericUpDown.ValueChanged -= HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged -= HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged -= this.TimeoutNumericUpDown_ValueChanged;
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
				var is32bit = cbx.Name.Contains("x86");
				bool applySettings = true;
				CheckBox[] cbxList = null;
				if (XInputCheckBoxes.Contains(cbx)) cbxList = XInputCheckBoxes;
				if (DInputCheckBoxes.Contains(cbx)) cbxList = DInputCheckBoxes;
				if (cbxList != null)
				{
					// If 64-bit checkbox an checked then...
					if (is64bit && cbx.Checked)
					{
						// Make sure that 32-bit is unchecked
						var cbx32 = cbxList.First(x => x.Name == cbx.Name.Replace("x64", "x86"));
						if (cbx32.Checked)
						{
							cbx32.Checked = false;
							applySettings = false;
						}
					}
					// If 32-bit checkbox an checked then...
					if (is32bit && cbx.Checked)
					{
						// Make sure that 64-bit is unchecked
						var cbx64 = cbxList.First(x => x.Name == cbx.Name.Replace("x86", "x64"));
						if (cbx64.Checked)
						{
							cbx64.Checked = false;
							applySettings = false;
						}
					}
				}
				// Set DInput mask.
				var dm = (int)GetMask<DInputMask>(DInputCheckBoxes);
				CurrentGame.DInputMask = dm;
				DInputMaskTextBox.Text = dm.ToString("X8");
				// Set XInput mask.
				var xm = (int)GetMask<XInputMask>(XInputCheckBoxes);
				CurrentGame.XInputMask = xm;
				XInputMaskTextBox.Text = xm.ToString("X8");
				// Set hook mask.
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
				// Reset to default all properties which affects checksum.
				_CurrentGame.XInputMask = _DefaultSettings.XInputMask;
				_CurrentGame.HookMask = _DefaultSettings.HookMask;
				_CurrentGame.DInputMask = _DefaultSettings.DInputMask;
				_CurrentGame.DInputFile = _DefaultSettings.DInputFile ?? "";
				_CurrentGame.FakeVID = _DefaultSettings.FakeVID;
				_CurrentGame.FakePID = _DefaultSettings.FakePID;
				_CurrentGame.Timeout = _DefaultSettings.Timeout;
				UpdateInterface();
			}
		}

		private void DInputFileTextBox_TextChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null) return;
			item.DInputFile = DInputFileTextBox.Text;
		}

		private void HookModeFakeVidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null) return;
			item.FakeVID = (int)HookModeFakeVidNumericUpDown.Value;
		}

		private void HookModeFakeVidNumericUpDown_ValueChanged2(object sender, EventArgs e)
		{
			HookModeFakeVidTextBox.Text = "0x" + ((int)HookModeFakeVidNumericUpDown.Value).ToString("X4");
		}

		private void HookModeFakePidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null) return;
			item.FakePID = (int)HookModeFakePidNumericUpDown.Value;
		}

		private void HookModeFakePidNumericUpDown_ValueChanged2(object sender, EventArgs e)
		{
			HookModeFakePidTextBox.Text = "0x" + ((int)HookModeFakePidNumericUpDown.Value).ToString("X4");
		}

		private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null) return;
			item.Timeout = (int)TimeoutNumericUpDown.Value;
		}

		private void HookPIDVIDCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateFakeVidPidControls();
		}

		int msVid = 0x45E;
		int msPid = 0x28E;
		string dinputFile = "asiloader.dll";

		void UpdateFakeVidPidControls()
		{
			var en = HookPIDVIDCheckBox.Checked;
			HookModeFakeVidNumericUpDown.Enabled = en;
			HookModeFakePidNumericUpDown.Enabled = en;
			if (en)
			{
				if (HookModeFakeVidNumericUpDown.Value == 0)
				{
					HookModeFakeVidNumericUpDown.Value = msVid;
				}
				if (HookModeFakePidNumericUpDown.Value == 0)
				{
					HookModeFakePidNumericUpDown.Value = msPid;
				}
			}
			else
			{
				if (HookModeFakeVidNumericUpDown.Value == msVid)
				{
					HookModeFakeVidNumericUpDown.Value = 0;
				}
				if (HookModeFakePidNumericUpDown.Value == msPid)
				{
					HookModeFakePidNumericUpDown.Value = 0;
				}
			}
		}

		private void DInput8_x86CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateDinputControls();
		}

		private void DInput8_x64CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateDinputControls();
		}

		void UpdateDinputControls()
		{
			var en = DInput8_x86CheckBox.Checked || DInput8_x64CheckBox.Checked;
			DInputFileTextBox.Enabled = en;
			if (en)
			{
				if (DInputFileTextBox.Text == "")
				{
					DInputFileTextBox.Text = dinputFile;
				}
			}
			else
			{
				if (DInputFileTextBox.Text == dinputFile)
				{
					DInputFileTextBox.Text = "";
				}
			}
		}

	}
}
