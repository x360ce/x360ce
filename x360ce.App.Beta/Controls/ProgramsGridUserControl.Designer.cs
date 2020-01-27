namespace x360ce.App.Controls
{
	partial class ProgramsGridUserControl
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
			this.ProgramsDataGridView = new System.Windows.Forms.DataGridView();
			this.ProgramImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.ProgramIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramFileNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProgramsToolStrip = new System.Windows.Forms.ToolStrip();
			this.GameDefaultDetailsControl = new x360ce.App.Controls.GameDetailsUserControl();
			this.ImportOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.ExportSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.RefreshProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.ImportProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.ExportProgramsButton = new System.Windows.Forms.ToolStripButton();
			this.DeleteProgramsButton = new System.Windows.Forms.ToolStripButton();
			((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).BeginInit();
			this.ProgramsToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// ProgramsDataGridView
			// 
			this.ProgramsDataGridView.AllowUserToAddRows = false;
			this.ProgramsDataGridView.AllowUserToDeleteRows = false;
			this.ProgramsDataGridView.AllowUserToOrderColumns = true;
			this.ProgramsDataGridView.AllowUserToResizeRows = false;
			this.ProgramsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.ProgramsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ProgramsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.ProgramsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ProgramsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ProgramImageColumn,
            this.ProgramIdColumn,
            this.ProgramFileNameColumn,
            this.ProgramProductNameColumn});
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ProgramsDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
			this.ProgramsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProgramsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.ProgramsDataGridView.Location = new System.Drawing.Point(0, 25);
			this.ProgramsDataGridView.Name = "ProgramsDataGridView";
			this.ProgramsDataGridView.ReadOnly = true;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
			this.ProgramsDataGridView.RowHeadersVisible = false;
			this.ProgramsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ProgramsDataGridView.Size = new System.Drawing.Size(857, 321);
			this.ProgramsDataGridView.TabIndex = 5;
			this.ProgramsDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ProgramsDataGridView_KeyDown);
			// 
			// ProgramImageColumn
			// 
			this.ProgramImageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ProgramImageColumn.HeaderText = "";
			this.ProgramImageColumn.MinimumWidth = 24;
			this.ProgramImageColumn.Name = "ProgramImageColumn";
			this.ProgramImageColumn.ReadOnly = true;
			this.ProgramImageColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.ProgramImageColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.ProgramImageColumn.Visible = false;
			this.ProgramImageColumn.Width = 24;
			// 
			// ProgramIdColumn
			// 
			this.ProgramIdColumn.DataPropertyName = "GameId";
			this.ProgramIdColumn.HeaderText = "ID";
			this.ProgramIdColumn.Name = "ProgramIdColumn";
			this.ProgramIdColumn.ReadOnly = true;
			this.ProgramIdColumn.Visible = false;
			// 
			// ProgramFileNameColumn
			// 
			this.ProgramFileNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProgramFileNameColumn.DataPropertyName = "FileName";
			this.ProgramFileNameColumn.FillWeight = 30F;
			this.ProgramFileNameColumn.HeaderText = "File Name";
			this.ProgramFileNameColumn.Name = "ProgramFileNameColumn";
			this.ProgramFileNameColumn.ReadOnly = true;
			// 
			// ProgramProductNameColumn
			// 
			this.ProgramProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProgramProductNameColumn.DataPropertyName = "FileProductName";
			this.ProgramProductNameColumn.FillWeight = 70F;
			this.ProgramProductNameColumn.HeaderText = "Product Name";
			this.ProgramProductNameColumn.Name = "ProgramProductNameColumn";
			this.ProgramProductNameColumn.ReadOnly = true;
			// 
			// ProgramsToolStrip
			// 
			this.ProgramsToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.ProgramsToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RefreshProgramsButton,
            this.ImportProgramsButton,
            this.ExportProgramsButton,
            this.DeleteProgramsButton});
			this.ProgramsToolStrip.Location = new System.Drawing.Point(0, 0);
			this.ProgramsToolStrip.Name = "ProgramsToolStrip";
			this.ProgramsToolStrip.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
			this.ProgramsToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.ProgramsToolStrip.Size = new System.Drawing.Size(857, 25);
			this.ProgramsToolStrip.TabIndex = 6;
			this.ProgramsToolStrip.Text = "MySettingsToolStrip";
			// 
			// GameDefaultDetailsControl
			// 
			this.GameDefaultDetailsControl.AutoSize = true;
			this.GameDefaultDetailsControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.GameDefaultDetailsControl.BackColor = System.Drawing.SystemColors.Control;
			this.GameDefaultDetailsControl.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.GameDefaultDetailsControl.Enabled = false;
			this.GameDefaultDetailsControl.Location = new System.Drawing.Point(0, 346);
			this.GameDefaultDetailsControl.Name = "GameDefaultDetailsControl";
			this.GameDefaultDetailsControl.Size = new System.Drawing.Size(857, 226);
			this.GameDefaultDetailsControl.TabIndex = 7;
			this.GameDefaultDetailsControl.Load += new System.EventHandler(this.GameDefaultDetailsControl_Load);
			// 
			// RefreshProgramsButton
			// 
			this.RefreshProgramsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshProgramsButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RefreshProgramsButton.Name = "RefreshProgramsButton";
			this.RefreshProgramsButton.Size = new System.Drawing.Size(66, 22);
			this.RefreshProgramsButton.Text = "&Refresh";
			this.RefreshProgramsButton.Click += new System.EventHandler(this.RefreshProgramsButton_Click);
			// 
			// ImportProgramsButton
			// 
			this.ImportProgramsButton.Image = global::x360ce.App.Properties.Resources.data_into_16x16;
			this.ImportProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ImportProgramsButton.Name = "ImportProgramsButton";
			this.ImportProgramsButton.Size = new System.Drawing.Size(72, 22);
			this.ImportProgramsButton.Text = "&Import...";
			this.ImportProgramsButton.Click += new System.EventHandler(this.ImportProgramsButton_Click);
			// 
			// ExportProgramsButton
			// 
			this.ExportProgramsButton.Image = global::x360ce.App.Properties.Resources.data_out_16x16;
			this.ExportProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.ExportProgramsButton.Name = "ExportProgramsButton";
			this.ExportProgramsButton.Size = new System.Drawing.Size(69, 22);
			this.ExportProgramsButton.Text = "&Export...";
			this.ExportProgramsButton.Click += new System.EventHandler(this.ExportProgramsButton_Click);
			// 
			// DeleteProgramsButton
			// 
			this.DeleteProgramsButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.DeleteProgramsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.DeleteProgramsButton.Name = "DeleteProgramsButton";
			this.DeleteProgramsButton.Size = new System.Drawing.Size(60, 22);
			this.DeleteProgramsButton.Text = "&Delete";
			this.DeleteProgramsButton.Click += new System.EventHandler(this.DeleteProgramsButton_Click);
			// 
			// ProgramsGridUserControl
			// 
			this.Controls.Add(this.ProgramsDataGridView);
			this.Controls.Add(this.GameDefaultDetailsControl);
			this.Controls.Add(this.ProgramsToolStrip);
			this.Name = "ProgramsGridUserControl";
			this.Size = new System.Drawing.Size(857, 572);
			((System.ComponentModel.ISupportInitialize)(this.ProgramsDataGridView)).EndInit();
			this.ProgramsToolStrip.ResumeLayout(false);
			this.ProgramsToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView ProgramsDataGridView;
		private System.Windows.Forms.DataGridViewImageColumn ProgramImageColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramFileNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProgramProductNameColumn;
		private System.Windows.Forms.ToolStrip ProgramsToolStrip;
		private System.Windows.Forms.ToolStripButton RefreshProgramsButton;
		private System.Windows.Forms.ToolStripButton ImportProgramsButton;
		private System.Windows.Forms.ToolStripButton ExportProgramsButton;
		private System.Windows.Forms.ToolStripButton DeleteProgramsButton;
		private GameDetailsUserControl GameDefaultDetailsControl;
		private System.Windows.Forms.OpenFileDialog ImportOpenFileDialog;
		private System.Windows.Forms.SaveFileDialog ExportSaveFileDialog;
	}
}
