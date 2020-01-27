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
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.ControllersPanel = new x360ce.App.Controls.UserDevicesUserControl();
			this.SuspendLayout();
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(456, 407);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(75, 23);
			this.OkButton.TabIndex = 24;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(537, 407);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 25;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			// 
			// ControllersPanel
			// 
			this.ControllersPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControllersPanel.Location = new System.Drawing.Point(9, 67);
			this.ControllersPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ControllersPanel.Name = "ControllersPanel";
			this.ControllersPanel.Padding = new System.Windows.Forms.Padding(3);
			this.ControllersPanel.Size = new System.Drawing.Size(606, 337);
			this.ControllersPanel.TabIndex = 26;
			// 
			// MapDeviceToControllerForm
			// 
			this.ClientSize = new System.Drawing.Size(624, 442);
			this.Controls.Add(this.ControllersPanel);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Name = "MapDeviceToControllerForm";
			this.Text = "X360CE - Map Device To Controller";
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.Controls.SetChildIndex(this.ControllersPanel, 0);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		public UserDevicesUserControl ControllersPanel;
	}
}
