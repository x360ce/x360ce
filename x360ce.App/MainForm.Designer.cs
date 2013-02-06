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
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.Pad1TabPage = new System.Windows.Forms.TabPage();
			this.Pad2TabPage = new System.Windows.Forms.TabPage();
			this.Pad3TabPage = new System.Windows.Forms.TabPage();
			this.Pad4TabPage = new System.Windows.Forms.TabPage();
			this.OptionsTabPage = new System.Windows.Forms.TabPage();
			this.TestingAndLoggingGroupBox = new System.Windows.Forms.GroupBox();
			this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
			this.UseInitBeepCheckBox = new System.Windows.Forms.CheckBox();
			this.ConsoleCheckBox = new System.Windows.Forms.CheckBox();
			this.DebugModeCheckBox = new System.Windows.Forms.CheckBox();
			this.EnableLoggingCheckBox = new System.Windows.Forms.CheckBox();
			this.InstalledFilesGroupBox = new System.Windows.Forms.GroupBox();
			this.InstallFilesX360ceCheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput13CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput12CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput11CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput910CheckBox = new System.Windows.Forms.CheckBox();
			this.OperationGroupBox = new System.Windows.Forms.GroupBox();
			this.AllowOnlyOneCopyCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetGroupBox = new System.Windows.Forms.GroupBox();
			this.InternetAutoloadCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetCheckBox = new System.Windows.Forms.CheckBox();
			this.FakeApiGroupBox = new System.Windows.Forms.GroupBox();
			this.FakeModeLabel = new System.Windows.Forms.Label();
			this.FakeModeComboBox = new System.Windows.Forms.ComboBox();
			this.SettingsDatabaseTabPage = new System.Windows.Forms.TabPage();
			this.onlineUserControl1 = new x360ce.App.Controls.InternetUserControl();
			this.HelpTabPage = new System.Windows.Forms.TabPage();
			this.HelpRichTextBox = new System.Windows.Forms.RichTextBox();
			this.AboutTabPage = new System.Windows.Forms.TabPage();
			this.BuletImageList = new System.Windows.Forms.ImageList(this.components);
			this.MainStatusStrip = new System.Windows.Forms.StatusStrip();
			this.StatusTimerLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusSaveLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIsAdminLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIniLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusDllLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.TopPanel = new System.Windows.Forms.Panel();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.HelpBodyLabel = new System.Windows.Forms.Label();
			this.HelpPictureBox = new System.Windows.Forms.PictureBox();
			this.HelpSubjectLabel = new System.Windows.Forms.Label();
			this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
			this.LoadinngCircleTimeout = new System.Windows.Forms.Timer(this.components);
			this.MainTabControl.SuspendLayout();
			this.OptionsTabPage.SuspendLayout();
			this.TestingAndLoggingGroupBox.SuspendLayout();
			this.InstalledFilesGroupBox.SuspendLayout();
			this.OperationGroupBox.SuspendLayout();
			this.InternetGroupBox.SuspendLayout();
			this.FakeApiGroupBox.SuspendLayout();
			this.SettingsDatabaseTabPage.SuspendLayout();
			this.HelpTabPage.SuspendLayout();
			this.MainStatusStrip.SuspendLayout();
			this.TopPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).BeginInit();
			this.SuspendLayout();
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
			this.MainTabControl.Controls.Add(this.SettingsDatabaseTabPage);
			this.MainTabControl.Controls.Add(this.HelpTabPage);
			this.MainTabControl.Controls.Add(this.AboutTabPage);
			this.MainTabControl.ImageList = this.BuletImageList;
			this.MainTabControl.Location = new System.Drawing.Point(6, 70);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(640, 496);
			this.MainTabControl.TabIndex = 1;
			// 
			// Pad1TabPage
			// 
			this.Pad1TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad1TabPage.Name = "Pad1TabPage";
			this.Pad1TabPage.Size = new System.Drawing.Size(632, 469);
			this.Pad1TabPage.TabIndex = 0;
			this.Pad1TabPage.Text = "Controller 1";
			// 
			// Pad2TabPage
			// 
			this.Pad2TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad2TabPage.Name = "Pad2TabPage";
			this.Pad2TabPage.Size = new System.Drawing.Size(632, 469);
			this.Pad2TabPage.TabIndex = 0;
			this.Pad2TabPage.Text = "Controller 2";
			// 
			// Pad3TabPage
			// 
			this.Pad3TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad3TabPage.Name = "Pad3TabPage";
			this.Pad3TabPage.Size = new System.Drawing.Size(632, 469);
			this.Pad3TabPage.TabIndex = 0;
			this.Pad3TabPage.Text = "Controller 3";
			// 
			// Pad4TabPage
			// 
			this.Pad4TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad4TabPage.Name = "Pad4TabPage";
			this.Pad4TabPage.Size = new System.Drawing.Size(632, 469);
			this.Pad4TabPage.TabIndex = 0;
			this.Pad4TabPage.Text = "Controller 4";
			// 
			// OptionsTabPage
			// 
			this.OptionsTabPage.BackColor = System.Drawing.Color.Transparent;
			this.OptionsTabPage.Controls.Add(this.TestingAndLoggingGroupBox);
			this.OptionsTabPage.Controls.Add(this.InstalledFilesGroupBox);
			this.OptionsTabPage.Controls.Add(this.OperationGroupBox);
			this.OptionsTabPage.Controls.Add(this.InternetGroupBox);
			this.OptionsTabPage.Controls.Add(this.FakeApiGroupBox);
			this.OptionsTabPage.Location = new System.Drawing.Point(4, 23);
			this.OptionsTabPage.Name = "OptionsTabPage";
			this.OptionsTabPage.Size = new System.Drawing.Size(632, 469);
			this.OptionsTabPage.TabIndex = 0;
			this.OptionsTabPage.Text = "Options";
			// 
			// TestingAndLoggingGroupBox
			// 
			this.TestingAndLoggingGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TestingAndLoggingGroupBox.Controls.Add(this.XInputEnableCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.UseInitBeepCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.ConsoleCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.DebugModeCheckBox);
			this.TestingAndLoggingGroupBox.Controls.Add(this.EnableLoggingCheckBox);
			this.TestingAndLoggingGroupBox.Location = new System.Drawing.Point(455, 3);
			this.TestingAndLoggingGroupBox.Name = "TestingAndLoggingGroupBox";
			this.TestingAndLoggingGroupBox.Size = new System.Drawing.Size(174, 143);
			this.TestingAndLoggingGroupBox.TabIndex = 0;
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
			this.XInputEnableCheckBox.TabIndex = 0;
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
			this.UseInitBeepCheckBox.TabIndex = 0;
			this.UseInitBeepCheckBox.Text = "Use Init Beep";
			this.UseInitBeepCheckBox.UseVisualStyleBackColor = true;
			// 
			// ConsoleCheckBox
			// 
			this.ConsoleCheckBox.AutoSize = true;
			this.ConsoleCheckBox.Location = new System.Drawing.Point(5, 96);
			this.ConsoleCheckBox.Name = "ConsoleCheckBox";
			this.ConsoleCheckBox.Size = new System.Drawing.Size(100, 17);
			this.ConsoleCheckBox.TabIndex = 0;
			this.ConsoleCheckBox.Text = "Enable Console";
			this.ConsoleCheckBox.UseVisualStyleBackColor = true;
			this.ConsoleCheckBox.CheckedChanged += new System.EventHandler(this.DebugModeCheckBox_CheckedChanged);
			// 
			// DebugModeCheckBox
			// 
			this.DebugModeCheckBox.AutoSize = true;
			this.DebugModeCheckBox.Checked = true;
			this.DebugModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DebugModeCheckBox.Location = new System.Drawing.Point(5, 119);
			this.DebugModeCheckBox.Name = "DebugModeCheckBox";
			this.DebugModeCheckBox.Size = new System.Drawing.Size(88, 17);
			this.DebugModeCheckBox.TabIndex = 0;
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
			this.EnableLoggingCheckBox.TabIndex = 0;
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
			this.InstalledFilesGroupBox.TabIndex = 0;
			this.InstalledFilesGroupBox.TabStop = false;
			this.InstalledFilesGroupBox.Text = "Installed Files";
			// 
			// InstallFilesX360ceCheckBox
			// 
			this.InstallFilesX360ceCheckBox.AutoSize = true;
			this.InstallFilesX360ceCheckBox.Location = new System.Drawing.Point(6, 27);
			this.InstallFilesX360ceCheckBox.Name = "InstallFilesX360ceCheckBox";
			this.InstallFilesX360ceCheckBox.Size = new System.Drawing.Size(74, 17);
			this.InstallFilesX360ceCheckBox.TabIndex = 0;
			this.InstallFilesX360ceCheckBox.Text = "x360ce.ini";
			this.InstallFilesX360ceCheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput13CheckBox
			// 
			this.InstallFilesXinput13CheckBox.AutoSize = true;
			this.InstallFilesXinput13CheckBox.Location = new System.Drawing.Point(6, 50);
			this.InstallFilesXinput13CheckBox.Name = "InstallFilesXinput13CheckBox";
			this.InstallFilesXinput13CheckBox.Size = new System.Drawing.Size(166, 17);
			this.InstallFilesXinput13CheckBox.TabIndex = 0;
			this.InstallFilesXinput13CheckBox.Text = "xinput1_3.dll (Recommended)";
			this.InstallFilesXinput13CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput12CheckBox
			// 
			this.InstallFilesXinput12CheckBox.AutoSize = true;
			this.InstallFilesXinput12CheckBox.Location = new System.Drawing.Point(6, 73);
			this.InstallFilesXinput12CheckBox.Name = "InstallFilesXinput12CheckBox";
			this.InstallFilesXinput12CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput12CheckBox.TabIndex = 0;
			this.InstallFilesXinput12CheckBox.Text = "xinput1_2.dll";
			this.InstallFilesXinput12CheckBox.UseVisualStyleBackColor = true;
			this.InstallFilesXinput12CheckBox.CheckedChanged += new System.EventHandler(this.InstallFilesXinput12CheckBox_CheckedChanged);
			// 
			// InstallFilesXinput11CheckBox
			// 
			this.InstallFilesXinput11CheckBox.AutoSize = true;
			this.InstallFilesXinput11CheckBox.Location = new System.Drawing.Point(6, 96);
			this.InstallFilesXinput11CheckBox.Name = "InstallFilesXinput11CheckBox";
			this.InstallFilesXinput11CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput11CheckBox.TabIndex = 0;
			this.InstallFilesXinput11CheckBox.Text = "xinput1_1.dll";
			this.InstallFilesXinput11CheckBox.UseVisualStyleBackColor = true;
			this.InstallFilesXinput11CheckBox.CheckedChanged += new System.EventHandler(this.InstallFilesXinput11CheckBox_CheckedChanged);
			// 
			// InstallFilesXinput910CheckBox
			// 
			this.InstallFilesXinput910CheckBox.AutoSize = true;
			this.InstallFilesXinput910CheckBox.Location = new System.Drawing.Point(6, 119);
			this.InstallFilesXinput910CheckBox.Name = "InstallFilesXinput910CheckBox";
			this.InstallFilesXinput910CheckBox.Size = new System.Drawing.Size(97, 17);
			this.InstallFilesXinput910CheckBox.TabIndex = 0;
			this.InstallFilesXinput910CheckBox.Text = "xinput9_1_0.dll";
			this.InstallFilesXinput910CheckBox.UseVisualStyleBackColor = true;
			this.InstallFilesXinput910CheckBox.CheckedChanged += new System.EventHandler(this.InstallFilesXinput910CheckBox_CheckedChanged);
			// 
			// OperationGroupBox
			// 
			this.OperationGroupBox.Controls.Add(this.AllowOnlyOneCopyCheckBox);
			this.OperationGroupBox.Location = new System.Drawing.Point(3, 122);
			this.OperationGroupBox.Name = "OperationGroupBox";
			this.OperationGroupBox.Size = new System.Drawing.Size(241, 47);
			this.OperationGroupBox.TabIndex = 0;
			this.OperationGroupBox.TabStop = false;
			this.OperationGroupBox.Text = "Operation";
			// 
			// AllowOnlyOneCopyCheckBox
			// 
			this.AllowOnlyOneCopyCheckBox.AutoSize = true;
			this.AllowOnlyOneCopyCheckBox.Location = new System.Drawing.Point(9, 19);
			this.AllowOnlyOneCopyCheckBox.Name = "AllowOnlyOneCopyCheckBox";
			this.AllowOnlyOneCopyCheckBox.Size = new System.Drawing.Size(230, 17);
			this.AllowOnlyOneCopyCheckBox.TabIndex = 1;
			this.AllowOnlyOneCopyCheckBox.Text = "Allow only one copy of Application at a time";
			this.AllowOnlyOneCopyCheckBox.UseVisualStyleBackColor = true;
			this.AllowOnlyOneCopyCheckBox.CheckedChanged += new System.EventHandler(this.InternetCheckBox_CheckedChanged);
			// 
			// InternetGroupBox
			// 
			this.InternetGroupBox.Controls.Add(this.InternetAutoloadCheckBox);
			this.InternetGroupBox.Controls.Add(this.InternetCheckBox);
			this.InternetGroupBox.Location = new System.Drawing.Point(3, 53);
			this.InternetGroupBox.Name = "InternetGroupBox";
			this.InternetGroupBox.Size = new System.Drawing.Size(241, 63);
			this.InternetGroupBox.TabIndex = 0;
			this.InternetGroupBox.TabStop = false;
			this.InternetGroupBox.Text = "Internet";
			// 
			// InternetAutoloadCheckBox
			// 
			this.InternetAutoloadCheckBox.AutoSize = true;
			this.InternetAutoloadCheckBox.Location = new System.Drawing.Point(9, 42);
			this.InternetAutoloadCheckBox.Name = "InternetAutoloadCheckBox";
			this.InternetAutoloadCheckBox.Size = new System.Drawing.Size(215, 17);
			this.InternetAutoloadCheckBox.TabIndex = 1;
			this.InternetAutoloadCheckBox.Text = "Auto Load Settings When Tab Selected";
			this.InternetAutoloadCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetCheckBox
			// 
			this.InternetCheckBox.AutoSize = true;
			this.InternetCheckBox.Location = new System.Drawing.Point(9, 19);
			this.InternetCheckBox.Name = "InternetCheckBox";
			this.InternetCheckBox.Size = new System.Drawing.Size(232, 17);
			this.InternetCheckBox.TabIndex = 1;
			this.InternetCheckBox.Text = "Enable Internet Settings Database Features";
			this.InternetCheckBox.UseVisualStyleBackColor = true;
			this.InternetCheckBox.CheckedChanged += new System.EventHandler(this.InternetCheckBox_CheckedChanged);
			// 
			// FakeApiGroupBox
			// 
			this.FakeApiGroupBox.Controls.Add(this.FakeModeLabel);
			this.FakeApiGroupBox.Controls.Add(this.FakeModeComboBox);
			this.FakeApiGroupBox.Location = new System.Drawing.Point(3, 3);
			this.FakeApiGroupBox.Name = "FakeApiGroupBox";
			this.FakeApiGroupBox.Size = new System.Drawing.Size(241, 44);
			this.FakeApiGroupBox.TabIndex = 0;
			this.FakeApiGroupBox.TabStop = false;
			this.FakeApiGroupBox.Text = "InputHook";
			// 
			// FakeModeLabel
			// 
			this.FakeModeLabel.AutoSize = true;
			this.FakeModeLabel.Location = new System.Drawing.Point(6, 16);
			this.FakeModeLabel.Name = "FakeModeLabel";
			this.FakeModeLabel.Size = new System.Drawing.Size(66, 13);
			this.FakeModeLabel.TabIndex = 0;
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
			this.FakeModeComboBox.TabIndex = 0;
			// 
			// SettingsDatabaseTabPage
			// 
			this.SettingsDatabaseTabPage.Controls.Add(this.onlineUserControl1);
			this.SettingsDatabaseTabPage.Location = new System.Drawing.Point(4, 23);
			this.SettingsDatabaseTabPage.Name = "SettingsDatabaseTabPage";
			this.SettingsDatabaseTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.SettingsDatabaseTabPage.Size = new System.Drawing.Size(632, 469);
			this.SettingsDatabaseTabPage.TabIndex = 1;
			this.SettingsDatabaseTabPage.Text = "Settings Database";
			// 
			// onlineUserControl1
			// 
			this.onlineUserControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.onlineUserControl1.Location = new System.Drawing.Point(3, 3);
			this.onlineUserControl1.Name = "onlineUserControl1";
			this.onlineUserControl1.Size = new System.Drawing.Size(626, 463);
			this.onlineUserControl1.TabIndex = 0;
			// 
			// HelpTabPage
			// 
			this.HelpTabPage.Controls.Add(this.HelpRichTextBox);
			this.HelpTabPage.Location = new System.Drawing.Point(4, 23);
			this.HelpTabPage.Name = "HelpTabPage";
			this.HelpTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.HelpTabPage.Size = new System.Drawing.Size(632, 469);
			this.HelpTabPage.TabIndex = 0;
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
			this.HelpRichTextBox.Size = new System.Drawing.Size(626, 463);
			this.HelpRichTextBox.TabIndex = 0;
			this.HelpRichTextBox.Text = "";
			// 
			// AboutTabPage
			// 
			this.AboutTabPage.BackColor = System.Drawing.Color.Transparent;
			this.AboutTabPage.Location = new System.Drawing.Point(4, 23);
			this.AboutTabPage.Name = "AboutTabPage";
			this.AboutTabPage.Size = new System.Drawing.Size(632, 469);
			this.AboutTabPage.TabIndex = 0;
			this.AboutTabPage.Text = "About";
			// 
			// BuletImageList
			// 
			this.BuletImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.BuletImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.BuletImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// MainStatusStrip
			// 
			this.MainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusTimerLabel,
            this.toolStripStatusLabel2,
            this.StatusEventsLabel,
            this.StatusSaveLabel,
            this.StatusIsAdminLabel,
            this.StatusIniLabel,
            this.StatusDllLabel});
			this.MainStatusStrip.Location = new System.Drawing.Point(0, 572);
			this.MainStatusStrip.Name = "MainStatusStrip";
			this.MainStatusStrip.Size = new System.Drawing.Size(650, 24);
			this.MainStatusStrip.SizingGrip = false;
			this.MainStatusStrip.TabIndex = 0;
			this.MainStatusStrip.Text = "statusStrip1";
			// 
			// StatusTimerLabel
			// 
			this.StatusTimerLabel.Name = "StatusTimerLabel";
			this.StatusTimerLabel.Size = new System.Drawing.Size(98, 19);
			this.StatusTimerLabel.Text = "StatusTimerLabel";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(53, 19);
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
			// TopPanel
			// 
			this.TopPanel.BackColor = System.Drawing.SystemColors.Info;
			this.TopPanel.Controls.Add(this.BusyLoadingCircle);
			this.TopPanel.Controls.Add(this.HelpBodyLabel);
			this.TopPanel.Controls.Add(this.HelpPictureBox);
			this.TopPanel.Controls.Add(this.HelpSubjectLabel);
			this.TopPanel.Controls.Add(this.HeaderPictureBox);
			this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.TopPanel.Location = new System.Drawing.Point(0, 0);
			this.TopPanel.Name = "TopPanel";
			this.TopPanel.Size = new System.Drawing.Size(650, 64);
			this.TopPanel.TabIndex = 3;
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(577, 9);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			// 
			// HelpBodyLabel
			// 
			this.HelpBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpBodyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpBodyLabel.Location = new System.Drawing.Point(42, 29);
			this.HelpBodyLabel.Name = "HelpBodyLabel";
			this.HelpBodyLabel.Size = new System.Drawing.Size(548, 32);
			this.HelpBodyLabel.TabIndex = 7;
			this.HelpBodyLabel.Text = resources.GetString("HelpBodyLabel.Text");
			// 
			// HelpPictureBox
			// 
			this.HelpPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HelpPictureBox.Image")));
			this.HelpPictureBox.Location = new System.Drawing.Point(6, 29);
			this.HelpPictureBox.Name = "HelpPictureBox";
			this.HelpPictureBox.Size = new System.Drawing.Size(24, 24);
			this.HelpPictureBox.TabIndex = 8;
			this.HelpPictureBox.TabStop = false;
			// 
			// HelpSubjectLabel
			// 
			this.HelpSubjectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpSubjectLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpSubjectLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HelpSubjectLabel.Location = new System.Drawing.Point(6, 9);
			this.HelpSubjectLabel.Name = "HelpSubjectLabel";
			this.HelpSubjectLabel.Size = new System.Drawing.Size(584, 20);
			this.HelpSubjectLabel.TabIndex = 5;
			this.HelpSubjectLabel.Text = "Controller 1 - General";
			// 
			// HeaderPictureBox
			// 
			this.HeaderPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HeaderPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HeaderPictureBox.Image")));
			this.HeaderPictureBox.Location = new System.Drawing.Point(596, 9);
			this.HeaderPictureBox.Name = "HeaderPictureBox";
			this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
			this.HeaderPictureBox.TabIndex = 6;
			this.HeaderPictureBox.TabStop = false;
			// 
			// LoadinngCircleTimeout
			// 
			this.LoadinngCircleTimeout.Tick += new System.EventHandler(this.LoadinngCircleTimeout_Tick);
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(650, 596);
			this.Controls.Add(this.TopPanel);
			this.Controls.Add(this.MainStatusStrip);
			this.Controls.Add(this.MainTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(666, 634);
			this.Name = "MainForm";
			this.Text = "TocaEdit Xbox 360 Controller Emulator Application";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.MainTabControl.ResumeLayout(false);
			this.OptionsTabPage.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.ResumeLayout(false);
			this.TestingAndLoggingGroupBox.PerformLayout();
			this.InstalledFilesGroupBox.ResumeLayout(false);
			this.InstalledFilesGroupBox.PerformLayout();
			this.OperationGroupBox.ResumeLayout(false);
			this.OperationGroupBox.PerformLayout();
			this.InternetGroupBox.ResumeLayout(false);
			this.InternetGroupBox.PerformLayout();
			this.FakeApiGroupBox.ResumeLayout(false);
			this.FakeApiGroupBox.PerformLayout();
			this.SettingsDatabaseTabPage.ResumeLayout(false);
			this.HelpTabPage.ResumeLayout(false);
			this.MainStatusStrip.ResumeLayout(false);
			this.MainStatusStrip.PerformLayout();
			this.TopPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private TabPage AboutTabPage;
		private StatusStrip MainStatusStrip;
		private TabPage OptionsTabPage;
		private CheckBox EnableLoggingCheckBox;
		private CheckBox UseInitBeepCheckBox;
		private CheckBox InstallFilesX360ceCheckBox;
		private CheckBox InstallFilesXinput910CheckBox;
		private CheckBox InstallFilesXinput11CheckBox;
		private CheckBox InstallFilesXinput12CheckBox;
		private CheckBox InstallFilesXinput13CheckBox;
		private TabPage Pad1TabPage;
		private TabPage Pad2TabPage;
		private TabPage Pad3TabPage;
		private TabPage Pad4TabPage;
		private ImageList BuletImageList;
		public ToolStripStatusLabel StatusTimerLabel;
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
		private CheckBox XInputEnableCheckBox;
		private CheckBox ConsoleCheckBox;
		private TabPage SettingsDatabaseTabPage;
		private GroupBox InternetGroupBox;
		private Panel TopPanel;
		internal Label HelpBodyLabel;
		internal PictureBox HelpPictureBox;
		internal Label HelpSubjectLabel;
		internal PictureBox HeaderPictureBox;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		private Timer LoadinngCircleTimeout;
		public CheckBox InternetCheckBox;
		public Controls.InternetUserControl onlineUserControl1;
		public CheckBox InternetAutoloadCheckBox;
		public TabControl MainTabControl;
		private GroupBox OperationGroupBox;
		public CheckBox AllowOnlyOneCopyCheckBox;

	}
}