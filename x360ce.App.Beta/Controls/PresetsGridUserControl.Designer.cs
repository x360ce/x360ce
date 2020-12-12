namespace x360ce.App.Controls
{
	partial class PresetsGridUserControl
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
			this.PresetsDataGridView = new System.Windows.Forms.DataGridView();
			this.PresetSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetTypeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetVendorNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetsDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresetsToolStrip = new System.Windows.Forms.ToolStrip();
			this.PresetRefreshButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).BeginInit();
			this.PresetsToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// PresetsDataGridView
			// 
			this.PresetsDataGridView.AllowUserToAddRows = false;
			this.PresetsDataGridView.AllowUserToDeleteRows = false;
			this.PresetsDataGridView.AllowUserToResizeRows = false;
			this.PresetsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.PresetsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.PresetsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.PresetsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.PresetsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PresetsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PresetSidColumn,
            this.PresetTypeColumn,
            this.PresetVendorNameColumn,
            this.PresetsDeviceNameColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.PresetsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.PresetsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PresetsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.PresetsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.PresetsDataGridView.MultiSelect = false;
			this.PresetsDataGridView.Name = "PresetsDataGridView";
			this.PresetsDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.PresetsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.PresetsDataGridView.RowHeadersVisible = false;
			this.PresetsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.PresetsDataGridView.Size = new System.Drawing.Size(480, 215);
			this.PresetsDataGridView.TabIndex = 4;
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
			this.PresetsToolStrip.Size = new System.Drawing.Size(480, 25);
			this.PresetsToolStrip.TabIndex = 5;
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
			// PresetsGridUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PresetsDataGridView);
			this.Controls.Add(this.PresetsToolStrip);
			this.Name = "PresetsGridUserControl";
			this.Size = new System.Drawing.Size(480, 240);
			((System.ComponentModel.ISupportInitialize)(this.PresetsDataGridView)).EndInit();
			this.PresetsToolStrip.ResumeLayout(false);
			this.PresetsToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetTypeColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetVendorNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresetsDeviceNameColumn;
		private System.Windows.Forms.ToolStrip PresetsToolStrip;
		private System.Windows.Forms.ToolStripButton PresetRefreshButton;
		public System.Windows.Forms.DataGridView PresetsDataGridView;
	}
}
