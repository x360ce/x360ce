using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
    public partial class OptionsControl : UserControl
    {
        public OptionsControl()
        {
            InitializeComponent();
            if (DesignMode) return;
        }

        public void InitOptions()
        {
            DebugModeCheckBox_CheckedChanged(DebugModeCheckBox, null);
            // Fill FakeWmi ComboBox.
            var fakeModeOptions = new List<KeyValuePair>();
            var fakeModeTypes = (HookMode[])Enum.GetValues(typeof(HookMode));
            foreach (var item in fakeModeTypes) fakeModeOptions.Add(new KeyValuePair(item.ToString(), ((int)item).ToString()));
            FakeModeComboBox.DataSource = fakeModeOptions;
            FakeModeComboBox.DisplayMember = "Key";
            FakeModeComboBox.ValueMember = "Value";
        }

        void DebugModeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var cbx = (CheckBox)sender;
            if (!cbx.Checked) Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
            else Application.ThreadException -= new System.Threading.ThreadExceptionEventHandler(Program.Application_ThreadException);
        }

        void XInputEnableCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            MainForm.Current.XInputEnable(XInputEnableCheckBox.Checked);
        }

        /// <summary>
        /// Link control with INI key. Value/Text of controll will be automatically tracked and INI file updated.
        /// </summary>
        public void InitSettingsManager()
        {
            // INI setting keys with controls.
            var sm = SettingManager.Current.SettingsMap;
            string section = @"Options\";
            sm.Add(section + SettingName.UseInitBeep, UseInitBeepCheckBox);
            sm.Add(section + SettingName.DebugMode, DebugModeCheckBox);
            sm.Add(section + SettingName.Log, EnableLoggingCheckBox);
            sm.Add(section + SettingName.Console, ConsoleCheckBox);
            sm.Add(section + SettingName.InternetDatabaseUrl, InternetDatabaseUrlTextBox);
            sm.Add(section + SettingName.InternetFeatures, InternetCheckBox);
            sm.Add(section + SettingName.InternetAutoload, InternetAutoloadCheckBox);
            sm.Add(section + SettingName.AllowOnlyOneCopy, AllowOnlyOneCopyCheckBox);
            sm.Add(section + SettingName.ProgramScanLocations, ProgramScanLocationsListBox);
            section = @"InputHook\";
            sm.Add(section + SettingName.HookMode, FakeModeComboBox);
        }

        void InternetCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            InternetAutoloadCheckBox.Enabled = InternetCheckBox.Checked;
        }

        private void AddLocationButton_Click(object sender, EventArgs e)
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        }

        private void RemoveLocationButton_Click(object sender, EventArgs e)
        {

        }

    }
}
