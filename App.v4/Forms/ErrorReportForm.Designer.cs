namespace x360ce.App.Forms
{
	partial class ErrorReportForm
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
			this.ErrorReportPanel = new JocysCom.ClassLibrary.Controls.ErrorReportUserControl();
			this.SuspendLayout();
			//
			// ErrorReportPanel
			//
			this.ErrorReportPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ErrorReportPanel.Location = new System.Drawing.Point(0, 0);
			this.ErrorReportPanel.Name = "ErrorReportPanel";
			this.ErrorReportPanel.Size = new System.Drawing.Size(784, 561);
			this.ErrorReportPanel.TabIndex = 0;
			//
			// ErrorReportForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(784, 561);
			this.Controls.Add(this.ErrorReportPanel);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(560, 420);
			this.Name = "ErrorReportForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "X360CE - Error Report";
			this.ResumeLayout(false);
		}

		#endregion

		public JocysCom.ClassLibrary.Controls.ErrorReportUserControl ErrorReportPanel;
	}
}
