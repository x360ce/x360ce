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
            this.MainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.GamesGroupBox = new System.Windows.Forms.GroupBox();
            this.GamesLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.MinimumInstanceCountLabel = new System.Windows.Forms.Label();
            this.GetProgramsMinInstancesUpDown = new System.Windows.Forms.NumericUpDown();
            this.GetProgramsIncludeEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.InternetGroupBox = new System.Windows.Forms.GroupBox();
            this.InternetLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.InternetCheckBoxesPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.InternetFeaturesCheckBox = new System.Windows.Forms.CheckBox();
            this.InternetAutoLoadCheckBox = new System.Windows.Forms.CheckBox();
            this.InternetAutoSaveCheckBox = new System.Windows.Forms.CheckBox();
            this.WebServiceUrlLabel = new System.Windows.Forms.Label();
            this.InternetDatabaseUrlComboBox = new System.Windows.Forms.ComboBox();
            this.TestServiceButton = new System.Windows.Forms.Button();
            this.OnlineAccountGroupBox = new System.Windows.Forms.GroupBox();
            this.OnlineAccountLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.ComputerDiskLabel = new System.Windows.Forms.Label();
            this.ComputerDiskTextBox = new System.Windows.Forms.TextBox();
            this.ComputerIdLabel = new System.Windows.Forms.Label();
            this.ComputerIdTextBox = new System.Windows.Forms.TextBox();
            this.ProfilePathLabel = new System.Windows.Forms.Label();
            this.ProfilePathTextBox = new System.Windows.Forms.TextBox();
            this.OpenSettingsFolderButton = new System.Windows.Forms.Button();
            this.ProfileIdLabel = new System.Windows.Forms.Label();
            this.ProfileIdTextBox = new System.Windows.Forms.TextBox();
            this.OnlineAccountLoginGroupBox = new System.Windows.Forms.GroupBox();
            this.OnlineAccountLoginLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.UsernameLabel = new System.Windows.Forms.Label();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.OnlineAccountLoginButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.LoginButton = new System.Windows.Forms.Button();
            this.CreateButton = new System.Windows.Forms.Button();
            this.ResetButton = new System.Windows.Forms.Button();
            this.MainLayoutPanel.SuspendLayout();
            this.GamesGroupBox.SuspendLayout();
            this.GamesLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GetProgramsMinInstancesUpDown)).BeginInit();
            this.InternetGroupBox.SuspendLayout();
            this.InternetLayoutPanel.SuspendLayout();
            this.InternetCheckBoxesPanel.SuspendLayout();
            this.OnlineAccountGroupBox.SuspendLayout();
            this.OnlineAccountLayoutPanel.SuspendLayout();
            this.OnlineAccountLoginGroupBox.SuspendLayout();
            this.OnlineAccountLoginLayoutPanel.SuspendLayout();
            this.OnlineAccountLoginButtonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayoutPanel
            // 
            this.MainLayoutPanel.ColumnCount = 1;
            this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainLayoutPanel.Controls.Add(this.GamesGroupBox, 0, 0);
            this.MainLayoutPanel.Controls.Add(this.InternetGroupBox, 0, 1);
            this.MainLayoutPanel.Controls.Add(this.OnlineAccountGroupBox, 0, 2);
            this.MainLayoutPanel.Controls.Add(this.OnlineAccountLoginGroupBox, 0, 3);
            this.MainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.MainLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.MainLayoutPanel.Name = "MainLayoutPanel";
            this.MainLayoutPanel.RowCount = 4;
            this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainLayoutPanel.Size = new System.Drawing.Size(966, 631);
            this.MainLayoutPanel.TabIndex = 0;
            // 
            // GamesGroupBox
            // 
            this.GamesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GamesGroupBox.AutoSize = true;
            this.GamesGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.GamesGroupBox.Controls.Add(this.GamesLayoutPanel);
            this.GamesGroupBox.Location = new System.Drawing.Point(4, 5);
            this.GamesGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GamesGroupBox.Name = "GamesGroupBox";
            this.GamesGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GamesGroupBox.Size = new System.Drawing.Size(958, 101);
            this.GamesGroupBox.TabIndex = 0;
            this.GamesGroupBox.TabStop = false;
            this.GamesGroupBox.Text = "Games / Cloud - Default Settings";
            // 
            // GamesLayoutPanel
            // 
            this.GamesLayoutPanel.AutoSize = true;
            this.GamesLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.GamesLayoutPanel.ColumnCount = 2;
            this.GamesLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.GamesLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.GamesLayoutPanel.Controls.Add(this.MinimumInstanceCountLabel, 0, 0);
            this.GamesLayoutPanel.Controls.Add(this.GetProgramsMinInstancesUpDown, 0, 1);
            this.GamesLayoutPanel.Controls.Add(this.GetProgramsIncludeEnabledCheckBox, 1, 1);
            this.GamesLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.GamesLayoutPanel.Location = new System.Drawing.Point(4, 24);
            this.GamesLayoutPanel.Name = "GamesLayoutPanel";
            this.GamesLayoutPanel.Padding = new System.Windows.Forms.Padding(4);
            this.GamesLayoutPanel.RowCount = 2;
            this.GamesLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.GamesLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.GamesLayoutPanel.Size = new System.Drawing.Size(950, 72);
            this.GamesLayoutPanel.TabIndex = 0;
            // 
            // MinimumInstanceCountLabel
            // 
            this.MinimumInstanceCountLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.MinimumInstanceCountLabel.AutoSize = true;
            this.MinimumInstanceCountLabel.BackColor = System.Drawing.SystemColors.Control;
            this.GamesLayoutPanel.SetColumnSpan(this.MinimumInstanceCountLabel, 2);
            this.MinimumInstanceCountLabel.Location = new System.Drawing.Point(8, 4);
            this.MinimumInstanceCountLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.MinimumInstanceCountLabel.Name = "MinimumInstanceCountLabel";
            this.MinimumInstanceCountLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.MinimumInstanceCountLabel.Size = new System.Drawing.Size(265, 28);
            this.MinimumInstanceCountLabel.TabIndex = 0;
            this.MinimumInstanceCountLabel.Text = "Default Settings Minimum Instances";
            // 
            // GetProgramsMinInstancesUpDown
            // 
            this.GetProgramsMinInstancesUpDown.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.GetProgramsMinInstancesUpDown.Location = new System.Drawing.Point(8, 37);
            this.GetProgramsMinInstancesUpDown.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GetProgramsMinInstancesUpDown.Name = "GetProgramsMinInstancesUpDown";
            this.GetProgramsMinInstancesUpDown.Size = new System.Drawing.Size(88, 26);
            this.GetProgramsMinInstancesUpDown.TabIndex = 1;
            this.GetProgramsMinInstancesUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.GetProgramsMinInstancesUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // GetProgramsIncludeEnabledCheckBox
            // 
            this.GetProgramsIncludeEnabledCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.GetProgramsIncludeEnabledCheckBox.AutoSize = true;
            this.GetProgramsIncludeEnabledCheckBox.Checked = true;
            this.GetProgramsIncludeEnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.GetProgramsIncludeEnabledCheckBox.Location = new System.Drawing.Point(104, 38);
            this.GetProgramsIncludeEnabledCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.GetProgramsIncludeEnabledCheckBox.Name = "GetProgramsIncludeEnabledCheckBox";
            this.GetProgramsIncludeEnabledCheckBox.Padding = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.GetProgramsIncludeEnabledCheckBox.Size = new System.Drawing.Size(158, 24);
            this.GetProgramsIncludeEnabledCheckBox.TabIndex = 2;
            this.GetProgramsIncludeEnabledCheckBox.Text = "Include Enabled";
            this.GetProgramsIncludeEnabledCheckBox.ThreeState = true;
            this.GetProgramsIncludeEnabledCheckBox.UseVisualStyleBackColor = true;
            // 
            // InternetGroupBox
            // 
            this.InternetGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InternetGroupBox.AutoSize = true;
            this.InternetGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.InternetGroupBox.Controls.Add(this.InternetLayoutPanel);
            this.InternetGroupBox.Location = new System.Drawing.Point(4, 116);
            this.InternetGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetGroupBox.Name = "InternetGroupBox";
            this.InternetGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetGroupBox.Size = new System.Drawing.Size(958, 108);
            this.InternetGroupBox.TabIndex = 1;
            this.InternetGroupBox.TabStop = false;
            this.InternetGroupBox.Text = "Internet";
            // 
            // InternetLayoutPanel
            // 
            this.InternetLayoutPanel.AutoSize = true;
            this.InternetLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.InternetLayoutPanel.ColumnCount = 3;
            this.InternetLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.InternetLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.InternetLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.InternetLayoutPanel.Controls.Add(this.InternetCheckBoxesPanel, 0, 0);
            this.InternetLayoutPanel.Controls.Add(this.WebServiceUrlLabel, 0, 1);
            this.InternetLayoutPanel.Controls.Add(this.InternetDatabaseUrlComboBox, 1, 1);
            this.InternetLayoutPanel.Controls.Add(this.TestServiceButton, 2, 1);
            this.InternetLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.InternetLayoutPanel.Location = new System.Drawing.Point(4, 24);
            this.InternetLayoutPanel.Name = "InternetLayoutPanel";
            this.InternetLayoutPanel.RowCount = 2;
            this.InternetLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.InternetLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.InternetLayoutPanel.Size = new System.Drawing.Size(950, 79);
            this.InternetLayoutPanel.TabIndex = 0;
            // 
            // InternetCheckBoxesPanel
            // 
            this.InternetCheckBoxesPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.InternetCheckBoxesPanel.AutoSize = true;
            this.InternetCheckBoxesPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.InternetLayoutPanel.SetColumnSpan(this.InternetCheckBoxesPanel, 3);
            this.InternetCheckBoxesPanel.Controls.Add(this.InternetFeaturesCheckBox);
            this.InternetCheckBoxesPanel.Controls.Add(this.InternetAutoLoadCheckBox);
            this.InternetCheckBoxesPanel.Controls.Add(this.InternetAutoSaveCheckBox);
            this.InternetCheckBoxesPanel.Location = new System.Drawing.Point(0, 0);
            this.InternetCheckBoxesPanel.Margin = new System.Windows.Forms.Padding(0);
            this.InternetCheckBoxesPanel.Name = "InternetCheckBoxesPanel";
            this.InternetCheckBoxesPanel.Size = new System.Drawing.Size(649, 34);
            this.InternetCheckBoxesPanel.TabIndex = 0;
            this.InternetCheckBoxesPanel.WrapContents = false;
            // 
            // InternetFeaturesCheckBox
            // 
            this.InternetFeaturesCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.InternetFeaturesCheckBox.AutoSize = true;
            this.InternetFeaturesCheckBox.Location = new System.Drawing.Point(4, 5);
            this.InternetFeaturesCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetFeaturesCheckBox.Name = "InternetFeaturesCheckBox";
            this.InternetFeaturesCheckBox.Size = new System.Drawing.Size(213, 24);
            this.InternetFeaturesCheckBox.TabIndex = 0;
            this.InternetFeaturesCheckBox.Text = "Enable Internet Features";
            this.InternetFeaturesCheckBox.UseVisualStyleBackColor = true;
            // 
            // InternetAutoLoadCheckBox
            // 
            this.InternetAutoLoadCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.InternetAutoLoadCheckBox.AutoSize = true;
            this.InternetAutoLoadCheckBox.Location = new System.Drawing.Point(225, 5);
            this.InternetAutoLoadCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetAutoLoadCheckBox.Name = "InternetAutoLoadCheckBox";
            this.InternetAutoLoadCheckBox.Size = new System.Drawing.Size(215, 24);
            this.InternetAutoLoadCheckBox.TabIndex = 1;
            this.InternetAutoLoadCheckBox.Text = "Load Settings from Cloud";
            this.InternetAutoLoadCheckBox.UseVisualStyleBackColor = true;
            // 
            // InternetAutoSaveCheckBox
            // 
            this.InternetAutoSaveCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.InternetAutoSaveCheckBox.AutoSize = true;
            this.InternetAutoSaveCheckBox.Location = new System.Drawing.Point(448, 5);
            this.InternetAutoSaveCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetAutoSaveCheckBox.Name = "InternetAutoSaveCheckBox";
            this.InternetAutoSaveCheckBox.Size = new System.Drawing.Size(197, 24);
            this.InternetAutoSaveCheckBox.TabIndex = 2;
            this.InternetAutoSaveCheckBox.Text = "Save Settings to Cloud";
            this.InternetAutoSaveCheckBox.UseVisualStyleBackColor = true;
            // 
            // WebServiceUrlLabel
            // 
            this.WebServiceUrlLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.WebServiceUrlLabel.AutoSize = true;
            this.WebServiceUrlLabel.Location = new System.Drawing.Point(4, 46);
            this.WebServiceUrlLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.WebServiceUrlLabel.Name = "WebServiceUrlLabel";
            this.WebServiceUrlLabel.Size = new System.Drawing.Size(135, 20);
            this.WebServiceUrlLabel.TabIndex = 1;
            this.WebServiceUrlLabel.Text = "Web Service URL";
            // 
            // InternetDatabaseUrlComboBox
            // 
            this.InternetDatabaseUrlComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.InternetDatabaseUrlComboBox.FormattingEnabled = true;
            this.InternetDatabaseUrlComboBox.Items.AddRange(new object[] {
            "https://www.x360ce.com/webservices/x360ce.asmx",
            "https://localhost:44360/webservices/x360ce.asmx"});
            this.InternetDatabaseUrlComboBox.Location = new System.Drawing.Point(147, 42);
            this.InternetDatabaseUrlComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.InternetDatabaseUrlComboBox.Name = "InternetDatabaseUrlComboBox";
            this.InternetDatabaseUrlComboBox.Size = new System.Drawing.Size(679, 28);
            this.InternetDatabaseUrlComboBox.TabIndex = 2;
            // 
            // TestServiceButton
            // 
            this.TestServiceButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.TestServiceButton.AutoSize = true;
            this.TestServiceButton.Location = new System.Drawing.Point(834, 39);
            this.TestServiceButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.TestServiceButton.Name = "TestServiceButton";
            this.TestServiceButton.Size = new System.Drawing.Size(112, 35);
            this.TestServiceButton.TabIndex = 3;
            this.TestServiceButton.Text = "Test";
            this.TestServiceButton.UseVisualStyleBackColor = true;
            this.TestServiceButton.Click += new System.EventHandler(this.TestServiceButton_Click);
            // 
            // OnlineAccountGroupBox
            // 
            this.OnlineAccountGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OnlineAccountGroupBox.AutoSize = true;
            this.OnlineAccountGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OnlineAccountGroupBox.Controls.Add(this.OnlineAccountLayoutPanel);
            this.OnlineAccountGroupBox.Location = new System.Drawing.Point(4, 234);
            this.OnlineAccountGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.OnlineAccountGroupBox.Name = "OnlineAccountGroupBox";
            this.OnlineAccountGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.OnlineAccountGroupBox.Size = new System.Drawing.Size(958, 182);
            this.OnlineAccountGroupBox.TabIndex = 2;
            this.OnlineAccountGroupBox.TabStop = false;
            this.OnlineAccountGroupBox.Text = "Online Account - Anonymous Computer ID and Profile ID will be used by default";
            // 
            // OnlineAccountLayoutPanel
            // 
            this.OnlineAccountLayoutPanel.AutoSize = true;
            this.OnlineAccountLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OnlineAccountLayoutPanel.ColumnCount = 3;
            this.OnlineAccountLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.OnlineAccountLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OnlineAccountLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.OnlineAccountLayoutPanel.Controls.Add(this.ComputerDiskLabel, 0, 0);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ComputerDiskTextBox, 1, 0);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ComputerIdLabel, 0, 1);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ComputerIdTextBox, 1, 1);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ProfilePathLabel, 0, 2);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ProfilePathTextBox, 1, 2);
            this.OnlineAccountLayoutPanel.Controls.Add(this.OpenSettingsFolderButton, 2, 2);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ProfileIdLabel, 0, 3);
            this.OnlineAccountLayoutPanel.Controls.Add(this.ProfileIdTextBox, 1, 3);
            this.OnlineAccountLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.OnlineAccountLayoutPanel.Location = new System.Drawing.Point(4, 24);
            this.OnlineAccountLayoutPanel.Name = "OnlineAccountLayoutPanel";
            this.OnlineAccountLayoutPanel.RowCount = 4;
            this.OnlineAccountLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLayoutPanel.Size = new System.Drawing.Size(950, 153);
            this.OnlineAccountLayoutPanel.TabIndex = 0;
            // 
            // ComputerDiskLabel
            // 
            this.ComputerDiskLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ComputerDiskLabel.AutoSize = true;
            this.ComputerDiskLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ComputerDiskLabel.Location = new System.Drawing.Point(4, 8);
            this.ComputerDiskLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ComputerDiskLabel.Name = "ComputerDiskLabel";
            this.ComputerDiskLabel.Size = new System.Drawing.Size(114, 20);
            this.ComputerDiskLabel.TabIndex = 0;
            this.ComputerDiskLabel.Text = "Computer Disk";
            // 
            // ComputerDiskTextBox
            // 
            this.ComputerDiskTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComputerDiskTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.OnlineAccountLayoutPanel.SetColumnSpan(this.ComputerDiskTextBox, 2);
            this.ComputerDiskTextBox.Location = new System.Drawing.Point(126, 5);
            this.ComputerDiskTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ComputerDiskTextBox.Name = "ComputerDiskTextBox";
            this.ComputerDiskTextBox.Size = new System.Drawing.Size(820, 26);
            this.ComputerDiskTextBox.TabIndex = 1;
            // 
            // ComputerIdLabel
            // 
            this.ComputerIdLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ComputerIdLabel.AutoSize = true;
            this.ComputerIdLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ComputerIdLabel.Location = new System.Drawing.Point(4, 44);
            this.ComputerIdLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ComputerIdLabel.Name = "ComputerIdLabel";
            this.ComputerIdLabel.Size = new System.Drawing.Size(100, 20);
            this.ComputerIdLabel.TabIndex = 2;
            this.ComputerIdLabel.Text = "Computer ID";
            // 
            // ComputerIdTextBox
            // 
            this.ComputerIdTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ComputerIdTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.ComputerIdTextBox.Location = new System.Drawing.Point(126, 41);
            this.ComputerIdTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ComputerIdTextBox.Name = "ComputerIdTextBox";
            this.ComputerIdTextBox.Size = new System.Drawing.Size(700, 26);
            this.ComputerIdTextBox.TabIndex = 3;
            // 
            // ProfilePathLabel
            // 
            this.ProfilePathLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ProfilePathLabel.AutoSize = true;
            this.ProfilePathLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ProfilePathLabel.Location = new System.Drawing.Point(4, 84);
            this.ProfilePathLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ProfilePathLabel.Name = "ProfilePathLabel";
            this.ProfilePathLabel.Size = new System.Drawing.Size(90, 20);
            this.ProfilePathLabel.TabIndex = 4;
            this.ProfilePathLabel.Text = "Profile Path";
            // 
            // ProfilePathTextBox
            // 
            this.ProfilePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ProfilePathTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.ProfilePathTextBox.Location = new System.Drawing.Point(126, 81);
            this.ProfilePathTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ProfilePathTextBox.Name = "ProfilePathTextBox";
            this.ProfilePathTextBox.Size = new System.Drawing.Size(700, 26);
            this.ProfilePathTextBox.TabIndex = 5;
            // 
            // OpenSettingsFolderButton
            // 
            this.OpenSettingsFolderButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.OpenSettingsFolderButton.AutoSize = true;
            this.OpenSettingsFolderButton.Image = global::x360ce.App.Properties.Resources.folder_16x16;
            this.OpenSettingsFolderButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.OpenSettingsFolderButton.Location = new System.Drawing.Point(834, 77);
            this.OpenSettingsFolderButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.OpenSettingsFolderButton.Name = "OpenSettingsFolderButton";
            this.OpenSettingsFolderButton.Size = new System.Drawing.Size(112, 35);
            this.OpenSettingsFolderButton.TabIndex = 6;
            this.OpenSettingsFolderButton.Text = "Open";
            this.OpenSettingsFolderButton.UseVisualStyleBackColor = true;
            this.OpenSettingsFolderButton.Click += new System.EventHandler(this.OpenSettingsFolderButton_Click);
            // 
            // ProfileIdLabel
            // 
            this.ProfileIdLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ProfileIdLabel.AutoSize = true;
            this.ProfileIdLabel.BackColor = System.Drawing.SystemColors.Control;
            this.ProfileIdLabel.Location = new System.Drawing.Point(4, 125);
            this.ProfileIdLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ProfileIdLabel.Name = "ProfileIdLabel";
            this.ProfileIdLabel.Size = new System.Drawing.Size(74, 20);
            this.ProfileIdLabel.TabIndex = 7;
            this.ProfileIdLabel.Text = "Profile ID";
            // 
            // ProfileIdTextBox
            // 
            this.ProfileIdTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ProfileIdTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.ProfileIdTextBox.Location = new System.Drawing.Point(126, 122);
            this.ProfileIdTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ProfileIdTextBox.Name = "ProfileIdTextBox";
            this.ProfileIdTextBox.Size = new System.Drawing.Size(700, 26);
            this.ProfileIdTextBox.TabIndex = 8;
            // 
            // OnlineAccountLoginGroupBox
            // 
            this.OnlineAccountLoginGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OnlineAccountLoginGroupBox.AutoSize = true;
            this.OnlineAccountLoginGroupBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OnlineAccountLoginGroupBox.Controls.Add(this.OnlineAccountLoginLayoutPanel);
            this.OnlineAccountLoginGroupBox.Location = new System.Drawing.Point(4, 426);
            this.OnlineAccountLoginGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.OnlineAccountLoginGroupBox.Name = "OnlineAccountLoginGroupBox";
            this.OnlineAccountLoginGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.OnlineAccountLoginGroupBox.Size = new System.Drawing.Size(958, 110);
            this.OnlineAccountLoginGroupBox.TabIndex = 3;
            this.OnlineAccountLoginGroupBox.TabStop = false;
            this.OnlineAccountLoginGroupBox.Text = "Online Account - Login";
            this.OnlineAccountLoginGroupBox.Visible = false;
            // 
            // OnlineAccountLoginLayoutPanel
            // 
            this.OnlineAccountLoginLayoutPanel.AutoSize = true;
            this.OnlineAccountLoginLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OnlineAccountLoginLayoutPanel.ColumnCount = 3;
            this.OnlineAccountLoginLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.OnlineAccountLoginLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OnlineAccountLoginLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.OnlineAccountLoginLayoutPanel.Controls.Add(this.UsernameLabel, 0, 0);
            this.OnlineAccountLoginLayoutPanel.Controls.Add(this.UsernameTextBox, 1, 0);
            this.OnlineAccountLoginLayoutPanel.Controls.Add(this.PasswordLabel, 0, 1);
            this.OnlineAccountLoginLayoutPanel.Controls.Add(this.PasswordTextBox, 1, 1);
            this.OnlineAccountLoginLayoutPanel.Controls.Add(this.OnlineAccountLoginButtonsPanel, 2, 1);
            this.OnlineAccountLoginLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.OnlineAccountLoginLayoutPanel.Location = new System.Drawing.Point(4, 24);
            this.OnlineAccountLoginLayoutPanel.Name = "OnlineAccountLoginLayoutPanel";
            this.OnlineAccountLoginLayoutPanel.RowCount = 2;
            this.OnlineAccountLoginLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLoginLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OnlineAccountLoginLayoutPanel.Size = new System.Drawing.Size(950, 81);
            this.OnlineAccountLoginLayoutPanel.TabIndex = 0;
            // 
            // UsernameLabel
            // 
            this.UsernameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.UsernameLabel.AutoSize = true;
            this.UsernameLabel.BackColor = System.Drawing.SystemColors.Control;
            this.UsernameLabel.Location = new System.Drawing.Point(4, 8);
            this.UsernameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.UsernameLabel.Name = "UsernameLabel";
            this.UsernameLabel.Size = new System.Drawing.Size(141, 20);
            this.UsernameLabel.TabIndex = 0;
            this.UsernameLabel.Text = "Username (E-mail)";
            // 
            // UsernameTextBox
            // 
            this.UsernameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.UsernameTextBox.Location = new System.Drawing.Point(153, 5);
            this.UsernameTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.UsernameTextBox.Name = "UsernameTextBox";
            this.UsernameTextBox.Size = new System.Drawing.Size(433, 26);
            this.UsernameTextBox.TabIndex = 1;
            // 
            // PasswordLabel
            // 
            this.PasswordLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PasswordLabel.AutoSize = true;
            this.PasswordLabel.BackColor = System.Drawing.SystemColors.Control;
            this.PasswordLabel.Location = new System.Drawing.Point(4, 48);
            this.PasswordLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.PasswordLabel.Name = "PasswordLabel";
            this.PasswordLabel.Size = new System.Drawing.Size(78, 20);
            this.PasswordLabel.TabIndex = 2;
            this.PasswordLabel.Text = "Password";
            // 
            // PasswordTextBox
            // 
            this.PasswordTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.PasswordTextBox.Location = new System.Drawing.Point(153, 45);
            this.PasswordTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.Size = new System.Drawing.Size(433, 26);
            this.PasswordTextBox.TabIndex = 3;
            // 
            // OnlineAccountLoginButtonsPanel
            // 
            this.OnlineAccountLoginButtonsPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.OnlineAccountLoginButtonsPanel.AutoSize = true;
            this.OnlineAccountLoginButtonsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OnlineAccountLoginButtonsPanel.Controls.Add(this.LoginButton);
            this.OnlineAccountLoginButtonsPanel.Controls.Add(this.CreateButton);
            this.OnlineAccountLoginButtonsPanel.Controls.Add(this.ResetButton);
            this.OnlineAccountLoginButtonsPanel.Location = new System.Drawing.Point(590, 36);
            this.OnlineAccountLoginButtonsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.OnlineAccountLoginButtonsPanel.Name = "OnlineAccountLoginButtonsPanel";
            this.OnlineAccountLoginButtonsPanel.Size = new System.Drawing.Size(360, 45);
            this.OnlineAccountLoginButtonsPanel.TabIndex = 4;
            this.OnlineAccountLoginButtonsPanel.WrapContents = false;
            // 
            // LoginButton
            // 
            this.LoginButton.AutoSize = true;
            this.LoginButton.Location = new System.Drawing.Point(4, 5);
            this.LoginButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.LoginButton.Name = "LoginButton";
            this.LoginButton.Size = new System.Drawing.Size(112, 35);
            this.LoginButton.TabIndex = 0;
            this.LoginButton.Text = "Log In";
            this.LoginButton.UseVisualStyleBackColor = true;
            this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
            // 
            // CreateButton
            // 
            this.CreateButton.AutoSize = true;
            this.CreateButton.Location = new System.Drawing.Point(124, 5);
            this.CreateButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(112, 35);
            this.CreateButton.TabIndex = 1;
            this.CreateButton.Text = "Create...";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // ResetButton
            // 
            this.ResetButton.AutoSize = true;
            this.ResetButton.Location = new System.Drawing.Point(244, 5);
            this.ResetButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ResetButton.Name = "ResetButton";
            this.ResetButton.Size = new System.Drawing.Size(112, 35);
            this.ResetButton.TabIndex = 2;
            this.ResetButton.Text = "Reset...";
            this.ResetButton.UseVisualStyleBackColor = true;
            this.ResetButton.Click += new System.EventHandler(this.ResetButton_Click);
            // 
            // OptionsInternetUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainLayoutPanel);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "OptionsInternetUserControl";
            this.Size = new System.Drawing.Size(966, 631);
            this.MainLayoutPanel.ResumeLayout(false);
            this.MainLayoutPanel.PerformLayout();
            this.GamesGroupBox.ResumeLayout(false);
            this.GamesGroupBox.PerformLayout();
            this.GamesLayoutPanel.ResumeLayout(false);
            this.GamesLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GetProgramsMinInstancesUpDown)).EndInit();
            this.InternetGroupBox.ResumeLayout(false);
            this.InternetGroupBox.PerformLayout();
            this.InternetLayoutPanel.ResumeLayout(false);
            this.InternetLayoutPanel.PerformLayout();
            this.InternetCheckBoxesPanel.ResumeLayout(false);
            this.InternetCheckBoxesPanel.PerformLayout();
            this.OnlineAccountGroupBox.ResumeLayout(false);
            this.OnlineAccountGroupBox.PerformLayout();
            this.OnlineAccountLayoutPanel.ResumeLayout(false);
            this.OnlineAccountLayoutPanel.PerformLayout();
            this.OnlineAccountLoginGroupBox.ResumeLayout(false);
            this.OnlineAccountLoginGroupBox.PerformLayout();
            this.OnlineAccountLoginLayoutPanel.ResumeLayout(false);
            this.OnlineAccountLoginLayoutPanel.PerformLayout();
            this.OnlineAccountLoginButtonsPanel.ResumeLayout(false);
            this.OnlineAccountLoginButtonsPanel.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
		private System.Windows.Forms.GroupBox GamesGroupBox;
		private System.Windows.Forms.TableLayoutPanel GamesLayoutPanel;
		public System.Windows.Forms.CheckBox GetProgramsIncludeEnabledCheckBox;
		private System.Windows.Forms.Label MinimumInstanceCountLabel;
		public System.Windows.Forms.NumericUpDown GetProgramsMinInstancesUpDown;
		private System.Windows.Forms.GroupBox InternetGroupBox;
		private System.Windows.Forms.TableLayoutPanel InternetLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel InternetCheckBoxesPanel;
		public System.Windows.Forms.CheckBox InternetAutoSaveCheckBox;
		public System.Windows.Forms.ComboBox InternetDatabaseUrlComboBox;
		private System.Windows.Forms.Label WebServiceUrlLabel;
		public System.Windows.Forms.Button TestServiceButton;
		public System.Windows.Forms.CheckBox InternetAutoLoadCheckBox;
		public System.Windows.Forms.CheckBox InternetFeaturesCheckBox;
		private System.Windows.Forms.GroupBox OnlineAccountGroupBox;
		private System.Windows.Forms.TableLayoutPanel OnlineAccountLayoutPanel;
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
		private System.Windows.Forms.GroupBox OnlineAccountLoginGroupBox;
		private System.Windows.Forms.TableLayoutPanel OnlineAccountLoginLayoutPanel;
		private System.Windows.Forms.FlowLayoutPanel OnlineAccountLoginButtonsPanel;
	}
}
