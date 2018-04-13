namespace x360ce.App.Controls
{
	partial class UserDevicesUserControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserDevicesUserControl));
			this.DevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.IsOnlineColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.IsEnabledColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.MySidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyDeviceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyGameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ControllersToolStrip = new System.Windows.Forms.ToolStrip();
			this.RefreshButton = new System.Windows.Forms.ToolStripButton();
			this.ControllerDeleteButton = new System.Windows.Forms.ToolStripButton();
			this.HardwareButton = new System.Windows.Forms.ToolStripButton();
			this.AddDemoDevice = new System.Windows.Forms.ToolStripButton();
			this.HiddenDevicesButton = new System.Windows.Forms.ToolStripButton();
			this.HideVirtualButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).BeginInit();
			this.ControllersToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// DevicesDataGridView
			// 
			this.DevicesDataGridView.AllowUserToAddRows = false;
			this.DevicesDataGridView.AllowUserToDeleteRows = false;
			this.DevicesDataGridView.AllowUserToResizeRows = false;
			this.DevicesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.DevicesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.DevicesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.DevicesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.DevicesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.DevicesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.IsOnlineColumn,
            this.IsEnabledColumn,
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
			this.DevicesDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.DevicesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.DevicesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.DevicesDataGridView.Name = "DevicesDataGridView";
			this.DevicesDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.DevicesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.DevicesDataGridView.RowHeadersVisible = false;
			this.DevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DevicesDataGridView.Size = new System.Drawing.Size(717, 411);
			this.DevicesDataGridView.TabIndex = 0;
			this.DevicesDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DevicesDataGridView_CellClick);
			this.DevicesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.ControllersDataGridView_CellFormatting);
			// 
			// IsOnlineColumn
			// 
			this.IsOnlineColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.IsOnlineColumn.DataPropertyName = "IsOnline";
			this.IsOnlineColumn.HeaderText = "";
			this.IsOnlineColumn.MinimumWidth = 24;
			this.IsOnlineColumn.Name = "IsOnlineColumn";
			this.IsOnlineColumn.ReadOnly = true;
			this.IsOnlineColumn.Width = 24;
			// 
			// IsEnabledColumn
			// 
			this.IsEnabledColumn.DataPropertyName = "IsEnabled";
			this.IsEnabledColumn.HeaderText = "";
			this.IsEnabledColumn.Name = "IsEnabledColumn";
			this.IsEnabledColumn.ReadOnly = true;
			this.IsEnabledColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.IsEnabledColumn.Width = 24;
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
			this.MyDeviceColumn.DataPropertyName = "HidManufacturer";
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
			this.MyGameColumn.DataPropertyName = "HidDeviceId";
			this.MyGameColumn.HeaderText = "Device ID";
			this.MyGameColumn.Name = "MyGameColumn";
			this.MyGameColumn.ReadOnly = true;
			// 
			// ControllersToolStrip
			// 
			this.ControllersToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.ControllersToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshButton,
            this.ControllerDeleteButton,
            this.HardwareButton,
            this.AddDemoDevice,
            this.HiddenDevicesButton,
            this.HideVirtualButton});
			this.ControllersToolStrip.Location = new System.Drawing.Point(0, 0);
			this.ControllersToolStrip.Name = "ControllersToolStrip";
			this.ControllersToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.ControllersToolStrip.Size = new System.Drawing.Size(717, 25);
			this.ControllersToolStrip.TabIndex = 1;
			this.ControllersToolStrip.Text = "MySettingsToolStrip";
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
			// ControllerDeleteButton
			// 
			this.ControllerDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.ControllerDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.ControllerDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ControllerDeleteButton.Name = "ControllerDeleteButton";
			this.ControllerDeleteButton.Size = new System.Drawing.Size(60, 22);
			this.ControllerDeleteButton.Text = "&Delete";
			this.ControllerDeleteButton.Click += new System.EventHandler(this.ControllerDeleteButton_Click);
			// 
			// HardwareButton
			// 
			this.HardwareButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.HardwareButton.Image = ((System.Drawing.Image)(resources.GetObject("HardwareButton.Image")));
			this.HardwareButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.HardwareButton.Name = "HardwareButton";
			this.HardwareButton.Size = new System.Drawing.Size(71, 22);
			this.HardwareButton.Text = "Hardware...";
			this.HardwareButton.Click += new System.EventHandler(this.HardwareButton_Click);
			// 
			// AddDemoDevice
			// 
			this.AddDemoDevice.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.AddDemoDevice.Image = ((System.Drawing.Image)(resources.GetObject("AddDemoDevice.Image")));
			this.AddDemoDevice.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddDemoDevice.Name = "AddDemoDevice";
			this.AddDemoDevice.Size = new System.Drawing.Size(106, 22);
			this.AddDemoDevice.Text = "Add Demo Device";
			this.AddDemoDevice.Click += new System.EventHandler(this.AddDemoDevice_Click);
			// 
			// HiddenDevicesButton
			// 
			this.HiddenDevicesButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.HiddenDevicesButton.Image = ((System.Drawing.Image)(resources.GetObject("HiddenDevicesButton.Image")));
			this.HiddenDevicesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.HiddenDevicesButton.Name = "HiddenDevicesButton";
			this.HiddenDevicesButton.Size = new System.Drawing.Size(93, 22);
			this.HiddenDevicesButton.Text = "Hidden Devices";
			this.HiddenDevicesButton.Click += new System.EventHandler(this.HiddenDevicesButton_Click);
			// 
			// HideVirtualButton
			// 
			this.HideVirtualButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.HideVirtualButton.Image = ((System.Drawing.Image)(resources.GetObject("HideVirtualButton.Image")));
			this.HideVirtualButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.HideVirtualButton.Name = "HideVirtualButton";
			this.HideVirtualButton.Size = new System.Drawing.Size(73, 22);
			this.HideVirtualButton.Text = "Hide Virtual";
			this.HideVirtualButton.Click += new System.EventHandler(this.HideVirtualButton_Click);
			// 
			// UserDevicesUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.DevicesDataGridView);
			this.Controls.Add(this.ControllersToolStrip);
			this.Name = "UserDevicesUserControl";
			this.Size = new System.Drawing.Size(717, 436);
			this.Load += new System.EventHandler(this.ControllersUserControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).EndInit();
			this.ControllersToolStrip.ResumeLayout(false);
			this.ControllersToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.DataGridView DevicesDataGridView;
		private System.Windows.Forms.ToolStrip ControllersToolStrip;
		private System.Windows.Forms.ToolStripButton ControllerDeleteButton;
		private System.Windows.Forms.ToolStripButton RefreshButton;
		private System.Windows.Forms.ToolStripButton HardwareButton;
		private System.Windows.Forms.ToolStripButton AddDemoDevice;
        private System.Windows.Forms.DataGridViewImageColumn IsOnlineColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsEnabledColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MySidColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyDeviceColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MyGameColumn;
        private System.Windows.Forms.ToolStripButton HiddenDevicesButton;
		private System.Windows.Forms.ToolStripButton HideVirtualButton;
	}
}
