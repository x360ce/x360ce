namespace x360ce.App.Controls
{
	partial class DirectInputControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent()
		{
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.DiEffectsDataGridView = new System.Windows.Forms.DataGridView();
            this.DiDPadLabel = new System.Windows.Forms.Label();
            this.DiUvaLabel = new System.Windows.Forms.Label();
            this.DiExtraVLabel = new System.Windows.Forms.Label();
            this.DiExtraFLabel = new System.Windows.Forms.Label();
            this.DiExtraALabel = new System.Windows.Forms.Label();
            this.DiButtonsLabel = new System.Windows.Forms.Label();
            this.DiAxisDataGridView = new System.Windows.Forms.DataGridView();
            this.DiColumnAxis = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnR = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnA = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnAR = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnF = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnFr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnV = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiColumnVr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DevicePidTextBox = new System.Windows.Forms.TextBox();
            this.DeviceVidTextBox = new System.Windows.Forms.TextBox();
            this.DiDPadTextBox = new System.Windows.Forms.TextBox();
            this.DiUvSliderTextBox = new System.Windows.Forms.TextBox();
            this.DiVSliderTextBox = new System.Windows.Forms.TextBox();
            this.DiFSliderTextBox = new System.Windows.Forms.TextBox();
            this.DiASliderTextBox = new System.Windows.Forms.TextBox();
            this.DiButtonsTextBox = new System.Windows.Forms.TextBox();
            this.DeviceTypeTextBox = new System.Windows.Forms.TextBox();
            this.DiDevicePidLabel = new System.Windows.Forms.Label();
            this.DiDeviceVidLabel = new System.Windows.Forms.Label();
            this.DiDeviceTypeLabel = new System.Windows.Forms.Label();
            this.DeviceInstanceGuidTextBox = new System.Windows.Forms.TextBox();
            this.DeviceProductGuidTextBox = new System.Windows.Forms.TextBox();
            this.DiProductGuidLabel = new System.Windows.Forms.Label();
            this.DiInstanceGuidLabel = new System.Windows.Forms.Label();
            this.DeviceProductNameTextBox = new System.Windows.Forms.TextBox();
            this.DiProductNameLabel = new System.Windows.Forms.Label();
            this.MapToControllerLabel = new System.Windows.Forms.Label();
            this.MapToPadComboBox = new System.Windows.Forms.ComboBox();
            this.DiCapFfStateTextBox = new System.Windows.Forms.TextBox();
            this.DiCapFfLabel = new System.Windows.Forms.Label();
            this.DiCapAxesTextBox = new System.Windows.Forms.TextBox();
            this.AxeCountLabel = new System.Windows.Forms.Label();
            this.DiCapButtonsTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.DiCapDPadsTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.DiEffectNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiEffectParamsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiEffectDynamicParameters = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.DiEffectsDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DiAxisDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // DiEffectsDataGridView
            // 
            this.DiEffectsDataGridView.AllowUserToAddRows = false;
            this.DiEffectsDataGridView.AllowUserToDeleteRows = false;
            this.DiEffectsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DiEffectsDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.DiEffectsDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.DiEffectsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DiEffectsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.DiEffectsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DiEffectsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DiEffectNameColumn,
            this.DiEffectParamsColumn,
            this.DiEffectDynamicParameters});
            this.DiEffectsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.DiEffectsDataGridView.Location = new System.Drawing.Point(3, 180);
            this.DiEffectsDataGridView.Name = "DiEffectsDataGridView";
            this.DiEffectsDataGridView.ReadOnly = true;
            this.DiEffectsDataGridView.RowHeadersVisible = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            this.DiEffectsDataGridView.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.DiEffectsDataGridView.Size = new System.Drawing.Size(609, 225);
            this.DiEffectsDataGridView.TabIndex = 0;
            // 
            // DiDPadLabel
            // 
            this.DiDPadLabel.AutoSize = true;
            this.DiDPadLabel.Location = new System.Drawing.Point(228, 32);
            this.DiDPadLabel.Name = "DiDPadLabel";
            this.DiDPadLabel.Size = new System.Drawing.Size(40, 13);
            this.DiDPadLabel.TabIndex = 0;
            this.DiDPadLabel.Text = "D-Pad:";
            // 
            // DiUvaLabel
            // 
            this.DiUvaLabel.AutoSize = true;
            this.DiUvaLabel.Location = new System.Drawing.Point(228, 58);
            this.DiUvaLabel.Name = "DiUvaLabel";
            this.DiUvaLabel.Size = new System.Drawing.Size(40, 13);
            this.DiUvaLabel.TabIndex = 0;
            this.DiUvaLabel.Text = "U/V A:";
            // 
            // DiExtraVLabel
            // 
            this.DiExtraVLabel.AutoSize = true;
            this.DiExtraVLabel.Location = new System.Drawing.Point(102, 58);
            this.DiExtraVLabel.Name = "DiExtraVLabel";
            this.DiExtraVLabel.Size = new System.Drawing.Size(44, 13);
            this.DiExtraVLabel.TabIndex = 0;
            this.DiExtraVLabel.Text = "Extra V:";
            // 
            // DiExtraFLabel
            // 
            this.DiExtraFLabel.AutoSize = true;
            this.DiExtraFLabel.Location = new System.Drawing.Point(103, 32);
            this.DiExtraFLabel.Name = "DiExtraFLabel";
            this.DiExtraFLabel.Size = new System.Drawing.Size(43, 13);
            this.DiExtraFLabel.TabIndex = 0;
            this.DiExtraFLabel.Text = "Extra F:";
            // 
            // DiExtraALabel
            // 
            this.DiExtraALabel.AutoSize = true;
            this.DiExtraALabel.Location = new System.Drawing.Point(103, 6);
            this.DiExtraALabel.Name = "DiExtraALabel";
            this.DiExtraALabel.Size = new System.Drawing.Size(44, 13);
            this.DiExtraALabel.TabIndex = 0;
            this.DiExtraALabel.Text = "Extra A:";
            // 
            // DiButtonsLabel
            // 
            this.DiButtonsLabel.AutoSize = true;
            this.DiButtonsLabel.Location = new System.Drawing.Point(228, 6);
            this.DiButtonsLabel.Name = "DiButtonsLabel";
            this.DiButtonsLabel.Size = new System.Drawing.Size(46, 13);
            this.DiButtonsLabel.TabIndex = 0;
            this.DiButtonsLabel.Text = "Buttons:";
            // 
            // DiAxisDataGridView
            // 
            this.DiAxisDataGridView.AllowUserToAddRows = false;
            this.DiAxisDataGridView.AllowUserToDeleteRows = false;
            this.DiAxisDataGridView.AllowUserToResizeColumns = false;
            this.DiAxisDataGridView.AllowUserToResizeRows = false;
            this.DiAxisDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.DiAxisDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.DiAxisDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.DiAxisDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DiAxisDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.DiAxisDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DiColumnAxis,
            this.DiColumnM,
            this.DiColumnR,
            this.DiColumnA,
            this.DiColumnAR,
            this.DiColumnF,
            this.DiColumnFr,
            this.DiColumnV,
            this.DiColumnVr});
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DiAxisDataGridView.DefaultCellStyle = dataGridViewCellStyle6;
            this.DiAxisDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.DiAxisDataGridView.Location = new System.Drawing.Point(3, 84);
            this.DiAxisDataGridView.Name = "DiAxisDataGridView";
            this.DiAxisDataGridView.ReadOnly = true;
            this.DiAxisDataGridView.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.DiAxisDataGridView.RowHeadersVisible = false;
            dataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control;
            this.DiAxisDataGridView.RowsDefaultCellStyle = dataGridViewCellStyle7;
            this.DiAxisDataGridView.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.DiAxisDataGridView.Size = new System.Drawing.Size(347, 90);
            this.DiAxisDataGridView.TabIndex = 0;
            // 
            // DiColumnAxis
            // 
            this.DiColumnAxis.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.DiColumnAxis.DataPropertyName = "Axis";
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.DiColumnAxis.DefaultCellStyle = dataGridViewCellStyle5;
            this.DiColumnAxis.HeaderText = "Axis";
            this.DiColumnAxis.Name = "DiColumnAxis";
            this.DiColumnAxis.ReadOnly = true;
            this.DiColumnAxis.Width = 32;
            // 
            // DiColumnM
            // 
            this.DiColumnM.DataPropertyName = "M";
            this.DiColumnM.HeaderText = "M";
            this.DiColumnM.Name = "DiColumnM";
            this.DiColumnM.ReadOnly = true;
            this.DiColumnM.ToolTipText = "Movement";
            // 
            // DiColumnR
            // 
            this.DiColumnR.DataPropertyName = "R";
            this.DiColumnR.HeaderText = "R";
            this.DiColumnR.Name = "DiColumnR";
            this.DiColumnR.ReadOnly = true;
            this.DiColumnR.ToolTipText = "Rotation";
            // 
            // DiColumnA
            // 
            this.DiColumnA.DataPropertyName = "A";
            this.DiColumnA.HeaderText = "A";
            this.DiColumnA.Name = "DiColumnA";
            this.DiColumnA.ReadOnly = true;
            this.DiColumnA.ToolTipText = "Acceleration";
            // 
            // DiColumnAR
            // 
            this.DiColumnAR.DataPropertyName = "AR";
            this.DiColumnAR.HeaderText = "AR";
            this.DiColumnAR.Name = "DiColumnAR";
            this.DiColumnAR.ReadOnly = true;
            this.DiColumnAR.ToolTipText = "Angular Acceleration";
            // 
            // DiColumnF
            // 
            this.DiColumnF.DataPropertyName = "F";
            this.DiColumnF.HeaderText = "F";
            this.DiColumnF.Name = "DiColumnF";
            this.DiColumnF.ReadOnly = true;
            this.DiColumnF.ToolTipText = "Force";
            // 
            // DiColumnFr
            // 
            this.DiColumnFr.DataPropertyName = "FR";
            this.DiColumnFr.HeaderText = "FR";
            this.DiColumnFr.Name = "DiColumnFr";
            this.DiColumnFr.ReadOnly = true;
            this.DiColumnFr.ToolTipText = "Torque";
            // 
            // DiColumnV
            // 
            this.DiColumnV.DataPropertyName = "V";
            this.DiColumnV.HeaderText = "V";
            this.DiColumnV.Name = "DiColumnV";
            this.DiColumnV.ReadOnly = true;
            this.DiColumnV.ToolTipText = "Velocity";
            // 
            // DiColumnVr
            // 
            this.DiColumnVr.DataPropertyName = "VR";
            this.DiColumnVr.HeaderText = "VR";
            this.DiColumnVr.Name = "DiColumnVr";
            this.DiColumnVr.ReadOnly = true;
            this.DiColumnVr.ToolTipText = "Angular Velocity";
            // 
            // DevicePidTextBox
            // 
            this.DevicePidTextBox.Location = new System.Drawing.Point(39, 55);
            this.DevicePidTextBox.Name = "DevicePidTextBox";
            this.DevicePidTextBox.ReadOnly = true;
            this.DevicePidTextBox.Size = new System.Drawing.Size(57, 20);
            this.DevicePidTextBox.TabIndex = 0;
            // 
            // DeviceVidTextBox
            // 
            this.DeviceVidTextBox.Location = new System.Drawing.Point(39, 29);
            this.DeviceVidTextBox.Name = "DeviceVidTextBox";
            this.DeviceVidTextBox.ReadOnly = true;
            this.DeviceVidTextBox.Size = new System.Drawing.Size(57, 20);
            this.DeviceVidTextBox.TabIndex = 0;
            // 
            // DiDPadTextBox
            // 
            this.DiDPadTextBox.Location = new System.Drawing.Point(280, 29);
            this.DiDPadTextBox.Name = "DiDPadTextBox";
            this.DiDPadTextBox.ReadOnly = true;
            this.DiDPadTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiDPadTextBox.TabIndex = 0;
            // 
            // DiUvSliderTextBox
            // 
            this.DiUvSliderTextBox.Location = new System.Drawing.Point(280, 55);
            this.DiUvSliderTextBox.Name = "DiUvSliderTextBox";
            this.DiUvSliderTextBox.ReadOnly = true;
            this.DiUvSliderTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiUvSliderTextBox.TabIndex = 0;
            // 
            // DiVSliderTextBox
            // 
            this.DiVSliderTextBox.Location = new System.Drawing.Point(152, 55);
            this.DiVSliderTextBox.Name = "DiVSliderTextBox";
            this.DiVSliderTextBox.ReadOnly = true;
            this.DiVSliderTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiVSliderTextBox.TabIndex = 0;
            // 
            // DiFSliderTextBox
            // 
            this.DiFSliderTextBox.Location = new System.Drawing.Point(152, 29);
            this.DiFSliderTextBox.Name = "DiFSliderTextBox";
            this.DiFSliderTextBox.ReadOnly = true;
            this.DiFSliderTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiFSliderTextBox.TabIndex = 0;
            // 
            // DiASliderTextBox
            // 
            this.DiASliderTextBox.Location = new System.Drawing.Point(152, 3);
            this.DiASliderTextBox.Name = "DiASliderTextBox";
            this.DiASliderTextBox.ReadOnly = true;
            this.DiASliderTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiASliderTextBox.TabIndex = 0;
            // 
            // DiButtonsTextBox
            // 
            this.DiButtonsTextBox.Location = new System.Drawing.Point(280, 3);
            this.DiButtonsTextBox.Name = "DiButtonsTextBox";
            this.DiButtonsTextBox.ReadOnly = true;
            this.DiButtonsTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiButtonsTextBox.TabIndex = 0;
            // 
            // DeviceTypeTextBox
            // 
            this.DeviceTypeTextBox.Location = new System.Drawing.Point(39, 3);
            this.DeviceTypeTextBox.Name = "DeviceTypeTextBox";
            this.DeviceTypeTextBox.ReadOnly = true;
            this.DeviceTypeTextBox.Size = new System.Drawing.Size(57, 20);
            this.DeviceTypeTextBox.TabIndex = 0;
            // 
            // DiDevicePidLabel
            // 
            this.DiDevicePidLabel.AutoSize = true;
            this.DiDevicePidLabel.Location = new System.Drawing.Point(5, 58);
            this.DiDevicePidLabel.Name = "DiDevicePidLabel";
            this.DiDevicePidLabel.Size = new System.Drawing.Size(28, 13);
            this.DiDevicePidLabel.TabIndex = 0;
            this.DiDevicePidLabel.Text = "PID:";
            // 
            // DiDeviceVidLabel
            // 
            this.DiDeviceVidLabel.AutoSize = true;
            this.DiDeviceVidLabel.Location = new System.Drawing.Point(5, 32);
            this.DiDeviceVidLabel.Name = "DiDeviceVidLabel";
            this.DiDeviceVidLabel.Size = new System.Drawing.Size(28, 13);
            this.DiDeviceVidLabel.TabIndex = 0;
            this.DiDeviceVidLabel.Text = "VID:";
            // 
            // DiDeviceTypeLabel
            // 
            this.DiDeviceTypeLabel.AutoSize = true;
            this.DiDeviceTypeLabel.Location = new System.Drawing.Point(5, 6);
            this.DiDeviceTypeLabel.Name = "DiDeviceTypeLabel";
            this.DiDeviceTypeLabel.Size = new System.Drawing.Size(34, 13);
            this.DiDeviceTypeLabel.TabIndex = 0;
            this.DiDeviceTypeLabel.Text = "Type:";
            // 
            // DeviceInstanceGuidTextBox
            // 
            this.DeviceInstanceGuidTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DeviceInstanceGuidTextBox.Location = new System.Drawing.Point(387, 55);
            this.DeviceInstanceGuidTextBox.Name = "DeviceInstanceGuidTextBox";
            this.DeviceInstanceGuidTextBox.ReadOnly = true;
            this.DeviceInstanceGuidTextBox.Size = new System.Drawing.Size(225, 20);
            this.DeviceInstanceGuidTextBox.TabIndex = 0;
            // 
            // DeviceProductGuidTextBox
            // 
            this.DeviceProductGuidTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DeviceProductGuidTextBox.Location = new System.Drawing.Point(387, 29);
            this.DeviceProductGuidTextBox.Name = "DeviceProductGuidTextBox";
            this.DeviceProductGuidTextBox.ReadOnly = true;
            this.DeviceProductGuidTextBox.Size = new System.Drawing.Size(225, 20);
            this.DeviceProductGuidTextBox.TabIndex = 0;
            // 
            // DiProductGuidLabel
            // 
            this.DiProductGuidLabel.AutoSize = true;
            this.DiProductGuidLabel.Location = new System.Drawing.Point(356, 32);
            this.DiProductGuidLabel.Name = "DiProductGuidLabel";
            this.DiProductGuidLabel.Size = new System.Drawing.Size(25, 13);
            this.DiProductGuidLabel.TabIndex = 0;
            this.DiProductGuidLabel.Text = "PG:";
            // 
            // DiInstanceGuidLabel
            // 
            this.DiInstanceGuidLabel.AutoSize = true;
            this.DiInstanceGuidLabel.Location = new System.Drawing.Point(356, 58);
            this.DiInstanceGuidLabel.Name = "DiInstanceGuidLabel";
            this.DiInstanceGuidLabel.Size = new System.Drawing.Size(21, 13);
            this.DiInstanceGuidLabel.TabIndex = 0;
            this.DiInstanceGuidLabel.Text = "IG:";
            // 
            // DeviceProductNameTextBox
            // 
            this.DeviceProductNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DeviceProductNameTextBox.Location = new System.Drawing.Point(387, 3);
            this.DeviceProductNameTextBox.Name = "DeviceProductNameTextBox";
            this.DeviceProductNameTextBox.ReadOnly = true;
            this.DeviceProductNameTextBox.Size = new System.Drawing.Size(225, 20);
            this.DeviceProductNameTextBox.TabIndex = 0;
            // 
            // DiProductNameLabel
            // 
            this.DiProductNameLabel.AutoSize = true;
            this.DiProductNameLabel.Location = new System.Drawing.Point(356, 6);
            this.DiProductNameLabel.Name = "DiProductNameLabel";
            this.DiProductNameLabel.Size = new System.Drawing.Size(25, 13);
            this.DiProductNameLabel.TabIndex = 0;
            this.DiProductNameLabel.Text = "PN:";
            // 
            // MapToControllerLabel
            // 
            this.MapToControllerLabel.AutoSize = true;
            this.MapToControllerLabel.Location = new System.Drawing.Point(444, 136);
            this.MapToControllerLabel.Name = "MapToControllerLabel";
            this.MapToControllerLabel.Size = new System.Drawing.Size(90, 13);
            this.MapToControllerLabel.TabIndex = 0;
            this.MapToControllerLabel.Text = "Map to Controller:";
            // 
            // MapToPadComboBox
            // 
            this.MapToPadComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MapToPadComboBox.FormattingEnabled = true;
            this.MapToPadComboBox.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4"});
            this.MapToPadComboBox.Location = new System.Drawing.Point(542, 133);
            this.MapToPadComboBox.Name = "MapToPadComboBox";
            this.MapToPadComboBox.Size = new System.Drawing.Size(70, 21);
            this.MapToPadComboBox.TabIndex = 1;
            // 
            // DiCapFfStateTextBox
            // 
            this.DiCapFfStateTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DiCapFfStateTextBox.Location = new System.Drawing.Point(542, 81);
            this.DiCapFfStateTextBox.Name = "DiCapFfStateTextBox";
            this.DiCapFfStateTextBox.ReadOnly = true;
            this.DiCapFfStateTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiCapFfStateTextBox.TabIndex = 0;
            // 
            // DiCapFfLabel
            // 
            this.DiCapFfLabel.AutoSize = true;
            this.DiCapFfLabel.Location = new System.Drawing.Point(484, 84);
            this.DiCapFfLabel.Name = "DiCapFfLabel";
            this.DiCapFfLabel.Size = new System.Drawing.Size(50, 13);
            this.DiCapFfLabel.TabIndex = 0;
            this.DiCapFfLabel.Text = "FF State:";
            // 
            // DiCapAxesTextBox
            // 
            this.DiCapAxesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DiCapAxesTextBox.Location = new System.Drawing.Point(542, 107);
            this.DiCapAxesTextBox.Name = "DiCapAxesTextBox";
            this.DiCapAxesTextBox.ReadOnly = true;
            this.DiCapAxesTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiCapAxesTextBox.TabIndex = 0;
            // 
            // AxeCountLabel
            // 
            this.AxeCountLabel.AutoSize = true;
            this.AxeCountLabel.Location = new System.Drawing.Point(484, 110);
            this.AxeCountLabel.Name = "AxeCountLabel";
            this.AxeCountLabel.Size = new System.Drawing.Size(33, 13);
            this.AxeCountLabel.TabIndex = 0;
            this.AxeCountLabel.Text = "Axes:";
            // 
            // DiCapButtonsTextBox
            // 
            this.DiCapButtonsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DiCapButtonsTextBox.Location = new System.Drawing.Point(408, 81);
            this.DiCapButtonsTextBox.Name = "DiCapButtonsTextBox";
            this.DiCapButtonsTextBox.ReadOnly = true;
            this.DiCapButtonsTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiCapButtonsTextBox.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(356, 84);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Buttons:";
            // 
            // DiCapDPadsTextBox
            // 
            this.DiCapDPadsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DiCapDPadsTextBox.Location = new System.Drawing.Point(408, 107);
            this.DiCapDPadsTextBox.Name = "DiCapDPadsTextBox";
            this.DiCapDPadsTextBox.ReadOnly = true;
            this.DiCapDPadsTextBox.Size = new System.Drawing.Size(70, 20);
            this.DiCapDPadsTextBox.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(356, 110);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "D-Pads:";
            // 
            // DiEffectNameColumn
            // 
            this.DiEffectNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DiEffectNameColumn.DataPropertyName = "Effect";
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DiEffectNameColumn.DefaultCellStyle = dataGridViewCellStyle1;
            this.DiEffectNameColumn.HeaderText = "FF Effect";
            this.DiEffectNameColumn.MinimumWidth = 75;
            this.DiEffectNameColumn.Name = "DiEffectNameColumn";
            this.DiEffectNameColumn.ReadOnly = true;
            this.DiEffectNameColumn.ToolTipText = "Supported force feedback effects";
            this.DiEffectNameColumn.Width = 75;
            // 
            // DiEffectParamsColumn
            // 
            this.DiEffectParamsColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DiEffectParamsColumn.DataPropertyName = "Parameters";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DiEffectParamsColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.DiEffectParamsColumn.HeaderText = "Parameters";
            this.DiEffectParamsColumn.Name = "DiEffectParamsColumn";
            this.DiEffectParamsColumn.ReadOnly = true;
            this.DiEffectParamsColumn.ToolTipText = "Parameters supported by the effect";
            // 
            // DiEffectDynamicParameters
            // 
            this.DiEffectDynamicParameters.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DiEffectDynamicParameters.DataPropertyName = "DynamicParameters";
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.DiEffectDynamicParameters.DefaultCellStyle = dataGridViewCellStyle3;
            this.DiEffectDynamicParameters.HeaderText = "Dynamic Parameters";
            this.DiEffectDynamicParameters.Name = "DiEffectDynamicParameters";
            this.DiEffectDynamicParameters.ReadOnly = true;
            this.DiEffectDynamicParameters.ToolTipText = "Parameters of the effect that can be modified while the effect is playing";
            // 
            // DirectInputControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MapToPadComboBox);
            this.Controls.Add(this.DiEffectsDataGridView);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.AxeCountLabel);
            this.Controls.Add(this.DiCapFfLabel);
            this.Controls.Add(this.DiInstanceGuidLabel);
            this.Controls.Add(this.DiProductNameLabel);
            this.Controls.Add(this.DiProductGuidLabel);
            this.Controls.Add(this.DiDPadLabel);
            this.Controls.Add(this.DiUvaLabel);
            this.Controls.Add(this.DiExtraVLabel);
            this.Controls.Add(this.DiExtraFLabel);
            this.Controls.Add(this.DiExtraALabel);
            this.Controls.Add(this.MapToControllerLabel);
            this.Controls.Add(this.DiButtonsLabel);
            this.Controls.Add(this.DiAxisDataGridView);
            this.Controls.Add(this.DevicePidTextBox);
            this.Controls.Add(this.DeviceVidTextBox);
            this.Controls.Add(this.DiDPadTextBox);
            this.Controls.Add(this.DeviceProductNameTextBox);
            this.Controls.Add(this.DeviceProductGuidTextBox);
            this.Controls.Add(this.DiCapDPadsTextBox);
            this.Controls.Add(this.DiCapButtonsTextBox);
            this.Controls.Add(this.DiCapAxesTextBox);
            this.Controls.Add(this.DiCapFfStateTextBox);
            this.Controls.Add(this.DeviceInstanceGuidTextBox);
            this.Controls.Add(this.DiUvSliderTextBox);
            this.Controls.Add(this.DiVSliderTextBox);
            this.Controls.Add(this.DiFSliderTextBox);
            this.Controls.Add(this.DiASliderTextBox);
            this.Controls.Add(this.DiButtonsTextBox);
            this.Controls.Add(this.DeviceTypeTextBox);
            this.Controls.Add(this.DiDevicePidLabel);
            this.Controls.Add(this.DiDeviceVidLabel);
            this.Controls.Add(this.DiDeviceTypeLabel);
            this.Name = "DirectInputControl";
            this.Size = new System.Drawing.Size(615, 408);
            ((System.ComponentModel.ISupportInitialize)(this.DiEffectsDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DiAxisDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        System.Windows.Forms.DataGridView DiEffectsDataGridView;
        System.Windows.Forms.Label DiDPadLabel;
        System.Windows.Forms.Label DiUvaLabel;
		System.Windows.Forms.Label DiExtraVLabel;
		System.Windows.Forms.Label DiExtraFLabel;
		System.Windows.Forms.Label DiExtraALabel;
		System.Windows.Forms.Label DiButtonsLabel;
		System.Windows.Forms.DataGridView DiAxisDataGridView;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnAxis;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnM;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnR;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnA;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnAR;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnF;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnFr;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnV;
		System.Windows.Forms.DataGridViewTextBoxColumn DiColumnVr;
		System.Windows.Forms.TextBox DiDPadTextBox;
		System.Windows.Forms.TextBox DiUvSliderTextBox;
		System.Windows.Forms.TextBox DiVSliderTextBox;
		System.Windows.Forms.TextBox DiFSliderTextBox;
		System.Windows.Forms.TextBox DiASliderTextBox;
		System.Windows.Forms.TextBox DiButtonsTextBox;
		System.Windows.Forms.TextBox DeviceTypeTextBox;
		System.Windows.Forms.Label DiDevicePidLabel;
		System.Windows.Forms.Label DiDeviceVidLabel;
		System.Windows.Forms.Label DiDeviceTypeLabel;
		public System.Windows.Forms.TextBox DevicePidTextBox;
        public System.Windows.Forms.TextBox DeviceVidTextBox;
		System.Windows.Forms.Label DiProductGuidLabel;
		System.Windows.Forms.Label DiInstanceGuidLabel;
		System.Windows.Forms.Label DiProductNameLabel;
		public System.Windows.Forms.TextBox DeviceProductNameTextBox;
        public System.Windows.Forms.TextBox DeviceInstanceGuidTextBox;
        public System.Windows.Forms.TextBox DeviceProductGuidTextBox;
        private System.Windows.Forms.Label MapToControllerLabel;
        public System.Windows.Forms.TextBox DiCapFfStateTextBox;
        private System.Windows.Forms.Label DiCapFfLabel;
        public System.Windows.Forms.TextBox DiCapAxesTextBox;
        private System.Windows.Forms.Label AxeCountLabel;
        public System.Windows.Forms.TextBox DiCapButtonsTextBox;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox DiCapDPadsTextBox;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.ComboBox MapToPadComboBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectParamsColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectDynamicParameters;
	}
}
