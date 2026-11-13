namespace x360ce.App.Controls
{
	partial class PromptForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.MainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.EditTextBox = new System.Windows.Forms.TextBox();
			this.CopyButton = new System.Windows.Forms.Button();
			this.SizeLabel = new System.Windows.Forms.Label();
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.MainLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// MainLayoutPanel
			//
			this.MainLayoutPanel.ColumnCount = 4;
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.Controls.Add(this.EditTextBox, 0, 0);
			this.MainLayoutPanel.Controls.Add(this.CopyButton, 0, 1);
			this.MainLayoutPanel.Controls.Add(this.SizeLabel, 1, 1);
			this.MainLayoutPanel.Controls.Add(this.OkButton, 2, 1);
			this.MainLayoutPanel.Controls.Add(this.CloseButton, 3, 1);
			this.MainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.MainLayoutPanel.Name = "MainLayoutPanel";
			this.MainLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
			this.MainLayoutPanel.RowCount = 2;
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.Size = new System.Drawing.Size(464, 264);
			this.MainLayoutPanel.TabIndex = 0;
			//
			// EditTextBox
			//
			this.EditTextBox.AcceptsReturn = true;
			this.MainLayoutPanel.SetColumnSpan(this.EditTextBox, 4);
			this.EditTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.EditTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.EditTextBox.Multiline = true;
			this.EditTextBox.Name = "EditTextBox";
			this.EditTextBox.Size = new System.Drawing.Size(440, 208);
			this.EditTextBox.TabIndex = 0;
			this.EditTextBox.TextChanged += new System.EventHandler(this.EditTextBox_TextChanged);
			this.EditTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EditTextBox_KeyUp);
			//
			// CopyButton
			//
			this.CopyButton.AutoSize = true;
			this.CopyButton.Margin = new System.Windows.Forms.Padding(4);
			this.CopyButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.CopyButton.Name = "CopyButton";
			this.CopyButton.Size = new System.Drawing.Size(76, 24);
			this.CopyButton.TabIndex = 1;
			this.CopyButton.Text = "Copy";
			this.CopyButton.UseVisualStyleBackColor = true;
			this.CopyButton.Click += new System.EventHandler(this.CopyButton_Click);
			//
			// SizeLabel
			//
			this.SizeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SizeLabel.AutoSize = true;
			this.SizeLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.SizeLabel.Margin = new System.Windows.Forms.Padding(4);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Size = new System.Drawing.Size(53, 13);
			this.SizeLabel.TabIndex = 2;
			this.SizeLabel.Text = "SizeLabel";
			//
			// OkButton
			//
			this.OkButton.AutoSize = true;
			this.OkButton.Margin = new System.Windows.Forms.Padding(4);
			this.OkButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(76, 24);
			this.OkButton.TabIndex = 3;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			//
			// CloseButton
			//
			this.CloseButton.AutoSize = true;
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Margin = new System.Windows.Forms.Padding(4);
			this.CloseButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(76, 24);
			this.CloseButton.TabIndex = 4;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			//
			// PromptForm
			//
			this.AcceptButton = this.OkButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CloseButton;
			this.ClientSize = new System.Drawing.Size(464, 264);
			this.Controls.Add(this.MainLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.Name = "PromptForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Prompt Form";
			this.Load += new System.EventHandler(this.PromptForm_Load);
			this.MainLayoutPanel.ResumeLayout(false);
			this.MainLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
		public System.Windows.Forms.TextBox EditTextBox;
		private System.Windows.Forms.Button CopyButton;
		private System.Windows.Forms.Label SizeLabel;
	}
}
