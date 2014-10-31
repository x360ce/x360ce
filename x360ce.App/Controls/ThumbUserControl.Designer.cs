namespace x360ce.App.Controls
{
	partial class ThumbUserControl
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
            this.DInputLabel = new System.Windows.Forms.Label();
            this.SensitivityTrackBar = new System.Windows.Forms.TrackBar();
            this.DInputTextBox = new System.Windows.Forms.TextBox();
            this.LinearPictureBox = new System.Windows.Forms.PictureBox();
            this.MainGroupBox = new System.Windows.Forms.GroupBox();
            this.DeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.AntiDeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.AntiDeadZoneLabel = new System.Windows.Forms.Label();
            this.DeadZoneLabel = new System.Windows.Forms.Label();
            this.DeadZoneTrackBar = new System.Windows.Forms.TrackBar();
            this.DeadZoneTextBox = new System.Windows.Forms.TextBox();
            this.SensitivityTextBox = new System.Windows.Forms.TextBox();
            this.XInputTextBox = new System.Windows.Forms.TextBox();
            this.SensitivityLabel = new System.Windows.Forms.Label();
            this.XInputLabel = new System.Windows.Forms.Label();
            this.AntiDeadZoneTrackBar = new System.Windows.Forms.TrackBar();
            this.AntiDeadZoneTextBox = new System.Windows.Forms.TextBox();
            this.SensitivityCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.SensitivityTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).BeginInit();
            this.MainGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // DInputLabel
            // 
            this.DInputLabel.AutoSize = true;
            this.DInputLabel.Location = new System.Drawing.Point(306, 19);
            this.DInputLabel.Name = "DInputLabel";
            this.DInputLabel.Size = new System.Drawing.Size(42, 13);
            this.DInputLabel.TabIndex = 7;
            this.DInputLabel.Text = "DInput:";
            // 
            // SensitivityTrackBar
            // 
            this.SensitivityTrackBar.AutoSize = false;
            this.SensitivityTrackBar.Location = new System.Drawing.Point(179, 154);
            this.SensitivityTrackBar.Maximum = 100;
            this.SensitivityTrackBar.Minimum = -100;
            this.SensitivityTrackBar.Name = "SensitivityTrackBar";
            this.SensitivityTrackBar.Size = new System.Drawing.Size(227, 32);
            this.SensitivityTrackBar.TabIndex = 9;
            this.SensitivityTrackBar.TickFrequency = 2;
            this.SensitivityTrackBar.ValueChanged += new System.EventHandler(this.LinearTrackBar_ValueChanged);
            // 
            // DInputTextBox
            // 
            this.DInputTextBox.Location = new System.Drawing.Point(358, 16);
            this.DInputTextBox.Name = "DInputTextBox";
            this.DInputTextBox.ReadOnly = true;
            this.DInputTextBox.Size = new System.Drawing.Size(43, 20);
            this.DInputTextBox.TabIndex = 8;
            this.DInputTextBox.TabStop = false;
            this.DInputTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // LinearPictureBox
            // 
            this.LinearPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.LinearPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LinearPictureBox.Location = new System.Drawing.Point(6, 19);
            this.LinearPictureBox.Name = "LinearPictureBox";
            this.LinearPictureBox.Size = new System.Drawing.Size(167, 167);
            this.LinearPictureBox.TabIndex = 10;
            this.LinearPictureBox.TabStop = false;
            this.LinearPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.LinearPictureBox_Paint);
            // 
            // MainGroupBox
            // 
            this.MainGroupBox.Controls.Add(this.SensitivityCheckBox);
            this.MainGroupBox.Controls.Add(this.DeadZoneNumericUpDown);
            this.MainGroupBox.Controls.Add(this.AntiDeadZoneNumericUpDown);
            this.MainGroupBox.Controls.Add(this.AntiDeadZoneLabel);
            this.MainGroupBox.Controls.Add(this.DeadZoneLabel);
            this.MainGroupBox.Controls.Add(this.AntiDeadZoneTrackBar);
            this.MainGroupBox.Controls.Add(this.DeadZoneTrackBar);
            this.MainGroupBox.Controls.Add(this.AntiDeadZoneTextBox);
            this.MainGroupBox.Controls.Add(this.DeadZoneTextBox);
            this.MainGroupBox.Controls.Add(this.SensitivityTextBox);
            this.MainGroupBox.Controls.Add(this.XInputTextBox);
            this.MainGroupBox.Controls.Add(this.DInputTextBox);
            this.MainGroupBox.Controls.Add(this.LinearPictureBox);
            this.MainGroupBox.Controls.Add(this.SensitivityLabel);
            this.MainGroupBox.Controls.Add(this.XInputLabel);
            this.MainGroupBox.Controls.Add(this.SensitivityTrackBar);
            this.MainGroupBox.Controls.Add(this.DInputLabel);
            this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
            this.MainGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.MainGroupBox.Name = "MainGroupBox";
            this.MainGroupBox.Size = new System.Drawing.Size(608, 193);
            this.MainGroupBox.TabIndex = 12;
            this.MainGroupBox.TabStop = false;
            this.MainGroupBox.Text = "Thumb";
            // 
            // DeadZoneNumericUpDown
            // 
            this.DeadZoneNumericUpDown.Location = new System.Drawing.Point(461, 52);
            this.DeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.DeadZoneNumericUpDown.Name = "DeadZoneNumericUpDown";
            this.DeadZoneNumericUpDown.Size = new System.Drawing.Size(75, 20);
            this.DeadZoneNumericUpDown.TabIndex = 19;
            this.DeadZoneNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.DeadZoneNumericUpDown.ValueChanged += new System.EventHandler(this.DeadZoneNumericUpDown_ValueChanged);
            // 
            // AntiDeadZoneNumericUpDown
            // 
            this.AntiDeadZoneNumericUpDown.Location = new System.Drawing.Point(461, 103);
            this.AntiDeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.AntiDeadZoneNumericUpDown.Name = "AntiDeadZoneNumericUpDown";
            this.AntiDeadZoneNumericUpDown.Size = new System.Drawing.Size(75, 20);
            this.AntiDeadZoneNumericUpDown.TabIndex = 18;
            this.AntiDeadZoneNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.AntiDeadZoneNumericUpDown.ValueChanged += new System.EventHandler(this.AntiDeadZoneNumericUpDown_ValueChanged);
            // 
            // AntiDeadZoneLabel
            // 
            this.AntiDeadZoneLabel.AutoSize = true;
            this.AntiDeadZoneLabel.Location = new System.Drawing.Point(179, 87);
            this.AntiDeadZoneLabel.Name = "AntiDeadZoneLabel";
            this.AntiDeadZoneLabel.Size = new System.Drawing.Size(85, 13);
            this.AntiDeadZoneLabel.TabIndex = 16;
            this.AntiDeadZoneLabel.Text = "Anti-Dead Zone:";
            // 
            // DeadZoneLabel
            // 
            this.DeadZoneLabel.AutoSize = true;
            this.DeadZoneLabel.Location = new System.Drawing.Point(179, 31);
            this.DeadZoneLabel.Name = "DeadZoneLabel";
            this.DeadZoneLabel.Size = new System.Drawing.Size(64, 13);
            this.DeadZoneLabel.TabIndex = 13;
            this.DeadZoneLabel.Text = "Dead Zone:";
            // 
            // DeadZoneTrackBar
            // 
            this.DeadZoneTrackBar.AutoSize = false;
            this.DeadZoneTrackBar.Location = new System.Drawing.Point(179, 52);
            this.DeadZoneTrackBar.Maximum = 100;
            this.DeadZoneTrackBar.Name = "DeadZoneTrackBar";
            this.DeadZoneTrackBar.Size = new System.Drawing.Size(227, 32);
            this.DeadZoneTrackBar.TabIndex = 15;
            this.DeadZoneTrackBar.TickFrequency = 2;
            this.DeadZoneTrackBar.ValueChanged += new System.EventHandler(this.DeadZoneTrackBar_ValueChanged);
            // 
            // DeadZoneTextBox
            // 
            this.DeadZoneTextBox.Location = new System.Drawing.Point(412, 52);
            this.DeadZoneTextBox.Name = "DeadZoneTextBox";
            this.DeadZoneTextBox.ReadOnly = true;
            this.DeadZoneTextBox.Size = new System.Drawing.Size(43, 20);
            this.DeadZoneTextBox.TabIndex = 14;
            this.DeadZoneTextBox.TabStop = false;
            this.DeadZoneTextBox.Text = "0 % ";
            this.DeadZoneTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SensitivityTextBox
            // 
            this.SensitivityTextBox.Location = new System.Drawing.Point(412, 154);
            this.SensitivityTextBox.Name = "SensitivityTextBox";
            this.SensitivityTextBox.ReadOnly = true;
            this.SensitivityTextBox.Size = new System.Drawing.Size(43, 20);
            this.SensitivityTextBox.TabIndex = 8;
            this.SensitivityTextBox.TabStop = false;
            this.SensitivityTextBox.Text = "0 %";
            this.SensitivityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // XInputTextBox
            // 
            this.XInputTextBox.Location = new System.Drawing.Point(467, 16);
            this.XInputTextBox.Name = "XInputTextBox";
            this.XInputTextBox.ReadOnly = true;
            this.XInputTextBox.Size = new System.Drawing.Size(43, 20);
            this.XInputTextBox.TabIndex = 8;
            this.XInputTextBox.TabStop = false;
            this.XInputTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SensitivityLabel
            // 
            this.SensitivityLabel.AutoSize = true;
            this.SensitivityLabel.Location = new System.Drawing.Point(179, 138);
            this.SensitivityLabel.Name = "SensitivityLabel";
            this.SensitivityLabel.Size = new System.Drawing.Size(57, 13);
            this.SensitivityLabel.TabIndex = 7;
            this.SensitivityLabel.Text = "Sensitivity:";
            // 
            // XInputLabel
            // 
            this.XInputLabel.AutoSize = true;
            this.XInputLabel.Location = new System.Drawing.Point(415, 19);
            this.XInputLabel.Name = "XInputLabel";
            this.XInputLabel.Size = new System.Drawing.Size(41, 13);
            this.XInputLabel.TabIndex = 7;
            this.XInputLabel.Text = "XInput:";
            // 
            // AntiDeadZoneTrackBar
            // 
            this.AntiDeadZoneTrackBar.AutoSize = false;
            this.AntiDeadZoneTrackBar.Location = new System.Drawing.Point(179, 103);
            this.AntiDeadZoneTrackBar.Maximum = 100;
            this.AntiDeadZoneTrackBar.Name = "AntiDeadZoneTrackBar";
            this.AntiDeadZoneTrackBar.Size = new System.Drawing.Size(227, 32);
            this.AntiDeadZoneTrackBar.TabIndex = 15;
            this.AntiDeadZoneTrackBar.TickFrequency = 2;
            this.AntiDeadZoneTrackBar.ValueChanged += new System.EventHandler(this.AntiDeadZoneTrackBar_ValueChanged);
            // 
            // AntiDeadZoneTextBox
            // 
            this.AntiDeadZoneTextBox.Location = new System.Drawing.Point(412, 103);
            this.AntiDeadZoneTextBox.Name = "AntiDeadZoneTextBox";
            this.AntiDeadZoneTextBox.ReadOnly = true;
            this.AntiDeadZoneTextBox.Size = new System.Drawing.Size(43, 20);
            this.AntiDeadZoneTextBox.TabIndex = 14;
            this.AntiDeadZoneTextBox.TabStop = false;
            this.AntiDeadZoneTextBox.Text = "0 % ";
            this.AntiDeadZoneTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // SensitivityCheckBox
            // 
            this.SensitivityCheckBox.AutoSize = true;
            this.SensitivityCheckBox.Location = new System.Drawing.Point(461, 156);
            this.SensitivityCheckBox.Name = "SensitivityCheckBox";
            this.SensitivityCheckBox.Size = new System.Drawing.Size(53, 17);
            this.SensitivityCheckBox.TabIndex = 20;
            this.SensitivityCheckBox.Text = "Invert";
            this.SensitivityCheckBox.UseVisualStyleBackColor = true;
            this.SensitivityCheckBox.CheckedChanged += new System.EventHandler(this.SensitivityCheckBox_CheckedChanged);
            // 
            // ThumbUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainGroupBox);
            this.Name = "ThumbUserControl";
            this.Size = new System.Drawing.Size(608, 193);
            this.Load += new System.EventHandler(this.LinearUserControl_Load);
            this.EnabledChanged += new System.EventHandler(this.LinearUserControl_EnabledChanged);
            ((System.ComponentModel.ISupportInitialize)(this.SensitivityTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).EndInit();
            this.MainGroupBox.ResumeLayout(false);
            this.MainGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneTrackBar)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label DInputLabel;
		private System.Windows.Forms.TextBox DInputTextBox;
		private System.Windows.Forms.PictureBox LinearPictureBox;
		private System.Windows.Forms.GroupBox MainGroupBox;
		public System.Windows.Forms.TrackBar SensitivityTrackBar;
		private System.Windows.Forms.TextBox XInputTextBox;
		private System.Windows.Forms.Label XInputLabel;
        private System.Windows.Forms.TextBox SensitivityTextBox;
		private System.Windows.Forms.Label SensitivityLabel;
		private System.Windows.Forms.Label DeadZoneLabel;
		public System.Windows.Forms.TrackBar DeadZoneTrackBar;
        private System.Windows.Forms.TextBox DeadZoneTextBox;
		public System.Windows.Forms.NumericUpDown AntiDeadZoneNumericUpDown;
		private System.Windows.Forms.Label AntiDeadZoneLabel;
        public System.Windows.Forms.NumericUpDown DeadZoneNumericUpDown;
        public System.Windows.Forms.TrackBar AntiDeadZoneTrackBar;
        private System.Windows.Forms.TextBox AntiDeadZoneTextBox;
        private System.Windows.Forms.CheckBox SensitivityCheckBox;
	}
}
