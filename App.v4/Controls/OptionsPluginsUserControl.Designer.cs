namespace x360ce.App.Controls
{
	partial class OptionsPluginsUserControl
	{
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.AllowThirdPartyPluginsCheckBox = new System.Windows.Forms.CheckBox();
			this.NoteLabel = new System.Windows.Forms.Label();
			this.FolderLabel = new System.Windows.Forms.Label();
			this.FolderTextBox = new System.Windows.Forms.TextBox();
			this.OpenFolderButton = new System.Windows.Forms.Button();
			this.PluginsDataGridView = new System.Windows.Forms.DataGridView();
			this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.StateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DevicesColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SignerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.SourceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ReasonColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MainTableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PluginsDataGridView)).BeginInit();
			this.SuspendLayout();
			//
			// MainTableLayoutPanel
			//
			this.MainTableLayoutPanel.ColumnCount = 3;
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainTableLayoutPanel.Controls.Add(this.AllowThirdPartyPluginsCheckBox, 0, 0);
			this.MainTableLayoutPanel.Controls.Add(this.NoteLabel, 0, 1);
			this.MainTableLayoutPanel.Controls.Add(this.FolderLabel, 0, 2);
			this.MainTableLayoutPanel.Controls.Add(this.FolderTextBox, 1, 2);
			this.MainTableLayoutPanel.Controls.Add(this.OpenFolderButton, 2, 2);
			this.MainTableLayoutPanel.Controls.Add(this.PluginsDataGridView, 0, 3);
			this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
			this.MainTableLayoutPanel.Padding = new System.Windows.Forms.Padding(6);
			this.MainTableLayoutPanel.RowCount = 4;
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.Size = new System.Drawing.Size(644, 410);
			this.MainTableLayoutPanel.TabIndex = 0;
			//
			// AllowThirdPartyPluginsCheckBox
			//
			this.AllowThirdPartyPluginsCheckBox.AutoSize = true;
			this.MainTableLayoutPanel.SetColumnSpan(this.AllowThirdPartyPluginsCheckBox, 3);
			this.AllowThirdPartyPluginsCheckBox.Location = new System.Drawing.Point(9, 9);
			this.AllowThirdPartyPluginsCheckBox.Name = "AllowThirdPartyPluginsCheckBox";
			this.AllowThirdPartyPluginsCheckBox.Size = new System.Drawing.Size(140, 17);
			this.AllowThirdPartyPluginsCheckBox.TabIndex = 0;
			this.AllowThirdPartyPluginsCheckBox.Text = "Allow third-party plugins";
			this.AllowThirdPartyPluginsCheckBox.UseVisualStyleBackColor = true;
			//
			// NoteLabel
			//
			this.NoteLabel.AutoSize = true;
			this.MainTableLayoutPanel.SetColumnSpan(this.NoteLabel, 3);
			this.NoteLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.NoteLabel.Location = new System.Drawing.Point(26, 29);
			this.NoteLabel.Margin = new System.Windows.Forms.Padding(20, 0, 3, 3);
			this.NoteLabel.Name = "NoteLabel";
			this.NoteLabel.Size = new System.Drawing.Size(600, 26);
			this.NoteLabel.TabIndex = 1;
			this.NoteLabel.Text = "Plugins add devices. Those inside x360ce, and those signed by its publisher, always load. A plugin runs with all the rights of x360ce, so allow plugins from others only from people you trust.";
			//
			// FolderLabel
			//
			this.FolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.FolderLabel.AutoSize = true;
			this.FolderLabel.Location = new System.Drawing.Point(9, 70);
			this.FolderLabel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
			this.FolderLabel.Name = "FolderLabel";
			this.FolderLabel.Size = new System.Drawing.Size(36, 13);
			this.FolderLabel.TabIndex = 2;
			this.FolderLabel.Text = "Folder";
			//
			// FolderTextBox
			//
			this.FolderTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.FolderTextBox.Location = new System.Drawing.Point(51, 66);
			this.FolderTextBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
			this.FolderTextBox.Name = "FolderTextBox";
			this.FolderTextBox.ReadOnly = true;
			this.FolderTextBox.Size = new System.Drawing.Size(495, 20);
			this.FolderTextBox.TabIndex = 3;
			//
			// OpenFolderButton
			//
			this.OpenFolderButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.OpenFolderButton.AutoSize = true;
			this.OpenFolderButton.Location = new System.Drawing.Point(552, 64);
			this.OpenFolderButton.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
			this.OpenFolderButton.Name = "OpenFolderButton";
			this.OpenFolderButton.Size = new System.Drawing.Size(80, 23);
			this.OpenFolderButton.TabIndex = 4;
			this.OpenFolderButton.Text = "Open Folder";
			this.OpenFolderButton.UseVisualStyleBackColor = true;
			this.OpenFolderButton.Click += new System.EventHandler(this.OpenFolderButton_Click);
			//
			// PluginsDataGridView
			//
			this.PluginsDataGridView.AllowUserToAddRows = false;
			this.PluginsDataGridView.AllowUserToDeleteRows = false;
			this.PluginsDataGridView.AllowUserToResizeRows = false;
			this.PluginsDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.PluginsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.PluginsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PluginsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.NameColumn,
			this.StateColumn,
			this.DevicesColumn,
			this.SignerColumn,
			this.SourceColumn,
			this.ReasonColumn});
			this.MainTableLayoutPanel.SetColumnSpan(this.PluginsDataGridView, 3);
			this.PluginsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PluginsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.PluginsDataGridView.Location = new System.Drawing.Point(9, 96);
			this.PluginsDataGridView.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.PluginsDataGridView.Name = "PluginsDataGridView";
			this.PluginsDataGridView.ReadOnly = true;
			this.PluginsDataGridView.RowHeadersVisible = false;
			this.PluginsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.PluginsDataGridView.Size = new System.Drawing.Size(626, 305);
			this.PluginsDataGridView.TabIndex = 5;
			//
			// NameColumn
			//
			this.NameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.NameColumn.DataPropertyName = "Name";
			this.NameColumn.HeaderText = "Plugin";
			this.NameColumn.Name = "NameColumn";
			this.NameColumn.ReadOnly = true;
			//
			// StateColumn
			//
			this.StateColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.StateColumn.DataPropertyName = "State";
			this.StateColumn.HeaderText = "State";
			this.StateColumn.Name = "StateColumn";
			this.StateColumn.ReadOnly = true;
			//
			// DevicesColumn
			//
			this.DevicesColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DevicesColumn.DataPropertyName = "Devices";
			this.DevicesColumn.HeaderText = "Devices";
			this.DevicesColumn.Name = "DevicesColumn";
			this.DevicesColumn.ReadOnly = true;
			//
			// SignerColumn
			//
			this.SignerColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SignerColumn.DataPropertyName = "Signer";
			this.SignerColumn.HeaderText = "Signed By";
			this.SignerColumn.Name = "SignerColumn";
			this.SignerColumn.ReadOnly = true;
			//
			// SourceColumn
			//
			this.SourceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SourceColumn.DataPropertyName = "Source";
			this.SourceColumn.HeaderText = "File";
			this.SourceColumn.Name = "SourceColumn";
			this.SourceColumn.ReadOnly = true;
			//
			// ReasonColumn
			//
			this.ReasonColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ReasonColumn.DataPropertyName = "Reason";
			this.ReasonColumn.HeaderText = "Why";
			this.ReasonColumn.Name = "ReasonColumn";
			this.ReasonColumn.ReadOnly = true;
			//
			// OptionsPluginsUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainTableLayoutPanel);
			this.Name = "OptionsPluginsUserControl";
			this.Size = new System.Drawing.Size(644, 410);
			this.MainTableLayoutPanel.ResumeLayout(false);
			this.MainTableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PluginsDataGridView)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
		private System.Windows.Forms.CheckBox AllowThirdPartyPluginsCheckBox;
		private System.Windows.Forms.Label NoteLabel;
		private System.Windows.Forms.Label FolderLabel;
		private System.Windows.Forms.TextBox FolderTextBox;
		private System.Windows.Forms.Button OpenFolderButton;
		private System.Windows.Forms.DataGridView PluginsDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn StateColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DevicesColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SignerColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SourceColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ReasonColumn;
	}
}
