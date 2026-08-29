namespace x360ce.App.Controls
{
	partial class OptionsSettingsUserControl
	{
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Laid out in a single column that grows with the control, because the paths it
		/// shows are as long as somebody's user name and the folders they chose.
		/// </summary>
		private void InitializeComponent()
		{
			this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.CurrentLabel = new System.Windows.Forms.Label();
			this.CurrentPanel = new System.Windows.Forms.Panel();
			this.CurrentPathTextBox = new System.Windows.Forms.TextBox();
			this.OpenFolderButton = new System.Windows.Forms.Button();
			this.KeepLabel = new System.Windows.Forms.Label();
			this.LocationComboBox = new System.Windows.Forms.ComboBox();
			this.PathLabel = new System.Windows.Forms.Label();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.MoveModeLabel = new System.Windows.Forms.Label();
			this.MoveModeComboBox = new System.Windows.Forms.ComboBox();
			this.ApplyButton = new System.Windows.Forms.Button();
			this.MainTableLayoutPanel.SuspendLayout();
			this.CurrentPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// MainTableLayoutPanel
			//
			this.MainTableLayoutPanel.ColumnCount = 1;
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.Controls.Add(this.CurrentLabel, 0, 0);
			this.MainTableLayoutPanel.Controls.Add(this.CurrentPanel, 0, 1);
			this.MainTableLayoutPanel.Controls.Add(this.KeepLabel, 0, 2);
			this.MainTableLayoutPanel.Controls.Add(this.LocationComboBox, 0, 3);
			this.MainTableLayoutPanel.Controls.Add(this.PathLabel, 0, 4);
			this.MainTableLayoutPanel.Controls.Add(this.StatusLabel, 0, 5);
			this.MainTableLayoutPanel.Controls.Add(this.MoveModeLabel, 0, 6);
			this.MainTableLayoutPanel.Controls.Add(this.MoveModeComboBox, 0, 7);
			this.MainTableLayoutPanel.Controls.Add(this.ApplyButton, 0, 8);
			this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
			this.MainTableLayoutPanel.Padding = new System.Windows.Forms.Padding(6);
			this.MainTableLayoutPanel.RowCount = 10;
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.Size = new System.Drawing.Size(560, 320);
			this.MainTableLayoutPanel.TabIndex = 0;
			//
			// CurrentLabel
			//
			this.CurrentLabel.AutoSize = true;
			this.CurrentLabel.Location = new System.Drawing.Point(9, 6);
			this.CurrentLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.CurrentLabel.Name = "CurrentLabel";
			this.CurrentLabel.Size = new System.Drawing.Size(105, 13);
			this.CurrentLabel.TabIndex = 0;
			this.CurrentLabel.Text = "Settings are kept in:";
			//
			// CurrentPanel
			//
			this.CurrentPanel.Controls.Add(this.CurrentPathTextBox);
			this.CurrentPanel.Controls.Add(this.OpenFolderButton);
			this.CurrentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CurrentPanel.Location = new System.Drawing.Point(9, 25);
			this.CurrentPanel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 12);
			this.CurrentPanel.Name = "CurrentPanel";
			this.CurrentPanel.Size = new System.Drawing.Size(542, 24);
			this.CurrentPanel.TabIndex = 1;
			//
			// CurrentPathTextBox
			//
			this.CurrentPathTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.CurrentPathTextBox.Location = new System.Drawing.Point(0, 0);
			this.CurrentPathTextBox.Name = "CurrentPathTextBox";
			this.CurrentPathTextBox.ReadOnly = true;
			this.CurrentPathTextBox.Size = new System.Drawing.Size(442, 20);
			this.CurrentPathTextBox.TabIndex = 0;
			//
			// OpenFolderButton
			//
			this.OpenFolderButton.AutoSize = true;
			this.OpenFolderButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OpenFolderButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.OpenFolderButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
			this.OpenFolderButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.OpenFolderButton.Location = new System.Drawing.Point(442, 0);
			this.OpenFolderButton.Name = "OpenFolderButton";
			this.OpenFolderButton.Padding = new System.Windows.Forms.Padding(4, 0, 6, 0);
			this.OpenFolderButton.Size = new System.Drawing.Size(100, 24);
			this.OpenFolderButton.TabIndex = 1;
			this.OpenFolderButton.Text = "&Open Folder";
			this.OpenFolderButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.OpenFolderButton.UseVisualStyleBackColor = true;
			this.OpenFolderButton.Click += new System.EventHandler(this.OpenFolderButton_Click);
			//
			// KeepLabel
			//
			this.KeepLabel.AutoSize = true;
			this.KeepLabel.Location = new System.Drawing.Point(9, 64);
			this.KeepLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.KeepLabel.Name = "KeepLabel";
			this.KeepLabel.Size = new System.Drawing.Size(93, 13);
			this.KeepLabel.TabIndex = 2;
			this.KeepLabel.Text = "Keep settings in:";
			//
			// LocationComboBox
			//
			this.LocationComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.LocationComboBox.AutoSize = true;
			this.LocationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.LocationComboBox.FormattingEnabled = true;
			this.LocationComboBox.Location = new System.Drawing.Point(9, 83);
			this.LocationComboBox.Name = "LocationComboBox";
			this.LocationComboBox.Size = new System.Drawing.Size(542, 21);
			this.LocationComboBox.TabIndex = 3;
			this.LocationComboBox.SelectedIndexChanged += new System.EventHandler(this.LocationComboBox_SelectedIndexChanged);
			//
			// PathLabel
			//
			this.PathLabel.AutoSize = true;
			this.PathLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.PathLabel.Location = new System.Drawing.Point(9, 110);
			this.PathLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
			this.PathLabel.Name = "PathLabel";
			this.PathLabel.Size = new System.Drawing.Size(0, 13);
			this.PathLabel.TabIndex = 4;
			//
			// StatusLabel
			//
			this.StatusLabel.AutoSize = true;
			this.StatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.StatusLabel.Location = new System.Drawing.Point(9, 129);
			this.StatusLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 12);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(0, 13);
			this.StatusLabel.TabIndex = 5;
			//
			// MoveModeLabel
			//
			this.MoveModeLabel.AutoSize = true;
			this.MoveModeLabel.Location = new System.Drawing.Point(9, 157);
			this.MoveModeLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
			this.MoveModeLabel.Name = "MoveModeLabel";
			this.MoveModeLabel.Size = new System.Drawing.Size(175, 13);
			this.MoveModeLabel.TabIndex = 6;
			this.MoveModeLabel.Text = "What to do with the settings you have:";
			//
			// MoveModeComboBox
			//
			this.MoveModeComboBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.MoveModeComboBox.AutoSize = true;
			this.MoveModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.MoveModeComboBox.FormattingEnabled = true;
			this.MoveModeComboBox.Location = new System.Drawing.Point(9, 176);
			this.MoveModeComboBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 12);
			this.MoveModeComboBox.Name = "MoveModeComboBox";
			this.MoveModeComboBox.Size = new System.Drawing.Size(542, 21);
			this.MoveModeComboBox.TabIndex = 7;
			//
			// ApplyButton
			//
			this.ApplyButton.AutoSize = true;
			this.ApplyButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ApplyButton.Image = global::x360ce.App.Properties.Resources.save_16x16;
			this.ApplyButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.ApplyButton.Location = new System.Drawing.Point(9, 212);
			this.ApplyButton.Name = "ApplyButton";
			this.ApplyButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.ApplyButton.Padding = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this.ApplyButton.Size = new System.Drawing.Size(133, 25);
			this.ApplyButton.TabIndex = 8;
			this.ApplyButton.Text = "&Keep settings here";
			this.ApplyButton.UseVisualStyleBackColor = true;
			this.ApplyButton.Click += new System.EventHandler(this.ApplyButton_Click);
			//
			// OptionsSettingsUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainTableLayoutPanel);
			this.Name = "OptionsSettingsUserControl";
			this.Size = new System.Drawing.Size(560, 320);
			this.MainTableLayoutPanel.ResumeLayout(false);
			this.MainTableLayoutPanel.PerformLayout();
			this.CurrentPanel.ResumeLayout(false);
			this.CurrentPanel.PerformLayout();
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
		private System.Windows.Forms.Label CurrentLabel;
		private System.Windows.Forms.Panel CurrentPanel;
		private System.Windows.Forms.TextBox CurrentPathTextBox;
		private System.Windows.Forms.Button OpenFolderButton;
		private System.Windows.Forms.Label KeepLabel;
		private System.Windows.Forms.ComboBox LocationComboBox;
		private System.Windows.Forms.Label PathLabel;
		private System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.Label MoveModeLabel;
		private System.Windows.Forms.ComboBox MoveModeComboBox;
		private System.Windows.Forms.Button ApplyButton;
	}
}
