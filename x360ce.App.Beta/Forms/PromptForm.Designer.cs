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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PromptForm));
			this.EditTextBox = new System.Windows.Forms.TextBox();
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.CopyButton = new System.Windows.Forms.Button();
			this.SizeLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// EditTextBox
			// 
			this.EditTextBox.AcceptsReturn = true;
			this.EditTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.EditTextBox.Location = new System.Drawing.Point(12, 12);
			this.EditTextBox.Multiline = true;
			this.EditTextBox.Name = "EditTextBox";
			this.EditTextBox.Size = new System.Drawing.Size(440, 208);
			this.EditTextBox.TabIndex = 0;
			this.EditTextBox.TextChanged += new System.EventHandler(this.EditTextBox_TextChanged);
			this.EditTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.EditTextBox_KeyUp);
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(296, 226);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(75, 23);
			this.OkButton.TabIndex = 1;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(377, 226);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 2;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			// 
			// CopyButton
			// 
			this.CopyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CopyButton.Location = new System.Drawing.Point(12, 226);
			this.CopyButton.Name = "CopyButton";
			this.CopyButton.Size = new System.Drawing.Size(75, 23);
			this.CopyButton.TabIndex = 1;
			this.CopyButton.Text = "Copy";
			this.CopyButton.UseVisualStyleBackColor = true;
			this.CopyButton.Click += new System.EventHandler(this.CopyButton_Click);
			// 
			// SizeLabel
			// 
			this.SizeLabel.AutoSize = true;
			this.SizeLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.SizeLabel.Location = new System.Drawing.Point(93, 231);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Size = new System.Drawing.Size(53, 13);
			this.SizeLabel.TabIndex = 3;
			this.SizeLabel.Text = "SizeLabel";
			// 
			// PromptForm
			// 
			this.AcceptButton = this.OkButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CloseButton;
			this.ClientSize = new System.Drawing.Size(464, 261);
			this.Controls.Add(this.SizeLabel);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.CopyButton);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.EditTextBox);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PromptForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Prompt Form";
			this.Load += new System.EventHandler(this.PromptForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		public System.Windows.Forms.TextBox EditTextBox;
		private System.Windows.Forms.Button CopyButton;
		private System.Windows.Forms.Label SizeLabel;
	}
}