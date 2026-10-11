namespace x360ce.App.Controls
{
	partial class WheelChartUserControl
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
			this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ValuesFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.AngleLabel = new System.Windows.Forms.Label();
			this.AngleValueLabel = new System.Windows.Forms.Label();
			this.ForceLabel = new System.Windows.Forms.Label();
			this.ForceValueLabel = new System.Windows.Forms.Label();
			this.MainPictureBox = new System.Windows.Forms.PictureBox();
			this.LeftStopLabel = new System.Windows.Forms.Label();
			this.CentreLabel = new System.Windows.Forms.Label();
			this.RightStopLabel = new System.Windows.Forms.Label();
			this.LegendLabel = new System.Windows.Forms.Label();
			this.MainTableLayoutPanel.SuspendLayout();
			this.ValuesFlowLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
			this.SuspendLayout();
			//
			// MainTableLayoutPanel
			//
			this.MainTableLayoutPanel.ColumnCount = 3;
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.MainTableLayoutPanel.Controls.Add(this.ValuesFlowLayoutPanel, 0, 0);
			this.MainTableLayoutPanel.Controls.Add(this.MainPictureBox, 0, 1);
			this.MainTableLayoutPanel.Controls.Add(this.LeftStopLabel, 0, 2);
			this.MainTableLayoutPanel.Controls.Add(this.CentreLabel, 1, 2);
			this.MainTableLayoutPanel.Controls.Add(this.RightStopLabel, 2, 2);
			this.MainTableLayoutPanel.Controls.Add(this.LegendLabel, 0, 3);
			this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
			this.MainTableLayoutPanel.RowCount = 4;
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainTableLayoutPanel.Size = new System.Drawing.Size(600, 600);
			this.MainTableLayoutPanel.TabIndex = 0;
			//
			// ValuesFlowLayoutPanel
			//
			this.ValuesFlowLayoutPanel.AutoSize = true;
			this.ValuesFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.MainTableLayoutPanel.SetColumnSpan(this.ValuesFlowLayoutPanel, 3);
			this.ValuesFlowLayoutPanel.Controls.Add(this.AngleLabel);
			this.ValuesFlowLayoutPanel.Controls.Add(this.AngleValueLabel);
			this.ValuesFlowLayoutPanel.Controls.Add(this.ForceLabel);
			this.ValuesFlowLayoutPanel.Controls.Add(this.ForceValueLabel);
			this.ValuesFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.ValuesFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ValuesFlowLayoutPanel.Name = "ValuesFlowLayoutPanel";
			this.ValuesFlowLayoutPanel.Size = new System.Drawing.Size(201, 30);
			this.ValuesFlowLayoutPanel.TabIndex = 0;
			this.ValuesFlowLayoutPanel.WrapContents = false;
			//
			// AngleLabel
			//
			this.AngleLabel.AutoSize = true;
			this.AngleLabel.Location = new System.Drawing.Point(4, 5);
			this.AngleLabel.Margin = new System.Windows.Forms.Padding(4, 5, 0, 5);
			this.AngleLabel.Name = "AngleLabel";
			this.AngleLabel.Size = new System.Drawing.Size(55, 20);
			this.AngleLabel.TabIndex = 0;
			this.AngleLabel.Text = "Wheel:";
			//
			// AngleValueLabel
			//
			this.AngleValueLabel.AutoSize = true;
			this.AngleValueLabel.ForeColor = System.Drawing.Color.Green;
			this.AngleValueLabel.Location = new System.Drawing.Point(63, 5);
			this.AngleValueLabel.Margin = new System.Windows.Forms.Padding(4, 5, 24, 5);
			this.AngleValueLabel.Name = "AngleValueLabel";
			this.AngleValueLabel.Size = new System.Drawing.Size(15, 20);
			this.AngleValueLabel.TabIndex = 1;
			this.AngleValueLabel.Text = "-";
			//
			// ForceLabel
			//
			this.ForceLabel.AutoSize = true;
			this.ForceLabel.Location = new System.Drawing.Point(102, 5);
			this.ForceLabel.Margin = new System.Windows.Forms.Padding(4, 5, 0, 5);
			this.ForceLabel.Name = "ForceLabel";
			this.ForceLabel.Size = new System.Drawing.Size(51, 20);
			this.ForceLabel.TabIndex = 2;
			this.ForceLabel.Text = "Force:";
			//
			// ForceValueLabel
			//
			this.ForceValueLabel.AutoSize = true;
			this.ForceValueLabel.ForeColor = System.Drawing.Color.Blue;
			this.ForceValueLabel.Location = new System.Drawing.Point(157, 5);
			this.ForceValueLabel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.ForceValueLabel.Name = "ForceValueLabel";
			this.ForceValueLabel.Size = new System.Drawing.Size(15, 20);
			this.ForceValueLabel.TabIndex = 3;
			this.ForceValueLabel.Text = "-";
			//
			// MainPictureBox
			//
			this.MainPictureBox.BackColor = System.Drawing.SystemColors.Window;
			this.MainPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MainTableLayoutPanel.SetColumnSpan(this.MainPictureBox, 3);
			this.MainPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainPictureBox.Location = new System.Drawing.Point(4, 35);
			this.MainPictureBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 0);
			this.MainPictureBox.MinimumSize = new System.Drawing.Size(150, 150);
			this.MainPictureBox.Name = "MainPictureBox";
			this.MainPictureBox.Size = new System.Drawing.Size(592, 480);
			this.MainPictureBox.TabIndex = 1;
			this.MainPictureBox.TabStop = false;
			this.MainPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.MainPictureBox_Paint);
			this.MainPictureBox.Resize += new System.EventHandler(this.MainPictureBox_Resize);
			//
			// LeftStopLabel
			//
			this.LeftStopLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.LeftStopLabel.AutoSize = true;
			this.LeftStopLabel.Location = new System.Drawing.Point(4, 520);
			this.LeftStopLabel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.LeftStopLabel.Name = "LeftStopLabel";
			this.LeftStopLabel.Size = new System.Drawing.Size(48, 20);
			this.LeftStopLabel.TabIndex = 2;
			this.LeftStopLabel.Text = "-450°";
			//
			// CentreLabel
			//
			this.CentreLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.CentreLabel.AutoSize = true;
			this.CentreLabel.Location = new System.Drawing.Point(289, 520);
			this.CentreLabel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.CentreLabel.Name = "CentreLabel";
			this.CentreLabel.Size = new System.Drawing.Size(23, 20);
			this.CentreLabel.TabIndex = 3;
			this.CentreLabel.Text = "0°";
			//
			// RightStopLabel
			//
			this.RightStopLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.RightStopLabel.AutoSize = true;
			this.RightStopLabel.Location = new System.Drawing.Point(544, 520);
			this.RightStopLabel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.RightStopLabel.Name = "RightStopLabel";
			this.RightStopLabel.Size = new System.Drawing.Size(52, 20);
			this.RightStopLabel.TabIndex = 4;
			this.RightStopLabel.Text = "+450°";
			//
			// LegendLabel
			//
			this.LegendLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.LegendLabel.AutoSize = true;
			this.MainTableLayoutPanel.SetColumnSpan(this.LegendLabel, 3);
			this.LegendLabel.Location = new System.Drawing.Point(4, 550);
			this.LegendLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 5);
			this.LegendLabel.Name = "LegendLabel";
			this.LegendLabel.Size = new System.Drawing.Size(592, 40);
			this.LegendLabel.TabIndex = 5;
			this.LegendLabel.Text = "Red: the force towards the centre at each angle. Grey: the wheel\'s own friction, which Auto measures; the wheel rests where the red is inside it.";
			//
			// WheelChartUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MainTableLayoutPanel);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "WheelChartUserControl";
			this.Size = new System.Drawing.Size(600, 600);
			this.MainTableLayoutPanel.ResumeLayout(false);
			this.MainTableLayoutPanel.PerformLayout();
			this.ValuesFlowLayoutPanel.ResumeLayout(false);
			this.ValuesFlowLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel ValuesFlowLayoutPanel;
		private System.Windows.Forms.Label AngleLabel;
		private System.Windows.Forms.Label AngleValueLabel;
		private System.Windows.Forms.Label ForceLabel;
		private System.Windows.Forms.Label ForceValueLabel;
		private System.Windows.Forms.PictureBox MainPictureBox;
		private System.Windows.Forms.Label LeftStopLabel;
		private System.Windows.Forms.Label CentreLabel;
		private System.Windows.Forms.Label RightStopLabel;
		private System.Windows.Forms.Label LegendLabel;
	}
}
