using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{
	public class SettingManager
	{
		public SettingManager()
		{
		}

		static SettingManager _current;
		public static SettingManager Current
		{
			get { return _current = _current ?? new SettingManager(); }
		}

		public string iniFile = "x360ce.ini";
		public string iniTmpFile = "x360ce.tmp";

		public int saveCount = 0;
		public int loadCount = 0;

		public event EventHandler<SettingEventArgs> ConfigSaved;
		public event EventHandler<SettingEventArgs> ConfigLoaded;

		public bool IsDebugMode { get { return ((CheckBox)SettingsMap[@"Options\" + SettingName.DebugMode]).Checked; } }

		Dictionary<string, Control> _settingsMap;
		/// <summary>
		/// Link control with INI key. Value/Text of controll will be automatically tracked and INI file updated.
		/// </summary>
		public Dictionary<string, Control> SettingsMap
		{
			get { return _settingsMap = _settingsMap ?? new Dictionary<string, Control>(); }
		}

		public void ReadSettings()
		{
			ReadSettings(iniFile);
		}

		public void ReadSetting(Control control, string key, string value)
		{
			if (control.Name == "GamePadTypeComboBox")
			{
				var cbx = (ComboBox)control;
				int n = 0;
				int.TryParse(value, out n);
				try { cbx.SelectedItem = (ControllerType)n; }
				catch (Exception) { if (IsDebugMode) throw; }
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
				var cbx = (ComboBox)control;
				if (key == SettingName.FakeMode) cbx.SelectedValue = value;
			}
			else if (control is TextBox)
			{
				// if setting is readonly.
				if (key == SettingName.ProductName) return;
				if (key == SettingName.ProductGuid) return;
				if (key == SettingName.InstanceGuid) return;
				if (key == SettingName.Pid) return;
				if (key == SettingName.Vid) return;
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
				if (key == SettingName.AxisToDPadDeadZone)
				{
					if (value == "") n = 256;
					// convert 256  to 100%
					n = System.Convert.ToInt32((float)n / 256F * 100F);
				}
				// convert 256  to 100%
				if (key == SettingName.AxisToDPadOffset) n = System.Convert.ToInt32((float)n / 256F * 100F);
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

		/// <summary>
		/// Read settings from INI file into windows form.
		/// </summary>
		/// <param name="file">INI file containing settings.</param>
		/// <param name="iniSection">Read setings from specified section only. Null - read from all sections.</param>
		public void ReadSettings(string file)
		{
			var ini2 = new Ini(file);
			foreach (string path in SettingsMap.Keys)
			{
				Control control = SettingsMap[path];
				string section = path.Split('\\')[0];
				string key = path.Split('\\')[1];
				string v = ini2.GetValue(section, key);
				ReadSetting(control, key, v);
			}
			loadCount++;
			if (ConfigLoaded != null) ConfigLoaded(this, new SettingEventArgs(ini2.File.Name, loadCount));
		}

		public void ReadPadSettings(string file, string iniSection, int padIndex)
		{
			var ini2 = new Ini(file);
			foreach (string path in SettingsMap.Keys)
			{
				Control control = SettingsMap[path];
				string section = path.Split('\\')[0];
				string key = path.Split('\\')[1];
				// Use only PAD1 section to get key names.
				if (section != "PAD1") continue;
				string dstPath = string.Format("PAD{0}\\{1}", padIndex + 1, key);
				control = SettingsMap[dstPath];
				string v = ini2.GetValue(iniSection, key);
				ReadSetting(control, key, v);
			}
			loadCount++;
			if (ConfigLoaded != null) ConfigLoaded(this, new SettingEventArgs(ini2.File.Name, loadCount));
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

		public bool SaveSettings()
		{
			var ini = new Ini(iniFile);
			var saved = false;
			foreach (string path in SettingsMap.Keys)
			{
				var r = SaveSetting(ini, path);
				if (r) saved = r;
			}
			return saved;
		}

		public bool SaveSetting(Control control)
		{
			var ini = new Ini(iniFile);
			var saved = false;
			foreach (string path in SettingsMap.Keys)
			{
				if (SettingsMap[path] == control)
				{
					var r = SaveSetting(ini, path);
					if (r) saved = r;
					break;
				}
			}
			return saved;
		}

		/// <summary>
		/// Save pad settings.
		/// </summary>
		/// <param name="file">INI file Name</param>
		/// <param name="iniSection">INI Section to save.</param>
		/// <param name="padIndex">Source PAD index.</param>
		public bool SavePadSettings(string file, string iniSection, int padIndex)
		{
			var ini = new Ini(file);
			var saved = false;
			foreach (string path in SettingsMap.Keys)
			{
				Control control = SettingsMap[path];
				string section = path.Split('\\')[0];
				string key = path.Split('\\')[1];
				// Use only PAD1 section to get key names.
				if (section != "PAD1") continue;
				string srcIniPath = string.Format("PAD{0}\\{1}", padIndex + 1, key);
				var r = SaveSetting(ini, srcIniPath, iniSection);
				if (r) saved = true;
			}
			return saved;
		}

		public bool SaveSetting(Ini ini, string path)
		{
			return SaveSetting(ini, path, null);
		}

		/// <summary>
		/// Save setting to current ini file.
		/// </summary>
		/// <param name="path">path of parameter (related to actual control)</param>
		/// <param name="dstIniSection">if not null then section will be different inside INI file than specified in path</param>
		public bool SaveSetting(Ini ini, string path, string dstIniSection)
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
				// make sure that disabled button value is "0".
				if (SettingName.IsButton(key) && string.IsNullOrEmpty(v)) v = "0";
			}
			else if (control is ComboBox)
			{
				var cbx = (ComboBox)control;
				if (key == SettingName.FakeMode) v = (string)cbx.SelectedValue;
			}
			else if (control is TextBox)
			{
				// if setting is readonly.
				if (key == SettingName.InstanceGuid || key == SettingName.ProductGuid)
				{
					v = string.IsNullOrEmpty(control.Text) ? Guid.Empty.ToString("D") : control.Text;
				}
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
				if (key == SettingName.AxisToDPadDeadZone || key == SettingName.AxisToDPadOffset)
				{
					// convert 100%  to 256
					v = System.Convert.ToInt32((float)tc.Value / 100F * 256F).ToString();
				}
				else v = tc.Value.ToString();
			}
			else if (control is CheckBox)
			{
				CheckBox tc = (CheckBox)control;
				v = tc.Checked ? "1" : "0";
			}
			if (SettingName.IsThumbAxis(key))
			{
				v = v.Replace(SettingName.SType.Axis, "");
			}
			if (SettingName.IsDPad(key)) v = v.Replace(SettingName.SType.DPad, "");
			if (v == "v1") v = "UP";
			if (v == "v2") v = "RIGHT";
			if (v == "v3") v = "DOWN";
			if (v == "v4") v = "LEFT";
			if (v == "")
			{
				if (key == SettingName.DPadUp) v = "UP";
				if (key == SettingName.DPadDown) v = "DOWN";
				if (key == SettingName.DPadLeft) v = "LEFT";
				if (key == SettingName.DPadRight) v = "RIGHT";
				if (key == SettingName.Vid || key == SettingName.Pid) v = "0x0";
			}
			string iniSection = string.IsNullOrEmpty(dstIniSection) ? section : dstIniSection;
			// add comment.
			//var l = SettingName.MaxNameLength - key.Length + 24;
			//v = string.Format("{0, -" + l + "} # {1} Default: '{2}'.", v, SettingName.GetDescription(key), SettingName.GetDefaultValue(key));
			var oldValue = ini.GetValue(iniSection, key);
			var saved = false;
			if (oldValue != v)
			{
				ini.SetValue(iniSection, key, v);
				saveCount++;
				saved = true;
				if (ConfigSaved != null) ConfigSaved(this, new SettingEventArgs(iniFile, saveCount));

			}
			return saved;
		}

	}
}
