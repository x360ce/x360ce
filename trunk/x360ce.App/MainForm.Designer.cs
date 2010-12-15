using System.Windows.Forms;
using System.Drawing;
using System;
namespace x360ce.App
{
	partial class MainForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.ResetButton = new System.Windows.Forms.Button();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.Pad1TabPage = new System.Windows.Forms.TabPage();
			this.Pad2TabPage = new System.Windows.Forms.TabPage();
			this.Pad3TabPage = new System.Windows.Forms.TabPage();
			this.Pad4TabPage = new System.Windows.Forms.TabPage();
			this.OptionsTabPage = new System.Windows.Forms.TabPage();
			this.TestingAndLoggingGroupBox = new System.Windows.Forms.GroupBox();
			this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
			this.UseInitBeepCheckBox = new System.Windows.Forms.CheckBox();
			this.DebugModeCheckBox = new System.Windows.Forms.CheckBox();
			this.EnableLoggingCheckBox = new System.Windows.Forms.CheckBox();
			this.InstalledFilesGroupBox = new System.Windows.Forms.GroupBox();
			this.InstallFilesX360ceCheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput13CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput12CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput11CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput910CheckBox = new System.Windows.Forms.CheckBox();
			this.FakeApiGroupBox = new System.Windows.Forms.GroupBox();
			this.FakeModeLabel = new System.Windows.Forms.Label();
			this.FakeModeComboBox = new System.Windows.Forms.ComboBox();
			this.HelpTabPage = new System.Windows.Forms.TabPage();
			this.HelpRichTextBox = new System.Windows.Forms.RichTextBox();
			this.AboutTabPage = new System.Windows.Forms.TabPage();
			this.BuletImageList = new System.Windows.Forms.ImageList(this.components);
			this.PresetComboBox = new System.Windows.Forms.ComboBox();
			this.LoadPresetButton = new System.Windows.Forms.Button();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusSaveLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIsAdminLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIniLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusDllLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.CleanStatusTimer = new System.Windows.Forms.Timer(this.components);
			this.SaveButton = new System.Windows.Forms.Button();
			this.SettingsTimer = new System.Windows.Forms.Timer(this.components);
			this.MainTabControl.SuspendLayout();
			this.OptionsTabPage.SuspendLayout();
			this.TestingAndLoggingGroupBox.SuspendLayout();
			this.InstalledFilesGroupBox.SuspendLayout();
			this.FakeApiGroupBox.SuspendLayout();
			this.HelpTabPage.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// UpdateTimer
			// 
			this.UpdateTimer.Interval = 50;
			this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
			// 
			// ResetButton
			// 
			this.ResetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ResetButton.Location = new System.Drawing.Point(496, 482);
			this.ResetButton.Name = "ResetButton";
			this.ResetButton.Size = new System.Drawing.Size(75, 23);
			this.ResetButton.TabIndex = 15;
			this.ResetButton.Text = "Reset";
			this.ResetButton.UseVisualStyleBackColor = true;
			this.ResetButton.Click += new System.EventHandler(this.ResetButton_Click);
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.Pad1TabPage);
			this.MainTabControl.Controls.Add(this.Pad2TabPage);
			this.MainTabControl.Controls.Add(this.Pad3TabPage);
			this.MainTabControl.Controls.Add(this.Pad4TabPage);
			this.MainTabControl.Controls.Add(this.OptionsTabPage);
			this.MainTabControl.Controls.Add(this.HelpTabPage);
			this.MainTabControl.Controls.Add(this.AboutTabPage);
			this.MainTabControl.ImageList = this.BuletImageList;
			this.MainTabControl.Location = new System.Drawing.Point(12, 12);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(640, 468);
			this.MainTabControl.TabIndex = 16;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// 
			// Pad1TabPage
			// 
			this.Pad1TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad1TabPage.Name = "Pad1TabPage";
			this.Pad1TabPage.Size = new System.Drawing.Size(632, 441);
			this.Pad1TabPage.TabIndex = 6;
			this.Pad1TabPage.Text = "Controller 1";
			// 
			// Pad2TabPage
			// 
			this.Pad2TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad2TabPage.Name = "Pad2TabPage";
			this.Pad2TabPage.Size = new System.Drawing.Size(632, 441);
			this.Pad2TabPage.TabIndex = 7;
			this.Pad2TabPage.Text = "Controller 2";
			// 
			// Pad3TabPage
			// 
			this.Pad3TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad3TabPage.Name = "Pad3TabPage";
			this.Pad3TabPage.Size = new System.Drawing.Size(632, 441);
			this.Pad3TabPage.TabIndex = 8;
			this.Pad3TabPage.Text = "Controller 3";
			// 
			// Pad4TabPage
			// 
			this.Pad4TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad4TabPage.Name = "Pad4TabPage";
			this.Pad4TabPage.Size = new System.Drawing.Size(632, 441);
			this.Pad4TabPage.TabIndex = 9;
			this.Pad4TabPage.Text = "Controller 4";
			// 
			// OptionsTabPage
			// 
			this.OptionsTabPage.BackColor = System.Drawing.Color.Transparent;
			this.OptionsTabPage.Controls.Add(this.TestingAndLoggingGroupBox);
			this.OptionsTabPage.Controls.Add(this.InstalledFilesGroupBox);
			this.OptionsTabPage.Controls.Add(this.FakeApiGroupBox);
			this.OptionsTabPage.Location = new System.Drawing.Point(4, 23);
			this.OptionsTabPage.Name = "OptionsTabPage";
			this.OptionsTabPage.Size = new System.Drawing.Size(632, 441);
			this.OptionsTabPage.TabIndex = 5;
			this.OptionsTabPage.Text = "Options";
			// 
			// TestingAndLoggingGroupBox
			// 
			this.TestingAndLoggingGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.TestingAndLoggingGroupBox.Controls.Add(this.XInputEnableCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.UseInitBeepCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.DebugModeCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.EnableLoggingCheckBox);
			this.TestingAndLoggingGroupBox.Location = new System.Drawing.Point(455, 3);
			this.TestingAndLoggingGroupBox.Name = "TestingAndLoggingGroupBox";
			this.TestingAndLoggingGroupBox.Size = new System.Drawing.Size(174, 143);
			this.TestingAndLoggingGroupBox.TabIndex = 31;
			this.TestingAndLoggingGroupBox.TabStop = false;
			this.TestingAndLoggingGroupBox.Text = "Testing and Logging";
			// 
			// XInputEnableCheckBox
			// 
			this.XInputEnableCheckBox.AutoSize = true;
			this.XInputEnableCheckBox.Checked = true;
			this.XInputEnableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.XInputEnableCheckBox.Location = new System.Drawing.Point(6, 27);
			this.XInputEnableCheckBox.Name = "XInputEnableCheckBox";
			this.XInputEnableCheckBox.Size = new System.Drawing.Size(93, 17);
			this.XInputEnableCheckBox.TabIndex = 22;
			this.XInputEnableCheckBox.Text = "Enable XInput";
			this.XInputEnableCheckBox.UseVisualStyleBackColor = true;
			this.XInputEnableCheckBox.CheckedChanged += new System.EventHandler(this.XInputEnableCheckBox_CheckedChanged);
			// 
			// UseInitBeepCheckBox
			// 
			this.UseInitBeepCheckBox.AutoSize = true;
			this.UseInitBeepCheckBox.Location = new System.Drawing.Point(5, 50);
			this.UseInitBeepCheckBox.Name = "UseInitBeepCheckBox";
			this.UseInitBeepCheckBox.Size = new System.Drawing.Size(90, 17);
			this.UseInitBeepCheckBox.TabIndex = 22;
			this.UseInitBeepCheckBox.Text = "Use Init Beep";
			this.UseInitBeepCheckBox.UseVisualStyleBackColor = true;
			// 
			// DebugModeCheckBox
			// 
			this.DebugModeCheckBox.AutoSize = true;
			this.DebugModeCheckBox.Checked = true;
			this.DebugModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DebugModeCheckBox.Location = new System.Drawing.Point(5, 96);
			this.DebugModeCheckBox.Name = "DebugModeCheckBox";
			this.DebugModeCheckBox.Size = new System.Drawing.Size(88, 17);
			this.DebugModeCheckBox.TabIndex = 21;
			this.DebugModeCheckBox.Text = "Debug Mode";
			this.DebugModeCheckBox.UseVisualStyleBackColor = true;
			this.DebugModeCheckBox.CheckedChanged += new System.EventHandler(this.DebugModeCheckBox_CheckedChanged);
			// 
			// EnableLoggingCheckBox
			// 
			this.EnableLoggingCheckBox.AutoSize = true;
			this.EnableLoggingCheckBox.Location = new System.Drawing.Point(5, 73);
			this.EnableLoggingCheckBox.Name = "EnableLoggingCheckBox";
			this.EnableLoggingCheckBox.Size = new System.Drawing.Size(100, 17);
			this.EnableLoggingCheckBox.TabIndex = 21;
			this.EnableLoggingCheckBox.Text = "Enable Logging";
			this.EnableLoggingCheckBox.UseVisualStyleBackColor = true;
			// 
			// InstalledFilesGroupBox
			// 
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesX360ceCheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput13CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput12CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput11CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput910CheckBox);
			this.InstalledFilesGroupBox.Location = new System.Drawing.Point(250, 3);
			this.InstalledFilesGroupBox.Name = "InstalledFilesGroupBox";
			this.InstalledFilesGroupBox.Size = new System.Drawing.Size(199, 143);
			this.InstalledFilesGroupBox.TabIndex = 30;
			this.InstalledFilesGroupBox.TabStop = false;
			this.InstalledFilesGroupBox.Text = "Installed Files";
			// 
			// InstallFilesX360ceCheckBox
			// 
			this.InstallFilesX360ceCheckBox.AutoSize = true;
			this.InstallFilesX360ceCheckBox.Location = new System.Drawing.Point(6, 27);
			this.InstallFilesX360ceCheckBox.Name = "InstallFilesX360ceCheckBox";
			this.InstallFilesX360ceCheckBox.Size = new System.Drawing.Size(74, 17);
			this.InstallFilesX360ceCheckBox.TabIndex = 19;
			this.InstallFilesX360ceCheckBox.Text = "x360ce.ini";
			this.InstallFilesX360ceCheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput13CheckBox
			// 
			this.InstallFilesXinput13CheckBox.AutoSize = true;
			this.InstallFilesXinput13CheckBox.Location = new System.Drawing.Point(6, 50);
			this.InstallFilesXinput13CheckBox.Name = "InstallFilesXinput13CheckBox";
			this.InstallFilesXinput13CheckBox.Size = new System.Drawing.Size(166, 17);
			this.InstallFilesXinput13CheckBox.TabIndex = 19;
			this.InstallFilesXinput13CheckBox.Text = "xinput1_3.dll (Recommended)";
			this.InstallFilesXinput13CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput12CheckBox
			// 
			this.InstallFilesXinput12CheckBox.AutoSize = true;
			this.InstallFilesXinput12CheckBox.Location = new System.Drawing.Point(6, 73);
			this.InstallFilesXinput12CheckBox.Name = "InstallFilesXinput12CheckBox";
			this.InstallFilesXinput12CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput12CheckBox.TabIndex = 19;
			this.InstallFilesXinput12CheckBox.Text = "xinput1_2.dll";
			this.InstallFilesXinput12CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput11CheckBox
			// 
			this.InstallFilesXinput11CheckBox.AutoSize = true;
			this.InstallFilesXinput11CheckBox.Location = new System.Drawing.Point(6, 96);
			this.InstallFilesXinput11CheckBox.Name = "InstallFilesXinput11CheckBox";
			this.InstallFilesXinput11CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput11CheckBox.TabIndex = 19;
			this.InstallFilesXinput11CheckBox.Text = "xinput1_1.dll";
			this.InstallFilesXinput11CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput910CheckBox
			// 
			this.InstallFilesXinput910CheckBox.AutoSize = true;
			this.InstallFilesXinput910CheckBox.Location = new System.Drawing.Point(6, 119);
			this.InstallFilesXinput910CheckBox.Name = "InstallFilesXinput910CheckBox";
			this.InstallFilesXinput910CheckBox.Size = new System.Drawing.Size(97, 17);
			this.InstallFilesXinput910CheckBox.TabIndex = 19;
			this.InstallFilesXinput910CheckBox.Text = "xinput9_1_0.dll";
			this.InstallFilesXinput910CheckBox.UseVisualStyleBackColor = true;
			// 
			// FakeApiGroupBox
			// 
			this.FakeApiGroupBox.Controls.Add(this.FakeModeLabel);
			this.FakeApiGroupBox.Controls.Add(this.FakeModeComboBox);
			this.FakeApiGroupBox.Location = new System.Drawing.Point(3, 3);
			this.FakeApiGroupBox.Name = "FakeApiGroupBox";
			this.FakeApiGroupBox.Size = new System.Drawing.Size(241, 143);
			this.FakeApiGroupBox.TabIndex = 29;
			this.FakeApiGroupBox.TabStop = false;
			this.FakeApiGroupBox.Text = "Fake API";
			// 
			// FakeModeLabel
			// 
			this.FakeModeLabel.AutoSize = true;
			this.FakeModeLabel.Location = new System.Drawing.Point(6, 16);
			this.FakeModeLabel.Name = "FakeModeLabel";
			this.FakeModeLabel.Size = new System.Drawing.Size(64, 13);
			this.FakeModeLabel.TabIndex = 26;
			this.FakeModeLabel.Text = "Hook Mode:";
			// 
			// FakeModeComboBox
			// 
			this.FakeModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.FakeModeComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.FakeModeComboBox.FormattingEnabled = true;
			this.FakeModeComboBox.Location = new System.Drawing.Point(76, 13);
			this.FakeModeComboBox.Name = "FakeModeComboBox";
			this.FakeModeComboBox.Size = new System.Drawing.Size(159, 21);
			this.FakeModeComboBox.TabIndex = 28;
			// 
			// HelpTabPage
			// 
			this.HelpTabPage.Controls.Add(this.HelpRichTextBox);
			this.HelpTabPage.Location = new System.Drawing.Point(4, 23);
			this.HelpTabPage.Name = "HelpTabPage";
			this.HelpTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.HelpTabPage.Size = new System.Drawing.Size(632, 441);
			this.HelpTabPage.TabIndex = 10;
			this.HelpTabPage.Text = "Help";
			// 
			// HelpRichTextBox
			// 
			this.HelpRichTextBox.BackColor = System.Drawing.Color.White;
			this.HelpRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.HelpRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HelpRichTextBox.Location = new System.Drawing.Point(3, 3);
			this.HelpRichTextBox.Name = "HelpRichTextBox";
			this.HelpRichTextBox.ReadOnly = true;
			this.HelpRichTextBox.Size = new System.Drawing.Size(626, 435);
			this.HelpRichTextBox.TabIndex = 0;
			this.HelpRichTextBox.Text = "";
			// 
			// AboutTabPage
			// 
			this.AboutTabPage.BackColor = System.Drawing.Color.Transparent;
			this.AboutTabPage.Location = new System.Drawing.Point(4, 23);
			this.AboutTabPage.Name = "AboutTabPage";
			this.AboutTabPage.Size = new System.Drawing.Size(632, 441);
			this.AboutTabPage.TabIndex = 2;
			this.AboutTabPage.Text = "About";
			// 
			// BuletImageList
			// 
			this.BuletImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.BuletImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.BuletImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// PresetComboBox
			// 
			this.PresetComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.PresetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PresetComboBox.FormattingEnabled = true;
			this.PresetComboBox.Location = new System.Drawing.Point(12, 482);
			this.PresetComboBox.Name = "PresetComboBox";
			this.PresetComboBox.Size = new System.Drawing.Size(397, 21);
			this.PresetComboBox.TabIndex = 27;
			// 
			// LoadPresetButton
			// 
			this.LoadPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.LoadPresetButton.Location = new System.Drawing.Point(415, 482);
			this.LoadPresetButton.Name = "LoadPresetButton";
			this.LoadPresetButton.Size = new System.Drawing.Size(75, 23);
			this.LoadPresetButton.TabIndex = 15;
			this.LoadPresetButton.Text = "Load";
			this.LoadPresetButton.UseVisualStyleBackColor = true;
			this.LoadPresetButton.Click += new System.EventHandler(this.LoadPresetButton_Click);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.StatusEventsLabel,
            this.StatusSaveLabel,
            this.StatusIsAdminLabel,
            this.StatusIniLabel,
            this.StatusDllLabel});
			this.statusStrip1.Location = new System.Drawing.Point(0, 510);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(664, 24);
			this.statusStrip1.SizingGrip = false;
			this.statusStrip1.TabIndex = 17;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 19);
			this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(47, 19);
			this.toolStripStatusLabel2.Spring = true;
			// 
			// StatusEventsLabel
			// 
			this.StatusEventsLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusEventsLabel.Name = "StatusEventsLabel";
			this.StatusEventsLabel.Size = new System.Drawing.Size(105, 19);
			this.StatusEventsLabel.Text = "StatusEventsLabel";
			// 
			// StatusSaveLabel
			// 
			this.StatusSaveLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusSaveLabel.Name = "StatusSaveLabel";
			this.StatusSaveLabel.Size = new System.Drawing.Size(95, 19);
			this.StatusSaveLabel.Text = "StatusSaveLabel";
			// 
			// StatusIsAdminLabel
			// 
			this.StatusIsAdminLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusIsAdminLabel.Name = "StatusIsAdminLabel";
			this.StatusIsAdminLabel.Size = new System.Drawing.Size(115, 19);
			this.StatusIsAdminLabel.Text = "StatusIsAdminLabel";
			// 
			// StatusIniLabel
			// 
			this.StatusIniLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusIniLabel.Name = "StatusIniLabel";
			this.StatusIniLabel.Size = new System.Drawing.Size(84, 19);
			this.StatusIniLabel.Text = "StatusIniLabel";
			// 
			// StatusDllLabel
			// 
			this.StatusDllLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusDllLabel.Name = "StatusDllLabel";
			this.StatusDllLabel.Size = new System.Drawing.Size(85, 19);
			this.StatusDllLabel.Text = "StatusDllLabel";
			// 
			// CleanStatusTimer
			// 
			this.CleanStatusTimer.Interval = 3000;
			this.CleanStatusTimer.Tick += new System.EventHandler(this.CleanStatusTimer_Tick);
			// 
			// SaveButton
			// 
			this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SaveButton.Location = new System.Drawing.Point(577, 482);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(75, 23);
			this.SaveButton.TabIndex = 15;
			this.SaveButton.Text = "Save";
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// SettingsTimer
			// 
			this.SettingsTimer.Interval = 500;
			this.SettingsTimer.Tick += new System.EventHandler(this.SettingsTimer_Tick);
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(664, 534);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.PresetComboBox);
			this.Controls.Add(this.MainTabControl);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.ResetButton);
			this.Controls.Add(this.LoadPresetButton);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MaximumSize = new System.Drawing.Size(680, 572);
			this.MinimumSize = new System.Drawing.Size(680, 572);
			this.Name = "MainForm";
			this.Text = "TocaEdit Xbox 360 Controller Emulator {0}";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.MainTabControl.ResumeLayout(false);
			this.OptionsTabPage.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.PerformLayout();
			this.InstalledFilesGroupBox.ResumeLayout(false);
			this.InstalledFilesGroupBox.PerformLayout();
			this.FakeApiGroupBox.ResumeLayout(false);
			this.FakeApiGroupBox.PerformLayout();
			this.HelpTabPage.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Button ResetButton;
		private TabControl MainTabControl;
		private TabPage AboutTabPage;
		private StatusStrip statusStrip1;
		private TabPage OptionsTabPage;
		private CheckBox EnableLoggingCheckBox;
		private CheckBox UseInitBeepCheckBox;
		private Timer CleanStatusTimer;
		private ComboBox PresetComboBox;
        private Button LoadPresetButton;
		private CheckBox InstallFilesX360ceCheckBox;
		private CheckBox InstallFilesXinput910CheckBox;
		private CheckBox InstallFilesXinput11CheckBox;
		private CheckBox InstallFilesXinput12CheckBox;
		private CheckBox InstallFilesXinput13CheckBox;
		private Button SaveButton;
		private TabPage Pad1TabPage;
		private TabPage Pad2TabPage;
		private TabPage Pad3TabPage;
		private TabPage Pad4TabPage;
		private ImageList BuletImageList;
        public ToolStripStatusLabel toolStripStatusLabel1;
		private Timer SettingsTimer;
		private ToolStripStatusLabel StatusEventsLabel;
		private ToolStripStatusLabel StatusSaveLabel;
		private ToolStripStatusLabel toolStripStatusLabel2;
		private TabPage HelpTabPage;
        private RichTextBox HelpRichTextBox;
		private ToolStripStatusLabel StatusIsAdminLabel;
		private ToolStripStatusLabel StatusIniLabel;
		private ToolStripStatusLabel StatusDllLabel;
        private ComboBox FakeModeComboBox;
        private Label FakeModeLabel;
		private GroupBox FakeApiGroupBox;
        private GroupBox InstalledFilesGroupBox;
		private GroupBox TestingAndLoggingGroupBox;
        private CheckBox DebugModeCheckBox;
		public Timer UpdateTimer;
		private CheckBox XInputEnableCheckBox;

	}
}