namespace JocysCom.ClassLibrary.Controls
{
	partial class ErrorReportUserControl
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
			this.RootLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ReportGroupBox = new System.Windows.Forms.GroupBox();
			this.FieldsLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ErrorsFolderLabel = new System.Windows.Forms.Label();
			this.ErrorsFolderTextBox = new System.Windows.Forms.TextBox();
			this.OpenErrorsFolderButton = new System.Windows.Forms.Button();
			this.ErrorLabel = new System.Windows.Forms.Label();
			this.ErrorComboBox = new System.Windows.Forms.ComboBox();
			this.FromEmailLabel = new System.Windows.Forms.Label();
			this.FromEmailTextBox = new System.Windows.Forms.TextBox();
			this.ToEmailLabel = new System.Windows.Forms.Label();
			this.ToEmailTextBox = new System.Windows.Forms.TextBox();
			this.OpenMailButton = new System.Windows.Forms.Button();
			this.SubjectLabel = new System.Windows.Forms.Label();
			this.SubjectTextBox = new System.Windows.Forms.TextBox();
			this.DetailsTabControl = new System.Windows.Forms.TabControl();
			this.ErrorDetailsTabPage = new System.Windows.Forms.TabPage();
			this.MainBrowser = new System.Windows.Forms.WebBrowser();
			this.ButtonsFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.CloseButton = new System.Windows.Forms.Button();
			this.ClearErrorsButton = new System.Windows.Forms.Button();
			this.SendErrorButton = new System.Windows.Forms.Button();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.RootLayoutPanel.SuspendLayout();
			this.ReportGroupBox.SuspendLayout();
			this.FieldsLayoutPanel.SuspendLayout();
			this.DetailsTabControl.SuspendLayout();
			this.ErrorDetailsTabPage.SuspendLayout();
			this.ButtonsFlowPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// RootLayoutPanel
			//
			this.RootLayoutPanel.ColumnCount = 1;
			this.RootLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootLayoutPanel.Controls.Add(this.ReportGroupBox, 0, 0);
			this.RootLayoutPanel.Controls.Add(this.DetailsTabControl, 0, 1);
			this.RootLayoutPanel.Controls.Add(this.ButtonsFlowPanel, 0, 2);
			this.RootLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RootLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.RootLayoutPanel.Name = "RootLayoutPanel";
			this.RootLayoutPanel.RowCount = 3;
			this.RootLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootLayoutPanel.Size = new System.Drawing.Size(600, 400);
			this.RootLayoutPanel.TabIndex = 0;
			//
			// ReportGroupBox
			//
			this.ReportGroupBox.AutoSize = true;
			this.ReportGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ReportGroupBox.Controls.Add(this.FieldsLayoutPanel);
			this.ReportGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ReportGroupBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 2);
			this.ReportGroupBox.Name = "ReportGroupBox";
			this.ReportGroupBox.Padding = new System.Windows.Forms.Padding(4, 2, 4, 4);
			this.ReportGroupBox.TabIndex = 0;
			this.ReportGroupBox.TabStop = false;
			this.ReportGroupBox.Text = "Report";
			//
			// FieldsLayoutPanel
			//
			this.FieldsLayoutPanel.AutoSize = true;
			this.FieldsLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.FieldsLayoutPanel.ColumnCount = 3;
			this.FieldsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.FieldsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.FieldsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.FieldsLayoutPanel.Controls.Add(this.ErrorsFolderLabel, 0, 0);
			this.FieldsLayoutPanel.Controls.Add(this.ErrorsFolderTextBox, 1, 0);
			this.FieldsLayoutPanel.Controls.Add(this.OpenErrorsFolderButton, 2, 0);
			this.FieldsLayoutPanel.Controls.Add(this.ErrorLabel, 0, 1);
			this.FieldsLayoutPanel.Controls.Add(this.ErrorComboBox, 1, 1);
			this.FieldsLayoutPanel.Controls.Add(this.FromEmailLabel, 0, 2);
			this.FieldsLayoutPanel.Controls.Add(this.FromEmailTextBox, 1, 2);
			this.FieldsLayoutPanel.Controls.Add(this.ToEmailLabel, 0, 3);
			this.FieldsLayoutPanel.Controls.Add(this.ToEmailTextBox, 1, 3);
			this.FieldsLayoutPanel.Controls.Add(this.OpenMailButton, 2, 3);
			this.FieldsLayoutPanel.Controls.Add(this.SubjectLabel, 0, 4);
			this.FieldsLayoutPanel.Controls.Add(this.SubjectTextBox, 1, 4);
			this.FieldsLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FieldsLayoutPanel.Name = "FieldsLayoutPanel";
			this.FieldsLayoutPanel.RowCount = 5;
			this.FieldsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.FieldsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.FieldsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.FieldsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.FieldsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.FieldsLayoutPanel.TabIndex = 0;
			//
			// ErrorsFolderLabel
			//
			this.ErrorsFolderLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ErrorsFolderLabel.AutoSize = true;
			this.ErrorsFolderLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.ErrorsFolderLabel.Name = "ErrorsFolderLabel";
			this.ErrorsFolderLabel.TabIndex = 0;
			this.ErrorsFolderLabel.Text = "Errors Folder";
			//
			// ErrorsFolderTextBox
			//
			this.ErrorsFolderTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ErrorsFolderTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ErrorsFolderTextBox.Name = "ErrorsFolderTextBox";
			this.ErrorsFolderTextBox.ReadOnly = true;
			this.ErrorsFolderTextBox.TabStop = false;
			this.ErrorsFolderTextBox.TabIndex = 1;
			//
			// OpenErrorsFolderButton
			//
			this.OpenErrorsFolderButton.AutoSize = true;
			this.OpenErrorsFolderButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OpenErrorsFolderButton.Name = "OpenErrorsFolderButton";
			this.OpenErrorsFolderButton.TabIndex = 2;
			this.OpenErrorsFolderButton.Text = "Open...";
			this.OpenErrorsFolderButton.UseVisualStyleBackColor = true;
			this.OpenErrorsFolderButton.Click += new System.EventHandler(this.OpenErrorsFolderButton_Click);
			//
			// ErrorLabel
			//
			this.ErrorLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ErrorLabel.AutoSize = true;
			this.ErrorLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.ErrorLabel.Name = "ErrorLabel";
			this.ErrorLabel.TabIndex = 3;
			this.ErrorLabel.Text = "Error";
			//
			// ErrorComboBox
			//
			this.ErrorComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ErrorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ErrorComboBox.FormattingEnabled = true;
			this.ErrorComboBox.Name = "ErrorComboBox";
			this.ErrorComboBox.TabIndex = 4;
			this.ErrorComboBox.SelectedIndexChanged += new System.EventHandler(this.ErrorComboBox_SelectedIndexChanged);
			//
			// FromEmailLabel
			//
			this.FromEmailLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.FromEmailLabel.AutoSize = true;
			this.FromEmailLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.FromEmailLabel.Name = "FromEmailLabel";
			this.FromEmailLabel.TabIndex = 5;
			this.FromEmailLabel.Text = "Email From (Optional)";
			//
			// FromEmailTextBox
			//
			this.FromEmailTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.FromEmailTextBox.Name = "FromEmailTextBox";
			this.FromEmailTextBox.TabIndex = 6;
			//
			// ToEmailLabel
			//
			this.ToEmailLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ToEmailLabel.AutoSize = true;
			this.ToEmailLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.ToEmailLabel.Name = "ToEmailLabel";
			this.ToEmailLabel.TabIndex = 7;
			this.ToEmailLabel.Text = "Email To";
			//
			// ToEmailTextBox
			//
			this.ToEmailTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ToEmailTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ToEmailTextBox.Name = "ToEmailTextBox";
			this.ToEmailTextBox.ReadOnly = true;
			this.ToEmailTextBox.TabStop = false;
			this.ToEmailTextBox.TabIndex = 8;
			//
			// OpenMailButton
			//
			this.OpenMailButton.AutoSize = true;
			this.OpenMailButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.OpenMailButton.Name = "OpenMailButton";
			this.OpenMailButton.TabIndex = 9;
			this.OpenMailButton.Text = "Open...";
			this.OpenMailButton.UseVisualStyleBackColor = true;
			this.OpenMailButton.Click += new System.EventHandler(this.OpenMailButton_Click);
			//
			// SubjectLabel
			//
			this.SubjectLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.SubjectLabel.AutoSize = true;
			this.SubjectLabel.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
			this.SubjectLabel.Name = "SubjectLabel";
			this.SubjectLabel.TabIndex = 10;
			this.SubjectLabel.Text = "Subject";
			//
			// SubjectTextBox
			//
			this.SubjectTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.SubjectTextBox.Name = "SubjectTextBox";
			this.SubjectTextBox.TabIndex = 11;
			//
			// DetailsTabControl
			//
			this.DetailsTabControl.Controls.Add(this.ErrorDetailsTabPage);
			this.DetailsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DetailsTabControl.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.DetailsTabControl.Name = "DetailsTabControl";
			this.DetailsTabControl.SelectedIndex = 0;
			this.DetailsTabControl.TabIndex = 1;
			//
			// ErrorDetailsTabPage
			//
			this.ErrorDetailsTabPage.Controls.Add(this.MainBrowser);
			this.ErrorDetailsTabPage.Name = "ErrorDetailsTabPage";
			this.ErrorDetailsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.ErrorDetailsTabPage.TabIndex = 0;
			this.ErrorDetailsTabPage.Text = "Error Details";
			this.ErrorDetailsTabPage.UseVisualStyleBackColor = true;
			//
			// MainBrowser
			//
			this.MainBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.MainBrowser.Name = "MainBrowser";
			this.MainBrowser.TabIndex = 0;
			//
			// ButtonsFlowPanel
			//
			this.ButtonsFlowPanel.AutoSize = true;
			this.ButtonsFlowPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ButtonsFlowPanel.Controls.Add(this.CloseButton);
			this.ButtonsFlowPanel.Controls.Add(this.ClearErrorsButton);
			this.ButtonsFlowPanel.Controls.Add(this.SendErrorButton);
			this.ButtonsFlowPanel.Controls.Add(this.StatusLabel);
			this.ButtonsFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ButtonsFlowPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.ButtonsFlowPanel.Margin = new System.Windows.Forms.Padding(4, 2, 4, 4);
			this.ButtonsFlowPanel.Name = "ButtonsFlowPanel";
			this.ButtonsFlowPanel.TabIndex = 2;
			this.ButtonsFlowPanel.WrapContents = false;
			//
			// CloseButton
			//
			this.CloseButton.AutoSize = true;
			this.CloseButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.TabIndex = 2;
			this.CloseButton.Text = "Close";
			this.CloseButton.UseVisualStyleBackColor = true;
			this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
			//
			// ClearErrorsButton
			//
			this.ClearErrorsButton.AutoSize = true;
			this.ClearErrorsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClearErrorsButton.Name = "ClearErrorsButton";
			this.ClearErrorsButton.TabIndex = 1;
			this.ClearErrorsButton.Text = "Clear Errors and Close";
			this.ClearErrorsButton.UseVisualStyleBackColor = true;
			this.ClearErrorsButton.Click += new System.EventHandler(this.ClearErrorsButton_Click);
			//
			// SendErrorButton
			//
			this.SendErrorButton.AutoSize = true;
			this.SendErrorButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.SendErrorButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
			this.SendErrorButton.Name = "SendErrorButton";
			this.SendErrorButton.TabIndex = 0;
			this.SendErrorButton.Text = "Send Error to X360CE";
			this.SendErrorButton.UseVisualStyleBackColor = true;
			this.SendErrorButton.Click += new System.EventHandler(this.SendErrorButton_Click);
			//
			// StatusLabel
			//
			this.StatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.StatusLabel.AutoSize = true;
			this.StatusLabel.Margin = new System.Windows.Forms.Padding(3, 6, 6, 3);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.TabIndex = 3;
			//
			// ErrorReportUserControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.RootLayoutPanel);
			this.Name = "ErrorReportUserControl";
			this.Size = new System.Drawing.Size(600, 400);
			this.RootLayoutPanel.ResumeLayout(false);
			this.RootLayoutPanel.PerformLayout();
			this.ReportGroupBox.ResumeLayout(false);
			this.ReportGroupBox.PerformLayout();
			this.FieldsLayoutPanel.ResumeLayout(false);
			this.FieldsLayoutPanel.PerformLayout();
			this.DetailsTabControl.ResumeLayout(false);
			this.ErrorDetailsTabPage.ResumeLayout(false);
			this.ButtonsFlowPanel.ResumeLayout(false);
			this.ButtonsFlowPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel RootLayoutPanel;
		private System.Windows.Forms.GroupBox ReportGroupBox;
		private System.Windows.Forms.TableLayoutPanel FieldsLayoutPanel;
		private System.Windows.Forms.Label ErrorsFolderLabel;
		private System.Windows.Forms.TextBox ErrorsFolderTextBox;
		private System.Windows.Forms.Button OpenErrorsFolderButton;
		private System.Windows.Forms.Label ErrorLabel;
		private System.Windows.Forms.ComboBox ErrorComboBox;
		private System.Windows.Forms.Label FromEmailLabel;
		private System.Windows.Forms.TextBox FromEmailTextBox;
		private System.Windows.Forms.Label ToEmailLabel;
		private System.Windows.Forms.TextBox ToEmailTextBox;
		private System.Windows.Forms.Button OpenMailButton;
		private System.Windows.Forms.Label SubjectLabel;
		private System.Windows.Forms.TextBox SubjectTextBox;
		private System.Windows.Forms.TabControl DetailsTabControl;
		private System.Windows.Forms.TabPage ErrorDetailsTabPage;
		private System.Windows.Forms.WebBrowser MainBrowser;
		private System.Windows.Forms.FlowLayoutPanel ButtonsFlowPanel;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.Button ClearErrorsButton;
		private System.Windows.Forms.Button SendErrorButton;
		public System.Windows.Forms.Label StatusLabel;
	}
}
