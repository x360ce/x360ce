namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	partial class IssuesUserControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IssuesUserControl));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            this.IssuesDataGridView = new System.Windows.Forms.DataGridView();
            this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
            this.IgnoreAllButton = new System.Windows.Forms.ToolStripButton();
            this.StatusLabel = new System.Windows.Forms.ToolStripLabel();
            this.NextRunSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.NextRunLabel = new System.Windows.Forms.ToolStripLabel();
            this.RunStateLabel = new System.Windows.Forms.ToolStripLabel();
            this.QueueMonitorTimer = new System.Windows.Forms.Timer(this.components);
            this.ExceptionInfoButton = new System.Windows.Forms.ToolStripButton();
            this.SeverityImageList = new System.Windows.Forms.ImageList(this.components);
            this.SeverityColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FixColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)(this.IssuesDataGridView)).BeginInit();
            this.GamesToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // IssuesDataGridView
            // 
            this.IssuesDataGridView.AllowUserToAddRows = false;
            this.IssuesDataGridView.AllowUserToDeleteRows = false;
            this.IssuesDataGridView.AllowUserToOrderColumns = true;
            this.IssuesDataGridView.AllowUserToResizeRows = false;
            this.IssuesDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.IssuesDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.IssuesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.IssuesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.IssuesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.IssuesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.IssuesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SeverityColumn,
            this.NameColumn,
            this.DescriptionColumn,
            this.FixColumn});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.IssuesDataGridView.DefaultCellStyle = dataGridViewCellStyle6;
            this.IssuesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IssuesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.IssuesDataGridView.Location = new System.Drawing.Point(0, 25);
            this.IssuesDataGridView.Name = "IssuesDataGridView";
            this.IssuesDataGridView.ReadOnly = true;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.IssuesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.IssuesDataGridView.RowHeadersVisible = false;
            this.IssuesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.IssuesDataGridView.Size = new System.Drawing.Size(605, 233);
            this.IssuesDataGridView.TabIndex = 23;
            this.IssuesDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.IssuesDataGridView_CellContentClick);
            this.IssuesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.IssuesDataGridView_CellFormatting);
            // 
            // GamesToolStrip
            // 
            this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IgnoreAllButton,
            this.ExceptionInfoButton,
            this.StatusLabel,
            this.NextRunSeparator,
            this.NextRunLabel,
            this.RunStateLabel});
            this.GamesToolStrip.Location = new System.Drawing.Point(0, 0);
            this.GamesToolStrip.Name = "GamesToolStrip";
            this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 1, 1, 0);
            this.GamesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.GamesToolStrip.Size = new System.Drawing.Size(605, 25);
            this.GamesToolStrip.TabIndex = 24;
            this.GamesToolStrip.Text = "MySettingsToolStrip";
            // 
            // IgnoreAllButton
            // 
            this.IgnoreAllButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.IgnoreAllButton.Image = ((System.Drawing.Image)(resources.GetObject("IgnoreAllButton.Image")));
            this.IgnoreAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.IgnoreAllButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
            this.IgnoreAllButton.Name = "IgnoreAllButton";
            this.IgnoreAllButton.Size = new System.Drawing.Size(78, 21);
            this.IgnoreAllButton.Text = "Ignore All";
            this.IgnoreAllButton.Click += new System.EventHandler(this.IgnoreAllButton_Click);
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(42, 21);
            this.StatusLabel.Text = "Status:";
            // 
            // NextRunSeparator
            // 
            this.NextRunSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NextRunSeparator.Name = "NextRunSeparator";
            this.NextRunSeparator.Size = new System.Drawing.Size(6, 24);
            // 
            // NextRunLabel
            // 
            this.NextRunLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NextRunLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.NextRunLabel.Name = "NextRunLabel";
            this.NextRunLabel.Size = new System.Drawing.Size(103, 21);
            this.NextRunLabel.Text = "Next Run: 00:00:00";
            // 
            // RunStateLabel
            // 
            this.RunStateLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.RunStateLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.RunStateLabel.Name = "RunStateLabel";
            this.RunStateLabel.Size = new System.Drawing.Size(10, 21);
            this.RunStateLabel.Text = " ";
            // 
            // QueueMonitorTimer
            // 
            this.QueueMonitorTimer.Interval = 500;
            this.QueueMonitorTimer.Tick += new System.EventHandler(this.QueueMonitorTimer_Tick);
            // 
            // ExceptionInfoButton
            // 
            this.ExceptionInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("ExceptionInfoButton.Image")));
            this.ExceptionInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ExceptionInfoButton.Name = "ExceptionInfoButton";
            this.ExceptionInfoButton.Size = new System.Drawing.Size(102, 21);
            this.ExceptionInfoButton.Text = "Exception Info";
            this.ExceptionInfoButton.Click += new System.EventHandler(this.ExceptionInfoButton_Click);
            // 
            // SeverityImageList
            // 
            this.SeverityImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("SeverityImageList.ImageStream")));
            this.SeverityImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.SeverityImageList.Images.SetKeyName(0, "Error");
            this.SeverityImageList.Images.SetKeyName(1, "Information");
            this.SeverityImageList.Images.SetKeyName(2, "Warning");
            // 
            // SeverityColumn
            // 
            this.SeverityColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeverityColumn.DataPropertyName = "Severity";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.NullValue = ((object)(resources.GetObject("dataGridViewCellStyle2.NullValue")));
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(3);
            this.SeverityColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.SeverityColumn.HeaderText = "";
            this.SeverityColumn.MinimumWidth = 32;
            this.SeverityColumn.Name = "SeverityColumn";
            this.SeverityColumn.ReadOnly = true;
            this.SeverityColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.SeverityColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.SeverityColumn.Width = 32;
            // 
            // NameColumn
            // 
            this.NameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.NameColumn.DataPropertyName = "Name";
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(3);
            this.NameColumn.DefaultCellStyle = dataGridViewCellStyle3;
            this.NameColumn.HeaderText = "Name";
            this.NameColumn.Name = "NameColumn";
            this.NameColumn.ReadOnly = true;
            this.NameColumn.Width = 60;
            // 
            // DescriptionColumn
            // 
            this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DescriptionColumn.DataPropertyName = "Description";
            dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(3);
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DescriptionColumn.DefaultCellStyle = dataGridViewCellStyle4;
            this.DescriptionColumn.HeaderText = "Description";
            this.DescriptionColumn.Name = "DescriptionColumn";
            this.DescriptionColumn.ReadOnly = true;
            // 
            // FixColumn
            // 
            this.FixColumn.DataPropertyName = "FixName";
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(3);
            this.FixColumn.DefaultCellStyle = dataGridViewCellStyle5;
            this.FixColumn.HeaderText = "Solution";
            this.FixColumn.Name = "FixColumn";
            this.FixColumn.ReadOnly = true;
            this.FixColumn.Text = "";
            // 
            // IssuesUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.IssuesDataGridView);
            this.Controls.Add(this.GamesToolStrip);
            this.Name = "IssuesUserControl";
            this.Size = new System.Drawing.Size(605, 258);
            ((System.ComponentModel.ISupportInitialize)(this.IssuesDataGridView)).EndInit();
            this.GamesToolStrip.ResumeLayout(false);
            this.GamesToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView IssuesDataGridView;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
		private System.Windows.Forms.ToolStripButton IgnoreAllButton;
        private System.Windows.Forms.ToolStripLabel StatusLabel;
        private System.Windows.Forms.ToolStripSeparator NextRunSeparator;
        private System.Windows.Forms.ToolStripLabel NextRunLabel;
        private System.Windows.Forms.Timer QueueMonitorTimer;
        private System.Windows.Forms.ToolStripLabel RunStateLabel;
        private System.Windows.Forms.ToolStripButton ExceptionInfoButton;
        private System.Windows.Forms.ImageList SeverityImageList;
        private System.Windows.Forms.DataGridViewImageColumn SeverityColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
        private System.Windows.Forms.DataGridViewButtonColumn FixColumn;
    }
}
