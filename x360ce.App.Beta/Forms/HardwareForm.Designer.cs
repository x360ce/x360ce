namespace x360ce.App.Forms
{
	partial class HardwareForm
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

		#region ■ Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HardwareForm));
			this.HardwarePanel = new JocysCom.ClassLibrary.IO.HardwareControl();
			this.SuspendLayout();
			// 
			// HardwarePanel
			// 
			this.HardwarePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HardwarePanel.Location = new System.Drawing.Point(3, 3);
			this.HardwarePanel.Name = "HardwarePanel";
			this.HardwarePanel.Size = new System.Drawing.Size(738, 495);
			this.HardwarePanel.TabIndex = 3;
			// 
			// HardwareForm
			// 
			this.ClientSize = new System.Drawing.Size(744, 501);
			this.Controls.Add(this.HardwarePanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HardwareForm";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Text = "Hardware";
			this.ResumeLayout(false);

		}

		#endregion

		private JocysCom.ClassLibrary.IO.HardwareControl HardwarePanel;
	}
}
