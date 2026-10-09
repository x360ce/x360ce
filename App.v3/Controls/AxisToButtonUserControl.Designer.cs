namespace x360ce.App.Controls
{
    partial class AxisToButtonUserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.DeadZoneTrackBar = new System.Windows.Forms.TrackBar();
			this.ButtonNameLabel = new System.Windows.Forms.Label();
			this.MappedAxisTextBox = new System.Windows.Forms.TextBox();
			this.DeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.DeadZoneTextBox = new System.Windows.Forms.TextBox();
			this.ArrowPictureBox = new System.Windows.Forms.PictureBox();
			this.ButtonImagePictureBox = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ArrowPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ButtonImagePictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// DeadZoneTrackBar
			// 
			this.DeadZoneTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DeadZoneTrackBar.AutoSize = false;
			this.DeadZoneTrackBar.LargeChange = 10;
			this.DeadZoneTrackBar.Location = new System.Drawing.Point(217, 0);
			this.DeadZoneTrackBar.Maximum = 100;
			this.DeadZoneTrackBar.Name = "DeadZoneTrackBar";
			this.DeadZoneTrackBar.Size = new System.Drawing.Size(227, 28);
			this.DeadZoneTrackBar.TabIndex = 7;
			this.DeadZoneTrackBar.TickFrequency = 2;
			// 
			// ButtonNameLabel
			// 
			this.ButtonNameLabel.AutoSize = true;
			this.ButtonNameLabel.Location = new System.Drawing.Point(134, 6);
			this.ButtonNameLabel.Name = "ButtonNameLabel";
			this.ButtonNameLabel.Size = new System.Drawing.Size(72, 13);
			this.ButtonNameLabel.TabIndex = 5;
			this.ButtonNameLabel.Text = "Button Name:";
			// 
			// MappedAxisTextBox
			// 
			this.MappedAxisTextBox.Location = new System.Drawing.Point(0, 3);
			this.MappedAxisTextBox.Name = "MappedAxisTextBox";
			this.MappedAxisTextBox.ReadOnly = true;
			this.MappedAxisTextBox.Size = new System.Drawing.Size(75, 20);
			this.MappedAxisTextBox.TabIndex = 6;
			this.MappedAxisTextBox.TabStop = false;
			// 
			// DeadZoneNumericUpDown
			// 
			this.DeadZoneNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DeadZoneNumericUpDown.Location = new System.Drawing.Point(499, 3);
			this.DeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
			this.DeadZoneNumericUpDown.Name = "DeadZoneNumericUpDown";
			this.DeadZoneNumericUpDown.Size = new System.Drawing.Size(52, 20);
			this.DeadZoneNumericUpDown.TabIndex = 20;
			this.DeadZoneNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// DeadZoneTextBox
			// 
			this.DeadZoneTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.DeadZoneTextBox.Location = new System.Drawing.Point(450, 3);
			this.DeadZoneTextBox.Name = "DeadZoneTextBox";
			this.DeadZoneTextBox.ReadOnly = true;
			this.DeadZoneTextBox.Size = new System.Drawing.Size(43, 20);
			this.DeadZoneTextBox.TabIndex = 19;
			this.DeadZoneTextBox.TabStop = false;
			this.DeadZoneTextBox.Text = "0 % ";
			this.DeadZoneTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// ArrowPictureBox
			// 
			this.ArrowPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.ArrowPictureBox.Image = global::x360ce.App.Properties.Resources.arrow_right_gray_16x16;
			this.ArrowPictureBox.Location = new System.Drawing.Point(81, 6);
			this.ArrowPictureBox.Name = "ArrowPictureBox";
			this.ArrowPictureBox.Size = new System.Drawing.Size(16, 16);
			this.ArrowPictureBox.TabIndex = 0;
			this.ArrowPictureBox.TabStop = false;
			this.ArrowPictureBox.EnabledChanged += new System.EventHandler(this.ArrowPictureBox_EnabledChanged);
			// 
			// ButtonImagePictureBox
			// 
			this.ButtonImagePictureBox.BackgroundImage = global::x360ce.App.Properties.Resources.add_16x16;
			this.ButtonImagePictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.ButtonImagePictureBox.Location = new System.Drawing.Point(103, 0);
			this.ButtonImagePictureBox.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.ButtonImagePictureBox.Name = "ButtonImagePictureBox";
			this.ButtonImagePictureBox.Size = new System.Drawing.Size(28, 28);
			this.ButtonImagePictureBox.TabIndex = 0;
			this.ButtonImagePictureBox.TabStop = false;
			this.ButtonImagePictureBox.EnabledChanged += new System.EventHandler(this.ButtonImagePictureBox_EnabledChanged);
			this.ButtonImagePictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.ButtonImagePictureBox_Paint);
			// 
			// AxisToButtonUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.DeadZoneNumericUpDown);
			this.Controls.Add(this.DeadZoneTextBox);
			this.Controls.Add(this.DeadZoneTrackBar);
			this.Controls.Add(this.ButtonNameLabel);
			this.Controls.Add(this.MappedAxisTextBox);
			this.Controls.Add(this.ArrowPictureBox);
			this.Controls.Add(this.ButtonImagePictureBox);
			this.Name = "AxisToButtonUserControl";
			this.Size = new System.Drawing.Size(554, 28);
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ArrowPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ButtonImagePictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox ButtonImagePictureBox;
        private System.Windows.Forms.TrackBar DeadZoneTrackBar;
        private System.Windows.Forms.Label ButtonNameLabel;
        private System.Windows.Forms.TextBox MappedAxisTextBox;
        public System.Windows.Forms.NumericUpDown DeadZoneNumericUpDown;
        private System.Windows.Forms.TextBox DeadZoneTextBox;
        private System.Windows.Forms.PictureBox ArrowPictureBox;
    }
}
