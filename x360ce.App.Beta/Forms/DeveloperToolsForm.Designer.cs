namespace x360ce.App.Forms
{
	partial class DeveloperToolsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeveloperToolsForm));
			this.CompressXmlResourcesButton = new System.Windows.Forms.Button();
			this.LogTextBox = new System.Windows.Forms.TextBox();
			this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// CompressXmlResourcesButton
			// 
			this.CompressXmlResourcesButton.Location = new System.Drawing.Point(12, 12);
			this.CompressXmlResourcesButton.Name = "CompressXmlResourcesButton";
			this.CompressXmlResourcesButton.Size = new System.Drawing.Size(168, 23);
			this.CompressXmlResourcesButton.TabIndex = 0;
			this.CompressXmlResourcesButton.Text = "Compress XML Resources";
			this.CompressXmlResourcesButton.UseVisualStyleBackColor = true;
			this.CompressXmlResourcesButton.Click += new System.EventHandler(this.CompressXmlResourcesButton_Click);
			// 
			// LogTextBox
			// 
			this.LogTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.LogTextBox.Location = new System.Drawing.Point(12, 87);
			this.LogTextBox.Multiline = true;
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.Size = new System.Drawing.Size(491, 285);
			this.LogTextBox.TabIndex = 1;
			// 
			// WorkingFolderTextBox
			// 
			this.WorkingFolderTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WorkingFolderTextBox.Location = new System.Drawing.Point(12, 54);
			this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
			this.WorkingFolderTextBox.Size = new System.Drawing.Size(491, 20);
			this.WorkingFolderTextBox.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 38);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(82, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Working Folder:";
			// 
			// DeveloperToolsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(515, 384);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.WorkingFolderTextBox);
			this.Controls.Add(this.LogTextBox);
			this.Controls.Add(this.CompressXmlResourcesButton);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "DeveloperToolsForm";
			this.Text = "Developer Tools";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DeveloperToolsForm_FormClosing);
			this.Load += new System.EventHandler(this.DeveloperToolsForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CompressXmlResourcesButton;
		private System.Windows.Forms.TextBox LogTextBox;
		private System.Windows.Forms.TextBox WorkingFolderTextBox;
		private System.Windows.Forms.Label label1;
	}
}