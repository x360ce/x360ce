namespace JocysCom.ClassLibrary.IO
{
	partial class HardwareControl
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
			this.DisableButton = new System.Windows.Forms.Button();
			this.EnableButton = new System.Windows.Forms.Button();
			this.DeviceTabControl = new System.Windows.Forms.TabControl();
			this.DeviceTabPage = new System.Windows.Forms.TabPage();
			this.DeviceDataGridView = new System.Windows.Forms.DataGridView();
			this.VendorIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.RevisionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ClassGuidColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ClassDescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DeviceDescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ManufacturerColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.IsHiddenColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PresentColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.RemovableColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DeviceIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MainTabPage = new System.Windows.Forms.TabPage();
			this.TreeSplitContainer = new System.Windows.Forms.SplitContainer();
			this.DevicesTreeView = new System.Windows.Forms.TreeView();
			this.TreeImageList = new System.Windows.Forms.ImageList(this.components);
			this.DeviceDetailsPanel = new System.Windows.Forms.Panel();
			this.ClassDescriptionTextBox = new System.Windows.Forms.TextBox();
			this.ClassGuidTextBox = new System.Windows.Forms.TextBox();
			this.ClassGuidLabel = new System.Windows.Forms.Label();
			this.ClassDescriptionLabel = new System.Windows.Forms.Label();
			this.ManufacturerTextBox = new System.Windows.Forms.TextBox();
			this.ManufacturerLabel = new System.Windows.Forms.Label();
			this.DescriptionTextBox = new System.Windows.Forms.TextBox();
			this.DescriptionLabel = new System.Windows.Forms.Label();
			this.RevisionTextBox = new System.Windows.Forms.TextBox();
			this.RevisionLabel = new System.Windows.Forms.Label();
			this.ProductIdTextBox = new System.Windows.Forms.TextBox();
			this.ProductIdLabel = new System.Windows.Forms.Label();
			this.VendorIdTextBox = new System.Windows.Forms.TextBox();
			this.VendorIdLabel = new System.Windows.Forms.Label();
			this.DeviceStatusLabel = new System.Windows.Forms.Label();
			this.DevicePathLabel = new System.Windows.Forms.Label();
			this.DeviceIdLabel = new System.Windows.Forms.Label();
			this.DeviceStatusTextBox = new System.Windows.Forms.TextBox();
			this.DevicePathTextBox = new System.Windows.Forms.TextBox();
			this.DeviceIdTextBox = new System.Windows.Forms.TextBox();
			this.ErrorLabel = new System.Windows.Forms.Label();
			this.RemoveButton = new System.Windows.Forms.Button();
			this.ScanButton = new System.Windows.Forms.Button();
			this.FilterTextBox = new System.Windows.Forms.TextBox();
			this.EnableFilterCheckBox = new System.Windows.Forms.CheckBox();
			this.DeviceTabControl.SuspendLayout();
			this.DeviceTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DeviceDataGridView)).BeginInit();
			this.MainTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TreeSplitContainer)).BeginInit();
			this.TreeSplitContainer.Panel1.SuspendLayout();
			this.TreeSplitContainer.Panel2.SuspendLayout();
			this.TreeSplitContainer.SuspendLayout();
			this.DeviceDetailsPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// DisableButton
			// 
			this.DisableButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.DisableButton.Location = new System.Drawing.Point(685, 499);
			this.DisableButton.Name = "DisableButton";
			this.DisableButton.Size = new System.Drawing.Size(75, 23);
			this.DisableButton.TabIndex = 7;
			this.DisableButton.Text = "Disable";
			this.DisableButton.UseVisualStyleBackColor = true;
			this.DisableButton.Click += new System.EventHandler(this.DisableButton_Click);
			// 
			// EnableButton
			// 
			this.EnableButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.EnableButton.Location = new System.Drawing.Point(0, 499);
			this.EnableButton.Name = "EnableButton";
			this.EnableButton.Size = new System.Drawing.Size(75, 23);
			this.EnableButton.TabIndex = 6;
			this.EnableButton.Text = "Enable";
			this.EnableButton.UseVisualStyleBackColor = true;
			this.EnableButton.Click += new System.EventHandler(this.EnableButton_Click);
			// 
			// DeviceTabControl
			// 
			this.DeviceTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DeviceTabControl.Controls.Add(this.DeviceTabPage);
			this.DeviceTabControl.Controls.Add(this.MainTabPage);
			this.DeviceTabControl.Location = new System.Drawing.Point(3, 29);
			this.DeviceTabControl.Name = "DeviceTabControl";
			this.DeviceTabControl.SelectedIndex = 0;
			this.DeviceTabControl.Size = new System.Drawing.Size(757, 464);
			this.DeviceTabControl.TabIndex = 9;
			// 
			// DeviceTabPage
			// 
			this.DeviceTabPage.Controls.Add(this.DeviceDataGridView);
			this.DeviceTabPage.Location = new System.Drawing.Point(4, 22);
			this.DeviceTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.DeviceTabPage.Name = "DeviceTabPage";
			this.DeviceTabPage.Size = new System.Drawing.Size(726, 438);
			this.DeviceTabPage.TabIndex = 1;
			this.DeviceTabPage.Text = "XX Devices Attached";
			this.DeviceTabPage.UseVisualStyleBackColor = true;
			// 
			// DeviceDataGridView
			// 
			this.DeviceDataGridView.AllowUserToAddRows = false;
			this.DeviceDataGridView.AllowUserToDeleteRows = false;
			this.DeviceDataGridView.AllowUserToResizeRows = false;
			this.DeviceDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.DeviceDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.DeviceDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			this.DeviceDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.DeviceDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.VendorIdColumn,
            this.ProductIdColumn,
            this.RevisionColumn,
            this.ClassGuidColumn,
            this.ClassDescriptionColumn,
            this.DeviceDescriptionColumn,
            this.ManufacturerColumn,
            this.IsHiddenColumn,
            this.PresentColumn,
            this.StatusColumn,
            this.RemovableColumn,
            this.DeviceIdColumn});
			this.DeviceDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DeviceDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.DeviceDataGridView.Location = new System.Drawing.Point(0, 0);
			this.DeviceDataGridView.Name = "DeviceDataGridView";
			this.DeviceDataGridView.RowHeadersVisible = false;
			this.DeviceDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DeviceDataGridView.Size = new System.Drawing.Size(726, 438);
			this.DeviceDataGridView.TabIndex = 6;
			this.DeviceDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.DeviceDataGridView_CellFormatting);
			this.DeviceDataGridView.SelectionChanged += new System.EventHandler(this.DeviceDataGridView_SelectionChanged);
			// 
			// VendorIdColumn
			// 
			this.VendorIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.VendorIdColumn.DataPropertyName = "VendorId";
			dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.VendorIdColumn.DefaultCellStyle = dataGridViewCellStyle7;
			this.VendorIdColumn.HeaderText = "VID";
			this.VendorIdColumn.Name = "VendorIdColumn";
			this.VendorIdColumn.Width = 50;
			// 
			// ProductIdColumn
			// 
			this.ProductIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ProductIdColumn.DataPropertyName = "ProductId";
			dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.ProductIdColumn.DefaultCellStyle = dataGridViewCellStyle8;
			this.ProductIdColumn.HeaderText = "PID";
			this.ProductIdColumn.Name = "ProductIdColumn";
			this.ProductIdColumn.Width = 50;
			// 
			// RevisionColumn
			// 
			this.RevisionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.RevisionColumn.DataPropertyName = "Revision";
			dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			this.RevisionColumn.DefaultCellStyle = dataGridViewCellStyle9;
			this.RevisionColumn.HeaderText = "REV";
			this.RevisionColumn.Name = "RevisionColumn";
			this.RevisionColumn.Width = 54;
			// 
			// ClassGuidColumn
			// 
			this.ClassGuidColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ClassGuidColumn.DataPropertyName = "ClassGuid";
			this.ClassGuidColumn.HeaderText = "Class GUID";
			this.ClassGuidColumn.Name = "ClassGuidColumn";
			this.ClassGuidColumn.Visible = false;
			// 
			// ClassDescriptionColumn
			// 
			this.ClassDescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ClassDescriptionColumn.DataPropertyName = "ClassDescription";
			this.ClassDescriptionColumn.HeaderText = "Class Description";
			this.ClassDescriptionColumn.Name = "ClassDescriptionColumn";
			this.ClassDescriptionColumn.Width = 104;
			// 
			// DeviceDescriptionColumn
			// 
			this.DeviceDescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DeviceDescriptionColumn.DataPropertyName = "Description";
			this.DeviceDescriptionColumn.HeaderText = "Description";
			this.DeviceDescriptionColumn.Name = "DeviceDescriptionColumn";
			this.DeviceDescriptionColumn.Width = 85;
			// 
			// ManufacturerColumn
			// 
			this.ManufacturerColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ManufacturerColumn.DataPropertyName = "Manufacturer";
			this.ManufacturerColumn.HeaderText = "Manufacturer";
			this.ManufacturerColumn.Name = "ManufacturerColumn";
			this.ManufacturerColumn.Width = 95;
			// 
			// IsHiddenColumn
			// 
			this.IsHiddenColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.IsHiddenColumn.DataPropertyName = "IsHidden";
			this.IsHiddenColumn.HeaderText = "Hidden";
			this.IsHiddenColumn.Name = "IsHiddenColumn";
			this.IsHiddenColumn.Visible = false;
			// 
			// PresentColumn
			// 
			this.PresentColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PresentColumn.DataPropertyName = "IsPresent";
			this.PresentColumn.HeaderText = "Present";
			this.PresentColumn.Name = "PresentColumn";
			this.PresentColumn.Visible = false;
			// 
			// StatusColumn
			// 
			this.StatusColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.StatusColumn.DataPropertyName = "Status";
			this.StatusColumn.HeaderText = "Status";
			this.StatusColumn.Name = "StatusColumn";
			this.StatusColumn.Width = 62;
			// 
			// RemovableColumn
			// 
			this.RemovableColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.RemovableColumn.DataPropertyName = "IsRemovable";
			this.RemovableColumn.HeaderText = "Removable";
			this.RemovableColumn.Name = "RemovableColumn";
			this.RemovableColumn.Width = 86;
			// 
			// DeviceIdColumn
			// 
			this.DeviceIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.DeviceIdColumn.DataPropertyName = "DeviceId";
			this.DeviceIdColumn.HeaderText = "Device ID";
			this.DeviceIdColumn.Name = "DeviceIdColumn";
			this.DeviceIdColumn.Width = 74;
			// 
			// MainTabPage
			// 
			this.MainTabPage.Controls.Add(this.TreeSplitContainer);
			this.MainTabPage.Location = new System.Drawing.Point(4, 22);
			this.MainTabPage.Name = "MainTabPage";
			this.MainTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.MainTabPage.Size = new System.Drawing.Size(749, 438);
			this.MainTabPage.TabIndex = 2;
			this.MainTabPage.Text = "Tree";
			this.MainTabPage.UseVisualStyleBackColor = true;
			// 
			// TreeSplitContainer
			// 
			this.TreeSplitContainer.BackColor = System.Drawing.SystemColors.Control;
			this.TreeSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TreeSplitContainer.Location = new System.Drawing.Point(3, 3);
			this.TreeSplitContainer.Name = "TreeSplitContainer";
			// 
			// TreeSplitContainer.Panel1
			// 
			this.TreeSplitContainer.Panel1.Controls.Add(this.DevicesTreeView);
			// 
			// TreeSplitContainer.Panel2
			// 
			this.TreeSplitContainer.Panel2.Controls.Add(this.DeviceDetailsPanel);
			this.TreeSplitContainer.Size = new System.Drawing.Size(743, 432);
			this.TreeSplitContainer.SplitterDistance = 398;
			this.TreeSplitContainer.TabIndex = 2;
			// 
			// DevicesTreeView
			// 
			this.DevicesTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.DevicesTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesTreeView.ImageIndex = 0;
			this.DevicesTreeView.ImageList = this.TreeImageList;
			this.DevicesTreeView.Location = new System.Drawing.Point(0, 0);
			this.DevicesTreeView.Name = "DevicesTreeView";
			this.DevicesTreeView.SelectedImageIndex = 0;
			this.DevicesTreeView.Size = new System.Drawing.Size(398, 432);
			this.DevicesTreeView.TabIndex = 0;
			this.DevicesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.DevicesTreeView_AfterSelect);
			// 
			// TreeImageList
			// 
			this.TreeImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.TreeImageList.ImageSize = new System.Drawing.Size(16, 16);
			this.TreeImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// DeviceDetailsPanel
			// 
			this.DeviceDetailsPanel.Controls.Add(this.ClassDescriptionTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.ClassGuidTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.ClassGuidLabel);
			this.DeviceDetailsPanel.Controls.Add(this.ClassDescriptionLabel);
			this.DeviceDetailsPanel.Controls.Add(this.ManufacturerTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.ManufacturerLabel);
			this.DeviceDetailsPanel.Controls.Add(this.DescriptionTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.DescriptionLabel);
			this.DeviceDetailsPanel.Controls.Add(this.RevisionTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.RevisionLabel);
			this.DeviceDetailsPanel.Controls.Add(this.ProductIdTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.ProductIdLabel);
			this.DeviceDetailsPanel.Controls.Add(this.VendorIdTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.VendorIdLabel);
			this.DeviceDetailsPanel.Controls.Add(this.DeviceStatusLabel);
			this.DeviceDetailsPanel.Controls.Add(this.DevicePathLabel);
			this.DeviceDetailsPanel.Controls.Add(this.DeviceIdLabel);
			this.DeviceDetailsPanel.Controls.Add(this.DeviceStatusTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.DevicePathTextBox);
			this.DeviceDetailsPanel.Controls.Add(this.DeviceIdTextBox);
			this.DeviceDetailsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DeviceDetailsPanel.Location = new System.Drawing.Point(0, 0);
			this.DeviceDetailsPanel.Name = "DeviceDetailsPanel";
			this.DeviceDetailsPanel.Size = new System.Drawing.Size(341, 432);
			this.DeviceDetailsPanel.TabIndex = 0;
			// 
			// ClassDescriptionTextBox
			// 
			this.ClassDescriptionTextBox.Location = new System.Drawing.Point(93, 133);
			this.ClassDescriptionTextBox.Name = "ClassDescriptionTextBox";
			this.ClassDescriptionTextBox.ReadOnly = true;
			this.ClassDescriptionTextBox.Size = new System.Drawing.Size(234, 20);
			this.ClassDescriptionTextBox.TabIndex = 4;
			// 
			// ClassGuidTextBox
			// 
			this.ClassGuidTextBox.Location = new System.Drawing.Point(93, 107);
			this.ClassGuidTextBox.Name = "ClassGuidTextBox";
			this.ClassGuidTextBox.ReadOnly = true;
			this.ClassGuidTextBox.Size = new System.Drawing.Size(234, 20);
			this.ClassGuidTextBox.TabIndex = 4;
			// 
			// ClassGuidLabel
			// 
			this.ClassGuidLabel.AutoSize = true;
			this.ClassGuidLabel.Location = new System.Drawing.Point(3, 110);
			this.ClassGuidLabel.Name = "ClassGuidLabel";
			this.ClassGuidLabel.Size = new System.Drawing.Size(65, 13);
			this.ClassGuidLabel.TabIndex = 3;
			this.ClassGuidLabel.Text = "Class GUID:";
			// 
			// ClassDescriptionLabel
			// 
			this.ClassDescriptionLabel.AutoSize = true;
			this.ClassDescriptionLabel.Location = new System.Drawing.Point(3, 136);
			this.ClassDescriptionLabel.Name = "ClassDescriptionLabel";
			this.ClassDescriptionLabel.Size = new System.Drawing.Size(91, 13);
			this.ClassDescriptionLabel.TabIndex = 3;
			this.ClassDescriptionLabel.Text = "Class Description:";
			// 
			// ManufacturerTextBox
			// 
			this.ManufacturerTextBox.Location = new System.Drawing.Point(93, 55);
			this.ManufacturerTextBox.Name = "ManufacturerTextBox";
			this.ManufacturerTextBox.ReadOnly = true;
			this.ManufacturerTextBox.Size = new System.Drawing.Size(234, 20);
			this.ManufacturerTextBox.TabIndex = 4;
			// 
			// ManufacturerLabel
			// 
			this.ManufacturerLabel.AutoSize = true;
			this.ManufacturerLabel.Location = new System.Drawing.Point(3, 58);
			this.ManufacturerLabel.Name = "ManufacturerLabel";
			this.ManufacturerLabel.Size = new System.Drawing.Size(73, 13);
			this.ManufacturerLabel.TabIndex = 3;
			this.ManufacturerLabel.Text = "Manufacturer:";
			// 
			// DescriptionTextBox
			// 
			this.DescriptionTextBox.Location = new System.Drawing.Point(93, 81);
			this.DescriptionTextBox.Name = "DescriptionTextBox";
			this.DescriptionTextBox.ReadOnly = true;
			this.DescriptionTextBox.Size = new System.Drawing.Size(234, 20);
			this.DescriptionTextBox.TabIndex = 4;
			// 
			// DescriptionLabel
			// 
			this.DescriptionLabel.AutoSize = true;
			this.DescriptionLabel.Location = new System.Drawing.Point(3, 84);
			this.DescriptionLabel.Name = "DescriptionLabel";
			this.DescriptionLabel.Size = new System.Drawing.Size(63, 13);
			this.DescriptionLabel.TabIndex = 3;
			this.DescriptionLabel.Text = "Description:";
			// 
			// RevisionTextBox
			// 
			this.RevisionTextBox.Location = new System.Drawing.Point(262, 29);
			this.RevisionTextBox.Name = "RevisionTextBox";
			this.RevisionTextBox.ReadOnly = true;
			this.RevisionTextBox.Size = new System.Drawing.Size(65, 20);
			this.RevisionTextBox.TabIndex = 4;
			// 
			// RevisionLabel
			// 
			this.RevisionLabel.AutoSize = true;
			this.RevisionLabel.Location = new System.Drawing.Point(181, 32);
			this.RevisionLabel.Name = "RevisionLabel";
			this.RevisionLabel.Size = new System.Drawing.Size(82, 13);
			this.RevisionLabel.TabIndex = 3;
			this.RevisionLabel.Text = "Revision (REV):";
			// 
			// ProductIdTextBox
			// 
			this.ProductIdTextBox.Location = new System.Drawing.Point(93, 29);
			this.ProductIdTextBox.Name = "ProductIdTextBox";
			this.ProductIdTextBox.ReadOnly = true;
			this.ProductIdTextBox.Size = new System.Drawing.Size(82, 20);
			this.ProductIdTextBox.TabIndex = 4;
			// 
			// ProductIdLabel
			// 
			this.ProductIdLabel.AutoSize = true;
			this.ProductIdLabel.Location = new System.Drawing.Point(3, 32);
			this.ProductIdLabel.Name = "ProductIdLabel";
			this.ProductIdLabel.Size = new System.Drawing.Size(86, 13);
			this.ProductIdLabel.TabIndex = 3;
			this.ProductIdLabel.Text = "Product Id (PID):";
			// 
			// VendorIdTextBox
			// 
			this.VendorIdTextBox.Location = new System.Drawing.Point(93, 3);
			this.VendorIdTextBox.Name = "VendorIdTextBox";
			this.VendorIdTextBox.ReadOnly = true;
			this.VendorIdTextBox.Size = new System.Drawing.Size(82, 20);
			this.VendorIdTextBox.TabIndex = 2;
			// 
			// VendorIdLabel
			// 
			this.VendorIdLabel.AutoSize = true;
			this.VendorIdLabel.Location = new System.Drawing.Point(4, 7);
			this.VendorIdLabel.Name = "VendorIdLabel";
			this.VendorIdLabel.Size = new System.Drawing.Size(83, 13);
			this.VendorIdLabel.TabIndex = 1;
			this.VendorIdLabel.Text = "Vendor Id (VID):";
			// 
			// DeviceStatusLabel
			// 
			this.DeviceStatusLabel.AutoSize = true;
			this.DeviceStatusLabel.Location = new System.Drawing.Point(2, 164);
			this.DeviceStatusLabel.Name = "DeviceStatusLabel";
			this.DeviceStatusLabel.Size = new System.Drawing.Size(77, 13);
			this.DeviceStatusLabel.TabIndex = 1;
			this.DeviceStatusLabel.Text = "Device Status:";
			// 
			// DevicePathLabel
			// 
			this.DevicePathLabel.AutoSize = true;
			this.DevicePathLabel.Location = new System.Drawing.Point(1, 224);
			this.DevicePathLabel.Name = "DevicePathLabel";
			this.DevicePathLabel.Size = new System.Drawing.Size(66, 13);
			this.DevicePathLabel.TabIndex = 1;
			this.DevicePathLabel.Text = "DevicePath:";
			// 
			// DeviceIdLabel
			// 
			this.DeviceIdLabel.AutoSize = true;
			this.DeviceIdLabel.Location = new System.Drawing.Point(4, 328);
			this.DeviceIdLabel.Name = "DeviceIdLabel";
			this.DeviceIdLabel.Size = new System.Drawing.Size(58, 13);
			this.DeviceIdLabel.TabIndex = 1;
			this.DeviceIdLabel.Text = "Device ID:";
			// 
			// DeviceStatusTextBox
			// 
			this.DeviceStatusTextBox.Location = new System.Drawing.Point(4, 183);
			this.DeviceStatusTextBox.Multiline = true;
			this.DeviceStatusTextBox.Name = "DeviceStatusTextBox";
			this.DeviceStatusTextBox.ReadOnly = true;
			this.DeviceStatusTextBox.Size = new System.Drawing.Size(324, 38);
			this.DeviceStatusTextBox.TabIndex = 0;
			// 
			// DevicePathTextBox
			// 
			this.DevicePathTextBox.Location = new System.Drawing.Point(3, 243);
			this.DevicePathTextBox.Multiline = true;
			this.DevicePathTextBox.Name = "DevicePathTextBox";
			this.DevicePathTextBox.ReadOnly = true;
			this.DevicePathTextBox.Size = new System.Drawing.Size(324, 82);
			this.DevicePathTextBox.TabIndex = 0;
			// 
			// DeviceIdTextBox
			// 
			this.DeviceIdTextBox.Location = new System.Drawing.Point(3, 347);
			this.DeviceIdTextBox.Multiline = true;
			this.DeviceIdTextBox.Name = "DeviceIdTextBox";
			this.DeviceIdTextBox.ReadOnly = true;
			this.DeviceIdTextBox.Size = new System.Drawing.Size(324, 82);
			this.DeviceIdTextBox.TabIndex = 0;
			// 
			// ErrorLabel
			// 
			this.ErrorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ErrorLabel.AutoSize = true;
			this.ErrorLabel.Location = new System.Drawing.Point(81, 504);
			this.ErrorLabel.Name = "ErrorLabel";
			this.ErrorLabel.Size = new System.Drawing.Size(55, 13);
			this.ErrorLabel.TabIndex = 10;
			this.ErrorLabel.Text = "ErrorLabel";
			this.ErrorLabel.Visible = false;
			// 
			// RemoveButton
			// 
			this.RemoveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoveButton.Location = new System.Drawing.Point(604, 499);
			this.RemoveButton.Name = "RemoveButton";
			this.RemoveButton.Size = new System.Drawing.Size(75, 23);
			this.RemoveButton.TabIndex = 11;
			this.RemoveButton.Text = "Remove";
			this.RemoveButton.UseVisualStyleBackColor = true;
			this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
			// 
			// ScanButton
			// 
			this.ScanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ScanButton.Location = new System.Drawing.Point(523, 499);
			this.ScanButton.Name = "ScanButton";
			this.ScanButton.Size = new System.Drawing.Size(75, 23);
			this.ScanButton.TabIndex = 11;
			this.ScanButton.Text = "Scan";
			this.ScanButton.UseVisualStyleBackColor = true;
			this.ScanButton.Click += new System.EventHandler(this.ScanButton_Click);
			// 
			// FilterTextBox
			// 
			this.FilterTextBox.Location = new System.Drawing.Point(96, 3);
			this.FilterTextBox.Name = "FilterTextBox";
			this.FilterTextBox.Size = new System.Drawing.Size(222, 20);
			this.FilterTextBox.TabIndex = 13;
			this.FilterTextBox.TextChanged += new System.EventHandler(this.FilterTextBox_TextChanged);
			// 
			// EnableFilterCheckBox
			// 
			this.EnableFilterCheckBox.AutoSize = true;
			this.EnableFilterCheckBox.Checked = true;
			this.EnableFilterCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.EnableFilterCheckBox.Location = new System.Drawing.Point(3, 5);
			this.EnableFilterCheckBox.Name = "EnableFilterCheckBox";
			this.EnableFilterCheckBox.Size = new System.Drawing.Size(87, 17);
			this.EnableFilterCheckBox.TabIndex = 14;
			this.EnableFilterCheckBox.Text = "Enable Filter:";
			this.EnableFilterCheckBox.UseVisualStyleBackColor = true;
			this.EnableFilterCheckBox.CheckedChanged += new System.EventHandler(this.EnableFilderCheckBox_CheckedChanged);
			// 
			// HardwareControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.EnableFilterCheckBox);
			this.Controls.Add(this.FilterTextBox);
			this.Controls.Add(this.ScanButton);
			this.Controls.Add(this.RemoveButton);
			this.Controls.Add(this.ErrorLabel);
			this.Controls.Add(this.DeviceTabControl);
			this.Controls.Add(this.DisableButton);
			this.Controls.Add(this.EnableButton);
			this.Name = "HardwareControl";
			this.Size = new System.Drawing.Size(763, 525);
			this.Load += new System.EventHandler(this.HardwareControl_Load);
			this.DeviceTabControl.ResumeLayout(false);
			this.DeviceTabPage.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.DeviceDataGridView)).EndInit();
			this.MainTabPage.ResumeLayout(false);
			this.TreeSplitContainer.Panel1.ResumeLayout(false);
			this.TreeSplitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.TreeSplitContainer)).EndInit();
			this.TreeSplitContainer.ResumeLayout(false);
			this.DeviceDetailsPanel.ResumeLayout(false);
			this.DeviceDetailsPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button DisableButton;
		private System.Windows.Forms.Button EnableButton;
		private System.Windows.Forms.TabControl DeviceTabControl;
		private System.Windows.Forms.TabPage DeviceTabPage;
		private System.Windows.Forms.DataGridView DeviceDataGridView;
		private System.Windows.Forms.Label ErrorLabel;
		private System.Windows.Forms.Button RemoveButton;
		private System.Windows.Forms.Button ScanButton;
		private System.Windows.Forms.TextBox FilterTextBox;
		private System.Windows.Forms.CheckBox EnableFilterCheckBox;
		private System.Windows.Forms.DataGridViewTextBoxColumn VendorIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn RevisionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ClassGuidColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ClassDescriptionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DeviceDescriptionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ManufacturerColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn IsHiddenColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PresentColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn RemovableColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DeviceIdColumn;
		private System.Windows.Forms.TabPage MainTabPage;
		private System.Windows.Forms.TreeView DevicesTreeView;
		private System.Windows.Forms.ImageList TreeImageList;
		private System.Windows.Forms.SplitContainer TreeSplitContainer;
		private System.Windows.Forms.Panel DeviceDetailsPanel;
		private System.Windows.Forms.Label DeviceIdLabel;
		private System.Windows.Forms.TextBox DeviceIdTextBox;
		private System.Windows.Forms.Label DevicePathLabel;
		private System.Windows.Forms.TextBox DevicePathTextBox;
		private System.Windows.Forms.TextBox ProductIdTextBox;
		private System.Windows.Forms.Label ProductIdLabel;
		private System.Windows.Forms.TextBox VendorIdTextBox;
		private System.Windows.Forms.Label VendorIdLabel;
		private System.Windows.Forms.TextBox ClassDescriptionTextBox;
		private System.Windows.Forms.TextBox ClassGuidTextBox;
		private System.Windows.Forms.Label ClassGuidLabel;
		private System.Windows.Forms.Label ClassDescriptionLabel;
		private System.Windows.Forms.TextBox ManufacturerTextBox;
		private System.Windows.Forms.Label ManufacturerLabel;
		private System.Windows.Forms.TextBox DescriptionTextBox;
		private System.Windows.Forms.Label DescriptionLabel;
		private System.Windows.Forms.TextBox RevisionTextBox;
		private System.Windows.Forms.Label RevisionLabel;
		private System.Windows.Forms.Label DeviceStatusLabel;
		private System.Windows.Forms.TextBox DeviceStatusTextBox;
	}
}
