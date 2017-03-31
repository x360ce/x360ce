namespace x360ce.App.Controls
{
	partial class ControllerSettingsUserControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.MySettingsDataGridView = new System.Windows.Forms.DataGridView();
            this.CommentLabel = new System.Windows.Forms.Label();
            this.CommentSelectedTextBox = new System.Windows.Forms.TextBox();
            this.SettingsListTabControl = new System.Windows.Forms.TabControl();
            this.MySettingsTabPage = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.MySettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
            this.MySettingsDeleteButton = new System.Windows.Forms.ToolStripButton();
            this.SummariesTabPage = new System.Windows.Forms.TabPage();
            this.SummariesDataGridView = new System.Windows.Forms.DataGridView();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.GlobalSettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
            this.GlobalSettingsLoadButton = new System.Windows.Forms.ToolStripButton();
            this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MyDeviceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MySidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MapToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).BeginInit();
            this.SettingsListTabControl.SuspendLayout();
            this.MySettingsTabPage.SuspendLayout();
            this.panel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SummariesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).BeginInit();
            this.toolStrip2.SuspendLayout();
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
            this.MyFileColumn,
            this.MyGameColumn,
            this.MyDeviceColumn,
            this.MySidColumn,
            this.MapToColumn});
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
            this.MySettingsDataGridView.Size = new System.Drawing.Size(632, 440);
            this.MySettingsDataGridView.TabIndex = 0;
            this.MySettingsDataGridView.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.SettingsDataGridView_CellContentDoubleClick);
            this.MySettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SettingsDataGridView_CellFormatting);
            this.MySettingsDataGridView.SelectionChanged += new System.EventHandler(this.SettingsDataGridView_SelectionChanged);
            // 
            // CommentLabel
            // 
            this.CommentLabel.AutoSize = true;
            this.CommentLabel.Location = new System.Drawing.Point(3, 3);
            this.CommentLabel.Name = "CommentLabel";
            this.CommentLabel.Size = new System.Drawing.Size(38, 13);
            this.CommentLabel.TabIndex = 5;
            this.CommentLabel.Text = "&Notes:";
            // 
            // CommentSelectedTextBox
            // 
            this.CommentSelectedTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CommentSelectedTextBox.Location = new System.Drawing.Point(11, 19);
            this.CommentSelectedTextBox.Multiline = true;
            this.CommentSelectedTextBox.Name = "CommentSelectedTextBox";
            this.CommentSelectedTextBox.Size = new System.Drawing.Size(610, 29);
            this.CommentSelectedTextBox.TabIndex = 20;
            // 
            // SettingsListTabControl
            // 
            this.SettingsListTabControl.Controls.Add(this.MySettingsTabPage);
            this.SettingsListTabControl.Controls.Add(this.SummariesTabPage);
            this.SettingsListTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsListTabControl.Location = new System.Drawing.Point(0, 0);
            this.SettingsListTabControl.Name = "SettingsListTabControl";
            this.SettingsListTabControl.SelectedIndex = 0;
            this.SettingsListTabControl.Size = new System.Drawing.Size(640, 491);
            this.SettingsListTabControl.TabIndex = 18;
            this.SettingsListTabControl.SelectedIndexChanged += new System.EventHandler(this.SettingsListTabControl_SelectedIndexChanged);
            // 
            // MySettingsTabPage
            // 
            this.MySettingsTabPage.Controls.Add(this.panel2);
            this.MySettingsTabPage.Controls.Add(this.panel1);
            this.MySettingsTabPage.Controls.Add(this.MySettingsDataGridView);
            this.MySettingsTabPage.Controls.Add(this.toolStrip1);
            this.MySettingsTabPage.Location = new System.Drawing.Point(4, 22);
            this.MySettingsTabPage.Name = "MySettingsTabPage";
            this.MySettingsTabPage.Size = new System.Drawing.Size(632, 465);
            this.MySettingsTabPage.TabIndex = 0;
            this.MySettingsTabPage.Text = "My Game, Device and Controller Setting Map";
            this.MySettingsTabPage.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 405);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(632, 1);
            this.panel2.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.CommentSelectedTextBox);
            this.panel1.Controls.Add(this.CommentLabel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 406);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(8, 16, 8, 8);
            this.panel1.Size = new System.Drawing.Size(632, 59);
            this.panel1.TabIndex = 2;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MySettingsRefreshButton,
            this.MySettingsDeleteButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip1.Size = new System.Drawing.Size(632, 25);
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
            this.MySettingsRefreshButton.Click += new System.EventHandler(this.MySettingsRefreshButton_Click);
            // 
            // MySettingsDeleteButton
            // 
            this.MySettingsDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.MySettingsDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
            this.MySettingsDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MySettingsDeleteButton.Name = "MySettingsDeleteButton";
            this.MySettingsDeleteButton.Size = new System.Drawing.Size(60, 22);
            this.MySettingsDeleteButton.Text = "&Delete";
            this.MySettingsDeleteButton.Click += new System.EventHandler(this.MySettingsDeleteButton_Click);
            // 
            // SummariesTabPage
            // 
            this.SummariesTabPage.Controls.Add(this.SummariesDataGridView);
            this.SummariesTabPage.Controls.Add(this.toolStrip2);
            this.SummariesTabPage.Location = new System.Drawing.Point(4, 22);
            this.SummariesTabPage.Name = "SummariesTabPage";
            this.SummariesTabPage.Size = new System.Drawing.Size(632, 465);
            this.SummariesTabPage.TabIndex = 1;
            this.SummariesTabPage.Text = "Most Popular Settings for My Games and Devices";
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
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.SidColumn,
            this.dataGridViewTextBoxColumn1});
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
            this.SummariesDataGridView.Size = new System.Drawing.Size(632, 440);
            this.SummariesDataGridView.TabIndex = 1;
            this.SummariesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SummariesDataGridView_CellFormatting);
            this.SummariesDataGridView.SelectionChanged += new System.EventHandler(this.SummariesDataGridView_SelectionChanged);
            this.SummariesDataGridView.DoubleClick += new System.EventHandler(this.SummariesDataGridView_DoubleClick);
            // 
            // toolStrip2
            // 
            this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GlobalSettingsRefreshButton,
            this.GlobalSettingsLoadButton});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip2.Size = new System.Drawing.Size(632, 25);
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
            this.GlobalSettingsRefreshButton.Click += new System.EventHandler(this.GlobalSettingsRefreshButton_Click);
            // 
            // GlobalSettingsLoadButton
            // 
            this.GlobalSettingsLoadButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.GlobalSettingsLoadButton.Image = global::x360ce.App.Properties.Resources.load_16x16;
            this.GlobalSettingsLoadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.GlobalSettingsLoadButton.Name = "GlobalSettingsLoadButton";
            this.GlobalSettingsLoadButton.Size = new System.Drawing.Size(53, 22);
            this.GlobalSettingsLoadButton.Text = "&Load";
            this.GlobalSettingsLoadButton.Click += new System.EventHandler(this.GlobalSettingsLoadButton_Click);
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
            // MyDeviceColumn
            // 
            this.MyDeviceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.MyDeviceColumn.DataPropertyName = "ProductName";
            this.MyDeviceColumn.HeaderText = "Device";
            this.MyDeviceColumn.Name = "MyDeviceColumn";
            this.MyDeviceColumn.ReadOnly = true;
            this.MyDeviceColumn.Width = 66;
            // 
            // MySidColumn
            // 
            this.MySidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.MySidColumn.DataPropertyName = "PadSettingChecksum";
            this.MySidColumn.HeaderText = "Setting ID";
            this.MySidColumn.Name = "MySidColumn";
            this.MySidColumn.ReadOnly = true;
            this.MySidColumn.Width = 79;
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
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn2.DataPropertyName = "ProductName";
            this.dataGridViewTextBoxColumn2.HeaderText = "Device";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 66;
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
            // SidColumn
            // 
            this.SidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SidColumn.DataPropertyName = "PadSettingChecksum";
            this.SidColumn.HeaderText = "Setting ID";
            this.SidColumn.Name = "SidColumn";
            this.SidColumn.ReadOnly = true;
            this.SidColumn.Width = 79;
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
            // ControllerSettingsUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SettingsListTabControl);
            this.Name = "ControllerSettingsUserControl";
            this.Size = new System.Drawing.Size(640, 491);
            this.Load += new System.EventHandler(this.InternetUserControl_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InternetUserControl_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).EndInit();
            this.SettingsListTabControl.ResumeLayout(false);
            this.MySettingsTabPage.ResumeLayout(false);
            this.MySettingsTabPage.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.SummariesTabPage.ResumeLayout(false);
            this.SummariesTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).EndInit();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

        System.Windows.Forms.DataGridView MySettingsDataGridView;
		System.Windows.Forms.Label CommentLabel;
		System.Windows.Forms.TextBox CommentSelectedTextBox;
		System.Windows.Forms.TabControl SettingsListTabControl;
		System.Windows.Forms.TabPage MySettingsTabPage;
		System.Windows.Forms.TabPage SummariesTabPage;
		System.Windows.Forms.DataGridView SummariesDataGridView;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton MySettingsDeleteButton;
        private System.Windows.Forms.ToolStripButton MySettingsRefreshButton;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton GlobalSettingsRefreshButton;
        private System.Windows.Forms.ToolStripButton GlobalSettingsLoadButton;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyDeviceColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MySidColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MapToColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn SidColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    }
}
