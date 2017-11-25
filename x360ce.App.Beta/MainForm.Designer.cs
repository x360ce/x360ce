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
		System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.Pad1TabPage = new System.Windows.Forms.TabPage();
			this.Pad2TabPage = new System.Windows.Forms.TabPage();
			this.Pad3TabPage = new System.Windows.Forms.TabPage();
			this.Pad4TabPage = new System.Windows.Forms.TabPage();
			this.OptionsTabPage = new System.Windows.Forms.TabPage();
			this.OptionsPanel = new x360ce.App.Controls.OptionsUserControl();
			this.GamesTabPage = new System.Windows.Forms.TabPage();
			this.GameSettingsPanel = new x360ce.App.Controls.GamesGridUserControl();
			this.ProgramsTabPage = new System.Windows.Forms.TabPage();
			this.ProgramsPanel = new x360ce.App.Controls.ProgramsGridUserControl();
			this.DevicesTabPage = new System.Windows.Forms.TabPage();
			this.DevicesPanel = new x360ce.App.Controls.UserDevicesUserControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsGridPanel = new x360ce.App.Controls.SettingsGridUserControl();
			this.IniTabPage = new System.Windows.Forms.TabPage();
			this.IniTextBox = new System.Windows.Forms.TextBox();
			this.CloudTabPage = new System.Windows.Forms.TabPage();
			this.CloudPanel = new x360ce.App.Controls.CloudUserControl();
			this.HelpTabPage = new System.Windows.Forms.TabPage();
			this.HelpRichTextBox = new System.Windows.Forms.RichTextBox();
			this.AboutTabPage = new System.Windows.Forms.TabPage();
			this.IssuesTabPage = new System.Windows.Forms.TabPage();
			this.IssuesPanel = new x360ce.App.Controls.IssuesUserControl();
			this.BuletImageList = new System.Windows.Forms.ImageList(this.components);
			this.MainStatusStrip = new System.Windows.Forms.StatusStrip();
			this.StatusTimerLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.UpdateFrequencyLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.UpdateDevicesStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.CloudMessagesLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusSaveLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIsAdminLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIniLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusDllLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.HelpBodyLabel = new System.Windows.Forms.Label();
			this.HelpPictureBox = new System.Windows.Forms.PictureBox();
			this.HelpSubjectLabel = new System.Windows.Forms.Label();
			this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
			this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.TrayNotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.TrayContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.OpenApplicationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
			this.SaveAllButton = new System.Windows.Forms.ToolStripButton();
			this.SaveButton = new System.Windows.Forms.ToolStripButton();
			this.MappedDevicesLabel = new System.Windows.Forms.ToolStripLabel();
			this.GameToCustomizeComboBox = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.VirtualButton = new System.Windows.Forms.ToolStripButton();
			this.LibraryButton = new System.Windows.Forms.ToolStripButton();
			this.MainTabControl.SuspendLayout();
			this.OptionsTabPage.SuspendLayout();
			this.GamesTabPage.SuspendLayout();
			this.ProgramsTabPage.SuspendLayout();
			this.DevicesTabPage.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			this.IniTabPage.SuspendLayout();
			this.CloudTabPage.SuspendLayout();
			this.HelpTabPage.SuspendLayout();
			this.IssuesTabPage.SuspendLayout();
			this.MainStatusStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).BeginInit();
			this.TrayContextMenuStrip.SuspendLayout();
			this.GamesToolStrip.SuspendLayout();
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
			this.MainTabControl.Controls.Add(this.GamesTabPage);
			this.MainTabControl.Controls.Add(this.ProgramsTabPage);
			this.MainTabControl.Controls.Add(this.DevicesTabPage);
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.IniTabPage);
			this.MainTabControl.Controls.Add(this.CloudTabPage);
			this.MainTabControl.Controls.Add(this.HelpTabPage);
			this.MainTabControl.Controls.Add(this.AboutTabPage);
			this.MainTabControl.Controls.Add(this.IssuesTabPage);
			this.MainTabControl.ImageList = this.BuletImageList;
			this.MainTabControl.Location = new System.Drawing.Point(6, 92);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(800, 629);
			this.MainTabControl.TabIndex = 1;
			// 
			// Pad1TabPage
			// 
			this.Pad1TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad1TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad1TabPage.Name = "Pad1TabPage";
			this.Pad1TabPage.Size = new System.Drawing.Size(792, 602);
			this.Pad1TabPage.TabIndex = 0;
			this.Pad1TabPage.Text = "PAD 1";
			// 
			// Pad2TabPage
			// 
			this.Pad2TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad2TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad2TabPage.Name = "Pad2TabPage";
			this.Pad2TabPage.Size = new System.Drawing.Size(792, 602);
			this.Pad2TabPage.TabIndex = 0;
			this.Pad2TabPage.Text = "PAD 2";
			// 
			// Pad3TabPage
			// 
			this.Pad3TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad3TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad3TabPage.Name = "Pad3TabPage";
			this.Pad3TabPage.Size = new System.Drawing.Size(792, 602);
			this.Pad3TabPage.TabIndex = 0;
			this.Pad3TabPage.Text = "PAD 3";
			// 
			// Pad4TabPage
			// 
			this.Pad4TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad4TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad4TabPage.Name = "Pad4TabPage";
			this.Pad4TabPage.Size = new System.Drawing.Size(792, 602);
			this.Pad4TabPage.TabIndex = 0;
			this.Pad4TabPage.Text = "PAD 4";
			// 
			// OptionsTabPage
			// 
			this.OptionsTabPage.BackColor = System.Drawing.Color.Transparent;
			this.OptionsTabPage.Controls.Add(this.OptionsPanel);
			this.OptionsTabPage.Location = new System.Drawing.Point(4, 23);
			this.OptionsTabPage.Name = "OptionsTabPage";
			this.OptionsTabPage.Size = new System.Drawing.Size(792, 602);
			this.OptionsTabPage.TabIndex = 0;
			this.OptionsTabPage.Text = "Options";
			// 
			// OptionsPanel
			// 
			this.OptionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.OptionsPanel.Location = new System.Drawing.Point(0, 0);
			this.OptionsPanel.Name = "OptionsPanel";
			this.OptionsPanel.Size = new System.Drawing.Size(792, 602);
			this.OptionsPanel.TabIndex = 30;
			// 
			// GamesTabPage
			// 
			this.GamesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.GamesTabPage.Controls.Add(this.GameSettingsPanel);
			this.GamesTabPage.Location = new System.Drawing.Point(4, 23);
			this.GamesTabPage.Name = "GamesTabPage";
			this.GamesTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.GamesTabPage.Size = new System.Drawing.Size(792, 602);
			this.GamesTabPage.TabIndex = 2;
			this.GamesTabPage.Text = "Games";
			// 
			// GameSettingsPanel
			// 
			this.GameSettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GameSettingsPanel.Location = new System.Drawing.Point(3, 3);
			this.GameSettingsPanel.Name = "GameSettingsPanel";
			this.GameSettingsPanel.Size = new System.Drawing.Size(786, 596);
			this.GameSettingsPanel.TabIndex = 1;
			// 
			// ProgramsTabPage
			// 
			this.ProgramsTabPage.Controls.Add(this.ProgramsPanel);
			this.ProgramsTabPage.Location = new System.Drawing.Point(4, 23);
			this.ProgramsTabPage.Name = "ProgramsTabPage";
			this.ProgramsTabPage.Size = new System.Drawing.Size(792, 602);
			this.ProgramsTabPage.TabIndex = 7;
			this.ProgramsTabPage.Text = "Programs";
			this.ProgramsTabPage.UseVisualStyleBackColor = true;
			// 
			// ProgramsPanel
			// 
			this.ProgramsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProgramsPanel.Location = new System.Drawing.Point(0, 0);
			this.ProgramsPanel.Name = "ProgramsPanel";
			this.ProgramsPanel.Size = new System.Drawing.Size(792, 602);
			this.ProgramsPanel.TabIndex = 0;
			// 
			// DevicesTabPage
			// 
			this.DevicesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.DevicesTabPage.Controls.Add(this.DevicesPanel);
			this.DevicesTabPage.Location = new System.Drawing.Point(4, 23);
			this.DevicesTabPage.Name = "DevicesTabPage";
			this.DevicesTabPage.Size = new System.Drawing.Size(792, 602);
			this.DevicesTabPage.TabIndex = 4;
			this.DevicesTabPage.Text = "Devices";
			// 
			// DevicesPanel
			// 
			this.DevicesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesPanel.Location = new System.Drawing.Point(0, 0);
			this.DevicesPanel.Name = "DevicesPanel";
			this.DevicesPanel.Size = new System.Drawing.Size(792, 602);
			this.DevicesPanel.TabIndex = 0;
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.Controls.Add(this.SettingsGridPanel);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 23);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Size = new System.Drawing.Size(792, 602);
			this.SettingsTabPage.TabIndex = 1;
			this.SettingsTabPage.Text = "Settings";
			// 
			// SettingsGridPanel
			// 
			this.SettingsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsGridPanel.Location = new System.Drawing.Point(0, 0);
			this.SettingsGridPanel.Name = "SettingsGridPanel";
			this.SettingsGridPanel.Size = new System.Drawing.Size(792, 602);
			this.SettingsGridPanel.TabIndex = 0;
			// 
			// IniTabPage
			// 
			this.IniTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.IniTabPage.Controls.Add(this.IniTextBox);
			this.IniTabPage.Location = new System.Drawing.Point(4, 23);
			this.IniTabPage.Name = "IniTabPage";
			this.IniTabPage.Size = new System.Drawing.Size(792, 602);
			this.IniTabPage.TabIndex = 3;
			this.IniTabPage.Text = "INI";
			// 
			// IniTextBox
			// 
			this.IniTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.IniTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IniTextBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.IniTextBox.Location = new System.Drawing.Point(0, 0);
			this.IniTextBox.Multiline = true;
			this.IniTextBox.Name = "IniTextBox";
			this.IniTextBox.ReadOnly = true;
			this.IniTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.IniTextBox.Size = new System.Drawing.Size(792, 602);
			this.IniTextBox.TabIndex = 1;
			// 
			// CloudTabPage
			// 
			this.CloudTabPage.Controls.Add(this.CloudPanel);
			this.CloudTabPage.Location = new System.Drawing.Point(4, 23);
			this.CloudTabPage.Name = "CloudTabPage";
			this.CloudTabPage.Size = new System.Drawing.Size(792, 602);
			this.CloudTabPage.TabIndex = 6;
			this.CloudTabPage.Text = "Cloud";
			this.CloudTabPage.UseVisualStyleBackColor = true;
			// 
			// CloudPanel
			// 
			this.CloudPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CloudPanel.Location = new System.Drawing.Point(0, 0);
			this.CloudPanel.Name = "CloudPanel";
			this.CloudPanel.Size = new System.Drawing.Size(792, 602);
			this.CloudPanel.TabIndex = 0;
			// 
			// HelpTabPage
			// 
			this.HelpTabPage.Controls.Add(this.HelpRichTextBox);
			this.HelpTabPage.Location = new System.Drawing.Point(4, 23);
			this.HelpTabPage.Name = "HelpTabPage";
			this.HelpTabPage.Size = new System.Drawing.Size(792, 602);
			this.HelpTabPage.TabIndex = 0;
			this.HelpTabPage.Text = "Help";
			// 
			// HelpRichTextBox
			// 
			this.HelpRichTextBox.BackColor = System.Drawing.Color.White;
			this.HelpRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.HelpRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HelpRichTextBox.Location = new System.Drawing.Point(0, 0);
			this.HelpRichTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.HelpRichTextBox.Name = "HelpRichTextBox";
			this.HelpRichTextBox.ReadOnly = true;
			this.HelpRichTextBox.Size = new System.Drawing.Size(792, 602);
			this.HelpRichTextBox.TabIndex = 0;
			this.HelpRichTextBox.Text = "";
			// 
			// AboutTabPage
			// 
			this.AboutTabPage.BackColor = System.Drawing.Color.Transparent;
			this.AboutTabPage.Location = new System.Drawing.Point(4, 23);
			this.AboutTabPage.Name = "AboutTabPage";
			this.AboutTabPage.Size = new System.Drawing.Size(792, 602);
			this.AboutTabPage.TabIndex = 0;
			this.AboutTabPage.Text = "About";
			// 
			// IssuesTabPage
			// 
			this.IssuesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.IssuesTabPage.Controls.Add(this.IssuesPanel);
			this.IssuesTabPage.ImageKey = "ok_off_16x16.png";
			this.IssuesTabPage.Location = new System.Drawing.Point(4, 23);
			this.IssuesTabPage.Name = "IssuesTabPage";
			this.IssuesTabPage.Size = new System.Drawing.Size(792, 602);
			this.IssuesTabPage.TabIndex = 8;
			this.IssuesTabPage.Text = "Issues";
			// 
			// IssuesPanel
			// 
			this.IssuesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IssuesPanel.Location = new System.Drawing.Point(0, 0);
			this.IssuesPanel.Name = "IssuesPanel";
			this.IssuesPanel.Size = new System.Drawing.Size(792, 602);
			this.IssuesPanel.TabIndex = 0;
			// 
			// BuletImageList
			// 
			this.BuletImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("BuletImageList.ImageStream")));
			this.BuletImageList.TransparentColor = System.Drawing.Color.Transparent;
			this.BuletImageList.Images.SetKeyName(0, "bullet_square_glass_blue.png");
			this.BuletImageList.Images.SetKeyName(1, "bullet_square_glass_green.png");
			this.BuletImageList.Images.SetKeyName(2, "bullet_square_glass_grey.png");
			this.BuletImageList.Images.SetKeyName(3, "bullet_square_glass_red.png");
			this.BuletImageList.Images.SetKeyName(4, "bullet_square_glass_yellow.png");
			this.BuletImageList.Images.SetKeyName(5, "ok_16x16.png");
			this.BuletImageList.Images.SetKeyName(6, "ok_off_16x16.png");
			this.BuletImageList.Images.SetKeyName(7, "fix_16x16.png");
			this.BuletImageList.Images.SetKeyName(8, "fix_off_16x16.png");
			// 
			// MainStatusStrip
			// 
			this.MainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusTimerLabel,
            this.toolStripStatusLabel2,
            this.UpdateFrequencyLabel,
            this.UpdateDevicesStatusLabel,
            this.CloudMessagesLabel,
            this.StatusEventsLabel,
            this.StatusSaveLabel,
            this.StatusIsAdminLabel,
            this.StatusIniLabel,
            this.StatusDllLabel});
			this.MainStatusStrip.Location = new System.Drawing.Point(0, 728);
			this.MainStatusStrip.Name = "MainStatusStrip";
			this.MainStatusStrip.Size = new System.Drawing.Size(810, 24);
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
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(119, 19);
			this.toolStripStatusLabel2.Spring = true;
			// 
			// UpdateFrequencyLabel
			// 
			this.UpdateFrequencyLabel.Name = "UpdateFrequencyLabel";
			this.UpdateFrequencyLabel.Size = new System.Drawing.Size(33, 19);
			this.UpdateFrequencyLabel.Text = "Hz: 0";
			// 
			// UpdateDevicesStatusLabel
			// 
			this.UpdateDevicesStatusLabel.Name = "UpdateDevicesStatusLabel";
			this.UpdateDevicesStatusLabel.Size = new System.Drawing.Size(27, 19);
			this.UpdateDevicesStatusLabel.Text = "D: 0";
			// 
			// CloudMessagesLabel
			// 
			this.CloudMessagesLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.CloudMessagesLabel.Name = "CloudMessagesLabel";
			this.CloudMessagesLabel.Size = new System.Drawing.Size(34, 19);
			this.CloudMessagesLabel.Text = "M: 0";
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
			this.StatusIniLabel.DoubleClick += new System.EventHandler(this.StatusIniLabel_DoubleClick);
			// 
			// StatusDllLabel
			// 
			this.StatusDllLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusDllLabel.Name = "StatusDllLabel";
			this.StatusDllLabel.Size = new System.Drawing.Size(85, 19);
			this.StatusDllLabel.Text = "StatusDllLabel";
			// 
			// HelpBodyLabel
			// 
			this.HelpBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpBodyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpBodyLabel.Location = new System.Drawing.Point(42, 29);
			this.HelpBodyLabel.Name = "HelpBodyLabel";
			this.HelpBodyLabel.Size = new System.Drawing.Size(582, 32);
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
			this.HelpSubjectLabel.Size = new System.Drawing.Size(618, 20);
			this.HelpSubjectLabel.TabIndex = 5;
			this.HelpSubjectLabel.Text = "Controller 1 - General";
			// 
			// HeaderPictureBox
			// 
			this.HeaderPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HeaderPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HeaderPictureBox.Image")));
			this.HeaderPictureBox.Location = new System.Drawing.Point(630, 9);
			this.HeaderPictureBox.Name = "HeaderPictureBox";
			this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
			this.HeaderPictureBox.TabIndex = 6;
			this.HeaderPictureBox.TabStop = false;
			// 
			// TrayNotifyIcon
			// 
			this.TrayNotifyIcon.ContextMenuStrip = this.TrayContextMenuStrip;
			this.TrayNotifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("TrayNotifyIcon.Icon")));
			this.TrayNotifyIcon.Visible = true;
			// 
			// TrayContextMenuStrip
			// 
			this.TrayContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenApplicationToolStripMenuItem,
            this.ExitToolStripMenuItem});
			this.TrayContextMenuStrip.Name = "TrayContextMenuStrip";
			this.TrayContextMenuStrip.Size = new System.Drawing.Size(170, 48);
			// 
			// OpenApplicationToolStripMenuItem
			// 
			this.OpenApplicationToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OpenApplicationToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.app_16x16;
			this.OpenApplicationToolStripMenuItem.Name = "OpenApplicationToolStripMenuItem";
			this.OpenApplicationToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
			this.OpenApplicationToolStripMenuItem.Text = "Open Application";
			this.OpenApplicationToolStripMenuItem.Click += new System.EventHandler(this.OpenApplicationToolStripMenuItem_Click);
			// 
			// ExitToolStripMenuItem
			// 
			this.ExitToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.exit_16x16;
			this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
			this.ExitToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
			this.ExitToolStripMenuItem.Text = "Exit";
			this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(630, 9);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			// 
			// GamesToolStrip
			// 
			this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveAllButton,
            this.SaveButton,
            this.MappedDevicesLabel,
            this.GameToCustomizeComboBox,
            this.toolStripLabel1,
            this.VirtualButton,
            this.LibraryButton});
			this.GamesToolStrip.Location = new System.Drawing.Point(0, 64);
			this.GamesToolStrip.Name = "GamesToolStrip";
			this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 2, 4, 0);
			this.GamesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.GamesToolStrip.Size = new System.Drawing.Size(810, 26);
			this.GamesToolStrip.TabIndex = 25;
			this.GamesToolStrip.Text = "MySettingsToolStrip";
			// 
			// SaveAllButton
			// 
			this.SaveAllButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SaveAllButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.SaveAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveAllButton.Margin = new System.Windows.Forms.Padding(1);
			this.SaveAllButton.Name = "SaveAllButton";
			this.SaveAllButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.SaveAllButton.Size = new System.Drawing.Size(68, 22);
			this.SaveAllButton.Text = "Save All";
			this.SaveAllButton.Click += new System.EventHandler(this.SaveAllButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SaveButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.SaveButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.SaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveButton.Margin = new System.Windows.Forms.Padding(1);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.SaveButton.Size = new System.Drawing.Size(51, 22);
			this.SaveButton.Text = "Save";
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// MappedDevicesLabel
			// 
			this.MappedDevicesLabel.Name = "MappedDevicesLabel";
			this.MappedDevicesLabel.Size = new System.Drawing.Size(41, 21);
			this.MappedDevicesLabel.Text = "Game:";
			// 
			// GameToCustomizeComboBox
			// 
			this.GameToCustomizeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.GameToCustomizeComboBox.Name = "GameToCustomizeComboBox";
			this.GameToCustomizeComboBox.Size = new System.Drawing.Size(360, 24);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(41, 21);
			this.toolStripLabel1.Text = "Mode:";
			// 
			// VirtualButton
			// 
			this.VirtualButton.Image = global::x360ce.App.Properties.Resources.emulation_virtual_16x16;
			this.VirtualButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.VirtualButton.Margin = new System.Windows.Forms.Padding(1);
			this.VirtualButton.Name = "VirtualButton";
			this.VirtualButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.VirtualButton.Size = new System.Drawing.Size(61, 22);
			this.VirtualButton.Text = "Virtual";
			this.VirtualButton.Click += new System.EventHandler(this.VirtualButton_Click);
			// 
			// LibraryButton
			// 
			this.LibraryButton.Image = global::x360ce.App.Properties.Resources.emulation_library_16x16;
			this.LibraryButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.LibraryButton.Margin = new System.Windows.Forms.Padding(1);
			this.LibraryButton.Name = "LibraryButton";
			this.LibraryButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.LibraryButton.Size = new System.Drawing.Size(63, 22);
			this.LibraryButton.Text = "Library";
			this.LibraryButton.Click += new System.EventHandler(this.LibraryButton_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(810, 752);
			this.Controls.Add(this.GamesToolStrip);
			this.Controls.Add(this.MainStatusStrip);
			this.Controls.Add(this.MainTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(700, 790);
			this.Name = "MainForm";
			this.Text = "TocaEdit Xbox 360 Controller Emulator Application";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.Controls.SetChildIndex(this.MainTabControl, 0);
			this.Controls.SetChildIndex(this.MainStatusStrip, 0);
			this.Controls.SetChildIndex(this.GamesToolStrip, 0);
			this.MainTabControl.ResumeLayout(false);
			this.OptionsTabPage.ResumeLayout(false);
			this.GamesTabPage.ResumeLayout(false);
			this.ProgramsTabPage.ResumeLayout(false);
			this.DevicesTabPage.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.IniTabPage.ResumeLayout(false);
			this.IniTabPage.PerformLayout();
			this.CloudTabPage.ResumeLayout(false);
			this.HelpTabPage.ResumeLayout(false);
			this.IssuesTabPage.ResumeLayout(false);
			this.MainStatusStrip.ResumeLayout(false);
			this.MainStatusStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).EndInit();
			this.TrayContextMenuStrip.ResumeLayout(false);
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		TabPage AboutTabPage;
		StatusStrip MainStatusStrip;
        TabPage OptionsTabPage;
		TabPage Pad1TabPage;
		TabPage Pad2TabPage;
		TabPage Pad3TabPage;
		TabPage Pad4TabPage;
		ImageList BuletImageList;
		public ToolStripStatusLabel StatusTimerLabel;
		ToolStripStatusLabel StatusEventsLabel;
		ToolStripStatusLabel StatusSaveLabel;
		ToolStripStatusLabel toolStripStatusLabel2;
		TabPage HelpTabPage;
        RichTextBox HelpRichTextBox;
		ToolStripStatusLabel StatusIsAdminLabel;
		ToolStripStatusLabel StatusIniLabel;
        ToolStripStatusLabel StatusDllLabel;
        TabPage SettingsTabPage;
        public TabControl MainTabControl;
        public Controls.OptionsUserControl OptionsPanel;
        public ToolTip ToolTip;
		public Controls.GamesGridUserControl GameSettingsPanel;
		private NotifyIcon TrayNotifyIcon;
		private ContextMenuStrip TrayContextMenuStrip;
		private ToolStripMenuItem OpenApplicationToolStripMenuItem;
		private ToolStripMenuItem ExitToolStripMenuItem;
		internal Label HelpBodyLabel;
		internal PictureBox HelpPictureBox;
		internal Label HelpSubjectLabel;
		internal PictureBox HeaderPictureBox;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		private TabPage IniTabPage;
		private TextBox IniTextBox;
		public ToolStripStatusLabel CloudMessagesLabel;
		private TabPage CloudTabPage;
		public Controls.CloudUserControl CloudPanel;
		private ToolStripStatusLabel UpdateDevicesStatusLabel;
		private Controls.SettingsGridUserControl SettingsGridPanel;
		private Controls.ProgramsGridUserControl ProgramsPanel;
		public TabPage ProgramsTabPage;
		public TabPage GamesTabPage;
		private TabPage DevicesTabPage;
		public Controls.UserDevicesUserControl DevicesPanel;
		private ToolStripStatusLabel UpdateFrequencyLabel;
		private ToolStrip GamesToolStrip;
		private ToolStripButton SaveAllButton;
		private ToolStripButton SaveButton;
		private ToolStripLabel MappedDevicesLabel;
		private ToolStripComboBox GameToCustomizeComboBox;
		private ToolStripButton VirtualButton;
		private ToolStripButton LibraryButton;
		private ToolStripLabel toolStripLabel1;
		private TabPage IssuesTabPage;
		private Controls.IssuesUserControl IssuesPanel;
	}
}
