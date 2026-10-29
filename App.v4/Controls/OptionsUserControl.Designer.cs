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
			this.TestingGroupBox = new System.Windows.Forms.GroupBox();
			this.ShowSettingsTabCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowDevicesTabCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowProgramsTabCheckBox = new System.Windows.Forms.CheckBox();
			this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
			this.OperationGroupBox = new System.Windows.Forms.GroupBox();
			this.StartWithWindowsStateComboBox = new System.Windows.Forms.ComboBox();
			this.ThemeLabel = new System.Windows.Forms.Label();
			this.ThemeComboBox = new System.Windows.Forms.ComboBox();
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
			this.DirectInputDevicesGroupBox = new System.Windows.Forms.GroupBox();
			this.ReadEveryChangeCheckBox = new System.Windows.Forms.CheckBox();
			this.ExcludeVirtualDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.ExcludeSupplementalDevicesCheckBox = new System.Windows.Forms.CheckBox();
			this.VirtualDeviceGroupBox = new System.Windows.Forms.GroupBox();
			this.AboutViGEmLinkLabel = new System.Windows.Forms.LinkLabel();
			this.ViGEmBusLabel = new System.Windows.Forms.Label();
			this.VigemLabel = new System.Windows.Forms.Label();
			this.PollingRateLabel = new System.Windows.Forms.Label();
			this.PollingRateComboBox = new System.Windows.Forms.ComboBox();
			this.ViGEmBusRefreshButton = new System.Windows.Forms.Button();
			this.ViGEmBusUninstallButton = new System.Windows.Forms.Button();
			this.ViGEmBusInstallButton = new System.Windows.Forms.Button();
			this.ViGEmBusTextBox = new System.Windows.Forms.TextBox();
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
			this.HidHideTabPage = new System.Windows.Forms.TabPage();
			this.HidHidePanel = new System.Windows.Forms.TableLayoutPanel();
			this.HidHideGroupBox = new System.Windows.Forms.GroupBox();
			this.HidHideLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.HidHideStatusLabel = new System.Windows.Forms.Label();
			this.HidHideStatusTextBox = new System.Windows.Forms.TextBox();
			this.HidHideRefreshButton = new System.Windows.Forms.Button();
			this.HidHideButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.HidHideDownloadButton = new System.Windows.Forms.Button();
			this.HidHideConfigureButton = new System.Windows.Forms.Button();
			this.HidHideInfoLabel = new System.Windows.Forms.Label();
			this.GeneralTabPage = new System.Windows.Forms.TabPage();
			this.GeneralPanel = new System.Windows.Forms.Panel();
			this.DevelopingGroupBox = new System.Windows.Forms.GroupBox();
			this.ShowTestButtonCheckBox = new System.Windows.Forms.CheckBox();
			this.ShowFormInfoCheckBox = new System.Windows.Forms.CheckBox();
			this.GuideButtonGroupBox = new System.Windows.Forms.GroupBox();
			this.GuideButtonActionLabel = new System.Windows.Forms.Label();
			this.GuideButtonActionTextBox = new System.Windows.Forms.TextBox();
			this.HotkeysGroupBox = new System.Windows.Forms.GroupBox();
			this.EmulationHotkeyCheckBox = new System.Windows.Forms.CheckBox();
			this.EmulationHotkeyTextBox = new System.Windows.Forms.TextBox();
			this.EmulationButtonsCheckBox = new System.Windows.Forms.CheckBox();
			this.EmulationButtonsTextBox = new System.Windows.Forms.TextBox();
			this.EmulationOverlayCheckBox = new System.Windows.Forms.CheckBox();
			this.DeviceChangeOverlayCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetOptionsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsPanel = new x360ce.App.Controls.OptionsSettingsUserControl();
			this.UpdateTabPage = new System.Windows.Forms.TabPage();
			this.AiTabPage = new System.Windows.Forms.TabPage();
			this.AiControl = new JocysCom.ClassLibrary.Mcp.AiAccessUserControl();
			this.PluginsTabPage = new System.Windows.Forms.TabPage();
			this.PluginsPanel = new x360ce.App.Controls.OptionsPluginsUserControl();
			this.UpdatePanel = new x360ce.App.Controls.OptionsUpdateUserControl();
			this.InternetPanel = new x360ce.App.Controls.OptionsInternetUserControl();
			this.VirtualDeviceTabPage = new System.Windows.Forms.TabPage();
			this.VirtualDevicePanel = new System.Windows.Forms.TableLayoutPanel();
			this.VirtualDeviceLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ViGEmLinkPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.HidGuardianTabPage = new System.Windows.Forms.TabPage();
			this.HidGuardianPanel = new System.Windows.Forms.TableLayoutPanel();
			this.HidGuardianLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.HelpRichTextBox = new System.Windows.Forms.RichTextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.HidGuardianRefreshButton = new System.Windows.Forms.Button();
			this.HidHideConfigureAutomaticallyCheckBox = new System.Windows.Forms.CheckBox();
			this.HidGuardianLabel = new System.Windows.Forms.Label();
			this.HidGuardianUninstallButton = new System.Windows.Forms.Button();
			this.HidGuardianInstallButton = new System.Windows.Forms.Button();
			this.HidGuardianTextBox = new System.Windows.Forms.TextBox();
			this.TestingGroupBox.SuspendLayout();
			this.OperationGroupBox.SuspendLayout();
			this.ProgramScanLocationsTabControl.SuspendLayout();
			this.GameScanLocationsTabPage.SuspendLayout();
			this.LocationsToolStrip.SuspendLayout();
			this.ConfigurationGroupBox.SuspendLayout();
			this.DirectInputDevicesGroupBox.SuspendLayout();
			this.VirtualDeviceGroupBox.SuspendLayout();
			this.VirtualDeviceLayoutPanel.SuspendLayout();
			this.ViGEmLinkPanel.SuspendLayout();
			this.AllowRemoteControllersGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RemotePortNumericUpDown)).BeginInit();
			this.MainTabControl.SuspendLayout();
			this.GeneralTabPage.SuspendLayout();
			this.GeneralPanel.SuspendLayout();
			this.DevelopingGroupBox.SuspendLayout();
			this.GuideButtonGroupBox.SuspendLayout();
			this.HotkeysGroupBox.SuspendLayout();
			this.InternetOptionsTabPage.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			this.UpdateTabPage.SuspendLayout();
			this.AiTabPage.SuspendLayout();
			this.PluginsTabPage.SuspendLayout();
			this.VirtualDeviceTabPage.SuspendLayout();
			this.VirtualDevicePanel.SuspendLayout();
			this.HidGuardianTabPage.SuspendLayout();
			this.HidHideTabPage.SuspendLayout();
			this.HidHidePanel.SuspendLayout();
			this.HidHideGroupBox.SuspendLayout();
			this.HidHideLayoutPanel.SuspendLayout();
			this.HidHideButtonsPanel.SuspendLayout();
			this.HidGuardianPanel.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.HidGuardianLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// TestingGroupBox
			// 
			this.TestingGroupBox.Controls.Add(this.ShowSettingsTabCheckBox);
			this.TestingGroupBox.Controls.Add(this.ShowDevicesTabCheckBox);
			this.TestingGroupBox.Controls.Add(this.ShowProgramsTabCheckBox);
			this.TestingGroupBox.Controls.Add(this.XInputEnableCheckBox);
			this.TestingGroupBox.Location = new System.Drawing.Point(3, 141);
			this.TestingGroupBox.Name = "TestingGroupBox";
			this.TestingGroupBox.Size = new System.Drawing.Size(254, 94);
			this.TestingGroupBox.TabIndex = 30;
			this.TestingGroupBox.TabStop = false;
			this.TestingGroupBox.Text = "Testing";
			// 
			// ShowSettingsTabCheckBox
			// 
			this.ShowSettingsTabCheckBox.AutoSize = true;
			this.ShowSettingsTabCheckBox.Location = new System.Drawing.Point(120, 71);
			this.ShowSettingsTabCheckBox.Name = "ShowSettingsTabCheckBox";
			this.ShowSettingsTabCheckBox.Size = new System.Drawing.Size(122, 17);
			this.ShowSettingsTabCheckBox.TabIndex = 1;
			this.ShowSettingsTabCheckBox.Text = "Show [Settings] Tab";
			this.ShowSettingsTabCheckBox.UseVisualStyleBackColor = true;
			this.ShowSettingsTabCheckBox.CheckedChanged += new System.EventHandler(this.ShowSettingsTabCheckBox_CheckedChanged);
			// 
			// ShowDevicesTabCheckBox
			// 
			this.ShowDevicesTabCheckBox.AutoSize = true;
			this.ShowDevicesTabCheckBox.Location = new System.Drawing.Point(120, 48);
			this.ShowDevicesTabCheckBox.Name = "ShowDevicesTabCheckBox";
			this.ShowDevicesTabCheckBox.Size = new System.Drawing.Size(123, 17);
			this.ShowDevicesTabCheckBox.TabIndex = 1;
			this.ShowDevicesTabCheckBox.Text = "Show [Devices] Tab";
			this.ShowDevicesTabCheckBox.UseVisualStyleBackColor = true;
			this.ShowDevicesTabCheckBox.CheckedChanged += new System.EventHandler(this.ShowDevicesTabCheckBox_CheckedChanged);
			// 
			// ShowProgramsTabCheckBox
			// 
			this.ShowProgramsTabCheckBox.AutoSize = true;
			this.ShowProgramsTabCheckBox.Location = new System.Drawing.Point(120, 25);
			this.ShowProgramsTabCheckBox.Name = "ShowProgramsTabCheckBox";
			this.ShowProgramsTabCheckBox.Size = new System.Drawing.Size(128, 17);
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
			this.XInputEnableCheckBox.Location = new System.Drawing.Point(6, 25);
			this.XInputEnableCheckBox.Name = "XInputEnableCheckBox";
			this.XInputEnableCheckBox.Size = new System.Drawing.Size(93, 17);
			this.XInputEnableCheckBox.TabIndex = 0;
			this.XInputEnableCheckBox.Text = "Enable XInput";
			this.XInputEnableCheckBox.UseVisualStyleBackColor = true;
			//
			// OperationGroupBox
			// 
			this.OperationGroupBox.Controls.Add(this.ThemeComboBox);
			this.OperationGroupBox.Controls.Add(this.ThemeLabel);
			this.OperationGroupBox.Controls.Add(this.StartWithWindowsStateComboBox);
			this.OperationGroupBox.Controls.Add(this.StartWithWindowsCheckBox);
			this.OperationGroupBox.Controls.Add(this.AlwaysOnTopCheckBox);
			this.OperationGroupBox.Controls.Add(this.MinimizeToTrayCheckBox);
			this.OperationGroupBox.Controls.Add(this.AllowOnlyOneCopyCheckBox);
			this.OperationGroupBox.Location = new System.Drawing.Point(3, 3);
			this.OperationGroupBox.Name = "OperationGroupBox";
			this.OperationGroupBox.Size = new System.Drawing.Size(254, 132);
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
			this.StartWithWindowsStateComboBox.Location = new System.Drawing.Point(124, 75);
			this.StartWithWindowsStateComboBox.Name = "StartWithWindowsStateComboBox";
			this.StartWithWindowsStateComboBox.Size = new System.Drawing.Size(90, 21);
			this.StartWithWindowsStateComboBox.TabIndex = 95;
			//
			// ThemeLabel
			//
			this.ThemeLabel.AutoSize = true;
			this.ThemeLabel.Location = new System.Drawing.Point(6, 103);
			this.ThemeLabel.Name = "ThemeLabel";
			this.ThemeLabel.Size = new System.Drawing.Size(43, 13);
			this.ThemeLabel.TabIndex = 96;
			this.ThemeLabel.Text = "Theme:";
			//
			// ThemeComboBox
			//
			this.ThemeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ThemeComboBox.FormattingEnabled = true;
			this.ThemeComboBox.Location = new System.Drawing.Point(124, 99);
			this.ThemeComboBox.Name = "ThemeComboBox";
			this.ThemeComboBox.Size = new System.Drawing.Size(90, 21);
			this.ThemeComboBox.TabIndex = 97;
			// 
			// StartWithWindowsCheckBox
			// 
			this.StartWithWindowsCheckBox.AutoSize = true;
			this.StartWithWindowsCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.StartWithWindowsCheckBox.Location = new System.Drawing.Point(6, 76);
			this.StartWithWindowsCheckBox.Name = "StartWithWindowsCheckBox";
			this.StartWithWindowsCheckBox.Size = new System.Drawing.Size(126, 18);
			this.StartWithWindowsCheckBox.TabIndex = 94;
			this.StartWithWindowsCheckBox.Text = "Start with Windows:";
			// 
			// AlwaysOnTopCheckBox
			// 
			this.AlwaysOnTopCheckBox.AutoSize = true;
			this.AlwaysOnTopCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.AlwaysOnTopCheckBox.Location = new System.Drawing.Point(120, 52);
			this.AlwaysOnTopCheckBox.Name = "AlwaysOnTopCheckBox";
			this.AlwaysOnTopCheckBox.Size = new System.Drawing.Size(102, 18);
			this.AlwaysOnTopCheckBox.TabIndex = 93;
			this.AlwaysOnTopCheckBox.Text = "Always on Top";
			// 
			// MinimizeToTrayCheckBox
			// 
			this.MinimizeToTrayCheckBox.AutoSize = true;
			this.MinimizeToTrayCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.MinimizeToTrayCheckBox.Location = new System.Drawing.Point(6, 52);
			this.MinimizeToTrayCheckBox.Name = "MinimizeToTrayCheckBox";
			this.MinimizeToTrayCheckBox.Size = new System.Drawing.Size(108, 18);
			this.MinimizeToTrayCheckBox.TabIndex = 93;
			this.MinimizeToTrayCheckBox.Text = "Minimize to Tray";
			// 
			// AllowOnlyOneCopyCheckBox
			// 
			this.AllowOnlyOneCopyCheckBox.AutoSize = true;
			this.AllowOnlyOneCopyCheckBox.Location = new System.Drawing.Point(6, 29);
			this.AllowOnlyOneCopyCheckBox.Name = "AllowOnlyOneCopyCheckBox";
			this.AllowOnlyOneCopyCheckBox.Size = new System.Drawing.Size(230, 17);
			this.AllowOnlyOneCopyCheckBox.TabIndex = 1;
			this.AllowOnlyOneCopyCheckBox.Text = "Allow only one copy of Application at a time";
			this.AllowOnlyOneCopyCheckBox.UseVisualStyleBackColor = true;
			// 
			// ProgramScanLocationsTabControl
			// 
			this.ProgramScanLocationsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProgramScanLocationsTabControl.Controls.Add(this.GameScanLocationsTabPage);
			this.ProgramScanLocationsTabControl.Location = new System.Drawing.Point(263, 9);
			this.ProgramScanLocationsTabControl.Name = "ProgramScanLocationsTabControl";
			this.ProgramScanLocationsTabControl.SelectedIndex = 0;
			this.ProgramScanLocationsTabControl.Size = new System.Drawing.Size(378, 223);
			this.ProgramScanLocationsTabControl.TabIndex = 34;
			// 
			// GameScanLocationsTabPage
			// 
			this.GameScanLocationsTabPage.Controls.Add(this.GameScanLocationsListBox);
			this.GameScanLocationsTabPage.Controls.Add(this.LocationsToolStrip);
			this.GameScanLocationsTabPage.Location = new System.Drawing.Point(4, 28);
			this.GameScanLocationsTabPage.Name = "GameScanLocationsTabPage";
			this.GameScanLocationsTabPage.Size = new System.Drawing.Size(370, 191);
			this.GameScanLocationsTabPage.TabIndex = 0;
			this.GameScanLocationsTabPage.Text = "Game Scan Locations";
			this.GameScanLocationsTabPage.UseVisualStyleBackColor = true;
			// 
			// GameScanLocationsListBox
			// 
			this.GameScanLocationsListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GameScanLocationsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GameScanLocationsListBox.FormattingEnabled = true;
			this.GameScanLocationsListBox.Location = new System.Drawing.Point(0, 27);
			this.GameScanLocationsListBox.Name = "GameScanLocationsListBox";
			this.GameScanLocationsListBox.Size = new System.Drawing.Size(370, 164);
			this.GameScanLocationsListBox.Sorted = true;
			this.GameScanLocationsListBox.TabIndex = 0;
			this.GameScanLocationsListBox.SelectedIndexChanged += new System.EventHandler(this.ProgramScanLocationsListBox_SelectedIndexChanged);
			// 
			// LocationsToolStrip
			// 
			this.LocationsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.LocationsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshLocationsButton,
            this.RemoveLocationButton,
            this.AddLocationButton});
			this.LocationsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.LocationsToolStrip.Name = "LocationsToolStrip";
			this.LocationsToolStrip.Size = new System.Drawing.Size(370, 27);
			this.LocationsToolStrip.TabIndex = 2;
			this.LocationsToolStrip.Text = "MySettingsToolStrip";
			// 
			// RefreshLocationsButton
			// 
			this.RefreshLocationsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshLocationsButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshLocationsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshLocationsButton.Name = "RefreshLocationsButton";
			this.RefreshLocationsButton.Size = new System.Drawing.Size(70, 24);
			this.RefreshLocationsButton.Text = "Refresh";
			this.RefreshLocationsButton.Click += new System.EventHandler(this.RefreshLocationsButton_Click);
			// 
			// RemoveLocationButton
			// 
			this.RemoveLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RemoveLocationButton.Image = global::x360ce.App.Properties.Resources.remove_16x16;
			this.RemoveLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveLocationButton.Name = "RemoveLocationButton";
			this.RemoveLocationButton.Size = new System.Drawing.Size(74, 24);
			this.RemoveLocationButton.Text = "Remove";
			this.RemoveLocationButton.Click += new System.EventHandler(this.RemoveLocationButton_Click);
			// 
			// AddLocationButton
			// 
			this.AddLocationButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AddLocationButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddLocationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddLocationButton.Name = "AddLocationButton";
			this.AddLocationButton.Size = new System.Drawing.Size(62, 24);
			this.AddLocationButton.Text = "&Add...";
			this.AddLocationButton.Click += new System.EventHandler(this.AddLocationButton_Click);
			// 
			// ConfigurationGroupBox
			// 
			this.ConfigurationGroupBox.Controls.Add(this.AutoDetectForegroundWindowCheckBox);
			this.ConfigurationGroupBox.Location = new System.Drawing.Point(263, 313);
			this.ConfigurationGroupBox.Name = "ConfigurationGroupBox";
			this.ConfigurationGroupBox.Size = new System.Drawing.Size(277, 47);
			this.ConfigurationGroupBox.TabIndex = 31;
			this.ConfigurationGroupBox.TabStop = false;
			this.ConfigurationGroupBox.Text = "Configuration";
			// 
			// AutoDetectForegroundWindowCheckBox
			// 
			this.AutoDetectForegroundWindowCheckBox.AutoSize = true;
			this.AutoDetectForegroundWindowCheckBox.Location = new System.Drawing.Point(6, 21);
			this.AutoDetectForegroundWindowCheckBox.Name = "AutoDetectForegroundWindowCheckBox";
			this.AutoDetectForegroundWindowCheckBox.Size = new System.Drawing.Size(244, 17);
			this.AutoDetectForegroundWindowCheckBox.TabIndex = 0;
			this.AutoDetectForegroundWindowCheckBox.Text = "Auto switch configuration when game focused";
			this.AutoDetectForegroundWindowCheckBox.UseVisualStyleBackColor = true;
			//
			// DirectInputDevicesGroupBox
			// 
			this.DirectInputDevicesGroupBox.Controls.Add(this.ReadEveryChangeCheckBox);
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeVirtualDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Controls.Add(this.ExcludeSupplementalDevicesCheckBox);
			this.DirectInputDevicesGroupBox.Location = new System.Drawing.Point(3, 241);
			this.DirectInputDevicesGroupBox.Name = "DirectInputDevicesGroupBox";
			this.DirectInputDevicesGroupBox.Size = new System.Drawing.Size(254, 91);
			this.DirectInputDevicesGroupBox.TabIndex = 31;
			this.DirectInputDevicesGroupBox.TabStop = false;
			this.DirectInputDevicesGroupBox.Text = "Direct Input Devices";
			// 
			// ReadEveryChangeCheckBox
			// 
			this.ReadEveryChangeCheckBox.AutoSize = true;
			this.ReadEveryChangeCheckBox.Location = new System.Drawing.Point(6, 65);
			this.ReadEveryChangeCheckBox.Name = "ReadEveryChangeCheckBox";
			this.ReadEveryChangeCheckBox.Size = new System.Drawing.Size(151, 17);
			this.ReadEveryChangeCheckBox.TabIndex = 0;
			this.ReadEveryChangeCheckBox.Text = "Read Every Change";
			this.ReadEveryChangeCheckBox.UseVisualStyleBackColor = true;
			// 
			// ExcludeVirtualDevicesCheckBox
			// 
			this.ExcludeVirtualDevicesCheckBox.AutoSize = true;
			this.ExcludeVirtualDevicesCheckBox.Location = new System.Drawing.Point(6, 42);
			this.ExcludeVirtualDevicesCheckBox.Name = "ExcludeVirtualDevicesCheckBox";
			this.ExcludeVirtualDevicesCheckBox.Size = new System.Drawing.Size(138, 17);
			this.ExcludeVirtualDevicesCheckBox.TabIndex = 0;
			this.ExcludeVirtualDevicesCheckBox.Text = "Exclude Virtual Devices";
			this.ExcludeVirtualDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// ExcludeSupplementalDevicesCheckBox
			// 
			this.ExcludeSupplementalDevicesCheckBox.AutoSize = true;
			this.ExcludeSupplementalDevicesCheckBox.Location = new System.Drawing.Point(6, 19);
			this.ExcludeSupplementalDevicesCheckBox.Name = "ExcludeSupplementalDevicesCheckBox";
			this.ExcludeSupplementalDevicesCheckBox.Size = new System.Drawing.Size(173, 17);
			this.ExcludeSupplementalDevicesCheckBox.TabIndex = 0;
			this.ExcludeSupplementalDevicesCheckBox.Text = "Exclude Supplemental Devices";
			this.ExcludeSupplementalDevicesCheckBox.UseVisualStyleBackColor = true;
			// 
			// VirtualDeviceGroupBox
			// 
			this.VirtualDeviceGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.VirtualDeviceGroupBox.AutoSize = true;
			this.VirtualDeviceGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.VirtualDeviceGroupBox.Controls.Add(this.VirtualDeviceLayoutPanel);
			this.VirtualDeviceGroupBox.Name = "VirtualDeviceGroupBox";
			this.VirtualDeviceGroupBox.TabIndex = 70;
			this.VirtualDeviceGroupBox.TabStop = false;
			this.VirtualDeviceGroupBox.Text = "Virtual Device Emulation by Nefarius";
			//
			// VirtualDeviceLayoutPanel
			//
			this.VirtualDeviceLayoutPanel.AutoSize = true;
			this.VirtualDeviceLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.VirtualDeviceLayoutPanel.ColumnCount = 5;
			this.VirtualDeviceLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.VirtualDeviceLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.VirtualDeviceLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.VirtualDeviceLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.VirtualDeviceLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.VirtualDeviceLayoutPanel.Controls.Add(this.PollingRateLabel, 0, 0);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.PollingRateComboBox, 1, 0);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmBusRefreshButton, 4, 0);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmBusLabel, 0, 1);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmBusTextBox, 1, 1);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmBusInstallButton, 3, 1);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmBusUninstallButton, 4, 1);
			this.VirtualDeviceLayoutPanel.Controls.Add(this.ViGEmLinkPanel, 1, 2);
			this.VirtualDeviceLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.VirtualDeviceLayoutPanel.Name = "VirtualDeviceLayoutPanel";
			this.VirtualDeviceLayoutPanel.RowCount = 3;
			this.VirtualDeviceLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.VirtualDeviceLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.VirtualDeviceLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.VirtualDeviceLayoutPanel.SetColumnSpan(this.ViGEmBusTextBox, 2);
			this.VirtualDeviceLayoutPanel.SetColumnSpan(this.ViGEmLinkPanel, 2);
			this.VirtualDeviceLayoutPanel.TabIndex = 0;
			//
			// ViGEmLinkPanel
			//
			this.ViGEmLinkPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ViGEmLinkPanel.AutoSize = true;
			this.ViGEmLinkPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ViGEmLinkPanel.Controls.Add(this.VigemLabel);
			this.ViGEmLinkPanel.Controls.Add(this.AboutViGEmLinkLabel);
			this.ViGEmLinkPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ViGEmLinkPanel.Name = "ViGEmLinkPanel";
			this.ViGEmLinkPanel.TabIndex = 46;
			this.ViGEmLinkPanel.WrapContents = false;
			//
			// AboutViGEmLinkLabel
			//
			this.AboutViGEmLinkLabel.AutoSize = true;
			this.AboutViGEmLinkLabel.Margin = new System.Windows.Forms.Padding(3);
			this.AboutViGEmLinkLabel.Name = "AboutViGEmLinkLabel";
			this.AboutViGEmLinkLabel.TabIndex = 45;
			this.AboutViGEmLinkLabel.TabStop = true;
			this.AboutViGEmLinkLabel.Text = "https://github.com/nefarius";
			this.AboutViGEmLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AboutViGEmLinkLabel_LinkClicked);
			//
			// ViGEmBusLabel
			//
			this.ViGEmBusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ViGEmBusLabel.AutoSize = true;
			this.ViGEmBusLabel.Name = "ViGEmBusLabel";
			this.ViGEmBusLabel.TabIndex = 29;
			this.ViGEmBusLabel.Text = "ViGEm Bus";
			//
			// VigemLabel
			//
			this.VigemLabel.AutoSize = true;
			this.VigemLabel.Margin = new System.Windows.Forms.Padding(3);
			this.VigemLabel.Name = "VigemLabel";
			this.VigemLabel.TabIndex = 29;
			this.VigemLabel.Text = "Download latest version from";
			//
			// PollingRateLabel
			//
			this.PollingRateLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.PollingRateLabel.AutoSize = true;
			this.PollingRateLabel.Name = "PollingRateLabel";
			this.PollingRateLabel.TabIndex = 29;
			this.PollingRateLabel.Text = "Polling Rate";
			//
			// PollingRateComboBox
			//
			this.PollingRateComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.PollingRateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PollingRateComboBox.FormattingEnabled = true;
			this.PollingRateComboBox.Name = "PollingRateComboBox";
			this.PollingRateComboBox.Size = new System.Drawing.Size(96, 21);
			this.PollingRateComboBox.TabIndex = 30;
			//
			// ViGEmBusRefreshButton
			//
			this.ViGEmBusRefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ViGEmBusRefreshButton.AutoSize = true;
			this.ViGEmBusRefreshButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ViGEmBusRefreshButton.Name = "ViGEmBusRefreshButton";
			this.ViGEmBusRefreshButton.TabIndex = 44;
			this.ViGEmBusRefreshButton.Text = "Refresh";
			this.ViGEmBusRefreshButton.UseVisualStyleBackColor = true;
			this.ViGEmBusRefreshButton.Click += new System.EventHandler(this.ViGEmBusRefreshButton_Click);
			//
			// ViGEmBusUninstallButton
			//
			this.ViGEmBusUninstallButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ViGEmBusUninstallButton.AutoSize = true;
			this.ViGEmBusUninstallButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ViGEmBusUninstallButton.Enabled = false;
			this.ViGEmBusUninstallButton.Name = "ViGEmBusUninstallButton";
			this.ViGEmBusUninstallButton.TabIndex = 44;
			this.ViGEmBusUninstallButton.Text = "Uninstall";
			this.ViGEmBusUninstallButton.UseVisualStyleBackColor = true;
			this.ViGEmBusUninstallButton.Click += new System.EventHandler(this.ViGEmBusUninstallButton_Click);
			//
			// ViGEmBusInstallButton
			//
			this.ViGEmBusInstallButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ViGEmBusInstallButton.AutoSize = true;
			this.ViGEmBusInstallButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ViGEmBusInstallButton.Enabled = false;
			this.ViGEmBusInstallButton.Name = "ViGEmBusInstallButton";
			this.ViGEmBusInstallButton.TabIndex = 44;
			this.ViGEmBusInstallButton.Text = "Install";
			this.ViGEmBusInstallButton.UseVisualStyleBackColor = true;
			this.ViGEmBusInstallButton.Click += new System.EventHandler(this.ViGEmBusInstallButton_Click);
			//
			// ViGEmBusTextBox
			//
			this.ViGEmBusTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ViGEmBusTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ViGEmBusTextBox.Name = "ViGEmBusTextBox";
			this.ViGEmBusTextBox.TabIndex = 41;
			// 
			// DeveloperToolsButton
			// 
			this.DeveloperToolsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DeveloperToolsButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.DeveloperToolsButton.Location = new System.Drawing.Point(3, 389);
			this.DeveloperToolsButton.Name = "DeveloperToolsButton";
			this.DeveloperToolsButton.Size = new System.Drawing.Size(114, 23);
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
			this.AllowRemoteControllersGroupBox.Location = new System.Drawing.Point(3, 89);
			this.AllowRemoteControllersGroupBox.Name = "AllowRemoteControllersGroupBox";
			this.AllowRemoteControllersGroupBox.Size = new System.Drawing.Size(269, 210);
			this.AllowRemoteControllersGroupBox.TabIndex = 31;
			this.AllowRemoteControllersGroupBox.TabStop = false;
			this.AllowRemoteControllersGroupBox.Text = "Allow Remote Controllers";
			this.AllowRemoteControllersGroupBox.Visible = false;
			// 
			// RemoteEnabledCheckBox
			// 
			this.RemoteEnabledCheckBox.AutoSize = true;
			this.RemoteEnabledCheckBox.Location = new System.Drawing.Point(104, 182);
			this.RemoteEnabledCheckBox.Name = "RemoteEnabledCheckBox";
			this.RemoteEnabledCheckBox.Size = new System.Drawing.Size(65, 17);
			this.RemoteEnabledCheckBox.TabIndex = 33;
			this.RemoteEnabledCheckBox.Text = "Enabled";
			this.RemoteEnabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// AllowRemote4CheckBox
			// 
			this.AllowRemote4CheckBox.AutoSize = true;
			this.AllowRemote4CheckBox.Location = new System.Drawing.Point(105, 107);
			this.AllowRemote4CheckBox.Name = "AllowRemote4CheckBox";
			this.AllowRemote4CheckBox.Size = new System.Drawing.Size(79, 17);
			this.AllowRemote4CheckBox.TabIndex = 30;
			this.AllowRemote4CheckBox.Text = "Controller 4";
			this.AllowRemote4CheckBox.UseVisualStyleBackColor = true;
			// 
			// RemotePortLabel
			// 
			this.RemotePortLabel.AutoSize = true;
			this.RemotePortLabel.BackColor = System.Drawing.SystemColors.Control;
			this.RemotePortLabel.Location = new System.Drawing.Point(6, 158);
			this.RemotePortLabel.Name = "RemotePortLabel";
			this.RemotePortLabel.Size = new System.Drawing.Size(86, 13);
			this.RemotePortLabel.TabIndex = 32;
			this.RemotePortLabel.Text = "UDP Server Port";
			// 
			// RemotePasswordLabel
			// 
			this.RemotePasswordLabel.AutoSize = true;
			this.RemotePasswordLabel.BackColor = System.Drawing.SystemColors.Control;
			this.RemotePasswordLabel.Location = new System.Drawing.Point(6, 133);
			this.RemotePasswordLabel.Name = "RemotePasswordLabel";
			this.RemotePasswordLabel.Size = new System.Drawing.Size(93, 13);
			this.RemotePasswordLabel.TabIndex = 32;
			this.RemotePasswordLabel.Text = "Remote Password";
			// 
			// AllowControlLabel
			// 
			this.AllowControlLabel.AutoSize = true;
			this.AllowControlLabel.BackColor = System.Drawing.SystemColors.Control;
			this.AllowControlLabel.Location = new System.Drawing.Point(6, 39);
			this.AllowControlLabel.Name = "AllowControlLabel";
			this.AllowControlLabel.Size = new System.Drawing.Size(68, 13);
			this.AllowControlLabel.TabIndex = 32;
			this.AllowControlLabel.Text = "Allow Control";
			// 
			// AllowRemote3CheckBox
			// 
			this.AllowRemote3CheckBox.AutoSize = true;
			this.AllowRemote3CheckBox.Location = new System.Drawing.Point(105, 84);
			this.AllowRemote3CheckBox.Name = "AllowRemote3CheckBox";
			this.AllowRemote3CheckBox.Size = new System.Drawing.Size(79, 17);
			this.AllowRemote3CheckBox.TabIndex = 30;
			this.AllowRemote3CheckBox.Text = "Controller 3";
			this.AllowRemote3CheckBox.UseVisualStyleBackColor = true;
			// 
			// RemotePortNumericUpDown
			// 
			this.RemotePortNumericUpDown.Location = new System.Drawing.Point(104, 156);
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
			this.RemotePortNumericUpDown.Size = new System.Drawing.Size(59, 20);
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
			this.RemotePasswordTextBox.Location = new System.Drawing.Point(105, 130);
			this.RemotePasswordTextBox.Name = "RemotePasswordTextBox";
			this.RemotePasswordTextBox.Size = new System.Drawing.Size(150, 20);
			this.RemotePasswordTextBox.TabIndex = 0;
			this.RemotePasswordTextBox.UseSystemPasswordChar = true;
			// 
			// AllowRemote2CheckBox
			// 
			this.AllowRemote2CheckBox.AutoSize = true;
			this.AllowRemote2CheckBox.Location = new System.Drawing.Point(105, 61);
			this.AllowRemote2CheckBox.Name = "AllowRemote2CheckBox";
			this.AllowRemote2CheckBox.Size = new System.Drawing.Size(79, 17);
			this.AllowRemote2CheckBox.TabIndex = 30;
			this.AllowRemote2CheckBox.Text = "Controller 2";
			this.AllowRemote2CheckBox.UseVisualStyleBackColor = true;
			// 
			// AllowRemote1CheckBox
			// 
			this.AllowRemote1CheckBox.AutoSize = true;
			this.AllowRemote1CheckBox.Location = new System.Drawing.Point(105, 38);
			this.AllowRemote1CheckBox.Name = "AllowRemote1CheckBox";
			this.AllowRemote1CheckBox.Size = new System.Drawing.Size(79, 17);
			this.AllowRemote1CheckBox.TabIndex = 30;
			this.AllowRemote1CheckBox.Text = "Controller 1";
			this.AllowRemote1CheckBox.UseVisualStyleBackColor = true;
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.GeneralTabPage);
			this.MainTabControl.Controls.Add(this.InternetOptionsTabPage);
			this.MainTabControl.Controls.Add(this.VirtualDeviceTabPage);
			this.MainTabControl.Controls.Add(this.HidHideTabPage);
			this.MainTabControl.Controls.Add(this.HidGuardianTabPage);
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.UpdateTabPage);
			this.MainTabControl.Controls.Add(this.AiTabPage);
			this.MainTabControl.Controls.Add(this.PluginsTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Location = new System.Drawing.Point(3, 3);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(652, 441);
			this.MainTabControl.TabIndex = 71;
			// 
			// GeneralTabPage
			// 
			this.GeneralTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.GeneralTabPage.Controls.Add(this.GeneralPanel);
			this.GeneralTabPage.Location = new System.Drawing.Point(4, 22);
			this.GeneralTabPage.Name = "GeneralTabPage";
			this.GeneralTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.GeneralTabPage.Size = new System.Drawing.Size(650, 421);
			this.GeneralTabPage.TabIndex = 0;
			this.GeneralTabPage.Text = "General";
			// 
			// GeneralPanel
			// 
			this.GeneralPanel.Controls.Add(this.OperationGroupBox);
			this.GeneralPanel.Controls.Add(this.DeveloperToolsButton);
			this.GeneralPanel.Controls.Add(this.TestingGroupBox);
			this.GeneralPanel.Controls.Add(this.DevelopingGroupBox);
			this.GeneralPanel.Controls.Add(this.DirectInputDevicesGroupBox);
			this.GeneralPanel.Controls.Add(this.GuideButtonGroupBox);
			this.GeneralPanel.Controls.Add(this.HotkeysGroupBox);
			this.GeneralPanel.Controls.Add(this.ConfigurationGroupBox);
			this.GeneralPanel.Controls.Add(this.ProgramScanLocationsTabControl);
			this.GeneralPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GeneralPanel.Location = new System.Drawing.Point(3, 3);
			this.GeneralPanel.Name = "GeneralPanel";
			this.GeneralPanel.Size = new System.Drawing.Size(644, 415);
			this.GeneralPanel.TabIndex = 0;
			// 
			// DevelopingGroupBox
			// 
			this.DevelopingGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DevelopingGroupBox.Controls.Add(this.ShowTestButtonCheckBox);
			this.DevelopingGroupBox.Controls.Add(this.ShowFormInfoCheckBox);
			this.DevelopingGroupBox.Location = new System.Drawing.Point(263, 238);
			this.DevelopingGroupBox.Name = "DevelopingGroupBox";
			this.DevelopingGroupBox.Size = new System.Drawing.Size(376, 69);
			this.DevelopingGroupBox.TabIndex = 31;
			this.DevelopingGroupBox.TabStop = false;
			this.DevelopingGroupBox.Text = "Developing";
			// 
			// ShowTestButtonCheckBox
			// 
			this.ShowTestButtonCheckBox.AutoSize = true;
			this.ShowTestButtonCheckBox.Location = new System.Drawing.Point(6, 42);
			this.ShowTestButtonCheckBox.Name = "ShowTestButtonCheckBox";
			this.ShowTestButtonCheckBox.Size = new System.Drawing.Size(126, 17);
			this.ShowTestButtonCheckBox.TabIndex = 1;
			this.ShowTestButtonCheckBox.Text = "Show [Test...] Button";
			this.ShowTestButtonCheckBox.UseVisualStyleBackColor = true;
			// 
			// ShowFormInfoCheckBox
			// 
			this.ShowFormInfoCheckBox.AutoSize = true;
			this.ShowFormInfoCheckBox.Location = new System.Drawing.Point(6, 19);
			this.ShowFormInfoCheckBox.Name = "ShowFormInfoCheckBox";
			this.ShowFormInfoCheckBox.Size = new System.Drawing.Size(213, 17);
			this.ShowFormInfoCheckBox.TabIndex = 1;
			this.ShowFormInfoCheckBox.Text = "Show Form Info on CTRL+SHIFT+RMB";
			this.ShowFormInfoCheckBox.UseVisualStyleBackColor = true;
			this.ShowFormInfoCheckBox.CheckedChanged += new System.EventHandler(this.ShowSettingsTabCheckBox_CheckedChanged);
			// 
			// GuideButtonGroupBox
			// 
			this.GuideButtonGroupBox.Controls.Add(this.GuideButtonActionLabel);
			this.GuideButtonGroupBox.Controls.Add(this.GuideButtonActionTextBox);
			this.GuideButtonGroupBox.Location = new System.Drawing.Point(3, 338);
			this.GuideButtonGroupBox.Name = "GuideButtonGroupBox";
			this.GuideButtonGroupBox.Size = new System.Drawing.Size(254, 47);
			this.GuideButtonGroupBox.TabIndex = 31;
			this.GuideButtonGroupBox.TabStop = false;
			this.GuideButtonGroupBox.Text = "Guide Button";
			// 
			// GuideButtonActionLabel
			// 
			this.GuideButtonActionLabel.AutoSize = true;
			this.GuideButtonActionLabel.Location = new System.Drawing.Point(4, 22);
			this.GuideButtonActionLabel.Name = "GuideButtonActionLabel";
			this.GuideButtonActionLabel.Size = new System.Drawing.Size(40, 13);
			this.GuideButtonActionLabel.TabIndex = 0;
			this.GuideButtonActionLabel.Text = "Action:";
			// 
			// GuideButtonActionTextBox
			// 
			this.GuideButtonActionTextBox.Location = new System.Drawing.Point(50, 19);
			this.GuideButtonActionTextBox.Name = "GuideButtonActionTextBox";
			this.GuideButtonActionTextBox.Size = new System.Drawing.Size(129, 20);
			this.GuideButtonActionTextBox.TabIndex = 0;
			// 
			// HotkeysGroupBox
			// 
			this.HotkeysGroupBox.Controls.Add(this.EmulationHotkeyCheckBox);
			this.HotkeysGroupBox.Controls.Add(this.EmulationHotkeyTextBox);
			this.HotkeysGroupBox.Controls.Add(this.EmulationButtonsCheckBox);
			this.HotkeysGroupBox.Controls.Add(this.EmulationButtonsTextBox);
			this.HotkeysGroupBox.Controls.Add(this.EmulationOverlayCheckBox);
			this.HotkeysGroupBox.Controls.Add(this.DeviceChangeOverlayCheckBox);
			this.HotkeysGroupBox.Location = new System.Drawing.Point(3, 391);
			this.HotkeysGroupBox.Name = "HotkeysGroupBox";
			this.HotkeysGroupBox.Size = new System.Drawing.Size(254, 116);
			this.HotkeysGroupBox.TabIndex = 32;
			this.HotkeysGroupBox.TabStop = false;
			this.HotkeysGroupBox.Text = "Hotkeys";
			// 
			// EmulationHotkeyCheckBox
			// 
			this.EmulationHotkeyCheckBox.AutoSize = true;
			this.EmulationHotkeyCheckBox.Location = new System.Drawing.Point(6, 21);
			this.EmulationHotkeyCheckBox.Name = "EmulationHotkeyCheckBox";
			this.EmulationHotkeyCheckBox.Size = new System.Drawing.Size(76, 17);
			this.EmulationHotkeyCheckBox.TabIndex = 0;
			this.EmulationHotkeyCheckBox.Text = "Emulation:";
			this.EmulationHotkeyCheckBox.UseVisualStyleBackColor = true;
			// 
			// EmulationHotkeyTextBox
			// 
			this.EmulationHotkeyTextBox.Location = new System.Drawing.Point(88, 19);
			this.EmulationHotkeyTextBox.Name = "EmulationHotkeyTextBox";
			this.EmulationHotkeyTextBox.Size = new System.Drawing.Size(160, 20);
			this.EmulationHotkeyTextBox.TabIndex = 1;
			//
			// EmulationButtonsCheckBox
			//
			this.EmulationButtonsCheckBox.AutoSize = true;
			this.EmulationButtonsCheckBox.Location = new System.Drawing.Point(6, 44);
			this.EmulationButtonsCheckBox.Name = "EmulationButtonsCheckBox";
			this.EmulationButtonsCheckBox.Size = new System.Drawing.Size(76, 17);
			this.EmulationButtonsCheckBox.TabIndex = 2;
			this.EmulationButtonsCheckBox.Text = "Controller:";
			this.EmulationButtonsCheckBox.UseVisualStyleBackColor = true;
			//
			// EmulationButtonsTextBox
			//
			this.EmulationButtonsTextBox.Location = new System.Drawing.Point(88, 42);
			this.EmulationButtonsTextBox.Name = "EmulationButtonsTextBox";
			this.EmulationButtonsTextBox.Size = new System.Drawing.Size(160, 20);
			this.EmulationButtonsTextBox.TabIndex = 3;
			//
			// EmulationOverlayCheckBox
			//
			this.EmulationOverlayCheckBox.AutoSize = true;
			this.EmulationOverlayCheckBox.Location = new System.Drawing.Point(6, 67);
			this.EmulationOverlayCheckBox.Name = "EmulationOverlayCheckBox";
			this.EmulationOverlayCheckBox.Size = new System.Drawing.Size(214, 17);
			this.EmulationOverlayCheckBox.TabIndex = 4;
			this.EmulationOverlayCheckBox.Text = "Show a note on the screen when pressed";
			this.EmulationOverlayCheckBox.UseVisualStyleBackColor = true;
			//
			// DeviceChangeOverlayCheckBox
			//
			this.DeviceChangeOverlayCheckBox.AutoSize = true;
			this.DeviceChangeOverlayCheckBox.Location = new System.Drawing.Point(6, 90);
			this.DeviceChangeOverlayCheckBox.Name = "DeviceChangeOverlayCheckBox";
			this.DeviceChangeOverlayCheckBox.Size = new System.Drawing.Size(212, 17);
			this.DeviceChangeOverlayCheckBox.TabIndex = 5;
			this.DeviceChangeOverlayCheckBox.Text = "Show a note when a device disconnects";
			this.DeviceChangeOverlayCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetOptionsTabPage
			// 
			this.InternetOptionsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.InternetOptionsTabPage.Controls.Add(this.InternetPanel);
			this.InternetOptionsTabPage.Location = new System.Drawing.Point(4, 22);
			this.InternetOptionsTabPage.Name = "InternetOptionsTabPage";
			this.InternetOptionsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.InternetOptionsTabPage.Size = new System.Drawing.Size(192, 74);
			this.InternetOptionsTabPage.TabIndex = 1;
			this.InternetOptionsTabPage.Text = "Internet";
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.SettingsTabPage.Controls.Add(this.SettingsPanel);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.SettingsTabPage.Size = new System.Drawing.Size(192, 74);
			this.SettingsTabPage.TabIndex = 5;
			this.SettingsTabPage.Text = "Settings";
			// 
			// SettingsPanel
			// 
			this.SettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsPanel.Location = new System.Drawing.Point(3, 3);
			this.SettingsPanel.Name = "SettingsPanel";
			this.SettingsPanel.Size = new System.Drawing.Size(186, 68);
			this.SettingsPanel.TabIndex = 0;
			// 
			// UpdateTabPage
			// 
			this.UpdateTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.UpdateTabPage.Controls.Add(this.UpdatePanel);
			this.UpdateTabPage.Location = new System.Drawing.Point(4, 22);
			this.UpdateTabPage.Name = "UpdateTabPage";
			this.UpdateTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.UpdateTabPage.Size = new System.Drawing.Size(192, 74);
			this.UpdateTabPage.TabIndex = 6;
			this.UpdateTabPage.Text = "Update";
			// 
			// AiTabPage
			// 
			this.AiTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.AiTabPage.Controls.Add(this.AiControl);
			this.AiTabPage.Location = new System.Drawing.Point(4, 22);
			this.AiTabPage.Name = "AiTabPage";
			this.AiTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.AiTabPage.Size = new System.Drawing.Size(650, 421);
			this.AiTabPage.TabIndex = 7;
			this.AiTabPage.Text = "AI";
			// 
			// AiControl
			// 
			this.AiControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AiControl.Location = new System.Drawing.Point(3, 3);
			this.AiControl.Name = "AiControl";
			this.AiControl.Size = new System.Drawing.Size(644, 415);
			this.AiControl.TabIndex = 0;
			//
			// PluginsTabPage
			//
			this.PluginsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.PluginsTabPage.Controls.Add(this.PluginsPanel);
			this.PluginsTabPage.Location = new System.Drawing.Point(4, 22);
			this.PluginsTabPage.Name = "PluginsTabPage";
			this.PluginsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.PluginsTabPage.Size = new System.Drawing.Size(650, 421);
			this.PluginsTabPage.TabIndex = 8;
			this.PluginsTabPage.Text = "Plugins";
			//
			// PluginsPanel
			//
			this.PluginsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PluginsPanel.Location = new System.Drawing.Point(3, 3);
			this.PluginsPanel.Name = "PluginsPanel";
			this.PluginsPanel.Size = new System.Drawing.Size(644, 415);
			this.PluginsPanel.TabIndex = 0;
			//
			// UpdatePanel
			// 
			this.UpdatePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.UpdatePanel.Location = new System.Drawing.Point(3, 3);
			this.UpdatePanel.Name = "UpdatePanel";
			this.UpdatePanel.Size = new System.Drawing.Size(186, 68);
			this.UpdatePanel.TabIndex = 0;
			// 
			// InternetPanel
			// 
			this.InternetPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.InternetPanel.Location = new System.Drawing.Point(3, 3);
			this.InternetPanel.Name = "InternetPanel";
			this.InternetPanel.Size = new System.Drawing.Size(186, 68);
			this.InternetPanel.TabIndex = 33;
			// 
			// VirtualDeviceTabPage
			// 
			this.VirtualDeviceTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.VirtualDeviceTabPage.Controls.Add(this.VirtualDevicePanel);
			this.VirtualDeviceTabPage.Location = new System.Drawing.Point(4, 22);
			this.VirtualDeviceTabPage.Name = "VirtualDeviceTabPage";
			this.VirtualDeviceTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.VirtualDeviceTabPage.Size = new System.Drawing.Size(192, 74);
			this.VirtualDeviceTabPage.TabIndex = 2;
			this.VirtualDeviceTabPage.Text = "Virtual Device";
			// 
			// VirtualDevicePanel
			// 
			this.VirtualDevicePanel.ColumnCount = 1;
			this.VirtualDevicePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.VirtualDevicePanel.Controls.Add(this.VirtualDeviceGroupBox, 0, 0);
			this.VirtualDevicePanel.Controls.Add(this.AllowRemoteControllersGroupBox, 0, 1);
			this.VirtualDevicePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.VirtualDevicePanel.Margin = new System.Windows.Forms.Padding(0);
			this.VirtualDevicePanel.Name = "VirtualDevicePanel";
			this.VirtualDevicePanel.RowCount = 2;
			this.VirtualDevicePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.VirtualDevicePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.VirtualDevicePanel.TabIndex = 72;
			// 
			// HidHideTabPage
			// 
			this.HidHideTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.HidHideTabPage.Controls.Add(this.HidHidePanel);
			this.HidHideTabPage.Location = new System.Drawing.Point(4, 22);
			this.HidHideTabPage.Name = "HidHideTabPage";
			this.HidHideTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.HidHideTabPage.Size = new System.Drawing.Size(192, 74);
			this.HidHideTabPage.TabIndex = 4;
			this.HidHideTabPage.Text = "HID Hide";
			//
			// HidHidePanel
			//
			this.HidHidePanel.ColumnCount = 1;
			this.HidHidePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidHidePanel.Controls.Add(this.HidHideGroupBox, 0, 0);
			this.HidHidePanel.Controls.Add(this.HidHideInfoLabel, 0, 1);
			this.HidHidePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HidHidePanel.Margin = new System.Windows.Forms.Padding(0);
			this.HidHidePanel.Name = "HidHidePanel";
			this.HidHidePanel.RowCount = 2;
			this.HidHidePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidHidePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidHidePanel.TabIndex = 0;
			//
			// HidHideGroupBox
			//
			this.HidHideGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HidHideGroupBox.AutoSize = true;
			this.HidHideGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideGroupBox.Controls.Add(this.HidHideLayoutPanel);
			this.HidHideGroupBox.Name = "HidHideGroupBox";
			this.HidHideGroupBox.Padding = new System.Windows.Forms.Padding(6, 3, 6, 6);
			this.HidHideGroupBox.TabIndex = 0;
			this.HidHideGroupBox.TabStop = false;
			this.HidHideGroupBox.Text = "HID Hide by Nefarius";
			// 
			// HidHideLayoutPanel
			// 
			this.HidHideLayoutPanel.AutoSize = true;
			this.HidHideLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideLayoutPanel.ColumnCount = 3;
			this.HidHideLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidHideLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidHideLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidHideLayoutPanel.Controls.Add(this.HidHideStatusLabel, 0, 0);
			this.HidHideLayoutPanel.Controls.Add(this.HidHideStatusTextBox, 1, 0);
			this.HidHideLayoutPanel.Controls.Add(this.HidHideRefreshButton, 2, 0);
			this.HidHideLayoutPanel.Controls.Add(this.HidHideButtonsPanel, 1, 1);
			this.HidHideLayoutPanel.Controls.Add(this.HidHideConfigureAutomaticallyCheckBox, 1, 2);
			this.HidHideLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.HidHideLayoutPanel.Name = "HidHideLayoutPanel";
			this.HidHideLayoutPanel.RowCount = 3;
			this.HidHideLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidHideLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidHideLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidHideLayoutPanel.SetColumnSpan(this.HidHideConfigureAutomaticallyCheckBox, 2);
			this.HidHideLayoutPanel.TabIndex = 0;
			//
			// HidHideConfigureAutomaticallyCheckBox
			//
			this.HidHideConfigureAutomaticallyCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidHideConfigureAutomaticallyCheckBox.AutoSize = true;
			this.HidHideConfigureAutomaticallyCheckBox.Enabled = false;
			this.HidHideConfigureAutomaticallyCheckBox.Name = "HidHideConfigureAutomaticallyCheckBox";
			this.HidHideConfigureAutomaticallyCheckBox.TabIndex = 4;
			this.HidHideConfigureAutomaticallyCheckBox.Text = "Configure automatically: hide a device from games while it is mapped to a controller";
			this.HidHideConfigureAutomaticallyCheckBox.UseVisualStyleBackColor = true;
			// 
			// HidHideStatusLabel
			// 
			this.HidHideStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidHideStatusLabel.AutoSize = true;
			this.HidHideStatusLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.HidHideStatusLabel.Name = "HidHideStatusLabel";
			this.HidHideStatusLabel.TabIndex = 0;
			this.HidHideStatusLabel.Text = "HID Hide";
			// 
			// HidHideStatusTextBox
			// 
			this.HidHideStatusTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HidHideStatusTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.HidHideStatusTextBox.Name = "HidHideStatusTextBox";
			this.HidHideStatusTextBox.ReadOnly = true;
			this.HidHideStatusTextBox.TabStop = false;
			this.HidHideStatusTextBox.TabIndex = 1;
			// 
			// HidHideRefreshButton
			// 
			this.HidHideRefreshButton.AutoSize = true;
			this.HidHideRefreshButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideRefreshButton.Name = "HidHideRefreshButton";
			this.HidHideRefreshButton.TabIndex = 2;
			this.HidHideRefreshButton.Text = "Refresh";
			this.HidHideRefreshButton.UseVisualStyleBackColor = true;
			this.HidHideRefreshButton.Click += new System.EventHandler(this.HidHideRefreshButton_Click);
			// 
			// HidHideButtonsPanel
			// 
			this.HidHideButtonsPanel.AutoSize = true;
			this.HidHideButtonsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideButtonsPanel.Controls.Add(this.HidHideDownloadButton);
			this.HidHideButtonsPanel.Controls.Add(this.HidHideConfigureButton);
			this.HidHideButtonsPanel.Margin = new System.Windows.Forms.Padding(0);
			this.HidHideButtonsPanel.Name = "HidHideButtonsPanel";
			this.HidHideButtonsPanel.TabIndex = 3;
			this.HidHideButtonsPanel.WrapContents = false;
			// 
			// HidHideDownloadButton
			// 
			this.HidHideDownloadButton.AutoSize = true;
			this.HidHideDownloadButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideDownloadButton.Name = "HidHideDownloadButton";
			this.HidHideDownloadButton.TabIndex = 0;
			this.HidHideDownloadButton.Text = "Download HID Hide...";
			this.HidHideDownloadButton.UseVisualStyleBackColor = true;
			this.HidHideDownloadButton.Click += new System.EventHandler(this.HidHideDownloadButton_Click);
			// 
			// HidHideConfigureButton
			// 
			this.HidHideConfigureButton.AutoSize = true;
			this.HidHideConfigureButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidHideConfigureButton.Name = "HidHideConfigureButton";
			this.HidHideConfigureButton.TabIndex = 1;
			this.HidHideConfigureButton.Text = "Open Configuration";
			this.HidHideConfigureButton.UseVisualStyleBackColor = true;
			this.HidHideConfigureButton.Click += new System.EventHandler(this.HidHideConfigureButton_Click);
			// 
			// HidHideInfoLabel
			// 
			this.HidHideInfoLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HidHideInfoLabel.Name = "HidHideInfoLabel";
			this.HidHideInfoLabel.TabIndex = 1;
			this.HidHideInfoLabel.Text = "HID Hide stops games from seeing the original controller, so only the emulated one is used. It is installed and updated separately. Choose what it hides in its own program, or with a device's Hide box on the Devices tab.\r\n\r\nAfter installing, open the configuration once: the driver stays inactive until you do.";
			// 
			// HidGuardianTabPage
			// 
			this.HidGuardianTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.HidGuardianTabPage.Controls.Add(this.HidGuardianPanel);
			this.HidGuardianTabPage.Location = new System.Drawing.Point(4, 22);
			this.HidGuardianTabPage.Name = "HidGuardianTabPage";
			this.HidGuardianTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.HidGuardianTabPage.Size = new System.Drawing.Size(192, 74);
			this.HidGuardianTabPage.TabIndex = 3;
			this.HidGuardianTabPage.Text = "HID Guardian (obsolete)";
			// 
			// HidGuardianPanel
			// 
			this.HidGuardianPanel.ColumnCount = 1;
			this.HidGuardianPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidGuardianPanel.Controls.Add(this.groupBox1, 0, 0);
			this.HidGuardianPanel.Controls.Add(this.HelpRichTextBox, 0, 1);
			this.HidGuardianPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HidGuardianPanel.Margin = new System.Windows.Forms.Padding(0);
			this.HidGuardianPanel.Name = "HidGuardianPanel";
			this.HidGuardianPanel.RowCount = 2;
			this.HidGuardianPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidGuardianPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidGuardianPanel.TabIndex = 72;
			//
			// HelpRichTextBox
			//
			this.HelpRichTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.HelpRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.HelpRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HelpRichTextBox.Name = "HelpRichTextBox";
			this.HelpRichTextBox.ReadOnly = true;
			this.HelpRichTextBox.TabIndex = 72;
			this.HelpRichTextBox.Text = "";
			//
			// groupBox1
			//
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.AutoSize = true;
			this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.groupBox1.Controls.Add(this.HidGuardianLayoutPanel);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabIndex = 71;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "HID Guardian by Nefarius - discontinued, replaced by HID Hide";
			//
			// HidGuardianLayoutPanel
			//
			this.HidGuardianLayoutPanel.AutoSize = true;
			this.HidGuardianLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidGuardianLayoutPanel.ColumnCount = 5;
			this.HidGuardianLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidGuardianLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HidGuardianLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidGuardianLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidGuardianLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HidGuardianLayoutPanel.Controls.Add(this.HidGuardianLabel, 0, 0);
			this.HidGuardianLayoutPanel.Controls.Add(this.HidGuardianTextBox, 1, 0);
			this.HidGuardianLayoutPanel.Controls.Add(this.HidGuardianRefreshButton, 2, 0);
			this.HidGuardianLayoutPanel.Controls.Add(this.HidGuardianInstallButton, 3, 0);
			this.HidGuardianLayoutPanel.Controls.Add(this.HidGuardianUninstallButton, 4, 0);
			this.HidGuardianLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.HidGuardianLayoutPanel.Name = "HidGuardianLayoutPanel";
			this.HidGuardianLayoutPanel.RowCount = 1;
			this.HidGuardianLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HidGuardianLayoutPanel.TabIndex = 0;
			//
			// HidGuardianRefreshButton
			//
			this.HidGuardianRefreshButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidGuardianRefreshButton.AutoSize = true;
			this.HidGuardianRefreshButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidGuardianRefreshButton.Name = "HidGuardianRefreshButton";
			this.HidGuardianRefreshButton.TabIndex = 46;
			this.HidGuardianRefreshButton.Text = "Refresh";
			this.HidGuardianRefreshButton.UseVisualStyleBackColor = true;
			this.HidGuardianRefreshButton.Click += new System.EventHandler(this.HidGuardianRefreshButton_Click);
			//
			// HidGuardianLabel
			//
			this.HidGuardianLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidGuardianLabel.AutoSize = true;
			this.HidGuardianLabel.Name = "HidGuardianLabel";
			this.HidGuardianLabel.TabIndex = 29;
			this.HidGuardianLabel.Text = "HID Guardian";
			//
			// HidGuardianUninstallButton
			//
			this.HidGuardianUninstallButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidGuardianUninstallButton.AutoSize = true;
			this.HidGuardianUninstallButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidGuardianUninstallButton.Enabled = false;
			this.HidGuardianUninstallButton.Name = "HidGuardianUninstallButton";
			this.HidGuardianUninstallButton.TabIndex = 44;
			this.HidGuardianUninstallButton.Text = "Uninstall";
			this.HidGuardianUninstallButton.UseVisualStyleBackColor = true;
			this.HidGuardianUninstallButton.Click += new System.EventHandler(this.HidGuardianUninstallButton_Click);
			//
			// HidGuardianInstallButton
			//
			this.HidGuardianInstallButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HidGuardianInstallButton.AutoSize = true;
			this.HidGuardianInstallButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HidGuardianInstallButton.Enabled = false;
			this.HidGuardianInstallButton.Name = "HidGuardianInstallButton";
			this.HidGuardianInstallButton.TabIndex = 44;
			this.HidGuardianInstallButton.Text = "Install";
			this.HidGuardianInstallButton.UseVisualStyleBackColor = true;
			this.HidGuardianInstallButton.Click += new System.EventHandler(this.HidGuardianInstallButton_Click);
			//
			// HidGuardianTextBox
			//
			this.HidGuardianTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HidGuardianTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.HidGuardianTextBox.Name = "HidGuardianTextBox";
			this.HidGuardianTextBox.TabIndex = 41;
			// 
			// OptionsUserControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.MainTabControl);
			this.Name = "OptionsUserControl";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Size = new System.Drawing.Size(658, 447);
			this.TestingGroupBox.ResumeLayout(false);
			this.TestingGroupBox.PerformLayout();
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
			this.ViGEmLinkPanel.ResumeLayout(false);
			this.ViGEmLinkPanel.PerformLayout();
			this.VirtualDeviceLayoutPanel.ResumeLayout(false);
			this.VirtualDeviceLayoutPanel.PerformLayout();
			this.VirtualDeviceGroupBox.ResumeLayout(false);
			this.VirtualDeviceGroupBox.PerformLayout();
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
			this.HotkeysGroupBox.ResumeLayout(false);
			this.HotkeysGroupBox.PerformLayout();
			this.InternetOptionsTabPage.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.UpdateTabPage.ResumeLayout(false);
			this.AiTabPage.ResumeLayout(false);
			this.PluginsTabPage.ResumeLayout(false);
			this.VirtualDeviceTabPage.ResumeLayout(false);
			this.VirtualDevicePanel.ResumeLayout(false);
			this.VirtualDevicePanel.PerformLayout();
			this.HidHideButtonsPanel.ResumeLayout(false);
			this.HidHideButtonsPanel.PerformLayout();
			this.HidHideLayoutPanel.ResumeLayout(false);
			this.HidHideLayoutPanel.PerformLayout();
			this.HidHideGroupBox.ResumeLayout(false);
			this.HidHideGroupBox.PerformLayout();
			this.HidHidePanel.ResumeLayout(false);
			this.HidHidePanel.PerformLayout();
			this.HidHideTabPage.ResumeLayout(false);
			this.HidGuardianTabPage.ResumeLayout(false);
			this.HidGuardianLayoutPanel.ResumeLayout(false);
			this.HidGuardianLayoutPanel.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.HidGuardianPanel.ResumeLayout(false);
			this.HidGuardianPanel.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox TestingGroupBox;
        private System.Windows.Forms.CheckBox XInputEnableCheckBox;
        private System.Windows.Forms.GroupBox OperationGroupBox;
        public System.Windows.Forms.CheckBox AllowOnlyOneCopyCheckBox;
        private System.Windows.Forms.TabControl ProgramScanLocationsTabControl;
		private System.Windows.Forms.TabPage GameScanLocationsTabPage;
		private System.Windows.Forms.FolderBrowserDialog LocationFolderBrowserDialog;
        public System.Windows.Forms.ListBox GameScanLocationsListBox;
        private System.Windows.Forms.GroupBox ConfigurationGroupBox;
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
		private System.Windows.Forms.GroupBox VirtualDeviceGroupBox;
		private System.Windows.Forms.Label PollingRateLabel;
		private System.Windows.Forms.ComboBox PollingRateComboBox;
		private System.Windows.Forms.Button DeveloperToolsButton;
        private System.Windows.Forms.Label ViGEmBusLabel;
        private System.Windows.Forms.TextBox ViGEmBusTextBox;
        public System.Windows.Forms.Button ViGEmBusRefreshButton;
		private System.Windows.Forms.Button ViGEmBusInstallButton;
		internal System.Windows.Forms.CheckBox AlwaysOnTopCheckBox;
		public System.Windows.Forms.ComboBox StartWithWindowsStateComboBox;
		private System.Windows.Forms.Label ThemeLabel;
		private System.Windows.Forms.ComboBox ThemeComboBox;
		public System.Windows.Forms.CheckBox StartWithWindowsCheckBox;
		private System.Windows.Forms.LinkLabel AboutViGEmLinkLabel;
		private System.Windows.Forms.GroupBox AllowRemoteControllersGroupBox;
		public System.Windows.Forms.CheckBox AllowRemote3CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote2CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote1CheckBox;
		public System.Windows.Forms.CheckBox AllowRemote4CheckBox;
		private System.Windows.Forms.TextBox RemotePasswordTextBox;
		private System.Windows.Forms.Label RemotePortLabel;
		public System.Windows.Forms.NumericUpDown RemotePortNumericUpDown;
		private System.Windows.Forms.Label RemotePasswordLabel;
		private System.Windows.Forms.Label VigemLabel;
		private System.Windows.Forms.Button ViGEmBusUninstallButton;
		public System.Windows.Forms.CheckBox RemoteEnabledCheckBox;
		private System.Windows.Forms.Label AllowControlLabel;
		private System.Windows.Forms.TabPage GeneralTabPage;
		private System.Windows.Forms.TabPage InternetOptionsTabPage;
		private System.Windows.Forms.TabPage SettingsTabPage;
		internal OptionsSettingsUserControl SettingsPanel;
		private System.Windows.Forms.TabPage UpdateTabPage;
		private System.Windows.Forms.TabPage AiTabPage;
		private JocysCom.ClassLibrary.Mcp.AiAccessUserControl AiControl;
		private System.Windows.Forms.TabPage PluginsTabPage;
		private x360ce.App.Controls.OptionsPluginsUserControl PluginsPanel;
		public OptionsUpdateUserControl UpdatePanel;
		private System.Windows.Forms.Panel GeneralPanel;
		private System.Windows.Forms.TableLayoutPanel VirtualDevicePanel;
		private System.Windows.Forms.TableLayoutPanel VirtualDeviceLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel ViGEmLinkPanel;
		public OptionsInternetUserControl InternetPanel;
		private System.Windows.Forms.GroupBox DevelopingGroupBox;
		private System.Windows.Forms.CheckBox ShowFormInfoCheckBox;
		private System.Windows.Forms.CheckBox ShowTestButtonCheckBox;
		private System.Windows.Forms.CheckBox ReadEveryChangeCheckBox;
		public System.Windows.Forms.TabControl MainTabControl;
		public System.Windows.Forms.TabPage VirtualDeviceTabPage;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox HidHideConfigureAutomaticallyCheckBox;
		private System.Windows.Forms.Label HidGuardianLabel;
		private System.Windows.Forms.Button HidGuardianUninstallButton;
		private System.Windows.Forms.Button HidGuardianInstallButton;
		private System.Windows.Forms.TextBox HidGuardianTextBox;
		public System.Windows.Forms.TabPage HidGuardianTabPage;
		public System.Windows.Forms.TabPage HidHideTabPage;
		private System.Windows.Forms.TableLayoutPanel HidHidePanel;
		private System.Windows.Forms.GroupBox HidHideGroupBox;
		private System.Windows.Forms.TableLayoutPanel HidHideLayoutPanel;
		private System.Windows.Forms.Label HidHideStatusLabel;
		private System.Windows.Forms.TextBox HidHideStatusTextBox;
		private System.Windows.Forms.Button HidHideRefreshButton;
		private System.Windows.Forms.FlowLayoutPanel HidHideButtonsPanel;
		private System.Windows.Forms.Button HidHideDownloadButton;
		private System.Windows.Forms.Button HidHideConfigureButton;
		private System.Windows.Forms.Label HidHideInfoLabel;
		private System.Windows.Forms.TableLayoutPanel HidGuardianPanel;
		private System.Windows.Forms.TableLayoutPanel HidGuardianLayoutPanel;
		public System.Windows.Forms.Button HidGuardianRefreshButton;
		private System.Windows.Forms.RichTextBox HelpRichTextBox;
		private System.Windows.Forms.GroupBox GuideButtonGroupBox;
		private System.Windows.Forms.Label GuideButtonActionLabel;
		private System.Windows.Forms.TextBox GuideButtonActionTextBox;
		private System.Windows.Forms.GroupBox HotkeysGroupBox;
		private System.Windows.Forms.CheckBox EmulationHotkeyCheckBox;
		private System.Windows.Forms.TextBox EmulationHotkeyTextBox;
		private System.Windows.Forms.CheckBox EmulationButtonsCheckBox;
		private System.Windows.Forms.TextBox EmulationButtonsTextBox;
		private System.Windows.Forms.CheckBox EmulationOverlayCheckBox;
		private System.Windows.Forms.CheckBox DeviceChangeOverlayCheckBox;
		public System.Windows.Forms.CheckBox AutoDetectForegroundWindowCheckBox;
	}
}
