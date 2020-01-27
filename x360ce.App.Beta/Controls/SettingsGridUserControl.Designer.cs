namespace x360ce.App.Controls
{
	partial class SettingsGridUserControl
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
			this.CommentPanel = new System.Windows.Forms.Panel();
			this.CommentLabel = new System.Windows.Forms.Label();
			this.SettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.SettingsSidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsFileTitleColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsDeviceNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsMapToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SettingsToolStrip = new System.Windows.Forms.ToolStrip();
			this.SettingsRefreshButton = new System.Windows.Forms.ToolStripButton();
			this.SettingsDeleteButton = new System.Windows.Forms.ToolStripButton();
			this.SettingsEditNoteButton = new System.Windows.Forms.ToolStripButton();
			this.CommentPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).BeginInit();
			this.SettingsToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// CommentPanel
			// 
			this.CommentPanel.AutoSize = true;
			this.CommentPanel.BackColor = System.Drawing.SystemColors.Control;
			this.CommentPanel.Controls.Add(this.CommentLabel);
			this.CommentPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.CommentPanel.Location = new System.Drawing.Point(0, 221);
			this.CommentPanel.Name = "CommentPanel";
			this.CommentPanel.Size = new System.Drawing.Size(480, 19);
			this.CommentPanel.TabIndex = 9;
			// 
			// CommentLabel
			// 
			this.CommentLabel.AutoSize = true;
			this.CommentLabel.BackColor = System.Drawing.SystemColors.Control;
			this.CommentLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.CommentLabel.Location = new System.Drawing.Point(0, 0);
			this.CommentLabel.Name = "CommentLabel";
			this.CommentLabel.Padding = new System.Windows.Forms.Padding(3);
			this.CommentLabel.Size = new System.Drawing.Size(44, 19);
			this.CommentLabel.TabIndex = 5;
			this.CommentLabel.Text = "&Notes:";
			// 
			// SettingsDataGridView
			// 
			this.SettingsDataGridView.AllowUserToAddRows = false;
			this.SettingsDataGridView.AllowUserToDeleteRows = false;
			this.SettingsDataGridView.AllowUserToResizeRows = false;
			this.SettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.SettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.SettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.SettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SettingsSidColumn,
            this.SettingsFileNameColumn,
            this.SettingsFileTitleColumn,
            this.SettingsDeviceNameColumn,
            this.SettingsMapToColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.SettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.SettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.SettingsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.SettingsDataGridView.MultiSelect = false;
			this.SettingsDataGridView.Name = "SettingsDataGridView";
			this.SettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.SettingsDataGridView.RowHeadersVisible = false;
			this.SettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.SettingsDataGridView.Size = new System.Drawing.Size(480, 215);
			this.SettingsDataGridView.TabIndex = 8;
			this.SettingsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.SettingsDataGridView_CellFormatting);
			// 
			// SettingsSidColumn
			// 
			this.SettingsSidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsSidColumn.DataPropertyName = "PadSettingChecksum";
			this.SettingsSidColumn.HeaderText = "SID";
			this.SettingsSidColumn.Name = "SettingsSidColumn";
			this.SettingsSidColumn.ReadOnly = true;
			this.SettingsSidColumn.Width = 50;
			// 
			// SettingsFileNameColumn
			// 
			this.SettingsFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsFileNameColumn.DataPropertyName = "FileName";
			this.SettingsFileNameColumn.HeaderText = "File Name";
			this.SettingsFileNameColumn.Name = "SettingsFileNameColumn";
			this.SettingsFileNameColumn.ReadOnly = true;
			this.SettingsFileNameColumn.Width = 79;
			// 
			// SettingsFileTitleColumn
			// 
			this.SettingsFileTitleColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.SettingsFileTitleColumn.DataPropertyName = "FileProductName";
			this.SettingsFileTitleColumn.HeaderText = "File Title";
			this.SettingsFileTitleColumn.Name = "SettingsFileTitleColumn";
			this.SettingsFileTitleColumn.ReadOnly = true;
			// 
			// SettingsDeviceNameColumn
			// 
			this.SettingsDeviceNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsDeviceNameColumn.DataPropertyName = "ProductName";
			this.SettingsDeviceNameColumn.HeaderText = "Device Name";
			this.SettingsDeviceNameColumn.Name = "SettingsDeviceNameColumn";
			this.SettingsDeviceNameColumn.ReadOnly = true;
			this.SettingsDeviceNameColumn.Width = 97;
			// 
			// SettingsMapToColumn
			// 
			this.SettingsMapToColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingsMapToColumn.DataPropertyName = "MapTo";
			this.SettingsMapToColumn.HeaderText = "Map To";
			this.SettingsMapToColumn.Name = "SettingsMapToColumn";
			this.SettingsMapToColumn.ReadOnly = true;
			this.SettingsMapToColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.SettingsMapToColumn.Width = 69;
			// 
			// SettingsToolStrip
			// 
			this.SettingsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.SettingsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SettingsRefreshButton,
            this.SettingsDeleteButton,
            this.SettingsEditNoteButton});
			this.SettingsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.SettingsToolStrip.Name = "SettingsToolStrip";
			this.SettingsToolStrip.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
			this.SettingsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.SettingsToolStrip.Size = new System.Drawing.Size(480, 25);
			this.SettingsToolStrip.TabIndex = 7;
			this.SettingsToolStrip.Text = "MySettingsToolStrip";
			// 
			// SettingsRefreshButton
			// 
			this.SettingsRefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SettingsRefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.SettingsRefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsRefreshButton.Name = "SettingsRefreshButton";
			this.SettingsRefreshButton.Size = new System.Drawing.Size(66, 22);
			this.SettingsRefreshButton.Text = "&Refresh";
			this.SettingsRefreshButton.Click += new System.EventHandler(this.SettingsRefreshButton_Click);
			// 
			// SettingsDeleteButton
			// 
			this.SettingsDeleteButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.SettingsDeleteButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.SettingsDeleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsDeleteButton.Name = "SettingsDeleteButton";
			this.SettingsDeleteButton.Size = new System.Drawing.Size(60, 22);
			this.SettingsDeleteButton.Text = "&Delete";
			this.SettingsDeleteButton.Click += new System.EventHandler(this.SettingsDeleteButton_Click);
			// 
			// SettingsEditNoteButton
			// 
			this.SettingsEditNoteButton.Image = global::x360ce.App.Properties.Resources.edit_note_16x16;
			this.SettingsEditNoteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.SettingsEditNoteButton.Name = "SettingsEditNoteButton";
			this.SettingsEditNoteButton.Size = new System.Drawing.Size(76, 22);
			this.SettingsEditNoteButton.Text = "Edit Note";
			this.SettingsEditNoteButton.Click += new System.EventHandler(this.SettingsEditNoteButton_Click);
			// 
			// SettingsGridUserControl
			// 
			this.Controls.Add(this.CommentPanel);
			this.Controls.Add(this.SettingsDataGridView);
			this.Controls.Add(this.SettingsToolStrip);
			this.Name = "SettingsGridUserControl";
			this.Size = new System.Drawing.Size(480, 240);
			this.CommentPanel.ResumeLayout(false);
			this.CommentPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.SettingsDataGridView)).EndInit();
			this.SettingsToolStrip.ResumeLayout(false);
			this.SettingsToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel CommentPanel;
		private System.Windows.Forms.Label CommentLabel;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsSidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsFileTitleColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsDeviceNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingsMapToColumn;
		private System.Windows.Forms.ToolStrip SettingsToolStrip;
		private System.Windows.Forms.ToolStripButton SettingsRefreshButton;
		private System.Windows.Forms.ToolStripButton SettingsDeleteButton;
		private System.Windows.Forms.ToolStripButton SettingsEditNoteButton;
		public System.Windows.Forms.DataGridView SettingsDataGridView;
	}
}
