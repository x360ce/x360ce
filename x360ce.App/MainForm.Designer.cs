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
            this.GameSettingsTabPage = new System.Windows.Forms.TabPage();
            this.ControllerSettingsTabPage = new System.Windows.Forms.TabPage();
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
            this.HelpBodyLabel = new System.Windows.Forms.Label();
            this.HelpPictureBox = new System.Windows.Forms.PictureBox();
            this.HelpSubjectLabel = new System.Windows.Forms.Label();
            this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
            this.LoadinngCircleTimeout = new System.Windows.Forms.Timer(this.components);
            this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
            this.OptionsPanel = new x360ce.App.Controls.OptionsControl();
            this.programsControl1 = new x360ce.App.Controls.GameSettingsUserControl();
            this.SettingsDatabasePanel = new x360ce.App.Controls.ControllerSettingsUserControl();
            this.MainTabControl.SuspendLayout();
            this.OptionsTabPage.SuspendLayout();
            this.GameSettingsTabPage.SuspendLayout();
            this.ControllerSettingsTabPage.SuspendLayout();
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
            this.MainTabControl.Controls.Add(this.GameSettingsTabPage);
            this.MainTabControl.Controls.Add(this.ControllerSettingsTabPage);
            this.MainTabControl.Controls.Add(this.HelpTabPage);
            this.MainTabControl.Controls.Add(this.AboutTabPage);
            this.MainTabControl.ImageList = this.BuletImageList;
            this.MainTabControl.Location = new System.Drawing.Point(6, 70);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(666, 496);
            this.MainTabControl.TabIndex = 1;
            // 
            // Pad1TabPage
            // 
            this.Pad1TabPage.ImageKey = "bullet_square_glass_grey.png";
            this.Pad1TabPage.Location = new System.Drawing.Point(4, 23);
            this.Pad1TabPage.Name = "Pad1TabPage";
            this.Pad1TabPage.Size = new System.Drawing.Size(658, 469);
            this.Pad1TabPage.TabIndex = 0;
            this.Pad1TabPage.Text = "Controller 1";
            // 
            // Pad2TabPage
            // 
            this.Pad2TabPage.ImageKey = "bullet_square_glass_grey.png";
            this.Pad2TabPage.Location = new System.Drawing.Point(4, 23);
            this.Pad2TabPage.Name = "Pad2TabPage";
            this.Pad2TabPage.Size = new System.Drawing.Size(658, 469);
            this.Pad2TabPage.TabIndex = 0;
            this.Pad2TabPage.Text = "Controller 2";
            // 
            // Pad3TabPage
            // 
            this.Pad3TabPage.ImageKey = "bullet_square_glass_grey.png";
            this.Pad3TabPage.Location = new System.Drawing.Point(4, 23);
            this.Pad3TabPage.Name = "Pad3TabPage";
            this.Pad3TabPage.Size = new System.Drawing.Size(658, 469);
            this.Pad3TabPage.TabIndex = 0;
            this.Pad3TabPage.Text = "Controller 3";
            // 
            // Pad4TabPage
            // 
            this.Pad4TabPage.ImageKey = "bullet_square_glass_grey.png";
            this.Pad4TabPage.Location = new System.Drawing.Point(4, 23);
            this.Pad4TabPage.Name = "Pad4TabPage";
            this.Pad4TabPage.Size = new System.Drawing.Size(658, 469);
            this.Pad4TabPage.TabIndex = 0;
            this.Pad4TabPage.Text = "Controller 4";
            // 
            // OptionsTabPage
            // 
            this.OptionsTabPage.BackColor = System.Drawing.Color.Transparent;
            this.OptionsTabPage.Controls.Add(this.OptionsPanel);
            this.OptionsTabPage.Location = new System.Drawing.Point(4, 23);
            this.OptionsTabPage.Name = "OptionsTabPage";
            this.OptionsTabPage.Size = new System.Drawing.Size(658, 469);
            this.OptionsTabPage.TabIndex = 0;
            this.OptionsTabPage.Text = "Options";
            // 
            // GameSettingsTabPage
            // 
            this.GameSettingsTabPage.BackColor = System.Drawing.SystemColors.Control;
            this.GameSettingsTabPage.Controls.Add(this.programsControl1);
            this.GameSettingsTabPage.Location = new System.Drawing.Point(4, 23);
            this.GameSettingsTabPage.Name = "GameSettingsTabPage";
            this.GameSettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.GameSettingsTabPage.Size = new System.Drawing.Size(658, 469);
            this.GameSettingsTabPage.TabIndex = 2;
            this.GameSettingsTabPage.Text = "Game Settings";
            // 
            // ControllerSettingsTabPage
            // 
            this.ControllerSettingsTabPage.Controls.Add(this.SettingsDatabasePanel);
            this.ControllerSettingsTabPage.Location = new System.Drawing.Point(4, 23);
            this.ControllerSettingsTabPage.Name = "ControllerSettingsTabPage";
            this.ControllerSettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.ControllerSettingsTabPage.Size = new System.Drawing.Size(658, 469);
            this.ControllerSettingsTabPage.TabIndex = 1;
            this.ControllerSettingsTabPage.Text = "Controller Settings";
            // 
            // HelpTabPage
            // 
            this.HelpTabPage.Controls.Add(this.HelpRichTextBox);
            this.HelpTabPage.Location = new System.Drawing.Point(4, 23);
            this.HelpTabPage.Name = "HelpTabPage";
            this.HelpTabPage.Size = new System.Drawing.Size(658, 469);
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
            this.HelpRichTextBox.Size = new System.Drawing.Size(658, 469);
            this.HelpRichTextBox.TabIndex = 0;
            this.HelpRichTextBox.Text = "";
            // 
            // AboutTabPage
            // 
            this.AboutTabPage.BackColor = System.Drawing.Color.Transparent;
            this.AboutTabPage.Location = new System.Drawing.Point(4, 23);
            this.AboutTabPage.Name = "AboutTabPage";
            this.AboutTabPage.Size = new System.Drawing.Size(658, 469);
            this.AboutTabPage.TabIndex = 0;
            this.AboutTabPage.Text = "About";
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
            this.MainStatusStrip.Size = new System.Drawing.Size(676, 24);
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
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(79, 19);
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
            this.TopPanel.Size = new System.Drawing.Size(676, 64);
            this.TopPanel.TabIndex = 3;
            // 
            // HelpBodyLabel
            // 
            this.HelpBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpBodyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.HelpBodyLabel.Location = new System.Drawing.Point(42, 29);
            this.HelpBodyLabel.Name = "HelpBodyLabel";
            this.HelpBodyLabel.Size = new System.Drawing.Size(574, 32);
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
            this.HelpSubjectLabel.Size = new System.Drawing.Size(610, 20);
            this.HelpSubjectLabel.TabIndex = 5;
            this.HelpSubjectLabel.Text = "Controller 1 - General";
            // 
            // HeaderPictureBox
            // 
            this.HeaderPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HeaderPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HeaderPictureBox.Image")));
            this.HeaderPictureBox.Location = new System.Drawing.Point(622, 9);
            this.HeaderPictureBox.Name = "HeaderPictureBox";
            this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
            this.HeaderPictureBox.TabIndex = 6;
            this.HeaderPictureBox.TabStop = false;
            // 
            // LoadinngCircleTimeout
            // 
            this.LoadinngCircleTimeout.Tick += new System.EventHandler(this.LoadinngCircleTimeout_Tick);
            // 
            // BusyLoadingCircle
            // 
            this.BusyLoadingCircle.Active = false;
            this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
            this.BusyLoadingCircle.InnerCircleRadius = 8;
            this.BusyLoadingCircle.Location = new System.Drawing.Point(622, 9);
            this.BusyLoadingCircle.Name = "BusyLoadingCircle";
            this.BusyLoadingCircle.NumberSpoke = 24;
            this.BusyLoadingCircle.OuterCircleRadius = 9;
            this.BusyLoadingCircle.RotationSpeed = 30;
            this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
            this.BusyLoadingCircle.SpokeThickness = 4;
            this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
            this.BusyLoadingCircle.TabIndex = 9;
            // 
            // OptionsPanel
            // 
            this.OptionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OptionsPanel.Location = new System.Drawing.Point(0, 0);
            this.OptionsPanel.Name = "OptionsPanel";
            this.OptionsPanel.Size = new System.Drawing.Size(658, 469);
            this.OptionsPanel.TabIndex = 30;
            // 
            // programsControl1
            // 
            this.programsControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.programsControl1.Location = new System.Drawing.Point(3, 3);
            this.programsControl1.Name = "programsControl1";
            this.programsControl1.Size = new System.Drawing.Size(652, 463);
            this.programsControl1.TabIndex = 1;
            // 
            // SettingsDatabasePanel
            // 
            this.SettingsDatabasePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsDatabasePanel.Location = new System.Drawing.Point(3, 3);
            this.SettingsDatabasePanel.Name = "SettingsDatabasePanel";
            this.SettingsDatabasePanel.Size = new System.Drawing.Size(652, 463);
            this.SettingsDatabasePanel.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(676, 596);
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
            this.GameSettingsTabPage.ResumeLayout(false);
            this.ControllerSettingsTabPage.ResumeLayout(false);
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
        TabPage ControllerSettingsTabPage;
		Panel TopPanel;
		internal Label HelpBodyLabel;
		internal PictureBox HelpPictureBox;
		internal Label HelpSubjectLabel;
		internal PictureBox HeaderPictureBox;
		MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
        Timer LoadinngCircleTimeout;
        public Controls.ControllerSettingsUserControl SettingsDatabasePanel;
        public TabControl MainTabControl;
		TabPage GameSettingsTabPage;
        Controls.GameSettingsUserControl programsControl1;
        public Controls.OptionsControl OptionsPanel;

	}
}