namespace x360ce.App.Controls
{
	partial class OptionsUpdateUserControl
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
			this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.CheckForUpdatesCheckBox = new System.Windows.Forms.CheckBox();
			this.PrivacyLabel = new System.Windows.Forms.Label();
			this.CheckButton = new System.Windows.Forms.Button();
			this.CheckDigitalSignatureCheckBox = new System.Windows.Forms.CheckBox();
			this.CheckVersionCheckBox = new System.Windows.Forms.CheckBox();
			this.LogTextBox = new System.Windows.Forms.TextBox();
			this.LayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// LayoutPanel
			//
			this.LayoutPanel.ColumnCount = 3;
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.LayoutPanel.Controls.Add(this.CheckForUpdatesCheckBox, 0, 0);
			this.LayoutPanel.Controls.Add(this.PrivacyLabel, 0, 1);
			this.LayoutPanel.Controls.Add(this.CheckButton, 0, 2);
			this.LayoutPanel.Controls.Add(this.CheckDigitalSignatureCheckBox, 1, 2);
			this.LayoutPanel.Controls.Add(this.CheckVersionCheckBox, 2, 2);
			this.LayoutPanel.Controls.Add(this.LogTextBox, 0, 3);
			this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.LayoutPanel.Name = "LayoutPanel";
			this.LayoutPanel.Padding = new System.Windows.Forms.Padding(6);
			this.LayoutPanel.RowCount = 4;
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.LayoutPanel.Size = new System.Drawing.Size(644, 410);
			this.LayoutPanel.TabIndex = 0;
			//
			// CheckForUpdatesCheckBox
			//
			this.CheckForUpdatesCheckBox.AutoSize = true;
			this.LayoutPanel.SetColumnSpan(this.CheckForUpdatesCheckBox, 3);
			this.CheckForUpdatesCheckBox.Location = new System.Drawing.Point(9, 9);
			this.CheckForUpdatesCheckBox.Name = "CheckForUpdatesCheckBox";
			this.CheckForUpdatesCheckBox.Size = new System.Drawing.Size(163, 17);
			this.CheckForUpdatesCheckBox.TabIndex = 0;
			this.CheckForUpdatesCheckBox.Text = "Check for updates on startup";
			this.CheckForUpdatesCheckBox.UseVisualStyleBackColor = true;
			//
			// PrivacyLabel
			//
			this.PrivacyLabel.AutoSize = true;
			this.LayoutPanel.SetColumnSpan(this.PrivacyLabel, 3);
			this.PrivacyLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.PrivacyLabel.Location = new System.Drawing.Point(26, 29);
			this.PrivacyLabel.Margin = new System.Windows.Forms.Padding(20, 0, 3, 3);
			this.PrivacyLabel.Name = "PrivacyLabel";
			this.PrivacyLabel.Size = new System.Drawing.Size(360, 13);
			this.PrivacyLabel.TabIndex = 1;
			this.PrivacyLabel.Text = "Once a day, at a random moment in the first hour. One request to api.github.com carrying only this program\'s version.";
			//
			// CheckButton
			//
			this.CheckButton.Location = new System.Drawing.Point(9, 52);
			this.CheckButton.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
			this.CheckButton.Name = "CheckButton";
			this.CheckButton.Size = new System.Drawing.Size(90, 23);
			this.CheckButton.TabIndex = 2;
			this.CheckButton.Text = "Check now";
			this.CheckButton.UseVisualStyleBackColor = true;
			this.CheckButton.Click += new System.EventHandler(this.CheckButton_Click);
			//
			// CheckDigitalSignatureCheckBox
			//
			this.CheckDigitalSignatureCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CheckDigitalSignatureCheckBox.AutoSize = true;
			this.CheckDigitalSignatureCheckBox.Checked = true;
			this.CheckDigitalSignatureCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.CheckDigitalSignatureCheckBox.Location = new System.Drawing.Point(108, 57);
			this.CheckDigitalSignatureCheckBox.Margin = new System.Windows.Forms.Padding(6, 7, 3, 3);
			this.CheckDigitalSignatureCheckBox.Name = "CheckDigitalSignatureCheckBox";
			this.CheckDigitalSignatureCheckBox.Size = new System.Drawing.Size(137, 17);
			this.CheckDigitalSignatureCheckBox.TabIndex = 3;
			this.CheckDigitalSignatureCheckBox.Text = "Check Digital Signature";
			this.CheckDigitalSignatureCheckBox.UseVisualStyleBackColor = true;
			//
			// CheckVersionCheckBox
			//
			this.CheckVersionCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CheckVersionCheckBox.AutoSize = true;
			this.CheckVersionCheckBox.Checked = true;
			this.CheckVersionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.CheckVersionCheckBox.Location = new System.Drawing.Point(251, 57);
			this.CheckVersionCheckBox.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
			this.CheckVersionCheckBox.Name = "CheckVersionCheckBox";
			this.CheckVersionCheckBox.Size = new System.Drawing.Size(95, 17);
			this.CheckVersionCheckBox.TabIndex = 4;
			this.CheckVersionCheckBox.Text = "Check Version";
			this.CheckVersionCheckBox.UseVisualStyleBackColor = true;
			//
			// LogTextBox
			//
			this.LayoutPanel.SetColumnSpan(this.LogTextBox, 3);
			this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LogTextBox.Location = new System.Drawing.Point(9, 84);
			this.LogTextBox.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.LogTextBox.Multiline = true;
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.ReadOnly = true;
			this.LogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.LogTextBox.Size = new System.Drawing.Size(626, 317);
			this.LogTextBox.TabIndex = 5;
			this.LogTextBox.WordWrap = false;
			//
			// OptionsUpdateUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.LayoutPanel);
			this.Name = "OptionsUpdateUserControl";
			this.Size = new System.Drawing.Size(644, 410);
			this.LayoutPanel.ResumeLayout(false);
			this.LayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel LayoutPanel;
		public System.Windows.Forms.CheckBox CheckForUpdatesCheckBox;
		private System.Windows.Forms.Label PrivacyLabel;
		private System.Windows.Forms.Button CheckButton;
		private System.Windows.Forms.CheckBox CheckDigitalSignatureCheckBox;
		private System.Windows.Forms.CheckBox CheckVersionCheckBox;
		private System.Windows.Forms.TextBox LogTextBox;
	}
}
