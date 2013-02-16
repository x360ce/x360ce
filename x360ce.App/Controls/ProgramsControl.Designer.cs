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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this.ProgramsListTabControl = new System.Windows.Forms.TabControl();
			this.ProgramsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsDataGridView = new System.Windows.Forms.DataGridView();
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
			this.InstallFilesXinput13CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput12CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput11CheckBox = new System.Windows.Forms.CheckBox();
			this.InstallFilesXinput910CheckBox = new System.Windows.Forms.CheckBox();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramsListTabControl.SuspendLayout();
			this.ProgramsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).BeginInit();
			this.HookMaskGroupBox.SuspendLayout();
			this.InstalledFilesGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// ProgramsListTabControl
			// 
			this.ProgramsListTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProgramsListTabControl.Controls.Add(this.ProgramsTabPage);
			this.ProgramsListTabControl.Location = new System.Drawing.Point(3, 3);
			this.ProgramsListTabControl.Name = "ProgramsListTabControl";
			this.ProgramsListTabControl.SelectedIndex = 0;
			this.ProgramsListTabControl.Size = new System.Drawing.Size(330, 426);
			this.ProgramsListTabControl.TabIndex = 19;
			// 
			// ProgramsTabPage
			// 
			this.ProgramsTabPage.Controls.Add(this.SettingsDataGridView);
			this.ProgramsTabPage.Location = new System.Drawing.Point(4, 22);
			this.ProgramsTabPage.Name = "ProgramsTabPage";
			this.ProgramsTabPage.Size = new System.Drawing.Size(322, 400);
			this.ProgramsTabPage.TabIndex = 0;
			this.ProgramsTabPage.Text = "Programs and Games:";
			this.ProgramsTabPage.UseVisualStyleBackColor = true;
			// 
			// SettingsDataGridView
			// 
			this.SettingsDataGridView.AllowUserToAddRows = false;
			this.SettingsDataGridView.AllowUserToDeleteRows = false;
			this.SettingsDataGridView.AllowUserToResizeRows = false;
			this.SettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.SettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.SettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MyIconColumn,
            this.MyFileColumn,
            this.MyGameColumn});
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle5;
			this.SettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.SettingsDataGridView.Location = new System.Drawing.Point(0, 0);
			this.SettingsDataGridView.MultiSelect = false;
			this.SettingsDataGridView.Name = "SettingsDataGridView";
			this.SettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
			this.SettingsDataGridView.RowHeadersVisible = false;
			this.SettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.SettingsDataGridView.Size = new System.Drawing.Size(322, 400);
			this.SettingsDataGridView.TabIndex = 0;
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
			this.HookMaskGroupBox.Location = new System.Drawing.Point(339, 3);
			this.HookMaskGroupBox.Name = "HookMaskGroupBox";
			this.HookMaskGroupBox.Size = new System.Drawing.Size(181, 253);
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
			this.InstalledFilesGroupBox.Controls.Add(this.textBox1);
			this.InstalledFilesGroupBox.Controls.Add(this.checkBox1);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput13CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput12CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput11CheckBox);
			this.InstalledFilesGroupBox.Controls.Add(this.InstallFilesXinput910CheckBox);
			this.InstalledFilesGroupBox.Location = new System.Drawing.Point(339, 262);
			this.InstalledFilesGroupBox.Name = "InstalledFilesGroupBox";
			this.InstalledFilesGroupBox.Size = new System.Drawing.Size(181, 162);
			this.InstalledFilesGroupBox.TabIndex = 21;
			this.InstalledFilesGroupBox.TabStop = false;
			this.InstalledFilesGroupBox.Text = "Installed XInput Files";
			// 
			// InstallFilesXinput13CheckBox
			// 
			this.InstallFilesXinput13CheckBox.AutoSize = true;
			this.InstallFilesXinput13CheckBox.Location = new System.Drawing.Point(6, 88);
			this.InstallFilesXinput13CheckBox.Name = "InstallFilesXinput13CheckBox";
			this.InstallFilesXinput13CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput13CheckBox.TabIndex = 0;
			this.InstallFilesXinput13CheckBox.Text = "xinput1_3.dll";
			this.InstallFilesXinput13CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput12CheckBox
			// 
			this.InstallFilesXinput12CheckBox.AutoSize = true;
			this.InstallFilesXinput12CheckBox.Location = new System.Drawing.Point(6, 65);
			this.InstallFilesXinput12CheckBox.Name = "InstallFilesXinput12CheckBox";
			this.InstallFilesXinput12CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput12CheckBox.TabIndex = 0;
			this.InstallFilesXinput12CheckBox.Text = "xinput1_2.dll";
			this.InstallFilesXinput12CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput11CheckBox
			// 
			this.InstallFilesXinput11CheckBox.AutoSize = true;
			this.InstallFilesXinput11CheckBox.Location = new System.Drawing.Point(6, 42);
			this.InstallFilesXinput11CheckBox.Name = "InstallFilesXinput11CheckBox";
			this.InstallFilesXinput11CheckBox.Size = new System.Drawing.Size(85, 17);
			this.InstallFilesXinput11CheckBox.TabIndex = 0;
			this.InstallFilesXinput11CheckBox.Text = "xinput1_1.dll";
			this.InstallFilesXinput11CheckBox.UseVisualStyleBackColor = true;
			// 
			// InstallFilesXinput910CheckBox
			// 
			this.InstallFilesXinput910CheckBox.AutoSize = true;
			this.InstallFilesXinput910CheckBox.Location = new System.Drawing.Point(6, 19);
			this.InstallFilesXinput910CheckBox.Name = "InstallFilesXinput910CheckBox";
			this.InstallFilesXinput910CheckBox.Size = new System.Drawing.Size(97, 17);
			this.InstallFilesXinput910CheckBox.TabIndex = 0;
			this.InstallFilesXinput910CheckBox.Text = "xinput9_1_0.dll";
			this.InstallFilesXinput910CheckBox.UseVisualStyleBackColor = true;
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(6, 111);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(85, 17);
			this.checkBox1.TabIndex = 0;
			this.checkBox1.Text = "xinput1_4.dll";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(6, 134);
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(71, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.Text = "0x00000000";
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
			// ProgramsControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.InstalledFilesGroupBox);
			this.Controls.Add(this.HookMaskGroupBox);
			this.Controls.Add(this.ProgramsListTabControl);
			this.Name = "ProgramsControl";
			this.Size = new System.Drawing.Size(523, 432);
			this.ProgramsListTabControl.ResumeLayout(false);
			this.ProgramsTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).EndInit();
			this.HookMaskGroupBox.ResumeLayout(false);
			this.HookMaskGroupBox.PerformLayout();
			this.InstalledFilesGroupBox.ResumeLayout(false);
			this.InstalledFilesGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		System.Windows.Forms.TabControl ProgramsListTabControl;
		System.Windows.Forms.TabPage ProgramsTabPage;
		System.Windows.Forms.DataGridView SettingsDataGridView;
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
		System.Windows.Forms.TextBox textBox1;
		System.Windows.Forms.CheckBox checkBox1;
		System.Windows.Forms.CheckBox InstallFilesXinput13CheckBox;
		System.Windows.Forms.CheckBox InstallFilesXinput12CheckBox;
		System.Windows.Forms.CheckBox InstallFilesXinput11CheckBox;
		System.Windows.Forms.CheckBox InstallFilesXinput910CheckBox;
		System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
	}
}
