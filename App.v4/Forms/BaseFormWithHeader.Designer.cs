namespace x360ce.App.Controls
{
	partial class BaseFormWithHeader
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseFormWithHeader));
			this.TopPanel = new System.Windows.Forms.Panel();
			this.HeaderTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.HelpSubjectLabel = new System.Windows.Forms.Label();
			this.HelpPictureBox = new System.Windows.Forms.PictureBox();
			this.HelpBodyLabel = new System.Windows.Forms.Label();
			this.HeaderPictureBox = new System.Windows.Forms.PictureBox();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.TopPanel.SuspendLayout();
			this.HeaderTableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).BeginInit();
			this.SuspendLayout();
			//
			// TopPanel
			//
			this.TopPanel.AutoSize = true;
			this.TopPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.TopPanel.BackColor = System.Drawing.SystemColors.Info;
			this.TopPanel.Controls.Add(this.BusyLoadingCircle);
			this.TopPanel.Controls.Add(this.HeaderTableLayoutPanel);
			this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.TopPanel.Location = new System.Drawing.Point(0, 0);
			this.TopPanel.Name = "TopPanel";
			this.TopPanel.Padding = new System.Windows.Forms.Padding(8);
			this.TopPanel.Size = new System.Drawing.Size(948, 72);
			this.TopPanel.TabIndex = 22;
			//
			// HeaderTableLayoutPanel
			//
			this.HeaderTableLayoutPanel.AutoSize = true;
			this.HeaderTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.HeaderTableLayoutPanel.ColumnCount = 3;
			this.HeaderTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HeaderTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.HeaderTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.HeaderTableLayoutPanel.Controls.Add(this.HelpSubjectLabel, 0, 0);
			this.HeaderTableLayoutPanel.Controls.Add(this.HelpPictureBox, 0, 1);
			this.HeaderTableLayoutPanel.Controls.Add(this.HelpBodyLabel, 1, 1);
			this.HeaderTableLayoutPanel.Controls.Add(this.HeaderPictureBox, 2, 0);
			this.HeaderTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HeaderTableLayoutPanel.Location = new System.Drawing.Point(8, 8);
			this.HeaderTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.HeaderTableLayoutPanel.Name = "HeaderTableLayoutPanel";
			this.HeaderTableLayoutPanel.RowCount = 2;
			this.HeaderTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HeaderTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.HeaderTableLayoutPanel.Size = new System.Drawing.Size(932, 56);
			this.HeaderTableLayoutPanel.TabIndex = 0;
			//
			// HelpSubjectLabel
			//
			this.HelpSubjectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HeaderTableLayoutPanel.SetColumnSpan(this.HelpSubjectLabel, 2);
			this.HelpSubjectLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.HelpSubjectLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HelpSubjectLabel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
			this.HelpSubjectLabel.Name = "HelpSubjectLabel";
			this.HelpSubjectLabel.Size = new System.Drawing.Size(860, 16);
			this.HelpSubjectLabel.TabIndex = 5;
			this.HelpSubjectLabel.Text = "Help Subject";
			//
			// HelpPictureBox
			//
			this.HelpPictureBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.HelpPictureBox.Image = global::x360ce.App.Properties.Resources.tip_48x48;
			this.HelpPictureBox.Margin = new System.Windows.Forms.Padding(4);
			this.HelpPictureBox.Name = "HelpPictureBox";
			this.HelpPictureBox.Size = new System.Drawing.Size(24, 24);
			this.HelpPictureBox.TabIndex = 8;
			this.HelpPictureBox.TabStop = false;
			//
			// HelpBodyLabel
			//
			this.HelpBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HelpBodyLabel.AutoEllipsis = true;
			this.HelpBodyLabel.Margin = new System.Windows.Forms.Padding(4);
			this.HelpBodyLabel.Name = "HelpBodyLabel";
			this.HelpBodyLabel.Size = new System.Drawing.Size(836, 32);
			this.HelpBodyLabel.TabIndex = 7;
			this.HelpBodyLabel.Text = resources.GetString("HelpBodyLabel.Text");
			//
			// HeaderPictureBox
			//
			this.HeaderPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HeaderPictureBox.Image = global::x360ce.App.Properties.Resources.app_96x96;
			this.HeaderPictureBox.Margin = new System.Windows.Forms.Padding(8, 4, 4, 4);
			this.HeaderPictureBox.Name = "HeaderPictureBox";
			this.HeaderTableLayoutPanel.SetRowSpan(this.HeaderPictureBox, 2);
			this.HeaderPictureBox.Size = new System.Drawing.Size(48, 48);
			this.HeaderPictureBox.TabIndex = 6;
			this.HeaderPictureBox.TabStop = false;
			//
			// BusyLoadingCircle
			//
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(880, 12);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			//
			// BaseFormWithHeader
			//
			this.ClientSize = new System.Drawing.Size(948, 525);
			this.Controls.Add(this.TopPanel);
			this.Name = "BaseFormWithHeader";
			this.Text = "BaseForm";
			this.TopPanel.ResumeLayout(false);
			this.TopPanel.PerformLayout();
			this.HeaderTableLayoutPanel.ResumeLayout(false);
			this.HeaderTableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.HelpPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.HeaderPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		internal System.Windows.Forms.Panel TopPanel;
		internal System.Windows.Forms.TableLayoutPanel HeaderTableLayoutPanel;
		internal MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		internal System.Windows.Forms.PictureBox HeaderPictureBox;
		internal System.Windows.Forms.Label HelpBodyLabel;
		internal System.Windows.Forms.PictureBox HelpPictureBox;
		internal System.Windows.Forms.Label HelpSubjectLabel;
	}
}
