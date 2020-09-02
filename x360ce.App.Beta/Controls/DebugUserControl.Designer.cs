namespace x360ce.App.Controls
{
	partial class DebugUserControl
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
			this.MainGroupBox = new System.Windows.Forms.GroupBox();
			this.LogTextBox = new System.Windows.Forms.TextBox();
			this.CleanupDevicesButton = new System.Windows.Forms.Button();
			this.TestButton = new System.Windows.Forms.Button();
			this.CpuLabel = new System.Windows.Forms.Label();
			this.CpuTextBox = new System.Windows.Forms.TextBox();
			this.EnableCheckBox = new System.Windows.Forms.CheckBox();
			this.GetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.TestCheckIssuesCheckBox = new System.Windows.Forms.CheckBox();
			this.SetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.GetDInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.UpdateInterfaceCheckBox = new System.Windows.Forms.CheckBox();
			this.ThrowExceptionButton = new System.Windows.Forms.Button();
			this.MainGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainGroupBox
			// 
			this.MainGroupBox.Controls.Add(this.LogTextBox);
			this.MainGroupBox.Controls.Add(this.ThrowExceptionButton);
			this.MainGroupBox.Controls.Add(this.CleanupDevicesButton);
			this.MainGroupBox.Controls.Add(this.TestButton);
			this.MainGroupBox.Controls.Add(this.CpuLabel);
			this.MainGroupBox.Controls.Add(this.CpuTextBox);
			this.MainGroupBox.Controls.Add(this.EnableCheckBox);
			this.MainGroupBox.Controls.Add(this.GetXInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.TestCheckIssuesCheckBox);
			this.MainGroupBox.Controls.Add(this.SetXInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.GetDInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.UpdateInterfaceCheckBox);
			this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
			this.MainGroupBox.Name = "MainGroupBox";
			this.MainGroupBox.Size = new System.Drawing.Size(235, 168);
			this.MainGroupBox.TabIndex = 0;
			this.MainGroupBox.TabStop = false;
			this.MainGroupBox.Text = "Performance Test";
			// 
			// LogTextBox
			// 
			this.LogTextBox.Location = new System.Drawing.Point(6, 140);
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.ReadOnly = true;
			this.LogTextBox.Size = new System.Drawing.Size(223, 20);
			this.LogTextBox.TabIndex = 5;
			// 
			// CleanupDevicesButton
			// 
			this.CleanupDevicesButton.Location = new System.Drawing.Point(123, 111);
			this.CleanupDevicesButton.Name = "CleanupDevicesButton";
			this.CleanupDevicesButton.Size = new System.Drawing.Size(106, 23);
			this.CleanupDevicesButton.TabIndex = 4;
			this.CleanupDevicesButton.Text = "Cleanup Devices";
			this.CleanupDevicesButton.UseVisualStyleBackColor = true;
			this.CleanupDevicesButton.Click += new System.EventHandler(this.CleanupDevicesButton_Click);
			// 
			// TestButton
			// 
			this.TestButton.Location = new System.Drawing.Point(92, 14);
			this.TestButton.Name = "TestButton";
			this.TestButton.Size = new System.Drawing.Size(25, 23);
			this.TestButton.TabIndex = 4;
			this.TestButton.Text = "T";
			this.TestButton.UseVisualStyleBackColor = true;
			this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
			// 
			// CpuLabel
			// 
			this.CpuLabel.AutoSize = true;
			this.CpuLabel.Location = new System.Drawing.Point(120, 19);
			this.CpuLabel.Name = "CpuLabel";
			this.CpuLabel.Size = new System.Drawing.Size(32, 13);
			this.CpuLabel.TabIndex = 3;
			this.CpuLabel.Text = "CPU:";
			// 
			// CpuTextBox
			// 
			this.CpuTextBox.Location = new System.Drawing.Point(158, 16);
			this.CpuTextBox.Name = "CpuTextBox";
			this.CpuTextBox.ReadOnly = true;
			this.CpuTextBox.Size = new System.Drawing.Size(71, 20);
			this.CpuTextBox.TabIndex = 2;
			this.CpuTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// EnableCheckBox
			// 
			this.EnableCheckBox.AutoSize = true;
			this.EnableCheckBox.Location = new System.Drawing.Point(6, 19);
			this.EnableCheckBox.Name = "EnableCheckBox";
			this.EnableCheckBox.Size = new System.Drawing.Size(59, 17);
			this.EnableCheckBox.TabIndex = 1;
			this.EnableCheckBox.Text = "Enable";
			this.EnableCheckBox.UseVisualStyleBackColor = true;
			// 
			// GetXInputStatesCheckBox
			// 
			this.GetXInputStatesCheckBox.AutoSize = true;
			this.GetXInputStatesCheckBox.Location = new System.Drawing.Point(123, 42);
			this.GetXInputStatesCheckBox.Name = "GetXInputStatesCheckBox";
			this.GetXInputStatesCheckBox.Size = new System.Drawing.Size(110, 17);
			this.GetXInputStatesCheckBox.TabIndex = 1;
			this.GetXInputStatesCheckBox.Text = "Get XInput States";
			this.GetXInputStatesCheckBox.UseVisualStyleBackColor = true;
			// 
			// TestCheckIssuesCheckBox
			// 
			this.TestCheckIssuesCheckBox.AutoSize = true;
			this.TestCheckIssuesCheckBox.Location = new System.Drawing.Point(6, 88);
			this.TestCheckIssuesCheckBox.Name = "TestCheckIssuesCheckBox";
			this.TestCheckIssuesCheckBox.Size = new System.Drawing.Size(90, 17);
			this.TestCheckIssuesCheckBox.TabIndex = 1;
			this.TestCheckIssuesCheckBox.Text = "Check Issues";
			this.TestCheckIssuesCheckBox.UseVisualStyleBackColor = true;
			// 
			// SetXInputStatesCheckBox
			// 
			this.SetXInputStatesCheckBox.AutoSize = true;
			this.SetXInputStatesCheckBox.Location = new System.Drawing.Point(6, 65);
			this.SetXInputStatesCheckBox.Name = "SetXInputStatesCheckBox";
			this.SetXInputStatesCheckBox.Size = new System.Drawing.Size(109, 17);
			this.SetXInputStatesCheckBox.TabIndex = 1;
			this.SetXInputStatesCheckBox.Text = "Set XInput States";
			this.SetXInputStatesCheckBox.UseVisualStyleBackColor = true;
			// 
			// GetDInputStatesCheckBox
			// 
			this.GetDInputStatesCheckBox.AutoSize = true;
			this.GetDInputStatesCheckBox.Location = new System.Drawing.Point(6, 42);
			this.GetDInputStatesCheckBox.Name = "GetDInputStatesCheckBox";
			this.GetDInputStatesCheckBox.Size = new System.Drawing.Size(111, 17);
			this.GetDInputStatesCheckBox.TabIndex = 1;
			this.GetDInputStatesCheckBox.Text = "Get DInput States";
			this.GetDInputStatesCheckBox.UseVisualStyleBackColor = true;
			// 
			// UpdateInterfaceCheckBox
			// 
			this.UpdateInterfaceCheckBox.AutoSize = true;
			this.UpdateInterfaceCheckBox.Location = new System.Drawing.Point(123, 65);
			this.UpdateInterfaceCheckBox.Name = "UpdateInterfaceCheckBox";
			this.UpdateInterfaceCheckBox.Size = new System.Drawing.Size(106, 17);
			this.UpdateInterfaceCheckBox.TabIndex = 1;
			this.UpdateInterfaceCheckBox.Text = "Update Interface";
			this.UpdateInterfaceCheckBox.UseVisualStyleBackColor = true;
			// 
			// ThrowExceptionButton
			// 
			this.ThrowExceptionButton.Location = new System.Drawing.Point(6, 111);
			this.ThrowExceptionButton.Name = "ThrowExceptionButton";
			this.ThrowExceptionButton.Size = new System.Drawing.Size(106, 23);
			this.ThrowExceptionButton.TabIndex = 4;
			this.ThrowExceptionButton.Text = "Throw Exception";
			this.ThrowExceptionButton.UseVisualStyleBackColor = true;
			this.ThrowExceptionButton.Click += new System.EventHandler(this.ThrowExceptionButton_Click);
			// 
			// PerformanceTestUserControl
			// 
			this.Controls.Add(this.MainGroupBox);
			this.Name = "PerformanceTestUserControl";
			this.Size = new System.Drawing.Size(235, 168);
			this.MainGroupBox.ResumeLayout(false);
			this.MainGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox MainGroupBox;
		public System.Windows.Forms.CheckBox EnableCheckBox;
		public System.Windows.Forms.CheckBox GetXInputStatesCheckBox;
		public System.Windows.Forms.CheckBox SetXInputStatesCheckBox;
		public System.Windows.Forms.CheckBox GetDInputStatesCheckBox;
		public System.Windows.Forms.CheckBox UpdateInterfaceCheckBox;
		private System.Windows.Forms.TextBox CpuTextBox;
		private System.Windows.Forms.Label CpuLabel;
		private System.Windows.Forms.Button TestButton;
		public System.Windows.Forms.CheckBox TestCheckIssuesCheckBox;
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.Button CleanupDevicesButton;
		private System.Windows.Forms.Button ThrowExceptionButton;
	}
}
