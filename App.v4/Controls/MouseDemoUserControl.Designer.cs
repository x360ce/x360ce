namespace x360ce.App.Controls
{
	partial class MouseDemoUserControl
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
			this.components = new System.ComponentModel.Container();
			this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.CaptionLabel = new System.Windows.Forms.Label();
			this.DemoPictureBox = new System.Windows.Forms.PictureBox();
			this.PlayButton = new System.Windows.Forms.Button();
			this.DescriptionLabel = new System.Windows.Forms.Label();
			this.PhaseLabel = new System.Windows.Forms.Label();
			this.NoteLabel = new System.Windows.Forms.Label();
			this.DemoTimer = new System.Windows.Forms.Timer(this.components);
			this.LayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DemoPictureBox)).BeginInit();
			this.SuspendLayout();
			//
			// LayoutPanel
			//
			this.LayoutPanel.AutoSize = true;
			this.LayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.LayoutPanel.ColumnCount = 2;
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.Controls.Add(this.DemoPictureBox, 0, 0);
			this.LayoutPanel.Controls.Add(this.CaptionLabel, 1, 0);
			this.LayoutPanel.Controls.Add(this.DescriptionLabel, 1, 1);
			this.LayoutPanel.Controls.Add(this.PlayButton, 1, 2);
			this.LayoutPanel.Controls.Add(this.PhaseLabel, 0, 3);
			this.LayoutPanel.Controls.Add(this.NoteLabel, 0, 4);
			this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.LayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.LayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.LayoutPanel.Name = "LayoutPanel";
			this.LayoutPanel.RowCount = 5;
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.SetColumnSpan(this.PhaseLabel, 2);
			this.LayoutPanel.SetColumnSpan(this.NoteLabel, 2);
			this.LayoutPanel.SetRowSpan(this.DemoPictureBox, 3);
			this.LayoutPanel.Size = new System.Drawing.Size(436, 197);
			this.LayoutPanel.TabIndex = 0;
			//
			// CaptionLabel
			//
			this.CaptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CaptionLabel.AutoSize = true;
			this.CaptionLabel.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.CaptionLabel.Name = "CaptionLabel";
			this.CaptionLabel.Size = new System.Drawing.Size(103, 13);
			this.CaptionLabel.TabIndex = 0;
			this.CaptionLabel.Text = "Left Stick: Position";
			//
			// DemoPictureBox
			//
			this.DemoPictureBox.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.DemoPictureBox.Name = "DemoPictureBox";
			this.DemoPictureBox.Size = new System.Drawing.Size(360, 144);
			this.DemoPictureBox.TabIndex = 1;
			this.DemoPictureBox.TabStop = false;
			this.DemoPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.DemoPictureBox_Paint);
			//
			// PlayButton
			//
			this.PlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this.PlayButton.AutoSize = true;
			this.PlayButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.PlayButton.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.PlayButton.Name = "PlayButton";
			this.PlayButton.Size = new System.Drawing.Size(48, 23);
			this.PlayButton.TabIndex = 2;
			this.PlayButton.Text = "Play";
			this.PlayButton.UseVisualStyleBackColor = true;
			this.PlayButton.Click += new System.EventHandler(this.PlayButton_Click);
			//
			// DescriptionLabel
			//
			this.DescriptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.DescriptionLabel.AutoSize = true;
			this.DescriptionLabel.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.DescriptionLabel.Name = "DescriptionLabel";
			this.DescriptionLabel.Size = new System.Drawing.Size(220, 13);
			this.DescriptionLabel.TabIndex = 3;
			this.DescriptionLabel.Text = "Follows how far the mouse moved.";
			//
			// PhaseLabel
			//
			this.PhaseLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.PhaseLabel.AutoSize = true;
			this.PhaseLabel.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.PhaseLabel.Name = "PhaseLabel";
			this.PhaseLabel.Size = new System.Drawing.Size(165, 13);
			this.PhaseLabel.TabIndex = 4;
			this.PhaseLabel.Text = "Mouse rests: stick at the centre";
			//
			// NoteLabel
			//
			this.NoteLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.NoteLabel.AutoSize = true;
			this.NoteLabel.Margin = new System.Windows.Forms.Padding(8, 8, 0, 0);
			this.NoteLabel.Name = "NoteLabel";
			this.NoteLabel.Size = new System.Drawing.Size(308, 13);
			this.NoteLabel.TabIndex = 5;
			this.NoteLabel.Visible = false;
			//
			// DemoTimer
			//
			this.DemoTimer.Interval = 33;
			this.DemoTimer.Tick += new System.EventHandler(this.DemoTimer_Tick);
			//
			// MouseDemoUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.Controls.Add(this.LayoutPanel);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "MouseDemoUserControl";
			this.Size = new System.Drawing.Size(436, 197);
			this.LayoutPanel.ResumeLayout(false);
			this.LayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DemoPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel LayoutPanel;
		public System.Windows.Forms.Label CaptionLabel;
		public System.Windows.Forms.PictureBox DemoPictureBox;
		public System.Windows.Forms.Button PlayButton;
		public System.Windows.Forms.Label DescriptionLabel;
		public System.Windows.Forms.Label PhaseLabel;
		public System.Windows.Forms.Label NoteLabel;
		private System.Windows.Forms.Timer DemoTimer;
	}
}
