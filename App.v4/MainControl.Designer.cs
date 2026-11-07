using System.Windows.Forms;
using System.Drawing;
using System;
namespace x360ce.App
{
	partial class MainControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

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
		void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
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
			this.XInputDevicesPanel = new x360ce.App.Controls.XInputDevicesUserControl();
			this.XInputDevicesHeaderLabel = new System.Windows.Forms.Label();
			this.DirectInputDevicesHeaderLabel = new System.Windows.Forms.Label();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsGridPanel = new x360ce.App.Controls.SettingsGridUserControl();
			this.CloudTabPage = new System.Windows.Forms.TabPage();
			this.CloudPanel = new x360ce.App.Controls.CloudUserControl();
			this.HelpTabPage = new System.Windows.Forms.TabPage();
			this.HelpRichTextBox = new System.Windows.Forms.RichTextBox();
			this.AboutTabPage = new System.Windows.Forms.TabPage();
			this.IssuesTabPage = new System.Windows.Forms.TabPage();
			this.IssuesPanel = new JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl();
			this.RecorderPanel = new x360ce.App.Controls.RecorderUserControl();
			this.BuletImageList = new System.Windows.Forms.ImageList(this.components);
			this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
			this.SaveAllButton = new System.Windows.Forms.ToolStripButton();
			this.TestButton = new System.Windows.Forms.ToolStripButton();
			this.MappedDevicesLabel = new System.Windows.Forms.ToolStripLabel();
			this.GameToCustomizeComboBox = new System.Windows.Forms.ToolStripComboBox();
			this.AddGameButton = new System.Windows.Forms.ToolStripButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.MainTabControl.SuspendLayout();
			this.OptionsTabPage.SuspendLayout();
			this.GamesTabPage.SuspendLayout();
			this.ProgramsTabPage.SuspendLayout();
			this.DevicesTabPage.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			this.CloudTabPage.SuspendLayout();
			this.HelpTabPage.SuspendLayout();
			this.IssuesTabPage.SuspendLayout();
			this.GamesToolStrip.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.Pad1TabPage);
			this.MainTabControl.Controls.Add(this.Pad2TabPage);
			this.MainTabControl.Controls.Add(this.Pad3TabPage);
			this.MainTabControl.Controls.Add(this.Pad4TabPage);
			this.MainTabControl.Controls.Add(this.OptionsTabPage);
			this.MainTabControl.Controls.Add(this.GamesTabPage);
			this.MainTabControl.Controls.Add(this.ProgramsTabPage);
			this.MainTabControl.Controls.Add(this.DevicesTabPage);
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.CloudTabPage);
			this.MainTabControl.Controls.Add(this.HelpTabPage);
			this.MainTabControl.Controls.Add(this.AboutTabPage);
			this.MainTabControl.Controls.Add(this.IssuesTabPage);
			this.MainTabControl.AccessibleName = "Main";
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.ImageList = this.BuletImageList;
			this.MainTabControl.Location = new System.Drawing.Point(3, 3);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.ShowToolTips = true;
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(798, 620);
			this.MainTabControl.TabIndex = 1;
			// 
			// Pad1TabPage
			// 
			this.Pad1TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad1TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad1TabPage.Name = "Pad1TabPage";
			this.Pad1TabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.Pad1TabPage.Size = new System.Drawing.Size(790, 593);
			this.Pad1TabPage.TabIndex = 0;
			this.Pad1TabPage.Text = "PAD 1";
			// 
			// Pad2TabPage
			// 
			this.Pad2TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad2TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad2TabPage.Name = "Pad2TabPage";
			this.Pad2TabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.Pad2TabPage.Size = new System.Drawing.Size(790, 593);
			this.Pad2TabPage.TabIndex = 0;
			this.Pad2TabPage.Text = "PAD 2";
			// 
			// Pad3TabPage
			// 
			this.Pad3TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad3TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad3TabPage.Name = "Pad3TabPage";
			this.Pad3TabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.Pad3TabPage.Size = new System.Drawing.Size(790, 593);
			this.Pad3TabPage.TabIndex = 0;
			this.Pad3TabPage.Text = "PAD 3";
			// 
			// Pad4TabPage
			// 
			this.Pad4TabPage.ImageKey = "bullet_square_glass_grey.png";
			this.Pad4TabPage.Location = new System.Drawing.Point(4, 23);
			this.Pad4TabPage.Name = "Pad4TabPage";
			this.Pad4TabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.Pad4TabPage.Size = new System.Drawing.Size(790, 593);
			this.Pad4TabPage.TabIndex = 0;
			this.Pad4TabPage.Text = "PAD 4";
			// 
			// OptionsTabPage
			// 
			this.OptionsTabPage.BackColor = System.Drawing.Color.Transparent;
			this.OptionsTabPage.Controls.Add(this.OptionsPanel);
			this.OptionsTabPage.Location = new System.Drawing.Point(4, 23);
			this.OptionsTabPage.Name = "OptionsTabPage";
			this.OptionsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.OptionsTabPage.Size = new System.Drawing.Size(790, 593);
			this.OptionsTabPage.TabIndex = 0;
			this.OptionsTabPage.Text = "Options";
			// 
			// OptionsPanel
			// 
			this.OptionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.OptionsPanel.Location = new System.Drawing.Point(3, 3);
			this.OptionsPanel.Name = "OptionsPanel";
			this.OptionsPanel.Size = new System.Drawing.Size(784, 587);
			this.OptionsPanel.TabIndex = 30;
			// 
			// GamesTabPage
			// 
			this.GamesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.GamesTabPage.Controls.Add(this.GameSettingsPanel);
			this.GamesTabPage.Location = new System.Drawing.Point(4, 23);
			this.GamesTabPage.Name = "GamesTabPage";
			this.GamesTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.GamesTabPage.Size = new System.Drawing.Size(790, 593);
			this.GamesTabPage.TabIndex = 2;
			this.GamesTabPage.Text = "Games";
			// 
			// GameSettingsPanel
			// 
			this.GameSettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GameSettingsPanel.Location = new System.Drawing.Point(3, 3);
			this.GameSettingsPanel.Name = "GameSettingsPanel";
			this.GameSettingsPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.GameSettingsPanel.Size = new System.Drawing.Size(784, 587);
			this.GameSettingsPanel.TabIndex = 1;
			// 
			// ProgramsTabPage
			// 
			this.ProgramsTabPage.Controls.Add(this.ProgramsPanel);
			this.ProgramsTabPage.Location = new System.Drawing.Point(4, 23);
			this.ProgramsTabPage.Name = "ProgramsTabPage";
			this.ProgramsTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.ProgramsTabPage.Size = new System.Drawing.Size(790, 593);
			this.ProgramsTabPage.TabIndex = 7;
			this.ProgramsTabPage.Text = "Programs";
			this.ProgramsTabPage.UseVisualStyleBackColor = true;
			// 
			// ProgramsPanel
			// 
			this.ProgramsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProgramsPanel.Location = new System.Drawing.Point(3, 3);
			this.ProgramsPanel.Name = "ProgramsPanel";
			this.ProgramsPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.ProgramsPanel.Size = new System.Drawing.Size(784, 587);
			this.ProgramsPanel.TabIndex = 0;
			// 
			// XInputDevicesHeaderLabel
			// 
			this.XInputDevicesHeaderLabel.AutoSize = true;
			this.XInputDevicesHeaderLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this.XInputDevicesHeaderLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
			this.XInputDevicesHeaderLabel.Name = "XInputDevicesHeaderLabel";
			this.XInputDevicesHeaderLabel.Padding = new System.Windows.Forms.Padding(7, 6, 7, 2);
			this.XInputDevicesHeaderLabel.Size = new System.Drawing.Size(784, 21);
			this.XInputDevicesHeaderLabel.Text = "XInput Devices - what holds each of the four places a game can read";
			// 
			// DirectInputDevicesHeaderLabel
			// 
			this.DirectInputDevicesHeaderLabel.AutoSize = true;
			this.DirectInputDevicesHeaderLabel.Dock = System.Windows.Forms.DockStyle.Top;
			this.DirectInputDevicesHeaderLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
			this.DirectInputDevicesHeaderLabel.Name = "DirectInputDevicesHeaderLabel";
			this.DirectInputDevicesHeaderLabel.Padding = new System.Windows.Forms.Padding(7, 6, 7, 2);
			this.DirectInputDevicesHeaderLabel.Size = new System.Drawing.Size(784, 21);
			this.DirectInputDevicesHeaderLabel.Text = "Direct Input Devices - everything this program can read and map";
			// 
			// XInputDevicesPanel
			// 
			this.XInputDevicesPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.XInputDevicesPanel.Name = "XInputDevicesPanel";
			this.XInputDevicesPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.XInputDevicesPanel.Size = new System.Drawing.Size(784, 180);
			this.XInputDevicesPanel.TabIndex = 0;
			// 
			// DevicesTabPage
			// 
			this.DevicesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.DevicesTabPage.Controls.Add(this.DevicesPanel);
			this.DevicesTabPage.Controls.Add(this.DirectInputDevicesHeaderLabel);
			this.DevicesTabPage.Controls.Add(this.XInputDevicesPanel);
			this.DevicesTabPage.Controls.Add(this.XInputDevicesHeaderLabel);
			this.DevicesTabPage.Location = new System.Drawing.Point(4, 23);
			this.DevicesTabPage.Name = "DevicesTabPage";
			this.DevicesTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.DevicesTabPage.Size = new System.Drawing.Size(790, 593);
			this.DevicesTabPage.TabIndex = 4;
			this.DevicesTabPage.Text = "Devices";
			// 
			// DevicesPanel
			// 
			this.DevicesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesPanel.Location = new System.Drawing.Point(3, 3);
			this.DevicesPanel.Name = "DevicesPanel";
			this.DevicesPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.DevicesPanel.Size = new System.Drawing.Size(784, 587);
			this.DevicesPanel.TabIndex = 0;
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.Controls.Add(this.SettingsGridPanel);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 23);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.SettingsTabPage.Size = new System.Drawing.Size(790, 593);
			this.SettingsTabPage.TabIndex = 1;
			this.SettingsTabPage.Text = "Settings";
			// 
			// SettingsGridPanel
			// 
			this.SettingsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsGridPanel.Location = new System.Drawing.Point(3, 3);
			this.SettingsGridPanel.Name = "SettingsGridPanel";
			this.SettingsGridPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.SettingsGridPanel.Size = new System.Drawing.Size(784, 587);
			this.SettingsGridPanel.TabIndex = 0;
			// 
			// CloudTabPage
			// 
			this.CloudTabPage.Controls.Add(this.CloudPanel);
			this.CloudTabPage.Location = new System.Drawing.Point(4, 23);
			this.CloudTabPage.Name = "CloudTabPage";
			this.CloudTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.CloudTabPage.Size = new System.Drawing.Size(790, 593);
			this.CloudTabPage.TabIndex = 6;
			this.CloudTabPage.Text = "Cloud";
			this.CloudTabPage.UseVisualStyleBackColor = true;
			// 
			// CloudPanel
			// 
			this.CloudPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CloudPanel.Location = new System.Drawing.Point(3, 3);
			this.CloudPanel.Name = "CloudPanel";
			this.CloudPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.CloudPanel.Size = new System.Drawing.Size(784, 587);
			this.CloudPanel.TabIndex = 0;
			// 
			// HelpTabPage
			// 
			this.HelpTabPage.Controls.Add(this.HelpRichTextBox);
			this.HelpTabPage.Location = new System.Drawing.Point(4, 23);
			this.HelpTabPage.Name = "HelpTabPage";
			this.HelpTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.HelpTabPage.Size = new System.Drawing.Size(790, 593);
			this.HelpTabPage.TabIndex = 0;
			this.HelpTabPage.Text = "Help";
			// 
			// HelpRichTextBox
			// 
			this.HelpRichTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.HelpRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.HelpRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HelpRichTextBox.Location = new System.Drawing.Point(3, 3);
			this.HelpRichTextBox.Margin = new System.Windows.Forms.Padding(0);
			this.HelpRichTextBox.Name = "HelpRichTextBox";
			this.HelpRichTextBox.ReadOnly = true;
			this.HelpRichTextBox.Size = new System.Drawing.Size(784, 587);
			this.HelpRichTextBox.TabIndex = 0;
			this.HelpRichTextBox.Text = "";
			// 
			// AboutTabPage
			// 
			this.AboutTabPage.BackColor = System.Drawing.Color.Transparent;
			this.AboutTabPage.Location = new System.Drawing.Point(4, 23);
			this.AboutTabPage.Name = "AboutTabPage";
			this.AboutTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.AboutTabPage.Size = new System.Drawing.Size(790, 593);
			this.AboutTabPage.TabIndex = 0;
			this.AboutTabPage.Text = "About";
			// 
			// IssuesTabPage
			// 
			this.IssuesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.IssuesTabPage.Controls.Add(this.IssuesPanel);
			this.IssuesTabPage.Controls.Add(this.RecorderPanel);
			this.IssuesTabPage.ImageKey = "refresh_32x32.png";
			this.IssuesTabPage.Location = new System.Drawing.Point(4, 23);
			this.IssuesTabPage.Name = "IssuesTabPage";
			this.IssuesTabPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.IssuesTabPage.Size = new System.Drawing.Size(790, 593);
			this.IssuesTabPage.TabIndex = 8;
			this.IssuesTabPage.Text = "Issues";
			// 
			// IssuesPanel
			// 
			this.IssuesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IssuesPanel.Location = new System.Drawing.Point(3, 3);
			this.IssuesPanel.Name = "IssuesPanel";
			this.IssuesPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.IssuesPanel.Size = new System.Drawing.Size(784, 587);
			this.IssuesPanel.TabIndex = 0;
			// 
			// RecorderPanel
			// 
			this.RecorderPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.RecorderPanel.Location = new System.Drawing.Point(3, 470);
			this.RecorderPanel.Name = "RecorderPanel";
			this.RecorderPanel.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.RecorderPanel.Size = new System.Drawing.Size(784, 120);
			this.RecorderPanel.TabIndex = 1;
			// 
			// BuletImageList
			// 
			this.BuletImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.BuletImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.BuletImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// GamesToolStrip
			// 
			this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SaveAllButton,
            this.TestButton,
            this.MappedDevicesLabel,
            this.GameToCustomizeComboBox,
            this.AddGameButton});
			this.GamesToolStrip.Location = new System.Drawing.Point(0, 64);
			this.GamesToolStrip.Name = "GamesToolStrip";
			this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 2, 4, 0);
			this.GamesToolStrip.Size = new System.Drawing.Size(804, 26);
			this.GamesToolStrip.TabIndex = 25;
			this.GamesToolStrip.Text = "MySettingsToolStrip";
			this.GamesToolStrip.Resize += new System.EventHandler(this.GamesToolStrip_Resize);
			// 
			// SaveAllButton
			// 
			this.SaveAllButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SaveAllButton.Image = global::x360ce.App.Properties.Resources.save_32x32;
			this.SaveAllButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.SaveAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveAllButton.Margin = new System.Windows.Forms.Padding(1);
			this.SaveAllButton.Name = "SaveAllButton";
			this.SaveAllButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.SaveAllButton.Size = new System.Drawing.Size(66, 22);
			this.SaveAllButton.Text = "Save All";
			this.SaveAllButton.Click += new System.EventHandler(this.SaveAllButton_Click);
			// 
			// TestButton
			// 
			this.TestButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.TestButton.Image = global::x360ce.App.Properties.Resources.test_32x32;
			this.TestButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.TestButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.TestButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.TestButton.Margin = new System.Windows.Forms.Padding(1);
			this.TestButton.Name = "TestButton";
			this.TestButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.TestButton.Size = new System.Drawing.Size(57, 22);
			this.TestButton.Text = "Test...";
			this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
			// 
			// MappedDevicesLabel
			// 
			this.MappedDevicesLabel.Name = "MappedDevicesLabel";
			this.MappedDevicesLabel.Size = new System.Drawing.Size(38, 21);
			this.MappedDevicesLabel.Text = "Game:";
			// 
			// GameToCustomizeComboBox
			// 
			this.GameToCustomizeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.GameToCustomizeComboBox.Name = "GameToCustomizeComboBox";
			this.GameToCustomizeComboBox.Size = new System.Drawing.Size(360, 24);
			// 
			// AddGameButton
			// 
			this.AddGameButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AddGameButton.Image = global::x360ce.App.Properties.Resources.add_32x32;
			this.AddGameButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AddGameButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.AddGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddGameButton.Margin = new System.Windows.Forms.Padding(1);
			this.AddGameButton.Name = "AddGameButton";
			this.AddGameButton.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
			this.AddGameButton.Size = new System.Drawing.Size(86, 22);
			this.AddGameButton.Text = "Add Game...";
			this.AddGameButton.Click += new System.EventHandler(this.AddGameButton_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.MainTabControl);
			this.panel1.AccessibleName = "";
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 90);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(3);
			this.panel1.Size = new System.Drawing.Size(804, 626);
			this.panel1.TabIndex = 26;
			//
			// MainControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.GamesToolStrip);
			this.Name = "MainControl";
			this.Size = new System.Drawing.Size(1064, 650);
			this.MainTabControl.ResumeLayout(false);
			this.OptionsTabPage.ResumeLayout(false);
			this.GamesTabPage.ResumeLayout(false);
			this.ProgramsTabPage.ResumeLayout(false);
			this.DevicesTabPage.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.CloudTabPage.ResumeLayout(false);
			this.HelpTabPage.ResumeLayout(false);
			this.IssuesTabPage.ResumeLayout(false);
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		internal TabPage AboutTabPage;
		internal TabPage Pad1TabPage;
		internal TabPage Pad2TabPage;
		internal TabPage Pad3TabPage;
		internal TabPage Pad4TabPage;
		internal ImageList BuletImageList;
		internal TabPage HelpTabPage;
		internal RichTextBox HelpRichTextBox;
		internal TabPage SettingsTabPage;
		public TabControl MainTabControl;
		public Controls.OptionsUserControl OptionsPanel;
		public Controls.GamesGridUserControl GameSettingsPanel;
		internal TabPage CloudTabPage;
		public Controls.CloudUserControl CloudPanel;
		internal Controls.SettingsGridUserControl SettingsGridPanel;
		internal Controls.ProgramsGridUserControl ProgramsPanel;
		public TabPage ProgramsTabPage;
		public TabPage GamesTabPage;
		internal TabPage DevicesTabPage;
		public Controls.UserDevicesUserControl DevicesPanel;
		public Controls.XInputDevicesUserControl XInputDevicesPanel;
		internal System.Windows.Forms.Label XInputDevicesHeaderLabel;
		internal System.Windows.Forms.Label DirectInputDevicesHeaderLabel;
		internal ToolStrip GamesToolStrip;
		internal ToolStripButton SaveAllButton;
		internal ToolStripLabel MappedDevicesLabel;
		internal ToolStripComboBox GameToCustomizeComboBox;
		internal TabPage IssuesTabPage;
		internal JocysCom.ClassLibrary.Controls.IssuesControl.IssuesUserControl IssuesPanel;
		public x360ce.App.Controls.RecorderUserControl RecorderPanel;
		internal ToolStripButton TestButton;
		internal ToolStripButton AddGameButton;
		internal Panel panel1;
		public TabPage OptionsTabPage;
	}
}
