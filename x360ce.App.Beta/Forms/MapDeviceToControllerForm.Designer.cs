namespace x360ce.App.Controls
{
	partial class MapDeviceToControllerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

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
			this.SettingsListTabControl = new System.Windows.Forms.TabControl();
			this.AvailableDInputDevicesTabPage = new System.Windows.Forms.TabPage();
			this.AvailableDInputDevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.InstanceIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.VendorNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.SettingsListTabControl.SuspendLayout();
			this.AvailableDInputDevicesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AvailableDInputDevicesDataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// SettingsListTabControl
			// 
			this.SettingsListTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SettingsListTabControl.Controls.Add(this.AvailableDInputDevicesTabPage);
			this.SettingsListTabControl.Location = new System.Drawing.Point(12, 70);
			this.SettingsListTabControl.Name = "SettingsListTabControl";
			this.SettingsListTabControl.SelectedIndex = 0;
			this.SettingsListTabControl.Size = new System.Drawing.Size(600, 331);
			this.SettingsListTabControl.TabIndex = 23;
			// 
			// AvailableDInputDevicesTabPage
			// 
			this.AvailableDInputDevicesTabPage.Controls.Add(this.AvailableDInputDevicesDataGridView);
			this.AvailableDInputDevicesTabPage.Location = new System.Drawing.Point(4, 22);
			this.AvailableDInputDevicesTabPage.Name = "AvailableDInputDevicesTabPage";
			this.AvailableDInputDevicesTabPage.Size = new System.Drawing.Size(592, 305);
			this.AvailableDInputDevicesTabPage.TabIndex = 2;
			this.AvailableDInputDevicesTabPage.Text = "Available Direct Input Devices";
			this.AvailableDInputDevicesTabPage.UseVisualStyleBackColor = true;
			// 
			// AvailableDInputDevicesDataGridView
			// 
			this.AvailableDInputDevicesDataGridView.AllowUserToAddRows = false;
			this.AvailableDInputDevicesDataGridView.AllowUserToDeleteRows = false;
			this.AvailableDInputDevicesDataGridView.AllowUserToResizeRows = false;
			this.AvailableDInputDevicesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.AvailableDInputDevicesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.AvailableDInputDevicesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.AvailableDInputDevicesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.AvailableDInputDevicesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.AvailableDInputDevicesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.InstanceIdColumn,
            this.VendorNameColumn,
            this.ProductNameColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.AvailableDInputDevicesDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.AvailableDInputDevicesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AvailableDInputDevicesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.AvailableDInputDevicesDataGridView.Location = new System.Drawing.Point(0, 0);
			this.AvailableDInputDevicesDataGridView.MultiSelect = false;
			this.AvailableDInputDevicesDataGridView.Name = "AvailableDInputDevicesDataGridView";
			this.AvailableDInputDevicesDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.AvailableDInputDevicesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.AvailableDInputDevicesDataGridView.RowHeadersVisible = false;
			this.AvailableDInputDevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.AvailableDInputDevicesDataGridView.Size = new System.Drawing.Size(592, 305);
			this.AvailableDInputDevicesDataGridView.TabIndex = 69;
			this.AvailableDInputDevicesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.AvailableDInputDevicesDataGridView_CellFormatting);
			// 
			// InstanceIdColumn
			// 
			this.InstanceIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.InstanceIdColumn.HeaderText = "Instance ID";
			this.InstanceIdColumn.Name = "InstanceIdColumn";
			this.InstanceIdColumn.ReadOnly = true;
			this.InstanceIdColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.InstanceIdColumn.Width = 68;
			// 
			// VendorNameColumn
			// 
			this.VendorNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.VendorNameColumn.DataPropertyName = "VendorName";
			this.VendorNameColumn.HeaderText = "Vendor Name";
			this.VendorNameColumn.Name = "VendorNameColumn";
			this.VendorNameColumn.ReadOnly = true;
			this.VendorNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.VendorNameColumn.Width = 78;
			// 
			// ProductNameColumn
			// 
			this.ProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProductNameColumn.DataPropertyName = "ProductName";
			this.ProductNameColumn.HeaderText = "Product Name";
			this.ProductNameColumn.Name = "ProductNameColumn";
			this.ProductNameColumn.ReadOnly = true;
			this.ProductNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(456, 407);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(75, 23);
			this.OkButton.TabIndex = 24;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(537, 407);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 25;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// MapDeviceToControllerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 442);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.SettingsListTabControl);
			this.Name = "MapDeviceToControllerForm";
			this.Text = "X360CE - Map Device To Controller";
			this.Load += new System.EventHandler(this.MapDeviceToControllerForm_Load);
			this.Controls.SetChildIndex(this.SettingsListTabControl, 0);
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.SettingsListTabControl.ResumeLayout(false);
			this.AvailableDInputDevicesTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.AvailableDInputDevicesDataGridView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl SettingsListTabControl;
		private System.Windows.Forms.TabPage AvailableDInputDevicesTabPage;
		private System.Windows.Forms.DataGridView AvailableDInputDevicesDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn InstanceIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn VendorNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
	}
}