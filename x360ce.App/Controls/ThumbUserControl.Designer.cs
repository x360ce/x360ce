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
			this.LinearTrackBar = new System.Windows.Forms.TrackBar();
			this.DInputTextBox = new System.Windows.Forms.TextBox();
			this.LinearPictureBox = new System.Windows.Forms.PictureBox();
			this.MainGroupBox = new System.Windows.Forms.GroupBox();
			this.AntiDeadZoneComboBox = new System.Windows.Forms.ComboBox();
			this.AntiDeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AntiDeadZoneLabel = new System.Windows.Forms.Label();
			this.DeadZoneLabel = new System.Windows.Forms.Label();
			this.DeadZoneTrackBar = new System.Windows.Forms.TrackBar();
			this.DeadZoneTextBox = new System.Windows.Forms.TextBox();
			this.ValueComboBox = new System.Windows.Forms.ComboBox();
			this.ValueTextBox = new System.Windows.Forms.TextBox();
			this.XInputTextBox = new System.Windows.Forms.TextBox();
			this.SensitivityLabel = new System.Windows.Forms.Label();
			this.XInputLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).BeginInit();
			this.MainGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).BeginInit();
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
			// LinearTrackBar
			// 
			this.LinearTrackBar.AutoSize = false;
			this.LinearTrackBar.Location = new System.Drawing.Point(297, 159);
			this.LinearTrackBar.Maximum = 100;
			this.LinearTrackBar.Minimum = -100;
			this.LinearTrackBar.Name = "LinearTrackBar";
			this.LinearTrackBar.Size = new System.Drawing.Size(227, 32);
			this.LinearTrackBar.TabIndex = 9;
			this.LinearTrackBar.TickFrequency = 2;
			this.LinearTrackBar.ValueChanged += new System.EventHandler(this.LinearTrackBar_ValueChanged);
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
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneComboBox);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneNumericUpDown);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneLabel);
			this.MainGroupBox.Controls.Add(this.DeadZoneLabel);
			this.MainGroupBox.Controls.Add(this.DeadZoneTrackBar);
			this.MainGroupBox.Controls.Add(this.DeadZoneTextBox);
			this.MainGroupBox.Controls.Add(this.ValueComboBox);
			this.MainGroupBox.Controls.Add(this.ValueTextBox);
			this.MainGroupBox.Controls.Add(this.XInputTextBox);
			this.MainGroupBox.Controls.Add(this.DInputTextBox);
			this.MainGroupBox.Controls.Add(this.LinearPictureBox);
			this.MainGroupBox.Controls.Add(this.SensitivityLabel);
			this.MainGroupBox.Controls.Add(this.XInputLabel);
			this.MainGroupBox.Controls.Add(this.LinearTrackBar);
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
			// AntiDeadZoneComboBox
			// 
			this.AntiDeadZoneComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.AntiDeadZoneComboBox.FormattingEnabled = true;
			this.AntiDeadZoneComboBox.Items.AddRange(new object[] {
            "Disabled",
            "Enabled, 10%",
            "Enabled, 20%",
            "Enabled, 30%",
            "Enabled, 40%",
            "Enabled, 50%",
            "Enabled, 60%",
            "Enabled, 70%",
            "Enabled, 80% (Recommended)",
            "Enabled, 90%",
            "Enabled, 100%"});
			this.AntiDeadZoneComboBox.Location = new System.Drawing.Point(192, 115);
			this.AntiDeadZoneComboBox.Name = "AntiDeadZoneComboBox";
			this.AntiDeadZoneComboBox.Size = new System.Drawing.Size(195, 21);
			this.AntiDeadZoneComboBox.TabIndex = 17;
			this.AntiDeadZoneComboBox.SelectedIndexChanged += new System.EventHandler(this.AntiDeadZoneComboBox_SelectedIndexChanged);
			// 
			// AntiDeadZoneNumericUpDown
			// 
			this.AntiDeadZoneNumericUpDown.Location = new System.Drawing.Point(393, 115);
			this.AntiDeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
			this.AntiDeadZoneNumericUpDown.Name = "AntiDeadZoneNumericUpDown";
			this.AntiDeadZoneNumericUpDown.Size = new System.Drawing.Size(75, 20);
			this.AntiDeadZoneNumericUpDown.TabIndex = 18;
			// 
			// AntiDeadZoneLabel
			// 
			this.AntiDeadZoneLabel.AutoSize = true;
			this.AntiDeadZoneLabel.Location = new System.Drawing.Point(189, 99);
			this.AntiDeadZoneLabel.Name = "AntiDeadZoneLabel";
			this.AntiDeadZoneLabel.Size = new System.Drawing.Size(85, 13);
			this.AntiDeadZoneLabel.TabIndex = 16;
			this.AntiDeadZoneLabel.Text = "Anti-Dead Zone:";
			// 
			// DeadZoneLabel
			// 
			this.DeadZoneLabel.AutoSize = true;
			this.DeadZoneLabel.Location = new System.Drawing.Point(189, 46);
			this.DeadZoneLabel.Name = "DeadZoneLabel";
			this.DeadZoneLabel.Size = new System.Drawing.Size(61, 13);
			this.DeadZoneLabel.TabIndex = 13;
			this.DeadZoneLabel.Text = "Dead Zone";
			// 
			// DeadZoneTrackBar
			// 
			this.DeadZoneTrackBar.AutoSize = false;
			this.DeadZoneTrackBar.Location = new System.Drawing.Point(192, 62);
			this.DeadZoneTrackBar.Maximum = 100;
			this.DeadZoneTrackBar.Name = "DeadZoneTrackBar";
			this.DeadZoneTrackBar.Size = new System.Drawing.Size(227, 32);
			this.DeadZoneTrackBar.TabIndex = 15;
			this.DeadZoneTrackBar.TickFrequency = 2;
			this.DeadZoneTrackBar.ValueChanged += new System.EventHandler(this.DeadZoneTrackBar_ValueChanged);
			// 
			// DeadZoneTextBox
			// 
			this.DeadZoneTextBox.Location = new System.Drawing.Point(425, 62);
			this.DeadZoneTextBox.Name = "DeadZoneTextBox";
			this.DeadZoneTextBox.ReadOnly = true;
			this.DeadZoneTextBox.Size = new System.Drawing.Size(43, 20);
			this.DeadZoneTextBox.TabIndex = 14;
			this.DeadZoneTextBox.TabStop = false;
			this.DeadZoneTextBox.Text = "0 % ";
			this.DeadZoneTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// ValueComboBox
			// 
			this.ValueComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ValueComboBox.FormattingEnabled = true;
			this.ValueComboBox.Items.AddRange(new object[] {
            "-100 %",
            "-80 %",
            "-60 %",
            "-40 %",
            "-20 %",
            "Disabled",
            " 20%",
            " 40%",
            " 60%",
            " 80%",
            " 100%"});
			this.ValueComboBox.Location = new System.Drawing.Point(192, 159);
			this.ValueComboBox.Name = "ValueComboBox";
			this.ValueComboBox.Size = new System.Drawing.Size(75, 21);
			this.ValueComboBox.TabIndex = 11;
			this.ValueComboBox.SelectedIndexChanged += new System.EventHandler(this.ValueComboBox_SelectedIndexChanged);
			// 
			// ValueTextBox
			// 
			this.ValueTextBox.Location = new System.Drawing.Point(530, 159);
			this.ValueTextBox.Name = "ValueTextBox";
			this.ValueTextBox.ReadOnly = true;
			this.ValueTextBox.Size = new System.Drawing.Size(43, 20);
			this.ValueTextBox.TabIndex = 8;
			this.ValueTextBox.TabStop = false;
			this.ValueTextBox.Text = "0 %";
			this.ValueTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
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
			this.SensitivityLabel.Location = new System.Drawing.Point(193, 139);
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
			// ThumbUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainGroupBox);
			this.Name = "ThumbUserControl";
			this.Size = new System.Drawing.Size(608, 193);
			this.Load += new System.EventHandler(this.LinearUserControl_Load);
			this.EnabledChanged += new System.EventHandler(this.LinearUserControl_EnabledChanged);
			((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).EndInit();
			this.MainGroupBox.ResumeLayout(false);
			this.MainGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label DInputLabel;
		private System.Windows.Forms.TextBox DInputTextBox;
		private System.Windows.Forms.PictureBox LinearPictureBox;
		private System.Windows.Forms.GroupBox MainGroupBox;
		public System.Windows.Forms.TrackBar LinearTrackBar;
		private System.Windows.Forms.TextBox XInputTextBox;
		private System.Windows.Forms.Label XInputLabel;
		private System.Windows.Forms.TextBox ValueTextBox;
		private System.Windows.Forms.ComboBox ValueComboBox;
		private System.Windows.Forms.Label SensitivityLabel;
		private System.Windows.Forms.Label DeadZoneLabel;
		public System.Windows.Forms.TrackBar DeadZoneTrackBar;
		private System.Windows.Forms.TextBox DeadZoneTextBox;
		private System.Windows.Forms.ComboBox AntiDeadZoneComboBox;
		public System.Windows.Forms.NumericUpDown AntiDeadZoneNumericUpDown;
		private System.Windows.Forms.Label AntiDeadZoneLabel;
	}
}
