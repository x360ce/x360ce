namespace x360ce.App.Controls
{
    partial class OptionsControl
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
            this.TestingAndLoggingGroupBox = new System.Windows.Forms.GroupBox();
            this.XInputEnableCheckBox = new System.Windows.Forms.CheckBox();
            this.UseInitBeepCheckBox = new System.Windows.Forms.CheckBox();
            this.ConsoleCheckBox = new System.Windows.Forms.CheckBox();
            this.DebugModeCheckBox = new System.Windows.Forms.CheckBox();
            this.EnableLoggingCheckBox = new System.Windows.Forms.CheckBox();
            this.OperationGroupBox = new System.Windows.Forms.GroupBox();
            this.AllowOnlyOneCopyCheckBox = new System.Windows.Forms.CheckBox();
            this.InternetGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.InternetDatabaseUrlTextBox = new System.Windows.Forms.TextBox();
            this.InternetAutoloadCheckBox = new System.Windows.Forms.CheckBox();
            this.InternetCheckBox = new System.Windows.Forms.CheckBox();
            this.FakeApiGroupBox = new System.Windows.Forms.GroupBox();
            this.FakeModeLabel = new System.Windows.Forms.Label();
            this.FakeModeComboBox = new System.Windows.Forms.ComboBox();
            this.ProgramScanLocationsTabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.AddLocationButton = new System.Windows.Forms.Button();
            this.RemoveLocationButton = new System.Windows.Forms.Button();
            this.ProgramScanLocationsListBox = new System.Windows.Forms.ListBox();
            this.TestingAndLoggingGroupBox.SuspendLayout();
            this.OperationGroupBox.SuspendLayout();
            this.InternetGroupBox.SuspendLayout();
            this.FakeApiGroupBox.SuspendLayout();
            this.ProgramScanLocationsTabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TestingAndLoggingGroupBox
            // 
            this.TestingAndLoggingGroupBox.Controls.Add(this.XInputEnableCheckBox);
            this.TestingAndLoggingGroupBox.Controls.Add(this.UseInitBeepCheckBox);
            this.TestingAndLoggingGroupBox.Controls.Add(this.ConsoleCheckBox);
            this.TestingAndLoggingGroupBox.Controls.Add(this.DebugModeCheckBox);
            this.TestingAndLoggingGroupBox.Controls.Add(this.EnableLoggingCheckBox);
            this.TestingAndLoggingGroupBox.Location = new System.Drawing.Point(3, 106);
            this.TestingAndLoggingGroupBox.Name = "TestingAndLoggingGroupBox";
            this.TestingAndLoggingGroupBox.Size = new System.Drawing.Size(241, 143);
            this.TestingAndLoggingGroupBox.TabIndex = 30;
            this.TestingAndLoggingGroupBox.TabStop = false;
            this.TestingAndLoggingGroupBox.Text = "Testing and Logging";
            // 
            // XInputEnableCheckBox
            // 
            this.XInputEnableCheckBox.AutoSize = true;
            this.XInputEnableCheckBox.Checked = true;
            this.XInputEnableCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.XInputEnableCheckBox.Location = new System.Drawing.Point(6, 27);
            this.XInputEnableCheckBox.Name = "XInputEnableCheckBox";
            this.XInputEnableCheckBox.Size = new System.Drawing.Size(93, 17);
            this.XInputEnableCheckBox.TabIndex = 0;
            this.XInputEnableCheckBox.Text = "Enable XInput";
            this.XInputEnableCheckBox.UseVisualStyleBackColor = true;
            // 
            // UseInitBeepCheckBox
            // 
            this.UseInitBeepCheckBox.AutoSize = true;
            this.UseInitBeepCheckBox.Location = new System.Drawing.Point(5, 50);
            this.UseInitBeepCheckBox.Name = "UseInitBeepCheckBox";
            this.UseInitBeepCheckBox.Size = new System.Drawing.Size(90, 17);
            this.UseInitBeepCheckBox.TabIndex = 0;
            this.UseInitBeepCheckBox.Text = "Use Init Beep";
            this.UseInitBeepCheckBox.UseVisualStyleBackColor = true;
            // 
            // ConsoleCheckBox
            // 
            this.ConsoleCheckBox.AutoSize = true;
            this.ConsoleCheckBox.Location = new System.Drawing.Point(5, 96);
            this.ConsoleCheckBox.Name = "ConsoleCheckBox";
            this.ConsoleCheckBox.Size = new System.Drawing.Size(100, 17);
            this.ConsoleCheckBox.TabIndex = 0;
            this.ConsoleCheckBox.Text = "Enable Console";
            this.ConsoleCheckBox.UseVisualStyleBackColor = true;
            // 
            // DebugModeCheckBox
            // 
            this.DebugModeCheckBox.AutoSize = true;
            this.DebugModeCheckBox.Checked = true;
            this.DebugModeCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DebugModeCheckBox.Location = new System.Drawing.Point(5, 119);
            this.DebugModeCheckBox.Name = "DebugModeCheckBox";
            this.DebugModeCheckBox.Size = new System.Drawing.Size(88, 17);
            this.DebugModeCheckBox.TabIndex = 0;
            this.DebugModeCheckBox.Text = "Debug Mode";
            this.DebugModeCheckBox.UseVisualStyleBackColor = true;
            // 
            // EnableLoggingCheckBox
            // 
            this.EnableLoggingCheckBox.AutoSize = true;
            this.EnableLoggingCheckBox.Location = new System.Drawing.Point(5, 73);
            this.EnableLoggingCheckBox.Name = "EnableLoggingCheckBox";
            this.EnableLoggingCheckBox.Size = new System.Drawing.Size(100, 17);
            this.EnableLoggingCheckBox.TabIndex = 0;
            this.EnableLoggingCheckBox.Text = "Enable Logging";
            this.EnableLoggingCheckBox.UseVisualStyleBackColor = true;
            // 
            // OperationGroupBox
            // 
            this.OperationGroupBox.Controls.Add(this.AllowOnlyOneCopyCheckBox);
            this.OperationGroupBox.Location = new System.Drawing.Point(3, 53);
            this.OperationGroupBox.Name = "OperationGroupBox";
            this.OperationGroupBox.Size = new System.Drawing.Size(241, 47);
            this.OperationGroupBox.TabIndex = 31;
            this.OperationGroupBox.TabStop = false;
            this.OperationGroupBox.Text = "Operation";
            // 
            // AllowOnlyOneCopyCheckBox
            // 
            this.AllowOnlyOneCopyCheckBox.AutoSize = true;
            this.AllowOnlyOneCopyCheckBox.Location = new System.Drawing.Point(9, 19);
            this.AllowOnlyOneCopyCheckBox.Name = "AllowOnlyOneCopyCheckBox";
            this.AllowOnlyOneCopyCheckBox.Size = new System.Drawing.Size(230, 17);
            this.AllowOnlyOneCopyCheckBox.TabIndex = 1;
            this.AllowOnlyOneCopyCheckBox.Text = "Allow only one copy of Application at a time";
            this.AllowOnlyOneCopyCheckBox.UseVisualStyleBackColor = true;
            // 
            // InternetGroupBox
            // 
            this.InternetGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InternetGroupBox.Controls.Add(this.label1);
            this.InternetGroupBox.Controls.Add(this.InternetDatabaseUrlTextBox);
            this.InternetGroupBox.Controls.Add(this.InternetAutoloadCheckBox);
            this.InternetGroupBox.Controls.Add(this.InternetCheckBox);
            this.InternetGroupBox.Location = new System.Drawing.Point(250, 3);
            this.InternetGroupBox.Name = "InternetGroupBox";
            this.InternetGroupBox.Size = new System.Drawing.Size(390, 114);
            this.InternetGroupBox.TabIndex = 32;
            this.InternetGroupBox.TabStop = false;
            this.InternetGroupBox.Text = "Internet";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 62);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Web Service URL:";
            // 
            // InternetDatabaseUrlTextBox
            // 
            this.InternetDatabaseUrlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InternetDatabaseUrlTextBox.Location = new System.Drawing.Point(6, 82);
            this.InternetDatabaseUrlTextBox.Name = "InternetDatabaseUrlTextBox";
            this.InternetDatabaseUrlTextBox.ReadOnly = true;
            this.InternetDatabaseUrlTextBox.Size = new System.Drawing.Size(378, 20);
            this.InternetDatabaseUrlTextBox.TabIndex = 13;
            // 
            // InternetAutoloadCheckBox
            // 
            this.InternetAutoloadCheckBox.AutoSize = true;
            this.InternetAutoloadCheckBox.Location = new System.Drawing.Point(9, 42);
            this.InternetAutoloadCheckBox.Name = "InternetAutoloadCheckBox";
            this.InternetAutoloadCheckBox.Size = new System.Drawing.Size(215, 17);
            this.InternetAutoloadCheckBox.TabIndex = 1;
            this.InternetAutoloadCheckBox.Text = "Auto Load Settings When Tab Selected";
            this.InternetAutoloadCheckBox.UseVisualStyleBackColor = true;
            // 
            // InternetCheckBox
            // 
            this.InternetCheckBox.AutoSize = true;
            this.InternetCheckBox.Location = new System.Drawing.Point(9, 19);
            this.InternetCheckBox.Name = "InternetCheckBox";
            this.InternetCheckBox.Size = new System.Drawing.Size(232, 17);
            this.InternetCheckBox.TabIndex = 1;
            this.InternetCheckBox.Text = "Enable Internet Settings Database Features";
            this.InternetCheckBox.UseVisualStyleBackColor = true;
            // 
            // FakeApiGroupBox
            // 
            this.FakeApiGroupBox.Controls.Add(this.FakeModeLabel);
            this.FakeApiGroupBox.Controls.Add(this.FakeModeComboBox);
            this.FakeApiGroupBox.Location = new System.Drawing.Point(3, 3);
            this.FakeApiGroupBox.Name = "FakeApiGroupBox";
            this.FakeApiGroupBox.Size = new System.Drawing.Size(241, 44);
            this.FakeApiGroupBox.TabIndex = 33;
            this.FakeApiGroupBox.TabStop = false;
            this.FakeApiGroupBox.Text = "InputHook";
            // 
            // FakeModeLabel
            // 
            this.FakeModeLabel.AutoSize = true;
            this.FakeModeLabel.Location = new System.Drawing.Point(6, 16);
            this.FakeModeLabel.Name = "FakeModeLabel";
            this.FakeModeLabel.Size = new System.Drawing.Size(66, 13);
            this.FakeModeLabel.TabIndex = 0;
            this.FakeModeLabel.Text = "Hook Mode:";
            // 
            // FakeModeComboBox
            // 
            this.FakeModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FakeModeComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.FakeModeComboBox.FormattingEnabled = true;
            this.FakeModeComboBox.Location = new System.Drawing.Point(76, 13);
            this.FakeModeComboBox.Name = "FakeModeComboBox";
            this.FakeModeComboBox.Size = new System.Drawing.Size(159, 21);
            this.FakeModeComboBox.TabIndex = 0;
            // 
            // ProgramScanLocationsTabControl
            // 
            this.ProgramScanLocationsTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgramScanLocationsTabControl.Controls.Add(this.tabPage1);
            this.ProgramScanLocationsTabControl.ItemSize = new System.Drawing.Size(116, 24);
            this.ProgramScanLocationsTabControl.Location = new System.Drawing.Point(250, 123);
            this.ProgramScanLocationsTabControl.Name = "ProgramScanLocationsTabControl";
            this.ProgramScanLocationsTabControl.SelectedIndex = 0;
            this.ProgramScanLocationsTabControl.Size = new System.Drawing.Size(391, 126);
            this.ProgramScanLocationsTabControl.TabIndex = 34;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.ProgramScanLocationsListBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 28);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(383, 94);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Game Scan Locations:";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // AddLocationButton
            // 
            this.AddLocationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddLocationButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
            this.AddLocationButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.AddLocationButton.Location = new System.Drawing.Point(489, 122);
            this.AddLocationButton.Name = "AddLocationButton";
            this.AddLocationButton.Size = new System.Drawing.Size(75, 25);
            this.AddLocationButton.TabIndex = 36;
            this.AddLocationButton.Text = "&Add...";
            this.AddLocationButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.AddLocationButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.AddLocationButton.UseVisualStyleBackColor = true;
            this.AddLocationButton.Click += new System.EventHandler(this.AddLocationButton_Click);
            // 
            // RemoveLocationButton
            // 
            this.RemoveLocationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveLocationButton.Image = global::x360ce.App.Properties.Resources.remove_16x16;
            this.RemoveLocationButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RemoveLocationButton.Location = new System.Drawing.Point(565, 122);
            this.RemoveLocationButton.Name = "RemoveLocationButton";
            this.RemoveLocationButton.Size = new System.Drawing.Size(75, 25);
            this.RemoveLocationButton.TabIndex = 35;
            this.RemoveLocationButton.Text = "&Remove";
            this.RemoveLocationButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.RemoveLocationButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.RemoveLocationButton.UseVisualStyleBackColor = true;
            this.RemoveLocationButton.Click += new System.EventHandler(this.RemoveLocationButton_Click);
            // 
            // ProgramScanLocationsListBox
            // 
            this.ProgramScanLocationsListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ProgramScanLocationsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProgramScanLocationsListBox.FormattingEnabled = true;
            this.ProgramScanLocationsListBox.Location = new System.Drawing.Point(0, 0);
            this.ProgramScanLocationsListBox.Name = "ProgramScanLocationsListBox";
            this.ProgramScanLocationsListBox.Size = new System.Drawing.Size(383, 94);
            this.ProgramScanLocationsListBox.TabIndex = 0;
            // 
            // OptionsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.AddLocationButton);
            this.Controls.Add(this.RemoveLocationButton);
            this.Controls.Add(this.TestingAndLoggingGroupBox);
            this.Controls.Add(this.OperationGroupBox);
            this.Controls.Add(this.InternetGroupBox);
            this.Controls.Add(this.FakeApiGroupBox);
            this.Controls.Add(this.ProgramScanLocationsTabControl);
            this.Name = "OptionsControl";
            this.Size = new System.Drawing.Size(644, 273);
            this.TestingAndLoggingGroupBox.ResumeLayout(false);
            this.TestingAndLoggingGroupBox.PerformLayout();
            this.OperationGroupBox.ResumeLayout(false);
            this.OperationGroupBox.PerformLayout();
            this.InternetGroupBox.ResumeLayout(false);
            this.InternetGroupBox.PerformLayout();
            this.FakeApiGroupBox.ResumeLayout(false);
            this.FakeApiGroupBox.PerformLayout();
            this.ProgramScanLocationsTabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button AddLocationButton;
        private System.Windows.Forms.Button RemoveLocationButton;
        private System.Windows.Forms.GroupBox TestingAndLoggingGroupBox;
        private System.Windows.Forms.CheckBox XInputEnableCheckBox;
        private System.Windows.Forms.CheckBox UseInitBeepCheckBox;
        private System.Windows.Forms.CheckBox ConsoleCheckBox;
        private System.Windows.Forms.CheckBox DebugModeCheckBox;
        private System.Windows.Forms.CheckBox EnableLoggingCheckBox;
        private System.Windows.Forms.GroupBox OperationGroupBox;
        public System.Windows.Forms.CheckBox AllowOnlyOneCopyCheckBox;
        private System.Windows.Forms.GroupBox InternetGroupBox;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox InternetDatabaseUrlTextBox;
        public System.Windows.Forms.CheckBox InternetAutoloadCheckBox;
        public System.Windows.Forms.CheckBox InternetCheckBox;
        private System.Windows.Forms.GroupBox FakeApiGroupBox;
        private System.Windows.Forms.Label FakeModeLabel;
        private System.Windows.Forms.ComboBox FakeModeComboBox;
        private System.Windows.Forms.TabControl ProgramScanLocationsTabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListBox ProgramScanLocationsListBox;
    }
}
