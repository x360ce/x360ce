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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IssuesUserControl));
			this.WarningsDataGridView = new System.Windows.Forms.DataGridView();
			this.SeverityColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MoreColumn = new System.Windows.Forms.DataGridViewLinkColumn();
			this.SolutionColumn = new System.Windows.Forms.DataGridViewButtonColumn();
			this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
			this.IgnoreAllButton = new System.Windows.Forms.ToolStripButton();
			this.IgnoreButton = new System.Windows.Forms.ToolStripButton();
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
			((System.ComponentModel.ISupportInitialize)(this.WarningsDataGridView)).BeginInit();
			this.GamesToolStrip.SuspendLayout();
			this.NoIssuesPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// WarningsDataGridView
			// 
			this.WarningsDataGridView.AllowUserToAddRows = false;
			this.WarningsDataGridView.AllowUserToDeleteRows = false;
			this.WarningsDataGridView.AllowUserToOrderColumns = true;
			this.WarningsDataGridView.AllowUserToResizeRows = false;
			this.WarningsDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
			this.WarningsDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.WarningsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.WarningsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.WarningsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.WarningsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.WarningsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.WarningsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SeverityColumn,
            this.NameColumn,
            this.DescriptionColumn,
            this.MoreColumn,
            this.SolutionColumn});
			dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.WarningsDataGridView.DefaultCellStyle = dataGridViewCellStyle6;
			this.WarningsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WarningsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.WarningsDataGridView.Location = new System.Drawing.Point(0, 43);
			this.WarningsDataGridView.Margin = new System.Windows.Forms.Padding(0);
			this.WarningsDataGridView.Name = "WarningsDataGridView";
			this.WarningsDataGridView.ReadOnly = true;
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			this.WarningsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
			this.WarningsDataGridView.RowHeadersVisible = false;
			this.WarningsDataGridView.RowHeadersWidth = 51;
			this.WarningsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.WarningsDataGridView.Size = new System.Drawing.Size(1072, 597);
			this.WarningsDataGridView.TabIndex = 23;
			this.WarningsDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.WarningsDataGridView_CellContentClick);
			this.WarningsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.WarningsDataGridView_CellFormatting);
			this.WarningsDataGridView.SelectionChanged += new System.EventHandler(this.WarningsDataGridView_SelectionChanged);
			// 
			// SeverityColumn
			// 
			this.SeverityColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SeverityColumn.DataPropertyName = "Severity";
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle2.NullValue = null;
			dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(3);
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SeverityColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.SeverityColumn.HeaderText = "";
			this.SeverityColumn.MinimumWidth = 24;
			this.SeverityColumn.Name = "SeverityColumn";
			this.SeverityColumn.ReadOnly = true;
			this.SeverityColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.SeverityColumn.Width = 24;
			// 
			// NameColumn
			// 
			this.NameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.NameColumn.DataPropertyName = "Name";
			dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(3);
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.NameColumn.DefaultCellStyle = dataGridViewCellStyle3;
			this.NameColumn.HeaderText = "Name";
			this.NameColumn.MinimumWidth = 6;
			this.NameColumn.Name = "NameColumn";
			this.NameColumn.ReadOnly = true;
			this.NameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.NameColumn.Width = 77;
			// 
			// DescriptionColumn
			// 
			this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.DescriptionColumn.DataPropertyName = "Description";
			dataGridViewCellStyle4.Padding = new System.Windows.Forms.Padding(3);
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.DescriptionColumn.DefaultCellStyle = dataGridViewCellStyle4;
			this.DescriptionColumn.HeaderText = "Description";
			this.DescriptionColumn.MinimumWidth = 6;
			this.DescriptionColumn.Name = "DescriptionColumn";
			this.DescriptionColumn.ReadOnly = true;
			this.DescriptionColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// MoreColumn
			// 
			this.MoreColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.MoreColumn.DataPropertyName = "MoreInfo";
			this.MoreColumn.HeaderText = "More";
			this.MoreColumn.MinimumWidth = 10;
			this.MoreColumn.Name = "MoreColumn";
			this.MoreColumn.ReadOnly = true;
			this.MoreColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.MoreColumn.Text = "More...";
			this.MoreColumn.Width = 67;
			// 
			// SolutionColumn
			// 
			this.SolutionColumn.DataPropertyName = "FixName";
			dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(3);
			dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SolutionColumn.DefaultCellStyle = dataGridViewCellStyle5;
			this.SolutionColumn.HeaderText = "Solution";
			this.SolutionColumn.MinimumWidth = 100;
			this.SolutionColumn.Name = "SolutionColumn";
			this.SolutionColumn.ReadOnly = true;
			this.SolutionColumn.Text = "";
			// 
			// GamesToolStrip
			// 
			this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.GamesToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IgnoreAllButton,
            this.IgnoreButton,
            this.ExceptionInfoButton,
            this.StatusLabel,
            this.NextRunSeparator,
            this.NextRunLabel,
            this.RunStateLabel});
			this.GamesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.GamesToolStrip.Name = "GamesToolStrip";
			this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 1, 1, 0);
			this.GamesToolStrip.Size = new System.Drawing.Size(1072, 43);
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
			this.IgnoreAllButton.Size = new System.Drawing.Size(141, 39);
			this.IgnoreAllButton.Text = "Ignore All";
			this.IgnoreAllButton.Click += new System.EventHandler(this.IgnoreAllButton_Click);
			// 
			// IgnoreButton
			// 
			this.IgnoreButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.IgnoreButton.Image = ((System.Drawing.Image)(resources.GetObject("IgnoreButton.Image")));
			this.IgnoreButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.IgnoreButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.IgnoreButton.Name = "IgnoreButton";
			this.IgnoreButton.Size = new System.Drawing.Size(107, 39);
			this.IgnoreButton.Text = "Ignore";
			this.IgnoreButton.Click += new System.EventHandler(this.IgnoreButton_Click);
			// 
			// ExceptionInfoButton
			// 
			this.ExceptionInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("ExceptionInfoButton.Image")));
			this.ExceptionInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ExceptionInfoButton.Name = "ExceptionInfoButton";
			this.ExceptionInfoButton.Size = new System.Drawing.Size(190, 36);
			this.ExceptionInfoButton.Text = "Exception Info";
			this.ExceptionInfoButton.Click += new System.EventHandler(this.ExceptionInfoButton_Click);
			// 
			// StatusLabel
			// 
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(83, 36);
			this.StatusLabel.Text = "Status:";
			// 
			// NextRunSeparator
			// 
			this.NextRunSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.NextRunSeparator.Name = "NextRunSeparator";
			this.NextRunSeparator.Size = new System.Drawing.Size(6, 42);
			// 
			// NextRunLabel
			// 
			this.NextRunLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.NextRunLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.NextRunLabel.Name = "NextRunLabel";
			this.NextRunLabel.Size = new System.Drawing.Size(213, 36);
			this.NextRunLabel.Text = "Next Run: 00:00:00";
			// 
			// RunStateLabel
			// 
			this.RunStateLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RunStateLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.RunStateLabel.Name = "RunStateLabel";
			this.RunStateLabel.Size = new System.Drawing.Size(21, 36);
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
			this.NoIssuesPanel.Location = new System.Drawing.Point(0, 641);
			this.NoIssuesPanel.Margin = new System.Windows.Forms.Padding(0);
			this.NoIssuesPanel.Name = "NoIssuesPanel";
			this.NoIssuesPanel.Padding = new System.Windows.Forms.Padding(16);
			this.NoIssuesPanel.Size = new System.Drawing.Size(1072, 61);
			this.NoIssuesPanel.TabIndex = 25;
			// 
			// NoIssuesLabel
			// 
			this.NoIssuesLabel.AutoSize = true;
			this.NoIssuesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.NoIssuesLabel.ForeColor = System.Drawing.Color.Green;
			this.NoIssuesLabel.Location = new System.Drawing.Point(19, 16);
			this.NoIssuesLabel.Name = "NoIssuesLabel";
			this.NoIssuesLabel.Size = new System.Drawing.Size(361, 29);
			this.NoIssuesLabel.TabIndex = 0;
			this.NoIssuesLabel.Text = "All OK. No issues were found.";
			// 
			// LinePanel
			// 
			this.LinePanel.BackColor = System.Drawing.SystemColors.ControlDark;
			this.LinePanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.LinePanel.Location = new System.Drawing.Point(0, 640);
			this.LinePanel.Name = "LinePanel";
			this.LinePanel.Size = new System.Drawing.Size(1072, 1);
			this.LinePanel.TabIndex = 26;
			// 
			// IssuesUserControl
			// 
			this.Controls.Add(this.WarningsDataGridView);
			this.Controls.Add(this.LinePanel);
			this.Controls.Add(this.NoIssuesPanel);
			this.Controls.Add(this.GamesToolStrip);
			this.Name = "IssuesUserControl";
			this.Size = new System.Drawing.Size(1072, 702);
			((System.ComponentModel.ISupportInitialize)(this.WarningsDataGridView)).EndInit();
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.NoIssuesPanel.ResumeLayout(false);
			this.NoIssuesPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView WarningsDataGridView;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
		private System.Windows.Forms.ToolStripButton IgnoreAllButton;
        private System.Windows.Forms.ToolStripLabel StatusLabel;
        private System.Windows.Forms.ToolStripSeparator NextRunSeparator;
        private System.Windows.Forms.ToolStripLabel NextRunLabel;
        private System.Windows.Forms.Timer QueueMonitorTimer;
        private System.Windows.Forms.ToolStripLabel RunStateLabel;
        private System.Windows.Forms.ToolStripButton ExceptionInfoButton;
        private System.Windows.Forms.ImageList SeverityImageList;
        private System.Windows.Forms.Panel NoIssuesPanel;
        private System.Windows.Forms.Label NoIssuesLabel;
        private System.Windows.Forms.Panel LinePanel;
        private System.Windows.Forms.ToolStripButton IgnoreButton;
		private System.Windows.Forms.DataGridViewImageColumn SeverityColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
		private System.Windows.Forms.DataGridViewLinkColumn MoreColumn;
		private System.Windows.Forms.DataGridViewButtonColumn SolutionColumn;
	}
}
