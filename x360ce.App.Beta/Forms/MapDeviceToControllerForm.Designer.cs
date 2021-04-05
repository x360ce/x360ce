namespace x360ce.App.Controls
{
	partial class MapDeviceToControllerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region ■ Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapDeviceToControllerForm));
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.DevicesTabPage = new System.Windows.Forms.TabPage();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.UserDevicesElementHost = new System.Windows.Forms.Integration.ElementHost();
			this.MainTabControl.SuspendLayout();
			this.DevicesTabPage.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.AutoSize = true;
			this.OkButton.Location = new System.Drawing.Point(706, 3);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(222, 35);
			this.OkButton.TabIndex = 24;
			this.OkButton.Text = "Add Selected Device";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.AutoSize = true;
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(934, 3);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(89, 35);
			this.CloseButton.TabIndex = 25;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.DevicesTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Location = new System.Drawing.Point(3, 3);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(1029, 339);
			this.MainTabControl.TabIndex = 27;
			// 
			// DevicesTabPage
			// 
			this.DevicesTabPage.BackColor = System.Drawing.SystemColors.Control;
			this.DevicesTabPage.Controls.Add(this.UserDevicesElementHost);
			this.DevicesTabPage.Location = new System.Drawing.Point(8, 39);
			this.DevicesTabPage.Name = "DevicesTabPage";
			this.DevicesTabPage.Size = new System.Drawing.Size(1013, 292);
			this.DevicesTabPage.TabIndex = 0;
			this.DevicesTabPage.Text = "Direct Input Devices";
			// 
			// panel1
			// 
			this.panel1.AutoSize = true;
			this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel1.Controls.Add(this.CloseButton);
			this.panel1.Controls.Add(this.OkButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 409);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1035, 41);
			this.panel1.TabIndex = 28;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.MainTabControl);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 64);
			this.panel2.Name = "panel2";
			this.panel2.Padding = new System.Windows.Forms.Padding(3);
			this.panel2.Size = new System.Drawing.Size(1035, 345);
			this.panel2.TabIndex = 29;
			// 
			// UserDevicesElementHost
			// 
			this.UserDevicesElementHost.Dock = System.Windows.Forms.DockStyle.Fill;
			this.UserDevicesElementHost.Location = new System.Drawing.Point(0, 0);
			this.UserDevicesElementHost.Name = "UserDevicesElementHost";
			this.UserDevicesElementHost.Size = new System.Drawing.Size(1013, 292);
			this.UserDevicesElementHost.TabIndex = 0;
			this.UserDevicesElementHost.Child = null;
			// 
			// MapDeviceToControllerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1035, 450);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MapDeviceToControllerForm";
			this.Text = "X360CE - Map Device To Controller";
			this.Controls.SetChildIndex(this.panel1, 0);
			this.Controls.SetChildIndex(this.panel2, 0);
			this.MainTabControl.ResumeLayout(false);
			this.DevicesTabPage.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage DevicesTabPage;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Integration.ElementHost UserDevicesElementHost;
	}
}
