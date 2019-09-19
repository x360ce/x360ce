namespace x360ce.App.Controls
{
	partial class GamesGridUserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this.GamesDataGridView = new System.Windows.Forms.DataGridView();
			this.IsEnabledColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.GameIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PlatformColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FileFolderColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.EmptyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
			this.ScanGamesButton = new System.Windows.Forms.ToolStripButton();
			this.AddGameButton = new System.Windows.Forms.ToolStripButton();
			this.DeleteGamesButton = new System.Windows.Forms.ToolStripButton();
			this.SaveGamesButton = new System.Windows.Forms.ToolStripButton();
			this.StartGameButton = new System.Windows.Forms.ToolStripButton();
			this.OpenGameButton = new System.Windows.Forms.ToolStripButton();
			this.ShowGamesDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.ShowAllGamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowEnabledGamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowDisabledGamesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScanProgressLevel0Label = new System.Windows.Forms.Label();
			this.AddGameOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ImportOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.ScanProgressPanel = new System.Windows.Forms.Panel();
			this.GameDetailsControl = new x360ce.App.Controls.GameDetailsUserControl();
			this.ScanProgressLevel1Label = new System.Windows.Forms.Label();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.GamesDataGridView)).BeginInit();
			this.GamesToolStrip.SuspendLayout();
			this.ScanProgressPanel.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// GamesDataGridView
			// 
			this.GamesDataGridView.AllowUserToAddRows = false;
			this.GamesDataGridView.AllowUserToDeleteRows = false;
			this.GamesDataGridView.AllowUserToResizeRows = false;
			this.GamesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.GamesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GamesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GamesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
			this.GamesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GamesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.IsEnabledColumn,
            this.MyIconColumn,
            this.GameIdColumn,
            this.FileNameColumn,
            this.ProductNameColumn,
            this.PlatformColumn,
            this.FileFolderColumn,
            this.EmptyColumn});
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.GamesDataGridView.DefaultCellStyle = dataGridViewCellStyle7;
			this.GamesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GamesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.GamesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.GamesDataGridView.Name = "GamesDataGridView";
			this.GamesDataGridView.ReadOnly = true;
			dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GamesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle8;
			this.GamesDataGridView.RowHeadersVisible = false;
			this.GamesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.GamesDataGridView.Size = new System.Drawing.Size(781, 338);
			this.GamesDataGridView.TabIndex = 0;
			this.GamesDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GamesDataGridView_CellClick);
			this.GamesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.GamesDataGridView_CellFormatting);
			this.GamesDataGridView.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.GamesDataGridView_DataBindingComplete);
			this.GamesDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GamesDataGridView_KeyDown);
			// 
			// IsEnabledColumn
			// 
			this.IsEnabledColumn.DataPropertyName = "IsEnabled";
			this.IsEnabledColumn.HeaderText = "";
			this.IsEnabledColumn.Name = "IsEnabledColumn";
			this.IsEnabledColumn.ReadOnly = true;
			this.IsEnabledColumn.Width = 24;
			// 
			// MyIconColumn
			// 
			this.MyIconColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyIconColumn.HeaderText = "";
			this.MyIconColumn.MinimumWidth = 24;
			this.MyIconColumn.Name = "MyIconColumn";
			this.MyIconColumn.ReadOnly = true;
			this.MyIconColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.MyIconColumn.Width = 24;
			// 
			// GameIdColumn
			// 
			this.GameIdColumn.DataPropertyName = "GameId";
			this.GameIdColumn.HeaderText = "ID";
			this.GameIdColumn.Name = "GameIdColumn";
			this.GameIdColumn.ReadOnly = true;
			this.GameIdColumn.Visible = false;
			// 
			// FileNameColumn
			// 
			this.FileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.FileNameColumn.DataPropertyName = "FileName";
			this.FileNameColumn.FillWeight = 30F;
			this.FileNameColumn.HeaderText = "File Name";
			this.FileNameColumn.Name = "FileNameColumn";
			this.FileNameColumn.ReadOnly = true;
			this.FileNameColumn.Width = 79;
			// 
			// ProductNameColumn
			// 
			this.ProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ProductNameColumn.DataPropertyName = "FileProductName";
			this.ProductNameColumn.FillWeight = 70F;
			this.ProductNameColumn.HeaderText = "Product Name";
			this.ProductNameColumn.Name = "ProductNameColumn";
			this.ProductNameColumn.ReadOnly = true;
			// 
			// PlatformColumn
			// 
			this.PlatformColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PlatformColumn.DataPropertyName = "ProcessorArchitecture";
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.PlatformColumn.DefaultCellStyle = dataGridViewCellStyle6;
			this.PlatformColumn.HeaderText = "Platform";
			this.PlatformColumn.Name = "PlatformColumn";
			this.PlatformColumn.ReadOnly = true;
			this.PlatformColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.PlatformColumn.Width = 51;
			// 
			// FileFolderColumn
			// 
			this.FileFolderColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.FileFolderColumn.DataPropertyName = "FullPath";
			this.FileFolderColumn.HeaderText = "File Folder";
			this.FileFolderColumn.Name = "FileFolderColumn";
			this.FileFolderColumn.ReadOnly = true;
			this.FileFolderColumn.Width = 80;
			// 
			// EmptyColumn
			// 
			this.EmptyColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.EmptyColumn.HeaderText = "";
			this.EmptyColumn.Name = "EmptyColumn";
			this.EmptyColumn.ReadOnly = true;
			// 
			// GamesToolStrip
			// 
			this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScanGamesButton,
            this.AddGameButton,
            this.DeleteGamesButton,
            this.SaveGamesButton,
            this.StartGameButton,
            this.OpenGameButton,
            this.ShowGamesDropDownButton});
			this.GamesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.GamesToolStrip.Name = "GamesToolStrip";
			this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
			this.GamesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.GamesToolStrip.Size = new System.Drawing.Size(781, 25);
			this.GamesToolStrip.TabIndex = 2;
			this.GamesToolStrip.Text = "MySettingsToolStrip";
			// 
			// ScanGamesButton
			// 
			this.ScanGamesButton.Image = global::x360ce.App.Properties.Resources.folder_view_16x16;
			this.ScanGamesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ScanGamesButton.Name = "ScanGamesButton";
			this.ScanGamesButton.Size = new System.Drawing.Size(52, 22);
			this.ScanGamesButton.Text = "&Scan";
			this.ScanGamesButton.Click += new System.EventHandler(this.ScanButton_Click);
			// 
			// AddGameButton
			// 
			this.AddGameButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddGameButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AddGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddGameButton.Name = "AddGameButton";
			this.AddGameButton.Size = new System.Drawing.Size(58, 22);
			this.AddGameButton.Text = "Add...";
			this.AddGameButton.Click += new System.EventHandler(this.AddGameButton_Click);
			// 
			// DeleteGamesButton
			// 
			this.DeleteGamesButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteGamesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DeleteGamesButton.Name = "DeleteGamesButton";
			this.DeleteGamesButton.Size = new System.Drawing.Size(60, 22);
			this.DeleteGamesButton.Text = "&Delete";
			this.DeleteGamesButton.Click += new System.EventHandler(this.DeleteGamesButton_Click);
			// 
			// SaveGamesButton
			// 
			this.SaveGamesButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.SaveGamesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveGamesButton.Name = "SaveGamesButton";
			this.SaveGamesButton.Size = new System.Drawing.Size(51, 22);
			this.SaveGamesButton.Text = "&Save";
			this.SaveGamesButton.Click += new System.EventHandler(this.SaveGamesButton_Click);
			// 
			// StartGameButton
			// 
			this.StartGameButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.StartGameButton.Image = global::x360ce.App.Properties.Resources.launch_16x16;
			this.StartGameButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StartGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StartGameButton.Name = "StartGameButton";
			this.StartGameButton.Size = new System.Drawing.Size(51, 22);
			this.StartGameButton.Text = "Start";
			this.StartGameButton.Click += new System.EventHandler(this.StartGameButton_Click);
			// 
			// OpenGameButton
			// 
			this.OpenGameButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.OpenGameButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
			this.OpenGameButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.OpenGameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenGameButton.Name = "OpenGameButton";
			this.OpenGameButton.Size = new System.Drawing.Size(65, 22);
			this.OpenGameButton.Text = "Open...";
			this.OpenGameButton.Click += new System.EventHandler(this.OpenGameFolderButton_Click);
			// 
			// ShowGamesDropDownButton
			// 
			this.ShowGamesDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowAllGamesMenuItem,
            this.ShowEnabledGamesMenuItem,
            this.ShowDisabledGamesMenuItem});
			this.ShowGamesDropDownButton.Image = global::x360ce.App.Properties.Resources.checkbox_undefined_16x16;
			this.ShowGamesDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ShowGamesDropDownButton.Name = "ShowGamesDropDownButton";
			this.ShowGamesDropDownButton.Size = new System.Drawing.Size(85, 22);
			this.ShowGamesDropDownButton.Text = "Show: All";
			// 
			// ShowAllGamesMenuItem
			// 
			this.ShowAllGamesMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_undefined_16x16;
			this.ShowAllGamesMenuItem.Name = "ShowAllGamesMenuItem";
			this.ShowAllGamesMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowAllGamesMenuItem.Text = "Show: All";
			this.ShowAllGamesMenuItem.Click += new System.EventHandler(this.ShowGamesMenuItem_Click);
			// 
			// ShowEnabledGamesMenuItem
			// 
			this.ShowEnabledGamesMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_16x16;
			this.ShowEnabledGamesMenuItem.Name = "ShowEnabledGamesMenuItem";
			this.ShowEnabledGamesMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowEnabledGamesMenuItem.Text = "Show: Enabled";
			this.ShowEnabledGamesMenuItem.Click += new System.EventHandler(this.ShowGamesMenuItem_Click);
			// 
			// ShowDisabledGamesMenuItem
			// 
			this.ShowDisabledGamesMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			this.ShowDisabledGamesMenuItem.Name = "ShowDisabledGamesMenuItem";
			this.ShowDisabledGamesMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowDisabledGamesMenuItem.Text = "Show: Disabled";
			this.ShowDisabledGamesMenuItem.Click += new System.EventHandler(this.ShowGamesMenuItem_Click);
			// 
			// ScanProgressLevel0Label
			// 
			this.ScanProgressLevel0Label.AutoSize = true;
			this.ScanProgressLevel0Label.BackColor = System.Drawing.SystemColors.Control;
			this.ScanProgressLevel0Label.Location = new System.Drawing.Point(3, 0);
			this.ScanProgressLevel0Label.Name = "ScanProgressLevel0Label";
			this.ScanProgressLevel0Label.Padding = new System.Windows.Forms.Padding(3);
			this.ScanProgressLevel0Label.Size = new System.Drawing.Size(143, 19);
			this.ScanProgressLevel0Label.TabIndex = 0;
			this.ScanProgressLevel0Label.Text = "[ScanProgressLevel0Label]";
			// 
			// ScanProgressPanel
			// 
			this.ScanProgressPanel.AutoSize = true;
			this.ScanProgressPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ScanProgressPanel.Controls.Add(this.tableLayoutPanel1);
			this.ScanProgressPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.ScanProgressPanel.Location = new System.Drawing.Point(0, 317);
			this.ScanProgressPanel.Name = "ScanProgressPanel";
			this.ScanProgressPanel.Size = new System.Drawing.Size(781, 46);
			this.ScanProgressPanel.TabIndex = 4;
			this.ScanProgressPanel.Visible = false;
			// 
			// GameDetailsControl
			// 
			this.GameDetailsControl.AutoSize = true;
			this.GameDetailsControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.GameDetailsControl.BackColor = System.Drawing.SystemColors.Control;
			this.GameDetailsControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.GameDetailsControl.Location = new System.Drawing.Point(0, 363);
			this.GameDetailsControl.Name = "GameDetailsControl";
			this.GameDetailsControl.Size = new System.Drawing.Size(781, 226);
			this.GameDetailsControl.TabIndex = 3;
			// 
			// ScanProgressLevel1Label
			// 
			this.ScanProgressLevel1Label.AutoSize = true;
			this.ScanProgressLevel1Label.BackColor = System.Drawing.SystemColors.Control;
			this.ScanProgressLevel1Label.Location = new System.Drawing.Point(3, 19);
			this.ScanProgressLevel1Label.Name = "ScanProgressLevel1Label";
			this.ScanProgressLevel1Label.Padding = new System.Windows.Forms.Padding(3);
			this.ScanProgressLevel1Label.Size = new System.Drawing.Size(143, 19);
			this.ScanProgressLevel1Label.TabIndex = 1;
			this.ScanProgressLevel1Label.Text = "[ScanProgressLevel1Label]";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.ScanProgressLevel0Label, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.ScanProgressLevel1Label, 0, 1);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(149, 38);
			this.tableLayoutPanel1.TabIndex = 2;
			// 
			// GamesGridUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.ScanProgressPanel);
			this.Controls.Add(this.GamesDataGridView);
			this.Controls.Add(this.GameDetailsControl);
			this.Controls.Add(this.GamesToolStrip);
			this.Name = "GamesGridUserControl";
			this.Size = new System.Drawing.Size(781, 589);
			this.Load += new System.EventHandler(this.GamesGridUserControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.GamesDataGridView)).EndInit();
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.ScanProgressPanel.ResumeLayout(false);
			this.ScanProgressPanel.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.OpenFileDialog AddGameOpenFileDialog;
		private System.Windows.Forms.Label ScanProgressLevel0Label;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
        private System.Windows.Forms.ToolStripButton DeleteGamesButton;
        private System.Windows.Forms.ToolStripButton SaveGamesButton;
        private System.Windows.Forms.ToolStripButton ScanGamesButton;
		private System.Windows.Forms.ToolStripButton AddGameButton;
		private GameDetailsUserControl GameDetailsControl;
		private System.Windows.Forms.ToolStripButton StartGameButton;
		private System.Windows.Forms.ToolStripButton OpenGameButton;
		private System.Windows.Forms.ToolStripDropDownButton ShowGamesDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem ShowAllGamesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowEnabledGamesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowDisabledGamesMenuItem;
		private System.Windows.Forms.OpenFileDialog ImportOpenFileDialog;
		private System.Windows.Forms.SaveFileDialog ExportSaveFileDialog;
		public System.Windows.Forms.DataGridView GamesDataGridView;
		private System.Windows.Forms.Panel ScanProgressPanel;
		private System.Windows.Forms.DataGridViewCheckBoxColumn IsEnabledColumn;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn GameIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PlatformColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FileFolderColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn EmptyColumn;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label ScanProgressLevel1Label;
	}
}
