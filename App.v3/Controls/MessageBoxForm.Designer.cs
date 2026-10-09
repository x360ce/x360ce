namespace x360ce.App.Controls
{
	partial class MessageBoxForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

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
		void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.Button3 = new System.Windows.Forms.Button();
			this.Button2 = new System.Windows.Forms.Button();
			this.Button1 = new System.Windows.Forms.Button();
			this.IconPictureBox = new System.Windows.Forms.PictureBox();
			this.TextLabel = new System.Windows.Forms.Label();
			this.CopyContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panel1 = new System.Windows.Forms.Panel();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.MainLinkLabel = new System.Windows.Forms.LinkLabel();
			((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).BeginInit();
			this.CopyContextMenuStrip.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// Button3
			// 
			this.Button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Button3.Location = new System.Drawing.Point(253, 70);
			this.Button3.Name = "Button3";
			this.Button3.Size = new System.Drawing.Size(75, 25);
			this.Button3.TabIndex = 9;
			this.Button3.Text = "Button3";
			// 
			// Button2
			// 
			this.Button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Button2.Location = new System.Drawing.Point(172, 70);
			this.Button2.Name = "Button2";
			this.Button2.Size = new System.Drawing.Size(75, 25);
			this.Button2.TabIndex = 8;
			this.Button2.Text = "Button2";
			// 
			// Button1
			// 
			this.Button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Button1.Location = new System.Drawing.Point(91, 70);
			this.Button1.Name = "Button1";
			this.Button1.Size = new System.Drawing.Size(75, 25);
			this.Button1.TabIndex = 6;
			this.Button1.Text = "Button1";
			// 
			// IconPictureBox
			// 
			this.IconPictureBox.BackColor = System.Drawing.Color.Transparent;
			this.IconPictureBox.Location = new System.Drawing.Point(12, 12);
			this.IconPictureBox.Name = "IconPictureBox";
			this.IconPictureBox.Size = new System.Drawing.Size(32, 32);
			this.IconPictureBox.TabIndex = 7;
			this.IconPictureBox.TabStop = false;
			// 
			// TextLabel
			// 
			this.TextLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.TextLabel.ContextMenuStrip = this.CopyContextMenuStrip;
			this.TextLabel.Location = new System.Drawing.Point(50, 12);
			this.TextLabel.MaximumSize = new System.Drawing.Size(640, 480);
			this.TextLabel.Name = "TextLabel";
			this.TextLabel.Size = new System.Drawing.Size(278, 27);
			this.TextLabel.TabIndex = 5;
			this.TextLabel.Text = "[TextLabel]";
			// 
			// CopyContextMenuStrip
			// 
			this.CopyContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
			this.CopyContextMenuStrip.Name = "CopyContextMenuStrip";
			this.CopyContextMenuStrip.Size = new System.Drawing.Size(103, 26);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BackColor = System.Drawing.SystemColors.Window;
			this.panel1.ContextMenuStrip = this.CopyContextMenuStrip;
			this.panel1.Controls.Add(this.textBox1);
			this.panel1.Controls.Add(this.IconPictureBox);
			this.panel1.Controls.Add(this.TextLabel);
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(340, 57);
			this.panel1.TabIndex = 11;
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(50, 12);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBox1.Size = new System.Drawing.Size(151, 32);
			this.textBox1.TabIndex = 12;
			// 
			// MainLinkLabel
			// 
			this.MainLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MainLinkLabel.AutoSize = true;
			this.MainLinkLabel.Location = new System.Drawing.Point(12, 76);
			this.MainLinkLabel.Name = "MainLinkLabel";
			this.MainLinkLabel.Size = new System.Drawing.Size(76, 13);
			this.MainLinkLabel.TabIndex = 12;
			this.MainLinkLabel.TabStop = true;
			this.MainLinkLabel.Text = "MainLinkLabel";
			this.MainLinkLabel.Visible = false;
			this.MainLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.MainLinkLabel_LinkClicked);
			// 
			// MessageBoxForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(340, 107);
			this.Controls.Add(this.MainLinkLabel);
			this.Controls.Add(this.Button2);
			this.Controls.Add(this.Button3);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.Button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(346, 135);
			this.Name = "MessageBoxForm";
			this.Text = "MessageBoxForm";
			((System.ComponentModel.ISupportInitialize)(this.IconPictureBox)).EndInit();
			this.CopyContextMenuStrip.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		internal System.Windows.Forms.Button Button3;
		internal System.Windows.Forms.Button Button2;
		internal System.Windows.Forms.Button Button1;
		internal System.Windows.Forms.PictureBox IconPictureBox;
		internal System.Windows.Forms.Label TextLabel;
		System.Windows.Forms.ContextMenuStrip CopyContextMenuStrip;
		System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		System.Windows.Forms.Panel panel1;
		System.Windows.Forms.TextBox textBox1;
		public System.Windows.Forms.LinkLabel MainLinkLabel;


	}
}