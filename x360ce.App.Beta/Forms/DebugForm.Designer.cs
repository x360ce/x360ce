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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DebugForm));
			this.performanceTestUserControl1 = new x360ce.App.Controls.DebugUserControl();
			this.SuspendLayout();
			// 
			// performanceTestUserControl1
			// 
			this.performanceTestUserControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.performanceTestUserControl1.Location = new System.Drawing.Point(12, 12);
			this.performanceTestUserControl1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
			this.performanceTestUserControl1.Name = "performanceTestUserControl1";
			this.performanceTestUserControl1.Size = new System.Drawing.Size(603, 343);
			this.performanceTestUserControl1.TabIndex = 0;
			// 
			// DebugForm
			// 
			this.ClientSize = new System.Drawing.Size(627, 367);
			this.Controls.Add(this.performanceTestUserControl1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "DebugForm";
			this.Text = "Debug Form";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DebugForm_FormClosing);
			this.Load += new System.EventHandler(this.DebugForm_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private Controls.DebugUserControl performanceTestUserControl1;
	}
}
