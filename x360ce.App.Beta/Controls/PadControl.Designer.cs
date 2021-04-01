namespace x360ce.App.Controls
{
	partial class PadControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.ResetPresetButton = new System.Windows.Forms.Button();
			this.MainToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.PastePresetButton = new System.Windows.Forms.Button();
			this.CopyPresetButton = new System.Windows.Forms.Button();
			this.ClearPresetButton = new System.Windows.Forms.Button();
			this.GameControllersButton = new System.Windows.Forms.Button();
			this.AutoPresetButton = new System.Windows.Forms.Button();
			this.MappedDevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.IsOnlineColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.ConnectionClassColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.IsEnabledColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.SettingIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.CompletionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.InstanceIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.VendorNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ProductNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MapToColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.LoadPresetButton = new System.Windows.Forms.Button();
			this.GamesToolStrip = new System.Windows.Forms.ToolStrip();
			this.RemoveMapButton = new System.Windows.Forms.ToolStripButton();
			this.AddMapButton = new System.Windows.Forms.ToolStripButton();
			this.AutoMapButton = new System.Windows.Forms.ToolStripButton();
			this.EnableButton = new System.Windows.Forms.ToolStripButton();
			this.GetXInputStatesCheckBox = new System.Windows.Forms.ToolStripButton();
			this.DxTweakButton = new System.Windows.Forms.Button();
			this.PadItemHost = new System.Windows.Forms.Integration.ElementHost();
			((System.ComponentModel.ISupportInitialize)(this.MappedDevicesDataGridView)).BeginInit();
			this.GamesToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// ResetPresetButton
			// 
			this.ResetPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ResetPresetButton.Location = new System.Drawing.Point(688, 584);
			this.ResetPresetButton.Name = "ResetPresetButton";
			this.ResetPresetButton.Size = new System.Drawing.Size(75, 23);
			this.ResetPresetButton.TabIndex = 66;
			this.ResetPresetButton.Text = "&Reset";
			this.ResetPresetButton.UseVisualStyleBackColor = true;
			this.ResetPresetButton.Click += new System.EventHandler(this.ResetPresetButton_Click);
			// 
			// PastePresetButton
			// 
			this.PastePresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.PastePresetButton.Image = global::x360ce.App.Properties.Resources.paste_16x16;
			this.PastePresetButton.Location = new System.Drawing.Point(316, 584);
			this.PastePresetButton.Name = "PastePresetButton";
			this.PastePresetButton.Size = new System.Drawing.Size(100, 23);
			this.PastePresetButton.TabIndex = 72;
			this.PastePresetButton.Text = "Paste Preset";
			this.PastePresetButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.MainToolTip.SetToolTip(this.PastePresetButton, "Paste Preset");
			this.PastePresetButton.UseVisualStyleBackColor = true;
			this.PastePresetButton.Click += new System.EventHandler(this.PastePresetButton_Click);
			// 
			// CopyPresetButton
			// 
			this.CopyPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CopyPresetButton.Image = global::x360ce.App.Properties.Resources.copy_16x16;
			this.CopyPresetButton.Location = new System.Drawing.Point(210, 584);
			this.CopyPresetButton.Name = "CopyPresetButton";
			this.CopyPresetButton.Size = new System.Drawing.Size(100, 23);
			this.CopyPresetButton.TabIndex = 71;
			this.CopyPresetButton.Text = "Copy Preset";
			this.CopyPresetButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.MainToolTip.SetToolTip(this.CopyPresetButton, "Copy Preset");
			this.CopyPresetButton.UseVisualStyleBackColor = true;
			this.CopyPresetButton.Click += new System.EventHandler(this.CopyPresetButton_Click);
			// 
			// ClearPresetButton
			// 
			this.ClearPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearPresetButton.Location = new System.Drawing.Point(607, 584);
			this.ClearPresetButton.Name = "ClearPresetButton";
			this.ClearPresetButton.Size = new System.Drawing.Size(75, 23);
			this.ClearPresetButton.TabIndex = 66;
			this.ClearPresetButton.Text = "&Clear";
			this.ClearPresetButton.UseVisualStyleBackColor = true;
			this.ClearPresetButton.Click += new System.EventHandler(this.ClearPresetButton_Click);
			// 
			// GameControllersButton
			// 
			this.GameControllersButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.GameControllersButton.Location = new System.Drawing.Point(3, 584);
			this.GameControllersButton.Name = "GameControllersButton";
			this.GameControllersButton.Size = new System.Drawing.Size(106, 23);
			this.GameControllersButton.TabIndex = 66;
			this.GameControllersButton.Text = "&Game Controllers...";
			this.GameControllersButton.UseVisualStyleBackColor = true;
			this.GameControllersButton.Click += new System.EventHandler(this.GameControllersButton_Click);
			// 
			// AutoPresetButton
			// 
			this.AutoPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoPresetButton.Location = new System.Drawing.Point(528, 584);
			this.AutoPresetButton.Name = "AutoPresetButton";
			this.AutoPresetButton.Size = new System.Drawing.Size(75, 23);
			this.AutoPresetButton.TabIndex = 66;
			this.AutoPresetButton.Text = "&Auto";
			this.AutoPresetButton.UseVisualStyleBackColor = true;
			this.AutoPresetButton.Click += new System.EventHandler(this.AutoPresetButton_Click);
			// 
			// MappedDevicesDataGridView
			// 
			this.MappedDevicesDataGridView.AllowUserToAddRows = false;
			this.MappedDevicesDataGridView.AllowUserToDeleteRows = false;
			this.MappedDevicesDataGridView.AllowUserToResizeRows = false;
			this.MappedDevicesDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MappedDevicesDataGridView.BackgroundColor = System.Drawing.Color.White;
			this.MappedDevicesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.MappedDevicesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			this.MappedDevicesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MappedDevicesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.IsOnlineColumn,
            this.ConnectionClassColumn,
            this.IsEnabledColumn,
            this.SettingIdColumn,
            this.CompletionColumn,
            this.InstanceIdColumn,
            this.VendorNameColumn,
            this.ProductNameColumn,
            this.MapToColumn});
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.MappedDevicesDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
			this.MappedDevicesDataGridView.EnableHeadersVisualStyles = false;
			this.MappedDevicesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.MappedDevicesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.MappedDevicesDataGridView.MultiSelect = false;
			this.MappedDevicesDataGridView.Name = "MappedDevicesDataGridView";
			this.MappedDevicesDataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.MappedDevicesDataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.MappedDevicesDataGridView.RowHeadersVisible = false;
			this.MappedDevicesDataGridView.RowHeadersWidth = 51;
			this.MappedDevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.MappedDevicesDataGridView.Size = new System.Drawing.Size(766, 85);
			this.MappedDevicesDataGridView.TabIndex = 68;
			this.MappedDevicesDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.MappedDevicesDataGridView_CellClick);
			this.MappedDevicesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.MappedDevicesDataGridView_CellFormatting);
			// 
			// IsOnlineColumn
			// 
			this.IsOnlineColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.IsOnlineColumn.DataPropertyName = "IsOnline";
			this.IsOnlineColumn.HeaderText = "";
			this.IsOnlineColumn.MinimumWidth = 24;
			this.IsOnlineColumn.Name = "IsOnlineColumn";
			this.IsOnlineColumn.ReadOnly = true;
			this.IsOnlineColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.IsOnlineColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.IsOnlineColumn.Width = 24;
			// 
			// ConnectionClassColumn
			// 
			this.ConnectionClassColumn.DataPropertyName = "ConnectionClass";
			this.ConnectionClassColumn.HeaderText = "";
			this.ConnectionClassColumn.MinimumWidth = 24;
			this.ConnectionClassColumn.Name = "ConnectionClassColumn";
			this.ConnectionClassColumn.ReadOnly = true;
			this.ConnectionClassColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.ConnectionClassColumn.Width = 24;
			// 
			// IsEnabledColumn
			// 
			this.IsEnabledColumn.DataPropertyName = "IsEnabled";
			this.IsEnabledColumn.HeaderText = "";
			this.IsEnabledColumn.MinimumWidth = 6;
			this.IsEnabledColumn.Name = "IsEnabledColumn";
			this.IsEnabledColumn.ReadOnly = true;
			this.IsEnabledColumn.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.IsEnabledColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.IsEnabledColumn.Width = 24;
			// 
			// SettingIdColumn
			// 
			this.SettingIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SettingIdColumn.DataPropertyName = "PadSettingChecksum";
			this.SettingIdColumn.HeaderText = "Setting ID";
			this.SettingIdColumn.MinimumWidth = 6;
			this.SettingIdColumn.Name = "SettingIdColumn";
			this.SettingIdColumn.ReadOnly = true;
			this.SettingIdColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.SettingIdColumn.Width = 59;
			// 
			// CompletionColumn
			// 
			this.CompletionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.CompletionColumn.DataPropertyName = "Completion";
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
			dataGridViewCellStyle1.NullValue = null;
			this.CompletionColumn.DefaultCellStyle = dataGridViewCellStyle1;
			this.CompletionColumn.HeaderText = "Map %";
			this.CompletionColumn.MinimumWidth = 6;
			this.CompletionColumn.Name = "CompletionColumn";
			this.CompletionColumn.ReadOnly = true;
			this.CompletionColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.CompletionColumn.Width = 40;
			// 
			// InstanceIdColumn
			// 
			this.InstanceIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			this.InstanceIdColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.InstanceIdColumn.HeaderText = "Instance ID";
			this.InstanceIdColumn.MinimumWidth = 6;
			this.InstanceIdColumn.Name = "InstanceIdColumn";
			this.InstanceIdColumn.ReadOnly = true;
			this.InstanceIdColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.InstanceIdColumn.Width = 60;
			// 
			// VendorNameColumn
			// 
			this.VendorNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.VendorNameColumn.DataPropertyName = "VendorName";
			this.VendorNameColumn.HeaderText = "Vendor Name";
			this.VendorNameColumn.MinimumWidth = 6;
			this.VendorNameColumn.Name = "VendorNameColumn";
			this.VendorNameColumn.ReadOnly = true;
			this.VendorNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.VendorNameColumn.Width = 69;
			// 
			// ProductNameColumn
			// 
			this.ProductNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.ProductNameColumn.DataPropertyName = "ProductName";
			this.ProductNameColumn.HeaderText = "Product Name";
			this.ProductNameColumn.MinimumWidth = 6;
			this.ProductNameColumn.Name = "ProductNameColumn";
			this.ProductNameColumn.ReadOnly = true;
			this.ProductNameColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// MapToColumn
			// 
			this.MapToColumn.HeaderText = "Map To";
			this.MapToColumn.MinimumWidth = 6;
			this.MapToColumn.Name = "MapToColumn";
			this.MapToColumn.ReadOnly = true;
			this.MapToColumn.Visible = false;
			this.MapToColumn.Width = 125;
			// 
			// LoadPresetButton
			// 
			this.LoadPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.LoadPresetButton.Location = new System.Drawing.Point(422, 584);
			this.LoadPresetButton.Name = "LoadPresetButton";
			this.LoadPresetButton.Size = new System.Drawing.Size(100, 23);
			this.LoadPresetButton.TabIndex = 66;
			this.LoadPresetButton.Text = "&Load Preset...";
			this.LoadPresetButton.UseVisualStyleBackColor = true;
			this.LoadPresetButton.Click += new System.EventHandler(this.LoadPresetButton_Click);
			// 
			// GamesToolStrip
			// 
			this.GamesToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.GamesToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.GamesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveMapButton,
            this.AddMapButton,
            this.AutoMapButton,
            this.EnableButton,
            this.GetXInputStatesCheckBox});
			this.GamesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.GamesToolStrip.Name = "GamesToolStrip";
			this.GamesToolStrip.Padding = new System.Windows.Forms.Padding(4, 0, 1, 0);
			this.GamesToolStrip.Size = new System.Drawing.Size(766, 50);
			this.GamesToolStrip.TabIndex = 3;
			this.GamesToolStrip.Text = "MySettingsToolStrip";
			// 
			// RemoveMapButton
			// 
			this.RemoveMapButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RemoveMapButton.Image = global::x360ce.App.Properties.Resources.delete_16x16;
			this.RemoveMapButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.RemoveMapButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.RemoveMapButton.Name = "RemoveMapButton";
			this.RemoveMapButton.Size = new System.Drawing.Size(125, 47);
			this.RemoveMapButton.Text = "&Remove";
			this.RemoveMapButton.Click += new System.EventHandler(this.RemoveMapButton_Click);
			// 
			// AddMapButton
			// 
			this.AddMapButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AddMapButton.Image = global::x360ce.App.Properties.Resources.add_16x16;
			this.AddMapButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AddMapButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AddMapButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.AddMapButton.Name = "AddMapButton";
			this.AddMapButton.Size = new System.Drawing.Size(97, 47);
			this.AddMapButton.Text = "Add...";
			this.AddMapButton.Click += new System.EventHandler(this.AddMapButton_Click);
			// 
			// AutoMapButton
			// 
			this.AutoMapButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AutoMapButton.Image = global::x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			this.AutoMapButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.AutoMapButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.AutoMapButton.Name = "AutoMapButton";
			this.AutoMapButton.Size = new System.Drawing.Size(145, 47);
			this.AutoMapButton.Text = "&Auto Map";
			this.AutoMapButton.Click += new System.EventHandler(this.AutoMapButton_Click);
			// 
			// EnableButton
			// 
			this.EnableButton.Image = global::x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			this.EnableButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.EnableButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.EnableButton.Name = "EnableButton";
			this.EnableButton.Size = new System.Drawing.Size(110, 47);
			this.EnableButton.Text = "&Enable";
			this.EnableButton.Click += new System.EventHandler(this.EnableButton_Click);
			// 
			// GetXInputStatesCheckBox
			// 
			this.GetXInputStatesCheckBox.Image = global::x360ce.App.Properties.Resources.checkbox_unchecked_16x16;
			this.GetXInputStatesCheckBox.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.GetXInputStatesCheckBox.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.GetXInputStatesCheckBox.Name = "GetXInputStatesCheckBox";
			this.GetXInputStatesCheckBox.Size = new System.Drawing.Size(213, 47);
			this.GetXInputStatesCheckBox.Text = "&Get XInput State";
			this.GetXInputStatesCheckBox.ToolTipText = "Load XInput DLL and query actual state";
			// 
			// DxTweakButton
			// 
			this.DxTweakButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DxTweakButton.Location = new System.Drawing.Point(115, 584);
			this.DxTweakButton.Name = "DxTweakButton";
			this.DxTweakButton.Size = new System.Drawing.Size(79, 23);
			this.DxTweakButton.TabIndex = 70;
			this.DxTweakButton.Text = "&DX Tweak...";
			this.DxTweakButton.UseVisualStyleBackColor = true;
			this.DxTweakButton.Click += new System.EventHandler(this.CalibrateButton_Click);
			// 
			// PadItemHost
			// 
			this.PadItemHost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PadItemHost.Location = new System.Drawing.Point(3, 116);
			this.PadItemHost.Name = "PadItemHost";
			this.PadItemHost.Size = new System.Drawing.Size(760, 462);
			this.PadItemHost.TabIndex = 73;
			this.PadItemHost.Child = null;
			// 
			// PadControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PadItemHost);
			this.Controls.Add(this.PastePresetButton);
			this.Controls.Add(this.CopyPresetButton);
			this.Controls.Add(this.DxTweakButton);
			this.Controls.Add(this.MappedDevicesDataGridView);
			this.Controls.Add(this.GamesToolStrip);
			this.Controls.Add(this.LoadPresetButton);
			this.Controls.Add(this.GameControllersButton);
			this.Controls.Add(this.AutoPresetButton);
			this.Controls.Add(this.ClearPresetButton);
			this.Controls.Add(this.ResetPresetButton);
			this.Name = "PadControl";
			this.Size = new System.Drawing.Size(766, 610);
			((System.ComponentModel.ISupportInitialize)(this.MappedDevicesDataGridView)).EndInit();
			this.GamesToolStrip.ResumeLayout(false);
			this.GamesToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		System.Windows.Forms.ToolTip MainToolTip;
        System.Windows.Forms.Button ResetPresetButton;
        private System.Windows.Forms.Button ClearPresetButton;
		private System.Windows.Forms.Button GameControllersButton;
		private System.Windows.Forms.Button AutoPresetButton;
		private System.Windows.Forms.Button LoadPresetButton;
		public System.Windows.Forms.DataGridView MappedDevicesDataGridView;
		private System.Windows.Forms.ToolStrip GamesToolStrip;
		private System.Windows.Forms.ToolStripButton AddMapButton;
		private System.Windows.Forms.ToolStripButton RemoveMapButton;
		private System.Windows.Forms.ToolStripButton AutoMapButton;
		private System.Windows.Forms.ToolStripButton EnableButton;
        private System.Windows.Forms.ToolStripButton GetXInputStatesCheckBox;
		private System.Windows.Forms.Button DxTweakButton;
		private System.Windows.Forms.DataGridViewImageColumn IsOnlineColumn;
		private System.Windows.Forms.DataGridViewImageColumn ConnectionClassColumn;
		private System.Windows.Forms.DataGridViewCheckBoxColumn IsEnabledColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn SettingIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn CompletionColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn InstanceIdColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn VendorNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn ProductNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MapToColumn;
		private System.Windows.Forms.Button CopyPresetButton;
		private System.Windows.Forms.Button PastePresetButton;
		private System.Windows.Forms.Integration.ElementHost PadItemHost;
	
	}
}
