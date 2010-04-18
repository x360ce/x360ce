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
        /// <summary>
        /// Link control with INI key. Value/Text of controll will be automatically tracked and INI file updated.
        /// </summary>
        Dictionary<string, Control> SettingsMap
        {
            get
            {
                if (_SettingsMap == null) _SettingsMap = new Dictionary<string, Control>();
                if (_SettingsMap.Count == 0)
                {
                    _SettingsMap.Add(@"Options\" + SettingName.UseInitBeep, UseInitBeepCheckBox);
                    _SettingsMap.Add(@"Options\" + SettingName.DebugMode, DebugModeCheckBox);
                    _SettingsMap.Add(@"Options\" + SettingName.Log, EnableLoggingCheckBox);
                    _SettingsMap.Add(@"FakeAPI\" + SettingName.FakeWinTrust, FakeWinTrustCheckBox);
                    _SettingsMap.Add(@"FakeAPI\" + SettingName.FakeWmi, FakeWmiComboBox);
                    _SettingsMap.Add(@"FakeAPI\" + SettingName.FakeDi, FakeDiComboBox);
                    _SettingsMap.Add(@"FakeAPI\" + SettingName.FakeVid, FakeVidTextBox);
                    _SettingsMap.Add(@"FakeAPI\" + SettingName.FakePid, FakePidTextBox);
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

        void ReadSettings()
        {
            ReadSettings(iniFile);
        }

        void ReadSetting(Control control, string key, string value)
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
                if (key == SettingName.FakeWmi || key == SettingName.FakeDi) cbx.SelectedValue = value;
            }
            else if (control is TextBox)
            {
                // if setting is readonly.
                if (key == SettingName.ProductName) return;
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
        void ReadSettings(string file)
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
            toolStripStatusLabel1.Text = string.Format("'{0}' loaded.", ini2.File.Name);
        }

        void ReadPadSettings(string file, string iniSection, int padIndex)
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
            toolStripStatusLabel1.Text = string.Format("'{0}' loaded.", ini2.File.Name);
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
            var ini = new Ini(iniFile);
            foreach (string path in SettingsMap.Keys) SaveSetting(ini, path);
        }

        public void SaveSetting(Control control)
        {
            var ini = new Ini(iniFile);
            foreach (string path in SettingsMap.Keys)
            {
                if (SettingsMap[path] == control)
                {
                    SaveSetting(ini, path);
                    break;
                }
            }
        }

        int saveCount = 0;

        /// <summary>
        /// Save pad settings.
        /// </summary>
        /// <param name="file">INI file Name</param>
        /// <param name="iniSection">INI Section to save.</param>
        /// <param name="padIndex">Source PAD index.</param>
        void SavePadSettings(string file, string iniSection, int padIndex)
        {
            var ini = new Ini(file);
            foreach (string path in SettingsMap.Keys)
            {
                Control control = SettingsMap[path];
                string section = path.Split('\\')[0];
                string key = path.Split('\\')[1];
                // Use only PAD1 section to get key names.
                if (section != "PAD1") continue;
                string srcIniPath = string.Format("PAD{0}\\{1}", padIndex + 1, key);
                SaveSetting(ini, srcIniPath, iniSection);
            }
        }

        void SaveSetting(Ini ini, string path)
        {
            SaveSetting(ini, path, null);
        }

        /// <summary>
        /// Save setting to current ini file.
        /// </summary>
        /// <param name="path">path of parameter (related to actual control)</param>
        /// <param name="dstIniSection">if not null then section will be different inside INI file than specified in path</param>
        void SaveSetting(Ini ini, string path, string dstIniSection)
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
                var cbx = (ComboBox)control;
                if (key == SettingName.FakeWmi || key == SettingName.FakeDi) v = (string)cbx.SelectedValue;
            }
            else if (control is TextBox)
            {
                // if setting is readonly.
                if (key == SettingName.InstanceGuid)
                {
                    v = string.IsNullOrEmpty(control.Text) ? Guid.Empty.ToString("B") : string.Format("{{{0}}}", control.Text);
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
                if (key == SettingName.DPadUp) v = "UP";
                if (key == SettingName.DPadDown) v = "DOWN";
                if (key == SettingName.DPadLeft) v = "LEFT";
                if (key == SettingName.DPadRight) v = "RIGHT";
                if (key == SettingName.Vid || key == SettingName.Pid || key == SettingName.FakeVid || key == SettingName.FakePid) v = "0x0";
            }
            string iniSection = string.IsNullOrEmpty(dstIniSection) ? section : dstIniSection;
            ini.SetValue(iniSection, key, v);
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

        /// <summary>
        /// Delay settings trough timer so interface will be more responsive on TrackBars.
        /// Or fast changes. Library will be reloaded as soon as user calms down (no setting changes in 500ms).
        /// </summary>
        public void NotifySettingsChange()
        {
            SettingsTimer.Stop();
            SettingsTimer.Start();
        }

        private void Control_TextChanged(object sender, EventArgs e)
        {
            SaveSetting((Control)sender);
            NotifySettingsChange();
        }

        private void Control_ValueChanged(object sender, EventArgs e)
        {
            SaveSetting((Control)sender);
            NotifySettingsChange();
        }

        private void Control_CheckedChanged(object sender, EventArgs e)
        {
            SaveSetting((Control)sender);
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
