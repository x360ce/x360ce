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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadPresetsForm));
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.SettingsSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsFileTitleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsMapToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.CommentLabel = new System.Windows.Forms.Label();
			this.SettingsToolStrip = new System.Windows.Forms.ToolStrip();
			this.SettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.SettingsDeleteButton = new System.Windows.Forms.ToolStripButton();
			this.SettingsEditNoteButton = new System.Windows.Forms.ToolStripButton();
			this.SummariesTabPage = new System.Windows.Forms.TabPage();
			this.SummariesDataGridView = new System.Windows.Forms.DataGridView();
			this.SummariesSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesUsersColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesFileTitleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesToolStrip = new System.Windows.Forms.ToolStrip();
			this.SummariesRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.PresetsTabPage = new System.Windows.Forms.TabPage();
			this.PresetsDataGridView = new System.Windows.Forms.DataGridView();
			this.PresetSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetTypeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetVendorNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetsDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetsToolStrip = new System.Windows.Forms.ToolStrip();
			this.PresetRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.CloseButton = new System.Windows.Forms.Button();
			this.OkButton = new System.Windows.Forms.Button();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.CommentPanel = new System.Windows.Forms.Panel();
			this.MainTabControl.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).BeginInit();
			this.SettingsToolStrip.SuspendLayout();
			this.SummariesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).BeginInit();
			this.SummariesToolStrip.SuspendLayout();
			this.PresetsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).BeginInit();
			this.PresetsToolStrip.SuspendLayout();
			this.CommentPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.SummariesTabPage);
			this.MainTabControl.Controls.Add(this.PresetsTabPage);
			this.MainTabControl.Location = new System.Drawing.Point(12, 70);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(600, 330);
			this.MainTabControl.TabIndex = 19;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.Controls.Add(this.CommentPanel);
			this.SettingsTabPage.Controls.Add(this.SettingsDataGridView);
			this.SettingsTabPage.Controls.Add(this.SettingsToolStrip);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Size = new System.Drawing.Size(592, 304);
			this.SettingsTabPage.TabIndex = 0;
			this.SettingsTabPage.Text = "My Settings";
			this.SettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// SettingsDataGridView
			// 
			this.SettingsDataGridView.AllowUserToAddRows = false;
			this.SettingsDataGridView.AllowUserToDeleteRows = false;
			this.SettingsDataGridView.AllowUserToResizeRows = false;
			this.SettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.SettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.SettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SettingsSidColumn,
            this.SettingsFileNameColumn,
            this.SettingsFileTitleColumn,
            this.SettingsDeviceNameColumn,
            this.SettingsMapToColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.SettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.SettingsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.SettingsDataGridView.MultiSelect = false;
			this.SettingsDataGridView.Name = "SettingsDataGridView";
			this.SettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.SettingsDataGridView.RowHeadersVisible = false;
			this.SettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.SettingsDataGridView.Size = new System.Drawing.Size(592, 279);
			this.SettingsDataGridView.TabIndex = 2;
			this.SettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SettingsDataGridView_CellFormatting);
			this.SettingsDataGridView.SelectionChanged += new System.EventHandler(this.SettingsDataGridView_SelectionChanged);
			// 
			// SettingsSidColumn
			// 
			this.SettingsSidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsSidColumn.DataPropertyName = "PadSettingChecksum";
			this.SettingsSidColumn.HeaderText = "SID";
			this.SettingsSidColumn.Name = "SettingsSidColumn";
			this.SettingsSidColumn.ReadOnly = true;
			this.SettingsSidColumn.Width = 50;
			// 
			// SettingsFileNameColumn
			// 
			this.SettingsFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsFileNameColumn.DataPropertyName = "FileName";
			this.SettingsFileNameColumn.HeaderText = "File Name";
			this.SettingsFileNameColumn.Name = "SettingsFileNameColumn";
			this.SettingsFileNameColumn.ReadOnly = true;
			this.SettingsFileNameColumn.Width = 79;
			// 
			// SettingsFileTitleColumn
			// 
			this.SettingsFileTitleColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.SettingsFileTitleColumn.DataPropertyName = "FileProductName";
			this.SettingsFileTitleColumn.HeaderText = "File Title";
			this.SettingsFileTitleColumn.Name = "SettingsFileTitleColumn";
			this.SettingsFileTitleColumn.ReadOnly = true;
			// 
			// SettingsDeviceNameColumn
			// 
			this.SettingsDeviceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsDeviceNameColumn.DataPropertyName = "ProductName";
			this.SettingsDeviceNameColumn.HeaderText = "Device Name";
			this.SettingsDeviceNameColumn.Name = "SettingsDeviceNameColumn";
			this.SettingsDeviceNameColumn.ReadOnly = true;
			this.SettingsDeviceNameColumn.Width = 97;
			// 
			// SettingsMapToColumn
			// 
			this.SettingsMapToColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsMapToColumn.DataPropertyName = "MapTo";
			this.SettingsMapToColumn.HeaderText = "Map To";
			this.SettingsMapToColumn.Name = "SettingsMapToColumn";
			this.SettingsMapToColumn.ReadOnly = true;
			this.SettingsMapToColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsMapToColumn.Width = 69;
			// 
			// CommentLabel
			// 
			this.CommentLabel.AutoSize = true;
			this.CommentLabel.BackColor = System.Drawing.SystemColors.Control;
			this.CommentLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.CommentLabel.Location = new System.Drawing.Point(0, 0);
			this.CommentLabel.Name = "CommentLabel";
			this.CommentLabel.Padding = new System.Windows.Forms.Padding(3);
			this.CommentLabel.Size = new System.Drawing.Size(44, 19);
			this.CommentLabel.TabIndex = 5;
			this.CommentLabel.Text = "&Notes:";
			// 
			// SettingsToolStrip
			// 
			this.SettingsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.SettingsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SettingsRefreshButton,
            this.SettingsDeleteButton,
            this.SettingsEditNoteButton});
			this.SettingsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.SettingsToolStrip.Name = "SettingsToolStrip";
			this.SettingsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.SettingsToolStrip.Size = new System.Drawing.Size(592, 25);
			this.SettingsToolStrip.TabIndex = 1;
			this.SettingsToolStrip.Text = "MySettingsToolStrip";
			// 
			// SettingsRefreshButton
			// 
			this.SettingsRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SettingsRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.SettingsRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsRefreshButton.Name = "SettingsRefreshButton";
			this.SettingsRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.SettingsRefreshButton.Text = "&Refresh";
			this.SettingsRefreshButton.Click += new System.EventHandler(this.SettingsRefreshButton_Click);
			// 
			// SettingsDeleteButton
			// 
			this.SettingsDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SettingsDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.SettingsDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsDeleteButton.Name = "SettingsDeleteButton";
			this.SettingsDeleteButton.Size = new System.Drawing.Size(60, 22);
			this.SettingsDeleteButton.Text = "&Delete";
			this.SettingsDeleteButton.Click += new System.EventHandler(this.SettingsDeleteButton_Click);
			// 
			// SettingsEditNoteButton
			// 
			this.SettingsEditNoteButton.Image = global::x360ce.App.Properties.Resources.edit_note_16x16;
			this.SettingsEditNoteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsEditNoteButton.Name = "SettingsEditNoteButton";
			this.SettingsEditNoteButton.Size = new System.Drawing.Size(76, 22);
			this.SettingsEditNoteButton.Text = "Edit Note";
			this.SettingsEditNoteButton.Click += new System.EventHandler(this.SettingsEditNoteButton_Click);
			// 
			// SummariesTabPage
			// 
			this.SummariesTabPage.Controls.Add(this.SummariesDataGridView);
			this.SummariesTabPage.Controls.Add(this.SummariesToolStrip);
			this.SummariesTabPage.Location = new System.Drawing.Point(4, 22);
			this.SummariesTabPage.Name = "SummariesTabPage";
			this.SummariesTabPage.Size = new System.Drawing.Size(592, 304);
			this.SummariesTabPage.TabIndex = 1;
			this.SummariesTabPage.Text = "Default Settings for My Controllers";
			this.SummariesTabPage.UseVisualStyleBackColor = true;
			// 
			// SummariesDataGridView
			// 
			this.SummariesDataGridView.AllowUserToAddRows = false;
			this.SummariesDataGridView.AllowUserToDeleteRows = false;
			this.SummariesDataGridView.AllowUserToResizeRows = false;
			this.SummariesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.SummariesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SummariesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SummariesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.SummariesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SummariesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SummariesSidColumn,
            this.SummariesUsersColumn,
            this.SummariesFileNameColumn,
            this.SummariesFileTitleColumn,
            this.SummariesDeviceNameColumn});
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SummariesDataGridView.DefaultCellStyle = dataGridViewCellStyle6;
			this.SummariesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SummariesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.SummariesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.SummariesDataGridView.MultiSelect = false;
			this.SummariesDataGridView.Name = "SummariesDataGridView";
			this.SummariesDataGridView.ReadOnly = true;
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SummariesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
			this.SummariesDataGridView.RowHeadersVisible = false;
			this.SummariesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.SummariesDataGridView.Size = new System.Drawing.Size(592, 279);
			this.SummariesDataGridView.TabIndex = 1;
			this.SummariesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SummariesDataGridView_CellFormatting);
			// 
			// SummariesSidColumn
			// 
			this.SummariesSidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesSidColumn.DataPropertyName = "PadSettingChecksum";
			this.SummariesSidColumn.HeaderText = "SID";
			this.SummariesSidColumn.Name = "SummariesSidColumn";
			this.SummariesSidColumn.ReadOnly = true;
			this.SummariesSidColumn.Width = 50;
			// 
			// SummariesUsersColumn
			// 
			this.SummariesUsersColumn.DataPropertyName = "Users";
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.SummariesUsersColumn.DefaultCellStyle = dataGridViewCellStyle5;
			this.SummariesUsersColumn.HeaderText = "Users";
			this.SummariesUsersColumn.Name = "SummariesUsersColumn";
			this.SummariesUsersColumn.ReadOnly = true;
			this.SummariesUsersColumn.Width = 42;
			// 
			// SummariesFileNameColumn
			// 
			this.SummariesFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesFileNameColumn.DataPropertyName = "FileName";
			this.SummariesFileNameColumn.HeaderText = "File Name";
			this.SummariesFileNameColumn.Name = "SummariesFileNameColumn";
			this.SummariesFileNameColumn.ReadOnly = true;
			this.SummariesFileNameColumn.Width = 79;
			// 
			// SummariesFileTitleColumn
			// 
			this.SummariesFileTitleColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.SummariesFileTitleColumn.DataPropertyName = "FileProductName";
			this.SummariesFileTitleColumn.HeaderText = "File Title";
			this.SummariesFileTitleColumn.Name = "SummariesFileTitleColumn";
			this.SummariesFileTitleColumn.ReadOnly = true;
			// 
			// SummariesDeviceNameColumn
			// 
			this.SummariesDeviceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesDeviceNameColumn.DataPropertyName = "ProductName";
			this.SummariesDeviceNameColumn.HeaderText = "Device Name";
			this.SummariesDeviceNameColumn.Name = "SummariesDeviceNameColumn";
			this.SummariesDeviceNameColumn.ReadOnly = true;
			this.SummariesDeviceNameColumn.Width = 97;
			// 
			// SummariesToolStrip
			// 
			this.SummariesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.SummariesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SummariesRefreshButton});
			this.SummariesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.SummariesToolStrip.Name = "SummariesToolStrip";
			this.SummariesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.SummariesToolStrip.Size = new System.Drawing.Size(592, 25);
			this.SummariesToolStrip.TabIndex = 2;
			this.SummariesToolStrip.Text = "MySettingsToolStrip";
			// 
			// SummariesRefreshButton
			// 
			this.SummariesRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SummariesRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.SummariesRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SummariesRefreshButton.Name = "SummariesRefreshButton";
			this.SummariesRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.SummariesRefreshButton.Text = "&Refresh";
			this.SummariesRefreshButton.Click += new System.EventHandler(this.SummariesRefreshButton_Click);
			// 
			// PresetsTabPage
			// 
			this.PresetsTabPage.Controls.Add(this.PresetsDataGridView);
			this.PresetsTabPage.Controls.Add(this.PresetsToolStrip);
			this.PresetsTabPage.Location = new System.Drawing.Point(4, 22);
			this.PresetsTabPage.Name = "PresetsTabPage";
			this.PresetsTabPage.Size = new System.Drawing.Size(592, 304);
			this.PresetsTabPage.TabIndex = 2;
			this.PresetsTabPage.Text = "Default Settings for Most Popular Controllers";
			this.PresetsTabPage.UseVisualStyleBackColor = true;
			// 
			// PresetsDataGridView
			// 
			this.PresetsDataGridView.AllowUserToAddRows = false;
			this.PresetsDataGridView.AllowUserToDeleteRows = false;
			this.PresetsDataGridView.AllowUserToResizeRows = false;
			this.PresetsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.PresetsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.PresetsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.PresetsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
			this.PresetsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PresetsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PresetSidColumn,
            this.PresetTypeColumn,
            this.PresetVendorNameColumn,
            this.PresetsDeviceNameColumn});
			dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.PresetsDataGridView.DefaultCellStyle = dataGridViewCellStyle9;
			this.PresetsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PresetsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.PresetsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.PresetsDataGridView.MultiSelect = false;
			this.PresetsDataGridView.Name = "PresetsDataGridView";
			this.PresetsDataGridView.ReadOnly = true;
			dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.PresetsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle10;
			this.PresetsDataGridView.RowHeadersVisible = false;
			this.PresetsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.PresetsDataGridView.Size = new System.Drawing.Size(592, 279);
			this.PresetsDataGridView.TabIndex = 2;
			this.PresetsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.PresetsDataGridView_CellFormatting);
			// 
			// PresetSidColumn
			// 
			this.PresetSidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PresetSidColumn.DataPropertyName = "PadSettingChecksum";
			this.PresetSidColumn.HeaderText = "SID";
			this.PresetSidColumn.Name = "PresetSidColumn";
			this.PresetSidColumn.ReadOnly = true;
			this.PresetSidColumn.Width = 50;
			// 
			// PresetTypeColumn
			// 
			this.PresetTypeColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PresetTypeColumn.DataPropertyName = "Type";
			this.PresetTypeColumn.HeaderText = "Type";
			this.PresetTypeColumn.Name = "PresetTypeColumn";
			this.PresetTypeColumn.ReadOnly = true;
			this.PresetTypeColumn.Visible = false;
			// 
			// PresetVendorNameColumn
			// 
			this.PresetVendorNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PresetVendorNameColumn.DataPropertyName = "VendorName";
			this.PresetVendorNameColumn.HeaderText = "Device Vendor Name";
			this.PresetVendorNameColumn.Name = "PresetVendorNameColumn";
			this.PresetVendorNameColumn.ReadOnly = true;
			this.PresetVendorNameColumn.Width = 134;
			// 
			// PresetsDeviceNameColumn
			// 
			this.PresetsDeviceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.PresetsDeviceNameColumn.DataPropertyName = "ProductName";
			this.PresetsDeviceNameColumn.HeaderText = "Device Name";
			this.PresetsDeviceNameColumn.Name = "PresetsDeviceNameColumn";
			this.PresetsDeviceNameColumn.ReadOnly = true;
			// 
			// PresetsToolStrip
			// 
			this.PresetsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.PresetsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PresetRefreshButton});
			this.PresetsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.PresetsToolStrip.Name = "PresetsToolStrip";
			this.PresetsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.PresetsToolStrip.Size = new System.Drawing.Size(592, 25);
			this.PresetsToolStrip.TabIndex = 3;
			this.PresetsToolStrip.Text = "MySettingsToolStrip";
			// 
			// PresetRefreshButton
			// 
			this.PresetRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.PresetRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.PresetRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PresetRefreshButton.Name = "PresetRefreshButton";
			this.PresetRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.PresetRefreshButton.Text = "&Refresh";
			this.PresetRefreshButton.Click += new System.EventHandler(this.PresetRefreshButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(537, 406);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 20;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(456, 406);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(75, 23);
			this.OkButton.TabIndex = 20;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(597, 9);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			// 
			// CommentPanel
			// 
			this.CommentPanel.AutoSize = true;
			this.CommentPanel.BackColor = System.Drawing.SystemColors.Control;
			this.CommentPanel.Controls.Add(this.CommentLabel);
			this.CommentPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.CommentPanel.Location = new System.Drawing.Point(0, 285);
			this.CommentPanel.Name = "CommentPanel";
			this.CommentPanel.Size = new System.Drawing.Size(592, 19);
			this.CommentPanel.TabIndex = 6;
			// 
			// LoadPresetsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 441);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.MainTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "LoadPresetsForm";
			this.Text = "X360CE - Load Preset";
			this.Controls.SetChildIndex(this.MainTabControl, 0);
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.MainTabControl.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.SettingsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).EndInit();
			this.SettingsToolStrip.ResumeLayout(false);
			this.SettingsToolStrip.PerformLayout();
			this.SummariesTabPage.ResumeLayout(false);
			this.SummariesTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).EndInit();
			this.SummariesToolStrip.ResumeLayout(false);
			this.SummariesToolStrip.PerformLayout();
			this.PresetsTabPage.ResumeLayout(false);
			this.PresetsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).EndInit();
			this.PresetsToolStrip.ResumeLayout(false);
			this.PresetsToolStrip.PerformLayout();
			this.CommentPanel.ResumeLayout(false);
			this.CommentPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage SettingsTabPage;
		private System.Windows.Forms.ToolStrip SettingsToolStrip;
		private System.Windows.Forms.ToolStripButton SettingsRefreshButton;
		private System.Windows.Forms.TabPage SummariesTabPage;
		private System.Windows.Forms.DataGridView SummariesDataGridView;
		private System.Windows.Forms.ToolStrip SummariesToolStrip;
		private System.Windows.Forms.ToolStripButton SummariesRefreshButton;
		private System.Windows.Forms.TabPage PresetsTabPage;
		private System.Windows.Forms.DataGridView PresetsDataGridView;
		private System.Windows.Forms.ToolStrip PresetsToolStrip;
		private System.Windows.Forms.ToolStripButton PresetRefreshButton;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.Button OkButton;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		private System.Windows.Forms.DataGridView SettingsDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsFileTitleColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsDeviceNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsMapToColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesUsersColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesFileTitleColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesDeviceNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetTypeColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetVendorNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetsDeviceNameColumn;
		private System.Windows.Forms.ToolStripButton SettingsDeleteButton;
		private System.Windows.Forms.Label CommentLabel;
		private System.Windows.Forms.ToolStripButton SettingsEditNoteButton;
		private System.Windows.Forms.Panel CommentPanel;
	}
}