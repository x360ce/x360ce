namespace x360ce.App.Controls
{
	partial class MapDeviceToControllerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapDeviceToControllerForm));
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.ControllersPanel = new x360ce.App.Controls.UserDevicesUserControl();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.DevicesTabPage = new System.Windows.Forms.TabPage();
			this.MainTabControl.SuspendLayout();
			this.DevicesTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(810, 415);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(132, 23);
			this.OkButton.TabIndex = 24;
			this.OkButton.Text = "Add Selected Device";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(948, 415);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 25;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// ControllersPanel
			// 
			this.ControllersPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControllersPanel.IsVisibleIsHiddenColumn = false;
			this.ControllersPanel.Location = new System.Drawing.Point(0, 0);
			this.ControllersPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ControllersPanel.Name = "ControllersPanel";
			this.ControllersPanel.Padding = new System.Windows.Forms.Padding(3);
			this.ControllersPanel.Size = new System.Drawing.Size(995, 292);
			this.ControllersPanel.TabIndex = 26;
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.DevicesTabPage);
			this.MainTabControl.Location = new System.Drawing.Point(12, 70);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(1011, 339);
			this.MainTabControl.TabIndex = 27;
			// 
			// DevicesTabPage
			// 
			this.DevicesTabPage.Controls.Add(this.ControllersPanel);
			this.DevicesTabPage.Location = new System.Drawing.Point(8, 39);
			this.DevicesTabPage.Name = "DevicesTabPage";
			this.DevicesTabPage.Size = new System.Drawing.Size(995, 292);
			this.DevicesTabPage.TabIndex = 0;
			this.DevicesTabPage.Text = "Direct Input Devices";
			this.DevicesTabPage.UseVisualStyleBackColor = true;
			// 
			// MapDeviceToControllerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1035, 450);
			this.Controls.Add(this.MainTabControl);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MapDeviceToControllerForm";
			this.Text = "X360CE - Map Device To Controller";
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.Controls.SetChildIndex(this.MainTabControl, 0);
			this.MainTabControl.ResumeLayout(false);
			this.DevicesTabPage.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		public UserDevicesUserControl ControllersPanel;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage DevicesTabPage;
	}
}
