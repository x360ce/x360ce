namespace x360ce.App.Forms
{
	partial class DebugForm
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
			this.performanceTestUserControl1 = new x360ce.App.Controls.DebugUserControl();
			this.SuspendLayout();
			// 
			// performanceTestUserControl1
			// 
			this.performanceTestUserControl1.AutoSize = true;
			this.performanceTestUserControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.performanceTestUserControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.performanceTestUserControl1.Location = new System.Drawing.Point(12, 12);
			this.performanceTestUserControl1.Margin = new System.Windows.Forms.Padding(0);
			this.performanceTestUserControl1.Name = "performanceTestUserControl1";
			this.performanceTestUserControl1.Size = new System.Drawing.Size(234, 169);
			this.performanceTestUserControl1.TabIndex = 0;
			// 
			// DebugForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(258, 193);
			this.Controls.Add(this.performanceTestUserControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.MaximizeBox = false;
			this.Name = "DebugForm";
			this.Padding = new System.Windows.Forms.Padding(12);
			this.Text = "Debug Form";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DebugForm_FormClosing);
			this.Load += new System.EventHandler(this.DebugForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private Controls.DebugUserControl performanceTestUserControl1;
	}
}
