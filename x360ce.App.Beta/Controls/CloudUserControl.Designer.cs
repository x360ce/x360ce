namespace x360ce.App.Controls
{
	partial class CloudUserControl
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.TasksDataGridView = new System.Windows.Forms.DataGridView();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.DateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ActionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.StateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.panel3 = new System.Windows.Forms.Panel();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.UploadToCloudButton = new System.Windows.Forms.ToolStripButton();
			this.DownloadFromCloudButton = new System.Windows.Forms.ToolStripButton();
			this.NextRunSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.NextRunLabel = new System.Windows.Forms.ToolStripLabel();
			this.QueueMonitorTimer = new System.Windows.Forms.Timer(this.components);
			this.DeleteButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.TasksDataGridView)).BeginInit();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// TasksDataGridView
			// 
			this.TasksDataGridView.AllowUserToAddRows = false;
			this.TasksDataGridView.AllowUserToDeleteRows = false;
			this.TasksDataGridView.AllowUserToOrderColumns = true;
			this.TasksDataGridView.AllowUserToResizeRows = false;
			this.TasksDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.TasksDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.TasksDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.TasksDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.TasksDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.TasksDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewImageColumn1,
            this.DateColumn,
            this.ActionColumn,
            this.DescriptionColumn,
            this.StateColumn});
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.TasksDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
			this.TasksDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TasksDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.TasksDataGridView.Location = new System.Drawing.Point(0, 25);
			this.TasksDataGridView.Name = "TasksDataGridView";
			this.TasksDataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.TasksDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.TasksDataGridView.RowHeadersVisible = false;
			this.TasksDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.TasksDataGridView.Size = new System.Drawing.Size(654, 338);
			this.TasksDataGridView.TabIndex = 7;
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewImageColumn1.HeaderText = "";
			this.dataGridViewImageColumn1.MinimumWidth = 24;
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			this.dataGridViewImageColumn1.ReadOnly = true;
			this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn1.Visible = false;
			// 
			// DateColumn
			// 
			this.DateColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DateColumn.DataPropertyName = "Date";
			dataGridViewCellStyle2.Format = "yyyy-MM-dd HH:mm:ss";
			this.DateColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.DateColumn.HeaderText = "Date";
			this.DateColumn.Name = "DateColumn";
			this.DateColumn.ReadOnly = true;
			this.DateColumn.Width = 55;
			// 
			// ActionColumn
			// 
			this.ActionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ActionColumn.DataPropertyName = "Action";
			this.ActionColumn.HeaderText = "Action";
			this.ActionColumn.Name = "ActionColumn";
			this.ActionColumn.ReadOnly = true;
			this.ActionColumn.Width = 62;
			// 
			// DescriptionColumn
			// 
			this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.DescriptionColumn.DataPropertyName = "Description";
			this.DescriptionColumn.HeaderText = "Description";
			this.DescriptionColumn.Name = "DescriptionColumn";
			this.DescriptionColumn.ReadOnly = true;
			// 
			// StateColumn
			// 
			this.StateColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.StateColumn.DataPropertyName = "State";
			this.StateColumn.HeaderText = "State";
			this.StateColumn.Name = "StateColumn";
			this.StateColumn.ReadOnly = true;
			this.StateColumn.Width = 57;
			// 
			// panel3
			// 
			this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel3.Location = new System.Drawing.Point(0, 363);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(654, 100);
			this.panel3.TabIndex = 9;
			// 
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.UploadToCloudButton,
            this.DownloadFromCloudButton,
            this.NextRunSeparator,
            this.NextRunLabel,
            this.DeleteButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(654, 25);
			this.toolStrip1.TabIndex = 8;
			this.toolStrip1.Text = "MySettingsToolStrip";
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripButton1.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(66, 22);
			this.toolStripButton1.Text = "&Refresh";
			// 
			// UploadToCloudButton
			// 
			this.UploadToCloudButton.Image = global::x360ce.App.Properties.Resources.cloud_computing_upload_16x16;
			this.UploadToCloudButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.UploadToCloudButton.Name = "UploadToCloudButton";
			this.UploadToCloudButton.Size = new System.Drawing.Size(116, 22);
			this.UploadToCloudButton.Text = "Upload To Cloud";
			this.UploadToCloudButton.ToolTipText = "Upload To Cloud";
			this.UploadToCloudButton.Click += new System.EventHandler(this.UploadToCloudButton_Click);
			// 
			// DownloadFromCloudButton
			// 
			this.DownloadFromCloudButton.Image = global::x360ce.App.Properties.Resources.cloud_computing_download_16x16;
			this.DownloadFromCloudButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DownloadFromCloudButton.Name = "DownloadFromCloudButton";
			this.DownloadFromCloudButton.Size = new System.Drawing.Size(147, 22);
			this.DownloadFromCloudButton.Text = "Download From Cloud";
			this.DownloadFromCloudButton.Click += new System.EventHandler(this.DownloadFromCloudButton_Click);
			// 
			// NextRunSeparator
			// 
			this.NextRunSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.NextRunSeparator.Name = "NextRunSeparator";
			this.NextRunSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// NextRunLabel
			// 
			this.NextRunLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.NextRunLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.NextRunLabel.Name = "NextRunLabel";
			this.NextRunLabel.Size = new System.Drawing.Size(103, 22);
			this.NextRunLabel.Text = "Next Run: 00:00:00";
			// 
			// QueueMonitorTimer
			// 
			this.QueueMonitorTimer.Interval = 500;
			this.QueueMonitorTimer.Tick += new System.EventHandler(this.QueueMonitorTimer_Tick);
			// 
			// DeleteButton
			// 
			this.DeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DeleteButton.Name = "DeleteButton";
			this.DeleteButton.Size = new System.Drawing.Size(60, 22);
			this.DeleteButton.Text = "&Delete";
			this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
			// 
			// CloudUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.TasksDataGridView);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.toolStrip1);
			this.Name = "CloudUserControl";
			this.Size = new System.Drawing.Size(654, 463);
			((System.ComponentModel.ISupportInitialize)(this.TasksDataGridView)).EndInit();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView TasksDataGridView;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton UploadToCloudButton;
        private System.Windows.Forms.ToolStripButton DownloadFromCloudButton;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn DateColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ActionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn StateColumn;
		private System.Windows.Forms.ToolStripSeparator NextRunSeparator;
		private System.Windows.Forms.ToolStripLabel NextRunLabel;
		private System.Windows.Forms.Timer QueueMonitorTimer;
		private System.Windows.Forms.ToolStripButton DeleteButton;
	}
}
