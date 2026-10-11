namespace x360ce.App.Forms
{
	partial class DeveloperToolsForm
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
			this.MainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.TestWindowsGroupBox = new System.Windows.Forms.GroupBox();
			this.TestWindowsLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.InformationButton = new System.Windows.Forms.Button();
			this.WarningButton = new System.Windows.Forms.Button();
			this.ErrorButton = new System.Windows.Forms.Button();
			this.QuestionButton = new System.Windows.Forms.Button();
			this.ExceptionButton = new System.Windows.Forms.Button();
			this.ErrorReportButton = new System.Windows.Forms.Button();
			this.CompressXmlResourcesButton = new System.Windows.Forms.Button();
			this.LogTextBox = new System.Windows.Forms.TextBox();
			this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.MainLayoutPanel.SuspendLayout();
			this.TestWindowsGroupBox.SuspendLayout();
			this.TestWindowsLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainLayoutPanel
			// 
			this.MainLayoutPanel.ColumnCount = 1;
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainLayoutPanel.Controls.Add(this.TestWindowsGroupBox, 0, 0);
			this.MainLayoutPanel.Controls.Add(this.CompressXmlResourcesButton, 0, 1);
			this.MainLayoutPanel.Controls.Add(this.label1, 0, 2);
			this.MainLayoutPanel.Controls.Add(this.WorkingFolderTextBox, 0, 3);
			this.MainLayoutPanel.Controls.Add(this.LogTextBox, 0, 4);
			this.MainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainLayoutPanel.Name = "MainLayoutPanel";
			this.MainLayoutPanel.Padding = new System.Windows.Forms.Padding(9);
			this.MainLayoutPanel.RowCount = 5;
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainLayoutPanel.Size = new System.Drawing.Size(515, 384);
			this.MainLayoutPanel.TabIndex = 0;
			// 
			// TestWindowsGroupBox
			// 
			this.TestWindowsGroupBox.AutoSize = true;
			this.TestWindowsGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.TestWindowsGroupBox.Controls.Add(this.TestWindowsLayoutPanel);
			this.TestWindowsGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestWindowsGroupBox.Location = new System.Drawing.Point(12, 12);
			this.TestWindowsGroupBox.Name = "TestWindowsGroupBox";
			this.TestWindowsGroupBox.Size = new System.Drawing.Size(491, 77);
			this.TestWindowsGroupBox.TabIndex = 0;
			this.TestWindowsGroupBox.TabStop = false;
			this.TestWindowsGroupBox.Text = "Test Windows";
			// 
			// TestWindowsLayoutPanel
			// 
			this.TestWindowsLayoutPanel.AutoSize = true;
			this.TestWindowsLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.TestWindowsLayoutPanel.ColumnCount = 4;
			this.TestWindowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.TestWindowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.TestWindowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.TestWindowsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.TestWindowsLayoutPanel.Controls.Add(this.InformationButton, 0, 0);
			this.TestWindowsLayoutPanel.Controls.Add(this.WarningButton, 1, 0);
			this.TestWindowsLayoutPanel.Controls.Add(this.ErrorButton, 2, 0);
			this.TestWindowsLayoutPanel.Controls.Add(this.QuestionButton, 3, 0);
			this.TestWindowsLayoutPanel.Controls.Add(this.ExceptionButton, 0, 1);
			this.TestWindowsLayoutPanel.Controls.Add(this.ErrorReportButton, 1, 1);
			this.TestWindowsLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TestWindowsLayoutPanel.Location = new System.Drawing.Point(3, 16);
			this.TestWindowsLayoutPanel.Name = "TestWindowsLayoutPanel";
			this.TestWindowsLayoutPanel.RowCount = 2;
			this.TestWindowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestWindowsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.TestWindowsLayoutPanel.Size = new System.Drawing.Size(485, 58);
			this.TestWindowsLayoutPanel.TabIndex = 0;
			// 
			// InformationButton
			// 
			this.InformationButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.InformationButton.Location = new System.Drawing.Point(3, 3);
			this.InformationButton.Name = "InformationButton";
			this.InformationButton.Size = new System.Drawing.Size(115, 23);
			this.InformationButton.TabIndex = 0;
			this.InformationButton.Text = "Information";
			this.InformationButton.UseVisualStyleBackColor = true;
			this.InformationButton.Click += new System.EventHandler(this.InformationButton_Click);
			// 
			// WarningButton
			// 
			this.WarningButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WarningButton.Location = new System.Drawing.Point(124, 3);
			this.WarningButton.Name = "WarningButton";
			this.WarningButton.Size = new System.Drawing.Size(115, 23);
			this.WarningButton.TabIndex = 1;
			this.WarningButton.Text = "Warning";
			this.WarningButton.UseVisualStyleBackColor = true;
			this.WarningButton.Click += new System.EventHandler(this.WarningButton_Click);
			// 
			// ErrorButton
			// 
			this.ErrorButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ErrorButton.Location = new System.Drawing.Point(245, 3);
			this.ErrorButton.Name = "ErrorButton";
			this.ErrorButton.Size = new System.Drawing.Size(115, 23);
			this.ErrorButton.TabIndex = 2;
			this.ErrorButton.Text = "Error";
			this.ErrorButton.UseVisualStyleBackColor = true;
			this.ErrorButton.Click += new System.EventHandler(this.ErrorButton_Click);
			// 
			// QuestionButton
			// 
			this.QuestionButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.QuestionButton.Location = new System.Drawing.Point(366, 3);
			this.QuestionButton.Name = "QuestionButton";
			this.QuestionButton.Size = new System.Drawing.Size(116, 23);
			this.QuestionButton.TabIndex = 3;
			this.QuestionButton.Text = "Question";
			this.QuestionButton.UseVisualStyleBackColor = true;
			this.QuestionButton.Click += new System.EventHandler(this.QuestionButton_Click);
			// 
			// ExceptionButton
			// 
			this.ExceptionButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ExceptionButton.Location = new System.Drawing.Point(3, 32);
			this.ExceptionButton.Name = "ExceptionButton";
			this.ExceptionButton.Size = new System.Drawing.Size(115, 23);
			this.ExceptionButton.TabIndex = 4;
			this.ExceptionButton.Text = "Exception";
			this.ExceptionButton.UseVisualStyleBackColor = true;
			this.ExceptionButton.Click += new System.EventHandler(this.ExceptionButton_Click);
			// 
			// ErrorReportButton
			// 
			this.ErrorReportButton.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ErrorReportButton.Location = new System.Drawing.Point(124, 32);
			this.ErrorReportButton.Name = "ErrorReportButton";
			this.ErrorReportButton.Size = new System.Drawing.Size(115, 23);
			this.ErrorReportButton.TabIndex = 5;
			this.ErrorReportButton.Text = "Error Report...";
			this.ErrorReportButton.UseVisualStyleBackColor = true;
			this.ErrorReportButton.Click += new System.EventHandler(this.ErrorReportButton_Click);
			// 
			// CompressXmlResourcesButton
			// 
			this.CompressXmlResourcesButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.CompressXmlResourcesButton.Location = new System.Drawing.Point(12, 66);
			this.CompressXmlResourcesButton.Name = "CompressXmlResourcesButton";
			this.CompressXmlResourcesButton.Size = new System.Drawing.Size(168, 23);
			this.CompressXmlResourcesButton.TabIndex = 1;
			this.CompressXmlResourcesButton.Text = "Compress XML Resources";
			this.CompressXmlResourcesButton.UseVisualStyleBackColor = true;
			this.CompressXmlResourcesButton.Click += new System.EventHandler(this.CompressXmlResourcesButton_Click);
			// 
			// LogTextBox
			// 
			this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LogTextBox.Location = new System.Drawing.Point(12, 137);
			this.LogTextBox.Multiline = true;
			this.LogTextBox.Name = "LogTextBox";
			this.LogTextBox.Size = new System.Drawing.Size(491, 235);
			this.LogTextBox.TabIndex = 4;
			// 
			// WorkingFolderTextBox
			// 
			this.WorkingFolderTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.WorkingFolderTextBox.Location = new System.Drawing.Point(12, 111);
			this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
			this.WorkingFolderTextBox.Size = new System.Drawing.Size(491, 20);
			this.WorkingFolderTextBox.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 95);
			this.label1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(82, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Working Folder:";
			// 
			// DeveloperToolsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(515, 384);
			this.Controls.Add(this.MainLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.Name = "DeveloperToolsForm";
			this.Text = "Developer Tools";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DeveloperToolsForm_FormClosing);
			this.Load += new System.EventHandler(this.DeveloperToolsForm_Load);
			this.MainLayoutPanel.ResumeLayout(false);
			this.MainLayoutPanel.PerformLayout();
			this.TestWindowsGroupBox.ResumeLayout(false);
			this.TestWindowsGroupBox.PerformLayout();
			this.TestWindowsLayoutPanel.ResumeLayout(false);
			this.TestWindowsLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
		private System.Windows.Forms.GroupBox TestWindowsGroupBox;
		private System.Windows.Forms.TableLayoutPanel TestWindowsLayoutPanel;
		private System.Windows.Forms.Button InformationButton;
		private System.Windows.Forms.Button WarningButton;
		private System.Windows.Forms.Button ErrorButton;
		private System.Windows.Forms.Button QuestionButton;
		private System.Windows.Forms.Button ExceptionButton;
		private System.Windows.Forms.Button ErrorReportButton;
		private System.Windows.Forms.Button CompressXmlResourcesButton;
		private System.Windows.Forms.TextBox LogTextBox;
		private System.Windows.Forms.TextBox WorkingFolderTextBox;
		private System.Windows.Forms.Label label1;
	}
}
