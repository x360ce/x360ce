namespace JocysCom.ClassLibrary.Controls
{
	partial class LogTextBoxUserControl
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
			this.LogTextBox = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// LogTextBox
			// 
			this.LogTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.LogTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LogTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
			this.LogTextBox.Location = new System.Drawing.Point(0, 0);
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.Size = new System.Drawing.Size(100, 100);
			this.LogTextBox.TabIndex = 0;
			this.LogTextBox.Text = "";
			// 
			// LogTextBoxUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.LogTextBox);
			this.Font = new System.Drawing.Font("Consolas", 8.25F);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "LogTextBoxUserControl";
			this.Size = new System.Drawing.Size(100, 100);
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.RichTextBox LogTextBox;
	}
}
