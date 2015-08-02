namespace x360ce.App
{
	partial class WarningsForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WarningsForm));
			this.WarningsTabControl = new System.Windows.Forms.TabControl();
			this.WarningsTabPage = new System.Windows.Forms.TabPage();
			this.WarningsDataGridView = new System.Windows.Forms.DataGridView();
			this.Closebutton = new System.Windows.Forms.Button();
			this.DontShowCheckBox = new System.Windows.Forms.CheckBox();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.FixColumn = new System.Windows.Forms.DataGridViewButtonColumn();
			this.WarningsTabControl.SuspendLayout();
			this.WarningsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.WarningsDataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// WarningsTabControl
			// 
			this.WarningsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WarningsTabControl.Controls.Add(this.WarningsTabPage);
			this.WarningsTabControl.ItemSize = new System.Drawing.Size(116, 18);
			this.WarningsTabControl.Location = new System.Drawing.Point(12, 12);
			this.WarningsTabControl.Name = "WarningsTabControl";
			this.WarningsTabControl.SelectedIndex = 0;
			this.WarningsTabControl.Size = new System.Drawing.Size(600, 328);
			this.WarningsTabControl.TabIndex = 20;
			// 
			// WarningsTabPage
			// 
			this.WarningsTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.WarningsTabPage.Controls.Add(this.WarningsDataGridView);
			this.WarningsTabPage.Location = new System.Drawing.Point(4, 22);
			this.WarningsTabPage.Name = "WarningsTabPage";
			this.WarningsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.WarningsTabPage.Size = new System.Drawing.Size(592, 302);
			this.WarningsTabPage.TabIndex = 3;
			this.WarningsTabPage.Text = "Warnings List";
			// 
			// WarningsDataGridView
			// 
			this.WarningsDataGridView.AllowUserToAddRows = false;
			this.WarningsDataGridView.AllowUserToDeleteRows = false;
			this.WarningsDataGridView.AllowUserToOrderColumns = true;
			this.WarningsDataGridView.AllowUserToResizeRows = false;
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
            this.dataGridViewImageColumn1,
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
			this.WarningsDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
			this.WarningsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WarningsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.WarningsDataGridView.Location = new System.Drawing.Point(3, 3);
			this.WarningsDataGridView.Name = "WarningsDataGridView";
			this.WarningsDataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.WarningsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.WarningsDataGridView.RowHeadersVisible = false;
			this.WarningsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.WarningsDataGridView.Size = new System.Drawing.Size(586, 296);
			this.WarningsDataGridView.TabIndex = 8;
			this.WarningsDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.WarningsDataGridView_CellContentClick);
			// 
			// Closebutton
			// 
			this.Closebutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Closebutton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Closebutton.Location = new System.Drawing.Point(537, 346);
			this.Closebutton.Name = "Closebutton";
			this.Closebutton.Size = new System.Drawing.Size(75, 23);
			this.Closebutton.TabIndex = 21;
			this.Closebutton.Text = "Cancel";
			this.Closebutton.UseVisualStyleBackColor = true;
			this.Closebutton.Click += new System.EventHandler(this.Closebutton_Click);
			// 
			// DontShowCheckBox
			// 
			this.DontShowCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DontShowCheckBox.AutoSize = true;
			this.DontShowCheckBox.Location = new System.Drawing.Point(12, 350);
			this.DontShowCheckBox.Name = "DontShowCheckBox";
			this.DontShowCheckBox.Size = new System.Drawing.Size(122, 17);
			this.DontShowCheckBox.TabIndex = 22;
			this.DontShowCheckBox.Text = "Don\'t show anymore";
			this.DontShowCheckBox.UseVisualStyleBackColor = true;
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewImageColumn1.HeaderText = "";
			this.dataGridViewImageColumn1.Image = global::x360ce.App.Properties.Resources.MessageBoxIcon_Warning_32x32;
			this.dataGridViewImageColumn1.MinimumWidth = 32;
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			this.dataGridViewImageColumn1.ReadOnly = true;
			this.dataGridViewImageColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridViewImageColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.dataGridViewImageColumn1.Width = 32;
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
			// WarningsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Closebutton;
			this.ClientSize = new System.Drawing.Size(624, 381);
			this.Controls.Add(this.DontShowCheckBox);
			this.Controls.Add(this.Closebutton);
			this.Controls.Add(this.WarningsTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "WarningsForm";
			this.Text = "Warnings Form";
			this.WarningsTabControl.ResumeLayout(false);
			this.WarningsTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.WarningsDataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabControl WarningsTabControl;
		private System.Windows.Forms.TabPage WarningsTabPage;
		private System.Windows.Forms.DataGridView WarningsDataGridView;
		private System.Windows.Forms.Button Closebutton;
		private System.Windows.Forms.CheckBox DontShowCheckBox;
		private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
		private System.Windows.Forms.DataGridViewButtonColumn FixColumn;
	}
}