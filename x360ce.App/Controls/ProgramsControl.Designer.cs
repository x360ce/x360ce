namespace x360ce.App.Controls
{
	partial class ProgramsControl
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
            this.ProgramsTabControl = new System.Windows.Forms.TabControl();
            this.ProgramsTabPage = new System.Windows.Forms.TabPage();
            this.ProgramsDataGridView = new System.Windows.Forms.DataGridView();
            this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.ProgramIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.SearchFilterTextBox = new System.Windows.Forms.TextBox();
            this.IncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.MinimumInstanceCountNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
            this.RefreshAllButton = new System.Windows.Forms.Button();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.ExportButton = new System.Windows.Forms.Button();
            this.ProgramsTabControl.SuspendLayout();
            this.ProgramsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).BeginInit();
            this.HookMaskGroupBox.SuspendLayout();
            this.InstalledFilesGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // ProgramsTabControl
            // 
            this.ProgramsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramsTabControl.Controls.Add(this.ProgramsTabPage);
            this.ProgramsTabControl.ItemSize = new System.Drawing.Size(116, 24);
            this.ProgramsTabControl.Location = new System.Drawing.Point(3, 68);
            this.ProgramsTabControl.Name = "ProgramsTabControl";
            this.ProgramsTabControl.SelectedIndex = 0;
            this.ProgramsTabControl.Size = new System.Drawing.Size(552, 363);
            this.ProgramsTabControl.TabIndex = 19;
            // 
            // ProgramsTabPage
            // 
            this.ProgramsTabPage.Controls.Add(this.ProgramsDataGridView);
            this.ProgramsTabPage.Location = new System.Drawing.Point(4, 28);
            this.ProgramsTabPage.Name = "ProgramsTabPage";
            this.ProgramsTabPage.Size = new System.Drawing.Size(544, 331);
            this.ProgramsTabPage.TabIndex = 0;
            this.ProgramsTabPage.Text = "Programs and Games:";
            this.ProgramsTabPage.UseVisualStyleBackColor = true;
            // 
            // ProgramsDataGridView
            // 
            this.ProgramsDataGridView.AllowUserToAddRows = false;
            this.ProgramsDataGridView.AllowUserToDeleteRows = false;
            this.ProgramsDataGridView.AllowUserToResizeRows = false;
            this.ProgramsDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.ProgramsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ProgramsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ProgramsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.ProgramsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ProgramsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MyIconColumn,
            this.ProgramIdColumn,
            this.MyFileColumn,
            this.MyGameColumn});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.ProgramsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
            this.ProgramsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgramsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.ProgramsDataGridView.Location = new System.Drawing.Point(0, 0);
            this.ProgramsDataGridView.MultiSelect = false;
            this.ProgramsDataGridView.Name = "ProgramsDataGridView";
            this.ProgramsDataGridView.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.ProgramsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.ProgramsDataGridView.RowHeadersVisible = false;
            this.ProgramsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.ProgramsDataGridView.Size = new System.Drawing.Size(544, 331);
            this.ProgramsDataGridView.TabIndex = 0;
            this.ProgramsDataGridView.DataSourceChanged += new System.EventHandler(this.ProgramsDataGridView_DataSourceChanged);
            this.ProgramsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.ProgramsDataGridView_CellFormatting);
            this.ProgramsDataGridView.SelectionChanged += new System.EventHandler(this.ProgramsDataGridView_SelectionChanged);
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
            // ProgramIdColumn
            // 
            this.ProgramIdColumn.DataPropertyName = "ProgramId";
            this.ProgramIdColumn.HeaderText = "ID";
            this.ProgramIdColumn.Name = "ProgramIdColumn";
            this.ProgramIdColumn.ReadOnly = true;
            this.ProgramIdColumn.Visible = false;
            // 
            // MyFileColumn
            // 
            this.MyFileColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.MyFileColumn.DataPropertyName = "FileName";
            this.MyFileColumn.FillWeight = 30F;
            this.MyFileColumn.HeaderText = "File Name";
            this.MyFileColumn.Name = "MyFileColumn";
            this.MyFileColumn.ReadOnly = true;
            // 
            // MyGameColumn
            // 
            this.MyGameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.MyGameColumn.DataPropertyName = "FileProductName";
            this.MyGameColumn.FillWeight = 70F;
            this.MyGameColumn.HeaderText = "Product Name";
            this.MyGameColumn.Name = "MyGameColumn";
            this.MyGameColumn.ReadOnly = true;
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
            this.HookMaskGroupBox.Location = new System.Drawing.Point(561, 3);
            this.HookMaskGroupBox.Name = "HookMaskGroupBox";
            this.HookMaskGroupBox.Size = new System.Drawing.Size(137, 253);
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
            this.InstalledFilesGroupBox.Location = new System.Drawing.Point(561, 262);
            this.InstalledFilesGroupBox.Name = "InstalledFilesGroupBox";
            this.InstalledFilesGroupBox.Size = new System.Drawing.Size(137, 162);
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
            // SearchFilterTextBox
            // 
            this.SearchFilterTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SearchFilterTextBox.Location = new System.Drawing.Point(128, 68);
            this.SearchFilterTextBox.Name = "SearchFilterTextBox";
            this.SearchFilterTextBox.Size = new System.Drawing.Size(425, 20);
            this.SearchFilterTextBox.TabIndex = 22;
            // 
            // IncludeEnabledCheckBox
            // 
            this.IncludeEnabledCheckBox.AutoSize = true;
            this.IncludeEnabledCheckBox.Checked = true;
            this.IncludeEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.IncludeEnabledCheckBox.Location = new System.Drawing.Point(3, 8);
            this.IncludeEnabledCheckBox.Name = "IncludeEnabledCheckBox";
            this.IncludeEnabledCheckBox.Size = new System.Drawing.Size(103, 17);
            this.IncludeEnabledCheckBox.TabIndex = 23;
            this.IncludeEnabledCheckBox.Text = "Include Enabled";
            this.IncludeEnabledCheckBox.ThreeState = true;
            this.IncludeEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // MinimumInstanceCountNumericUpDown
            // 
            this.MinimumInstanceCountNumericUpDown.Location = new System.Drawing.Point(109, 38);
            this.MinimumInstanceCountNumericUpDown.Name = "MinimumInstanceCountNumericUpDown";
            this.MinimumInstanceCountNumericUpDown.Size = new System.Drawing.Size(59, 20);
            this.MinimumInstanceCountNumericUpDown.TabIndex = 24;
            this.MinimumInstanceCountNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // MinimumInstanceCountLabel
            // 
            this.MinimumInstanceCountLabel.AutoSize = true;
            this.MinimumInstanceCountLabel.Location = new System.Drawing.Point(3, 40);
            this.MinimumInstanceCountLabel.Name = "MinimumInstanceCountLabel";
            this.MinimumInstanceCountLabel.Size = new System.Drawing.Size(100, 13);
            this.MinimumInstanceCountLabel.TabIndex = 29;
            this.MinimumInstanceCountLabel.Text = "Minimum Instances:";
            // 
            // RefreshAllButton
            // 
            this.RefreshAllButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshAllButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
            this.RefreshAllButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RefreshAllButton.Location = new System.Drawing.Point(313, 3);
            this.RefreshAllButton.Name = "RefreshAllButton";
            this.RefreshAllButton.Size = new System.Drawing.Size(86, 25);
            this.RefreshAllButton.TabIndex = 28;
            this.RefreshAllButton.Text = "Refresh &All";
            this.RefreshAllButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RefreshAllButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.RefreshAllButton.UseVisualStyleBackColor = true;
            this.RefreshAllButton.Click += new System.EventHandler(this.RefreshAllButton_Click);
            // 
            // RefreshButton
            // 
            this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
            this.RefreshButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RefreshButton.Location = new System.Drawing.Point(405, 3);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(70, 25);
            this.RefreshButton.TabIndex = 28;
            this.RefreshButton.Text = "&Refresh";
            this.RefreshButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RefreshButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
            this.DeleteButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.DeleteButton.Location = new System.Drawing.Point(481, 3);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(70, 25);
            this.DeleteButton.TabIndex = 27;
            this.DeleteButton.Text = "&Delete";
            this.DeleteButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.DeleteButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.DeleteButton.UseVisualStyleBackColor = true;
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
            this.SaveButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.SaveButton.Location = new System.Drawing.Point(313, 34);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(116, 25);
            this.SaveButton.TabIndex = 26;
            this.SaveButton.Text = "&Save on Internet";
            this.SaveButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.SaveButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.SaveButton.UseVisualStyleBackColor = true;
            // 
            // ExportButton
            // 
            this.ExportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ExportButton.Image = global::x360ce.App.Properties.Resources.load_16x16;
            this.ExportButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ExportButton.Location = new System.Drawing.Point(435, 34);
            this.ExportButton.Name = "ExportButton";
            this.ExportButton.Size = new System.Drawing.Size(116, 25);
            this.ExportButton.TabIndex = 25;
            this.ExportButton.Text = "&Export to GDB";
            this.ExportButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ExportButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.ExportButton.UseVisualStyleBackColor = true;
            // 
            // ProgramsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MinimumInstanceCountLabel);
            this.Controls.Add(this.SearchFilterTextBox);
            this.Controls.Add(this.RefreshAllButton);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.ExportButton);
            this.Controls.Add(this.MinimumInstanceCountNumericUpDown);
            this.Controls.Add(this.IncludeEnabledCheckBox);
            this.Controls.Add(this.InstalledFilesGroupBox);
            this.Controls.Add(this.HookMaskGroupBox);
            this.Controls.Add(this.ProgramsTabControl);
            this.Name = "ProgramsControl";
            this.Size = new System.Drawing.Size(701, 434);
            this.ProgramsTabControl.ResumeLayout(false);
            this.ProgramsTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).EndInit();
            this.HookMaskGroupBox.ResumeLayout(false);
            this.HookMaskGroupBox.PerformLayout();
            this.InstalledFilesGroupBox.ResumeLayout(false);
            this.InstalledFilesGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MinimumInstanceCountNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		System.Windows.Forms.TabControl ProgramsTabControl;
		System.Windows.Forms.TabPage ProgramsTabPage;
		System.Windows.Forms.DataGridView ProgramsDataGridView;
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
		private System.Windows.Forms.TextBox SearchFilterTextBox;
		private System.Windows.Forms.CheckBox IncludeEnabledCheckBox;
		private System.Windows.Forms.NumericUpDown MinimumInstanceCountNumericUpDown;
		private System.Windows.Forms.Button ExportButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.Button DeleteButton;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
		private System.Windows.Forms.Button RefreshAllButton;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
	}
}
