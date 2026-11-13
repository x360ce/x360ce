namespace x360ce.App.Controls
{
	partial class AboutControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

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
		void InitializeComponent()
		{
			this.ChangeLogTextBox = new System.Windows.Forms.TextBox();
			this.AboutDescriptionLabel = new System.Windows.Forms.Label();
			this.AboutProductLabel = new System.Windows.Forms.Label();
			this.ProductPictureBox = new System.Windows.Forms.PictureBox();
			this.AboutTabControl = new System.Windows.Forms.TabControl();
			this.ProgramTabPage = new System.Windows.Forms.TabPage();
			this.ProgramRichTextBox = new System.Windows.Forms.RichTextBox();
			this.ChangesTabPage = new System.Windows.Forms.TabPage();
			this.LicenseTabPage = new System.Windows.Forms.TabPage();
			this.LicenseTextBox = new System.Windows.Forms.TextBox();
			this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.ProductPictureBox)).BeginInit();
			this.AboutTabControl.SuspendLayout();
			this.ProgramTabPage.SuspendLayout();
			this.ChangesTabPage.SuspendLayout();
			this.LicenseTabPage.SuspendLayout();
			this.MainTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// ChangeLogTextBox
			// 
			this.ChangeLogTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ChangeLogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ChangeLogTextBox.Location = new System.Drawing.Point(0, 0);
			this.ChangeLogTextBox.Multiline = true;
			this.ChangeLogTextBox.Name = "ChangeLogTextBox";
			this.ChangeLogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.ChangeLogTextBox.Size = new System.Drawing.Size(605, 330);
			this.ChangeLogTextBox.TabIndex = 0;
			// 
			// AboutDescriptionLabel
			// 
			this.AboutDescriptionLabel.AutoSize = true;
			this.AboutDescriptionLabel.Location = new System.Drawing.Point(43, 16);
			this.AboutDescriptionLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 4);
			this.AboutDescriptionLabel.Name = "AboutDescriptionLabel";
			this.AboutDescriptionLabel.Size = new System.Drawing.Size(328, 13);
			this.AboutDescriptionLabel.TabIndex = 0;
			this.AboutDescriptionLabel.Text = "Settings editor and test application for XBOX 360 Controller Emulator";
			// 
			// AboutProductLabel
			// 
			this.AboutProductLabel.AutoSize = true;
			this.AboutProductLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AboutProductLabel.Location = new System.Drawing.Point(43, 3);
			this.AboutProductLabel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
			this.AboutProductLabel.Name = "AboutProductLabel";
			this.AboutProductLabel.Size = new System.Drawing.Size(197, 13);
			this.AboutProductLabel.TabIndex = 0;
			this.AboutProductLabel.Text = "XBOX 360 Controller Emulator {0}";
			// 
			// ProductPictureBox
			// 
			this.ProductPictureBox.Image = global::x360ce.App.Properties.Resources.app_128x128;
			this.ProductPictureBox.Location = new System.Drawing.Point(3, 3);
			this.ProductPictureBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
			this.ProductPictureBox.Name = "ProductPictureBox";
			this.MainTableLayoutPanel.SetRowSpan(this.ProductPictureBox, 2);
			this.ProductPictureBox.Size = new System.Drawing.Size(34, 26);
			this.ProductPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.ProductPictureBox.TabIndex = 0;
			this.ProductPictureBox.TabStop = false;
			// 
			// AboutTabControl
			// 
			this.MainTableLayoutPanel.SetColumnSpan(this.AboutTabControl, 2);
			this.AboutTabControl.Controls.Add(this.ProgramTabPage);
			this.AboutTabControl.Controls.Add(this.ChangesTabPage);
			this.AboutTabControl.Controls.Add(this.LicenseTabPage);
			this.AboutTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.AboutTabControl.Location = new System.Drawing.Point(3, 32);
			this.AboutTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.AboutTabControl.Name = "AboutTabControl";
			this.AboutTabControl.SelectedIndex = 0;
			this.AboutTabControl.Size = new System.Drawing.Size(613, 461);
			this.AboutTabControl.TabIndex = 1;
			// 
			// ProgramTabPage
			// 
			this.ProgramTabPage.Controls.Add(this.ProgramRichTextBox);
			this.ProgramTabPage.Location = new System.Drawing.Point(4, 22);
			this.ProgramTabPage.Name = "ProgramTabPage";
			this.ProgramTabPage.Size = new System.Drawing.Size(605, 330);
			this.ProgramTabPage.TabIndex = 0;
			this.ProgramTabPage.Text = "Program";
			this.ProgramTabPage.UseVisualStyleBackColor = true;
			// 
			// ProgramRichTextBox
			// 
			this.ProgramRichTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.ProgramRichTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ProgramRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProgramRichTextBox.Location = new System.Drawing.Point(0, 0);
			this.ProgramRichTextBox.Name = "ProgramRichTextBox";
			this.ProgramRichTextBox.ReadOnly = true;
			this.ProgramRichTextBox.Size = new System.Drawing.Size(605, 330);
			this.ProgramRichTextBox.TabIndex = 0;
			this.ProgramRichTextBox.Text = "";
			// 
			// ChangesTabPage
			// 
			this.ChangesTabPage.Controls.Add(this.ChangeLogTextBox);
			this.ChangesTabPage.Location = new System.Drawing.Point(4, 22);
			this.ChangesTabPage.Name = "ChangesTabPage";
			this.ChangesTabPage.Size = new System.Drawing.Size(605, 330);
			this.ChangesTabPage.TabIndex = 1;
			this.ChangesTabPage.Text = "Changes";
			this.ChangesTabPage.UseVisualStyleBackColor = true;
			// 
			// LicenseTabPage
			// 
			this.LicenseTabPage.Controls.Add(this.LicenseTextBox);
			this.LicenseTabPage.Location = new System.Drawing.Point(4, 22);
			this.LicenseTabPage.Name = "LicenseTabPage";
			this.LicenseTabPage.Size = new System.Drawing.Size(605, 330);
			this.LicenseTabPage.TabIndex = 2;
			this.LicenseTabPage.Text = "License";
			this.LicenseTabPage.UseVisualStyleBackColor = true;
			// 
			// LicenseTextBox
			// 
			this.LicenseTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.LicenseTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LicenseTextBox.Location = new System.Drawing.Point(0, 0);
			this.LicenseTextBox.Multiline = true;
			this.LicenseTextBox.Name = "LicenseTextBox";
			this.LicenseTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.LicenseTextBox.Size = new System.Drawing.Size(605, 330);
			this.LicenseTextBox.TabIndex = 1;
			// 
			// MainTableLayoutPanel
			// 
			this.MainTableLayoutPanel.ColumnCount = 2;
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.Controls.Add(this.ProductPictureBox, 0, 0);
			this.MainTableLayoutPanel.Controls.Add(this.AboutProductLabel, 1, 0);
			this.MainTableLayoutPanel.Controls.Add(this.AboutDescriptionLabel, 1, 1);
			this.MainTableLayoutPanel.Controls.Add(this.AboutTabControl, 0, 2);
			this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
			this.MainTableLayoutPanel.RowCount = 3;
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.Size = new System.Drawing.Size(619, 496);
			this.MainTableLayoutPanel.TabIndex = 0;
			// 
			// AboutControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainTableLayoutPanel);
			this.Name = "AboutControl";
			this.Size = new System.Drawing.Size(619, 496);
			this.Load += new System.EventHandler(this.AboutControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.ProductPictureBox)).EndInit();
			this.AboutTabControl.ResumeLayout(false);
			this.ProgramTabPage.ResumeLayout(false);
			this.ChangesTabPage.ResumeLayout(false);
			this.ChangesTabPage.PerformLayout();
			this.LicenseTabPage.ResumeLayout(false);
			this.LicenseTabPage.PerformLayout();
			this.MainTableLayoutPanel.ResumeLayout(false);
			this.MainTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		internal System.Windows.Forms.TextBox ChangeLogTextBox;
		internal System.Windows.Forms.Label AboutDescriptionLabel;
		internal System.Windows.Forms.Label AboutProductLabel;
		internal System.Windows.Forms.PictureBox ProductPictureBox;
		internal System.Windows.Forms.TabControl AboutTabControl;
		internal System.Windows.Forms.TabPage ProgramTabPage;
		internal System.Windows.Forms.RichTextBox ProgramRichTextBox;
		internal System.Windows.Forms.TabPage ChangesTabPage;
		internal System.Windows.Forms.TabPage LicenseTabPage;
		internal System.Windows.Forms.TextBox LicenseTextBox;
		private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
	}
}
