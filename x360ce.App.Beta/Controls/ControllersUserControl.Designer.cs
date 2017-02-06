namespace x360ce.App.Controls
{
	partial class ControllersUserControl
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
			this.ControllersTabControl = new System.Windows.Forms.TabControl();
			this.DirectInputDevicesTabPage = new System.Windows.Forms.TabPage();
			this.ControllersDataGridView = new System.Windows.Forms.DataGridView();
			this.ControllersToolStrip = new System.Windows.Forms.ToolStrip();
			this.ControllerDeleteButton = new System.Windows.Forms.ToolStripButton();
			this.RefreshButton = new System.Windows.Forms.ToolStripButton();
			this.MyIconColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.MySidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyDeviceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ControllersTabControl.SuspendLayout();
			this.DirectInputDevicesTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ControllersDataGridView)).BeginInit();
			this.ControllersToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// ControllersTabControl
			// 
			this.ControllersTabControl.Controls.Add(this.DirectInputDevicesTabPage);
			this.ControllersTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControllersTabControl.Location = new System.Drawing.Point(3, 3);
			this.ControllersTabControl.Name = "ControllersTabControl";
			this.ControllersTabControl.SelectedIndex = 0;
			this.ControllersTabControl.Size = new System.Drawing.Size(711, 430);
			this.ControllersTabControl.TabIndex = 19;
			// 
			// DirectInputDevicesTabPage
			// 
			this.DirectInputDevicesTabPage.Controls.Add(this.ControllersDataGridView);
			this.DirectInputDevicesTabPage.Controls.Add(this.ControllersToolStrip);
			this.DirectInputDevicesTabPage.Location = new System.Drawing.Point(4, 22);
			this.DirectInputDevicesTabPage.Name = "DirectInputDevicesTabPage";
			this.DirectInputDevicesTabPage.Size = new System.Drawing.Size(703, 404);
			this.DirectInputDevicesTabPage.TabIndex = 0;
			this.DirectInputDevicesTabPage.Text = "Direct Input Devices";
			this.DirectInputDevicesTabPage.UseVisualStyleBackColor = true;
			// 
			// ControllersDataGridView
			// 
			this.ControllersDataGridView.AllowUserToAddRows = false;
			this.ControllersDataGridView.AllowUserToDeleteRows = false;
			this.ControllersDataGridView.AllowUserToResizeRows = false;
			this.ControllersDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.ControllersDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ControllersDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ControllersDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.ControllersDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ControllersDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MyIconColumn,
            this.MySidColumn,
            this.MyDeviceColumn,
            this.MyFileColumn,
            this.MyGameColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ControllersDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.ControllersDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControllersDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.ControllersDataGridView.Location = new System.Drawing.Point(0, 25);
			this.ControllersDataGridView.MultiSelect = false;
			this.ControllersDataGridView.Name = "ControllersDataGridView";
			this.ControllersDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ControllersDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.ControllersDataGridView.RowHeadersVisible = false;
			this.ControllersDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ControllersDataGridView.Size = new System.Drawing.Size(703, 379);
			this.ControllersDataGridView.TabIndex = 0;
			this.ControllersDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.ControllersDataGridView_CellFormatting);
			// 
			// ControllersToolStrip
			// 
			this.ControllersToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.ControllersToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshButton,
            this.ControllerDeleteButton});
			this.ControllersToolStrip.Location = new System.Drawing.Point(0, 0);
			this.ControllersToolStrip.Name = "ControllersToolStrip";
			this.ControllersToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.ControllersToolStrip.Size = new System.Drawing.Size(703, 25);
			this.ControllersToolStrip.TabIndex = 1;
			this.ControllersToolStrip.Text = "MySettingsToolStrip";
			// 
			// ControllerDeleteButton
			// 
			this.ControllerDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.ControllerDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.ControllerDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ControllerDeleteButton.Name = "ControllerDeleteButton";
			this.ControllerDeleteButton.Size = new System.Drawing.Size(60, 22);
			this.ControllerDeleteButton.Text = "&Delete";
			// 
			// RefreshButton
			// 
			this.RefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(66, 22);
			this.RefreshButton.Text = "&Refresh";
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// MyIconColumn
			// 
			this.MyIconColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyIconColumn.DataPropertyName = "IsOnline";
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
			this.MySidColumn.DataPropertyName = "InstanceId";
			this.MySidColumn.HeaderText = "Instance ID";
			this.MySidColumn.Name = "MySidColumn";
			this.MySidColumn.ReadOnly = true;
			this.MySidColumn.Width = 87;
			// 
			// MyDeviceColumn
			// 
			this.MyDeviceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyDeviceColumn.DataPropertyName = "VendorName";
			this.MyDeviceColumn.HeaderText = "Vendor Name";
			this.MyDeviceColumn.Name = "MyDeviceColumn";
			this.MyDeviceColumn.ReadOnly = true;
			this.MyDeviceColumn.Width = 97;
			// 
			// MyFileColumn
			// 
			this.MyFileColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MyFileColumn.DataPropertyName = "ProductName";
			this.MyFileColumn.HeaderText = "Product Name";
			this.MyFileColumn.Name = "MyFileColumn";
			this.MyFileColumn.ReadOnly = true;
			// 
			// MyGameColumn
			// 
			this.MyGameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.MyGameColumn.DataPropertyName = "DeviceId";
			this.MyGameColumn.HeaderText = "Device ID";
			this.MyGameColumn.Name = "MyGameColumn";
			this.MyGameColumn.ReadOnly = true;
			// 
			// ControllersUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.ControllersTabControl);
			this.Name = "ControllersUserControl";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Size = new System.Drawing.Size(717, 436);
			this.Load += new System.EventHandler(this.ControllersUserControl_Load);
			this.ControllersTabControl.ResumeLayout(false);
			this.DirectInputDevicesTabPage.ResumeLayout(false);
			this.DirectInputDevicesTabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ControllersDataGridView)).EndInit();
			this.ControllersToolStrip.ResumeLayout(false);
			this.ControllersToolStrip.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl ControllersTabControl;
		private System.Windows.Forms.TabPage DirectInputDevicesTabPage;
		private System.Windows.Forms.DataGridView ControllersDataGridView;
		private System.Windows.Forms.ToolStrip ControllersToolStrip;
		private System.Windows.Forms.ToolStripButton ControllerDeleteButton;
		private System.Windows.Forms.ToolStripButton RefreshButton;
		private System.Windows.Forms.DataGridViewImageColumn MyIconColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MySidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyDeviceColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
	}
}
