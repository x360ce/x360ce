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
			this.MainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.EnableCheckBox = new System.Windows.Forms.CheckBox();
			this.CpuPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.TestButton = new System.Windows.Forms.Button();
			this.CpuLabel = new System.Windows.Forms.Label();
			this.CpuTextBox = new System.Windows.Forms.TextBox();
			this.GetDInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.GetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.SetXInputStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.UpdateInterfaceCheckBox = new System.Windows.Forms.CheckBox();
			this.TestCheckIssuesCheckBox = new System.Windows.Forms.CheckBox();
			this.ThrowExceptionButton = new System.Windows.Forms.Button();
			this.CleanupDevicesButton = new System.Windows.Forms.Button();
			this.ShowNavImagesButton = new System.Windows.Forms.Button();
			this.LogTextBox = new System.Windows.Forms.TextBox();
			this.MainGroupBox.SuspendLayout();
			this.MainLayoutPanel.SuspendLayout();
			this.CpuPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// MainGroupBox
			//
			this.MainGroupBox.AutoSize = true;
			this.MainGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.MainGroupBox.Controls.Add(this.MainLayoutPanel);
			this.MainGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainGroupBox.Location = new System.Drawing.Point(0, 0);
			this.MainGroupBox.Margin = new System.Windows.Forms.Padding(0);
			this.MainGroupBox.Name = "MainGroupBox";
			this.MainGroupBox.Size = new System.Drawing.Size(250, 203);
			this.MainGroupBox.TabIndex = 0;
			this.MainGroupBox.TabStop = false;
			this.MainGroupBox.Text = "Performance Test";
			//
			// MainLayoutPanel
			//
			this.MainLayoutPanel.AutoSize = true;
			this.MainLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.MainLayoutPanel.ColumnCount = 2;
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.Controls.Add(this.EnableCheckBox, 0, 0);
			this.MainLayoutPanel.Controls.Add(this.CpuPanel, 1, 0);
			this.MainLayoutPanel.Controls.Add(this.GetDInputStatesCheckBox, 0, 1);
			this.MainLayoutPanel.Controls.Add(this.GetXInputStatesCheckBox, 1, 1);
			this.MainLayoutPanel.Controls.Add(this.SetXInputStatesCheckBox, 0, 2);
			this.MainLayoutPanel.Controls.Add(this.UpdateInterfaceCheckBox, 1, 2);
			this.MainLayoutPanel.Controls.Add(this.TestCheckIssuesCheckBox, 0, 3);
			this.MainLayoutPanel.Controls.Add(this.ThrowExceptionButton, 0, 4);
			this.MainLayoutPanel.Controls.Add(this.CleanupDevicesButton, 1, 4);
			this.MainLayoutPanel.Controls.Add(this.ShowNavImagesButton, 0, 5);
			this.MainLayoutPanel.Controls.Add(this.LogTextBox, 0, 6);
			this.MainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainLayoutPanel.Location = new System.Drawing.Point(3, 16);
			this.MainLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.MainLayoutPanel.Name = "MainLayoutPanel";
			this.MainLayoutPanel.RowCount = 7;
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.Size = new System.Drawing.Size(244, 184);
			this.MainLayoutPanel.TabIndex = 0;
			//
			// EnableCheckBox
			//
			this.EnableCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.EnableCheckBox.AutoSize = true;
			this.EnableCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.EnableCheckBox.Name = "EnableCheckBox";
			this.EnableCheckBox.Size = new System.Drawing.Size(59, 17);
			this.EnableCheckBox.TabIndex = 0;
			this.EnableCheckBox.Text = "Enable";
			this.EnableCheckBox.UseVisualStyleBackColor = true;
			//
			// CpuPanel
			//
			this.CpuPanel.AutoSize = true;
			this.CpuPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CpuPanel.Controls.Add(this.TestButton);
			this.CpuPanel.Controls.Add(this.CpuLabel);
			this.CpuPanel.Controls.Add(this.CpuTextBox);
			this.CpuPanel.Margin = new System.Windows.Forms.Padding(0);
			this.CpuPanel.Name = "CpuPanel";
			this.CpuPanel.Size = new System.Drawing.Size(150, 31);
			this.CpuPanel.TabIndex = 1;
			this.CpuPanel.WrapContents = false;
			//
			// TestButton
			//
			this.TestButton.Margin = new System.Windows.Forms.Padding(4);
			this.TestButton.Name = "TestButton";
			this.TestButton.Size = new System.Drawing.Size(25, 23);
			this.TestButton.TabIndex = 0;
			this.TestButton.Text = "T";
			this.TestButton.UseVisualStyleBackColor = true;
			this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
			//
			// CpuLabel
			//
			this.CpuLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CpuLabel.AutoSize = true;
			this.CpuLabel.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);
			this.CpuLabel.Name = "CpuLabel";
			this.CpuLabel.Size = new System.Drawing.Size(32, 13);
			this.CpuLabel.TabIndex = 1;
			this.CpuLabel.Text = "CPU";
			//
			// CpuTextBox
			//
			this.CpuTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CpuTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.CpuTextBox.Name = "CpuTextBox";
			this.CpuTextBox.ReadOnly = true;
			this.CpuTextBox.Size = new System.Drawing.Size(71, 20);
			this.CpuTextBox.TabIndex = 2;
			this.CpuTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// GetDInputStatesCheckBox
			//
			this.GetDInputStatesCheckBox.AutoSize = true;
			this.GetDInputStatesCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.GetDInputStatesCheckBox.Name = "GetDInputStatesCheckBox";
			this.GetDInputStatesCheckBox.Size = new System.Drawing.Size(111, 17);
			this.GetDInputStatesCheckBox.TabIndex = 2;
			this.GetDInputStatesCheckBox.Text = "Get DInput States";
			this.GetDInputStatesCheckBox.UseVisualStyleBackColor = true;
			//
			// GetXInputStatesCheckBox
			//
			this.GetXInputStatesCheckBox.AutoSize = true;
			this.GetXInputStatesCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.GetXInputStatesCheckBox.Name = "GetXInputStatesCheckBox";
			this.GetXInputStatesCheckBox.Size = new System.Drawing.Size(118, 17);
			this.GetXInputStatesCheckBox.TabIndex = 3;
			this.GetXInputStatesCheckBox.Text = "Show XInput States";
			this.GetXInputStatesCheckBox.UseVisualStyleBackColor = true;
			//
			// SetXInputStatesCheckBox
			//
			this.SetXInputStatesCheckBox.AutoSize = true;
			this.SetXInputStatesCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.SetXInputStatesCheckBox.Name = "SetXInputStatesCheckBox";
			this.SetXInputStatesCheckBox.Size = new System.Drawing.Size(109, 17);
			this.SetXInputStatesCheckBox.TabIndex = 4;
			this.SetXInputStatesCheckBox.Text = "Set XInput States";
			this.SetXInputStatesCheckBox.UseVisualStyleBackColor = true;
			//
			// UpdateInterfaceCheckBox
			//
			this.UpdateInterfaceCheckBox.AutoSize = true;
			this.UpdateInterfaceCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.UpdateInterfaceCheckBox.Name = "UpdateInterfaceCheckBox";
			this.UpdateInterfaceCheckBox.Size = new System.Drawing.Size(106, 17);
			this.UpdateInterfaceCheckBox.TabIndex = 5;
			this.UpdateInterfaceCheckBox.Text = "Update Interface";
			this.UpdateInterfaceCheckBox.UseVisualStyleBackColor = true;
			//
			// TestCheckIssuesCheckBox
			//
			this.TestCheckIssuesCheckBox.AutoSize = true;
			this.TestCheckIssuesCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.TestCheckIssuesCheckBox.Name = "TestCheckIssuesCheckBox";
			this.TestCheckIssuesCheckBox.Size = new System.Drawing.Size(90, 17);
			this.TestCheckIssuesCheckBox.TabIndex = 6;
			this.TestCheckIssuesCheckBox.Text = "Check Issues";
			this.TestCheckIssuesCheckBox.UseVisualStyleBackColor = true;
			//
			// ThrowExceptionButton
			//
			this.ThrowExceptionButton.AutoSize = true;
			this.ThrowExceptionButton.Margin = new System.Windows.Forms.Padding(4);
			this.ThrowExceptionButton.Name = "ThrowExceptionButton";
			this.ThrowExceptionButton.Size = new System.Drawing.Size(106, 23);
			this.ThrowExceptionButton.TabIndex = 7;
			this.ThrowExceptionButton.Text = "Throw Exception";
			this.ThrowExceptionButton.UseVisualStyleBackColor = true;
			this.ThrowExceptionButton.Click += new System.EventHandler(this.ThrowExceptionButton_Click);
			//
			// CleanupDevicesButton
			//
			this.CleanupDevicesButton.AutoSize = true;
			this.CleanupDevicesButton.Margin = new System.Windows.Forms.Padding(4);
			this.CleanupDevicesButton.Name = "CleanupDevicesButton";
			this.CleanupDevicesButton.Size = new System.Drawing.Size(106, 23);
			this.CleanupDevicesButton.TabIndex = 8;
			this.CleanupDevicesButton.Text = "Cleanup Devices";
			this.CleanupDevicesButton.UseVisualStyleBackColor = true;
			this.CleanupDevicesButton.Click += new System.EventHandler(this.CleanupDevicesButton_Click);
			//
			// ShowNavImagesButton
			//
			this.ShowNavImagesButton.AutoSize = true;
			this.ShowNavImagesButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ShowNavImagesButton.Margin = new System.Windows.Forms.Padding(4);
			this.ShowNavImagesButton.Name = "ShowNavImagesButton";
			this.ShowNavImagesButton.TabIndex = 9;
			this.ShowNavImagesButton.Text = "Show All Pad Images";
			this.ShowNavImagesButton.UseVisualStyleBackColor = true;
			this.ShowNavImagesButton.Click += new System.EventHandler(this.ShowNavImagesButton_Click);
			//
			// LogTextBox
			//
			this.MainLayoutPanel.SetColumnSpan(this.LogTextBox, 2);
			this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LogTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.ReadOnly = true;
			this.LogTextBox.Size = new System.Drawing.Size(236, 20);
			this.LogTextBox.TabIndex = 10;
			//
			// DebugUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.Controls.Add(this.MainGroupBox);
			this.Name = "DebugUserControl";
			this.Size = new System.Drawing.Size(250, 203);
			this.MainGroupBox.ResumeLayout(false);
			this.MainGroupBox.PerformLayout();
			this.MainLayoutPanel.ResumeLayout(false);
			this.MainLayoutPanel.PerformLayout();
			this.CpuPanel.ResumeLayout(false);
			this.CpuPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox MainGroupBox;
		private System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel CpuPanel;
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
		private System.Windows.Forms.Button ShowNavImagesButton;
	}
}
