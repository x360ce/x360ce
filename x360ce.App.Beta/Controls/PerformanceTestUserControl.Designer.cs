namespace x360ce.App.Controls
{
	partial class PerformanceTestUserControl
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
			this.MainGroupBox = new System.Windows.Forms.GroupBox();
			this.UpdateInterfaceCheckBox = new System.Windows.Forms.CheckBox();
			this.EnableCheckBox = new System.Windows.Forms.CheckBox();
			this.GetDInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.SetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.GetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.CpuTextBox = new System.Windows.Forms.TextBox();
			this.MainGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainGroupBox
			// 
			this.MainGroupBox.Controls.Add(this.CpuTextBox);
			this.MainGroupBox.Controls.Add(this.EnableCheckBox);
			this.MainGroupBox.Controls.Add(this.GetXInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.SetXInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.GetDInputStatesCheckBox);
			this.MainGroupBox.Controls.Add(this.UpdateInterfaceCheckBox);
			this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
			this.MainGroupBox.Name = "MainGroupBox";
			this.MainGroupBox.Size = new System.Drawing.Size(235, 94);
			this.MainGroupBox.TabIndex = 0;
			this.MainGroupBox.TabStop = false;
			this.MainGroupBox.Text = "Performance Test";
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
			// CpuTextBox
			// 
			this.CpuTextBox.Location = new System.Drawing.Point(123, 16);
			this.CpuTextBox.Name = "CpuTextBox";
			this.CpuTextBox.ReadOnly = true;
			this.CpuTextBox.Size = new System.Drawing.Size(106, 20);
			this.CpuTextBox.TabIndex = 2;
			// 
			// PerformanceTestUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainGroupBox);
			this.Name = "PerformanceTestUserControl";
			this.Size = new System.Drawing.Size(235, 94);
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
	}
}
