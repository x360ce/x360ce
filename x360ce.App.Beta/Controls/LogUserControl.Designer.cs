namespace x360ce.App.Controls
{
	partial class LogUserControl : System.Windows.Forms.UserControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.DebugTabControl = new System.Windows.Forms.TabControl();
            this.LogTabPage = new System.Windows.Forms.TabPage();
            this.LogDataGridView = new System.Windows.Forms.DataGridView();
            this.LogSizeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.LogSizeLabel = new System.Windows.Forms.Label();
            this.LogToolStrip = new System.Windows.Forms.ToolStrip();
            this.ClearLogButton = new System.Windows.Forms.ToolStripButton();
            this.DateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DelayColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DebugTabControl.SuspendLayout();
            this.LogTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LogSizeNumericUpDown)).BeginInit();
            this.LogToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // DebugTabControl
            // 
            this.DebugTabControl.Controls.Add(this.LogTabPage);
            this.DebugTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DebugTabControl.Location = new System.Drawing.Point(0, 0);
            this.DebugTabControl.Multiline = true;
            this.DebugTabControl.Name = "DebugTabControl";
            this.DebugTabControl.SelectedIndex = 0;
            this.DebugTabControl.Size = new System.Drawing.Size(744, 305);
            this.DebugTabControl.TabIndex = 11;
            // 
            // LogTabPage
            // 
            this.LogTabPage.Controls.Add(this.LogDataGridView);
            this.LogTabPage.Controls.Add(this.LogSizeNumericUpDown);
            this.LogTabPage.Controls.Add(this.LogSizeLabel);
            this.LogTabPage.Controls.Add(this.LogToolStrip);
            this.LogTabPage.Location = new System.Drawing.Point(4, 22);
            this.LogTabPage.Margin = new System.Windows.Forms.Padding(0);
            this.LogTabPage.Name = "LogTabPage";
            this.LogTabPage.Size = new System.Drawing.Size(736, 279);
            this.LogTabPage.TabIndex = 0;
            this.LogTabPage.Text = "Log";
            this.LogTabPage.UseVisualStyleBackColor = true;
            // 
            // LogDataGridView
            // 
            this.LogDataGridView.AllowUserToAddRows = false;
            this.LogDataGridView.AllowUserToDeleteRows = false;
            this.LogDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this.LogDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.LogDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LogDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.LogDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.LogDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.LogDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DateColumn,
            this.DelayColumn,
            this.StatusColumn,
            this.DataColumn});
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.LogDataGridView.DefaultCellStyle = dataGridViewCellStyle5;
            this.LogDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogDataGridView.Location = new System.Drawing.Point(0, 28);
            this.LogDataGridView.Name = "LogDataGridView";
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.LogDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this.LogDataGridView.RowHeadersVisible = false;
            this.LogDataGridView.Size = new System.Drawing.Size(736, 251);
            this.LogDataGridView.TabIndex = 4;
            this.LogDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.LogDataGridView_CellDoubleClick);
            this.LogDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.LogDataGridView_CellFormatting);
            this.LogDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.LogDataGridView_RowsAdded);
            this.LogDataGridView.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.LogDataGridView_RowsRemoved);
            // 
            // LogSizeNumericUpDown
            // 
            this.LogSizeNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LogSizeNumericUpDown.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.LogSizeNumericUpDown.Location = new System.Drawing.Point(536, 4);
            this.LogSizeNumericUpDown.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.LogSizeNumericUpDown.Name = "LogSizeNumericUpDown";
            this.LogSizeNumericUpDown.Size = new System.Drawing.Size(48, 20);
            this.LogSizeNumericUpDown.TabIndex = 10;
            this.LogSizeNumericUpDown.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.LogSizeNumericUpDown.ValueChanged += new System.EventHandler(this.LogSizeNumericUpDown_ValueChanged);
            // 
            // LogSizeLabel
            // 
            this.LogSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LogSizeLabel.AutoSize = true;
            this.LogSizeLabel.BackColor = System.Drawing.SystemColors.Control;
            this.LogSizeLabel.Location = new System.Drawing.Point(479, 6);
            this.LogSizeLabel.Name = "LogSizeLabel";
            this.LogSizeLabel.Size = new System.Drawing.Size(51, 13);
            this.LogSizeLabel.TabIndex = 7;
            this.LogSizeLabel.Text = "Log Size:";
            // 
            // LogToolStrip
            // 
            this.LogToolStrip.AutoSize = false;
            this.LogToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.LogToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearLogButton});
            this.LogToolStrip.Location = new System.Drawing.Point(0, 0);
            this.LogToolStrip.Name = "LogToolStrip";
            this.LogToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.LogToolStrip.Size = new System.Drawing.Size(736, 28);
            this.LogToolStrip.TabIndex = 8;
            this.LogToolStrip.Text = "toolStrip2";
            // 
            // ClearLogButton
            // 
            this.ClearLogButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ClearLogButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
            this.ClearLogButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ClearLogButton.Name = "ClearLogButton";
            this.ClearLogButton.Size = new System.Drawing.Size(54, 25);
            this.ClearLogButton.Text = "Clear";
            this.ClearLogButton.ToolTipText = "Delete Message from Send Queue";
            this.ClearLogButton.Click += new System.EventHandler(this.ClearLogButton_Click);
            // 
            // DateColumn
            // 
            this.DateColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DateColumn.DataPropertyName = "Date";
            dataGridViewCellStyle2.Format = "HH:mm:ss.fff";
            this.DateColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.DateColumn.HeaderText = "Date";
            this.DateColumn.Name = "DateColumn";
            this.DateColumn.ReadOnly = true;
            this.DateColumn.Width = 55;
            // 
            // DelayColumn
            // 
            this.DelayColumn.DataPropertyName = "Delay";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.DelayColumn.DefaultCellStyle = dataGridViewCellStyle3;
            this.DelayColumn.HeaderText = "Delay";
            this.DelayColumn.Name = "DelayColumn";
            this.DelayColumn.Visible = false;
            this.DelayColumn.Width = 48;
            // 
            // StatusColumn
            // 
            this.StatusColumn.DataPropertyName = "Status";
            this.StatusColumn.HeaderText = "Status";
            this.StatusColumn.Name = "StatusColumn";
            this.StatusColumn.ReadOnly = true;
            this.StatusColumn.Visible = false;
            this.StatusColumn.Width = 72;
            // 
            // DataColumn
            // 
            this.DataColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DataColumn.DataPropertyName = "Message";
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DataColumn.DefaultCellStyle = dataGridViewCellStyle4;
            this.DataColumn.HeaderText = "Message";
            this.DataColumn.Name = "DataColumn";
            this.DataColumn.ReadOnly = true;
            // 
            // LogUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DebugTabControl);
            this.DoubleBuffered = true;
            this.Name = "LogUserControl";
            this.Size = new System.Drawing.Size(744, 305);
            this.Load += new System.EventHandler(this.DebugForm_Load);
            this.DebugTabControl.ResumeLayout(false);
            this.LogTabPage.ResumeLayout(false);
            this.LogTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LogDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LogSizeNumericUpDown)).EndInit();
            this.LogToolStrip.ResumeLayout(false);
            this.LogToolStrip.PerformLayout();
            this.ResumeLayout(false);

		}
        internal System.Windows.Forms.DataGridView LogDataGridView;
		internal System.Windows.Forms.NumericUpDown LogSizeNumericUpDown;
        internal System.Windows.Forms.Label LogSizeLabel;
        internal System.Windows.Forms.TabControl DebugTabControl;
		internal System.Windows.Forms.TabPage LogTabPage;

		#endregion
        private System.Windows.Forms.ToolStrip LogToolStrip;
        private System.Windows.Forms.ToolStripButton ClearLogButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn DateColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DelayColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DataColumn;
    }

}
