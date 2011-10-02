namespace x360ce.App.Controls
{
	partial class OnlineUserControl
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

		#region Component Designer generated code

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
			this.MySettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.MySidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyControllerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ControllerComboBox = new System.Windows.Forms.ComboBox();
			this.OnlineDatabaseUrlTextBox = new System.Windows.Forms.TextBox();
			this.ControllerLabel = new System.Windows.Forms.Label();
			this.GameComboBox = new System.Windows.Forms.ComboBox();
			this.GameLabel = new System.Windows.Forms.Label();
			this.CommentTextBox = new System.Windows.Forms.TextBox();
			this.CommentLabel = new System.Windows.Forms.Label();
			this.CommentSelectedTextBox = new System.Windows.Forms.TextBox();
			this.SettingsListTabControl = new System.Windows.Forms.TabControl();
			this.MySettingsTabPage = new System.Windows.Forms.TabPage();
			this.GlobalSettingsTabPage = new System.Windows.Forms.TabPage();
			this.GlobalSettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.SidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DeleteButton = new System.Windows.Forms.Button();
			this.LoadButton = new System.Windows.Forms.Button();
			this.SaveButton = new System.Windows.Forms.Button();
			this.RefreshButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).BeginInit();
			this.SettingsListTabControl.SuspendLayout();
			this.MySettingsTabPage.SuspendLayout();
			this.GlobalSettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.GlobalSettingsDataGridView)).BeginInit();
			this.SuspendLayout();
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
            this.MyIconColumn,
            this.MySidColumn,
            this.MyControllerColumn,
            this.MyFileColumn,
            this.MyGameColumn});
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
			this.MySettingsDataGridView.Location = new System.Drawing.Point(0, 0);
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
			this.MySettingsDataGridView.Size = new System.Drawing.Size(592, 251);
			this.MySettingsDataGridView.TabIndex = 0;
			this.MySettingsDataGridView.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.MySettingsDataGridView_CellContentDoubleClick);
			this.MySettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.MySettingsDataGridView_CellFormatting);
			this.MySettingsDataGridView.SelectionChanged += new System.EventHandler(this.MySettingsDataGridView_SelectionChanged);
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
			// MySidColumn
			// 
			this.MySidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MySidColumn.DataPropertyName = "PadSettingChecksum";
			this.MySidColumn.HeaderText = "SID";
			this.MySidColumn.Name = "MySidColumn";
			this.MySidColumn.ReadOnly = true;
			this.MySidColumn.Width = 50;
			// 
			// MyControllerColumn
			// 
			this.MyControllerColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyControllerColumn.DataPropertyName = "ProductName";
			this.MyControllerColumn.HeaderText = "Controller";
			this.MyControllerColumn.Name = "MyControllerColumn";
			this.MyControllerColumn.ReadOnly = true;
			this.MyControllerColumn.Width = 76;
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
			this.MyGameColumn.HeaderText = "Product Title";
			this.MyGameColumn.Name = "MyGameColumn";
			this.MyGameColumn.ReadOnly = true;
			// 
			// ControllerComboBox
			// 
			this.ControllerComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControllerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ControllerComboBox.FormattingEnabled = true;
			this.ControllerComboBox.Location = new System.Drawing.Point(3, 16);
			this.ControllerComboBox.Name = "ControllerComboBox";
			this.ControllerComboBox.Size = new System.Drawing.Size(276, 21);
			this.ControllerComboBox.TabIndex = 2;
			this.ControllerComboBox.SelectedIndexChanged += new System.EventHandler(this.ControllerComboBox_SelectedIndexChanged);
			// 
			// OnlineDatabaseUrlTextBox
			// 
			this.OnlineDatabaseUrlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OnlineDatabaseUrlTextBox.Location = new System.Drawing.Point(3, 73);
			this.OnlineDatabaseUrlTextBox.Name = "OnlineDatabaseUrlTextBox";
			this.OnlineDatabaseUrlTextBox.ReadOnly = true;
			this.OnlineDatabaseUrlTextBox.Size = new System.Drawing.Size(438, 20);
			this.OnlineDatabaseUrlTextBox.TabIndex = 12;
			// 
			// ControllerLabel
			// 
			this.ControllerLabel.AutoSize = true;
			this.ControllerLabel.Location = new System.Drawing.Point(3, 0);
			this.ControllerLabel.Name = "ControllerLabel";
			this.ControllerLabel.Size = new System.Drawing.Size(99, 13);
			this.ControllerLabel.TabIndex = 1;
			this.ControllerLabel.Text = "&Controller / Device:";
			// 
			// GameComboBox
			// 
			this.GameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.GameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.GameComboBox.FormattingEnabled = true;
			this.GameComboBox.Location = new System.Drawing.Point(285, 16);
			this.GameComboBox.Name = "GameComboBox";
			this.GameComboBox.Size = new System.Drawing.Size(318, 21);
			this.GameComboBox.TabIndex = 4;
			this.GameComboBox.SelectedIndexChanged += new System.EventHandler(this.GameComboBox_SelectedIndexChanged);
			// 
			// GameLabel
			// 
			this.GameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.GameLabel.AutoSize = true;
			this.GameLabel.Location = new System.Drawing.Point(282, 0);
			this.GameLabel.Name = "GameLabel";
			this.GameLabel.Size = new System.Drawing.Size(117, 13);
			this.GameLabel.TabIndex = 3;
			this.GameLabel.Text = "&Product / Game Name:";
			// 
			// CommentTextBox
			// 
			this.CommentTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CommentTextBox.Location = new System.Drawing.Point(47, 45);
			this.CommentTextBox.Name = "CommentTextBox";
			this.CommentTextBox.Size = new System.Drawing.Size(394, 20);
			this.CommentTextBox.TabIndex = 6;
			// 
			// CommentLabel
			// 
			this.CommentLabel.AutoSize = true;
			this.CommentLabel.Location = new System.Drawing.Point(3, 48);
			this.CommentLabel.Name = "CommentLabel";
			this.CommentLabel.Size = new System.Drawing.Size(38, 13);
			this.CommentLabel.TabIndex = 5;
			this.CommentLabel.Text = "&Notes:";
			// 
			// CommentSelectedTextBox
			// 
			this.CommentSelectedTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CommentSelectedTextBox.Location = new System.Drawing.Point(3, 382);
			this.CommentSelectedTextBox.Name = "CommentSelectedTextBox";
			this.CommentSelectedTextBox.ReadOnly = true;
			this.CommentSelectedTextBox.Size = new System.Drawing.Size(600, 20);
			this.CommentSelectedTextBox.TabIndex = 20;
			// 
			// SettingsListTabControl
			// 
			this.SettingsListTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SettingsListTabControl.Controls.Add(this.MySettingsTabPage);
			this.SettingsListTabControl.Controls.Add(this.GlobalSettingsTabPage);
			this.SettingsListTabControl.Location = new System.Drawing.Point(3, 99);
			this.SettingsListTabControl.Name = "SettingsListTabControl";
			this.SettingsListTabControl.SelectedIndex = 0;
			this.SettingsListTabControl.Size = new System.Drawing.Size(600, 277);
			this.SettingsListTabControl.TabIndex = 18;
			// 
			// MySettingsTabPage
			// 
			this.MySettingsTabPage.Controls.Add(this.MySettingsDataGridView);
			this.MySettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.MySettingsTabPage.Name = "MySettingsTabPage";
			this.MySettingsTabPage.Size = new System.Drawing.Size(592, 251);
			this.MySettingsTabPage.TabIndex = 0;
			this.MySettingsTabPage.Text = "My Settings";
			this.MySettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// GlobalSettingsTabPage
			// 
			this.GlobalSettingsTabPage.Controls.Add(this.GlobalSettingsDataGridView);
			this.GlobalSettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.GlobalSettingsTabPage.Name = "GlobalSettingsTabPage";
			this.GlobalSettingsTabPage.Size = new System.Drawing.Size(592, 251);
			this.GlobalSettingsTabPage.TabIndex = 1;
			this.GlobalSettingsTabPage.Text = "Global Settings";
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
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GlobalSettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.GlobalSettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.GlobalSettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
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
			this.GlobalSettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle6;
			this.GlobalSettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.GlobalSettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.GlobalSettingsDataGridView.Location = new System.Drawing.Point(0, 0);
			this.GlobalSettingsDataGridView.MultiSelect = false;
			this.GlobalSettingsDataGridView.Name = "GlobalSettingsDataGridView";
			this.GlobalSettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.GlobalSettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
			this.GlobalSettingsDataGridView.RowHeadersVisible = false;
			this.GlobalSettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.GlobalSettingsDataGridView.Size = new System.Drawing.Size(592, 251);
			this.GlobalSettingsDataGridView.TabIndex = 1;
			this.GlobalSettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.GlobalSettingsDataGridView_CellFormatting);
			this.GlobalSettingsDataGridView.SelectionChanged += new System.EventHandler(this.GlobalSettingsDataGridView_SelectionChanged);
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
			this.dataGridViewTextBoxColumn4.HeaderText = "Product Title";
			this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
			this.dataGridViewTextBoxColumn4.ReadOnly = true;
			// 
			// DeleteButton
			// 
			this.DeleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteButton.Location = new System.Drawing.Point(528, 42);
			this.DeleteButton.Name = "DeleteButton";
			this.DeleteButton.Size = new System.Drawing.Size(75, 25);
			this.DeleteButton.TabIndex = 10;
			this.DeleteButton.Text = "&Delete";
			this.DeleteButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.DeleteButton.UseVisualStyleBackColor = true;
			this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
			// 
			// LoadButton
			// 
			this.LoadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LoadButton.Image = global::x360ce.App.Properties.Resources.load_16x16;
			this.LoadButton.Location = new System.Drawing.Point(528, 71);
			this.LoadButton.Name = "LoadButton";
			this.LoadButton.Size = new System.Drawing.Size(75, 25);
			this.LoadButton.TabIndex = 16;
			this.LoadButton.Text = "&Load";
			this.LoadButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.LoadButton.UseVisualStyleBackColor = true;
			this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
			// 
			// SaveButton
			// 
			this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SaveButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.SaveButton.Location = new System.Drawing.Point(447, 42);
			this.SaveButton.Name = "SaveButton";
			this.SaveButton.Size = new System.Drawing.Size(75, 25);
			this.SaveButton.TabIndex = 8;
			this.SaveButton.Text = "&Save";
			this.SaveButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.SaveButton.UseVisualStyleBackColor = true;
			this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
			// 
			// RefreshButton
			// 
			this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshButton.Location = new System.Drawing.Point(447, 71);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(75, 25);
			this.RefreshButton.TabIndex = 14;
			this.RefreshButton.Text = "&Refresh";
			this.RefreshButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// OnlineUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.SettingsListTabControl);
			this.Controls.Add(this.CommentSelectedTextBox);
			this.Controls.Add(this.CommentTextBox);
			this.Controls.Add(this.OnlineDatabaseUrlTextBox);
			this.Controls.Add(this.GameLabel);
			this.Controls.Add(this.ControllerLabel);
			this.Controls.Add(this.CommentLabel);
			this.Controls.Add(this.GameComboBox);
			this.Controls.Add(this.ControllerComboBox);
			this.Controls.Add(this.DeleteButton);
			this.Controls.Add(this.LoadButton);
			this.Controls.Add(this.SaveButton);
			this.Controls.Add(this.RefreshButton);
			this.Name = "OnlineUserControl";
			this.Size = new System.Drawing.Size(606, 405);
			this.Load += new System.EventHandler(this.OnlineUserControl_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnlineUserControl_KeyDown);
			((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).EndInit();
			this.SettingsListTabControl.ResumeLayout(false);
			this.MySettingsTabPage.ResumeLayout(false);
			this.GlobalSettingsTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.GlobalSettingsDataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView MySettingsDataGridView;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.Button SaveButton;
		private System.Windows.Forms.Button LoadButton;
		private System.Windows.Forms.ComboBox ControllerComboBox;
		private System.Windows.Forms.Label ControllerLabel;
		private System.Windows.Forms.ComboBox GameComboBox;
		private System.Windows.Forms.Label GameLabel;
		private System.Windows.Forms.Button DeleteButton;
		private System.Windows.Forms.TextBox CommentTextBox;
		private System.Windows.Forms.Label CommentLabel;
		public System.Windows.Forms.TextBox OnlineDatabaseUrlTextBox;
		private System.Windows.Forms.TextBox CommentSelectedTextBox;
		private System.Windows.Forms.TabControl SettingsListTabControl;
		private System.Windows.Forms.TabPage MySettingsTabPage;
		private System.Windows.Forms.TabPage GlobalSettingsTabPage;
		private System.Windows.Forms.DataGridView GlobalSettingsDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn SidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MySidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyControllerColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
	}
}
