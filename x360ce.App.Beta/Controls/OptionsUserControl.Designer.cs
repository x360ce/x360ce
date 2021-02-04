namespace x360ce.App.Controls
{
    partial class OptionsUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.TestingAndLoggingGroupBox = new System.Windows.Forms.GroupBox();
			this.ShowSettingsTabCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowDevicesTabCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowProgramsTabCheckBox = new System.Windows.Forms.CheckBox();
			this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
			this.ConsoleCheckBox = new System.Windows.Forms.CheckBox();
			this.DebugModeCheckBox = new System.Windows.Forms.CheckBox();
			this.EnableLoggingCheckBox = new System.Windows.Forms.CheckBox();
			this.OperationGroupBox = new System.Windows.Forms.GroupBox();
			this.StartWithWindowsStateComboBox = new System.Windows.Forms.ComboBox();
			this.IsProcessDPIAwareCheckBox = new System.Windows.Forms.CheckBox();
			this.StartWithWindowsCheckBox = new System.Windows.Forms.CheckBox();
			this.AlwaysOnTopCheckBox = new System.Windows.Forms.CheckBox();
			this.MinimizeToTrayCheckBox = new System.Windows.Forms.CheckBox();
			this.AllowOnlyOneCopyCheckBox = new System.Windows.Forms.CheckBox();
			this.ProgramScanLocationsTabControl = new System.Windows.Forms.TabControl();
			this.GameScanLocationsTabPage = new System.Windows.Forms.TabPage();
			this.GameScanLocationsListBox = new System.Windows.Forms.ListBox();
			this.LocationsToolStrip = new System.Windows.Forms.ToolStrip();
			this.RefreshLocationsButton = new System.Windows.Forms.ToolStripButton();
			this.RemoveLocationButton = new System.Windows.Forms.ToolStripButton();
			this.AddLocationButton = new System.Windows.Forms.ToolStripButton();
			this.LocationFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.ConfigurationGroupBox = new System.Windows.Forms.GroupBox();
			this.AutoDetectForegroundWindowCheckBox = new System.Windows.Forms.CheckBox();
			this.IncludeProductsCheckBox = new System.Windows.Forms.CheckBox();
			this.ConfigurationVersionLabel = new System.Windows.Forms.Label();
			this.ConfigurationVersionTextBox = new System.Windows.Forms.TextBox();
			this.DirectInputDevicesGroupBox = new System.Windows.Forms.GroupBox();
			this.UseDeviceBufferedDataCheckBox = new System.Windows.Forms.CheckBox();
			this.ExcludeVirtualDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.ExcludeSupplementalDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.DeveloperToolsButton = new System.Windows.Forms.Button();
			this.AllowRemoteControllersGroupBox = new System.Windows.Forms.GroupBox();
			this.RemoteEnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.AllowRemote4CheckBox = new System.Windows.Forms.CheckBox();
			this.RemotePortLabel = new System.Windows.Forms.Label();
			this.RemotePasswordLabel = new System.Windows.Forms.Label();
			this.AllowControlLabel = new System.Windows.Forms.Label();
			this.AllowRemote3CheckBox = new System.Windows.Forms.CheckBox();
			this.RemotePortNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.RemotePasswordTextBox = new System.Windows.Forms.TextBox();
			this.AllowRemote2CheckBox = new System.Windows.Forms.CheckBox();
			this.AllowRemote1CheckBox = new System.Windows.Forms.CheckBox();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.GeneralTabPage = new System.Windows.Forms.TabPage();
			this.GeneralPanel = new System.Windows.Forms.Panel();
			this.DevelopingGroupBox = new System.Windows.Forms.GroupBox();
			this.ShowTestButtonCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowFormInfoCheckBox = new System.Windows.Forms.CheckBox();
			this.GuideButtonGroupBox = new System.Windows.Forms.GroupBox();
			this.GuideButtonActionLabel = new System.Windows.Forms.Label();
			this.GuideButtonActionTextBox = new System.Windows.Forms.TextBox();
			this.InternetOptionsTabPage = new System.Windows.Forms.TabPage();
			this.RemoteControllerTabPage = new System.Windows.Forms.TabPage();
			this.VirtualDevicePanel = new System.Windows.Forms.Panel();
			this.VirtualDeviceTabPage = new System.Windows.Forms.TabPage();
			this.OptionsVirtualDeviceHost = new System.Windows.Forms.Integration.ElementHost();
			this.HidGuardianTabPage = new System.Windows.Forms.TabPage();
			this.OptionsHidGuardianHost = new System.Windows.Forms.Integration.ElementHost();
			this.InternetHost = new System.Windows.Forms.Integration.ElementHost();
			this.InternetPanel = new x360ce.App.Controls.OptionsInternetControl();
			this.TestingAndLoggingGroupBox.SuspendLayout();
			this.OperationGroupBox.SuspendLayout();
			this.ProgramScanLocationsTabControl.SuspendLayout();
			this.GameScanLocationsTabPage.SuspendLayout();
			this.LocationsToolStrip.SuspendLayout();
			this.ConfigurationGroupBox.SuspendLayout();
			this.DirectInputDevicesGroupBox.SuspendLayout();
			this.AllowRemoteControllersGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RemotePortNumericUpDown)).BeginInit();
			this.MainTabControl.SuspendLayout();
			this.GeneralTabPage.SuspendLayout();
			this.GeneralPanel.SuspendLayout();
			this.DevelopingGroupBox.SuspendLayout();
			this.GuideButtonGroupBox.SuspendLayout();
			this.InternetOptionsTabPage.SuspendLayout();
			this.RemoteControllerTabPage.SuspendLayout();
			this.VirtualDevicePanel.SuspendLayout();
			this.VirtualDeviceTabPage.SuspendLayout();
			this.HidGuardianTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// TestingAndLoggingGroupBox
			// 
			this.TestingAndLoggingGroupBox.Controls.Add(this.ShowSettingsTabCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.ShowDevicesTabCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.ShowProgramsTabCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.XInputEnableCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.ConsoleCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.DebugModeCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.EnableLoggingCheckBox);
			this.TestingAndLoggingGroupBox.Location = new System.Drawing.Point(0, 260);
			this.TestingAndLoggingGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.TestingAndLoggingGroupBox.Name = "TestingAndLoggingGroupBox";
			this.TestingAndLoggingGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.TestingAndLoggingGroupBox.Size = new System.Drawing.Size(508, 225);
			this.TestingAndLoggingGroupBox.TabIndex = 30;
			this.TestingAndLoggingGroupBox.TabStop = false;
			this.TestingAndLoggingGroupBox.Text = "Testing and Logging";
			// 
			// ShowSettingsTabCheckBox
			// 
			this.ShowSettingsTabCheckBox.AutoSize = true;
			this.ShowSettingsTabCheckBox.Location = new System.Drawing.Point(240, 137);
			this.ShowSettingsTabCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ShowSettingsTabCheckBox.Name = "ShowSettingsTabCheckBox";
			this.ShowSettingsTabCheckBox.Size = new System.Drawing.Size(236, 29);
			this.ShowSettingsTabCheckBox.TabIndex = 1;
			this.ShowSettingsTabCheckBox.Text = "Show [Settings] Tab";
			this.ShowSettingsTabCheckBox.UseVisualStyleBackColor = true;
			this.ShowSettingsTabCheckBox.CheckedChanged += new System.EventHandler(this.ShowSettingsTabCheckBox_CheckedChanged);
			// 
			// ShowDevicesTabCheckBox
			// 
			this.ShowDevicesTabCheckBox.AutoSize = true;
			this.ShowDevicesTabCheckBox.Location = new System.Drawing.Point(240, 92);
			this.ShowDevicesTabCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ShowDevicesTabCheckBox.Name = "ShowDevicesTabCheckBox";
			this.ShowDevicesTabCheckBox.Size = new System.Drawing.Size(235, 29);
			this.ShowDevicesTabCheckBox.TabIndex = 1;
			this.ShowDevicesTabCheckBox.Text = "Show [Devices] Tab";
			this.ShowDevicesTabCheckBox.UseVisualStyleBackColor = true;
			this.ShowDevicesTabCheckBox.CheckedChanged += new System.EventHandler(this.ShowDevicesTabCheckBox_CheckedChanged);
			// 
			// ShowProgramsTabCheckBox
			// 
			this.ShowProgramsTabCheckBox.AutoSize = true;
			this.ShowProgramsTabCheckBox.Location = new System.Drawing.Point(240, 48);
			this.ShowProgramsTabCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ShowProgramsTabCheckBox.Name = "ShowProgramsTabCheckBox";
			this.ShowProgramsTabCheckBox.Size = new System.Drawing.Size(250, 29);
			this.ShowProgramsTabCheckBox.TabIndex = 1;
			this.ShowProgramsTabCheckBox.Text = "Show [Programs] Tab";
			this.ShowProgramsTabCheckBox.UseVisualStyleBackColor = true;
			this.ShowProgramsTabCheckBox.CheckedChanged += new System.EventHandler(this.ShowProgramsTabCheckBox_CheckedChanged);
			// 
			// XInputEnableCheckBox
			// 
			this.XInputEnableCheckBox.AutoSize = true;
			this.XInputEnableCheckBox.Checked = true;
			this.XInputEnableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.XInputEnableCheckBox.Location = new System.Drawing.Point(12, 48);
			this.XInputEnableCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.XInputEnableCheckBox.Name = "XInputEnableCheckBox";
			this.XInputEnableCheckBox.Size = new System.Drawing.Size(178, 29);
			this.XInputEnableCheckBox.TabIndex = 0;
			this.XInputEnableCheckBox.Text = "Enable XInput";
			this.XInputEnableCheckBox.UseVisualStyleBackColor = true;
			// 
			// ConsoleCheckBox
			// 
			this.ConsoleCheckBox.AutoSize = true;
			this.ConsoleCheckBox.Location = new System.Drawing.Point(12, 137);
			this.ConsoleCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ConsoleCheckBox.Name = "ConsoleCheckBox";
			this.ConsoleCheckBox.Size = new System.Drawing.Size(196, 29);
			this.ConsoleCheckBox.TabIndex = 0;
			this.ConsoleCheckBox.Text = "Enable Console";
			this.ConsoleCheckBox.UseVisualStyleBackColor = true;
			// 
			// DebugModeCheckBox
			// 
			this.DebugModeCheckBox.AutoSize = true;
			this.DebugModeCheckBox.Checked = true;
			this.DebugModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DebugModeCheckBox.Location = new System.Drawing.Point(12, 181);
			this.DebugModeCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.DebugModeCheckBox.Name = "DebugModeCheckBox";
			this.DebugModeCheckBox.Size = new System.Drawing.Size(167, 29);
			this.DebugModeCheckBox.TabIndex = 0;
			this.DebugModeCheckBox.Text = "Debug Mode";
			this.DebugModeCheckBox.UseVisualStyleBackColor = true;
			// 
			// EnableLoggingCheckBox
			// 
			this.EnableLoggingCheckBox.AutoSize = true;
			this.EnableLoggingCheckBox.Location = new System.Drawing.Point(12, 92);
			this.EnableLoggingCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.EnableLoggingCheckBox.Name = "EnableLoggingCheckBox";
			this.EnableLoggingCheckBox.Size = new System.Drawing.Size(194, 29);
			this.EnableLoggingCheckBox.TabIndex = 0;
			this.EnableLoggingCheckBox.Text = "Enable Logging";
			this.EnableLoggingCheckBox.UseVisualStyleBackColor = true;
			// 
			// OperationGroupBox
			// 
			this.OperationGroupBox.Controls.Add(this.StartWithWindowsStateComboBox);
			this.OperationGroupBox.Controls.Add(this.IsProcessDPIAwareCheckBox);
			this.OperationGroupBox.Controls.Add(this.StartWithWindowsCheckBox);
			this.OperationGroupBox.Controls.Add(this.AlwaysOnTopCheckBox);
			this.OperationGroupBox.Controls.Add(this.MinimizeToTrayCheckBox);
			this.OperationGroupBox.Controls.Add(this.AllowOnlyOneCopyCheckBox);
			this.OperationGroupBox.Location = new System.Drawing.Point(6, 6);
			this.OperationGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.OperationGroupBox.Name = "OperationGroupBox";
			this.OperationGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.OperationGroupBox.Size = new System.Drawing.Size(508, 242);
			this.OperationGroupBox.TabIndex = 31;
			this.OperationGroupBox.TabStop = false;
			this.OperationGroupBox.Text = "Operation";
			// 
			// StartWithWindowsStateComboBox
			// 
			this.StartWithWindowsStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.StartWithWindowsStateComboBox.FormattingEnabled = true;
			this.StartWithWindowsStateComboBox.Items.AddRange(new object[] {
            "Maximized",
            "Normal",
            "Minimized"});
			this.StartWithWindowsStateComboBox.Location = new System.Drawing.Point(248, 144);
			this.StartWithWindowsStateComboBox.Margin = new System.Windows.Forms.Padding(6);
			this.StartWithWindowsStateComboBox.Name = "StartWithWindowsStateComboBox";
			this.StartWithWindowsStateComboBox.Size = new System.Drawing.Size(176, 33);
			this.StartWithWindowsStateComboBox.TabIndex = 95;
			// 
			// IsProcessDPIAwareCheckBox
			// 
			this.IsProcessDPIAwareCheckBox.AutoSize = true;
			this.IsProcessDPIAwareCheckBox.Location = new System.Drawing.Point(12, 192);
			this.IsProcessDPIAwareCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.IsProcessDPIAwareCheckBox.Name = "IsProcessDPIAwareCheckBox";
			this.IsProcessDPIAwareCheckBox.Size = new System.Drawing.Size(399, 29);
			this.IsProcessDPIAwareCheckBox.TabIndex = 94;
			this.IsProcessDPIAwareCheckBox.Text = "Make Application Process DPI Aware";
			// 
			// StartWithWindowsCheckBox
			// 
			this.StartWithWindowsCheckBox.AutoSize = true;
			this.StartWithWindowsCheckBox.Location = new System.Drawing.Point(12, 146);
			this.StartWithWindowsCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.StartWithWindowsCheckBox.Name = "StartWithWindowsCheckBox";
			this.StartWithWindowsCheckBox.Size = new System.Drawing.Size(232, 29);
			this.StartWithWindowsCheckBox.TabIndex = 94;
			this.StartWithWindowsCheckBox.Text = "Start with Windows:";
			// 
			// AlwaysOnTopCheckBox
			// 
			this.AlwaysOnTopCheckBox.AutoSize = true;
			this.AlwaysOnTopCheckBox.Location = new System.Drawing.Point(240, 100);
			this.AlwaysOnTopCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AlwaysOnTopCheckBox.Name = "AlwaysOnTopCheckBox";
			this.AlwaysOnTopCheckBox.Size = new System.Drawing.Size(185, 29);
			this.AlwaysOnTopCheckBox.TabIndex = 93;
			this.AlwaysOnTopCheckBox.Text = "Always on Top";
			// 
			// MinimizeToTrayCheckBox
			// 
			this.MinimizeToTrayCheckBox.AutoSize = true;
			this.MinimizeToTrayCheckBox.Location = new System.Drawing.Point(12, 100);
			this.MinimizeToTrayCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.MinimizeToTrayCheckBox.Name = "MinimizeToTrayCheckBox";
			this.MinimizeToTrayCheckBox.Size = new System.Drawing.Size(202, 29);
			this.MinimizeToTrayCheckBox.TabIndex = 93;
			this.MinimizeToTrayCheckBox.Text = "Minimize to Tray";
			// 
			// AllowOnlyOneCopyCheckBox
			// 
			this.AllowOnlyOneCopyCheckBox.AutoSize = true;
			this.AllowOnlyOneCopyCheckBox.Location = new System.Drawing.Point(12, 56);
			this.AllowOnlyOneCopyCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowOnlyOneCopyCheckBox.Name = "AllowOnlyOneCopyCheckBox";
			this.AllowOnlyOneCopyCheckBox.Size = new System.Drawing.Size(459, 29);
			this.AllowOnlyOneCopyCheckBox.TabIndex = 1;
			this.AllowOnlyOneCopyCheckBox.Text = "Allow only one copy of Application at a time";
			this.AllowOnlyOneCopyCheckBox.UseVisualStyleBackColor = true;
			// 
			// ProgramScanLocationsTabControl
			// 
			this.ProgramScanLocationsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProgramScanLocationsTabControl.Controls.Add(this.GameScanLocationsTabPage);
			this.ProgramScanLocationsTabControl.Location = new System.Drawing.Point(526, 17);
			this.ProgramScanLocationsTabControl.Margin = new System.Windows.Forms.Padding(6);
			this.ProgramScanLocationsTabControl.Name = "ProgramScanLocationsTabControl";
			this.ProgramScanLocationsTabControl.SelectedIndex = 0;
			this.ProgramScanLocationsTabControl.Size = new System.Drawing.Size(756, 429);
			this.ProgramScanLocationsTabControl.TabIndex = 34;
			// 
			// GameScanLocationsTabPage
			// 
			this.GameScanLocationsTabPage.Controls.Add(this.GameScanLocationsListBox);
			this.GameScanLocationsTabPage.Controls.Add(this.LocationsToolStrip);
			this.GameScanLocationsTabPage.Location = new System.Drawing.Point(8, 39);
			this.GameScanLocationsTabPage.Margin = new System.Windows.Forms.Padding(6);
			this.GameScanLocationsTabPage.Name = "GameScanLocationsTabPage";
			this.GameScanLocationsTabPage.Size = new System.Drawing.Size(740, 382);
			this.GameScanLocationsTabPage.TabIndex = 0;
			this.GameScanLocationsTabPage.Text = "Game Scan Locations";
			this.GameScanLocationsTabPage.UseVisualStyleBackColor = true;
			// 
			// GameScanLocationsListBox
			// 
			this.GameScanLocationsListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GameScanLocationsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GameScanLocationsListBox.FormattingEnabled = true;
			this.GameScanLocationsListBox.ItemHeight = 25;
			this.GameScanLocationsListBox.Location = new System.Drawing.Point(0, 42);
			this.GameScanLocationsListBox.Margin = new System.Windows.Forms.Padding(6);
			this.GameScanLocationsListBox.Name = "GameScanLocationsListBox";
			this.GameScanLocationsListBox.Size = new System.Drawing.Size(740, 340);
			this.GameScanLocationsListBox.Sorted = true;
			this.GameScanLocationsListBox.TabIndex = 0;
			this.GameScanLocationsListBox.SelectedIndexChanged += new System.EventHandler(this.ProgramScanLocationsListBox_SelectedIndexChanged);
			// 
			// LocationsToolStrip
			// 
			this.LocationsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.LocationsToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.LocationsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshLocationsButton,
            this.RemoveLocationButton,
            this.AddLocationButton});
			this.LocationsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.LocationsToolStrip.Name = "LocationsToolStrip";
			this.LocationsToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this.LocationsToolStrip.Size = new System.Drawing.Size(740, 42);
			this.LocationsToolStrip.TabIndex = 2;
			this.LocationsToolStrip.Text = "MySettingsToolStrip";
			// 
			// RefreshLocationsButton
			// 
			this.RefreshLocationsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshLocationsButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshLocationsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshLocationsButton.Name = "RefreshLocationsButton";
			this.RefreshLocationsButton.Size = new System.Drawing.Size(118, 36);
			this.RefreshLocationsButton.Text = "Refresh";
			this.RefreshLocationsButton.Click += new System.EventHandler(this.RefreshLocationsButton_Click);
			// 
			// RemoveLocationButton
			// 
			this.RemoveLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RemoveLocationButton.Image = global::x360ce.App.Properties.Resources.remove_16x16;
			this.RemoveLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveLocationButton.Name = "RemoveLocationButton";
			this.RemoveLocationButton.Size = new System.Drawing.Size(125, 36);
			this.RemoveLocationButton.Text = "Remove";
			this.RemoveLocationButton.Click += new System.EventHandler(this.RemoveLocationButton_Click);
			// 
			// AddLocationButton
			// 
			this.AddLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AddLocationButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddLocationButton.Name = "AddLocationButton";
			this.AddLocationButton.Size = new System.Drawing.Size(97, 36);
			this.AddLocationButton.Text = "&Add...";
			this.AddLocationButton.Click += new System.EventHandler(this.AddLocationButton_Click);
			// 
			// ConfigurationGroupBox
			// 
			this.ConfigurationGroupBox.Controls.Add(this.AutoDetectForegroundWindowCheckBox);
			this.ConfigurationGroupBox.Controls.Add(this.IncludeProductsCheckBox);
			this.ConfigurationGroupBox.Controls.Add(this.ConfigurationVersionLabel);
			this.ConfigurationGroupBox.Controls.Add(this.ConfigurationVersionTextBox);
			this.ConfigurationGroupBox.Location = new System.Drawing.Point(526, 602);
			this.ConfigurationGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
			this.ConfigurationGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.ConfigurationGroupBox.Size = new System.Drawing.Size(554, 137);
			this.ConfigurationGroupBox.TabIndex = 31;
			this.ConfigurationGroupBox.TabStop = false;
			this.ConfigurationGroupBox.Text = "Configuration";
			// 
			// AutoDetectForegroundWindowCheckBox
			// 
			this.AutoDetectForegroundWindowCheckBox.AutoSize = true;
			this.AutoDetectForegroundWindowCheckBox.Location = new System.Drawing.Point(12, 87);
			this.AutoDetectForegroundWindowCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AutoDetectForegroundWindowCheckBox.Name = "AutoDetectForegroundWindowCheckBox";
			this.AutoDetectForegroundWindowCheckBox.Size = new System.Drawing.Size(482, 29);
			this.AutoDetectForegroundWindowCheckBox.TabIndex = 0;
			this.AutoDetectForegroundWindowCheckBox.Text = "Auto switch configuration when game focused";
			this.AutoDetectForegroundWindowCheckBox.UseVisualStyleBackColor = true;
			// 
			// IncludeProductsCheckBox
			// 
			this.IncludeProductsCheckBox.AutoSize = true;
			this.IncludeProductsCheckBox.Location = new System.Drawing.Point(248, 40);
			this.IncludeProductsCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.IncludeProductsCheckBox.Name = "IncludeProductsCheckBox";
			this.IncludeProductsCheckBox.Size = new System.Drawing.Size(216, 29);
			this.IncludeProductsCheckBox.TabIndex = 0;
			this.IncludeProductsCheckBox.Text = "Include [Products]";
			this.IncludeProductsCheckBox.UseVisualStyleBackColor = true;
			// 
			// ConfigurationVersionLabel
			// 
			this.ConfigurationVersionLabel.AutoSize = true;
			this.ConfigurationVersionLabel.Location = new System.Drawing.Point(8, 42);
			this.ConfigurationVersionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this.ConfigurationVersionLabel.Name = "ConfigurationVersionLabel";
			this.ConfigurationVersionLabel.Size = new System.Drawing.Size(91, 25);
			this.ConfigurationVersionLabel.TabIndex = 0;
			this.ConfigurationVersionLabel.Text = "Version:";
			// 
			// ConfigurationVersionTextBox
			// 
			this.ConfigurationVersionTextBox.Enabled = false;
			this.ConfigurationVersionTextBox.Location = new System.Drawing.Point(110, 37);
			this.ConfigurationVersionTextBox.Margin = new System.Windows.Forms.Padding(6);
			this.ConfigurationVersionTextBox.Name = "ConfigurationVersionTextBox";
			this.ConfigurationVersionTextBox.Size = new System.Drawing.Size(98, 31);
			this.ConfigurationVersionTextBox.TabIndex = 0;
			// 
			// DirectInputDevicesGroupBox
			// 
			this.DirectInputDevicesGroupBox.Controls.Add(this.UseDeviceBufferedDataCheckBox);
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeVirtualDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeSupplementalDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Location = new System.Drawing.Point(6, 496);
			this.DirectInputDevicesGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.DirectInputDevicesGroupBox.Name = "DirectInputDevicesGroupBox";
			this.DirectInputDevicesGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.DirectInputDevicesGroupBox.Size = new System.Drawing.Size(508, 175);
			this.DirectInputDevicesGroupBox.TabIndex = 31;
			this.DirectInputDevicesGroupBox.TabStop = false;
			this.DirectInputDevicesGroupBox.Text = "Direct Input Devices";
			// 
			// UseDeviceBufferedDataCheckBox
			// 
			this.UseDeviceBufferedDataCheckBox.AutoSize = true;
			this.UseDeviceBufferedDataCheckBox.Location = new System.Drawing.Point(12, 125);
			this.UseDeviceBufferedDataCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.UseDeviceBufferedDataCheckBox.Name = "UseDeviceBufferedDataCheckBox";
			this.UseDeviceBufferedDataCheckBox.Size = new System.Drawing.Size(292, 29);
			this.UseDeviceBufferedDataCheckBox.TabIndex = 0;
			this.UseDeviceBufferedDataCheckBox.Text = "Use Device Buffered Data";
			this.UseDeviceBufferedDataCheckBox.UseVisualStyleBackColor = true;
			// 
			// ExcludeVirtualDevicesCheckBox
			// 
			this.ExcludeVirtualDevicesCheckBox.AutoSize = true;
			this.ExcludeVirtualDevicesCheckBox.Location = new System.Drawing.Point(12, 81);
			this.ExcludeVirtualDevicesCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ExcludeVirtualDevicesCheckBox.Name = "ExcludeVirtualDevicesCheckBox";
			this.ExcludeVirtualDevicesCheckBox.Size = new System.Drawing.Size(271, 29);
			this.ExcludeVirtualDevicesCheckBox.TabIndex = 0;
			this.ExcludeVirtualDevicesCheckBox.Text = "Exclude Virtual Devices";
			this.ExcludeVirtualDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// ExcludeSupplementalDevicesCheckBox
			// 
			this.ExcludeSupplementalDevicesCheckBox.AutoSize = true;
			this.ExcludeSupplementalDevicesCheckBox.Location = new System.Drawing.Point(12, 37);
			this.ExcludeSupplementalDevicesCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ExcludeSupplementalDevicesCheckBox.Name = "ExcludeSupplementalDevicesCheckBox";
			this.ExcludeSupplementalDevicesCheckBox.Size = new System.Drawing.Size(341, 29);
			this.ExcludeSupplementalDevicesCheckBox.TabIndex = 0;
			this.ExcludeSupplementalDevicesCheckBox.Text = "Exclude Supplemental Devices";
			this.ExcludeSupplementalDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// DeveloperToolsButton
			// 
			this.DeveloperToolsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DeveloperToolsButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.DeveloperToolsButton.Location = new System.Drawing.Point(6, 845);
			this.DeveloperToolsButton.Margin = new System.Windows.Forms.Padding(6);
			this.DeveloperToolsButton.Name = "DeveloperToolsButton";
			this.DeveloperToolsButton.Size = new System.Drawing.Size(228, 44);
			this.DeveloperToolsButton.TabIndex = 69;
			this.DeveloperToolsButton.Text = "Developer Tools...";
			this.DeveloperToolsButton.UseVisualStyleBackColor = true;
			this.DeveloperToolsButton.Click += new System.EventHandler(this.DeveloperToolsButton_Click);
			// 
			// AllowRemoteControllersGroupBox
			// 
			this.AllowRemoteControllersGroupBox.Controls.Add(this.RemoteEnabledCheckBox);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.AllowRemote4CheckBox);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.RemotePortLabel);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.RemotePasswordLabel);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.AllowControlLabel);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.AllowRemote3CheckBox);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.RemotePortNumericUpDown);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.RemotePasswordTextBox);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.AllowRemote2CheckBox);
			this.AllowRemoteControllersGroupBox.Controls.Add(this.AllowRemote1CheckBox);
			this.AllowRemoteControllersGroupBox.Location = new System.Drawing.Point(6, 6);
			this.AllowRemoteControllersGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowRemoteControllersGroupBox.Name = "AllowRemoteControllersGroupBox";
			this.AllowRemoteControllersGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.AllowRemoteControllersGroupBox.Size = new System.Drawing.Size(538, 404);
			this.AllowRemoteControllersGroupBox.TabIndex = 31;
			this.AllowRemoteControllersGroupBox.TabStop = false;
			this.AllowRemoteControllersGroupBox.Text = "Allow Remote Controllers";
			this.AllowRemoteControllersGroupBox.Visible = false;
			// 
			// RemoteEnabledCheckBox
			// 
			this.RemoteEnabledCheckBox.AutoSize = true;
			this.RemoteEnabledCheckBox.Location = new System.Drawing.Point(208, 350);
			this.RemoteEnabledCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.RemoteEnabledCheckBox.Name = "RemoteEnabledCheckBox";
			this.RemoteEnabledCheckBox.Size = new System.Drawing.Size(123, 29);
			this.RemoteEnabledCheckBox.TabIndex = 33;
			this.RemoteEnabledCheckBox.Text = "Enabled";
			this.RemoteEnabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// AllowRemote4CheckBox
			// 
			this.AllowRemote4CheckBox.AutoSize = true;
			this.AllowRemote4CheckBox.Location = new System.Drawing.Point(210, 206);
			this.AllowRemote4CheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowRemote4CheckBox.Name = "AllowRemote4CheckBox";
			this.AllowRemote4CheckBox.Size = new System.Drawing.Size(155, 29);
			this.AllowRemote4CheckBox.TabIndex = 30;
			this.AllowRemote4CheckBox.Text = "Controller 4";
			this.AllowRemote4CheckBox.UseVisualStyleBackColor = true;
			// 
			// RemotePortLabel
			// 
			this.RemotePortLabel.AutoSize = true;
			this.RemotePortLabel.BackColor = System.Drawing.SystemColors.Control;
			this.RemotePortLabel.Location = new System.Drawing.Point(12, 304);
			this.RemotePortLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this.RemotePortLabel.Name = "RemotePortLabel";
			this.RemotePortLabel.Size = new System.Drawing.Size(170, 25);
			this.RemotePortLabel.TabIndex = 32;
			this.RemotePortLabel.Text = "UDP Server Port";
			// 
			// RemotePasswordLabel
			// 
			this.RemotePasswordLabel.AutoSize = true;
			this.RemotePasswordLabel.BackColor = System.Drawing.SystemColors.Control;
			this.RemotePasswordLabel.Location = new System.Drawing.Point(12, 256);
			this.RemotePasswordLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this.RemotePasswordLabel.Name = "RemotePasswordLabel";
			this.RemotePasswordLabel.Size = new System.Drawing.Size(186, 25);
			this.RemotePasswordLabel.TabIndex = 32;
			this.RemotePasswordLabel.Text = "Remote Password";
			// 
			// AllowControlLabel
			// 
			this.AllowControlLabel.AutoSize = true;
			this.AllowControlLabel.BackColor = System.Drawing.SystemColors.Control;
			this.AllowControlLabel.Location = new System.Drawing.Point(12, 75);
			this.AllowControlLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this.AllowControlLabel.Name = "AllowControlLabel";
			this.AllowControlLabel.Size = new System.Drawing.Size(138, 25);
			this.AllowControlLabel.TabIndex = 32;
			this.AllowControlLabel.Text = "Allow Control";
			// 
			// AllowRemote3CheckBox
			// 
			this.AllowRemote3CheckBox.AutoSize = true;
			this.AllowRemote3CheckBox.Location = new System.Drawing.Point(210, 162);
			this.AllowRemote3CheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowRemote3CheckBox.Name = "AllowRemote3CheckBox";
			this.AllowRemote3CheckBox.Size = new System.Drawing.Size(155, 29);
			this.AllowRemote3CheckBox.TabIndex = 30;
			this.AllowRemote3CheckBox.Text = "Controller 3";
			this.AllowRemote3CheckBox.UseVisualStyleBackColor = true;
			// 
			// RemotePortNumericUpDown
			// 
			this.RemotePortNumericUpDown.Location = new System.Drawing.Point(208, 300);
			this.RemotePortNumericUpDown.Margin = new System.Windows.Forms.Padding(6);
			this.RemotePortNumericUpDown.Maximum = new decimal(new int[] {
            49151,
            0,
            0,
            0});
			this.RemotePortNumericUpDown.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.RemotePortNumericUpDown.Name = "RemotePortNumericUpDown";
			this.RemotePortNumericUpDown.Size = new System.Drawing.Size(118, 31);
			this.RemotePortNumericUpDown.TabIndex = 31;
			this.RemotePortNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.RemotePortNumericUpDown.Value = new decimal(new int[] {
            26010,
            0,
            0,
            0});
			// 
			// RemotePasswordTextBox
			// 
			this.RemotePasswordTextBox.Location = new System.Drawing.Point(210, 250);
			this.RemotePasswordTextBox.Margin = new System.Windows.Forms.Padding(6);
			this.RemotePasswordTextBox.Name = "RemotePasswordTextBox";
			this.RemotePasswordTextBox.Size = new System.Drawing.Size(296, 31);
			this.RemotePasswordTextBox.TabIndex = 0;
			this.RemotePasswordTextBox.UseSystemPasswordChar = true;
			// 
			// AllowRemote2CheckBox
			// 
			this.AllowRemote2CheckBox.AutoSize = true;
			this.AllowRemote2CheckBox.Location = new System.Drawing.Point(210, 117);
			this.AllowRemote2CheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowRemote2CheckBox.Name = "AllowRemote2CheckBox";
			this.AllowRemote2CheckBox.Size = new System.Drawing.Size(155, 29);
			this.AllowRemote2CheckBox.TabIndex = 30;
			this.AllowRemote2CheckBox.Text = "Controller 2";
			this.AllowRemote2CheckBox.UseVisualStyleBackColor = true;
			// 
			// AllowRemote1CheckBox
			// 
			this.AllowRemote1CheckBox.AutoSize = true;
			this.AllowRemote1CheckBox.Location = new System.Drawing.Point(210, 73);
			this.AllowRemote1CheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.AllowRemote1CheckBox.Name = "AllowRemote1CheckBox";
			this.AllowRemote1CheckBox.Size = new System.Drawing.Size(155, 29);
			this.AllowRemote1CheckBox.TabIndex = 30;
			this.AllowRemote1CheckBox.Text = "Controller 1";
			this.AllowRemote1CheckBox.UseVisualStyleBackColor = true;
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.GeneralTabPage);
			this.MainTabControl.Controls.Add(this.InternetOptionsTabPage);
			this.MainTabControl.Controls.Add(this.RemoteControllerTabPage);
			this.MainTabControl.Controls.Add(this.VirtualDeviceTabPage);
			this.MainTabControl.Controls.Add(this.HidGuardianTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Location = new System.Drawing.Point(0, 0);
			this.MainTabControl.Margin = new System.Windows.Forms.Padding(6);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(1316, 954);
			this.MainTabControl.TabIndex = 71;
			// 
			// GeneralTabPage
			// 
			this.GeneralTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.GeneralTabPage.Controls.Add(this.GeneralPanel);
			this.GeneralTabPage.Location = new System.Drawing.Point(8, 39);
			this.GeneralTabPage.Margin = new System.Windows.Forms.Padding(6);
			this.GeneralTabPage.Name = "GeneralTabPage";
			this.GeneralTabPage.Padding = new System.Windows.Forms.Padding(6);
			this.GeneralTabPage.Size = new System.Drawing.Size(1300, 907);
			this.GeneralTabPage.TabIndex = 0;
			this.GeneralTabPage.Text = "General";
			// 
			// GeneralPanel
			// 
			this.GeneralPanel.Controls.Add(this.OperationGroupBox);
			this.GeneralPanel.Controls.Add(this.DeveloperToolsButton);
			this.GeneralPanel.Controls.Add(this.TestingAndLoggingGroupBox);
			this.GeneralPanel.Controls.Add(this.DevelopingGroupBox);
			this.GeneralPanel.Controls.Add(this.DirectInputDevicesGroupBox);
			this.GeneralPanel.Controls.Add(this.GuideButtonGroupBox);
			this.GeneralPanel.Controls.Add(this.ConfigurationGroupBox);
			this.GeneralPanel.Controls.Add(this.ProgramScanLocationsTabControl);
			this.GeneralPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GeneralPanel.Location = new System.Drawing.Point(6, 6);
			this.GeneralPanel.Margin = new System.Windows.Forms.Padding(6);
			this.GeneralPanel.Name = "GeneralPanel";
			this.GeneralPanel.Size = new System.Drawing.Size(1288, 895);
			this.GeneralPanel.TabIndex = 0;
			// 
			// DevelopingGroupBox
			// 
			this.DevelopingGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DevelopingGroupBox.Controls.Add(this.ShowTestButtonCheckBox);
			this.DevelopingGroupBox.Controls.Add(this.ShowFormInfoCheckBox);
			this.DevelopingGroupBox.Location = new System.Drawing.Point(526, 458);
			this.DevelopingGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.DevelopingGroupBox.Name = "DevelopingGroupBox";
			this.DevelopingGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.DevelopingGroupBox.Size = new System.Drawing.Size(752, 133);
			this.DevelopingGroupBox.TabIndex = 31;
			this.DevelopingGroupBox.TabStop = false;
			this.DevelopingGroupBox.Text = "Developing";
			// 
			// ShowTestButtonCheckBox
			// 
			this.ShowTestButtonCheckBox.AutoSize = true;
			this.ShowTestButtonCheckBox.Location = new System.Drawing.Point(12, 81);
			this.ShowTestButtonCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ShowTestButtonCheckBox.Name = "ShowTestButtonCheckBox";
			this.ShowTestButtonCheckBox.Size = new System.Drawing.Size(243, 29);
			this.ShowTestButtonCheckBox.TabIndex = 1;
			this.ShowTestButtonCheckBox.Text = "Show [Test...] Button";
			this.ShowTestButtonCheckBox.UseVisualStyleBackColor = true;
			// 
			// ShowFormInfoCheckBox
			// 
			this.ShowFormInfoCheckBox.AutoSize = true;
			this.ShowFormInfoCheckBox.Location = new System.Drawing.Point(12, 37);
			this.ShowFormInfoCheckBox.Margin = new System.Windows.Forms.Padding(6);
			this.ShowFormInfoCheckBox.Name = "ShowFormInfoCheckBox";
			this.ShowFormInfoCheckBox.Size = new System.Drawing.Size(445, 29);
			this.ShowFormInfoCheckBox.TabIndex = 1;
			this.ShowFormInfoCheckBox.Text = "Show Form Info on CTRL+SHIFT+RButton";
			this.ShowFormInfoCheckBox.UseVisualStyleBackColor = true;
			this.ShowFormInfoCheckBox.CheckedChanged += new System.EventHandler(this.ShowSettingsTabCheckBox_CheckedChanged);
			// 
			// GuideButtonGroupBox
			// 
			this.GuideButtonGroupBox.Controls.Add(this.GuideButtonActionLabel);
			this.GuideButtonGroupBox.Controls.Add(this.GuideButtonActionTextBox);
			this.GuideButtonGroupBox.Location = new System.Drawing.Point(6, 675);
			this.GuideButtonGroupBox.Margin = new System.Windows.Forms.Padding(6);
			this.GuideButtonGroupBox.Name = "GuideButtonGroupBox";
			this.GuideButtonGroupBox.Padding = new System.Windows.Forms.Padding(6);
			this.GuideButtonGroupBox.Size = new System.Drawing.Size(508, 90);
			this.GuideButtonGroupBox.TabIndex = 31;
			this.GuideButtonGroupBox.TabStop = false;
			this.GuideButtonGroupBox.Text = "Guide Button";
			// 
			// GuideButtonActionLabel
			// 
			this.GuideButtonActionLabel.AutoSize = true;
			this.GuideButtonActionLabel.Location = new System.Drawing.Point(8, 42);
			this.GuideButtonActionLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this.GuideButtonActionLabel.Name = "GuideButtonActionLabel";
			this.GuideButtonActionLabel.Size = new System.Drawing.Size(78, 25);
			this.GuideButtonActionLabel.TabIndex = 0;
			this.GuideButtonActionLabel.Text = "Action:";
			// 
			// GuideButtonActionTextBox
			// 
			this.GuideButtonActionTextBox.Location = new System.Drawing.Point(100, 37);
			this.GuideButtonActionTextBox.Margin = new System.Windows.Forms.Padding(6);
			this.GuideButtonActionTextBox.Name = "GuideButtonActionTextBox";
			this.GuideButtonActionTextBox.Size = new System.Drawing.Size(254, 31);
			this.GuideButtonActionTextBox.TabIndex = 0;
			// 
			// InternetOptionsTabPage
			// 
			this.InternetOptionsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.InternetOptionsTabPage.Controls.Add(this.InternetHost);
			this.InternetOptionsTabPage.Location = new System.Drawing.Point(8, 39);
			this.InternetOptionsTabPage.Margin = new System.Windows.Forms.Padding(6);
			this.InternetOptionsTabPage.Name = "InternetOptionsTabPage";
			this.InternetOptionsTabPage.Padding = new System.Windows.Forms.Padding(6);
			this.InternetOptionsTabPage.Size = new System.Drawing.Size(1300, 907);
			this.InternetOptionsTabPage.TabIndex = 1;
			this.InternetOptionsTabPage.Text = "Internet";
			// 
			// RemoteControllerTabPage
			// 
			this.RemoteControllerTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.RemoteControllerTabPage.Controls.Add(this.VirtualDevicePanel);
			this.RemoteControllerTabPage.Location = new System.Drawing.Point(8, 39);
			this.RemoteControllerTabPage.Margin = new System.Windows.Forms.Padding(6);
			this.RemoteControllerTabPage.Name = "RemoteControllerTabPage";
			this.RemoteControllerTabPage.Size = new System.Drawing.Size(1300, 907);
			this.RemoteControllerTabPage.TabIndex = 2;
			this.RemoteControllerTabPage.Text = "Remote Controller";
			// 
			// VirtualDevicePanel
			// 
			this.VirtualDevicePanel.Controls.Add(this.AllowRemoteControllersGroupBox);
			this.VirtualDevicePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.VirtualDevicePanel.Location = new System.Drawing.Point(0, 0);
			this.VirtualDevicePanel.Margin = new System.Windows.Forms.Padding(6);
			this.VirtualDevicePanel.Name = "VirtualDevicePanel";
			this.VirtualDevicePanel.Size = new System.Drawing.Size(1300, 907);
			this.VirtualDevicePanel.TabIndex = 72;
			// 
			// VirtualDeviceTabPage
			// 
			this.VirtualDeviceTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.VirtualDeviceTabPage.Controls.Add(this.OptionsVirtualDeviceHost);
			this.VirtualDeviceTabPage.Location = new System.Drawing.Point(8, 39);
			this.VirtualDeviceTabPage.Name = "VirtualDeviceTabPage";
			this.VirtualDeviceTabPage.Size = new System.Drawing.Size(1300, 907);
			this.VirtualDeviceTabPage.TabIndex = 4;
			this.VirtualDeviceTabPage.Text = "Virtual Device";
			// 
			// OptionsVirtualDeviceHost
			// 
			this.OptionsVirtualDeviceHost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.OptionsVirtualDeviceHost.Location = new System.Drawing.Point(0, 0);
			this.OptionsVirtualDeviceHost.Name = "OptionsVirtualDeviceHost";
			this.OptionsVirtualDeviceHost.Size = new System.Drawing.Size(1300, 907);
			this.OptionsVirtualDeviceHost.TabIndex = 0;
			this.OptionsVirtualDeviceHost.Child = null;
			// 
			// HidGuardianTabPage
			// 
			this.HidGuardianTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.HidGuardianTabPage.Controls.Add(this.OptionsHidGuardianHost);
			this.HidGuardianTabPage.Location = new System.Drawing.Point(8, 39);
			this.HidGuardianTabPage.Margin = new System.Windows.Forms.Padding(6);
			this.HidGuardianTabPage.Name = "HidGuardianTabPage";
			this.HidGuardianTabPage.Size = new System.Drawing.Size(1300, 907);
			this.HidGuardianTabPage.TabIndex = 3;
			this.HidGuardianTabPage.Text = "HID Guardian";
			// 
			// OptionsHidGuardianHost
			// 
			this.OptionsHidGuardianHost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.OptionsHidGuardianHost.Location = new System.Drawing.Point(0, 0);
			this.OptionsHidGuardianHost.Name = "OptionsHidGuardianHost";
			this.OptionsHidGuardianHost.Size = new System.Drawing.Size(1300, 907);
			this.OptionsHidGuardianHost.TabIndex = 0;
			this.OptionsHidGuardianHost.Child = null;
			// 
			// InternetHost
			// 
			this.InternetHost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.InternetHost.Location = new System.Drawing.Point(6, 6);
			this.InternetHost.Name = "InternetHost";
			this.InternetHost.Size = new System.Drawing.Size(1288, 895);
			this.InternetHost.TabIndex = 0;
			this.InternetHost.Child = this.InternetPanel;
			// 
			// OptionsUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainTabControl);
			this.Margin = new System.Windows.Forms.Padding(6);
			this.Name = "OptionsUserControl";
			this.Size = new System.Drawing.Size(1316, 954);
			this.TestingAndLoggingGroupBox.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.PerformLayout();
			this.OperationGroupBox.ResumeLayout(false);
			this.OperationGroupBox.PerformLayout();
			this.ProgramScanLocationsTabControl.ResumeLayout(false);
			this.GameScanLocationsTabPage.ResumeLayout(false);
			this.GameScanLocationsTabPage.PerformLayout();
			this.LocationsToolStrip.ResumeLayout(false);
			this.LocationsToolStrip.PerformLayout();
			this.ConfigurationGroupBox.ResumeLayout(false);
			this.ConfigurationGroupBox.PerformLayout();
			this.DirectInputDevicesGroupBox.ResumeLayout(false);
			this.DirectInputDevicesGroupBox.PerformLayout();
			this.AllowRemoteControllersGroupBox.ResumeLayout(false);
			this.AllowRemoteControllersGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.RemotePortNumericUpDown)).EndInit();
			this.MainTabControl.ResumeLayout(false);
			this.GeneralTabPage.ResumeLayout(false);
			this.GeneralPanel.ResumeLayout(false);
			this.DevelopingGroupBox.ResumeLayout(false);
			this.DevelopingGroupBox.PerformLayout();
			this.GuideButtonGroupBox.ResumeLayout(false);
			this.GuideButtonGroupBox.PerformLayout();
			this.InternetOptionsTabPage.ResumeLayout(false);
			this.RemoteControllerTabPage.ResumeLayout(false);
			this.VirtualDevicePanel.ResumeLayout(false);
			this.VirtualDeviceTabPage.ResumeLayout(false);
			this.HidGuardianTabPage.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox TestingAndLoggingGroupBox;
        private System.Windows.Forms.CheckBox XInputEnableCheckBox;
        private System.Windows.Forms.CheckBox ConsoleCheckBox;
        private System.Windows.Forms.CheckBox DebugModeCheckBox;
        private System.Windows.Forms.CheckBox EnableLoggingCheckBox;
        private System.Windows.Forms.GroupBox OperationGroupBox;
        public System.Windows.Forms.CheckBox AllowOnlyOneCopyCheckBox;
        private System.Windows.Forms.TabControl ProgramScanLocationsTabControl;
		private System.Windows.Forms.TabPage GameScanLocationsTabPage;
		private System.Windows.Forms.FolderBrowserDialog LocationFolderBrowserDialog;
        public System.Windows.Forms.ListBox GameScanLocationsListBox;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
        private System.Windows.Forms.Label ConfigurationVersionLabel;
        private System.Windows.Forms.TextBox ConfigurationVersionTextBox;
        private System.Windows.Forms.ToolStrip LocationsToolStrip;
        private System.Windows.Forms.ToolStripButton RefreshLocationsButton;
        private System.Windows.Forms.ToolStripButton AddLocationButton;
        private System.Windows.Forms.ToolStripButton RemoveLocationButton;
		private System.Windows.Forms.GroupBox DirectInputDevicesGroupBox;
		private System.Windows.Forms.CheckBox ExcludeSupplementalDevicesCheckBox;
		private System.Windows.Forms.CheckBox ExcludeVirtualDevicesCheckBox;
		internal System.Windows.Forms.CheckBox MinimizeToTrayCheckBox;
		private System.Windows.Forms.CheckBox ShowProgramsTabCheckBox;
		private System.Windows.Forms.CheckBox ShowSettingsTabCheckBox;
		private System.Windows.Forms.CheckBox ShowDevicesTabCheckBox;
		public System.Windows.Forms.CheckBox IncludeProductsCheckBox;
		private System.Windows.Forms.Button DeveloperToolsButton;
		internal System.Windows.Forms.CheckBox AlwaysOnTopCheckBox;
		public System.Windows.Forms.ComboBox StartWithWindowsStateComboBox;
		public System.Windows.Forms.CheckBox StartWithWindowsCheckBox;
		private System.Windows.Forms.GroupBox AllowRemoteControllersGroupBox;
		public System.Windows.Forms.CheckBox AllowRemote3CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote2CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote1CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote4CheckBox;
		private System.Windows.Forms.TextBox RemotePasswordTextBox;
		private System.Windows.Forms.Label RemotePortLabel;
		public System.Windows.Forms.NumericUpDown RemotePortNumericUpDown;
		private System.Windows.Forms.Label RemotePasswordLabel;
		public System.Windows.Forms.CheckBox RemoteEnabledCheckBox;
		private System.Windows.Forms.Label AllowControlLabel;
		private System.Windows.Forms.TabPage GeneralTabPage;
		private System.Windows.Forms.TabPage InternetOptionsTabPage;
		private System.Windows.Forms.Panel GeneralPanel;
		private System.Windows.Forms.Panel VirtualDevicePanel;
		private System.Windows.Forms.GroupBox DevelopingGroupBox;
		private System.Windows.Forms.CheckBox ShowFormInfoCheckBox;
		private System.Windows.Forms.CheckBox ShowTestButtonCheckBox;
		private System.Windows.Forms.CheckBox UseDeviceBufferedDataCheckBox;
		public System.Windows.Forms.TabControl MainTabControl;
		public System.Windows.Forms.TabPage RemoteControllerTabPage;
		public System.Windows.Forms.TabPage HidGuardianTabPage;
		private System.Windows.Forms.GroupBox GuideButtonGroupBox;
		private System.Windows.Forms.Label GuideButtonActionLabel;
		private System.Windows.Forms.TextBox GuideButtonActionTextBox;
		public System.Windows.Forms.CheckBox AutoDetectForegroundWindowCheckBox;
		public System.Windows.Forms.CheckBox IsProcessDPIAwareCheckBox;
		private System.Windows.Forms.Integration.ElementHost OptionsHidGuardianHost;
		private System.Windows.Forms.TabPage VirtualDeviceTabPage;
		private System.Windows.Forms.Integration.ElementHost OptionsVirtualDeviceHost;
		public System.Windows.Forms.Integration.ElementHost InternetHost;
		public OptionsInternetControl InternetPanel;
	}
}
