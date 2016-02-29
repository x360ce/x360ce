namespace x360ce.App.Controls
{
	partial class BaseForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseForm));
			this.TopPanel = new System.Windows.Forms.Panel();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.HelpBodyLabel = new System.Windows.Forms.Label();
			this.HelpPictureBox = new System.Windows.Forms.PictureBox();
			this.HelpSubjectLabel = new System.Windows.Forms.Label();
			this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
			this.TopPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// TopPanel
			// 
			this.TopPanel.BackColor = System.Drawing.SystemColors.Info;
			this.TopPanel.Controls.Add(this.BusyLoadingCircle);
			this.TopPanel.Controls.Add(this.HelpBodyLabel);
			this.TopPanel.Controls.Add(this.HelpPictureBox);
			this.TopPanel.Controls.Add(this.HelpSubjectLabel);
			this.TopPanel.Controls.Add(this.HeaderPictureBox);
			this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.TopPanel.Location = new System.Drawing.Point(0, 0);
			this.TopPanel.Name = "TopPanel";
			this.TopPanel.Size = new System.Drawing.Size(948, 64);
			this.TopPanel.TabIndex = 22;
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(894, 9);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			// 
			// HelpBodyLabel
			// 
			this.HelpBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpBodyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpBodyLabel.Location = new System.Drawing.Point(42, 29);
			this.HelpBodyLabel.Name = "HelpBodyLabel";
			this.HelpBodyLabel.Size = new System.Drawing.Size(846, 32);
			this.HelpBodyLabel.TabIndex = 7;
			this.HelpBodyLabel.Text = "Help Body";
			// 
			// HelpPictureBox
			// 
			this.HelpPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HelpPictureBox.Image")));
			this.HelpPictureBox.Location = new System.Drawing.Point(6, 29);
			this.HelpPictureBox.Name = "HelpPictureBox";
			this.HelpPictureBox.Size = new System.Drawing.Size(24, 24);
			this.HelpPictureBox.TabIndex = 8;
			this.HelpPictureBox.TabStop = false;
			// 
			// HelpSubjectLabel
			// 
			this.HelpSubjectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpSubjectLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpSubjectLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HelpSubjectLabel.Location = new System.Drawing.Point(6, 9);
			this.HelpSubjectLabel.Name = "HelpSubjectLabel";
			this.HelpSubjectLabel.Size = new System.Drawing.Size(882, 20);
			this.HelpSubjectLabel.TabIndex = 5;
			this.HelpSubjectLabel.Text = "Help Subject";
			// 
			// HeaderPictureBox
			// 
			this.HeaderPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HeaderPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("HeaderPictureBox.Image")));
			this.HeaderPictureBox.Location = new System.Drawing.Point(894, 9);
			this.HeaderPictureBox.Name = "HeaderPictureBox";
			this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
			this.HeaderPictureBox.TabIndex = 6;
			this.HeaderPictureBox.TabStop = false;
			// 
			// BaseForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(948, 525);
			this.Controls.Add(this.TopPanel);
			this.Name = "BaseForm";
			this.Text = "BaseForm";
			this.TopPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel TopPanel;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		private System.Windows.Forms.PictureBox HeaderPictureBox;
		private System.Windows.Forms.Label HelpBodyLabel;
		private System.Windows.Forms.PictureBox HelpPictureBox;
		private System.Windows.Forms.Label HelpSubjectLabel;
	}
}