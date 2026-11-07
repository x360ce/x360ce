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
			this.MainStatusStrip = new System.Windows.Forms.StatusStrip();
			this.LoadingLabel = new System.Windows.Forms.Label();
			this.StatusTimerLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.UpdateFrequencyLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.FormUpdateFrequencyLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.InterfaceUpdatesButton = new System.Windows.Forms.ToolStripButton();
			this.UpdateDevicesStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.CloudMessagesLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusEventsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusSaveLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusIsAdminLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusAiAccessLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusErrorsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusDllLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.HelpBodyLabel = new System.Windows.Forms.Label();
			this.HelpPictureBox = new System.Windows.Forms.PictureBox();
			this.HelpSubjectLabel = new System.Windows.Forms.Label();
			this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
			this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.TrayNotifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.TrayContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.OpenApplicationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EmulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.MainStatusStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).BeginInit();
			this.TrayContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainStatusStrip
			// 
			this.MainStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusTimerLabel,
            this.UpdateFrequencyLabel,
            this.InterfaceUpdatesButton,
            this.UpdateDevicesStatusLabel,
            this.CloudMessagesLabel,
            this.StatusEventsLabel,
            this.StatusSaveLabel,
            this.StatusIsAdminLabel,
            this.StatusAiAccessLabel,
            this.StatusErrorsLabel,
            this.toolStripStatusLabel2,
            this.StatusDllLabel});
			this.MainStatusStrip.Location = new System.Drawing.Point(0, 716);
			this.MainStatusStrip.Name = "MainStatusStrip";
			this.MainStatusStrip.Size = new System.Drawing.Size(804, 25);
			this.MainStatusStrip.SizingGrip = false;
			this.MainStatusStrip.TabIndex = 0;
			this.MainStatusStrip.Text = "statusStrip1";
			// 
			// StatusTimerLabel
			// 
			this.StatusTimerLabel.Name = "StatusTimerLabel";
			this.StatusTimerLabel.Size = new System.Drawing.Size(97, 20);
			this.StatusTimerLabel.Text = "StatusTimerLabel";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(92, 20);
			this.toolStripStatusLabel2.Spring = true;
			// 
			// UpdateFrequencyLabel
			// 
			this.UpdateFrequencyLabel.Name = "UpdateFrequencyLabel";
			this.UpdateFrequencyLabel.Size = new System.Drawing.Size(33, 20);
			this.UpdateFrequencyLabel.Text = "HW Hz: 0";
			// 
			// InterfaceUpdatesButton
			// 
			this.InterfaceUpdatesButton.CheckOnClick = true;
			this.InterfaceUpdatesButton.Checked = true;
			this.InterfaceUpdatesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.InterfaceUpdatesButton.Name = "InterfaceUpdatesButton";
			this.InterfaceUpdatesButton.Text = "UI Hz: 0";
			this.InterfaceUpdatesButton.ToolTipText = "How often the window redraws. Press to stop it redrawing, to see what the engine reaches with nothing drawing.";
			// 
			// FormUpdateFrequencyLabel
			// 
			this.FormUpdateFrequencyLabel.Name = "FormUpdateFrequencyLabel";
			this.FormUpdateFrequencyLabel.Size = new System.Drawing.Size(33, 20);
			this.FormUpdateFrequencyLabel.Text = "UI Hz: 0";
			// 
			// UpdateDevicesStatusLabel
			// 
			this.UpdateDevicesStatusLabel.Name = "UpdateDevicesStatusLabel";
			this.UpdateDevicesStatusLabel.Size = new System.Drawing.Size(27, 20);
			this.UpdateDevicesStatusLabel.Text = "D: 0";
			// 
			// CloudMessagesLabel
			// 
			this.CloudMessagesLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.CloudMessagesLabel.Name = "CloudMessagesLabel";
			this.CloudMessagesLabel.Size = new System.Drawing.Size(34, 20);
			this.CloudMessagesLabel.Text = "M: 0";
			// 
			// StatusEventsLabel
			// 
			this.StatusEventsLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusEventsLabel.Name = "StatusEventsLabel";
			this.StatusEventsLabel.Size = new System.Drawing.Size(105, 20);
			this.StatusEventsLabel.Text = "StatusEventsLabel";
			// 
			// StatusSaveLabel
			// 
			this.StatusSaveLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusSaveLabel.Name = "StatusSaveLabel";
			this.StatusSaveLabel.Size = new System.Drawing.Size(95, 20);
			this.StatusSaveLabel.Text = "StatusSaveLabel";
			// 
			// StatusIsAdminLabel
			// 
			this.StatusIsAdminLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusIsAdminLabel.Name = "StatusIsAdminLabel";
			this.StatusIsAdminLabel.Size = new System.Drawing.Size(115, 20);
			this.StatusIsAdminLabel.Text = "StatusIsAdminLabel";
			// 
			// StatusAiAccessLabel
			// 
			this.StatusAiAccessLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusAiAccessLabel.IsLink = true;
			this.StatusAiAccessLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
			this.StatusAiAccessLabel.Name = "StatusAiAccessLabel";
			this.StatusAiAccessLabel.Size = new System.Drawing.Size(48, 20);
			this.StatusAiAccessLabel.Text = "AI: off";
			this.StatusAiAccessLabel.Click += new System.EventHandler(this.StatusAiAccessLabel_Click);
			// 
			// StatusErrorsLabel
			// 
			this.StatusErrorsLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusErrorsLabel.Image = global::x360ce.App.Properties.Resources.error_16x16;
			this.StatusErrorsLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.StatusErrorsLabel.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.StatusErrorsLabel.IsLink = true;
			this.StatusErrorsLabel.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
			this.StatusErrorsLabel.Name = "StatusErrorsLabel";
			this.StatusErrorsLabel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.StatusErrorsLabel.Size = new System.Drawing.Size(73, 20);
			this.StatusErrorsLabel.Text = "Errors: 0";
			this.StatusErrorsLabel.Click += new System.EventHandler(this.StatusErrorLabel_Click);
			// 
			// StatusDllLabel
			// 
			this.StatusDllLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
			this.StatusDllLabel.Name = "StatusDllLabel";
			this.StatusDllLabel.Size = new System.Drawing.Size(85, 20);
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
			this.HelpPictureBox.Image = global::x360ce.App.Properties.Resources.tip_24x24;
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
			this.HeaderPictureBox.Image = global::x360ce.App.Properties.Resources.app_48x48;
			this.HeaderPictureBox.Location = new System.Drawing.Point(630, 9);
			this.HeaderPictureBox.Name = "HeaderPictureBox";
			this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
			this.HeaderPictureBox.TabIndex = 6;
			this.HeaderPictureBox.TabStop = false;
			// 
			// TrayNotifyIcon
			// 
			this.TrayNotifyIcon.ContextMenuStrip = this.TrayContextMenuStrip;
			this.TrayNotifyIcon.Icon = global::x360ce.App.Properties.Resources.app;
			this.TrayNotifyIcon.Text = "X360CE 4 Controller Emulator";
			this.TrayNotifyIcon.Visible = true;
			// 
			// TrayContextMenuStrip
			// 
			this.TrayContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenApplicationToolStripMenuItem,
            this.EmulationToolStripMenuItem,
            this.ExitToolStripMenuItem});
			this.TrayContextMenuStrip.Name = "TrayContextMenuStrip";
			this.TrayContextMenuStrip.Size = new System.Drawing.Size(174, 56);
			// 
			// OpenApplicationToolStripMenuItem
			// 
			this.OpenApplicationToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.app_16x16;
			this.OpenApplicationToolStripMenuItem.Name = "OpenApplicationToolStripMenuItem";
			this.OpenApplicationToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
			this.OpenApplicationToolStripMenuItem.Text = "Open Application";
			this.OpenApplicationToolStripMenuItem.Click += new System.EventHandler(this.OpenApplicationToolStripMenuItem_Click);
			// 
			// EmulationToolStripMenuItem
			// 
			this.EmulationToolStripMenuItem.CheckOnClick = true;
			this.EmulationToolStripMenuItem.Name = "EmulationToolStripMenuItem";
			this.EmulationToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
			this.EmulationToolStripMenuItem.Text = "Enable XInput";
			this.EmulationToolStripMenuItem.Click += new System.EventHandler(this.EmulationToolStripMenuItem_Click);
			// 
			// ExitToolStripMenuItem
			// 
			this.ExitToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.exit_16x16;
			this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
			this.ExitToolStripMenuItem.Size = new System.Drawing.Size(173, 26);
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
			// LoadingLabel
			//
			this.LoadingLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LoadingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F);
			this.LoadingLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.LoadingLabel.Name = "LoadingLabel";
			this.LoadingLabel.Text = "Loading. Please wait...";
			this.LoadingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// MainForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(1064, 771);
			this.Controls.Add(this.LoadingLabel);
			this.Controls.Add(this.MainStatusStrip);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(1000, 600);
			this.Name = "MainForm";
			this.Text = "TocaEdit Xbox 360 Controller Emulator Application";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.Controls.SetChildIndex(this.MainStatusStrip, 0);
			this.Controls.SetChildIndex(this.LoadingLabel, 0);
			this.MainStatusStrip.ResumeLayout(false);
			this.MainStatusStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).EndInit();
			this.TrayContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		internal StatusStrip MainStatusStrip;
		internal Label LoadingLabel;
		public ToolStripStatusLabel StatusTimerLabel;
		internal ToolStripStatusLabel StatusEventsLabel;
		internal ToolStripStatusLabel StatusSaveLabel;
		ToolStripStatusLabel toolStripStatusLabel2;
		internal ToolStripStatusLabel StatusIsAdminLabel;
		internal ToolStripStatusLabel StatusAiAccessLabel;
		internal ToolStripStatusLabel StatusErrorsLabel;
        internal ToolStripStatusLabel StatusDllLabel;
        public ToolTip ToolTip;
		private NotifyIcon TrayNotifyIcon;
		internal ContextMenuStrip TrayContextMenuStrip;
		internal ToolStripMenuItem OpenApplicationToolStripMenuItem;
		internal ToolStripMenuItem EmulationToolStripMenuItem;
		internal ToolStripMenuItem ExitToolStripMenuItem;
		internal Label HelpBodyLabel;
		internal PictureBox HelpPictureBox;
		internal Label HelpSubjectLabel;
		internal PictureBox HeaderPictureBox;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		public ToolStripStatusLabel CloudMessagesLabel;
		internal ToolStripStatusLabel UpdateDevicesStatusLabel;
		internal ToolStripStatusLabel UpdateFrequencyLabel;
		private ToolStripStatusLabel FormUpdateFrequencyLabel;
		internal ToolStripButton InterfaceUpdatesButton;
	}
}
