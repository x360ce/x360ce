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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.MainWebBrowser = new System.Windows.Forms.WebBrowser();
			this.ButtonsPanel = new System.Windows.Forms.TableLayoutPanel();
			this.CloseButton = new System.Windows.Forms.Button();
			this.RootLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ButtonsPanel.SuspendLayout();
			this.RootLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// MainWebBrowser
			//
			this.MainWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainWebBrowser.Location = new System.Drawing.Point(0, 0);
			this.MainWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.MainWebBrowser.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.MainWebBrowser.Name = "MainWebBrowser";
			this.MainWebBrowser.Size = new System.Drawing.Size(464, 396);
			this.MainWebBrowser.TabIndex = 0;
			//
			// ButtonsPanel
			//
			this.ButtonsPanel.AutoSize = true;
			this.ButtonsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ButtonsPanel.ColumnCount = 1;
			this.ButtonsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ButtonsPanel.Controls.Add(this.CloseButton, 0, 0);
			this.ButtonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.ButtonsPanel.Location = new System.Drawing.Point(0, 396);
			this.ButtonsPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonsPanel.Name = "ButtonsPanel";
			this.ButtonsPanel.Padding = new System.Windows.Forms.Padding(0);
			this.ButtonsPanel.RowCount = 1;
			this.ButtonsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ButtonsPanel.Size = new System.Drawing.Size(464, 44);
			this.ButtonsPanel.TabIndex = 1;
			//
			// CloseButton
			//
			this.CloseButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.CloseButton.AutoSize = true;
			this.CloseButton.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.CloseButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(76, 24);
			this.CloseButton.TabIndex = 0;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// RootLayoutPanel
			// 
			this.RootLayoutPanel.ColumnCount = 1;
			this.RootLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootLayoutPanel.Controls.Add(this.MainWebBrowser, 0, 0);
			this.RootLayoutPanel.Controls.Add(this.ButtonsPanel, 0, 1);
			this.RootLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RootLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.RootLayoutPanel.Name = "RootLayoutPanel";
			this.RootLayoutPanel.RowCount = 2;
			this.RootLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.RootLayoutPanel.TabIndex = 0;
			//
			// WebBrowserForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(464, 440);
			this.Controls.Add(this.RootLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.Padding = new System.Windows.Forms.Padding(0, 0, 8, 8);
			this.Name = "WebBrowserForm";
			this.Text = "WebBrowserForm";
			this.ButtonsPanel.ResumeLayout(false);
			this.ButtonsPanel.PerformLayout();
			this.RootLayoutPanel.ResumeLayout(false);
			this.RootLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.WebBrowser MainWebBrowser;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.TableLayoutPanel ButtonsPanel;
		private System.Windows.Forms.TableLayoutPanel RootLayoutPanel;
	}
}
