namespace JocysCom.ClassLibrary.IO
{
	partial class HardwareControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
			this.DisableButton = new System.Windows.Forms.Button();
			this.EnableButton = new System.Windows.Forms.Button();
			this.DeviceTabControl = new System.Windows.Forms.TabControl();
			this.DeviceTabPage = new System.Windows.Forms.TabPage();
			this.DeviceDataGridView = new System.Windows.Forms.DataGridView();
			this.ErrorLabel = new System.Windows.Forms.Label();
			this.RemoveButton = new System.Windows.Forms.Button();
			this.ScanButton = new System.Windows.Forms.Button();
			this.FilterTextBox = new System.Windows.Forms.TextBox();
			this.EnableFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.VendorIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.RevisionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ClassGuidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ClassDescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DeviceDescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ManufacturerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.IsHiddenColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresentColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.RemovableColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DeviceIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.label1 = new System.Windows.Forms.Label();
			this.DeviceTabControl.SuspendLayout();
			this.DeviceTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DeviceDataGridView)).BeginInit();
			this.tabPage1.SuspendLayout();
			this.SuspendLayout();
			// 
			// DisableButton
			// 
			this.DisableButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.DisableButton.Location = new System.Drawing.Point(662, 292);
			this.DisableButton.Name = "DisableButton";
			this.DisableButton.Size = new System.Drawing.Size(75, 23);
			this.DisableButton.TabIndex = 7;
			this.DisableButton.Text = "Disable";
			this.DisableButton.UseVisualStyleBackColor = true;
			this.DisableButton.Click += new System.EventHandler(this.DisableButton_Click);
			// 
			// EnableButton
			// 
			this.EnableButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.EnableButton.Location = new System.Drawing.Point(0, 292);
			this.EnableButton.Name = "EnableButton";
			this.EnableButton.Size = new System.Drawing.Size(75, 23);
			this.EnableButton.TabIndex = 6;
			this.EnableButton.Text = "Enable";
			this.EnableButton.UseVisualStyleBackColor = true;
			this.EnableButton.Click += new System.EventHandler(this.EnableButton_Click);
			// 
			// DeviceTabControl
			// 
			this.DeviceTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DeviceTabControl.Controls.Add(this.DeviceTabPage);
			this.DeviceTabControl.Controls.Add(this.tabPage1);
			this.DeviceTabControl.Location = new System.Drawing.Point(3, 29);
			this.DeviceTabControl.Name = "DeviceTabControl";
			this.DeviceTabControl.SelectedIndex = 0;
			this.DeviceTabControl.Size = new System.Drawing.Size(734, 257);
			this.DeviceTabControl.TabIndex = 9;
			// 
			// DeviceTabPage
			// 
			this.DeviceTabPage.Controls.Add(this.DeviceDataGridView);
			this.DeviceTabPage.Location = new System.Drawing.Point(4, 22);
			this.DeviceTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.DeviceTabPage.Name = "DeviceTabPage";
			this.DeviceTabPage.Size = new System.Drawing.Size(726, 231);
			this.DeviceTabPage.TabIndex = 1;
			this.DeviceTabPage.Text = "XX Devices Attached";
			this.DeviceTabPage.UseVisualStyleBackColor = true;
			// 
			// DeviceDataGridView
			// 
			this.DeviceDataGridView.AllowUserToAddRows = false;
			this.DeviceDataGridView.AllowUserToDeleteRows = false;
			this.DeviceDataGridView.AllowUserToResizeRows = false;
			this.DeviceDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.DeviceDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.DeviceDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			this.DeviceDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.DeviceDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.VendorIdColumn,
            this.ProductIdColumn,
            this.RevisionColumn,
            this.ClassGuidColumn,
            this.ClassDescriptionColumn,
            this.DeviceDescriptionColumn,
            this.ManufacturerColumn,
            this.IsHiddenColumn,
            this.PresentColumn,
            this.StatusColumn,
            this.RemovableColumn,
            this.DeviceIdColumn});
			this.DeviceDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DeviceDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.DeviceDataGridView.Location = new System.Drawing.Point(0, 0);
			this.DeviceDataGridView.Name = "DeviceDataGridView";
			this.DeviceDataGridView.RowHeadersVisible = false;
			this.DeviceDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DeviceDataGridView.Size = new System.Drawing.Size(726, 231);
			this.DeviceDataGridView.TabIndex = 6;
			this.DeviceDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.DeviceDataGridView_CellFormatting);
			this.DeviceDataGridView.SelectionChanged += new System.EventHandler(this.DeviceDataGridView_SelectionChanged);
			// 
			// ErrorLabel
			// 
			this.ErrorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ErrorLabel.AutoSize = true;
			this.ErrorLabel.Location = new System.Drawing.Point(81, 297);
			this.ErrorLabel.Name = "ErrorLabel";
			this.ErrorLabel.Size = new System.Drawing.Size(55, 13);
			this.ErrorLabel.TabIndex = 10;
			this.ErrorLabel.Text = "ErrorLabel";
			this.ErrorLabel.Visible = false;
			// 
			// RemoveButton
			// 
			this.RemoveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveButton.Location = new System.Drawing.Point(581, 292);
			this.RemoveButton.Name = "RemoveButton";
			this.RemoveButton.Size = new System.Drawing.Size(75, 23);
			this.RemoveButton.TabIndex = 11;
			this.RemoveButton.Text = "Remove";
			this.RemoveButton.UseVisualStyleBackColor = true;
			this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
			// 
			// ScanButton
			// 
			this.ScanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ScanButton.Location = new System.Drawing.Point(500, 292);
			this.ScanButton.Name = "ScanButton";
			this.ScanButton.Size = new System.Drawing.Size(75, 23);
			this.ScanButton.TabIndex = 11;
			this.ScanButton.Text = "Scan";
			this.ScanButton.UseVisualStyleBackColor = true;
			this.ScanButton.Click += new System.EventHandler(this.ScanButton_Click);
			// 
			// FilterTextBox
			// 
			this.FilterTextBox.Location = new System.Drawing.Point(96, 3);
			this.FilterTextBox.Name = "FilterTextBox";
			this.FilterTextBox.Size = new System.Drawing.Size(222, 20);
			this.FilterTextBox.TabIndex = 13;
			this.FilterTextBox.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
			// 
			// EnableFilterCheckBox
			// 
			this.EnableFilterCheckBox.AutoSize = true;
			this.EnableFilterCheckBox.Checked = true;
			this.EnableFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.EnableFilterCheckBox.Location = new System.Drawing.Point(3, 5);
			this.EnableFilterCheckBox.Name = "EnableFilterCheckBox";
			this.EnableFilterCheckBox.Size = new System.Drawing.Size(87, 17);
			this.EnableFilterCheckBox.TabIndex = 14;
			this.EnableFilterCheckBox.Text = "Enable Filter:";
			this.EnableFilterCheckBox.UseVisualStyleBackColor = true;
			this.EnableFilterCheckBox.CheckedChanged += new System.EventHandler(this.EnableFilderCheckBox_CheckedChanged);
			// 
			// VendorIdColumn
			// 
			this.VendorIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.VendorIdColumn.DataPropertyName = "VendorId";
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.VendorIdColumn.DefaultCellStyle = dataGridViewCellStyle4;
			this.VendorIdColumn.HeaderText = "VID";
			this.VendorIdColumn.Name = "VendorIdColumn";
			this.VendorIdColumn.Width = 50;
			// 
			// ProductIdColumn
			// 
			this.ProductIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ProductIdColumn.DataPropertyName = "ProductId";
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.ProductIdColumn.DefaultCellStyle = dataGridViewCellStyle5;
			this.ProductIdColumn.HeaderText = "PID";
			this.ProductIdColumn.Name = "ProductIdColumn";
			this.ProductIdColumn.Width = 50;
			// 
			// RevisionColumn
			// 
			this.RevisionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.RevisionColumn.DataPropertyName = "Revision";
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.RevisionColumn.DefaultCellStyle = dataGridViewCellStyle6;
			this.RevisionColumn.HeaderText = "REV";
			this.RevisionColumn.Name = "RevisionColumn";
			this.RevisionColumn.Width = 54;
			// 
			// ClassGuidColumn
			// 
			this.ClassGuidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ClassGuidColumn.DataPropertyName = "ClassGuid";
			this.ClassGuidColumn.HeaderText = "Class GUID";
			this.ClassGuidColumn.Name = "ClassGuidColumn";
			this.ClassGuidColumn.Visible = false;
			this.ClassGuidColumn.Width = 87;
			// 
			// ClassDescriptionColumn
			// 
			this.ClassDescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ClassDescriptionColumn.DataPropertyName = "ClassDescription";
			this.ClassDescriptionColumn.HeaderText = "Class Description";
			this.ClassDescriptionColumn.Name = "ClassDescriptionColumn";
			this.ClassDescriptionColumn.Width = 113;
			// 
			// DeviceDescriptionColumn
			// 
			this.DeviceDescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DeviceDescriptionColumn.DataPropertyName = "Description";
			this.DeviceDescriptionColumn.HeaderText = "Description";
			this.DeviceDescriptionColumn.Name = "DeviceDescriptionColumn";
			this.DeviceDescriptionColumn.Width = 85;
			// 
			// ManufacturerColumn
			// 
			this.ManufacturerColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ManufacturerColumn.DataPropertyName = "Manufacturer";
			this.ManufacturerColumn.HeaderText = "Manufacturer";
			this.ManufacturerColumn.Name = "ManufacturerColumn";
			this.ManufacturerColumn.Width = 95;
			// 
			// IsHiddenColumn
			// 
			this.IsHiddenColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.IsHiddenColumn.DataPropertyName = "IsHidden";
			this.IsHiddenColumn.HeaderText = "Hidden";
			this.IsHiddenColumn.Name = "IsHiddenColumn";
			this.IsHiddenColumn.Visible = false;
			this.IsHiddenColumn.Width = 66;
			// 
			// PresentColumn
			// 
			this.PresentColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PresentColumn.DataPropertyName = "IsPresent";
			this.PresentColumn.HeaderText = "Present";
			this.PresentColumn.Name = "PresentColumn";
			this.PresentColumn.Visible = false;
			this.PresentColumn.Width = 68;
			// 
			// StatusColumn
			// 
			this.StatusColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.StatusColumn.DataPropertyName = "Status";
			this.StatusColumn.HeaderText = "Status";
			this.StatusColumn.Name = "StatusColumn";
			this.StatusColumn.Width = 62;
			// 
			// RemovableColumn
			// 
			this.RemovableColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.RemovableColumn.DataPropertyName = "IsRemovable";
			this.RemovableColumn.HeaderText = "Removable";
			this.RemovableColumn.Name = "RemovableColumn";
			this.RemovableColumn.Width = 86;
			// 
			// DeviceIdColumn
			// 
			this.DeviceIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DeviceIdColumn.DataPropertyName = "DeviceId";
			this.DeviceIdColumn.HeaderText = "Device ID";
			this.DeviceIdColumn.Name = "DeviceIdColumn";
			this.DeviceIdColumn.Width = 80;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.treeView1);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(726, 231);
			this.tabPage1.TabIndex = 2;
			this.tabPage1.Text = "Tree";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Left;
			this.treeView1.Location = new System.Drawing.Point(3, 3);
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(401, 225);
			this.treeView1.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(420, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(35, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "label1";
			// 
			// HardwareControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.EnableFilterCheckBox);
			this.Controls.Add(this.FilterTextBox);
			this.Controls.Add(this.ScanButton);
			this.Controls.Add(this.RemoveButton);
			this.Controls.Add(this.ErrorLabel);
			this.Controls.Add(this.DeviceTabControl);
			this.Controls.Add(this.DisableButton);
			this.Controls.Add(this.EnableButton);
			this.Name = "HardwareControl";
			this.Size = new System.Drawing.Size(740, 318);
			this.Load += new System.EventHandler(this.HardwareControl_Load);
			this.DeviceTabControl.ResumeLayout(false);
			this.DeviceTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.DeviceDataGridView)).EndInit();
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button DisableButton;
		private System.Windows.Forms.Button EnableButton;
		private System.Windows.Forms.TabControl DeviceTabControl;
		private System.Windows.Forms.TabPage DeviceTabPage;
		private System.Windows.Forms.DataGridView DeviceDataGridView;
		private System.Windows.Forms.Label ErrorLabel;
		private System.Windows.Forms.Button RemoveButton;
		private System.Windows.Forms.Button ScanButton;
		private System.Windows.Forms.TextBox FilterTextBox;
		private System.Windows.Forms.CheckBox EnableFilterCheckBox;
		private System.Windows.Forms.DataGridViewTextBoxColumn VendorIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn RevisionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ClassGuidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ClassDescriptionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DeviceDescriptionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ManufacturerColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn IsHiddenColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresentColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn RemovableColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DeviceIdColumn;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Label label1;
	}
}
