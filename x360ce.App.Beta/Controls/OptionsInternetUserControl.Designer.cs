namespace x360ce.App.Controls
{
	partial class OptionsInternetUserControl
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
			this.GamesGroupBox = new System.Windows.Forms.GroupBox();
			this.GetProgramsIncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
			this.GetProgramsMinInstancesUpDown = new System.Windows.Forms.NumericUpDown();
			this.UpdateOptionsGroupBox = new System.Windows.Forms.GroupBox();
			this.CheckForUpdatesCheckBox = new System.Windows.Forms.CheckBox();
			this.CheckUpdatesButton = new System.Windows.Forms.Button();
			this.InternetGroupBox = new System.Windows.Forms.GroupBox();
			this.InternetAutoSaveCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetDatabaseUrlComboBox = new System.Windows.Forms.ComboBox();
			this.WebServiceUrlLabel = new System.Windows.Forms.Label();
			this.InternetAutoLoadCheckBox = new System.Windows.Forms.CheckBox();
			this.InternetFeaturesCheckBox = new System.Windows.Forms.CheckBox();
			this.OnlineAccountGroupBox = new System.Windows.Forms.GroupBox();
			this.PasswordTextBox = new System.Windows.Forms.TextBox();
			this.ResetButton = new System.Windows.Forms.Button();
			this.OpenSettingsFolderButton = new System.Windows.Forms.Button();
			this.CreateButton = new System.Windows.Forms.Button();
			this.LoginButton = new System.Windows.Forms.Button();
			this.UsernameTextBox = new System.Windows.Forms.TextBox();
			this.ProfilePathTextBox = new System.Windows.Forms.TextBox();
			this.ComputerDiskTextBox = new System.Windows.Forms.TextBox();
			this.ProfileIdTextBox = new System.Windows.Forms.TextBox();
			this.ComputerIdTextBox = new System.Windows.Forms.TextBox();
			this.PasswordLabel = new System.Windows.Forms.Label();
			this.ProfileIdLabel = new System.Windows.Forms.Label();
			this.UsernameLabel = new System.Windows.Forms.Label();
			this.ProfilePathLabel = new System.Windows.Forms.Label();
			this.ComputerIdLabel = new System.Windows.Forms.Label();
			this.ComputerDiskLabel = new System.Windows.Forms.Label();
			this.GamesGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.GetProgramsMinInstancesUpDown)).BeginInit();
			this.UpdateOptionsGroupBox.SuspendLayout();
			this.InternetGroupBox.SuspendLayout();
			this.OnlineAccountGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// GamesGroupBox
			// 
			this.GamesGroupBox.Controls.Add(this.GetProgramsIncludeEnabledCheckBox);
			this.GamesGroupBox.Controls.Add(this.MinimumInstanceCountLabel);
			this.GamesGroupBox.Controls.Add(this.GetProgramsMinInstancesUpDown);
			this.GamesGroupBox.Location = new System.Drawing.Point(3, 3);
			this.GamesGroupBox.Name = "GamesGroupBox";
			this.GamesGroupBox.Size = new System.Drawing.Size(254, 69);
			this.GamesGroupBox.TabIndex = 33;
			this.GamesGroupBox.TabStop = false;
			this.GamesGroupBox.Text = "Games / Cloud - Default Settings";
			// 
			// GetProgramsIncludeEnabledCheckBox
			// 
			this.GetProgramsIncludeEnabledCheckBox.AutoSize = true;
			this.GetProgramsIncludeEnabledCheckBox.Checked = true;
			this.GetProgramsIncludeEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.GetProgramsIncludeEnabledCheckBox.Location = new System.Drawing.Point(74, 33);
			this.GetProgramsIncludeEnabledCheckBox.Name = "GetProgramsIncludeEnabledCheckBox";
			this.GetProgramsIncludeEnabledCheckBox.Size = new System.Drawing.Size(103, 17);
			this.GetProgramsIncludeEnabledCheckBox.TabIndex = 30;
			this.GetProgramsIncludeEnabledCheckBox.Text = "Include Enabled";
			this.GetProgramsIncludeEnabledCheckBox.ThreeState = true;
			this.GetProgramsIncludeEnabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// MinimumInstanceCountLabel
			// 
			this.MinimumInstanceCountLabel.AutoSize = true;
			this.MinimumInstanceCountLabel.BackColor = System.Drawing.SystemColors.Control;
			this.MinimumInstanceCountLabel.Location = new System.Drawing.Point(6, 16);
			this.MinimumInstanceCountLabel.Name = "MinimumInstanceCountLabel";
			this.MinimumInstanceCountLabel.Size = new System.Drawing.Size(175, 13);
			this.MinimumInstanceCountLabel.TabIndex = 32;
			this.MinimumInstanceCountLabel.Text = "Default Settings Minimum Instances";
			// 
			// GetProgramsMinInstancesUpDown
			// 
			this.GetProgramsMinInstancesUpDown.Location = new System.Drawing.Point(9, 32);
			this.GetProgramsMinInstancesUpDown.Name = "GetProgramsMinInstancesUpDown";
			this.GetProgramsMinInstancesUpDown.Size = new System.Drawing.Size(59, 20);
			this.GetProgramsMinInstancesUpDown.TabIndex = 31;
			this.GetProgramsMinInstancesUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.GetProgramsMinInstancesUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
			// 
			// UpdateOptionsGroupBox
			// 
			this.UpdateOptionsGroupBox.Controls.Add(this.CheckForUpdatesCheckBox);
			this.UpdateOptionsGroupBox.Controls.Add(this.CheckUpdatesButton);
			this.UpdateOptionsGroupBox.Location = new System.Drawing.Point(263, 18);
			this.UpdateOptionsGroupBox.Name = "UpdateOptionsGroupBox";
			this.UpdateOptionsGroupBox.Size = new System.Drawing.Size(254, 54);
			this.UpdateOptionsGroupBox.TabIndex = 34;
			this.UpdateOptionsGroupBox.TabStop = false;
			this.UpdateOptionsGroupBox.Text = "Update Options";
			// 
			// CheckForUpdatesCheckBox
			// 
			this.CheckForUpdatesCheckBox.AutoSize = true;
			this.CheckForUpdatesCheckBox.Location = new System.Drawing.Point(6, 23);
			this.CheckForUpdatesCheckBox.Name = "CheckForUpdatesCheckBox";
			this.CheckForUpdatesCheckBox.Size = new System.Drawing.Size(163, 17);
			this.CheckForUpdatesCheckBox.TabIndex = 30;
			this.CheckForUpdatesCheckBox.Text = "Check for updates on startup";
			this.CheckForUpdatesCheckBox.UseVisualStyleBackColor = true;
			// 
			// CheckUpdatesButton
			// 
			this.CheckUpdatesButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CheckUpdatesButton.Location = new System.Drawing.Point(173, 19);
			this.CheckUpdatesButton.Name = "CheckUpdatesButton";
			this.CheckUpdatesButton.Size = new System.Drawing.Size(75, 23);
			this.CheckUpdatesButton.TabIndex = 68;
			this.CheckUpdatesButton.Text = "Check...";
			this.CheckUpdatesButton.UseVisualStyleBackColor = true;
			this.CheckUpdatesButton.Click += new System.EventHandler(this.CheckUpdatesButton_Click);
			// 
			// InternetGroupBox
			// 
			this.InternetGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InternetGroupBox.Controls.Add(this.InternetAutoSaveCheckBox);
			this.InternetGroupBox.Controls.Add(this.InternetDatabaseUrlComboBox);
			this.InternetGroupBox.Controls.Add(this.WebServiceUrlLabel);
			this.InternetGroupBox.Controls.Add(this.InternetAutoLoadCheckBox);
			this.InternetGroupBox.Controls.Add(this.InternetFeaturesCheckBox);
			this.InternetGroupBox.Location = new System.Drawing.Point(3, 78);
			this.InternetGroupBox.Name = "InternetGroupBox";
			this.InternetGroupBox.Size = new System.Drawing.Size(638, 74);
			this.InternetGroupBox.TabIndex = 36;
			this.InternetGroupBox.TabStop = false;
			this.InternetGroupBox.Text = "Internet";
			// 
			// InternetAutoSaveCheckBox
			// 
			this.InternetAutoSaveCheckBox.AutoSize = true;
			this.InternetAutoSaveCheckBox.Location = new System.Drawing.Point(304, 21);
			this.InternetAutoSaveCheckBox.Name = "InternetAutoSaveCheckBox";
			this.InternetAutoSaveCheckBox.Size = new System.Drawing.Size(134, 17);
			this.InternetAutoSaveCheckBox.TabIndex = 15;
			this.InternetAutoSaveCheckBox.Text = "Save Settings to Cloud";
			this.InternetAutoSaveCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetDatabaseUrlComboBox
			// 
			this.InternetDatabaseUrlComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.InternetDatabaseUrlComboBox.FormattingEnabled = true;
			this.InternetDatabaseUrlComboBox.Items.AddRange(new object[] {
            "http://www.x360ce.com/webservices/x360ce.asmx",
            "http://localhost:20360/webservices/x360ce.asmx"});
			this.InternetDatabaseUrlComboBox.Location = new System.Drawing.Point(109, 44);
			this.InternetDatabaseUrlComboBox.Name = "InternetDatabaseUrlComboBox";
			this.InternetDatabaseUrlComboBox.Size = new System.Drawing.Size(523, 21);
			this.InternetDatabaseUrlComboBox.TabIndex = 14;
			// 
			// WebServiceUrlLabel
			// 
			this.WebServiceUrlLabel.AutoSize = true;
			this.WebServiceUrlLabel.Location = new System.Drawing.Point(6, 47);
			this.WebServiceUrlLabel.Name = "WebServiceUrlLabel";
			this.WebServiceUrlLabel.Size = new System.Drawing.Size(94, 13);
			this.WebServiceUrlLabel.TabIndex = 1;
			this.WebServiceUrlLabel.Text = "Web Service URL";
			// 
			// InternetAutoLoadCheckBox
			// 
			this.InternetAutoLoadCheckBox.AutoSize = true;
			this.InternetAutoLoadCheckBox.Location = new System.Drawing.Point(154, 21);
			this.InternetAutoLoadCheckBox.Name = "InternetAutoLoadCheckBox";
			this.InternetAutoLoadCheckBox.Size = new System.Drawing.Size(144, 17);
			this.InternetAutoLoadCheckBox.TabIndex = 1;
			this.InternetAutoLoadCheckBox.Text = "Load Settings from Cloud";
			this.InternetAutoLoadCheckBox.UseVisualStyleBackColor = true;
			// 
			// InternetFeaturesCheckBox
			// 
			this.InternetFeaturesCheckBox.AutoSize = true;
			this.InternetFeaturesCheckBox.Location = new System.Drawing.Point(6, 21);
			this.InternetFeaturesCheckBox.Name = "InternetFeaturesCheckBox";
			this.InternetFeaturesCheckBox.Size = new System.Drawing.Size(142, 17);
			this.InternetFeaturesCheckBox.TabIndex = 1;
			this.InternetFeaturesCheckBox.Text = "Enable Internet Features";
			this.InternetFeaturesCheckBox.UseVisualStyleBackColor = true;
			// 
			// OnlineAccountGroupBox
			// 
			this.OnlineAccountGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.OnlineAccountGroupBox.Controls.Add(this.PasswordTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.ResetButton);
			this.OnlineAccountGroupBox.Controls.Add(this.OpenSettingsFolderButton);
			this.OnlineAccountGroupBox.Controls.Add(this.CreateButton);
			this.OnlineAccountGroupBox.Controls.Add(this.LoginButton);
			this.OnlineAccountGroupBox.Controls.Add(this.UsernameTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.ProfilePathTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.ComputerDiskTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.ProfileIdTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.ComputerIdTextBox);
			this.OnlineAccountGroupBox.Controls.Add(this.PasswordLabel);
			this.OnlineAccountGroupBox.Controls.Add(this.ProfileIdLabel);
			this.OnlineAccountGroupBox.Controls.Add(this.UsernameLabel);
			this.OnlineAccountGroupBox.Controls.Add(this.ProfilePathLabel);
			this.OnlineAccountGroupBox.Controls.Add(this.ComputerIdLabel);
			this.OnlineAccountGroupBox.Controls.Add(this.ComputerDiskLabel);
			this.OnlineAccountGroupBox.Location = new System.Drawing.Point(3, 158);
			this.OnlineAccountGroupBox.Name = "OnlineAccountGroupBox";
			this.OnlineAccountGroupBox.Size = new System.Drawing.Size(638, 180);
			this.OnlineAccountGroupBox.TabIndex = 35;
			this.OnlineAccountGroupBox.TabStop = false;
			this.OnlineAccountGroupBox.Text = "Online Account - Anonymous Computer ID and Profile ID will be used by default";
			// 
			// PasswordTextBox
			// 
			this.PasswordTextBox.Location = new System.Drawing.Point(106, 149);
			this.PasswordTextBox.Name = "PasswordTextBox";
			this.PasswordTextBox.Size = new System.Drawing.Size(152, 20);
			this.PasswordTextBox.TabIndex = 43;
			// 
			// ResetButton
			// 
			this.ResetButton.Location = new System.Drawing.Point(426, 147);
			this.ResetButton.Name = "ResetButton";
			this.ResetButton.Size = new System.Drawing.Size(75, 23);
			this.ResetButton.TabIndex = 45;
			this.ResetButton.Text = "Reset...";
			this.ResetButton.UseVisualStyleBackColor = true;
			this.ResetButton.Click += new System.EventHandler(this.ResetButton_Click);
			// 
			// OpenSettingsFolderButton
			// 
			this.OpenSettingsFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OpenSettingsFolderButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
			this.OpenSettingsFolderButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.OpenSettingsFolderButton.Location = new System.Drawing.Point(556, 71);
			this.OpenSettingsFolderButton.Name = "OpenSettingsFolderButton";
			this.OpenSettingsFolderButton.Size = new System.Drawing.Size(75, 23);
			this.OpenSettingsFolderButton.TabIndex = 69;
			this.OpenSettingsFolderButton.Text = "Open";
			this.OpenSettingsFolderButton.UseVisualStyleBackColor = true;
			this.OpenSettingsFolderButton.Click += new System.EventHandler(this.OpenSettingsFolderButton_Click);
			// 
			// CreateButton
			// 
			this.CreateButton.Location = new System.Drawing.Point(345, 147);
			this.CreateButton.Name = "CreateButton";
			this.CreateButton.Size = new System.Drawing.Size(75, 23);
			this.CreateButton.TabIndex = 45;
			this.CreateButton.Text = "Create...";
			this.CreateButton.UseVisualStyleBackColor = true;
			this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
			// 
			// LoginButton
			// 
			this.LoginButton.Location = new System.Drawing.Point(264, 147);
			this.LoginButton.Name = "LoginButton";
			this.LoginButton.Size = new System.Drawing.Size(75, 23);
			this.LoginButton.TabIndex = 44;
			this.LoginButton.Text = "Log In";
			this.LoginButton.UseVisualStyleBackColor = true;
			this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
			// 
			// UsernameTextBox
			// 
			this.UsernameTextBox.Location = new System.Drawing.Point(106, 123);
			this.UsernameTextBox.Name = "UsernameTextBox";
			this.UsernameTextBox.Size = new System.Drawing.Size(152, 20);
			this.UsernameTextBox.TabIndex = 42;
			// 
			// ProfilePathTextBox
			// 
			this.ProfilePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProfilePathTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ProfilePathTextBox.Location = new System.Drawing.Point(106, 71);
			this.ProfilePathTextBox.Name = "ProfilePathTextBox";
			this.ProfilePathTextBox.Size = new System.Drawing.Size(445, 20);
			this.ProfilePathTextBox.TabIndex = 40;
			// 
			// ComputerDiskTextBox
			// 
			this.ComputerDiskTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ComputerDiskTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ComputerDiskTextBox.Location = new System.Drawing.Point(107, 19);
			this.ComputerDiskTextBox.Name = "ComputerDiskTextBox";
			this.ComputerDiskTextBox.Size = new System.Drawing.Size(525, 20);
			this.ComputerDiskTextBox.TabIndex = 40;
			// 
			// ProfileIdTextBox
			// 
			this.ProfileIdTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ProfileIdTextBox.Location = new System.Drawing.Point(106, 97);
			this.ProfileIdTextBox.Name = "ProfileIdTextBox";
			this.ProfileIdTextBox.Size = new System.Drawing.Size(233, 20);
			this.ProfileIdTextBox.TabIndex = 41;
			// 
			// ComputerIdTextBox
			// 
			this.ComputerIdTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.ComputerIdTextBox.Location = new System.Drawing.Point(107, 45);
			this.ComputerIdTextBox.Name = "ComputerIdTextBox";
			this.ComputerIdTextBox.Size = new System.Drawing.Size(233, 20);
			this.ComputerIdTextBox.TabIndex = 41;
			// 
			// PasswordLabel
			// 
			this.PasswordLabel.AutoSize = true;
			this.PasswordLabel.BackColor = System.Drawing.SystemColors.Control;
			this.PasswordLabel.Location = new System.Drawing.Point(5, 152);
			this.PasswordLabel.Name = "PasswordLabel";
			this.PasswordLabel.Size = new System.Drawing.Size(53, 13);
			this.PasswordLabel.TabIndex = 31;
			this.PasswordLabel.Text = "Password";
			// 
			// ProfileIdLabel
			// 
			this.ProfileIdLabel.AutoSize = true;
			this.ProfileIdLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ProfileIdLabel.Location = new System.Drawing.Point(6, 100);
			this.ProfileIdLabel.Name = "ProfileIdLabel";
			this.ProfileIdLabel.Size = new System.Drawing.Size(50, 13);
			this.ProfileIdLabel.TabIndex = 31;
			this.ProfileIdLabel.Text = "Profile ID";
			// 
			// UsernameLabel
			// 
			this.UsernameLabel.AutoSize = true;
			this.UsernameLabel.BackColor = System.Drawing.SystemColors.Control;
			this.UsernameLabel.Location = new System.Drawing.Point(5, 126);
			this.UsernameLabel.Name = "UsernameLabel";
			this.UsernameLabel.Size = new System.Drawing.Size(92, 13);
			this.UsernameLabel.TabIndex = 31;
			this.UsernameLabel.Text = "Username (E-mail)";
			// 
			// ProfilePathLabel
			// 
			this.ProfilePathLabel.AutoSize = true;
			this.ProfilePathLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ProfilePathLabel.Location = new System.Drawing.Point(6, 74);
			this.ProfilePathLabel.Name = "ProfilePathLabel";
			this.ProfilePathLabel.Size = new System.Drawing.Size(61, 13);
			this.ProfilePathLabel.TabIndex = 32;
			this.ProfilePathLabel.Text = "Profile Path";
			// 
			// ComputerIdLabel
			// 
			this.ComputerIdLabel.AutoSize = true;
			this.ComputerIdLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ComputerIdLabel.Location = new System.Drawing.Point(6, 48);
			this.ComputerIdLabel.Name = "ComputerIdLabel";
			this.ComputerIdLabel.Size = new System.Drawing.Size(66, 13);
			this.ComputerIdLabel.TabIndex = 31;
			this.ComputerIdLabel.Text = "Computer ID";
			// 
			// ComputerDiskLabel
			// 
			this.ComputerDiskLabel.AutoSize = true;
			this.ComputerDiskLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ComputerDiskLabel.Location = new System.Drawing.Point(6, 22);
			this.ComputerDiskLabel.Name = "ComputerDiskLabel";
			this.ComputerDiskLabel.Size = new System.Drawing.Size(76, 13);
			this.ComputerDiskLabel.TabIndex = 32;
			this.ComputerDiskLabel.Text = "Computer Disk";
			// 
			// OptionsInternetUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.GamesGroupBox);
			this.Controls.Add(this.UpdateOptionsGroupBox);
			this.Controls.Add(this.InternetGroupBox);
			this.Controls.Add(this.OnlineAccountGroupBox);
			this.Name = "OptionsInternetUserControl";
			this.Size = new System.Drawing.Size(644, 415);
			this.GamesGroupBox.ResumeLayout(false);
			this.GamesGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.GetProgramsMinInstancesUpDown)).EndInit();
			this.UpdateOptionsGroupBox.ResumeLayout(false);
			this.UpdateOptionsGroupBox.PerformLayout();
			this.InternetGroupBox.ResumeLayout(false);
			this.InternetGroupBox.PerformLayout();
			this.OnlineAccountGroupBox.ResumeLayout(false);
			this.OnlineAccountGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox GamesGroupBox;
		public System.Windows.Forms.CheckBox GetProgramsIncludeEnabledCheckBox;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
		public System.Windows.Forms.NumericUpDown GetProgramsMinInstancesUpDown;
		private System.Windows.Forms.GroupBox UpdateOptionsGroupBox;
		public System.Windows.Forms.CheckBox CheckForUpdatesCheckBox;
		private System.Windows.Forms.Button CheckUpdatesButton;
		private System.Windows.Forms.GroupBox InternetGroupBox;
		public System.Windows.Forms.CheckBox InternetAutoSaveCheckBox;
		public System.Windows.Forms.ComboBox InternetDatabaseUrlComboBox;
		private System.Windows.Forms.Label WebServiceUrlLabel;
		public System.Windows.Forms.CheckBox InternetAutoLoadCheckBox;
		public System.Windows.Forms.CheckBox InternetFeaturesCheckBox;
		private System.Windows.Forms.GroupBox OnlineAccountGroupBox;
		private System.Windows.Forms.TextBox PasswordTextBox;
		private System.Windows.Forms.Button ResetButton;
		private System.Windows.Forms.Button OpenSettingsFolderButton;
		private System.Windows.Forms.Button CreateButton;
		private System.Windows.Forms.Button LoginButton;
		private System.Windows.Forms.TextBox UsernameTextBox;
		private System.Windows.Forms.TextBox ProfilePathTextBox;
		private System.Windows.Forms.TextBox ComputerDiskTextBox;
		private System.Windows.Forms.TextBox ProfileIdTextBox;
		private System.Windows.Forms.TextBox ComputerIdTextBox;
		private System.Windows.Forms.Label PasswordLabel;
		private System.Windows.Forms.Label ProfileIdLabel;
		private System.Windows.Forms.Label UsernameLabel;
		private System.Windows.Forms.Label ProfilePathLabel;
		private System.Windows.Forms.Label ComputerIdLabel;
		private System.Windows.Forms.Label ComputerDiskLabel;
	}
}
