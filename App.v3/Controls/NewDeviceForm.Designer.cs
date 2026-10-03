namespace x360ce.App.Controls
{
	partial class NewDeviceForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewDeviceForm));
			this.SearchLabel = new System.Windows.Forms.Label();
			this.BrowseLabel = new System.Windows.Forms.Label();
			this.CloseButton = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.WizzardTabControl = new System.Windows.Forms.TabControl();
			this.Step1TabPage = new System.Windows.Forms.TabPage();
			this.SearchTheInternetCheckBox = new System.Windows.Forms.CheckBox();
			this.IncludeSubfoldersCheckBox = new System.Windows.Forms.CheckBox();
			this.DescriptionLabel = new System.Windows.Forms.Label();
			this.FolderPathTextBox = new System.Windows.Forms.TextBox();
			this.BrowseButton = new System.Windows.Forms.Button();
			this.BrowsePictureBox = new System.Windows.Forms.PictureBox();
			this.SearchPictureBox = new System.Windows.Forms.PictureBox();
			this.BrowseRadioButton = new System.Windows.Forms.RadioButton();
			this.SearchRadioButton = new System.Windows.Forms.RadioButton();
			this.Step2TabPage = new System.Windows.Forms.TabPage();
			this.InternetPictureBox = new System.Windows.Forms.PictureBox();
			this.LocalPictureBox = new System.Windows.Forms.PictureBox();
			this.ResultsLabel = new System.Windows.Forms.Label();
			this.InternetLabel = new System.Windows.Forms.Label();
			this.LocalLabel = new System.Windows.Forms.Label();
			this.SettingsListTabControl = new System.Windows.Forms.TabControl();
			this.MySettingsTabPage = new System.Windows.Forms.TabPage();
			this.MySettingsDataGridView = new System.Windows.Forms.DataGridView();
			this.DateColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MyFileColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.NextButton = new System.Windows.Forms.Button();
			this.SettingsFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.BackButton = new System.Windows.Forms.Button();
			this.WizzardTabControl.SuspendLayout();
			this.Step1TabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.BrowsePictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SearchPictureBox)).BeginInit();
			this.Step2TabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.InternetPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.LocalPictureBox)).BeginInit();
			this.SettingsListTabControl.SuspendLayout();
			this.MySettingsTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).BeginInit();
			this.SuspendLayout();
			// 
			// SearchLabel
			// 
			this.SearchLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SearchLabel.AutoSize = true;
			this.SearchLabel.Location = new System.Drawing.Point(45, 56);
			this.SearchLabel.Margin = new System.Windows.Forms.Padding(3);
			this.SearchLabel.Name = "SearchLabel";
			this.SearchLabel.Size = new System.Drawing.Size(384, 13);
			this.SearchLabel.TabIndex = 0;
			this.SearchLabel.Text = "Application will search your computer for the best settings match for your device" +
    ".";
			this.SearchLabel.Click += new System.EventHandler(this.SearchLabel_Click);
			// 
			// BrowseLabel
			// 
			this.BrowseLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseLabel.AutoSize = true;
			this.BrowseLabel.Location = new System.Drawing.Point(45, 121);
			this.BrowseLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.BrowseLabel.Name = "BrowseLabel";
			this.BrowseLabel.Size = new System.Drawing.Size(340, 13);
			this.BrowseLabel.TabIndex = 0;
			this.BrowseLabel.Text = "Locate and install settings manually. Search for settings in this location:";
			this.BrowseLabel.Click += new System.EventHandler(this.BrowseLabel_Click);
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(456, 259);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 4;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 3);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(0, 13);
			this.label5.TabIndex = 0;
			// 
			// WizzardTabControl
			// 
			this.WizzardTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WizzardTabControl.Controls.Add(this.Step1TabPage);
			this.WizzardTabControl.Controls.Add(this.Step2TabPage);
			this.WizzardTabControl.Location = new System.Drawing.Point(12, 12);
			this.WizzardTabControl.Name = "WizzardTabControl";
			this.WizzardTabControl.SelectedIndex = 0;
			this.WizzardTabControl.Size = new System.Drawing.Size(519, 241);
			this.WizzardTabControl.TabIndex = 2;
			// 
			// Step1TabPage
			// 
			this.Step1TabPage.Controls.Add(this.SearchTheInternetCheckBox);
			this.Step1TabPage.Controls.Add(this.IncludeSubfoldersCheckBox);
			this.Step1TabPage.Controls.Add(this.DescriptionLabel);
			this.Step1TabPage.Controls.Add(this.FolderPathTextBox);
			this.Step1TabPage.Controls.Add(this.BrowseButton);
			this.Step1TabPage.Controls.Add(this.BrowsePictureBox);
			this.Step1TabPage.Controls.Add(this.SearchPictureBox);
			this.Step1TabPage.Controls.Add(this.BrowseLabel);
			this.Step1TabPage.Controls.Add(this.BrowseRadioButton);
			this.Step1TabPage.Controls.Add(this.SearchRadioButton);
			this.Step1TabPage.Controls.Add(this.SearchLabel);
			this.Step1TabPage.Controls.Add(this.label5);
			this.Step1TabPage.Location = new System.Drawing.Point(4, 22);
			this.Step1TabPage.Name = "Step1TabPage";
			this.Step1TabPage.Padding = new System.Windows.Forms.Padding(3);
			this.Step1TabPage.Size = new System.Drawing.Size(511, 215);
			this.Step1TabPage.TabIndex = 0;
			this.Step1TabPage.Text = "Step 1";
			this.Step1TabPage.UseVisualStyleBackColor = true;
			// 
			// SearchTheInternetCheckBox
			// 
			this.SearchTheInternetCheckBox.AutoSize = true;
			this.SearchTheInternetCheckBox.Location = new System.Drawing.Point(48, 75);
			this.SearchTheInternetCheckBox.Name = "SearchTheInternetCheckBox";
			this.SearchTheInternetCheckBox.Size = new System.Drawing.Size(117, 17);
			this.SearchTheInternetCheckBox.TabIndex = 4;
			this.SearchTheInternetCheckBox.Text = "Search the Internet";
			this.SearchTheInternetCheckBox.UseVisualStyleBackColor = true;
			this.SearchTheInternetCheckBox.CheckedChanged += new System.EventHandler(this.SearchTheInternetCheckBox_CheckedChanged);
			// 
			// IncludeSubfoldersCheckBox
			// 
			this.IncludeSubfoldersCheckBox.AutoSize = true;
			this.IncludeSubfoldersCheckBox.Checked = true;
			this.IncludeSubfoldersCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.IncludeSubfoldersCheckBox.Location = new System.Drawing.Point(48, 163);
			this.IncludeSubfoldersCheckBox.Name = "IncludeSubfoldersCheckBox";
			this.IncludeSubfoldersCheckBox.Size = new System.Drawing.Size(112, 17);
			this.IncludeSubfoldersCheckBox.TabIndex = 4;
			this.IncludeSubfoldersCheckBox.Text = "Include subfolders";
			this.IncludeSubfoldersCheckBox.UseVisualStyleBackColor = true;
			// 
			// DescriptionLabel
			// 
			this.DescriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DescriptionLabel.Location = new System.Drawing.Point(3, 7);
			this.DescriptionLabel.Name = "DescriptionLabel";
			this.DescriptionLabel.Size = new System.Drawing.Size(499, 23);
			this.DescriptionLabel.TabIndex = 3;
			this.DescriptionLabel.Text = "New device with unique instance id was detected. How do you want to search for se" +
    "ttings?";
			// 
			// FolderPathTextBox
			// 
			this.FolderPathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FolderPathTextBox.Location = new System.Drawing.Point(48, 137);
			this.FolderPathTextBox.Name = "FolderPathTextBox";
			this.FolderPathTextBox.Size = new System.Drawing.Size(376, 20);
			this.FolderPathTextBox.TabIndex = 2;
			// 
			// BrowseButton
			// 
			this.BrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseButton.Location = new System.Drawing.Point(430, 135);
			this.BrowseButton.Name = "BrowseButton";
			this.BrowseButton.Size = new System.Drawing.Size(75, 23);
			this.BrowseButton.TabIndex = 2;
			this.BrowseButton.Text = "Browse...";
			this.BrowseButton.UseVisualStyleBackColor = true;
			this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
			// 
			// BrowsePictureBox
			// 
			this.BrowsePictureBox.Image = global::x360ce.App.Properties.Resources.arrow_right_16x16;
			this.BrowsePictureBox.Location = new System.Drawing.Point(6, 98);
			this.BrowsePictureBox.Name = "BrowsePictureBox";
			this.BrowsePictureBox.Size = new System.Drawing.Size(16, 16);
			this.BrowsePictureBox.TabIndex = 1;
			this.BrowsePictureBox.TabStop = false;
			// 
			// SearchPictureBox
			// 
			this.SearchPictureBox.Image = global::x360ce.App.Properties.Resources.arrow_right_16x16;
			this.SearchPictureBox.Location = new System.Drawing.Point(6, 33);
			this.SearchPictureBox.Name = "SearchPictureBox";
			this.SearchPictureBox.Size = new System.Drawing.Size(16, 16);
			this.SearchPictureBox.TabIndex = 1;
			this.SearchPictureBox.TabStop = false;
			// 
			// BrowseRadioButton
			// 
			this.BrowseRadioButton.AutoSize = true;
			this.BrowseRadioButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BrowseRadioButton.Location = new System.Drawing.Point(28, 98);
			this.BrowseRadioButton.Name = "BrowseRadioButton";
			this.BrowseRadioButton.Size = new System.Drawing.Size(208, 17);
			this.BrowseRadioButton.TabIndex = 1;
			this.BrowseRadioButton.Text = "Browse my computer for settings";
			this.BrowseRadioButton.UseVisualStyleBackColor = true;
			this.BrowseRadioButton.CheckedChanged += new System.EventHandler(this.BrowseRadioButton_CheckedChanged);
			// 
			// SearchRadioButton
			// 
			this.SearchRadioButton.AutoSize = true;
			this.SearchRadioButton.Checked = true;
			this.SearchRadioButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SearchRadioButton.Location = new System.Drawing.Point(28, 33);
			this.SearchRadioButton.Name = "SearchRadioButton";
			this.SearchRadioButton.Size = new System.Drawing.Size(210, 17);
			this.SearchRadioButton.TabIndex = 1;
			this.SearchRadioButton.TabStop = true;
			this.SearchRadioButton.Text = "Search automatically for settings";
			this.SearchRadioButton.UseVisualStyleBackColor = true;
			this.SearchRadioButton.CheckedChanged += new System.EventHandler(this.SearchRadioButton_CheckedChanged);
			// 
			// Step2TabPage
			// 
			this.Step2TabPage.Controls.Add(this.InternetPictureBox);
			this.Step2TabPage.Controls.Add(this.LocalPictureBox);
			this.Step2TabPage.Controls.Add(this.ResultsLabel);
			this.Step2TabPage.Controls.Add(this.InternetLabel);
			this.Step2TabPage.Controls.Add(this.LocalLabel);
			this.Step2TabPage.Controls.Add(this.SettingsListTabControl);
			this.Step2TabPage.Controls.Add(this.BusyLoadingCircle);
			this.Step2TabPage.Location = new System.Drawing.Point(4, 22);
			this.Step2TabPage.Name = "Step2TabPage";
			this.Step2TabPage.Padding = new System.Windows.Forms.Padding(3);
			this.Step2TabPage.Size = new System.Drawing.Size(511, 215);
			this.Step2TabPage.TabIndex = 1;
			this.Step2TabPage.Text = "Step 2";
			this.Step2TabPage.UseVisualStyleBackColor = true;
			// 
			// InternetPictureBox
			// 
			this.InternetPictureBox.Image = global::x360ce.App.Properties.Resources.check_disabled_16x16;
			this.InternetPictureBox.Location = new System.Drawing.Point(6, 28);
			this.InternetPictureBox.Name = "InternetPictureBox";
			this.InternetPictureBox.Size = new System.Drawing.Size(16, 16);
			this.InternetPictureBox.TabIndex = 22;
			this.InternetPictureBox.TabStop = false;
			// 
			// LocalPictureBox
			// 
			this.LocalPictureBox.Image = global::x360ce.App.Properties.Resources.check_disabled_16x16;
			this.LocalPictureBox.Location = new System.Drawing.Point(6, 6);
			this.LocalPictureBox.Name = "LocalPictureBox";
			this.LocalPictureBox.Size = new System.Drawing.Size(16, 16);
			this.LocalPictureBox.TabIndex = 22;
			this.LocalPictureBox.TabStop = false;
			// 
			// ResultsLabel
			// 
			this.ResultsLabel.AutoSize = true;
			this.ResultsLabel.Location = new System.Drawing.Point(7, 51);
			this.ResultsLabel.Name = "ResultsLabel";
			this.ResultsLabel.Size = new System.Drawing.Size(74, 13);
			this.ResultsLabel.TabIndex = 20;
			this.ResultsLabel.Text = "[ResultsLabel]";
			// 
			// InternetLabel
			// 
			this.InternetLabel.AutoSize = true;
			this.InternetLabel.Location = new System.Drawing.Point(28, 31);
			this.InternetLabel.Name = "InternetLabel";
			this.InternetLabel.Size = new System.Drawing.Size(149, 13);
			this.InternetLabel.TabIndex = 20;
			this.InternetLabel.Text = "Searching online for settings...";
			// 
			// LocalLabel
			// 
			this.LocalLabel.AutoSize = true;
			this.LocalLabel.Location = new System.Drawing.Point(28, 9);
			this.LocalLabel.Name = "LocalLabel";
			this.LocalLabel.Size = new System.Drawing.Size(150, 13);
			this.LocalLabel.TabIndex = 20;
			this.LocalLabel.Text = "Searching locally for settings...";
			// 
			// SettingsListTabControl
			// 
			this.SettingsListTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SettingsListTabControl.Controls.Add(this.MySettingsTabPage);
			this.SettingsListTabControl.Location = new System.Drawing.Point(6, 78);
			this.SettingsListTabControl.Name = "SettingsListTabControl";
			this.SettingsListTabControl.SelectedIndex = 0;
			this.SettingsListTabControl.Size = new System.Drawing.Size(496, 131);
			this.SettingsListTabControl.TabIndex = 19;
			// 
			// MySettingsTabPage
			// 
			this.MySettingsTabPage.Controls.Add(this.MySettingsDataGridView);
			this.MySettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.MySettingsTabPage.Name = "MySettingsTabPage";
			this.MySettingsTabPage.Size = new System.Drawing.Size(488, 105);
			this.MySettingsTabPage.TabIndex = 0;
			this.MySettingsTabPage.Text = "Search Results";
			this.MySettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// MySettingsDataGridView
			// 
			this.MySettingsDataGridView.AllowUserToAddRows = false;
			this.MySettingsDataGridView.AllowUserToDeleteRows = false;
			this.MySettingsDataGridView.AllowUserToResizeRows = false;
			this.MySettingsDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.MySettingsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.MySettingsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MySettingsDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.MySettingsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MySettingsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DateColumn,
            this.MyFileColumn});
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.MySettingsDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
			this.MySettingsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MySettingsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.MySettingsDataGridView.Location = new System.Drawing.Point(0, 0);
			this.MySettingsDataGridView.MultiSelect = false;
			this.MySettingsDataGridView.Name = "MySettingsDataGridView";
			this.MySettingsDataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MySettingsDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.MySettingsDataGridView.RowHeadersVisible = false;
			this.MySettingsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MySettingsDataGridView.Size = new System.Drawing.Size(488, 105);
			this.MySettingsDataGridView.TabIndex = 0;
			// 
			// DateColumn
			// 
			this.DateColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DateColumn.DataPropertyName = "DateUpdated";
			dataGridViewCellStyle2.Format = "yyyy-MM-dd HH:mm";
			dataGridViewCellStyle2.NullValue = null;
			this.DateColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.DateColumn.HeaderText = "Date";
			this.DateColumn.Name = "DateColumn";
			this.DateColumn.ReadOnly = true;
			this.DateColumn.Width = 55;
			// 
			// MyFileColumn
			// 
			this.MyFileColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.MyFileColumn.DataPropertyName = "FileName";
			this.MyFileColumn.HeaderText = "Source";
			this.MyFileColumn.Name = "MyFileColumn";
			this.MyFileColumn.ReadOnly = true;
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(454, 6);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 21;
			// 
			// NextButton
			// 
			this.NextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.NextButton.Location = new System.Drawing.Point(375, 259);
			this.NextButton.Name = "NextButton";
			this.NextButton.Size = new System.Drawing.Size(75, 23);
			this.NextButton.TabIndex = 3;
			this.NextButton.Text = "Next >";
			this.NextButton.UseVisualStyleBackColor = true;
			this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
			// 
			// BackButton
			// 
			this.BackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.BackButton.Location = new System.Drawing.Point(294, 259);
			this.BackButton.Name = "BackButton";
			this.BackButton.Size = new System.Drawing.Size(75, 23);
			this.BackButton.TabIndex = 3;
			this.BackButton.Text = "< Back";
			this.BackButton.UseVisualStyleBackColor = true;
			this.BackButton.Click += new System.EventHandler(this.BackButton_Click);
			// 
			// NewDeviceForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CloseButton;
			this.ClientSize = new System.Drawing.Size(543, 288);
			this.Controls.Add(this.WizzardTabControl);
			this.Controls.Add(this.BackButton);
			this.Controls.Add(this.NextButton);
			this.Controls.Add(this.CloseButton);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(556, 326);
			this.Name = "NewDeviceForm";
			this.Text = "New Device Detected - {0}";
			this.Load += new System.EventHandler(this.NewDeviceForm_Load);
			this.WizzardTabControl.ResumeLayout(false);
			this.Step1TabPage.ResumeLayout(false);
			this.Step1TabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.BrowsePictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SearchPictureBox)).EndInit();
			this.Step2TabPage.ResumeLayout(false);
			this.Step2TabPage.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.InternetPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.LocalPictureBox)).EndInit();
			this.SettingsListTabControl.ResumeLayout(false);
			this.MySettingsTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.MySettingsDataGridView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		System.Windows.Forms.PictureBox SearchPictureBox;
		System.Windows.Forms.Label SearchLabel;
		System.Windows.Forms.Label BrowseLabel;
		System.Windows.Forms.Button CloseButton;
		System.Windows.Forms.Label label5;
		System.Windows.Forms.TabControl WizzardTabControl;
		System.Windows.Forms.TabPage Step1TabPage;
		System.Windows.Forms.TabPage Step2TabPage;
		System.Windows.Forms.PictureBox BrowsePictureBox;
		System.Windows.Forms.RadioButton BrowseRadioButton;
		System.Windows.Forms.RadioButton SearchRadioButton;
		System.Windows.Forms.Button NextButton;
		System.Windows.Forms.TextBox FolderPathTextBox;
		System.Windows.Forms.Button BrowseButton;
		System.Windows.Forms.Label DescriptionLabel;
		System.Windows.Forms.FolderBrowserDialog SettingsFolderBrowserDialog;
		System.Windows.Forms.TabControl SettingsListTabControl;
		System.Windows.Forms.TabPage MySettingsTabPage;
		System.Windows.Forms.DataGridView MySettingsDataGridView;
		System.Windows.Forms.CheckBox IncludeSubfoldersCheckBox;
		System.Windows.Forms.Label ResultsLabel;
		System.Windows.Forms.Label LocalLabel;
		MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		System.Windows.Forms.Label InternetLabel;
		System.Windows.Forms.PictureBox InternetPictureBox;
		System.Windows.Forms.PictureBox LocalPictureBox;
		System.Windows.Forms.Button BackButton;
		System.Windows.Forms.DataGridViewTextBoxColumn DateColumn;
		System.Windows.Forms.DataGridViewTextBoxColumn MyFileColumn;
		System.Windows.Forms.CheckBox SearchTheInternetCheckBox;
	}
}