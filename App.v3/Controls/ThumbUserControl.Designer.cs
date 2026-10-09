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
			this.MainPictureBox = new System.Windows.Forms.PictureBox();
			this.MainGroupBox = new System.Windows.Forms.GroupBox();
			this.SensitivityCheckBox = new System.Windows.Forms.CheckBox();
			this.DeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.SensitivityNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AntiDeadZoneNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.AntiDeadZoneLabel = new System.Windows.Forms.Label();
			this.DeadZoneLabel = new System.Windows.Forms.Label();
			this.AntiDeadZoneTrackBar = new System.Windows.Forms.TrackBar();
			this.DeadZoneTrackBar = new System.Windows.Forms.TrackBar();
			this.AntiDeadZoneTextBox = new System.Windows.Forms.TextBox();
			this.DeadZoneTextBox = new System.Windows.Forms.TextBox();
			this.SensitivityTextBox = new System.Windows.Forms.TextBox();
			this.SensitivityLabel = new System.Windows.Forms.Label();
			this.XInputLabel = new System.Windows.Forms.Label();
			this.XInputValueLabel = new System.Windows.Forms.Label();
			this.DInputValueLabel = new System.Windows.Forms.Label();
			this.PresetMenuStrip = new System.Windows.Forms.MenuStrip();
			this.ApplyPresetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_5_100_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_0_100_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_0_80_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_0_60_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_0_40_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.P_0_20_0_MenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
			this.MainGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).BeginInit();
			this.PresetMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// DInputLabel
			// 
			this.DInputLabel.AutoSize = true;
			this.DInputLabel.Location = new System.Drawing.Point(6, 20);
			this.DInputLabel.Name = "DInputLabel";
			this.DInputLabel.Size = new System.Drawing.Size(21, 13);
			this.DInputLabel.TabIndex = 7;
			this.DInputLabel.Text = "DI:";
			// 
			// SensitivityTrackBar
			// 
			this.SensitivityTrackBar.AutoSize = false;
			this.SensitivityTrackBar.Location = new System.Drawing.Point(162, 154);
			this.SensitivityTrackBar.Maximum = 100;
			this.SensitivityTrackBar.Name = "SensitivityTrackBar";
			this.SensitivityTrackBar.Size = new System.Drawing.Size(227, 32);
			this.SensitivityTrackBar.TabIndex = 9;
			this.SensitivityTrackBar.TickFrequency = 2;
			this.SensitivityTrackBar.ValueChanged += new System.EventHandler(this.SensitivityTrackBar_ValueChanged);
			// 
			// MainPictureBox
			// 
			this.MainPictureBox.BackColor = System.Drawing.Color.White;
			this.MainPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.MainPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MainPictureBox.Location = new System.Drawing.Point(6, 36);
			this.MainPictureBox.Name = "MainPictureBox";
			this.MainPictureBox.Size = new System.Drawing.Size(150, 150);
			this.MainPictureBox.TabIndex = 10;
			this.MainPictureBox.TabStop = false;
			this.MainPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.LinearPictureBox_Paint);
			// 
			// MainGroupBox
			// 
			this.MainGroupBox.Controls.Add(this.SensitivityCheckBox);
			this.MainGroupBox.Controls.Add(this.DeadZoneNumericUpDown);
			this.MainGroupBox.Controls.Add(this.SensitivityNumericUpDown);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneNumericUpDown);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneLabel);
			this.MainGroupBox.Controls.Add(this.DeadZoneLabel);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneTrackBar);
			this.MainGroupBox.Controls.Add(this.DeadZoneTrackBar);
			this.MainGroupBox.Controls.Add(this.AntiDeadZoneTextBox);
			this.MainGroupBox.Controls.Add(this.DeadZoneTextBox);
			this.MainGroupBox.Controls.Add(this.SensitivityTextBox);
			this.MainGroupBox.Controls.Add(this.MainPictureBox);
			this.MainGroupBox.Controls.Add(this.SensitivityLabel);
			this.MainGroupBox.Controls.Add(this.XInputLabel);
			this.MainGroupBox.Controls.Add(this.SensitivityTrackBar);
			this.MainGroupBox.Controls.Add(this.XInputValueLabel);
			this.MainGroupBox.Controls.Add(this.DInputValueLabel);
			this.MainGroupBox.Controls.Add(this.DInputLabel);
			this.MainGroupBox.Controls.Add(this.PresetMenuStrip);
			this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
			this.MainGroupBox.Margin = new System.Windows.Forms.Padding(0);
			this.MainGroupBox.Name = "MainGroupBox";
			this.MainGroupBox.Size = new System.Drawing.Size(578, 193);
			this.MainGroupBox.TabIndex = 12;
			this.MainGroupBox.TabStop = false;
			this.MainGroupBox.Text = "Thumb";
			// 
			// SensitivityCheckBox
			// 
			this.SensitivityCheckBox.AutoSize = true;
			this.SensitivityCheckBox.Location = new System.Drawing.Point(443, 156);
			this.SensitivityCheckBox.Name = "SensitivityCheckBox";
			this.SensitivityCheckBox.Size = new System.Drawing.Size(53, 17);
			this.SensitivityCheckBox.TabIndex = 20;
			this.SensitivityCheckBox.Text = "Invert";
			this.SensitivityCheckBox.UseVisualStyleBackColor = true;
			this.SensitivityCheckBox.CheckedChanged += new System.EventHandler(this.SensitivityCheckBox_CheckedChanged);
			// 
			// DeadZoneNumericUpDown
			// 
			this.DeadZoneNumericUpDown.Location = new System.Drawing.Point(444, 103);
			this.DeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
			this.DeadZoneNumericUpDown.Name = "DeadZoneNumericUpDown";
			this.DeadZoneNumericUpDown.Size = new System.Drawing.Size(52, 20);
			this.DeadZoneNumericUpDown.TabIndex = 19;
			this.DeadZoneNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// SensitivityNumericUpDown
			// 
			this.SensitivityNumericUpDown.Location = new System.Drawing.Point(502, 155);
			this.SensitivityNumericUpDown.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
			this.SensitivityNumericUpDown.Name = "SensitivityNumericUpDown";
			this.SensitivityNumericUpDown.Size = new System.Drawing.Size(52, 20);
			this.SensitivityNumericUpDown.TabIndex = 18;
			this.SensitivityNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.SensitivityNumericUpDown.Visible = false;
			this.SensitivityNumericUpDown.ValueChanged += new System.EventHandler(this.SensitivityNumericUpDown_ValueChanged);
			// 
			// AntiDeadZoneNumericUpDown
			// 
			this.AntiDeadZoneNumericUpDown.Location = new System.Drawing.Point(444, 52);
			this.AntiDeadZoneNumericUpDown.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
			this.AntiDeadZoneNumericUpDown.Name = "AntiDeadZoneNumericUpDown";
			this.AntiDeadZoneNumericUpDown.Size = new System.Drawing.Size(52, 20);
			this.AntiDeadZoneNumericUpDown.TabIndex = 18;
			this.AntiDeadZoneNumericUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// AntiDeadZoneLabel
			// 
			this.AntiDeadZoneLabel.AutoSize = true;
			this.AntiDeadZoneLabel.Location = new System.Drawing.Point(159, 36);
			this.AntiDeadZoneLabel.Name = "AntiDeadZoneLabel";
			this.AntiDeadZoneLabel.Size = new System.Drawing.Size(85, 13);
			this.AntiDeadZoneLabel.TabIndex = 16;
			this.AntiDeadZoneLabel.Text = "Anti-Dead Zone:";
			// 
			// DeadZoneLabel
			// 
			this.DeadZoneLabel.AutoSize = true;
			this.DeadZoneLabel.Location = new System.Drawing.Point(159, 87);
			this.DeadZoneLabel.Name = "DeadZoneLabel";
			this.DeadZoneLabel.Size = new System.Drawing.Size(64, 13);
			this.DeadZoneLabel.TabIndex = 13;
			this.DeadZoneLabel.Text = "Dead Zone:";
			// 
			// AntiDeadZoneTrackBar
			// 
			this.AntiDeadZoneTrackBar.AutoSize = false;
			this.AntiDeadZoneTrackBar.Location = new System.Drawing.Point(162, 52);
			this.AntiDeadZoneTrackBar.Maximum = 100;
			this.AntiDeadZoneTrackBar.Name = "AntiDeadZoneTrackBar";
			this.AntiDeadZoneTrackBar.Size = new System.Drawing.Size(227, 32);
			this.AntiDeadZoneTrackBar.TabIndex = 15;
			this.AntiDeadZoneTrackBar.TickFrequency = 2;
			// 
			// DeadZoneTrackBar
			// 
			this.DeadZoneTrackBar.AutoSize = false;
			this.DeadZoneTrackBar.Location = new System.Drawing.Point(162, 103);
			this.DeadZoneTrackBar.Maximum = 100;
			this.DeadZoneTrackBar.Name = "DeadZoneTrackBar";
			this.DeadZoneTrackBar.Size = new System.Drawing.Size(227, 32);
			this.DeadZoneTrackBar.TabIndex = 15;
			this.DeadZoneTrackBar.TickFrequency = 2;
			// 
			// AntiDeadZoneTextBox
			// 
			this.AntiDeadZoneTextBox.Location = new System.Drawing.Point(395, 52);
			this.AntiDeadZoneTextBox.Name = "AntiDeadZoneTextBox";
			this.AntiDeadZoneTextBox.ReadOnly = true;
			this.AntiDeadZoneTextBox.Size = new System.Drawing.Size(43, 20);
			this.AntiDeadZoneTextBox.TabIndex = 14;
			this.AntiDeadZoneTextBox.TabStop = false;
			this.AntiDeadZoneTextBox.Text = "0 % ";
			this.AntiDeadZoneTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// DeadZoneTextBox
			// 
			this.DeadZoneTextBox.Location = new System.Drawing.Point(395, 103);
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
			this.SensitivityTextBox.Location = new System.Drawing.Point(395, 154);
			this.SensitivityTextBox.Name = "SensitivityTextBox";
			this.SensitivityTextBox.ReadOnly = true;
			this.SensitivityTextBox.Size = new System.Drawing.Size(43, 20);
			this.SensitivityTextBox.TabIndex = 8;
			this.SensitivityTextBox.TabStop = false;
			this.SensitivityTextBox.Text = "0 %";
			this.SensitivityTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// SensitivityLabel
			// 
			this.SensitivityLabel.AutoSize = true;
			this.SensitivityLabel.Location = new System.Drawing.Point(159, 138);
			this.SensitivityLabel.Name = "SensitivityLabel";
			this.SensitivityLabel.Size = new System.Drawing.Size(57, 13);
			this.SensitivityLabel.TabIndex = 7;
			this.SensitivityLabel.Text = "Sensitivity:";
			// 
			// XInputLabel
			// 
			this.XInputLabel.AutoSize = true;
			this.XInputLabel.Location = new System.Drawing.Point(81, 20);
			this.XInputLabel.Name = "XInputLabel";
			this.XInputLabel.Size = new System.Drawing.Size(20, 13);
			this.XInputLabel.TabIndex = 7;
			this.XInputLabel.Text = "XI:";
			// 
			// XInputValueLabel
			// 
			this.XInputValueLabel.ForeColor = System.Drawing.Color.Blue;
			this.XInputValueLabel.Location = new System.Drawing.Point(107, 20);
			this.XInputValueLabel.Name = "XInputValueLabel";
			this.XInputValueLabel.Size = new System.Drawing.Size(42, 13);
			this.XInputValueLabel.TabIndex = 7;
			this.XInputValueLabel.Text = "0";
			this.XInputValueLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// DInputValueLabel
			// 
			this.DInputValueLabel.ForeColor = System.Drawing.Color.Green;
			this.DInputValueLabel.Location = new System.Drawing.Point(33, 20);
			this.DInputValueLabel.Name = "DInputValueLabel";
			this.DInputValueLabel.Size = new System.Drawing.Size(42, 13);
			this.DInputValueLabel.TabIndex = 7;
			this.DInputValueLabel.Text = "0";
			this.DInputValueLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// PresetMenuStrip
			// 
			this.PresetMenuStrip.AutoSize = false;
			this.PresetMenuStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.PresetMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ApplyPresetMenuItem});
			this.PresetMenuStrip.Location = new System.Drawing.Point(395, 16);
			this.PresetMenuStrip.Name = "PresetMenuStrip";
			this.PresetMenuStrip.Padding = new System.Windows.Forms.Padding(0);
			this.PresetMenuStrip.Size = new System.Drawing.Size(160, 20);
			this.PresetMenuStrip.TabIndex = 22;
			this.PresetMenuStrip.Text = "ApplyPresetsMenuStrip";
			// 
			// ApplyPresetMenuItem
			// 
			this.ApplyPresetMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.P_5_100_0_MenuItem,
            this.P_0_100_0_MenuItem,
            this.P_0_80_0_MenuItem,
            this.P_0_60_0_MenuItem,
            this.P_0_40_0_MenuItem,
            this.P_0_20_0_MenuItem});
			this.ApplyPresetMenuItem.Name = "ApplyPresetMenuItem";
			this.ApplyPresetMenuItem.Size = new System.Drawing.Size(156, 20);
			this.ApplyPresetMenuItem.Text = "Click Here to Apply Preset";
			// 
			// P_5_100_0_MenuItem
			// 
			this.P_5_100_0_MenuItem.Name = "P_5_100_0_MenuItem";
			this.P_5_100_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_5_100_0_MenuItem.Text = "5% DeadZone, 100% Controller Anti-DeadZone";
			this.P_5_100_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// P_0_100_0_MenuItem
			// 
			this.P_0_100_0_MenuItem.Name = "P_0_100_0_MenuItem";
			this.P_0_100_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_0_100_0_MenuItem.Text = "100% Controller Anti-DeadZone";
			this.P_0_100_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// P_0_80_0_MenuItem
			// 
			this.P_0_80_0_MenuItem.Name = "P_0_80_0_MenuItem";
			this.P_0_80_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_0_80_0_MenuItem.Text = "80% Controller Anti-DeadZone";
			this.P_0_80_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// P_0_60_0_MenuItem
			// 
			this.P_0_60_0_MenuItem.Name = "P_0_60_0_MenuItem";
			this.P_0_60_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_0_60_0_MenuItem.Text = "60% Controller Anti-DeadZone";
			this.P_0_60_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// P_0_40_0_MenuItem
			// 
			this.P_0_40_0_MenuItem.Name = "P_0_40_0_MenuItem";
			this.P_0_40_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_0_40_0_MenuItem.Text = "40% Controller Anti-DeadZone";
			this.P_0_40_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// P_0_20_0_MenuItem
			// 
			this.P_0_20_0_MenuItem.Name = "P_0_20_0_MenuItem";
			this.P_0_20_0_MenuItem.Size = new System.Drawing.Size(321, 22);
			this.P_0_20_0_MenuItem.Text = "20% Controller Anti-DeadZone";
			this.P_0_20_0_MenuItem.Click += new System.EventHandler(this.P_X_Y_Z_MenuItem_Click);
			// 
			// ThumbUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainGroupBox);
			this.Name = "ThumbUserControl";
			this.Size = new System.Drawing.Size(578, 193);
			this.Load += new System.EventHandler(this.LinearUserControl_Load);
			this.EnabledChanged += new System.EventHandler(this.ThumbUserControl_EnabledChanged);
			((System.ComponentModel.ISupportInitialize)(this.SensitivityTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
			this.MainGroupBox.ResumeLayout(false);
			this.MainGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneNumericUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.AntiDeadZoneTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DeadZoneTrackBar)).EndInit();
			this.PresetMenuStrip.ResumeLayout(false);
			this.PresetMenuStrip.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.Label DInputLabel;
		private System.Windows.Forms.PictureBox MainPictureBox;
		private System.Windows.Forms.GroupBox MainGroupBox;
        public System.Windows.Forms.TrackBar SensitivityTrackBar;
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
        public System.Windows.Forms.NumericUpDown SensitivityNumericUpDown;
        private System.Windows.Forms.Label XInputValueLabel;
        private System.Windows.Forms.Label DInputValueLabel;
        private System.Windows.Forms.MenuStrip PresetMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem ApplyPresetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_5_100_0_MenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_0_100_0_MenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_0_80_0_MenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_0_60_0_MenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_0_40_0_MenuItem;
        private System.Windows.Forms.ToolStripMenuItem P_0_20_0_MenuItem;
	}
}
