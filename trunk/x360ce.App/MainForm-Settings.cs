using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App.XnaInput;

namespace x360ce.App
{
	public partial class MainForm
	{
		Dictionary<string, Control> _SettingsMap;
		Dictionary<string, Control> SettingsMap
		{
			get
			{
				// Adopted names:
				//
				// Internal Microsoft Name - "Public Name"
				// Big = "Guide"
				// Thumb = "Stick"
				// Shoulder = "Bumper"
				//
				// ButtonA
				// ButtonB
				// ButtonBack
				// ButtonBig
				// ButtonStart
				// ButtonX
				// ButtonY
				// DPad
				// DPadDown
				// DPadLeft
				// DPadRight
				// DPadUp
				// LeftShoulder
				// LeftThumb
				// LeftThumbAxisX
				// LeftThumbAxisXmax
				// LeftThumbAxisXmin
				// LeftThumbAxisY
				// LeftThumbAxisYmax
				// LeftThumbAxisYmin
				// LeftThumbDown
				// LeftThumbLeft
				// LeftThumbRight
				// LeftThumbUp
				// LeftTrigger
				// RightShoulder
				// RightStick
				// RightStickAxisX
				// RightStickAxisXmax
				// RightStickAxisXmin
				// RightStickAxisY
				// RightStickAxisYmax
				// RightStickAxisYmin
				// RightStickDown
				// RightStickLeft
				// RightStickRight
				// RightStickUp
				// RightTrigger

				if (_SettingsMap == null) _SettingsMap = new Dictionary<string, Control>();
				if (_SettingsMap.Count == 0)
				{
					_SettingsMap.Add(@"Options\UseInitBeep", UseInitBeepCheckBox);
					_SettingsMap.Add(@"Options\Log", EnableLoggingCheckBox);
					_SettingsMap.Add(@"FakeAPI\FakeWMI", FakeApiCheckBox);
					_SettingsMap.Add(@"FakeAPI\FakeDI", FakeDiCheckBox);
					_SettingsMap.Add(@"FakeAPI\FakeVID", FakeVidTextBox);
					_SettingsMap.Add(@"FakeAPI\FakePID", FakePidTextBox);
					// Add PAD settings.
					for (int i = 0; i < ControlPads.Length; i++)
					{
						var map = ControlPads[i].SettingsMap;
						foreach (var key in map.Keys) _SettingsMap.Add(key, map[key]);
					}
				}
				return _SettingsMap;
			}
		}

		Ini ini;

		void ReadSettings()
		{
			ReadSettings(iniFile, -1);
		}

		void ReadSetting(Control control, string key, string value)
		{
			if (control.Name == "GamePadTypeComboBox")
			{
				var cbx = (ComboBox)control;
				int n = 0;
				int.TryParse(value, out n);
				try { cbx.SelectedItem = (ControllerType)n; }
				catch (Exception) { }
			}
			// If Di menu strip attached.
			else if (control is ComboBox && control.ContextMenuStrip != null)
			{
				var cbx = (ComboBox)control;
				var text = new SettingsConverter(value, key).ToFrmSetting();
				SetComboBoxValue(cbx, text);
			}
			else if (control is ComboBox)
			{

			}
			else if (control is TextBox)
			{
				// if setting is readonly.
				if (key == "ProductName") return;
                if (key == "Instance") return;
                if (key == "PID") return;
				if (key == "VID") return;
				control.Text = value;
			}
			else if (control is NumericUpDown)
			{
				NumericUpDown nud = (NumericUpDown)control;
				decimal n = 0;
				decimal.TryParse(value, out n);
				if (n < nud.Minimum) n = nud.Minimum;
				if (n > nud.Maximum) n = nud.Maximum;
				nud.Value = n;
			}
			else if (control is TrackBar)
			{
				TrackBar tc = (TrackBar)control;
				int n = 0;
				int.TryParse(value, out n);
				if (n < tc.Minimum) n = tc.Minimum;
				if (n > tc.Maximum) n = tc.Maximum;
				tc.Value = n;
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				int n = 0;
				int.TryParse(value, out n);
				tc.Checked = n != 0;
			}
		}

		void ReadSettings(string file, int padIndex)
		{
			ini = new Ini(file);
			// -1 - All settings
			// 0 - 3 - read only pad settings from section [PAD1].
			foreach (string path in SettingsMap.Keys)
			{
				Control control = SettingsMap[path];
				string section = path.Split('\\')[0];
				string key = path.Split('\\')[1];
				if (padIndex > -1)
				{
					if (section != "PAD1") continue;
					// override target control.
					string targetPath = string.Format("PAD{0}\\{1}", controllerIndex + 1, key);
					control = SettingsMap[targetPath];
				}
				string v = ini.GetValue(section, key);
				ReadSetting(control, key, v);
			}
			toolStripStatusLabel1.Text = string.Format("'{0}' loaded.", ini.File.Name);
		}

		public void SetComboBoxValue(ComboBox cbx, string text)
		{
			// Remove value from other box.
			foreach (Control control in SettingsMap.Values)
			{
				if (
					// Control is combobox.
					control is ComboBox
					// controls belong to same parent.
					&& cbx.Parent == control.Parent
					// This is not same control.
					&& control != cbx
					// Text value is same.
					&& control.Text == text
					// Text value is not empty.
					&& !string.IsNullOrEmpty(text))
				{
					((ComboBox)control).Items.Clear();
					//SaveSettings(control);
				}
			}
			cbx.Items.Clear();
			cbx.Items.Add(text);
			cbx.SelectedIndex = 0;
		}


		void SaveSettings()
		{
			ini = new Ini(iniFile);
			foreach (string key in SettingsMap.Keys) SaveSettings(key);
		}

		public void SaveSettings(Control control)
		{
			foreach (string path in SettingsMap.Keys)
			{
				if (SettingsMap[path] == control)
				{
					SaveSettings(path);
					break;
				}
			}
		}

		int saveCount = 0;

		void SaveSettings(string path)
		{
			var control = SettingsMap[path];
			string section = path.Split('\\')[0];
			string key = path.Split('\\')[1];
			string v = string.Empty;
			if (control.Name == "GamePadTypeComboBox")
			{
				v = ((int)(ControllerType)((ComboBox)control).SelectedItem).ToString();
			}
			// If di menu strip attached.
			else if (control is ComboBox && control.ContextMenuStrip != null)
			{
				v = new SettingsConverter(control.Text, key).ToIniSetting();
			}
			else if (control is ComboBox)
			{

			}
			else if (control is TextBox)
			{
				// if setting is readonly.
                if (key == "Instance") v = string.Format("{{{0}}}", control.Text);
				else v = control.Text;
			}
			else if (control is NumericUpDown)
			{
				NumericUpDown nud = (NumericUpDown)control;
				v = nud.Value.ToString();
			}
			else if (control is TrackBar)
			{
				TrackBar tc = (TrackBar)control;
				v = tc.Value.ToString();
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				v = tc.Checked ? "1" : "0";
			}
			if (key.Contains("Analog") && !key.Contains("Button"))
			{
				v = v.Replace("a", "");
			}
			if (key.Contains("D-pad")) v = v.Replace("p", "");
			if (v == "v1") v = "UP";
			if (v == "v2") v = "RIGHT";
			if (v == "v3") v = "DOWN";
			if (v == "v4") v = "LEFT";
			if (v == "")
			{
				if (key == "D-pad Up") v = "UP";
				if (key == "D-pad Down") v = "DOWN";
				if (key == "D-pad Left") v = "LEFT";
				if (key == "D-pad Right") v = "RIGHT";
				if (key == "VID" || key == "PID" || key == "FakeVID" || key == "FakePID") v = "0x0";
			}
			ini.SetValue(section, key, v);
			saveCount++;
			StatusSaveLabel.Text = string.Format("S {0}", saveCount);
		}

		#region Setting Events

		int resumed = 0;
		int suspended = 0;

		public void SuspendEvents()
		{
			StatusEventsLabel.Text = "OFF...";
			// Don't allow controls to fire events.
			foreach (var control in SettingsMap.Values)
			{
                if (control is TrackBar) ((TrackBar)control).ValueChanged -= new EventHandler(Control_ValueChanged);
				if (control is CheckBox) ((CheckBox)control).CheckedChanged -= new EventHandler(Control_CheckedChanged);
				if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged -= new EventHandler(this.Control_TextChanged);
				if (control is ComboBox || control is TextBox) control.TextChanged -= new System.EventHandler(this.Control_TextChanged);
			}
			suspended++;
			StatusEventsLabel.Text = string.Format("OFF {0} {1}", suspended, resumed);
		}

		public void ResumeEvents()
		{
			StatusEventsLabel.Text = "ON...";
			// Allow controls to fire events.
			foreach (var control in SettingsMap.Values)
			{
				if (control is TrackBar) ((TrackBar)control).ValueChanged += new EventHandler(Control_ValueChanged);
				if (control is CheckBox) ((CheckBox)control).CheckedChanged += new EventHandler(Control_CheckedChanged);
				if (control is ComboBox) ((ComboBox)control).SelectedIndexChanged += new EventHandler(this.Control_TextChanged);
				if (control is ComboBox || control is TextBox) control.TextChanged += new System.EventHandler(this.Control_TextChanged);
			}
			resumed++;
			StatusEventsLabel.Text = string.Format("ON {0} {1}", suspended, resumed);
		}

		private void Control_TextChanged(object sender, EventArgs e)
		{
			SaveSettings((Control)sender);
			NotifySettingsChange();
		}

		private void Control_ValueChanged(object sender, EventArgs e)
		{
			SaveSettings((Control)sender);
			NotifySettingsChange();
		}

		private void Control_CheckedChanged(object sender, EventArgs e)
		{
			SaveSettings((Control)sender);
			NotifySettingsChange();
		}

		#endregion

		void ReloadXinputSettings()
		{
			SuspendEvents();
			ReadSettings();
			ResumeEvents();
		}

		/// <summary>
		/// Can be reloaded on two ocations:
		/// a) Direct Input Device was disconnected, connected or order changed.
		/// b) Settings file (*.ini) was updated.
		/// </summary>
		void ReloadXInputLibrary()
		{
			if (timer.Enabled) timer.Stop();
			XInput.ReLoadLibrary(dllFile);
			timer.Start();
		}

	}
}
