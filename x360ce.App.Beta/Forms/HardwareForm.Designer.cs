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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.HardwarePanel = new JocysCom.ClassLibrary.IO.HardwareControl();
			this.SuspendLayout();
			// 
			// HardwarePanel
			// 
			this.HardwarePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HardwarePanel.Location = new System.Drawing.Point(0, 0);
			this.HardwarePanel.Name = "HardwarePanel";
			this.HardwarePanel.Size = new System.Drawing.Size(704, 441);
			this.HardwarePanel.TabIndex = 3;
			// 
			// HardwareForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(704, 441);
			this.Controls.Add(this.HardwarePanel);
			this.Name = "HardwareForm";
			this.Text = "Hardware";
			this.ResumeLayout(false);

		}

		#endregion

		private JocysCom.ClassLibrary.IO.HardwareControl HardwarePanel;
	}
}