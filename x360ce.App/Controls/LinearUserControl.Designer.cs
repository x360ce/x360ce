namespace x360ce.App.Controls
{
	partial class LinearUserControl
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
            this.ValueComboBox = new System.Windows.Forms.ComboBox();
            this.ValueTextBox = new System.Windows.Forms.TextBox();
            this.XInputTextBox = new System.Windows.Forms.TextBox();
            this.XInputLabel = new System.Windows.Forms.Label();
            this.ValueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).BeginInit();
            this.MainGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // DInputLabel
            // 
            this.DInputLabel.AutoSize = true;
            this.DInputLabel.Location = new System.Drawing.Point(6, 48);
            this.DInputLabel.Name = "DInputLabel";
            this.DInputLabel.Size = new System.Drawing.Size(42, 13);
            this.DInputLabel.TabIndex = 7;
            this.DInputLabel.Text = "DInput:";
            // 
            // LinearTrackBar
            // 
            this.LinearTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LinearTrackBar.AutoSize = false;
            this.LinearTrackBar.Location = new System.Drawing.Point(20, 155);
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
            this.DInputTextBox.Location = new System.Drawing.Point(58, 45);
            this.DInputTextBox.Name = "DInputTextBox";
            this.DInputTextBox.ReadOnly = true;
            this.DInputTextBox.Size = new System.Drawing.Size(43, 20);
            this.DInputTextBox.TabIndex = 8;
            this.DInputTextBox.TabStop = false;
            this.DInputTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // LinearPictureBox
            // 
            this.LinearPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LinearPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.LinearPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LinearPictureBox.Location = new System.Drawing.Point(117, 19);
            this.LinearPictureBox.Name = "LinearPictureBox";
            this.LinearPictureBox.Size = new System.Drawing.Size(130, 130);
            this.LinearPictureBox.TabIndex = 10;
            this.LinearPictureBox.TabStop = false;
            this.LinearPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.LinearPictureBox_Paint);
            // 
            // MainGroupBox
            // 
            this.MainGroupBox.Controls.Add(this.ValueComboBox);
            this.MainGroupBox.Controls.Add(this.ValueTextBox);
            this.MainGroupBox.Controls.Add(this.XInputTextBox);
            this.MainGroupBox.Controls.Add(this.DInputTextBox);
            this.MainGroupBox.Controls.Add(this.LinearPictureBox);
            this.MainGroupBox.Controls.Add(this.XInputLabel);
            this.MainGroupBox.Controls.Add(this.LinearTrackBar);
            this.MainGroupBox.Controls.Add(this.ValueLabel);
            this.MainGroupBox.Controls.Add(this.DInputLabel);
            this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
            this.MainGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.MainGroupBox.Name = "MainGroupBox";
            this.MainGroupBox.Size = new System.Drawing.Size(253, 200);
            this.MainGroupBox.TabIndex = 12;
            this.MainGroupBox.TabStop = false;
            this.MainGroupBox.Text = "Thumb";
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
            this.ValueComboBox.Location = new System.Drawing.Point(26, 128);
            this.ValueComboBox.Name = "ValueComboBox";
            this.ValueComboBox.Size = new System.Drawing.Size(75, 21);
            this.ValueComboBox.TabIndex = 11;
            this.ValueComboBox.SelectedIndexChanged += new System.EventHandler(this.ValueComboBox_SelectedIndexChanged);
            // 
            // ValueTextBox
            // 
            this.ValueTextBox.Location = new System.Drawing.Point(58, 19);
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
            this.XInputTextBox.Location = new System.Drawing.Point(58, 71);
            this.XInputTextBox.Name = "XInputTextBox";
            this.XInputTextBox.ReadOnly = true;
            this.XInputTextBox.Size = new System.Drawing.Size(43, 20);
            this.XInputTextBox.TabIndex = 8;
            this.XInputTextBox.TabStop = false;
            this.XInputTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // XInputLabel
            // 
            this.XInputLabel.AutoSize = true;
            this.XInputLabel.Location = new System.Drawing.Point(6, 74);
            this.XInputLabel.Name = "XInputLabel";
            this.XInputLabel.Size = new System.Drawing.Size(41, 13);
            this.XInputLabel.TabIndex = 7;
            this.XInputLabel.Text = "XInput:";
            // 
            // ValueLabel
            // 
            this.ValueLabel.AutoSize = true;
            this.ValueLabel.Location = new System.Drawing.Point(6, 22);
            this.ValueLabel.Name = "ValueLabel";
            this.ValueLabel.Size = new System.Drawing.Size(37, 13);
            this.ValueLabel.TabIndex = 7;
            this.ValueLabel.Text = "Value:";
            // 
            // LinearUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainGroupBox);
            this.Name = "LinearUserControl";
            this.Size = new System.Drawing.Size(253, 200);
            this.Load += new System.EventHandler(this.LinearUserControl_Load);
            this.EnabledChanged += new System.EventHandler(this.LinearUserControl_EnabledChanged);
            ((System.ComponentModel.ISupportInitialize)(this.LinearTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LinearPictureBox)).EndInit();
            this.MainGroupBox.ResumeLayout(false);
            this.MainGroupBox.PerformLayout();
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
		private System.Windows.Forms.Label ValueLabel;
		private System.Windows.Forms.ComboBox ValueComboBox;
	}
}
