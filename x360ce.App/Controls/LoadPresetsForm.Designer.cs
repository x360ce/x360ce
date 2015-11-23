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
			this.MyDeviceSettingsTabPage = new System.Windows.Forms.TabPage();
			this.MyDevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.MySidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyDeviceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MapToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.MySettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.SummariesTabPage = new System.Windows.Forms.TabPage();
			this.SummariesDataGridView = new System.Windows.Forms.DataGridView();
			this.SidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolStrip2 = new System.Windows.Forms.ToolStrip();
			this.GlobalSettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.PresetsTabPage = new System.Windows.Forms.TabPage();
			this.PresetsDataGridView = new System.Windows.Forms.DataGridView();
			this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetTypeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetVendorNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.toolStrip3 = new System.Windows.Forms.ToolStrip();
			this.PresetRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.CloseButton = new System.Windows.Forms.Button();
			this.OkButton = new System.Windows.Forms.Button();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.MainTabControl.SuspendLayout();
			this.MyDeviceSettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MyDevicesDataGridView)).BeginInit();
			this.toolStrip1.SuspendLayout();
			this.SummariesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).BeginInit();
			this.toolStrip2.SuspendLayout();
			this.PresetsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).BeginInit();
			this.toolStrip3.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.MyDeviceSettingsTabPage);
			this.MainTabControl.Controls.Add(this.SummariesTabPage);
			this.MainTabControl.Controls.Add(this.PresetsTabPage);
			this.MainTabControl.Location = new System.Drawing.Point(12, 70);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(627, 376);
			this.MainTabControl.TabIndex = 19;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// 
			// MyDeviceSettingsTabPage
			// 
			this.MyDeviceSettingsTabPage.Controls.Add(this.MyDevicesDataGridView);
			this.MyDeviceSettingsTabPage.Controls.Add(this.toolStrip1);
			this.MyDeviceSettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.MyDeviceSettingsTabPage.Name = "MyDeviceSettingsTabPage";
			this.MyDeviceSettingsTabPage.Size = new System.Drawing.Size(619, 350);
			this.MyDeviceSettingsTabPage.TabIndex = 0;
			this.MyDeviceSettingsTabPage.Text = "My Device Settings";
			this.MyDeviceSettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// MyDevicesDataGridView
			// 
			this.MyDevicesDataGridView.AllowUserToAddRows = false;
			this.MyDevicesDataGridView.AllowUserToDeleteRows = false;
			this.MyDevicesDataGridView.AllowUserToResizeRows = false;
			this.MyDevicesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.MyDevicesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.MyDevicesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MyDevicesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.MyDevicesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MyDevicesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MyIconColumn,
            this.MySidColumn,
            this.MyDeviceColumn,
            this.MyFileColumn,
            this.MyGameColumn,
            this.MapToColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.MyDevicesDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.MyDevicesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MyDevicesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.MyDevicesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.MyDevicesDataGridView.MultiSelect = false;
			this.MyDevicesDataGridView.Name = "MyDevicesDataGridView";
			this.MyDevicesDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MyDevicesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.MyDevicesDataGridView.RowHeadersVisible = false;
			this.MyDevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MyDevicesDataGridView.Size = new System.Drawing.Size(619, 325);
			this.MyDevicesDataGridView.TabIndex = 0;
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
			// MySidColumn
			// 
			this.MySidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MySidColumn.DataPropertyName = "PadSettingChecksum";
			this.MySidColumn.HeaderText = "SID";
			this.MySidColumn.Name = "MySidColumn";
			this.MySidColumn.ReadOnly = true;
			this.MySidColumn.Width = 50;
			// 
			// MyDeviceColumn
			// 
			this.MyDeviceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyDeviceColumn.DataPropertyName = "ProductName";
			this.MyDeviceColumn.HeaderText = "Device";
			this.MyDeviceColumn.Name = "MyDeviceColumn";
			this.MyDeviceColumn.ReadOnly = true;
			this.MyDeviceColumn.Width = 66;
			// 
			// MyFileColumn
			// 
			this.MyFileColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyFileColumn.DataPropertyName = "FileName";
			this.MyFileColumn.HeaderText = "File Name";
			this.MyFileColumn.Name = "MyFileColumn";
			this.MyFileColumn.ReadOnly = true;
			this.MyFileColumn.Width = 79;
			// 
			// MyGameColumn
			// 
			this.MyGameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.MyGameColumn.DataPropertyName = "FileProductName";
			this.MyGameColumn.HeaderText = "File Product Title";
			this.MyGameColumn.Name = "MyGameColumn";
			this.MyGameColumn.ReadOnly = true;
			// 
			// MapToColumn
			// 
			this.MapToColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MapToColumn.DataPropertyName = "MapTo";
			this.MapToColumn.HeaderText = "Map To";
			this.MapToColumn.Name = "MapToColumn";
			this.MapToColumn.ReadOnly = true;
			this.MapToColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.MapToColumn.Width = 69;
			// 
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MySettingsRefreshButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(619, 25);
			this.toolStrip1.TabIndex = 1;
			this.toolStrip1.Text = "MySettingsToolStrip";
			// 
			// MySettingsRefreshButton
			// 
			this.MySettingsRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.MySettingsRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.MySettingsRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.MySettingsRefreshButton.Name = "MySettingsRefreshButton";
			this.MySettingsRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.MySettingsRefreshButton.Text = "&Refresh";
			// 
			// SummariesTabPage
			// 
			this.SummariesTabPage.Controls.Add(this.SummariesDataGridView);
			this.SummariesTabPage.Controls.Add(this.toolStrip2);
			this.SummariesTabPage.Location = new System.Drawing.Point(4, 22);
			this.SummariesTabPage.Name = "SummariesTabPage";
			this.SummariesTabPage.Size = new System.Drawing.Size(619, 350);
			this.SummariesTabPage.TabIndex = 1;
			this.SummariesTabPage.Text = "Most Popular Settings for My Controllers";
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
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SummariesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.SummariesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SummariesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SidColumn,
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4});
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
			this.SummariesDataGridView.Size = new System.Drawing.Size(619, 325);
			this.SummariesDataGridView.TabIndex = 1;
			// 
			// SidColumn
			// 
			this.SidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SidColumn.DataPropertyName = "PadSettingChecksum";
			this.SidColumn.HeaderText = "SID";
			this.SidColumn.Name = "SidColumn";
			this.SidColumn.ReadOnly = true;
			this.SidColumn.Width = 50;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.DataPropertyName = "Users";
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle5;
			this.dataGridViewTextBoxColumn1.HeaderText = "Users";
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			this.dataGridViewTextBoxColumn1.ReadOnly = true;
			this.dataGridViewTextBoxColumn1.Width = 42;
			// 
			// dataGridViewTextBoxColumn2
			// 
			this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn2.DataPropertyName = "ProductName";
			this.dataGridViewTextBoxColumn2.HeaderText = "Controller";
			this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
			this.dataGridViewTextBoxColumn2.ReadOnly = true;
			this.dataGridViewTextBoxColumn2.Width = 76;
			// 
			// dataGridViewTextBoxColumn3
			// 
			this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn3.DataPropertyName = "FileName";
			this.dataGridViewTextBoxColumn3.HeaderText = "File Name";
			this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
			this.dataGridViewTextBoxColumn3.ReadOnly = true;
			this.dataGridViewTextBoxColumn3.Width = 79;
			// 
			// dataGridViewTextBoxColumn4
			// 
			this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn4.DataPropertyName = "FileProductName";
			this.dataGridViewTextBoxColumn4.HeaderText = "File Product Title";
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			// 
			// toolStrip2
			// 
			this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GlobalSettingsRefreshButton});
			this.toolStrip2.Location = new System.Drawing.Point(0, 0);
			this.toolStrip2.Name = "toolStrip2";
			this.toolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip2.Size = new System.Drawing.Size(619, 25);
			this.toolStrip2.TabIndex = 2;
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
			// 
			// PresetsTabPage
			// 
			this.PresetsTabPage.Controls.Add(this.PresetsDataGridView);
			this.PresetsTabPage.Controls.Add(this.toolStrip3);
			this.PresetsTabPage.Location = new System.Drawing.Point(4, 22);
			this.PresetsTabPage.Name = "PresetsTabPage";
			this.PresetsTabPage.Size = new System.Drawing.Size(619, 350);
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
			dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.PresetsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
			this.PresetsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PresetsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn5,
            this.PresetTypeColumn,
            this.PresetVendorNameColumn,
            this.dataGridViewTextBoxColumn7});
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
			this.PresetsDataGridView.Size = new System.Drawing.Size(619, 325);
			this.PresetsDataGridView.TabIndex = 2;
			// 
			// dataGridViewTextBoxColumn5
			// 
			this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewTextBoxColumn5.DataPropertyName = "PadSettingChecksum";
			this.dataGridViewTextBoxColumn5.HeaderText = "SID";
			this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
			this.dataGridViewTextBoxColumn5.ReadOnly = true;
			this.dataGridViewTextBoxColumn5.Visible = false;
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
			this.PresetVendorNameColumn.HeaderText = "Vendor Name";
			this.PresetVendorNameColumn.Name = "PresetVendorNameColumn";
			this.PresetVendorNameColumn.ReadOnly = true;
			this.PresetVendorNameColumn.Width = 97;
			// 
			// dataGridViewTextBoxColumn7
			// 
			this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn7.DataPropertyName = "ProductName";
			this.dataGridViewTextBoxColumn7.HeaderText = "Product Name";
			this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
			this.dataGridViewTextBoxColumn7.ReadOnly = true;
			// 
			// toolStrip3
			// 
			this.toolStrip3.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PresetRefreshButton});
			this.toolStrip3.Location = new System.Drawing.Point(0, 0);
			this.toolStrip3.Name = "toolStrip3";
			this.toolStrip3.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip3.Size = new System.Drawing.Size(619, 25);
			this.toolStrip3.TabIndex = 3;
			this.toolStrip3.Text = "MySettingsToolStrip";
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
			this.CloseButton.Location = new System.Drawing.Point(564, 452);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 20;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(483, 452);
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
			// LoadPresetsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(651, 487);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.MainTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "LoadPresetsForm";
			this.Text = "TocaEdit Xbox 360 Controller Emulator Application - Load Preset";
			this.Controls.SetChildIndex(this.MainTabControl, 0);
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.MainTabControl.ResumeLayout(false);
			this.MyDeviceSettingsTabPage.ResumeLayout(false);
			this.MyDeviceSettingsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MyDevicesDataGridView)).EndInit();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.SummariesTabPage.ResumeLayout(false);
			this.SummariesTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).EndInit();
			this.toolStrip2.ResumeLayout(false);
			this.toolStrip2.PerformLayout();
			this.PresetsTabPage.ResumeLayout(false);
			this.PresetsTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).EndInit();
			this.toolStrip3.ResumeLayout(false);
			this.toolStrip3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage MyDeviceSettingsTabPage;
		private System.Windows.Forms.DataGridView MyDevicesDataGridView;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MySidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyDeviceColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MapToColumn;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton MySettingsRefreshButton;
		private System.Windows.Forms.TabPage SummariesTabPage;
		private System.Windows.Forms.DataGridView SummariesDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn SidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.ToolStrip toolStrip2;
		private System.Windows.Forms.ToolStripButton GlobalSettingsRefreshButton;
		private System.Windows.Forms.TabPage PresetsTabPage;
		private System.Windows.Forms.DataGridView PresetsDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetTypeColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetVendorNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
		private System.Windows.Forms.ToolStrip toolStrip3;
		private System.Windows.Forms.ToolStripButton PresetRefreshButton;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.Button OkButton;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
	}
}