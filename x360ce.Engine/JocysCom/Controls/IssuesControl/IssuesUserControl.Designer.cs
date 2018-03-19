namespace JocysCom.ClassLibrary.Controls.IssueControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.IssuesDataGridView = new System.Windows.Forms.DataGridView();
            this.SeverityColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FixColumn = new System.Windows.Forms.DataGridViewButtonColumn();
            this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
            this.IgnoreAllButton = new System.Windows.Forms.ToolStripButton();
            this.StatusLabel = new System.Windows.Forms.ToolStripLabel();
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
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.IssuesDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
            this.IssuesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IssuesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.IssuesDataGridView.Location = new System.Drawing.Point(0, 25);
            this.IssuesDataGridView.Name = "IssuesDataGridView";
            this.IssuesDataGridView.ReadOnly = true;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.IssuesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.IssuesDataGridView.RowHeadersVisible = false;
            this.IssuesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.IssuesDataGridView.Size = new System.Drawing.Size(605, 233);
            this.IssuesDataGridView.TabIndex = 23;
            this.IssuesDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.IssuesDataGridView_CellContentClick);
            this.IssuesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.IssuesDataGridView_CellFormatting);
            // 
            // SeverityColumn
            // 
            this.SeverityColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SeverityColumn.DataPropertyName = "Severity";
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
            this.NameColumn.HeaderText = "Name";
            this.NameColumn.Name = "NameColumn";
            this.NameColumn.ReadOnly = true;
            this.NameColumn.Width = 60;
            // 
            // DescriptionColumn
            // 
            this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DescriptionColumn.DataPropertyName = "Description";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DescriptionColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.DescriptionColumn.HeaderText = "Description";
            this.DescriptionColumn.Name = "DescriptionColumn";
            this.DescriptionColumn.ReadOnly = true;
            // 
            // FixColumn
            // 
            this.FixColumn.DataPropertyName = "FixName";
            this.FixColumn.HeaderText = "Fix";
            this.FixColumn.Name = "FixColumn";
            this.FixColumn.ReadOnly = true;
            this.FixColumn.Text = "Fix";
            // 
            // GamesToolStrip
            // 
            this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IgnoreAllButton,
            this.StatusLabel});
            this.GamesToolStrip.Location = new System.Drawing.Point(0, 0);
            this.GamesToolStrip.Name = "GamesToolStrip";
            this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
            this.GamesToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.GamesToolStrip.Size = new System.Drawing.Size(605, 25);
            this.GamesToolStrip.TabIndex = 24;
            this.GamesToolStrip.Text = "MySettingsToolStrip";
            // 
            // IgnoreAllButton
            // 
            this.IgnoreAllButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.IgnoreAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.IgnoreAllButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
            this.IgnoreAllButton.Name = "IgnoreAllButton";
            this.IgnoreAllButton.Size = new System.Drawing.Size(62, 22);
            this.IgnoreAllButton.Text = "Ignore All";
            this.IgnoreAllButton.Click += new System.EventHandler(this.IgnoreButton_Click);
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(42, 22);
            this.StatusLabel.Text = "Status:";
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
		private System.Windows.Forms.DataGridViewImageColumn SeverityColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
		private System.Windows.Forms.DataGridViewButtonColumn FixColumn;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
		private System.Windows.Forms.ToolStripButton IgnoreAllButton;
        private System.Windows.Forms.ToolStripLabel StatusLabel;
    }
}
