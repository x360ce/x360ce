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
			this.MySettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.EnabledColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.GameIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FilePathColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.ScanButton = new System.Windows.Forms.ToolStripButton();
			this.AddButton = new System.Windows.Forms.ToolStripButton();
			this.DeleteButton = new System.Windows.Forms.ToolStripButton();
			this.SaveButton = new System.Windows.Forms.ToolStripButton();
			this.StartButton = new System.Windows.Forms.ToolStripButton();
			this.FolderButton = new System.Windows.Forms.ToolStripButton();
			this.ShowToolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.ShowAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowEnabledToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowDisabledToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ScanProgressLabel = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.GameDetailsControl = new x360ce.App.Controls.GameSettingDetailsUserControl();
			this.GlobalSettingsTabPage = new System.Windows.Forms.TabPage();
			this.GlobalSettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.ProgramImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.ProgramIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolStrip2 = new System.Windows.Forms.ToolStrip();
			this.GlobalSettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.ImportButton = new System.Windows.Forms.ToolStripButton();
			this.ExportButton = new System.Windows.Forms.ToolStripButton();
			this.panel2 = new System.Windows.Forms.Panel();
			this.GameDefaultDetailsControl = new x360ce.App.Controls.GameSettingDetailsUserControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.IncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
			this.MinimumInstanceCountNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.GameApplicationOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ImportOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.GamesTabControl.SuspendLayout();
			this.GamesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).BeginInit();
			this.toolStrip1.SuspendLayout();
			this.GlobalSettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.GlobalSettingsDataGridView)).BeginInit();
			this.toolStrip2.SuspendLayout();
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
			this.GamesTabPage.Controls.Add(this.MySettingsDataGridView);
			this.GamesTabPage.Controls.Add(this.toolStrip1);
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
			// MySettingsDataGridView
			// 
			this.MySettingsDataGridView.AllowUserToAddRows = false;
			this.MySettingsDataGridView.AllowUserToDeleteRows = false;
			this.MySettingsDataGridView.AllowUserToResizeRows = false;
			this.MySettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.MySettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.MySettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MySettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.MySettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MySettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
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
			this.MySettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.MySettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MySettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.MySettingsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.MySettingsDataGridView.MultiSelect = false;
			this.MySettingsDataGridView.Name = "MySettingsDataGridView";
			this.MySettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MySettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.MySettingsDataGridView.RowHeadersVisible = false;
			this.MySettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MySettingsDataGridView.Size = new System.Drawing.Size(532, 385);
			this.MySettingsDataGridView.TabIndex = 0;
			this.MySettingsDataGridView.DataSourceChanged += new System.EventHandler(this.ProgramsDataGridView_DataSourceChanged);
			this.MySettingsDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.MySettingsDataGridView_CellClick);
			this.MySettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.MySettingsDataGridView_CellFormatting);
			this.MySettingsDataGridView.SelectionChanged += new System.EventHandler(this.ProgramsDataGridView_SelectionChanged);
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
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScanButton,
            this.AddButton,
            this.DeleteButton,
            this.SaveButton,
            this.StartButton,
            this.FolderButton,
            this.ShowToolStripDropDownButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(532, 25);
			this.toolStrip1.TabIndex = 2;
			this.toolStrip1.Text = "MySettingsToolStrip";
			// 
			// ScanButton
			// 
			this.ScanButton.Image = global::x360ce.App.Properties.Resources.folder_view_16x16;
			this.ScanButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ScanButton.Name = "ScanButton";
			this.ScanButton.Size = new System.Drawing.Size(52, 22);
			this.ScanButton.Text = "&Scan";
			this.ScanButton.Click += new System.EventHandler(this.ScanButton_Click);
			// 
			// AddButton
			// 
			this.AddButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AddButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddButton.Name = "AddButton";
			this.AddButton.Size = new System.Drawing.Size(58, 22);
			this.AddButton.Text = "Add...";
			this.AddButton.Click += new System.EventHandler(this.MyGamesAddButton_Click);
			// 
			// DeleteButton
			// 
			this.DeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DeleteButton.Name = "DeleteButton";
			this.DeleteButton.Size = new System.Drawing.Size(60, 22);
			this.DeleteButton.Text = "&Delete";
			this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.SaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(51, 22);
			this.SaveButton.Text = "&Save";
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// StartButton
			// 
			this.StartButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.StartButton.Image = global::x360ce.App.Properties.Resources.launch_16x16;
			this.StartButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StartButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(51, 22);
			this.StartButton.Text = "Start";
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// FolderButton
			// 
			this.FolderButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.FolderButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
			this.FolderButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.FolderButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FolderButton.Name = "FolderButton";
			this.FolderButton.Size = new System.Drawing.Size(69, 22);
			this.FolderButton.Text = "Folder...";
			this.FolderButton.Click += new System.EventHandler(this.FolderButton_Click);
			// 
			// ShowToolStripDropDownButton
			// 
			this.ShowToolStripDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowAllToolStripMenuItem,
            this.ShowEnabledToolStripMenuItem,
            this.ShowDisabledToolStripMenuItem});
			this.ShowToolStripDropDownButton.Image = global::x360ce.App.Properties.Resources.checkbox_undefined_16x16;
			this.ShowToolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ShowToolStripDropDownButton.Name = "ShowToolStripDropDownButton";
			this.ShowToolStripDropDownButton.Size = new System.Drawing.Size(85, 22);
			this.ShowToolStripDropDownButton.Text = "Show: All";
			// 
			// ShowAllToolStripMenuItem
			// 
			this.ShowAllToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_undefined_16x16;
			this.ShowAllToolStripMenuItem.Name = "ShowAllToolStripMenuItem";
			this.ShowAllToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowAllToolStripMenuItem.Text = "Show: All";
			this.ShowAllToolStripMenuItem.Click += new System.EventHandler(this.ShowToolStripMenuItem_Click);
			// 
			// ShowEnabledToolStripMenuItem
			// 
			this.ShowEnabledToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_16x16;
			this.ShowEnabledToolStripMenuItem.Name = "ShowEnabledToolStripMenuItem";
			this.ShowEnabledToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowEnabledToolStripMenuItem.Text = "Show: Enabled";
			this.ShowEnabledToolStripMenuItem.Click += new System.EventHandler(this.ShowToolStripMenuItem_Click);
			// 
			// ShowDisabledToolStripMenuItem
			// 
			this.ShowDisabledToolStripMenuItem.Image = global::x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			this.ShowDisabledToolStripMenuItem.Name = "ShowDisabledToolStripMenuItem";
			this.ShowDisabledToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.ShowDisabledToolStripMenuItem.Text = "Show: Disabled";
			this.ShowDisabledToolStripMenuItem.Click += new System.EventHandler(this.ShowToolStripMenuItem_Click);
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
			this.GlobalSettingsTabPage.Controls.Add(this.GlobalSettingsDataGridView);
			this.GlobalSettingsTabPage.Controls.Add(this.toolStrip2);
			this.GlobalSettingsTabPage.Controls.Add(this.panel2);
			this.GlobalSettingsTabPage.Controls.Add(this.GameDefaultDetailsControl);
			this.GlobalSettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.GlobalSettingsTabPage.Name = "GlobalSettingsTabPage";
			this.GlobalSettingsTabPage.Size = new System.Drawing.Size(687, 450);
			this.GlobalSettingsTabPage.TabIndex = 1;
			this.GlobalSettingsTabPage.Text = "Default Settings for Most Popular Games";
			this.GlobalSettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// GlobalSettingsDataGridView
			// 
			this.GlobalSettingsDataGridView.AllowUserToAddRows = false;
			this.GlobalSettingsDataGridView.AllowUserToDeleteRows = false;
			this.GlobalSettingsDataGridView.AllowUserToOrderColumns = true;
			this.GlobalSettingsDataGridView.AllowUserToResizeRows = false;
			this.GlobalSettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.GlobalSettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.GlobalSettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GlobalSettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.GlobalSettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GlobalSettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
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
			this.GlobalSettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle5;
			this.GlobalSettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GlobalSettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.GlobalSettingsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.GlobalSettingsDataGridView.MultiSelect = false;
			this.GlobalSettingsDataGridView.Name = "GlobalSettingsDataGridView";
			this.GlobalSettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GlobalSettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
			this.GlobalSettingsDataGridView.RowHeadersVisible = false;
			this.GlobalSettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.GlobalSettingsDataGridView.Size = new System.Drawing.Size(532, 425);
			this.GlobalSettingsDataGridView.TabIndex = 1;
			this.GlobalSettingsDataGridView.SelectionChanged += new System.EventHandler(this.GlobalSettingsDataGridView_SelectionChanged);
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
			// toolStrip2
			// 
			this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GlobalSettingsRefreshButton,
            this.ImportButton,
            this.ExportButton});
			this.toolStrip2.Location = new System.Drawing.Point(0, 0);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip2.Size = new System.Drawing.Size(532, 25);
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
			// ImportButton
			// 
			this.ImportButton.Image = global::x360ce.App.Properties.Resources.data_into_16x16;
			this.ImportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ImportButton.Name = "ImportButton";
			this.ImportButton.Size = new System.Drawing.Size(72, 22);
			this.ImportButton.Text = "&Import...";
			this.ImportButton.Click += new System.EventHandler(this.ImportButton_Click);
			// 
			// ExportButton
			// 
			this.ExportButton.Image = global::x360ce.App.Properties.Resources.data_out_16x16;
			this.ExportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ExportButton.Name = "ExportButton";
			this.ExportButton.Size = new System.Drawing.Size(69, 22);
			this.ExportButton.Text = "&Export...";
			this.ExportButton.Click += new System.EventHandler(this.ExportButton_Click);
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
			// GameApplicationOpenFileDialog
			// 
			this.GameApplicationOpenFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.ProgramOpenFileDialog_FileOk);
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
			this.ResumeLayout(false);

		}

		#endregion

		System.Windows.Forms.TabControl GamesTabControl;
		System.Windows.Forms.TabPage GamesTabPage;
		System.Windows.Forms.DataGridView MySettingsDataGridView;
		private System.Windows.Forms.CheckBox IncludeEnabledCheckBox;
        private System.Windows.Forms.NumericUpDown MinimumInstanceCountNumericUpDown;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
		private System.Windows.Forms.OpenFileDialog GameApplicationOpenFileDialog;
		private System.Windows.Forms.Label ScanProgressLabel;
        private System.Windows.Forms.TabPage GlobalSettingsTabPage;
        private System.Windows.Forms.DataGridView GlobalSettingsDataGridView;
		private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton DeleteButton;
        private System.Windows.Forms.ToolStripButton SaveButton;
        private System.Windows.Forms.ToolStripButton ScanButton;
        private System.Windows.Forms.ToolStrip toolStrip2;
		private System.Windows.Forms.ToolStripButton GlobalSettingsRefreshButton;
		private System.Windows.Forms.TabPage SettingsTabPage;
		private System.Windows.Forms.ToolStripButton AddButton;
		private System.Windows.Forms.Panel panel1;
		private GameSettingDetailsUserControl GameDetailsControl;
		private System.Windows.Forms.Panel panel2;
		private GameSettingDetailsUserControl GameDefaultDetailsControl;
		private System.Windows.Forms.ToolStripButton StartButton;
		private System.Windows.Forms.ToolStripButton FolderButton;
		private System.Windows.Forms.DataGridViewCheckBoxColumn EnabledColumn;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn GameIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn FilePathColumn;
		private System.Windows.Forms.ToolStripDropDownButton ShowToolStripDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem ShowAllToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowEnabledToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ShowDisabledToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton ImportButton;
		private System.Windows.Forms.ToolStripButton ExportButton;
		private System.Windows.Forms.OpenFileDialog ImportOpenFileDialog;
		private System.Windows.Forms.SaveFileDialog ExportSaveFileDialog;
		private System.Windows.Forms.DataGridViewImageColumn ProgramImageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramProductNameColumn;
	}
}
