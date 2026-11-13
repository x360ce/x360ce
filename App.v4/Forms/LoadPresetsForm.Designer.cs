namespace x360ce.App.Controls
{
	partial class LoadPresetsForm
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
			this.RootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ButtonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.CopyPresetButton = new System.Windows.Forms.Button();
			this.CopyPresetFormatButton = new System.Windows.Forms.Button();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsGridPanel = new x360ce.App.Controls.SettingsGridUserControl();
			this.SummariesTabPage = new System.Windows.Forms.TabPage();
			this.SummariesGridPanel = new x360ce.App.Controls.SummariesGridUserControl();
			this.PresetsTabPage = new System.Windows.Forms.TabPage();
			this.PresetsGridPanel = new x360ce.App.Controls.PresetsGridUserControl();
			this.CloseButton = new System.Windows.Forms.Button();
			this.OkButton = new System.Windows.Forms.Button();
			this.OpenFileButton = new System.Windows.Forms.Button();
			this.RootTableLayoutPanel.SuspendLayout();
			this.ButtonsTableLayoutPanel.SuspendLayout();
			this.MainTabControl.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			this.SummariesTabPage.SuspendLayout();
			this.PresetsTabPage.SuspendLayout();
			this.SuspendLayout();
			//
			// RootTableLayoutPanel
			//
			this.RootTableLayoutPanel.ColumnCount = 1;
			this.RootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.Controls.Add(this.MainTabControl, 0, 0);
			this.RootTableLayoutPanel.Controls.Add(this.ButtonsTableLayoutPanel, 0, 1);
			this.RootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RootTableLayoutPanel.Name = "RootTableLayoutPanel";
			this.RootTableLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
			this.RootTableLayoutPanel.RowCount = 2;
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootTableLayoutPanel.TabIndex = 0;
			//
			// ButtonsTableLayoutPanel
			//
			this.ButtonsTableLayoutPanel.AutoSize = true;
			this.ButtonsTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ButtonsTableLayoutPanel.ColumnCount = 6;
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ButtonsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.ButtonsTableLayoutPanel.Controls.Add(this.OpenFileButton, 0, 0);
			this.ButtonsTableLayoutPanel.Controls.Add(this.CopyPresetButton, 1, 0);
			this.ButtonsTableLayoutPanel.Controls.Add(this.CopyPresetFormatButton, 2, 0);
			this.ButtonsTableLayoutPanel.Controls.Add(this.OkButton, 4, 0);
			this.ButtonsTableLayoutPanel.Controls.Add(this.CloseButton, 5, 0);
			this.ButtonsTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ButtonsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonsTableLayoutPanel.Name = "ButtonsTableLayoutPanel";
			this.ButtonsTableLayoutPanel.RowCount = 1;
			this.ButtonsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ButtonsTableLayoutPanel.TabIndex = 1;
			//
			// MainTabControl
			//
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.SummariesTabPage);
			this.MainTabControl.Controls.Add(this.PresetsTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.TabIndex = 0;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.Controls.Add(this.SettingsGridPanel);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Size = new System.Drawing.Size(592, 304);
			this.SettingsTabPage.TabIndex = 0;
			this.SettingsTabPage.Text = "My Settings";
			this.SettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// SettingsGridPanel
			// 
			this.SettingsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsGridPanel.Location = new System.Drawing.Point(0, 0);
			this.SettingsGridPanel.Name = "SettingsGridPanel";
			this.SettingsGridPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 0);
			this.SettingsGridPanel.Size = new System.Drawing.Size(592, 304);
			this.SettingsGridPanel.TabIndex = 0;
			// 
			// SummariesTabPage
			// 
			this.SummariesTabPage.Controls.Add(this.SummariesGridPanel);
			this.SummariesTabPage.Location = new System.Drawing.Point(4, 22);
			this.SummariesTabPage.Name = "SummariesTabPage";
			this.SummariesTabPage.Size = new System.Drawing.Size(592, 304);
			this.SummariesTabPage.TabIndex = 1;
			this.SummariesTabPage.Text = "Default Settings for My Controllers";
			this.SummariesTabPage.UseVisualStyleBackColor = true;
			// 
			// SummariesGridPanel
			// 
			this.SummariesGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SummariesGridPanel.Location = new System.Drawing.Point(0, 0);
			this.SummariesGridPanel.Name = "SummariesGridPanel";
			this.SummariesGridPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 0);
			this.SummariesGridPanel.Size = new System.Drawing.Size(592, 304);
			this.SummariesGridPanel.TabIndex = 0;
			// 
			// PresetsTabPage
			// 
			this.PresetsTabPage.Controls.Add(this.PresetsGridPanel);
			this.PresetsTabPage.Location = new System.Drawing.Point(4, 22);
			this.PresetsTabPage.Name = "PresetsTabPage";
			this.PresetsTabPage.Size = new System.Drawing.Size(592, 304);
			this.PresetsTabPage.TabIndex = 2;
			this.PresetsTabPage.Text = "Default Settings for Most Popular Controllers";
			this.PresetsTabPage.UseVisualStyleBackColor = true;
			// 
			// PresetsGridPanel
			// 
			this.PresetsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PresetsGridPanel.Location = new System.Drawing.Point(0, 0);
			this.PresetsGridPanel.Name = "PresetsGridPanel";
			this.PresetsGridPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 0);
			this.PresetsGridPanel.Size = new System.Drawing.Size(592, 304);
			this.PresetsGridPanel.TabIndex = 0;
			//
			// CloseButton
			//
			this.CloseButton.AutoSize = true;
			this.CloseButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.CloseButton.Margin = new System.Windows.Forms.Padding(4);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.TabIndex = 4;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			//
			// OkButton
			//
			this.OkButton.AutoSize = true;
			this.OkButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OkButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.OkButton.Margin = new System.Windows.Forms.Padding(4);
			this.OkButton.Name = "OkButton";
			this.OkButton.TabIndex = 3;
			this.OkButton.Text = "Load Selected Preset";
			this.OkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			//
			// OpenFileButton
			//
			this.OpenFileButton.AutoSize = true;
			this.OpenFileButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OpenFileButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.OpenFileButton.Margin = new System.Windows.Forms.Padding(4);
			this.OpenFileButton.Name = "OpenFileButton";
			this.OpenFileButton.TabIndex = 0;
			this.OpenFileButton.Text = "Open File...";
			this.OpenFileButton.UseVisualStyleBackColor = true;
			this.OpenFileButton.Click += new System.EventHandler(this.OpenFileButton_Click);
			//
			// CopyPresetButton
			//
			this.CopyPresetButton.AutoSize = true;
			this.CopyPresetButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CopyPresetButton.Image = global::x360ce.App.Properties.Resources.copy_32x32;
			this.CopyPresetButton.Margin = new System.Windows.Forms.Padding(4, 4, 0, 4);
			this.CopyPresetButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.CopyPresetButton.Name = "CopyPresetButton";
			this.CopyPresetButton.TabIndex = 1;
			this.CopyPresetButton.Text = "Copy Preset";
			this.CopyPresetButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.CopyPresetButton.UseVisualStyleBackColor = true;
			this.CopyPresetButton.Click += new System.EventHandler(this.CopyPresetButton_Click);
			//
			// CopyPresetFormatButton
			//
			this.CopyPresetFormatButton.AutoSize = true;
			this.CopyPresetFormatButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CopyPresetFormatButton.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
			this.CopyPresetFormatButton.MinimumSize = new System.Drawing.Size(20, 23);
			this.CopyPresetFormatButton.Name = "CopyPresetFormatButton";
			this.CopyPresetFormatButton.TabIndex = 2;
			this.CopyPresetFormatButton.Text = "▾";
			this.CopyPresetFormatButton.UseVisualStyleBackColor = true;
			this.CopyPresetFormatButton.Click += new System.EventHandler(this.CopyPresetFormatButton_Click);
			// 
			// LoadPresetsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 441);
			this.Controls.Add(this.RootTableLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "LoadPresetsForm";
			this.Text = "X360CE - Load Preset";
			// In front of the header, so the header docks to the top first and the table fills the rest.
			this.Controls.SetChildIndex(this.RootTableLayoutPanel, 0);
			this.MainTabControl.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.SummariesTabPage.ResumeLayout(false);
			this.PresetsTabPage.ResumeLayout(false);
			this.ButtonsTableLayoutPanel.ResumeLayout(false);
			this.ButtonsTableLayoutPanel.PerformLayout();
			this.RootTableLayoutPanel.ResumeLayout(false);
			this.RootTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel RootTableLayoutPanel;
		private System.Windows.Forms.TableLayoutPanel ButtonsTableLayoutPanel;
		public System.Windows.Forms.Button CopyPresetButton;
		public System.Windows.Forms.Button CopyPresetFormatButton;
		public System.Windows.Forms.TabControl MainTabControl;
		public System.Windows.Forms.TabPage SettingsTabPage;
		public System.Windows.Forms.TabPage SummariesTabPage;
		public System.Windows.Forms.TabPage PresetsTabPage;
		private System.Windows.Forms.Button CloseButton;
		internal System.Windows.Forms.Button OpenFileButton;
		public System.Windows.Forms.Button OkButton;
		public SettingsGridUserControl SettingsGridPanel;
		public SummariesGridUserControl SummariesGridPanel;
		public PresetsGridUserControl PresetsGridPanel;
	}
}
