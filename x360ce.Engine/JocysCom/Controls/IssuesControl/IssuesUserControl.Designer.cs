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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IssuesUserControl));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            this.IssuesDataGridView = new System.Windows.Forms.DataGridView();
            this.SeverityColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SolutionColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
            this.IgnoreAllButton = new System.Windows.Forms.ToolStripButton();
            this.ExceptionInfoButton = new System.Windows.Forms.ToolStripButton();
            this.StatusLabel = new System.Windows.Forms.ToolStripLabel();
            this.NextRunSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.NextRunLabel = new System.Windows.Forms.ToolStripLabel();
            this.RunStateLabel = new System.Windows.Forms.ToolStripLabel();
            this.QueueMonitorTimer = new System.Windows.Forms.Timer(this.components);
            this.SeverityImageList = new System.Windows.Forms.ImageList(this.components);
            this.NoIssuesPanel = new System.Windows.Forms.Panel();
            this.NoIssuesLabel = new System.Windows.Forms.Label();
            this.LinePanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.IssuesDataGridView)).BeginInit();
            this.GamesToolStrip.SuspendLayout();
            this.NoIssuesPanel.SuspendLayout();
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
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.IssuesDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle8;
            this.IssuesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.IssuesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SeverityColumn,
            this.NameColumn,
            this.DescriptionColumn,
            this.SolutionColumn});
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle13.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.IssuesDataGridView.DefaultCellStyle = dataGridViewCellStyle13;
            this.IssuesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IssuesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.IssuesDataGridView.Location = new System.Drawing.Point(0, 25);
            this.IssuesDataGridView.Margin = new System.Windows.Forms.Padding(0);
            this.IssuesDataGridView.Name = "IssuesDataGridView";
            this.IssuesDataGridView.ReadOnly = true;
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle14.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.IssuesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle14;
            this.IssuesDataGridView.RowHeadersVisible = false;
            this.IssuesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.IssuesDataGridView.Size = new System.Drawing.Size(605, 185);
            this.IssuesDataGridView.TabIndex = 23;
            this.IssuesDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.IssuesDataGridView_CellContentClick);
            this.IssuesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.IssuesDataGridView_CellFormatting);
            // 
            // SeverityColumn
            // 
            this.SeverityColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeverityColumn.DataPropertyName = "Severity";
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle9.NullValue = ((object)(resources.GetObject("dataGridViewCellStyle9.NullValue")));
            dataGridViewCellStyle9.Padding = new System.Windows.Forms.Padding(3);
            this.SeverityColumn.DefaultCellStyle = dataGridViewCellStyle9;
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
            dataGridViewCellStyle10.Padding = new System.Windows.Forms.Padding(3);
            this.NameColumn.DefaultCellStyle = dataGridViewCellStyle10;
            this.NameColumn.HeaderText = "Name";
            this.NameColumn.Name = "NameColumn";
            this.NameColumn.ReadOnly = true;
            this.NameColumn.Width = 60;
            // 
            // DescriptionColumn
            // 
            this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DescriptionColumn.DataPropertyName = "Description";
            dataGridViewCellStyle11.Padding = new System.Windows.Forms.Padding(3);
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DescriptionColumn.DefaultCellStyle = dataGridViewCellStyle11;
            this.DescriptionColumn.HeaderText = "Description";
            this.DescriptionColumn.Name = "DescriptionColumn";
            this.DescriptionColumn.ReadOnly = true;
            // 
            // SolutionColumn
            // 
            this.SolutionColumn.DataPropertyName = "FixName";
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle12.Padding = new System.Windows.Forms.Padding(3);
            this.SolutionColumn.DefaultCellStyle = dataGridViewCellStyle12;
            this.SolutionColumn.HeaderText = "Solution";
            this.SolutionColumn.Name = "SolutionColumn";
            this.SolutionColumn.ReadOnly = true;
            this.SolutionColumn.Text = "";
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
            // ExceptionInfoButton
            // 
            this.ExceptionInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("ExceptionInfoButton.Image")));
            this.ExceptionInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ExceptionInfoButton.Name = "ExceptionInfoButton";
            this.ExceptionInfoButton.Size = new System.Drawing.Size(102, 21);
            this.ExceptionInfoButton.Text = "Exception Info";
            this.ExceptionInfoButton.Click += new System.EventHandler(this.ExceptionInfoButton_Click);
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
            // SeverityImageList
            // 
            this.SeverityImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("SeverityImageList.ImageStream")));
            this.SeverityImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.SeverityImageList.Images.SetKeyName(0, "Error");
            this.SeverityImageList.Images.SetKeyName(1, "Information");
            this.SeverityImageList.Images.SetKeyName(2, "Warning");
            // 
            // NoIssuesPanel
            // 
            this.NoIssuesPanel.AutoSize = true;
            this.NoIssuesPanel.Controls.Add(this.NoIssuesLabel);
            this.NoIssuesPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.NoIssuesPanel.Location = new System.Drawing.Point(0, 211);
            this.NoIssuesPanel.Margin = new System.Windows.Forms.Padding(0);
            this.NoIssuesPanel.Name = "NoIssuesPanel";
            this.NoIssuesPanel.Padding = new System.Windows.Forms.Padding(16);
            this.NoIssuesPanel.Size = new System.Drawing.Size(605, 47);
            this.NoIssuesPanel.TabIndex = 25;
            // 
            // NoIssuesLabel
            // 
            this.NoIssuesLabel.AutoSize = true;
            this.NoIssuesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NoIssuesLabel.ForeColor = System.Drawing.Color.Green;
            this.NoIssuesLabel.Location = new System.Drawing.Point(19, 16);
            this.NoIssuesLabel.Name = "NoIssuesLabel";
            this.NoIssuesLabel.Size = new System.Drawing.Size(196, 15);
            this.NoIssuesLabel.TabIndex = 0;
            this.NoIssuesLabel.Text = "All OK. No issues were found.";
            // 
            // LinePanel
            // 
            this.LinePanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.LinePanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.LinePanel.Location = new System.Drawing.Point(0, 210);
            this.LinePanel.Name = "LinePanel";
            this.LinePanel.Size = new System.Drawing.Size(605, 1);
            this.LinePanel.TabIndex = 26;
            // 
            // IssuesUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.IssuesDataGridView);
            this.Controls.Add(this.LinePanel);
            this.Controls.Add(this.NoIssuesPanel);
            this.Controls.Add(this.GamesToolStrip);
            this.Name = "IssuesUserControl";
            this.Size = new System.Drawing.Size(605, 258);
            ((System.ComponentModel.ISupportInitialize)(this.IssuesDataGridView)).EndInit();
            this.GamesToolStrip.ResumeLayout(false);
            this.GamesToolStrip.PerformLayout();
            this.NoIssuesPanel.ResumeLayout(false);
            this.NoIssuesPanel.PerformLayout();
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
        private System.Windows.Forms.DataGridViewButtonColumn SolutionColumn;
        private System.Windows.Forms.Panel NoIssuesPanel;
        private System.Windows.Forms.Label NoIssuesLabel;
        private System.Windows.Forms.Panel LinePanel;
    }
}
