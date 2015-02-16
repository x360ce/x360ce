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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle31 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle32 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle33 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle34 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle35 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle36 = new System.Windows.Forms.DataGridViewCellStyle();
            this.GamesTabControl = new System.Windows.Forms.TabControl();
            this.GamesTabPage = new System.Windows.Forms.TabPage();
            this.MySettingsDataGridView = new System.Windows.Forms.DataGridView();
            this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.GameIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ScanProgressLabel = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.MyGamesRefreshButton = new System.Windows.Forms.ToolStripButton();
            this.MyGamesDeleteButton = new System.Windows.Forms.ToolStripButton();
            this.MyGamesSaveButton = new System.Windows.Forms.ToolStripButton();
            this.MyGamesScanButton = new System.Windows.Forms.ToolStripButton();
            this.GlobalSettingsTabPage = new System.Windows.Forms.TabPage();
            this.GlobalSettingsDataGridView = new System.Windows.Forms.DataGridView();
            this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.GlobalSettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
            this.SettingsTabPage = new System.Windows.Forms.TabPage();
            this.IncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
            this.MinimumInstanceCountNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.HookMaskGroupBox = new System.Windows.Forms.GroupBox();
            this.HookMaskTextBox = new System.Windows.Forms.TextBox();
            this.HookDISABLECheckBox = new System.Windows.Forms.CheckBox();
            this.HookNameCheckBox = new System.Windows.Forms.CheckBox();
            this.HookSTOPCheckBox = new System.Windows.Forms.CheckBox();
            this.HookPIDVIDCheckBox = new System.Windows.Forms.CheckBox();
            this.HookDICheckBox = new System.Windows.Forms.CheckBox();
            this.HookWTCheckBox = new System.Windows.Forms.CheckBox();
            this.HookSACheckBox = new System.Windows.Forms.CheckBox();
            this.HookCOMCheckBox = new System.Windows.Forms.CheckBox();
            this.HookLLCheckBox = new System.Windows.Forms.CheckBox();
            this.InstalledFilesGroupBox = new System.Windows.Forms.GroupBox();
            this.XInputMaskTextBox = new System.Windows.Forms.TextBox();
            this.Xinput14CheckBox = new System.Windows.Forms.CheckBox();
            this.Xinput13CheckBox = new System.Windows.Forms.CheckBox();
            this.Xinput12CheckBox = new System.Windows.Forms.CheckBox();
            this.Xinput11CheckBox = new System.Windows.Forms.CheckBox();
            this.Xinput91CheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ProcessorArchitectureComboBox = new System.Windows.Forms.ComboBox();
            this.ProcessorArchitectureLabel = new System.Windows.Forms.Label();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.GameApplicationLocationTextBox = new System.Windows.Forms.TextBox();
            this.GameApplicationOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.GamesTabControl.SuspendLayout();
            this.GamesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.GlobalSettingsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GlobalSettingsDataGridView)).BeginInit();
            this.toolStrip2.SuspendLayout();
            this.SettingsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).BeginInit();
            this.HookMaskGroupBox.SuspendLayout();
            this.InstalledFilesGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
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
            this.GamesTabControl.Size = new System.Drawing.Size(543, 387);
            this.GamesTabControl.TabIndex = 19;
            this.GamesTabControl.SelectedIndexChanged += new System.EventHandler(this.GamesTabControl_SelectedIndexChanged);
            // 
            // GamesTabPage
            // 
            this.GamesTabPage.Controls.Add(this.MySettingsDataGridView);
            this.GamesTabPage.Controls.Add(this.ScanProgressLabel);
            this.GamesTabPage.Controls.Add(this.toolStrip1);
            this.GamesTabPage.Location = new System.Drawing.Point(4, 22);
            this.GamesTabPage.Name = "GamesTabPage";
            this.GamesTabPage.Size = new System.Drawing.Size(535, 361);
            this.GamesTabPage.TabIndex = 0;
            this.GamesTabPage.Text = "My Game Settings";
            this.GamesTabPage.UseVisualStyleBackColor = true;
            // 
            // MySettingsDataGridView
            // 
            this.MySettingsDataGridView.AllowUserToAddRows = false;
            this.MySettingsDataGridView.AllowUserToDeleteRows = false;
            this.MySettingsDataGridView.AllowUserToResizeRows = false;
            this.MySettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.MySettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MySettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle31.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle31.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle31.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle31.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle31.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle31.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.MySettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle31;
            this.MySettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.MySettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MyIconColumn,
            this.GameIdColumn,
            this.FileNameColumn,
            this.ProductNameColumn});
            dataGridViewCellStyle32.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle32.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle32.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle32.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle32.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle32.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle32.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.MySettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle32;
            this.MySettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MySettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.MySettingsDataGridView.Location = new System.Drawing.Point(0, 25);
            this.MySettingsDataGridView.MultiSelect = false;
            this.MySettingsDataGridView.Name = "MySettingsDataGridView";
            this.MySettingsDataGridView.ReadOnly = true;
            dataGridViewCellStyle33.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle33.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle33.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle33.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle33.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle33.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle33.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.MySettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle33;
            this.MySettingsDataGridView.RowHeadersVisible = false;
            this.MySettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.MySettingsDataGridView.Size = new System.Drawing.Size(535, 317);
            this.MySettingsDataGridView.TabIndex = 0;
            this.MySettingsDataGridView.DataSourceChanged += new System.EventHandler(this.ProgramsDataGridView_DataSourceChanged);
            this.MySettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.ProgramsDataGridView_CellFormatting);
            this.MySettingsDataGridView.SelectionChanged += new System.EventHandler(this.ProgramsDataGridView_SelectionChanged);
            // 
            // MyIconColumn
            // 
            this.MyIconColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.MyIconColumn.HeaderText = "";
            this.MyIconColumn.MinimumWidth = 24;
            this.MyIconColumn.Name = "MyIconColumn";
            this.MyIconColumn.ReadOnly = true;
            this.MyIconColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
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
            this.FileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.FileNameColumn.DataPropertyName = "FileName";
            this.FileNameColumn.FillWeight = 30F;
            this.FileNameColumn.HeaderText = "File Name";
            this.FileNameColumn.Name = "FileNameColumn";
            this.FileNameColumn.ReadOnly = true;
            // 
            // ProductNameColumn
            // 
            this.ProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ProductNameColumn.DataPropertyName = "FileProductName";
            this.ProductNameColumn.FillWeight = 70F;
            this.ProductNameColumn.HeaderText = "Product Name";
            this.ProductNameColumn.Name = "ProductNameColumn";
            this.ProductNameColumn.ReadOnly = true;
            // 
            // ScanProgressLabel
            // 
            this.ScanProgressLabel.AutoSize = true;
            this.ScanProgressLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ScanProgressLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ScanProgressLabel.Location = new System.Drawing.Point(0, 342);
            this.ScanProgressLabel.Name = "ScanProgressLabel";
            this.ScanProgressLabel.Padding = new System.Windows.Forms.Padding(3);
            this.ScanProgressLabel.Size = new System.Drawing.Size(111, 19);
            this.ScanProgressLabel.TabIndex = 0;
            this.ScanProgressLabel.Text = "[ScanProgressLabel]";
            this.ScanProgressLabel.Visible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MyGamesRefreshButton,
            this.MyGamesDeleteButton,
            this.MyGamesSaveButton,
            this.MyGamesScanButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip1.Size = new System.Drawing.Size(535, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "MySettingsToolStrip";
            // 
            // MyGamesRefreshButton
            // 
            this.MyGamesRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MyGamesRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
            this.MyGamesRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MyGamesRefreshButton.Name = "MyGamesRefreshButton";
            this.MyGamesRefreshButton.Size = new System.Drawing.Size(66, 22);
            this.MyGamesRefreshButton.Text = "&Refresh";
            this.MyGamesRefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // MyGamesDeleteButton
            // 
            this.MyGamesDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MyGamesDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
            this.MyGamesDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MyGamesDeleteButton.Name = "MyGamesDeleteButton";
            this.MyGamesDeleteButton.Size = new System.Drawing.Size(60, 22);
            this.MyGamesDeleteButton.Text = "&Delete";
            // 
            // MyGamesSaveButton
            // 
            this.MyGamesSaveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MyGamesSaveButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
            this.MyGamesSaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MyGamesSaveButton.Name = "MyGamesSaveButton";
            this.MyGamesSaveButton.Size = new System.Drawing.Size(51, 22);
            this.MyGamesSaveButton.Text = "&Save";
            // 
            // MyGamesScanButton
            // 
            this.MyGamesScanButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MyGamesScanButton.Image = global::x360ce.App.Properties.Resources.folder_view_16x16;
            this.MyGamesScanButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MyGamesScanButton.Name = "MyGamesScanButton";
            this.MyGamesScanButton.Size = new System.Drawing.Size(52, 22);
            this.MyGamesScanButton.Text = "&Scan";
            this.MyGamesScanButton.Click += new System.EventHandler(this.ScanButton_Click);
            // 
            // GlobalSettingsTabPage
            // 
            this.GlobalSettingsTabPage.Controls.Add(this.GlobalSettingsDataGridView);
            this.GlobalSettingsTabPage.Controls.Add(this.toolStrip2);
            this.GlobalSettingsTabPage.Location = new System.Drawing.Point(4, 22);
            this.GlobalSettingsTabPage.Name = "GlobalSettingsTabPage";
            this.GlobalSettingsTabPage.Size = new System.Drawing.Size(535, 361);
            this.GlobalSettingsTabPage.TabIndex = 1;
            this.GlobalSettingsTabPage.Text = "Default Settings for Most Popular Games";
            this.GlobalSettingsTabPage.UseVisualStyleBackColor = true;
            // 
            // GlobalSettingsDataGridView
            // 
            this.GlobalSettingsDataGridView.AllowUserToAddRows = false;
            this.GlobalSettingsDataGridView.AllowUserToDeleteRows = false;
            this.GlobalSettingsDataGridView.AllowUserToResizeRows = false;
            this.GlobalSettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.GlobalSettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.GlobalSettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle34.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle34.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle34.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle34.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle34.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle34.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.GlobalSettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle34;
            this.GlobalSettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.GlobalSettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewImageColumn1,
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3});
            dataGridViewCellStyle35.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle35.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle35.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle35.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle35.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle35.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle35.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.GlobalSettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle35;
            this.GlobalSettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GlobalSettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.GlobalSettingsDataGridView.Location = new System.Drawing.Point(0, 25);
            this.GlobalSettingsDataGridView.MultiSelect = false;
            this.GlobalSettingsDataGridView.Name = "GlobalSettingsDataGridView";
            this.GlobalSettingsDataGridView.ReadOnly = true;
            dataGridViewCellStyle36.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle36.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle36.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle36.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle36.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle36.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle36.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.GlobalSettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle36;
            this.GlobalSettingsDataGridView.RowHeadersVisible = false;
            this.GlobalSettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.GlobalSettingsDataGridView.Size = new System.Drawing.Size(535, 336);
            this.GlobalSettingsDataGridView.TabIndex = 1;
            this.GlobalSettingsDataGridView.SelectionChanged += new System.EventHandler(this.GlobalSettingsDataGridView_SelectionChanged);
            // 
            // dataGridViewImageColumn1
            // 
            this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewImageColumn1.HeaderText = "";
            this.dataGridViewImageColumn1.MinimumWidth = 24;
            this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            this.dataGridViewImageColumn1.ReadOnly = true;
            this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.dataGridViewImageColumn1.Visible = false;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "GameId";
            this.dataGridViewTextBoxColumn1.HeaderText = "ID";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Visible = false;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "FileName";
            this.dataGridViewTextBoxColumn2.FillWeight = 30F;
            this.dataGridViewTextBoxColumn2.HeaderText = "File Name";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "FileProductName";
            this.dataGridViewTextBoxColumn3.FillWeight = 70F;
            this.dataGridViewTextBoxColumn3.HeaderText = "Product Name";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // toolStrip2
            // 
            this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GlobalSettingsRefreshButton});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip2.Size = new System.Drawing.Size(535, 25);
            this.toolStrip2.TabIndex = 3;
            this.toolStrip2.Text = "MySettingsToolStrip";
            // 
            // GlobalSettingsRefreshButton
            // 
            this.GlobalSettingsRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.GlobalSettingsRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
            this.GlobalSettingsRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.GlobalSettingsRefreshButton.Name = "GlobalSettingsRefreshButton";
            this.GlobalSettingsRefreshButton.Size = new System.Drawing.Size(66, 22);
            this.GlobalSettingsRefreshButton.Text = "&Refresh";
            this.GlobalSettingsRefreshButton.Click += new System.EventHandler(this.GlobalSettingsRefreshButton_Click);
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
            this.SettingsTabPage.Size = new System.Drawing.Size(535, 361);
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
            // HookMaskGroupBox
            // 
            this.HookMaskGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HookMaskGroupBox.Controls.Add(this.HookMaskTextBox);
            this.HookMaskGroupBox.Controls.Add(this.HookDISABLECheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookNameCheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookSTOPCheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookPIDVIDCheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookDICheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookWTCheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookSACheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookCOMCheckBox);
            this.HookMaskGroupBox.Controls.Add(this.HookLLCheckBox);
            this.HookMaskGroupBox.Location = new System.Drawing.Point(552, 3);
            this.HookMaskGroupBox.Name = "HookMaskGroupBox";
            this.HookMaskGroupBox.Size = new System.Drawing.Size(146, 253);
            this.HookMaskGroupBox.TabIndex = 20;
            this.HookMaskGroupBox.TabStop = false;
            this.HookMaskGroupBox.Text = "Hook Mask";
            // 
            // HookMaskTextBox
            // 
            this.HookMaskTextBox.Location = new System.Drawing.Point(6, 226);
            this.HookMaskTextBox.Name = "HookMaskTextBox";
            this.HookMaskTextBox.ReadOnly = true;
            this.HookMaskTextBox.Size = new System.Drawing.Size(71, 20);
            this.HookMaskTextBox.TabIndex = 1;
            this.HookMaskTextBox.Text = "0x00000000";
            // 
            // HookDISABLECheckBox
            // 
            this.HookDISABLECheckBox.AutoSize = true;
            this.HookDISABLECheckBox.Location = new System.Drawing.Point(6, 203);
            this.HookDISABLECheckBox.Name = "HookDISABLECheckBox";
            this.HookDISABLECheckBox.Size = new System.Drawing.Size(71, 17);
            this.HookDISABLECheckBox.TabIndex = 0;
            this.HookDISABLECheckBox.Text = "DISABLE";
            this.HookDISABLECheckBox.UseVisualStyleBackColor = true;
            // 
            // HookNameCheckBox
            // 
            this.HookNameCheckBox.AutoSize = true;
            this.HookNameCheckBox.Location = new System.Drawing.Point(6, 111);
            this.HookNameCheckBox.Name = "HookNameCheckBox";
            this.HookNameCheckBox.Size = new System.Drawing.Size(57, 17);
            this.HookNameCheckBox.TabIndex = 0;
            this.HookNameCheckBox.Text = "NAME";
            this.HookNameCheckBox.UseVisualStyleBackColor = true;
            // 
            // HookSTOPCheckBox
            // 
            this.HookSTOPCheckBox.AutoSize = true;
            this.HookSTOPCheckBox.Location = new System.Drawing.Point(6, 180);
            this.HookSTOPCheckBox.Name = "HookSTOPCheckBox";
            this.HookSTOPCheckBox.Size = new System.Drawing.Size(55, 17);
            this.HookSTOPCheckBox.TabIndex = 0;
            this.HookSTOPCheckBox.Text = "STOP";
            this.HookSTOPCheckBox.UseVisualStyleBackColor = true;
            // 
            // HookPIDVIDCheckBox
            // 
            this.HookPIDVIDCheckBox.AutoSize = true;
            this.HookPIDVIDCheckBox.Location = new System.Drawing.Point(6, 88);
            this.HookPIDVIDCheckBox.Name = "HookPIDVIDCheckBox";
            this.HookPIDVIDCheckBox.Size = new System.Drawing.Size(62, 17);
            this.HookPIDVIDCheckBox.TabIndex = 0;
            this.HookPIDVIDCheckBox.Text = "PIDVID";
            this.HookPIDVIDCheckBox.UseVisualStyleBackColor = true;
            // 
            // HookDICheckBox
            // 
            this.HookDICheckBox.AutoSize = true;
            this.HookDICheckBox.Location = new System.Drawing.Point(6, 65);
            this.HookDICheckBox.Name = "HookDICheckBox";
            this.HookDICheckBox.Size = new System.Drawing.Size(37, 17);
            this.HookDICheckBox.TabIndex = 0;
            this.HookDICheckBox.Text = "DI";
            this.HookDICheckBox.UseVisualStyleBackColor = true;
            // 
            // HookWTCheckBox
            // 
            this.HookWTCheckBox.AutoSize = true;
            this.HookWTCheckBox.Location = new System.Drawing.Point(6, 157);
            this.HookWTCheckBox.Name = "HookWTCheckBox";
            this.HookWTCheckBox.Size = new System.Drawing.Size(122, 17);
            this.HookWTCheckBox.TabIndex = 0;
            this.HookWTCheckBox.Text = "WT (WinVerifyTrust)";
            this.HookWTCheckBox.UseVisualStyleBackColor = true;
            // 
            // HookSACheckBox
            // 
            this.HookSACheckBox.AutoSize = true;
            this.HookSACheckBox.Location = new System.Drawing.Point(6, 134);
            this.HookSACheckBox.Name = "HookSACheckBox";
            this.HookSACheckBox.Size = new System.Drawing.Size(94, 17);
            this.HookSACheckBox.TabIndex = 0;
            this.HookSACheckBox.Text = "SA (SetupAPI)";
            this.HookSACheckBox.UseVisualStyleBackColor = true;
            // 
            // HookCOMCheckBox
            // 
            this.HookCOMCheckBox.AutoSize = true;
            this.HookCOMCheckBox.Location = new System.Drawing.Point(6, 42);
            this.HookCOMCheckBox.Name = "HookCOMCheckBox";
            this.HookCOMCheckBox.Size = new System.Drawing.Size(50, 17);
            this.HookCOMCheckBox.TabIndex = 0;
            this.HookCOMCheckBox.Text = "COM";
            this.HookCOMCheckBox.UseVisualStyleBackColor = true;
            // 
            // HookLLCheckBox
            // 
            this.HookLLCheckBox.AutoSize = true;
            this.HookLLCheckBox.Location = new System.Drawing.Point(6, 19);
            this.HookLLCheckBox.Name = "HookLLCheckBox";
            this.HookLLCheckBox.Size = new System.Drawing.Size(105, 17);
            this.HookLLCheckBox.TabIndex = 0;
            this.HookLLCheckBox.Tag = "";
            this.HookLLCheckBox.Text = "LL (Load Library)";
            this.HookLLCheckBox.UseVisualStyleBackColor = true;
            // 
            // InstalledFilesGroupBox
            // 
            this.InstalledFilesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.InstalledFilesGroupBox.Controls.Add(this.XInputMaskTextBox);
            this.InstalledFilesGroupBox.Controls.Add(this.Xinput14CheckBox);
            this.InstalledFilesGroupBox.Controls.Add(this.Xinput13CheckBox);
            this.InstalledFilesGroupBox.Controls.Add(this.Xinput12CheckBox);
            this.InstalledFilesGroupBox.Controls.Add(this.Xinput11CheckBox);
            this.InstalledFilesGroupBox.Controls.Add(this.Xinput91CheckBox);
            this.InstalledFilesGroupBox.Location = new System.Drawing.Point(552, 262);
            this.InstalledFilesGroupBox.Name = "InstalledFilesGroupBox";
            this.InstalledFilesGroupBox.Size = new System.Drawing.Size(146, 162);
            this.InstalledFilesGroupBox.TabIndex = 21;
            this.InstalledFilesGroupBox.TabStop = false;
            this.InstalledFilesGroupBox.Text = "Installed XInput Files";
            // 
            // XInputMaskTextBox
            // 
            this.XInputMaskTextBox.Location = new System.Drawing.Point(6, 134);
            this.XInputMaskTextBox.Name = "XInputMaskTextBox";
            this.XInputMaskTextBox.ReadOnly = true;
            this.XInputMaskTextBox.Size = new System.Drawing.Size(71, 20);
            this.XInputMaskTextBox.TabIndex = 1;
            this.XInputMaskTextBox.Text = "0x00000000";
            // 
            // Xinput14CheckBox
            // 
            this.Xinput14CheckBox.AutoSize = true;
            this.Xinput14CheckBox.Location = new System.Drawing.Point(6, 111);
            this.Xinput14CheckBox.Name = "Xinput14CheckBox";
            this.Xinput14CheckBox.Size = new System.Drawing.Size(85, 17);
            this.Xinput14CheckBox.TabIndex = 0;
            this.Xinput14CheckBox.Text = "xinput1_4.dll";
            this.Xinput14CheckBox.UseVisualStyleBackColor = true;
            this.Xinput14CheckBox.CheckedChanged += new System.EventHandler(this.Xinput14CheckBox_CheckedChanged);
            // 
            // Xinput13CheckBox
            // 
            this.Xinput13CheckBox.AutoSize = true;
            this.Xinput13CheckBox.Location = new System.Drawing.Point(6, 88);
            this.Xinput13CheckBox.Name = "Xinput13CheckBox";
            this.Xinput13CheckBox.Size = new System.Drawing.Size(85, 17);
            this.Xinput13CheckBox.TabIndex = 0;
            this.Xinput13CheckBox.Text = "xinput1_3.dll";
            this.Xinput13CheckBox.UseVisualStyleBackColor = true;
            this.Xinput13CheckBox.CheckedChanged += new System.EventHandler(this.Xinput13CheckBox_CheckedChanged);
            // 
            // Xinput12CheckBox
            // 
            this.Xinput12CheckBox.AutoSize = true;
            this.Xinput12CheckBox.Location = new System.Drawing.Point(6, 65);
            this.Xinput12CheckBox.Name = "Xinput12CheckBox";
            this.Xinput12CheckBox.Size = new System.Drawing.Size(85, 17);
            this.Xinput12CheckBox.TabIndex = 0;
            this.Xinput12CheckBox.Text = "xinput1_2.dll";
            this.Xinput12CheckBox.UseVisualStyleBackColor = true;
            this.Xinput12CheckBox.CheckedChanged += new System.EventHandler(this.Xinput12CheckBox_CheckedChanged);
            // 
            // Xinput11CheckBox
            // 
            this.Xinput11CheckBox.AutoSize = true;
            this.Xinput11CheckBox.Location = new System.Drawing.Point(6, 42);
            this.Xinput11CheckBox.Name = "Xinput11CheckBox";
            this.Xinput11CheckBox.Size = new System.Drawing.Size(85, 17);
            this.Xinput11CheckBox.TabIndex = 0;
            this.Xinput11CheckBox.Text = "xinput1_1.dll";
            this.Xinput11CheckBox.UseVisualStyleBackColor = true;
            this.Xinput11CheckBox.CheckedChanged += new System.EventHandler(this.Xinput11CheckBox_CheckedChanged);
            // 
            // Xinput91CheckBox
            // 
            this.Xinput91CheckBox.AutoSize = true;
            this.Xinput91CheckBox.Location = new System.Drawing.Point(6, 19);
            this.Xinput91CheckBox.Name = "Xinput91CheckBox";
            this.Xinput91CheckBox.Size = new System.Drawing.Size(97, 17);
            this.Xinput91CheckBox.TabIndex = 0;
            this.Xinput91CheckBox.Text = "xinput9_1_0.dll";
            this.Xinput91CheckBox.UseVisualStyleBackColor = true;
            this.Xinput91CheckBox.CheckedChanged += new System.EventHandler(this.Xinput91CheckBox_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.ProcessorArchitectureComboBox);
            this.groupBox1.Controls.Add(this.ProcessorArchitectureLabel);
            this.groupBox1.Controls.Add(this.BrowseButton);
            this.groupBox1.Controls.Add(this.GameApplicationLocationTextBox);
            this.groupBox1.Location = new System.Drawing.Point(3, 396);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(543, 83);
            this.groupBox1.TabIndex = 21;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "File Details";
            // 
            // ProcessorArchitectureComboBox
            // 
            this.ProcessorArchitectureComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ProcessorArchitectureComboBox.Enabled = false;
            this.ProcessorArchitectureComboBox.FormattingEnabled = true;
            this.ProcessorArchitectureComboBox.Location = new System.Drawing.Point(129, 50);
            this.ProcessorArchitectureComboBox.Name = "ProcessorArchitectureComboBox";
            this.ProcessorArchitectureComboBox.Size = new System.Drawing.Size(80, 21);
            this.ProcessorArchitectureComboBox.TabIndex = 30;
            // 
            // ProcessorArchitectureLabel
            // 
            this.ProcessorArchitectureLabel.AutoSize = true;
            this.ProcessorArchitectureLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ProcessorArchitectureLabel.Location = new System.Drawing.Point(6, 53);
            this.ProcessorArchitectureLabel.Name = "ProcessorArchitectureLabel";
            this.ProcessorArchitectureLabel.Size = new System.Drawing.Size(117, 13);
            this.ProcessorArchitectureLabel.TabIndex = 29;
            this.ProcessorArchitectureLabel.Text = "Processor Architecture:";
            // 
            // BrowseButton
            // 
            this.BrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowseButton.Image = global::x360ce.App.Properties.Resources.folder_view_16x16;
            this.BrowseButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BrowseButton.Location = new System.Drawing.Point(462, 19);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(75, 25);
            this.BrowseButton.TabIndex = 26;
            this.BrowseButton.Text = "&Browse...";
            this.BrowseButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BrowseButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // GameApplicationLocationTextBox
            // 
            this.GameApplicationLocationTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GameApplicationLocationTextBox.Location = new System.Drawing.Point(6, 22);
            this.GameApplicationLocationTextBox.Name = "GameApplicationLocationTextBox";
            this.GameApplicationLocationTextBox.ReadOnly = true;
            this.GameApplicationLocationTextBox.Size = new System.Drawing.Size(450, 20);
            this.GameApplicationLocationTextBox.TabIndex = 22;
            // 
            // GameApplicationOpenFileDialog
            // 
            this.GameApplicationOpenFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.ProgramOpenFileDialog_FileOk);
            // 
            // GameSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.InstalledFilesGroupBox);
            this.Controls.Add(this.HookMaskGroupBox);
            this.Controls.Add(this.GamesTabControl);
            this.Name = "GameSettingsUserControl";
            this.Size = new System.Drawing.Size(701, 482);
            this.Load += new System.EventHandler(this.GamesControl_Load);
            this.GamesTabControl.ResumeLayout(false);
            this.GamesTabPage.ResumeLayout(false);
            this.GamesTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.GlobalSettingsTabPage.ResumeLayout(false);
            this.GlobalSettingsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GlobalSettingsDataGridView)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.SettingsTabPage.ResumeLayout(false);
            this.SettingsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).EndInit();
            this.HookMaskGroupBox.ResumeLayout(false);
            this.HookMaskGroupBox.PerformLayout();
            this.InstalledFilesGroupBox.ResumeLayout(false);
            this.InstalledFilesGroupBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		System.Windows.Forms.TabControl GamesTabControl;
		System.Windows.Forms.TabPage GamesTabPage;
		System.Windows.Forms.DataGridView MySettingsDataGridView;
		System.Windows.Forms.GroupBox HookMaskGroupBox;
		System.Windows.Forms.CheckBox HookDISABLECheckBox;
		System.Windows.Forms.CheckBox HookNameCheckBox;
		System.Windows.Forms.CheckBox HookSTOPCheckBox;
		System.Windows.Forms.CheckBox HookPIDVIDCheckBox;
		System.Windows.Forms.CheckBox HookDICheckBox;
		System.Windows.Forms.CheckBox HookWTCheckBox;
		System.Windows.Forms.CheckBox HookSACheckBox;
		System.Windows.Forms.CheckBox HookCOMCheckBox;
		System.Windows.Forms.CheckBox HookLLCheckBox;
		System.Windows.Forms.TextBox HookMaskTextBox;
		System.Windows.Forms.GroupBox InstalledFilesGroupBox;
		System.Windows.Forms.TextBox XInputMaskTextBox;
		System.Windows.Forms.CheckBox Xinput14CheckBox;
		System.Windows.Forms.CheckBox Xinput13CheckBox;
		System.Windows.Forms.CheckBox Xinput12CheckBox;
		System.Windows.Forms.CheckBox Xinput11CheckBox;
		System.Windows.Forms.CheckBox Xinput91CheckBox;
		private System.Windows.Forms.CheckBox IncludeEnabledCheckBox;
        private System.Windows.Forms.NumericUpDown MinimumInstanceCountNumericUpDown;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.TextBox GameApplicationLocationTextBox;
		private System.Windows.Forms.OpenFileDialog GameApplicationOpenFileDialog;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn GameIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FileNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.Label ScanProgressLabel;
        private System.Windows.Forms.TabPage GlobalSettingsTabPage;
        private System.Windows.Forms.DataGridView GlobalSettingsDataGridView;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton MyGamesRefreshButton;
        private System.Windows.Forms.ToolStripButton MyGamesDeleteButton;
        private System.Windows.Forms.ToolStripButton MyGamesSaveButton;
        private System.Windows.Forms.ToolStripButton MyGamesScanButton;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton GlobalSettingsRefreshButton;
        private System.Windows.Forms.Label ProcessorArchitectureLabel;
        private System.Windows.Forms.ComboBox ProcessorArchitectureComboBox;
        private System.Windows.Forms.TabPage SettingsTabPage;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
	}
}
