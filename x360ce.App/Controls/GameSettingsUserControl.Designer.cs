namespace x360ce.App.Controls
{
	partial class GameSettingsUserControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this.GamesTabControl = new System.Windows.Forms.TabControl();
			this.GamesTabPage = new System.Windows.Forms.TabPage();
			this.GamesDataGridView = new System.Windows.Forms.DataGridView();
			this.EnabledColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.GameIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FilePathColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
			this.ScanProgressLabel = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.GameDetailsControl = new x360ce.App.Controls.GameSettingDetailsUserControl();
			this.GlobalSettingsTabPage = new System.Windows.Forms.TabPage();
			this.ProgramsDataGridView = new System.Windows.Forms.DataGridView();
			this.ProgramImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.ProgramIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramsToolStrip = new System.Windows.Forms.ToolStrip();
			this.RefreshProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.ImportProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.ExportProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.DeleteProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.panel2 = new System.Windows.Forms.Panel();
			this.GameDefaultDetailsControl = new x360ce.App.Controls.GameSettingDetailsUserControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.IncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
			this.MinimumInstanceCountNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AddGameOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ImportOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.GamesTabControl.SuspendLayout();
			this.GamesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.GamesDataGridView)).BeginInit();
			this.GamesToolStrip.SuspendLayout();
			this.GlobalSettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).BeginInit();
			this.ProgramsToolStrip.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// GamesTabControl
			// 
			this.GamesTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.GamesTabControl.Controls.Add(this.GamesTabPage);
			this.GamesTabControl.Controls.Add(this.GlobalSettingsTabPage);
			this.GamesTabControl.Controls.Add(this.SettingsTabPage);
			this.GamesTabControl.ItemSize = new System.Drawing.Size(116, 18);
			this.GamesTabControl.Location = new System.Drawing.Point(3, 3);
			this.GamesTabControl.Name = "GamesTabControl";
			this.GamesTabControl.SelectedIndex = 0;
			this.GamesTabControl.Size = new System.Drawing.Size(695, 476);
			this.GamesTabControl.TabIndex = 19;
			this.GamesTabControl.SelectedIndexChanged += new System.EventHandler(this.GamesTabControl_SelectedIndexChanged);
			// 
			// GamesTabPage
			// 
			this.GamesTabPage.Controls.Add(this.GamesDataGridView);
			this.GamesTabPage.Controls.Add(this.GamesToolStrip);
			this.GamesTabPage.Controls.Add(this.ScanProgressLabel);
			this.GamesTabPage.Controls.Add(this.panel1);
			this.GamesTabPage.Controls.Add(this.GameDetailsControl);
			this.GamesTabPage.Location = new System.Drawing.Point(4, 22);
			this.GamesTabPage.Name = "GamesTabPage";
			this.GamesTabPage.Size = new System.Drawing.Size(687, 450);
			this.GamesTabPage.TabIndex = 0;
			this.GamesTabPage.Text = "My Game Settings";
			this.GamesTabPage.UseVisualStyleBackColor = true;
			// 
			// GamesDataGridView
			// 
			this.GamesDataGridView.AllowUserToAddRows = false;
			this.GamesDataGridView.AllowUserToDeleteRows = false;
			this.GamesDataGridView.AllowUserToResizeRows = false;
			this.GamesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.GamesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GamesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GamesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.GamesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GamesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EnabledColumn,
            this.MyIconColumn,
            this.GameIdColumn,
            this.FileNameColumn,
            this.ProductNameColumn,
            this.FilePathColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.GamesDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.GamesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GamesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.GamesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.GamesDataGridView.Name = "GamesDataGridView";
			this.GamesDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GamesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.GamesDataGridView.RowHeadersVisible = false;
			this.GamesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.GamesDataGridView.Size = new System.Drawing.Size(532, 385);
			this.GamesDataGridView.TabIndex = 0;
			this.GamesDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.GamesDataGridView_CellClick);
			this.GamesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.GamesDataGridView_CellFormatting);
			this.GamesDataGridView.SelectionChanged += new System.EventHandler(this.GamesDataGridView_SelectionChanged);
			this.GamesDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GamesDataGridView_KeyDown);
			// 
			// EnabledColumn
			// 
			this.EnabledColumn.DataPropertyName = "IsEnabled";
			this.EnabledColumn.HeaderText = "";
			this.EnabledColumn.Name = "EnabledColumn";
			this.EnabledColumn.ReadOnly = true;
			this.EnabledColumn.Width = 24;
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
			// FilePathColumn
			// 
			this.FilePathColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.FilePathColumn.DataPropertyName = "FullPath";
			this.FilePathColumn.HeaderText = "Full Path";
			this.FilePathColumn.Name = "FilePathColumn";
			this.FilePathColumn.ReadOnly = true;
			this.FilePathColumn.Width = 73;
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
			this.GamesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.GamesToolStrip.Size = new System.Drawing.Size(532, 25);
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
			// ScanProgressLabel
			// 
			this.ScanProgressLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ScanProgressLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.ScanProgressLabel.Location = new System.Drawing.Point(0, 410);
			this.ScanProgressLabel.Name = "ScanProgressLabel";
			this.ScanProgressLabel.Padding = new System.Windows.Forms.Padding(3);
			this.ScanProgressLabel.Size = new System.Drawing.Size(532, 40);
			this.ScanProgressLabel.TabIndex = 0;
			this.ScanProgressLabel.Text = "[ScanProgressLabel]";
			this.ScanProgressLabel.Visible = false;
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(532, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1, 450);
			this.panel1.TabIndex = 4;
			// 
			// GameDetailsControl
			// 
			this.GameDetailsControl.BackColor = System.Drawing.SystemColors.Control;
			this.GameDetailsControl.Dock = System.Windows.Forms.DockStyle.Right;
			this.GameDetailsControl.Location = new System.Drawing.Point(533, 0);
			this.GameDetailsControl.Name = "GameDetailsControl";
			this.GameDetailsControl.Size = new System.Drawing.Size(154, 450);
			this.GameDetailsControl.TabIndex = 3;
			// 
			// GlobalSettingsTabPage
			// 
			this.GlobalSettingsTabPage.Controls.Add(this.ProgramsDataGridView);
			this.GlobalSettingsTabPage.Controls.Add(this.ProgramsToolStrip);
			this.GlobalSettingsTabPage.Controls.Add(this.panel2);
			this.GlobalSettingsTabPage.Controls.Add(this.GameDefaultDetailsControl);
			this.GlobalSettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.GlobalSettingsTabPage.Name = "GlobalSettingsTabPage";
			this.GlobalSettingsTabPage.Size = new System.Drawing.Size(687, 450);
			this.GlobalSettingsTabPage.TabIndex = 1;
			this.GlobalSettingsTabPage.Text = "Default Settings for Most Popular Games";
			this.GlobalSettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// ProgramsDataGridView
			// 
			this.ProgramsDataGridView.AllowUserToAddRows = false;
			this.ProgramsDataGridView.AllowUserToDeleteRows = false;
			this.ProgramsDataGridView.AllowUserToOrderColumns = true;
			this.ProgramsDataGridView.AllowUserToResizeRows = false;
			this.ProgramsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.ProgramsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ProgramsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.ProgramsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ProgramsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProgramImageColumn,
            this.ProgramIdColumn,
            this.ProgramFileNameColumn,
            this.ProgramProductNameColumn});
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ProgramsDataGridView.DefaultCellStyle = dataGridViewCellStyle5;
			this.ProgramsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProgramsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.ProgramsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.ProgramsDataGridView.Name = "ProgramsDataGridView";
			this.ProgramsDataGridView.ReadOnly = true;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
			this.ProgramsDataGridView.RowHeadersVisible = false;
			this.ProgramsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ProgramsDataGridView.Size = new System.Drawing.Size(532, 425);
			this.ProgramsDataGridView.TabIndex = 1;
			this.ProgramsDataGridView.SelectionChanged += new System.EventHandler(this.ProgramsDataGridView_SelectionChanged);
			this.ProgramsDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProgramsDataGridView_KeyDown);
			// 
			// ProgramImageColumn
			// 
			this.ProgramImageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ProgramImageColumn.HeaderText = "";
			this.ProgramImageColumn.MinimumWidth = 24;
			this.ProgramImageColumn.Name = "ProgramImageColumn";
			this.ProgramImageColumn.ReadOnly = true;
			this.ProgramImageColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramImageColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.ProgramImageColumn.Visible = false;
			// 
			// ProgramIdColumn
			// 
			this.ProgramIdColumn.DataPropertyName = "GameId";
			this.ProgramIdColumn.HeaderText = "ID";
			this.ProgramIdColumn.Name = "ProgramIdColumn";
			this.ProgramIdColumn.ReadOnly = true;
			this.ProgramIdColumn.Visible = false;
			// 
			// ProgramFileNameColumn
			// 
			this.ProgramFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProgramFileNameColumn.DataPropertyName = "FileName";
			this.ProgramFileNameColumn.FillWeight = 30F;
			this.ProgramFileNameColumn.HeaderText = "File Name";
			this.ProgramFileNameColumn.Name = "ProgramFileNameColumn";
			this.ProgramFileNameColumn.ReadOnly = true;
			// 
			// ProgramProductNameColumn
			// 
			this.ProgramProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProgramProductNameColumn.DataPropertyName = "FileProductName";
			this.ProgramProductNameColumn.FillWeight = 70F;
			this.ProgramProductNameColumn.HeaderText = "Product Name";
			this.ProgramProductNameColumn.Name = "ProgramProductNameColumn";
			this.ProgramProductNameColumn.ReadOnly = true;
			// 
			// ProgramsToolStrip
			// 
			this.ProgramsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.ProgramsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshProgramsButton,
            this.ImportProgramsButton,
            this.ExportProgramsButton,
            this.DeleteProgramsButton});
			this.ProgramsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.ProgramsToolStrip.Name = "ProgramsToolStrip";
			this.ProgramsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.ProgramsToolStrip.Size = new System.Drawing.Size(532, 25);
			this.ProgramsToolStrip.TabIndex = 3;
			this.ProgramsToolStrip.Text = "MySettingsToolStrip";
			// 
			// RefreshProgramsButton
			// 
			this.RefreshProgramsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshProgramsButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshProgramsButton.Name = "RefreshProgramsButton";
			this.RefreshProgramsButton.Size = new System.Drawing.Size(66, 22);
			this.RefreshProgramsButton.Text = "&Refresh";
			this.RefreshProgramsButton.Click += new System.EventHandler(this.RefreshProgramsButton_Click);
			// 
			// ImportProgramsButton
			// 
			this.ImportProgramsButton.Image = global::x360ce.App.Properties.Resources.data_into_16x16;
			this.ImportProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ImportProgramsButton.Name = "ImportProgramsButton";
			this.ImportProgramsButton.Size = new System.Drawing.Size(72, 22);
			this.ImportProgramsButton.Text = "&Import...";
			this.ImportProgramsButton.Click += new System.EventHandler(this.ImportProgramsButton_Click);
			// 
			// ExportProgramsButton
			// 
			this.ExportProgramsButton.Image = global::x360ce.App.Properties.Resources.data_out_16x16;
			this.ExportProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ExportProgramsButton.Name = "ExportProgramsButton";
			this.ExportProgramsButton.Size = new System.Drawing.Size(69, 22);
			this.ExportProgramsButton.Text = "&Export...";
			this.ExportProgramsButton.Click += new System.EventHandler(this.ExportProgramsButton_Click);
			// 
			// DeleteProgramsButton
			// 
			this.DeleteProgramsButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DeleteProgramsButton.Name = "DeleteProgramsButton";
			this.DeleteProgramsButton.Size = new System.Drawing.Size(60, 22);
			this.DeleteProgramsButton.Text = "&Delete";
			this.DeleteProgramsButton.Click += new System.EventHandler(this.DeleteProgramsButton_Click);
			// 
			// panel2
			// 
			this.panel2.BackColor = System.Drawing.SystemColors.ControlDark;
			this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel2.Location = new System.Drawing.Point(532, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(1, 450);
			this.panel2.TabIndex = 5;
			// 
			// GameDefaultDetailsControl
			// 
			this.GameDefaultDetailsControl.BackColor = System.Drawing.SystemColors.Control;
			this.GameDefaultDetailsControl.Dock = System.Windows.Forms.DockStyle.Right;
			this.GameDefaultDetailsControl.Enabled = false;
			this.GameDefaultDetailsControl.Location = new System.Drawing.Point(533, 0);
			this.GameDefaultDetailsControl.Name = "GameDefaultDetailsControl";
			this.GameDefaultDetailsControl.Size = new System.Drawing.Size(154, 450);
			this.GameDefaultDetailsControl.TabIndex = 4;
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.SettingsTabPage.Controls.Add(this.IncludeEnabledCheckBox);
			this.SettingsTabPage.Controls.Add(this.MinimumInstanceCountLabel);
			this.SettingsTabPage.Controls.Add(this.MinimumInstanceCountNumericUpDown);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.SettingsTabPage.Size = new System.Drawing.Size(687, 450);
			this.SettingsTabPage.TabIndex = 2;
			this.SettingsTabPage.Text = "Options";
			// 
			// IncludeEnabledCheckBox
			// 
			this.IncludeEnabledCheckBox.AutoSize = true;
			this.IncludeEnabledCheckBox.Checked = true;
			this.IncludeEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.IncludeEnabledCheckBox.Location = new System.Drawing.Point(16, 15);
			this.IncludeEnabledCheckBox.Name = "IncludeEnabledCheckBox";
			this.IncludeEnabledCheckBox.Size = new System.Drawing.Size(103, 17);
			this.IncludeEnabledCheckBox.TabIndex = 23;
			this.IncludeEnabledCheckBox.Text = "Include Enabled";
			this.IncludeEnabledCheckBox.ThreeState = true;
			this.IncludeEnabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// MinimumInstanceCountLabel
			// 
			this.MinimumInstanceCountLabel.AutoSize = true;
			this.MinimumInstanceCountLabel.BackColor = System.Drawing.SystemColors.Control;
			this.MinimumInstanceCountLabel.Location = new System.Drawing.Point(13, 49);
			this.MinimumInstanceCountLabel.Name = "MinimumInstanceCountLabel";
			this.MinimumInstanceCountLabel.Size = new System.Drawing.Size(100, 13);
			this.MinimumInstanceCountLabel.TabIndex = 29;
			this.MinimumInstanceCountLabel.Text = "Minimum Instances:";
			// 
			// MinimumInstanceCountNumericUpDown
			// 
			this.MinimumInstanceCountNumericUpDown.Location = new System.Drawing.Point(119, 47);
			this.MinimumInstanceCountNumericUpDown.Name = "MinimumInstanceCountNumericUpDown";
			this.MinimumInstanceCountNumericUpDown.Size = new System.Drawing.Size(59, 20);
			this.MinimumInstanceCountNumericUpDown.TabIndex = 24;
			this.MinimumInstanceCountNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.MinimumInstanceCountNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
			// 
			// GameSettingsUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.GamesTabControl);
			this.Name = "GameSettingsUserControl";
			this.Size = new System.Drawing.Size(701, 482);
			this.Load += new System.EventHandler(this.GameSettingsUserControl_Load);
			this.GamesTabControl.ResumeLayout(false);
			this.GamesTabPage.ResumeLayout(false);
			this.GamesTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.GamesDataGridView)).EndInit();
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.GlobalSettingsTabPage.ResumeLayout(false);
			this.GlobalSettingsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).EndInit();
			this.ProgramsToolStrip.ResumeLayout(false);
			this.ProgramsToolStrip.PerformLayout();
			this.SettingsTabPage.ResumeLayout(false);
			this.SettingsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		System.Windows.Forms.TabControl GamesTabControl;
		System.Windows.Forms.TabPage GamesTabPage;
		System.Windows.Forms.DataGridView GamesDataGridView;
		private System.Windows.Forms.CheckBox IncludeEnabledCheckBox;
        private System.Windows.Forms.NumericUpDown MinimumInstanceCountNumericUpDown;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
		private System.Windows.Forms.OpenFileDialog AddGameOpenFileDialog;
		private System.Windows.Forms.Label ScanProgressLabel;
        private System.Windows.Forms.TabPage GlobalSettingsTabPage;
        private System.Windows.Forms.DataGridView ProgramsDataGridView;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
        private System.Windows.Forms.ToolStripButton DeleteGamesButton;
        private System.Windows.Forms.ToolStripButton SaveGamesButton;
        private System.Windows.Forms.ToolStripButton ScanGamesButton;
        private System.Windows.Forms.ToolStrip ProgramsToolStrip;
		private System.Windows.Forms.ToolStripButton RefreshProgramsButton;
		private System.Windows.Forms.TabPage SettingsTabPage;
		private System.Windows.Forms.ToolStripButton AddGameButton;
		private System.Windows.Forms.Panel panel1;
		private GameSettingDetailsUserControl GameDetailsControl;
		private System.Windows.Forms.Panel panel2;
		private GameSettingDetailsUserControl GameDefaultDetailsControl;
		private System.Windows.Forms.ToolStripButton StartGameButton;
		private System.Windows.Forms.ToolStripButton OpenGameButton;
		private System.Windows.Forms.DataGridViewCheckBoxColumn EnabledColumn;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn GameIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FilePathColumn;
		private System.Windows.Forms.ToolStripDropDownButton ShowGamesDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem ShowAllGamesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowEnabledGamesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowDisabledGamesMenuItem;
		private System.Windows.Forms.ToolStripButton ImportProgramsButton;
		private System.Windows.Forms.ToolStripButton ExportProgramsButton;
		private System.Windows.Forms.OpenFileDialog ImportOpenFileDialog;
		private System.Windows.Forms.SaveFileDialog ExportSaveFileDialog;
		private System.Windows.Forms.DataGridViewImageColumn ProgramImageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramProductNameColumn;
		private System.Windows.Forms.ToolStripButton DeleteProgramsButton;
	}
}
