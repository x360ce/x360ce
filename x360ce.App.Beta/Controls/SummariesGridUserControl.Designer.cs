namespace x360ce.App.Controls
{
	partial class SummariesGridUserControl
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.SummariesDataGridView = new System.Windows.Forms.DataGridView();
			this.SummariesSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesUsersColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesFileTitleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SummariesToolStrip = new System.Windows.Forms.ToolStrip();
			this.SummariesRefreshButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).BeginInit();
			this.SummariesToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// SummariesDataGridView
			// 
			this.SummariesDataGridView.AllowUserToAddRows = false;
			this.SummariesDataGridView.AllowUserToDeleteRows = false;
			this.SummariesDataGridView.AllowUserToResizeRows = false;
			this.SummariesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.SummariesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SummariesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SummariesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.SummariesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SummariesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SummariesSidColumn,
            this.SummariesUsersColumn,
            this.SummariesFileNameColumn,
            this.SummariesFileTitleColumn,
            this.SummariesDeviceNameColumn});
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SummariesDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
			this.SummariesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SummariesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.SummariesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.SummariesDataGridView.MultiSelect = false;
			this.SummariesDataGridView.Name = "SummariesDataGridView";
			this.SummariesDataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SummariesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.SummariesDataGridView.RowHeadersVisible = false;
			this.SummariesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.SummariesDataGridView.Size = new System.Drawing.Size(480, 215);
			this.SummariesDataGridView.TabIndex = 3;
			this.SummariesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SummariesDataGridView_CellFormatting);
			// 
			// SummariesSidColumn
			// 
			this.SummariesSidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesSidColumn.DataPropertyName = "PadSettingChecksum";
			this.SummariesSidColumn.HeaderText = "SID";
			this.SummariesSidColumn.Name = "SummariesSidColumn";
			this.SummariesSidColumn.ReadOnly = true;
			this.SummariesSidColumn.Width = 50;
			// 
			// SummariesUsersColumn
			// 
			this.SummariesUsersColumn.DataPropertyName = "Users";
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.SummariesUsersColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.SummariesUsersColumn.HeaderText = "Users";
			this.SummariesUsersColumn.Name = "SummariesUsersColumn";
			this.SummariesUsersColumn.ReadOnly = true;
			this.SummariesUsersColumn.Width = 42;
			// 
			// SummariesFileNameColumn
			// 
			this.SummariesFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesFileNameColumn.DataPropertyName = "FileName";
			this.SummariesFileNameColumn.HeaderText = "File Name";
			this.SummariesFileNameColumn.Name = "SummariesFileNameColumn";
			this.SummariesFileNameColumn.ReadOnly = true;
			this.SummariesFileNameColumn.Width = 79;
			// 
			// SummariesFileTitleColumn
			// 
			this.SummariesFileTitleColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.SummariesFileTitleColumn.DataPropertyName = "FileProductName";
			this.SummariesFileTitleColumn.HeaderText = "File Title";
			this.SummariesFileTitleColumn.Name = "SummariesFileTitleColumn";
			this.SummariesFileTitleColumn.ReadOnly = true;
			// 
			// SummariesDeviceNameColumn
			// 
			this.SummariesDeviceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SummariesDeviceNameColumn.DataPropertyName = "ProductName";
			this.SummariesDeviceNameColumn.HeaderText = "Device Name";
			this.SummariesDeviceNameColumn.Name = "SummariesDeviceNameColumn";
			this.SummariesDeviceNameColumn.ReadOnly = true;
			this.SummariesDeviceNameColumn.Width = 97;
			// 
			// SummariesToolStrip
			// 
			this.SummariesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.SummariesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SummariesRefreshButton});
			this.SummariesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.SummariesToolStrip.Name = "SummariesToolStrip";
			this.SummariesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.SummariesToolStrip.Size = new System.Drawing.Size(480, 25);
			this.SummariesToolStrip.TabIndex = 4;
			this.SummariesToolStrip.Text = "MySettingsToolStrip";
			// 
			// SummariesRefreshButton
			// 
			this.SummariesRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SummariesRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.SummariesRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SummariesRefreshButton.Name = "SummariesRefreshButton";
			this.SummariesRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.SummariesRefreshButton.Text = "&Refresh";
			this.SummariesRefreshButton.Click += new System.EventHandler(this.SummariesRefreshButton_Click);
			// 
			// SummariesGridUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.SummariesDataGridView);
			this.Controls.Add(this.SummariesToolStrip);
			this.Name = "SummariesGridUserControl";
			this.Size = new System.Drawing.Size(480, 240);
			((System.ComponentModel.ISupportInitialize)(this.SummariesDataGridView)).EndInit();
			this.SummariesToolStrip.ResumeLayout(false);
			this.SummariesToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.DataGridView SummariesDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesUsersColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesFileTitleColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SummariesDeviceNameColumn;
		private System.Windows.Forms.ToolStrip SummariesToolStrip;
		private System.Windows.Forms.ToolStripButton SummariesRefreshButton;
	}
}
