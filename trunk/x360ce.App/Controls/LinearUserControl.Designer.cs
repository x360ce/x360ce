namespace x360ce.App.Controls
{
	partial class LinearUserControl
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
			this.LeftTriggerDeadZoneYLabel = new System.Windows.Forms.Label();
			this.LinearTrackBar = new System.Windows.Forms.TrackBar();
			this.LeftThumbDeadZoneYTextBox = new System.Windows.Forms.TextBox();
			this.LinearPictureBox = new System.Windows.Forms.PictureBox();
			this.TestButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// LeftTriggerDeadZoneYLabel
			// 
			this.LeftTriggerDeadZoneYLabel.AutoSize = true;
			this.LeftTriggerDeadZoneYLabel.Location = new System.Drawing.Point(12, 36);
			this.LeftTriggerDeadZoneYLabel.Name = "LeftTriggerDeadZoneYLabel";
			this.LeftTriggerDeadZoneYLabel.Size = new System.Drawing.Size(17, 13);
			this.LeftTriggerDeadZoneYLabel.TabIndex = 7;
			this.LeftTriggerDeadZoneYLabel.Text = "Y:";
			// 
			// LinearTrackBar
			// 
			this.LinearTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LinearTrackBar.AutoSize = false;
			this.LinearTrackBar.Location = new System.Drawing.Point(1, 137);
			this.LinearTrackBar.Maximum = 100;
			this.LinearTrackBar.Minimum = -100;
			this.LinearTrackBar.Name = "LinearTrackBar";
			this.LinearTrackBar.Size = new System.Drawing.Size(227, 32);
			this.LinearTrackBar.TabIndex = 9;
			this.LinearTrackBar.TickFrequency = 2;
			// 
			// LeftThumbDeadZoneYTextBox
			// 
			this.LeftThumbDeadZoneYTextBox.Location = new System.Drawing.Point(35, 33);
			this.LeftThumbDeadZoneYTextBox.Name = "LeftThumbDeadZoneYTextBox";
			this.LeftThumbDeadZoneYTextBox.ReadOnly = true;
			this.LeftThumbDeadZoneYTextBox.Size = new System.Drawing.Size(43, 20);
			this.LeftThumbDeadZoneYTextBox.TabIndex = 8;
			this.LeftThumbDeadZoneYTextBox.TabStop = false;
			this.LeftThumbDeadZoneYTextBox.Text = "0 % ";
			this.LeftThumbDeadZoneYTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// LinearPictureBox
			// 
			this.LinearPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LinearPictureBox.BackColor = System.Drawing.Color.White;
			this.LinearPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.LinearPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.LinearPictureBox.Location = new System.Drawing.Point(98, 3);
			this.LinearPictureBox.Name = "LinearPictureBox";
			this.LinearPictureBox.Size = new System.Drawing.Size(130, 130);
			this.LinearPictureBox.TabIndex = 10;
			this.LinearPictureBox.TabStop = false;
			// 
			// TestButton
			// 
			this.TestButton.Location = new System.Drawing.Point(4, 4);
			this.TestButton.Name = "TestButton";
			this.TestButton.Size = new System.Drawing.Size(75, 23);
			this.TestButton.TabIndex = 11;
			this.TestButton.Text = "Test";
			this.TestButton.UseVisualStyleBackColor = true;
			this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
			// 
			// LinearUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.TestButton);
			this.Controls.Add(this.LinearPictureBox);
			this.Controls.Add(this.LeftTriggerDeadZoneYLabel);
			this.Controls.Add(this.LinearTrackBar);
			this.Controls.Add(this.LeftThumbDeadZoneYTextBox);
			this.Name = "LinearUserControl";
			this.Size = new System.Drawing.Size(231, 179);
			((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label LeftTriggerDeadZoneYLabel;
		private System.Windows.Forms.TrackBar LinearTrackBar;
		private System.Windows.Forms.TextBox LeftThumbDeadZoneYTextBox;
		private System.Windows.Forms.PictureBox LinearPictureBox;
		private System.Windows.Forms.Button TestButton;
	}
}
