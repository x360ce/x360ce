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
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	public partial class GameDetailsUserControl : UserControl
	{
		public GameDetailsUserControl()
		{
			InitializeComponent();
			if (IsDesignMode) return;
			DInputCheckBoxes = DInputMaskGroupBox.Controls.OfType<CheckBox>().ToArray();
			XInputCheckBoxes = XInputMaskGroupBox.Controls.OfType<CheckBox>().ToArray();
			HookCheckBoxes = HookMaskGroupBox.Controls.OfType<CheckBox>().ToArray();
			AutoMapCheckBoxes = AutoMapMaskGroupBox.Controls.OfType<CheckBox>().ToArray();
			// Fill architecture combo box.
			var paItems = (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
			foreach (var item in paItems) ProcessorArchitectureComboBox.Items.Add(item);
			// Fill emulation type combo box.
			var etItems = (EmulationType[])Enum.GetValues(typeof(EmulationType));
			foreach (var item in etItems) EmulationTypeComboBox.Items.Add(item);
			lock (CurrentGameLock)
			{
				EnableEvents();
			}
		}

		internal bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		object CurrentGameLock = new object();
		bool EnabledEvents = false;
		bool ApplySettingsToFolderInstantly = false;

		CheckBox[] XInputCheckBoxes;
		CheckBox[] DInputCheckBoxes;
		CheckBox[] HookCheckBoxes;
		CheckBox[] AutoMapCheckBoxes;

		x360ce.Engine.Data.UserGame _CurrentGame;
		x360ce.Engine.Data.Program _DefaultSettings;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.UserGame CurrentGame
		{
			get { return _CurrentGame; }
			set
			{
				if (_CurrentGame != null)
				{
					// Detach event from old game.
					_CurrentGame.PropertyChanged -= CurrentGame_PropertyChanged;
				}
				// Assign new value
				_CurrentGame = value;
				if (_CurrentGame != null)
				{
					// attach event to new game.
					_CurrentGame.PropertyChanged += CurrentGame_PropertyChanged;
				}
				UpdateInterface();
				UpdateFakeVidPidControls();
				UpdateDinputControls();
				UpdateHelpButtons();
			}
		}

		/// <summary>
		/// Event will be triggered if somebody will change property directly on an object.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var game = CurrentGame;
			if (game == null)
				return;
			lock (CurrentGameLock)
			{
				// Set values without triggering events.
				DisableEvents();
				if (e.PropertyName == AppHelper.GetPropertyName<UserGame>(x => x.EmulationType))
					AppHelper.SetItem(EmulationTypeComboBox, (EmulationType)game.EmulationType);
				EnableEvents();
			}
		}

		void UpdateInterface()
		{
			var en = (CurrentGame != null);
			var item = CurrentGame ?? new x360ce.Engine.Data.UserGame();
			var dInputMask = (DInputMask)item.DInputMask;
			var xInputMask = (XInputMask)item.XInputMask;
			var hookMask = (HookMask)item.HookMask;
			var autoMapMask = (MapToMask)item.AutoMapMask;
			SetMask(en, hookMask, dInputMask, xInputMask, autoMapMask, item.FullPath, item.ProcessorArchitecture);
			EmulationTypeComboBox.SelectedItem = Enum.IsDefined(typeof(EmulationType), item.EmulationType)
					? (EmulationType)item.EmulationType
					: EmulationType.None;
			HookModeFakeVidNumericUpDown_ValueChanged2(null, null);
			HookModeFakeVidNumericUpDown.Value = item.FakeVID;
			HookModeFakePidNumericUpDown.Value = item.FakePID;
			HookModeFakePidNumericUpDown_ValueChanged2(null, null);
			TimeoutNumericUpDown.Value = item.Timeout;
			if (en)
			{
				var status = SettingsManager.Current.GetDllAndIniStatus(CurrentGame, false);
				ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
				SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK);
				_DefaultSettings = SettingsManager.Programs.Items.FirstOrDefault(x => x.FileName == CurrentGame.FileName);
				ResetToDefaultButton.Enabled = _DefaultSettings != null;
				if (ApplySettingsToFolderInstantly)
				{

				}
			}
		}


		public void SetMask(bool showButtons, HookMask hookMask, DInputMask dInputMask, XInputMask xInputMask, MapToMask autoMapMask, string path, int processorArchitecture)
		{
			lock (CurrentGameLock)
			{
				DisableEvents();
				// Set textboxes
				SetMask(DInputCheckBoxes, dInputMask);
				SetMask(XInputCheckBoxes, xInputMask);
				SetMask(HookCheckBoxes, hookMask);
				SetMask(AutoMapCheckBoxes, autoMapMask);
				// Processor architecture.
				ProcessorArchitectureComboBox.SelectedItem = Enum.IsDefined(typeof(ProcessorArchitecture), processorArchitecture)
					? (ProcessorArchitecture)processorArchitecture
					: ProcessorArchitecture.None;
				SynchronizeSettingsButton.Visible = showButtons;
				ResetToDefaultButton.Visible = showButtons;
				// Enable events.
				EnableEvents();
			}
		}

		T GetMask<T>(CheckBox[] boxes) where T : struct
		{
			uint mask = 0;
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get CheckBox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null && cb.Checked) mask |= (uint)(object)value;
			}
			return (T)(object)mask;
		}

		void SetMask<T>(CheckBox[] boxes, T mask) where T : struct
		{
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get CheckBox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null)
				{
					var v = Convert.ToUInt32(value);
					var m = Convert.ToUInt32(mask);
					cb.Checked = ((m & v) != 0);
				}
			}
		}

		/// <summary>
		/// Run inside "CurrentGameLock" lock only.
		/// </summary>
		void EnableEvents()
		{
			if (EnabledEvents)
				return;
			foreach (var cb in DInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			foreach (var cb in AutoMapCheckBoxes) cb.CheckedChanged += CheckBox_Changed;
			HookModeFakeVidNumericUpDown.ValueChanged += HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged += HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged += TimeoutNumericUpDown_ValueChanged;
			EmulationTypeComboBox.SelectedIndexChanged += EmulationTypeComboBox_SelectedIndexChanged;
			EnabledEvents = true;
		}

		/// <summary>
		/// Run inside "CurrentGameLock" lock only.
		/// </summary>
		void DisableEvents()
		{
			if (!EnabledEvents)
				return;
			foreach (var cb in DInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in XInputCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in HookCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			foreach (var cb in AutoMapCheckBoxes) cb.CheckedChanged -= CheckBox_Changed;
			HookModeFakeVidNumericUpDown.ValueChanged -= HookModeFakeVidNumericUpDown_ValueChanged;
			HookModeFakePidNumericUpDown.ValueChanged -= HookModeFakePidNumericUpDown_ValueChanged;
			TimeoutNumericUpDown.ValueChanged -= TimeoutNumericUpDown_ValueChanged;
			EmulationTypeComboBox.SelectedIndexChanged -= EmulationTypeComboBox_SelectedIndexChanged;
			EnabledEvents = false;
		}

		/// <summary>
		/// CheckBox events could fire at the same time.
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
					// If 64-bit CheckBox an checked then...
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
					// If 32-bit CheckBox an checked then...
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
				DInputMaskGroupBox.Text = dm.ToString("X8");
				// Set XInput mask.
				var xm = (int)GetMask<XInputMask>(XInputCheckBoxes);
				CurrentGame.XInputMask = xm;
				XInputMaskGroupBox.Text = xm.ToString("X8");
				// Set hook mask.
				var hm = (int)GetMask<HookMask>(HookCheckBoxes);
				CurrentGame.HookMask = hm;
				HookMaskGroupBox.Text = string.Format("Hook Mask {0:X8}", hm);
				// Set auto map mask.
				var am = (int)GetMask<MapToMask>(AutoMapCheckBoxes);
				CurrentGame.AutoMapMask = am;
				AutoMapMaskGroupBox.Text = am.ToString("X8");
				SettingsManager.Save();
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
			var status = SettingsManager.Current.GetDllAndIniStatus(CurrentGame, false);
			var values = ((GameRefreshStatus[])Enum.GetValues(typeof(GameRefreshStatus))).Except(new[] { GameRefreshStatus.OK }).ToArray();
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
			var status = SettingsManager.Current.GetDllAndIniStatus(CurrentGame, true);
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
				_CurrentGame.AutoMapMask = (int)MapToMask.None;
				_CurrentGame.EnableMask = (int)MapToMask.None;
				_CurrentGame.EmulationType = (int)EmulationType.None;
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
			AppHelper.SetText(HookModeFakeVidTextBox, "0x{0:X4}", (int)HookModeFakeVidNumericUpDown.Value);
		}

		private void HookModeFakePidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null)
				return;
			var value = (int)HookModeFakePidNumericUpDown.Value;
			if (item.FakePID != value)
				item.FakePID = value;
		}

		private void HookModeFakePidNumericUpDown_ValueChanged2(object sender, EventArgs e)
		{
			AppHelper.SetText(HookModeFakePidTextBox, "0x{0:X4}", (int)HookModeFakePidNumericUpDown.Value);
		}

		private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null)
				return;
			var value = (int)TimeoutNumericUpDown.Value;
			if (item.Timeout != value)
				item.Timeout = value;
		}

		private void EmulationTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = CurrentGame;
			if (item == null)
				return;
			var value = (int)EmulationTypeComboBox.SelectedItem;
			if (item.EmulationType != value)
				item.EmulationType = value;
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

		string GetGoogleSearchUrl()
		{
			var c = CurrentGame;
			if (c == null) return "";
			var url = "https://www.google.co.uk/?#q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}

		string GetNGemuSearchUrl()
		{
			var c = CurrentGame;
			if (c == null) return "";
			var url = "http://ngemu.com/search/5815705?q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}
		string GetNGemuThreadUrl()
		{
			var c = CurrentGame;
			if (c == null) return "";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false);
			var url = "http://ngemu.com/threads/";
			url += System.Web.HttpUtility.UrlEncode(keyName) + "/";
			return url;
		}

		void UpdateHelpButtons()
		{
		}

		private void GoogleSearchButton_Click(object sender, EventArgs e)
		{
			EngineHelper.OpenUrl(GetGoogleSearchUrl());
		}

		private void NGEmuSearchLinkButton_Click(object sender, EventArgs e)
		{
			EngineHelper.OpenUrl(GetNGemuSearchUrl());
		}

		private void NGEmuThreadLinkButton_Click(object sender, EventArgs e)
		{
			EngineHelper.OpenUrl(GetNGemuThreadUrl());
		}

	}
}
