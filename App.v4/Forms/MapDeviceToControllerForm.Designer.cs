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
			this.RootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ButtonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.OkButton = new System.Windows.Forms.Button();
			this.CloseButton = new System.Windows.Forms.Button();
			this.ControllersPanel = new x360ce.App.Controls.UserDevicesUserControl();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.DevicesTabPage = new System.Windows.Forms.TabPage();
			this.RootTableLayoutPanel.SuspendLayout();
			this.ButtonsFlowLayoutPanel.SuspendLayout();
			this.MainTabControl.SuspendLayout();
			this.DevicesTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// OkButton
			// 
			this.OkButton.AutoSize = true;
			this.OkButton.Margin = new System.Windows.Forms.Padding(4);
			this.OkButton.Name = "OkButton";
			this.OkButton.TabIndex = 24;
			this.OkButton.Text = "Add Selected Device";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.AutoSize = true;
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Margin = new System.Windows.Forms.Padding(4);
			this.CloseButton.Name = "CloseButton";
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
			this.ControllersPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 0);
			this.ControllersPanel.Size = new System.Drawing.Size(592, 305);
			this.ControllersPanel.TabIndex = 26;
			// 
			// RootTableLayoutPanel
			// 
			this.RootTableLayoutPanel.ColumnCount = 1;
			this.RootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.Controls.Add(this.MainTabControl, 0, 0);
			this.RootTableLayoutPanel.Controls.Add(this.ButtonsFlowLayoutPanel, 0, 1);
			this.RootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RootTableLayoutPanel.Location = new System.Drawing.Point(0, 64);
			this.RootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.RootTableLayoutPanel.Name = "RootTableLayoutPanel";
			this.RootTableLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
			this.RootTableLayoutPanel.RowCount = 2;
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootTableLayoutPanel.Size = new System.Drawing.Size(624, 378);
			this.RootTableLayoutPanel.TabIndex = 28;
			// 
			// ButtonsFlowLayoutPanel
			// 
			this.ButtonsFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.ButtonsFlowLayoutPanel.AutoSize = true;
			this.ButtonsFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ButtonsFlowLayoutPanel.Controls.Add(this.CloseButton);
			this.ButtonsFlowLayoutPanel.Controls.Add(this.OkButton);
			this.ButtonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.ButtonsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonsFlowLayoutPanel.Name = "ButtonsFlowLayoutPanel";
			this.ButtonsFlowLayoutPanel.TabIndex = 29;
			this.ButtonsFlowLayoutPanel.WrapContents = false;
			// 
			// MainTabControl
			// 
			this.MainTabControl.Controls.Add(this.DevicesTabPage);
			this.MainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(600, 331);
			this.MainTabControl.TabIndex = 27;
			// 
			// DevicesTabPage
			// 
			this.DevicesTabPage.Controls.Add(this.ControllersPanel);
			this.DevicesTabPage.Location = new System.Drawing.Point(4, 22);
			this.DevicesTabPage.Name = "DevicesTabPage";
			this.DevicesTabPage.Size = new System.Drawing.Size(592, 305);
			this.DevicesTabPage.TabIndex = 0;
			this.DevicesTabPage.Text = "Input Devices";
			this.DevicesTabPage.UseVisualStyleBackColor = true;
			// 
			// MapDeviceToControllerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 442);
			this.Controls.Add(this.RootTableLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.Name = "MapDeviceToControllerForm";
			this.Text = "X360CE - Map Device To Controller";
			this.Controls.SetChildIndex(this.RootTableLayoutPanel, 0);
			this.RootTableLayoutPanel.ResumeLayout(false);
			this.RootTableLayoutPanel.PerformLayout();
			this.ButtonsFlowLayoutPanel.ResumeLayout(false);
			this.ButtonsFlowLayoutPanel.PerformLayout();
			this.MainTabControl.ResumeLayout(false);
			this.DevicesTabPage.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.TableLayoutPanel RootTableLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel ButtonsFlowLayoutPanel;
		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.Button CloseButton;
		public UserDevicesUserControl ControllersPanel;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage DevicesTabPage;
	}
}
