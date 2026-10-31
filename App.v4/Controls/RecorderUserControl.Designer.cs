namespace x360ce.App.Controls
{
	partial class RecorderUserControl
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
			if (disposing)
				StopRecording();
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
			this.components = new System.ComponentModel.Container();
			this.RecordGroupBox = new System.Windows.Forms.GroupBox();
			this.LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.NoteLabel = new System.Windows.Forms.Label();
			this.PartsPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.SettingsCheckBox = new System.Windows.Forms.CheckBox();
			this.InputCheckBox = new System.Windows.Forms.CheckBox();
			this.OutputCheckBox = new System.Windows.Forms.CheckBox();
			this.ForcesCheckBox = new System.Windows.Forms.CheckBox();
			this.EngineCheckBox = new System.Windows.Forms.CheckBox();
			this.WindowCheckBox = new System.Windows.Forms.CheckBox();
			this.ActionsPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.RecordButton = new System.Windows.Forms.Button();
			this.OpenFolderButton = new System.Windows.Forms.Button();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.BeatTimer = new System.Windows.Forms.Timer(this.components);
			this.RecordGroupBox.SuspendLayout();
			this.LayoutPanel.SuspendLayout();
			this.PartsPanel.SuspendLayout();
			this.ActionsPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// RecordGroupBox
			//
			this.RecordGroupBox.Controls.Add(this.LayoutPanel);
			this.RecordGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RecordGroupBox.Location = new System.Drawing.Point(0, 0);
			this.RecordGroupBox.Name = "RecordGroupBox";
			this.RecordGroupBox.Size = new System.Drawing.Size(784, 120);
			this.RecordGroupBox.TabIndex = 0;
			this.RecordGroupBox.TabStop = false;
			this.RecordGroupBox.Text = "Record for a bug report";
			//
			// LayoutPanel
			//
			this.LayoutPanel.AutoSize = true;
			this.LayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.LayoutPanel.ColumnCount = 1;
			this.LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.LayoutPanel.Controls.Add(this.NoteLabel, 0, 0);
			this.LayoutPanel.Controls.Add(this.PartsPanel, 0, 1);
			this.LayoutPanel.Controls.Add(this.ActionsPanel, 0, 2);
			this.LayoutPanel.Controls.Add(this.StatusLabel, 0, 3);
			this.LayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.LayoutPanel.Location = new System.Drawing.Point(3, 16);
			this.LayoutPanel.Name = "LayoutPanel";
			this.LayoutPanel.RowCount = 4;
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.LayoutPanel.Size = new System.Drawing.Size(778, 100);
			this.LayoutPanel.TabIndex = 0;
			this.LayoutPanel.SizeChanged += new System.EventHandler(this.LayoutPanel_SizeChanged);
			//
			// NoteLabel
			//
			this.NoteLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.NoteLabel.AutoSize = true;
			this.NoteLabel.Location = new System.Drawing.Point(3, 3);
			this.NoteLabel.Margin = new System.Windows.Forms.Padding(3);
			this.NoteLabel.Name = "NoteLabel";
			this.NoteLabel.Size = new System.Drawing.Size(772, 13);
			this.NoteLabel.TabIndex = 0;
			this.NoteLabel.Text = "Records what x360ce reads and sends. Names, folders and keys are left out, so the file can be posted in a GitHub issue.";
			//
			// PartsPanel
			//
			this.PartsPanel.AutoSize = true;
			this.PartsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.PartsPanel.Controls.Add(this.SettingsCheckBox);
			this.PartsPanel.Controls.Add(this.InputCheckBox);
			this.PartsPanel.Controls.Add(this.OutputCheckBox);
			this.PartsPanel.Controls.Add(this.ForcesCheckBox);
			this.PartsPanel.Controls.Add(this.EngineCheckBox);
			this.PartsPanel.Controls.Add(this.WindowCheckBox);
			this.PartsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PartsPanel.Location = new System.Drawing.Point(0, 19);
			this.PartsPanel.Margin = new System.Windows.Forms.Padding(0);
			this.PartsPanel.Name = "PartsPanel";
			this.PartsPanel.Size = new System.Drawing.Size(778, 23);
			this.PartsPanel.TabIndex = 1;
			//
			// SettingsCheckBox
			//
			this.SettingsCheckBox.AutoSize = true;
			this.SettingsCheckBox.Location = new System.Drawing.Point(3, 3);
			this.SettingsCheckBox.Name = "SettingsCheckBox";
			this.SettingsCheckBox.Size = new System.Drawing.Size(125, 17);
			this.SettingsCheckBox.TabIndex = 0;
			this.SettingsCheckBox.Text = "Settings and devices";
			this.SettingsCheckBox.UseVisualStyleBackColor = true;
			//
			// InputCheckBox
			//
			this.InputCheckBox.AutoSize = true;
			this.InputCheckBox.Location = new System.Drawing.Point(134, 3);
			this.InputCheckBox.Name = "InputCheckBox";
			this.InputCheckBox.Size = new System.Drawing.Size(88, 17);
			this.InputCheckBox.TabIndex = 1;
			this.InputCheckBox.Text = "Device input";
			this.InputCheckBox.UseVisualStyleBackColor = true;
			//
			// OutputCheckBox
			//
			this.OutputCheckBox.AutoSize = true;
			this.OutputCheckBox.Location = new System.Drawing.Point(228, 3);
			this.OutputCheckBox.Name = "OutputCheckBox";
			this.OutputCheckBox.Size = new System.Drawing.Size(111, 17);
			this.OutputCheckBox.TabIndex = 2;
			this.OutputCheckBox.Text = "Controller output";
			this.OutputCheckBox.UseVisualStyleBackColor = true;
			//
			// ForcesCheckBox
			//
			this.ForcesCheckBox.AutoSize = true;
			this.ForcesCheckBox.Location = new System.Drawing.Point(345, 3);
			this.ForcesCheckBox.Name = "ForcesCheckBox";
			this.ForcesCheckBox.Size = new System.Drawing.Size(98, 17);
			this.ForcesCheckBox.TabIndex = 3;
			this.ForcesCheckBox.Text = "Force feedback";
			this.ForcesCheckBox.UseVisualStyleBackColor = true;
			//
			// EngineCheckBox
			//
			this.EngineCheckBox.AutoSize = true;
			this.EngineCheckBox.Location = new System.Drawing.Point(449, 3);
			this.EngineCheckBox.Name = "EngineCheckBox";
			this.EngineCheckBox.Size = new System.Drawing.Size(92, 17);
			this.EngineCheckBox.TabIndex = 4;
			this.EngineCheckBox.Text = "Engine timing";
			this.EngineCheckBox.UseVisualStyleBackColor = true;
			//
			// WindowCheckBox
			//
			this.WindowCheckBox.AutoSize = true;
			this.WindowCheckBox.Location = new System.Drawing.Point(547, 3);
			this.WindowCheckBox.Name = "WindowCheckBox";
			this.WindowCheckBox.Size = new System.Drawing.Size(116, 17);
			this.WindowCheckBox.TabIndex = 5;
			this.WindowCheckBox.Text = "Window and errors";
			this.WindowCheckBox.UseVisualStyleBackColor = true;
			//
			// ActionsPanel
			//
			this.ActionsPanel.AutoSize = true;
			this.ActionsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ActionsPanel.Controls.Add(this.RecordButton);
			this.ActionsPanel.Controls.Add(this.OpenFolderButton);
			this.ActionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ActionsPanel.Location = new System.Drawing.Point(0, 42);
			this.ActionsPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ActionsPanel.Name = "ActionsPanel";
			this.ActionsPanel.Size = new System.Drawing.Size(778, 29);
			this.ActionsPanel.TabIndex = 2;
			this.ActionsPanel.WrapContents = false;
			//
			// RecordButton
			//
			this.RecordButton.AutoSize = true;
			this.RecordButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.RecordButton.Location = new System.Drawing.Point(3, 3);
			this.RecordButton.Name = "RecordButton";
			this.RecordButton.Size = new System.Drawing.Size(103, 23);
			this.RecordButton.TabIndex = 0;
			this.RecordButton.Text = "Start Recording";
			this.RecordButton.UseVisualStyleBackColor = true;
			this.RecordButton.Click += new System.EventHandler(this.RecordButton_Click);
			//
			// OpenFolderButton
			//
			this.OpenFolderButton.AutoSize = true;
			this.OpenFolderButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OpenFolderButton.Location = new System.Drawing.Point(112, 3);
			this.OpenFolderButton.Name = "OpenFolderButton";
			this.OpenFolderButton.Size = new System.Drawing.Size(87, 23);
			this.OpenFolderButton.TabIndex = 1;
			this.OpenFolderButton.Text = "Open Folder";
			this.OpenFolderButton.UseVisualStyleBackColor = true;
			this.OpenFolderButton.Click += new System.EventHandler(this.OpenFolderButton_Click);
			//
			// StatusLabel
			//
			this.StatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.StatusLabel.AutoSize = true;
			this.StatusLabel.Location = new System.Drawing.Point(3, 74);
			this.StatusLabel.Margin = new System.Windows.Forms.Padding(3);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(772, 13);
			this.StatusLabel.TabIndex = 3;
			//
			// BeatTimer
			//
			this.BeatTimer.Interval = 250;
			this.BeatTimer.Tick += new System.EventHandler(this.BeatTimer_Tick);
			//
			// RecorderUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RecordGroupBox);
			this.Name = "RecorderUserControl";
			this.Size = new System.Drawing.Size(784, 120);
			this.RecordGroupBox.ResumeLayout(false);
			this.RecordGroupBox.PerformLayout();
			this.LayoutPanel.ResumeLayout(false);
			this.LayoutPanel.PerformLayout();
			this.PartsPanel.ResumeLayout(false);
			this.PartsPanel.PerformLayout();
			this.ActionsPanel.ResumeLayout(false);
			this.ActionsPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox RecordGroupBox;
		private System.Windows.Forms.TableLayoutPanel LayoutPanel;
		private System.Windows.Forms.Label NoteLabel;
		private System.Windows.Forms.FlowLayoutPanel PartsPanel;
		public System.Windows.Forms.CheckBox SettingsCheckBox;
		public System.Windows.Forms.CheckBox InputCheckBox;
		public System.Windows.Forms.CheckBox OutputCheckBox;
		public System.Windows.Forms.CheckBox ForcesCheckBox;
		public System.Windows.Forms.CheckBox EngineCheckBox;
		public System.Windows.Forms.CheckBox WindowCheckBox;
		private System.Windows.Forms.FlowLayoutPanel ActionsPanel;
		public System.Windows.Forms.Button RecordButton;
		private System.Windows.Forms.Button OpenFolderButton;
		public System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.Timer BeatTimer;
	}
}
