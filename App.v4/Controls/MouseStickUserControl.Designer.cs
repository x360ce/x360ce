namespace x360ce.App.Controls
{
	partial class MouseStickUserControl
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
			this.StickGroupBox = new System.Windows.Forms.GroupBox();
			this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.StyleLabel = new System.Windows.Forms.Label();
			this.StyleComboBox = new System.Windows.Forms.ComboBox();
			this.SensitivityXLabel = new System.Windows.Forms.Label();
			this.SensitivityXTrackBar = new System.Windows.Forms.TrackBar();
			this.SensitivityXTextBox = new System.Windows.Forms.TextBox();
			this.SensitivityYLabel = new System.Windows.Forms.Label();
			this.SensitivityYTrackBar = new System.Windows.Forms.TrackBar();
			this.SensitivityYTextBox = new System.Windows.Forms.TextBox();
			this.SmoothingLabel = new System.Windows.Forms.Label();
			this.SmoothingTrackBar = new System.Windows.Forms.TrackBar();
			this.SmoothingTextBox = new System.Windows.Forms.TextBox();
			this.ReturnLabel = new System.Windows.Forms.Label();
			this.ReturnTrackBar = new System.Windows.Forms.TrackBar();
			this.ReturnTextBox = new System.Windows.Forms.TextBox();
			this.ActiveLabel = new System.Windows.Forms.Label();
			this.ActiveComboBox = new System.Windows.Forms.ComboBox();
			this.ButtonLabel = new System.Windows.Forms.Label();
			this.ButtonComboBox = new System.Windows.Forms.ComboBox();
			this.StickGroupBox.SuspendLayout();
			this.LayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityXTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityYTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SmoothingTrackBar)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ReturnTrackBar)).BeginInit();
			this.SuspendLayout();
			//
			// StickGroupBox
			//
			this.StickGroupBox.Controls.Add(this.LayoutPanel);
			this.StickGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.StickGroupBox.Location = new System.Drawing.Point(0, 0);
			this.StickGroupBox.Name = "StickGroupBox";
			this.StickGroupBox.Size = new System.Drawing.Size(360, 260);
			this.StickGroupBox.TabIndex = 0;
			this.StickGroupBox.TabStop = false;
			this.StickGroupBox.Text = "Stick";
			//
			// LayoutPanel
			//
			this.LayoutPanel.AutoSize = true;
			this.LayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.LayoutPanel.ColumnCount = 3;
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.LayoutPanel.Controls.Add(this.StyleLabel, 0, 0);
			this.LayoutPanel.Controls.Add(this.StyleComboBox, 1, 0);
			this.LayoutPanel.Controls.Add(this.SensitivityXLabel, 0, 1);
			this.LayoutPanel.Controls.Add(this.SensitivityXTrackBar, 1, 1);
			this.LayoutPanel.Controls.Add(this.SensitivityXTextBox, 2, 1);
			this.LayoutPanel.Controls.Add(this.SensitivityYLabel, 0, 2);
			this.LayoutPanel.Controls.Add(this.SensitivityYTrackBar, 1, 2);
			this.LayoutPanel.Controls.Add(this.SensitivityYTextBox, 2, 2);
			this.LayoutPanel.Controls.Add(this.SmoothingLabel, 0, 3);
			this.LayoutPanel.Controls.Add(this.SmoothingTrackBar, 1, 3);
			this.LayoutPanel.Controls.Add(this.SmoothingTextBox, 2, 3);
			this.LayoutPanel.Controls.Add(this.ReturnLabel, 0, 4);
			this.LayoutPanel.Controls.Add(this.ReturnTrackBar, 1, 4);
			this.LayoutPanel.Controls.Add(this.ReturnTextBox, 2, 4);
			this.LayoutPanel.Controls.Add(this.ActiveLabel, 0, 5);
			this.LayoutPanel.Controls.Add(this.ActiveComboBox, 1, 5);
			this.LayoutPanel.Controls.Add(this.ButtonLabel, 0, 6);
			this.LayoutPanel.Controls.Add(this.ButtonComboBox, 1, 6);
			this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.LayoutPanel.Location = new System.Drawing.Point(3, 16);
			this.LayoutPanel.Name = "LayoutPanel";
			this.LayoutPanel.RowCount = 7;
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.Size = new System.Drawing.Size(354, 230);
			this.LayoutPanel.TabIndex = 0;
			//
			// StyleLabel
			//
			this.StyleLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.StyleLabel.AutoSize = true;
			this.StyleLabel.Name = "StyleLabel";
			this.StyleLabel.TabIndex = 0;
			this.StyleLabel.Text = "Style:";
			//
			// StyleComboBox
			//
			this.StyleComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.LayoutPanel.SetColumnSpan(this.StyleComboBox, 2);
			this.StyleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.StyleComboBox.Name = "StyleComboBox";
			this.StyleComboBox.TabIndex = 1;
			//
			// SensitivityXLabel
			//
			this.SensitivityXLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SensitivityXLabel.AutoSize = true;
			this.SensitivityXLabel.Name = "SensitivityXLabel";
			this.SensitivityXLabel.TabIndex = 2;
			this.SensitivityXLabel.Text = "Sensitivity across:";
			//
			// SensitivityXTrackBar
			//
			this.SensitivityXTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.SensitivityXTrackBar.LargeChange = 10;
			this.SensitivityXTrackBar.Maximum = 400;
			this.SensitivityXTrackBar.Minimum = 1;
			this.SensitivityXTrackBar.Name = "SensitivityXTrackBar";
			this.SensitivityXTrackBar.TabIndex = 3;
			this.SensitivityXTrackBar.TickFrequency = 25;
			this.SensitivityXTrackBar.Value = 100;
			//
			// SensitivityXTextBox
			//
			this.SensitivityXTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SensitivityXTextBox.Name = "SensitivityXTextBox";
			this.SensitivityXTextBox.ReadOnly = true;
			this.SensitivityXTextBox.Size = new System.Drawing.Size(52, 20);
			this.SensitivityXTextBox.TabStop = false;
			this.SensitivityXTextBox.Text = "100 %";
			this.SensitivityXTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// SensitivityYLabel
			//
			this.SensitivityYLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SensitivityYLabel.AutoSize = true;
			this.SensitivityYLabel.Name = "SensitivityYLabel";
			this.SensitivityYLabel.TabIndex = 4;
			this.SensitivityYLabel.Text = "Sensitivity up and down:";
			//
			// SensitivityYTrackBar
			//
			this.SensitivityYTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.SensitivityYTrackBar.LargeChange = 10;
			this.SensitivityYTrackBar.Maximum = 400;
			this.SensitivityYTrackBar.Minimum = 1;
			this.SensitivityYTrackBar.Name = "SensitivityYTrackBar";
			this.SensitivityYTrackBar.TabIndex = 5;
			this.SensitivityYTrackBar.TickFrequency = 25;
			this.SensitivityYTrackBar.Value = 100;
			//
			// SensitivityYTextBox
			//
			this.SensitivityYTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SensitivityYTextBox.Name = "SensitivityYTextBox";
			this.SensitivityYTextBox.ReadOnly = true;
			this.SensitivityYTextBox.Size = new System.Drawing.Size(52, 20);
			this.SensitivityYTextBox.TabStop = false;
			this.SensitivityYTextBox.Text = "100 %";
			this.SensitivityYTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// SmoothingLabel
			//
			this.SmoothingLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SmoothingLabel.AutoSize = true;
			this.SmoothingLabel.Name = "SmoothingLabel";
			this.SmoothingLabel.TabIndex = 6;
			this.SmoothingLabel.Text = "Smoothing:";
			//
			// SmoothingTrackBar
			//
			this.SmoothingTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.SmoothingTrackBar.LargeChange = 10;
			this.SmoothingTrackBar.Maximum = 500;
			this.SmoothingTrackBar.Name = "SmoothingTrackBar";
			this.SmoothingTrackBar.TabIndex = 7;
			this.SmoothingTrackBar.TickFrequency = 25;
			this.SmoothingTrackBar.Value = 30;
			//
			// SmoothingTextBox
			//
			this.SmoothingTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SmoothingTextBox.Name = "SmoothingTextBox";
			this.SmoothingTextBox.ReadOnly = true;
			this.SmoothingTextBox.Size = new System.Drawing.Size(52, 20);
			this.SmoothingTextBox.TabStop = false;
			this.SmoothingTextBox.Text = "30 ms";
			this.SmoothingTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// ReturnLabel
			//
			this.ReturnLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ReturnLabel.AutoSize = true;
			this.ReturnLabel.Name = "ReturnLabel";
			this.ReturnLabel.TabIndex = 8;
			this.ReturnLabel.Text = "Back to centre:";
			//
			// ReturnTrackBar
			//
			this.ReturnTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ReturnTrackBar.LargeChange = 50;
			this.ReturnTrackBar.Maximum = 2000;
			this.ReturnTrackBar.Name = "ReturnTrackBar";
			this.ReturnTrackBar.SmallChange = 10;
			this.ReturnTrackBar.TabIndex = 9;
			this.ReturnTrackBar.TickFrequency = 100;
			//
			// ReturnTextBox
			//
			this.ReturnTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ReturnTextBox.Name = "ReturnTextBox";
			this.ReturnTextBox.ReadOnly = true;
			this.ReturnTextBox.Size = new System.Drawing.Size(52, 20);
			this.ReturnTextBox.TabStop = false;
			this.ReturnTextBox.Text = "0 ms";
			this.ReturnTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// ActiveLabel
			//
			this.ActiveLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ActiveLabel.AutoSize = true;
			this.ActiveLabel.Name = "ActiveLabel";
			this.ActiveLabel.TabIndex = 10;
			this.ActiveLabel.Text = "Active:";
			//
			// ActiveComboBox
			//
			this.ActiveComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.LayoutPanel.SetColumnSpan(this.ActiveComboBox, 2);
			this.ActiveComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ActiveComboBox.Name = "ActiveComboBox";
			this.ActiveComboBox.TabIndex = 11;
			//
			// ButtonLabel
			//
			this.ButtonLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ButtonLabel.AutoSize = true;
			this.ButtonLabel.Name = "ButtonLabel";
			this.ButtonLabel.TabIndex = 12;
			this.ButtonLabel.Text = "Button:";
			//
			// ButtonComboBox
			//
			this.ButtonComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.LayoutPanel.SetColumnSpan(this.ButtonComboBox, 2);
			this.ButtonComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ButtonComboBox.Name = "ButtonComboBox";
			this.ButtonComboBox.TabIndex = 13;
			//
			// MouseStickUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.StickGroupBox);
			this.Name = "MouseStickUserControl";
			this.Size = new System.Drawing.Size(360, 260);
			this.StickGroupBox.ResumeLayout(false);
			this.StickGroupBox.PerformLayout();
			this.LayoutPanel.ResumeLayout(false);
			this.LayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityXTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SensitivityYTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SmoothingTrackBar)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ReturnTrackBar)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox StickGroupBox;
		private System.Windows.Forms.TableLayoutPanel LayoutPanel;
		private System.Windows.Forms.Label StyleLabel;
		public System.Windows.Forms.ComboBox StyleComboBox;
		private System.Windows.Forms.Label SensitivityXLabel;
		public System.Windows.Forms.TrackBar SensitivityXTrackBar;
		private System.Windows.Forms.TextBox SensitivityXTextBox;
		private System.Windows.Forms.Label SensitivityYLabel;
		public System.Windows.Forms.TrackBar SensitivityYTrackBar;
		private System.Windows.Forms.TextBox SensitivityYTextBox;
		private System.Windows.Forms.Label SmoothingLabel;
		public System.Windows.Forms.TrackBar SmoothingTrackBar;
		private System.Windows.Forms.TextBox SmoothingTextBox;
		private System.Windows.Forms.Label ReturnLabel;
		public System.Windows.Forms.TrackBar ReturnTrackBar;
		private System.Windows.Forms.TextBox ReturnTextBox;
		private System.Windows.Forms.Label ActiveLabel;
		public System.Windows.Forms.ComboBox ActiveComboBox;
		private System.Windows.Forms.Label ButtonLabel;
		public System.Windows.Forms.ComboBox ButtonComboBox;
	}
}
