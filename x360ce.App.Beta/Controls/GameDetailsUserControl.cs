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
			foreach (var item in paItems)
				ProcessorArchitectureComboBox.Items.Add(item);
			// Fill emulation type combo box.
			var etItems = (EmulationType[])Enum.GetValues(typeof(EmulationType));
			foreach (var item in etItems)
				EmulationTypeComboBox.Items.Add(item);
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

		x360ce.Engine.Data.IProgram _CurrentItem;
		x360ce.Engine.Data.Program _DefaultSettings;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public x360ce.Engine.Data.IProgram CurrentItem
		{
			get { return _CurrentItem; }
			set
			{
				lock (CurrentGameLock)
				{
					if (_CurrentItem != null)
					{
						// Detach event from old game.
						_CurrentItem.PropertyChanged -= CurrentGame_PropertyChanged;
					}
					// Assign new value
					_CurrentItem = value;
					// Update interface.
					DisableEvents();
					var isGame = _CurrentItem as UserGame != null;
					var item = _CurrentItem ?? new x360ce.Engine.Data.Program();
					// Set textboxes
					SetMask(DInputCheckBoxes, (DInputMask)item.DInputMask);
					SetMask(XInputCheckBoxes, (XInputMask)item.XInputMask);
					SetMask(HookCheckBoxes, (HookMask)item.HookMask);
					SetMask(AutoMapCheckBoxes, (MapToMask)item.AutoMapMask);
					HookModeFakeVidNumericUpDown.Value = item.FakeVID;
					HookModeFakePidNumericUpDown.Value = item.FakePID;
					TimeoutNumericUpDown.Value = item.Timeout;
					AppHelper.SetItem(ProcessorArchitectureComboBox, (ProcessorArchitecture)CurrentItem.ProcessorArchitecture);
					AppHelper.SetItem(EmulationTypeComboBox, (EmulationType)CurrentItem.EmulationType);
					var game = CurrentItem as UserGame;
					if (game != null)
					{
						var status = SettingsManager.Current.GetDllAndIniStatus(game, false);
						ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
						SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK);
						_DefaultSettings = SettingsManager.Programs.Items.FirstOrDefault(x => x.FileName == game.FileName);
						ResetToDefaultButton.Enabled = _DefaultSettings != null;
						if (ApplySettingsToFolderInstantly)
						{

						}
					}
					// Allow sync settings for game.
					SynchronizeSettingsButton.Visible = isGame;
					ResetToDefaultButton.Visible = isGame && _DefaultSettings != null;
					UpdateFakeVidPidControls();
					UpdateDinputControls();

					// Enable events.
					EnableEvents();
					if (_CurrentItem != null)
					{
						// attach event to new game.
						_CurrentItem.PropertyChanged += CurrentGame_PropertyChanged;
					}
				}
			}
		}

		/// <summary>
		/// Event will be triggered if somebody will change property directly on an object.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentGame_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var game = CurrentItem;
			if (game == null)
				return;
			lock (CurrentGameLock)
			{
				// Set values without triggering events.
				DisableEvents();
				if (e.PropertyName == AppHelper.GetPropertyName<UserGame>(x => x.EmulationType))
					AppHelper.SetItem(EmulationTypeComboBox, (EmulationType)game.EmulationType);
				if (e.PropertyName == AppHelper.GetPropertyName<UserGame>(x => x.ProcessorArchitecture))
					AppHelper.SetItem(ProcessorArchitectureComboBox, (ProcessorArchitecture)game.ProcessorArchitecture);
				EnableEvents();
			}
		}

		int GetMask<T>(CheckBox[] boxes) where T : struct, IConvertible
		{
			uint mask = 0;
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			foreach (var value in xs)
			{
				// Get CheckBox linked to enum value.
				var boxName = string.Format("{0}CheckBox", value);
				var cb = boxes.FirstOrDefault(x => x.Name.Equals(boxName, StringComparison.OrdinalIgnoreCase));
				if (cb != null && cb.Checked) mask |= (uint)(object)value;
			}
			return (int)mask;
		}

		void SetMask<T>(CheckBox[] boxes, T mask) where T : struct, IConvertible
		{
			// Check/Uncheck CheckBox.
			var xs = (T[])Enum.GetValues(typeof(T));
			var m = Convert.ToUInt32(mask);
			foreach (var value in xs)
			{
				// Get CheckBox linked to enum value.
				var cb = boxes.FirstOrDefault(x => x.Name.StartsWith(value.ToString()));
				if (cb != null)
				{
					var v = Convert.ToUInt32(value);
					cb.Checked = ((m & v) != 0);
				}
			}
			UpdateTitle(boxes.FirstOrDefault().Parent as GroupBox, (int)m);
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
			ProcessorArchitectureComboBox.SelectedIndexChanged += ProcessorArchitectureComboBox_SelectedIndexChanged;
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
			ProcessorArchitectureComboBox.SelectedIndexChanged -= ProcessorArchitectureComboBox_SelectedIndexChanged;
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
			if (CurrentItem == null) return;
			lock (CheckBoxLock)
			{
				var cbx = (CheckBox)sender;
				bool applySettings = true;
				CheckBox[] cbxList = null;
				if (XInputCheckBoxes.Contains(cbx)) cbxList = XInputCheckBoxes;
				if (DInputCheckBoxes.Contains(cbx)) cbxList = DInputCheckBoxes;
				if (cbxList != null)
				{
					var is64bit = cbx.Name.Contains("x64");
					var is32bit = cbx.Name.Contains("x86");
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
				int mask = 0;
				if (DInputCheckBoxes.Contains(cbx))
				{
					// Set DInput mask.DInputCheckBoxes
					mask = GetMask<DInputMask>(DInputCheckBoxes);
					if (CurrentItem.DInputMask != mask)
						CurrentItem.DInputMask = mask;
				}
				else if (XInputCheckBoxes.Contains(cbx))
				{
					// Set XInput mask.
					mask = GetMask<XInputMask>(XInputCheckBoxes);
					if (CurrentItem.XInputMask != mask)
						CurrentItem.XInputMask = mask;
				}
				else if (HookCheckBoxes.Contains(cbx))
				{
					// Set hook mask.
					mask = GetMask<HookMask>(HookCheckBoxes);
					if (CurrentItem.HookMask != mask)
						CurrentItem.HookMask = mask;
				}
				else if (AutoMapCheckBoxes.Contains(cbx))
				{
					// Set auto map mask.
					mask = GetMask<MapToMask>(AutoMapCheckBoxes);
					if (CurrentItem.AutoMapMask != mask)
						CurrentItem.AutoMapMask = mask;
				}
				UpdateTitle(cbx.Parent as GroupBox, mask);
				if (CurrentItem.EmulationType == (int)EmulationType.Library)
				{
					SettingsManager.Save();
					if (applySettings && ApplySettingsToFolderInstantly) ApplySettings();
				}
			}
		}

		void UpdateTitle(GroupBox gp, int mask)
		{
			var end = gp.Text.IndexOf(" - ") + 3;
			var prefix = gp.Text.Substring(0, end);
			AppHelper.SetText(gp, "{0}{1:X8}", prefix, mask);
		}

		/// <summary>
		/// Button must be available only if editing UserGame .
		/// </summary>
		private void SynchronizeSettingsButton_Click(object sender, EventArgs e)
		{
			var game = CurrentItem as UserGame;
			MessageBoxForm form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var status = SettingsManager.Current.GetDllAndIniStatus(game, false);
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
			var game = CurrentItem as UserGame;
			var status = SettingsManager.Current.GetDllAndIniStatus(game, true);
			ApplySettingsToFolderInstantly = (status == GameRefreshStatus.OK);
			SynchronizeSettingsButton.Visible = (status != GameRefreshStatus.OK) && (status != GameRefreshStatus.OK);
		}

		private void ResetToDefaultButton_Click(object sender, EventArgs e)
		{
			var game = CurrentItem;
			if (game == null)
				return;
			var program = _DefaultSettings;
			if (program == null)
				return;
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var result = form.ShowForm("Reset current settings to default?", "Reset", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (result != DialogResult.OK)
				return;
			// Reset to default all properties which affects checksum.
			game.XInputMask = program.XInputMask;
			game.HookMask = program.HookMask;
			game.AutoMapMask = (int)MapToMask.None;
			game.EmulationType = (int)EmulationType.None;
			game.DInputMask = program.DInputMask;
			game.DInputFile = program.DInputFile ?? "";
			game.FakeVID = program.FakeVID;
			game.FakePID = program.FakePID;
			game.Timeout = program.Timeout;
		}

		private void DInputFileTextBox_TextChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null) return;
			item.DInputFile = DInputFileTextBox.Text;
		}

		private void HookModeFakeVidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)HookModeFakeVidNumericUpDown.Value;
			if (item.FakeVID != value)
			{
				item.FakeVID = value;
				AppHelper.SetText(HookModeFakeVidTextBox, "0x{0:X4}", (int)HookModeFakeVidNumericUpDown.Value);
			}
		}

		private void HookModeFakePidNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)HookModeFakePidNumericUpDown.Value;
			if (item.FakePID != value)
			{
				item.FakePID = value;
				AppHelper.SetText(HookModeFakePidTextBox, "0x{0:X4}", (int)HookModeFakePidNumericUpDown.Value);
			}
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
					HookModeFakeVidNumericUpDown.Value = msVid;
				if (HookModeFakePidNumericUpDown.Value == 0)
					HookModeFakePidNumericUpDown.Value = msPid;
			}
			else
			{
				if (HookModeFakeVidNumericUpDown.Value == msVid)
					HookModeFakeVidNumericUpDown.Value = 0;
				if (HookModeFakePidNumericUpDown.Value == msPid)
					HookModeFakePidNumericUpDown.Value = 0;
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


		#region Update original item when user interacts with the interface

		private void TimeoutNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)TimeoutNumericUpDown.Value;
			if (item.Timeout != value)
				item.Timeout = value;
		}

		private void ProcessorArchitectureComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)ProcessorArchitectureComboBox.SelectedItem;
			if (item.ProcessorArchitecture != value)
				item.ProcessorArchitecture = value;
		}

		private void EmulationTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var item = CurrentItem;
			if (item == null)
				return;
			var value = (int)EmulationTypeComboBox.SelectedItem;
			if (item.EmulationType != value)
				item.EmulationType = value;
		}

		#endregion

		#region Help Links

		string GetGoogleSearchUrl()
		{
			var c = CurrentItem;
			if (c == null) return "";
			var url = "https://www.google.co.uk/?#q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}

		string GetNGemuSearchUrl()
		{
			var c = CurrentItem;
			if (c == null) return "";
			var url = "http://ngemu.com/search/5815705?q=";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false, " ");
			url += System.Web.HttpUtility.UrlEncode(keyName);
			return url;
		}

		string GetNGemuThreadUrl()
		{
			var c = CurrentItem;
			if (c == null) return "";
			var q = "x360ce " + c.FileProductName;
			var keyName = EngineHelper.GetKey(q, false);
			var url = "http://ngemu.com/threads/";
			url += System.Web.HttpUtility.UrlEncode(keyName) + "/";
			return url;
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

		#endregion

	}
}
