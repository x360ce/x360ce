namespace x360ce.App.Forms
{
	partial class WebBrowserForm
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

		#region ■ Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.MainWebBrowser = new System.Windows.Forms.WebBrowser();
			this.CloseButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// MainWebBrowser
			// 
			this.MainWebBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainWebBrowser.Location = new System.Drawing.Point(12, 12);
			this.MainWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.MainWebBrowser.Name = "MainWebBrowser";
			this.MainWebBrowser.Size = new System.Drawing.Size(440, 388);
			this.MainWebBrowser.TabIndex = 0;
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.Location = new System.Drawing.Point(377, 406);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 1;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// WebBrowserForm
			// 
			this.ClientSize = new System.Drawing.Size(464, 441);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.MainWebBrowser);
			this.Name = "WebBrowserForm";
			this.Text = "WebBrowserForm";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser MainWebBrowser;
		private System.Windows.Forms.Button CloseButton;
	}
}
