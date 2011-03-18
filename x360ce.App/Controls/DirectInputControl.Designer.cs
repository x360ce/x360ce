namespace x360ce.App.Controls
{
	partial class DirectInputControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.DiEffectsDataGridView = new System.Windows.Forms.DataGridView();
            this.DiEffectNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiEffectParamsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiEffectDynamicParameters = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DiCapDPadsLabel = new System.Windows.Forms.Label();
            this.DiCapButtonsLabel = new System.Windows.Forms.Label();
            this.DiDPadLabel = new System.Windows.Forms.Label();
            this.DiCapAxesLabel = new System.Windows.Forms.Label();
            this.DiUvaLabel = new System.Windows.Forms.Label();
            this.DiCapFfLabel = new System.Windows.Forms.Label();
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
            this.DiEffectsDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.DiEffectsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.DiEffectsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.DiEffectsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.DiEffectsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.DiEffectNameColumn,
            this.DiEffectParamsColumn,
            this.DiEffectDynamicParameters});
            this.DiEffectsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.DiEffectsDataGridView.Location = new System.Drawing.Point(3, 174);
            this.DiEffectsDataGridView.Name = "DiEffectsDataGridView";
            this.DiEffectsDataGridView.ReadOnly = true;
            this.DiEffectsDataGridView.RowHeadersVisible = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            this.DiEffectsDataGridView.RowsDefaultCellStyle = dataGridViewCellStyle1;
            this.DiEffectsDataGridView.Size = new System.Drawing.Size(609, 231);
            this.DiEffectsDataGridView.TabIndex = 0;
            // 
            // DiEffectNameColumn
            // 
            this.DiEffectNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.DiEffectNameColumn.DataPropertyName = "Effect";
            this.DiEffectNameColumn.HeaderText = "FF Effect";
            this.DiEffectNameColumn.Name = "DiEffectNameColumn";
            this.DiEffectNameColumn.ReadOnly = true;
            this.DiEffectNameColumn.ToolTipText = "Supported force feedback effects";
            this.DiEffectNameColumn.Width = 70;
            // 
            // DiEffectParamsColumn
            // 
            this.DiEffectParamsColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DiEffectParamsColumn.DataPropertyName = "Parameters";
            this.DiEffectParamsColumn.HeaderText = "Parameters";
            this.DiEffectParamsColumn.Name = "DiEffectParamsColumn";
            this.DiEffectParamsColumn.ReadOnly = true;
            this.DiEffectParamsColumn.ToolTipText = "Parameters supported by the effect";
            // 
            // DiEffectDynamicParameters
            // 
            this.DiEffectDynamicParameters.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.DiEffectDynamicParameters.DataPropertyName = "DynamicParameters";
            this.DiEffectDynamicParameters.HeaderText = "Dynamic Parameters";
            this.DiEffectDynamicParameters.Name = "DiEffectDynamicParameters";
            this.DiEffectDynamicParameters.ReadOnly = true;
            this.DiEffectDynamicParameters.ToolTipText = "Parameters of the effect that can be modified while the effect is playing";
            // 
            // DiCapDPadsLabel
            // 
            this.DiCapDPadsLabel.AutoSize = true;
            this.DiCapDPadsLabel.Location = new System.Drawing.Point(427, 143);
            this.DiCapDPadsLabel.Name = "DiCapDPadsLabel";
            this.DiCapDPadsLabel.Size = new System.Drawing.Size(86, 13);
            this.DiCapDPadsLabel.TabIndex = 0;
            this.DiCapDPadsLabel.Text = "DiCapPovsLabel";
            // 
            // DiCapButtonsLabel
            // 
            this.DiCapButtonsLabel.AutoSize = true;
            this.DiCapButtonsLabel.Location = new System.Drawing.Point(427, 126);
            this.DiCapButtonsLabel.Name = "DiCapButtonsLabel";
            this.DiCapButtonsLabel.Size = new System.Drawing.Size(98, 13);
            this.DiCapButtonsLabel.TabIndex = 0;
            this.DiCapButtonsLabel.Text = "DiCapButtonsLabel";
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
            // DiCapAxesLabel
            // 
            this.DiCapAxesLabel.AutoSize = true;
            this.DiCapAxesLabel.Location = new System.Drawing.Point(427, 109);
            this.DiCapAxesLabel.Name = "DiCapAxesLabel";
            this.DiCapAxesLabel.Size = new System.Drawing.Size(85, 13);
            this.DiCapAxesLabel.TabIndex = 0;
            this.DiCapAxesLabel.Text = "DiCapAxesLabel";
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
            // DiCapFfLabel
            // 
            this.DiCapFfLabel.AutoSize = true;
            this.DiCapFfLabel.Location = new System.Drawing.Point(427, 92);
            this.DiCapFfLabel.Name = "DiCapFfLabel";
            this.DiCapFfLabel.Size = new System.Drawing.Size(71, 13);
            this.DiCapFfLabel.TabIndex = 0;
            this.DiCapFfLabel.Text = "DiCapFfLabel";
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
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.DiAxisDataGridView.DefaultCellStyle = dataGridViewCellStyle3;
            this.DiAxisDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
            this.DiAxisDataGridView.Location = new System.Drawing.Point(5, 84);
            this.DiAxisDataGridView.Name = "DiAxisDataGridView";
            this.DiAxisDataGridView.ReadOnly = true;
            this.DiAxisDataGridView.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.DiAxisDataGridView.RowHeadersVisible = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            this.DiAxisDataGridView.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.DiAxisDataGridView.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.DiAxisDataGridView.Size = new System.Drawing.Size(362, 84);
            this.DiAxisDataGridView.TabIndex = 0;
            // 
            // DiColumnAxis
            // 
            this.DiColumnAxis.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.DiColumnAxis.DataPropertyName = "Axis";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.DiColumnAxis.DefaultCellStyle = dataGridViewCellStyle2;
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
            // DirectInputControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DiEffectsDataGridView);
            this.Controls.Add(this.DiInstanceGuidLabel);
            this.Controls.Add(this.DiProductNameLabel);
            this.Controls.Add(this.DiProductGuidLabel);
            this.Controls.Add(this.DiCapDPadsLabel);
            this.Controls.Add(this.DiCapButtonsLabel);
            this.Controls.Add(this.DiDPadLabel);
            this.Controls.Add(this.DiCapAxesLabel);
            this.Controls.Add(this.DiUvaLabel);
            this.Controls.Add(this.DiCapFfLabel);
            this.Controls.Add(this.DiExtraVLabel);
            this.Controls.Add(this.DiExtraFLabel);
            this.Controls.Add(this.DiExtraALabel);
            this.Controls.Add(this.DiButtonsLabel);
            this.Controls.Add(this.DiAxisDataGridView);
            this.Controls.Add(this.DevicePidTextBox);
            this.Controls.Add(this.DeviceVidTextBox);
            this.Controls.Add(this.DiDPadTextBox);
            this.Controls.Add(this.DeviceProductNameTextBox);
            this.Controls.Add(this.DeviceProductGuidTextBox);
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

		private System.Windows.Forms.DataGridView DiEffectsDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectNameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectParamsColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiEffectDynamicParameters;
		private System.Windows.Forms.Label DiCapDPadsLabel;
		private System.Windows.Forms.Label DiCapButtonsLabel;
		private System.Windows.Forms.Label DiDPadLabel;
		private System.Windows.Forms.Label DiCapAxesLabel;
		private System.Windows.Forms.Label DiUvaLabel;
		private System.Windows.Forms.Label DiCapFfLabel;
		private System.Windows.Forms.Label DiExtraVLabel;
		private System.Windows.Forms.Label DiExtraFLabel;
		private System.Windows.Forms.Label DiExtraALabel;
		private System.Windows.Forms.Label DiButtonsLabel;
		private System.Windows.Forms.DataGridView DiAxisDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnAxis;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnM;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnR;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnA;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnAR;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnF;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnFr;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnV;
		private System.Windows.Forms.DataGridViewTextBoxColumn DiColumnVr;
		private System.Windows.Forms.TextBox DiDPadTextBox;
		private System.Windows.Forms.TextBox DiUvSliderTextBox;
		private System.Windows.Forms.TextBox DiVSliderTextBox;
		private System.Windows.Forms.TextBox DiFSliderTextBox;
		private System.Windows.Forms.TextBox DiASliderTextBox;
		private System.Windows.Forms.TextBox DiButtonsTextBox;
		private System.Windows.Forms.TextBox DeviceTypeTextBox;
		private System.Windows.Forms.Label DiDevicePidLabel;
		private System.Windows.Forms.Label DiDeviceVidLabel;
		private System.Windows.Forms.Label DiDeviceTypeLabel;
		public System.Windows.Forms.TextBox DevicePidTextBox;
        public System.Windows.Forms.TextBox DeviceVidTextBox;
		private System.Windows.Forms.Label DiProductGuidLabel;
		private System.Windows.Forms.Label DiInstanceGuidLabel;
		private System.Windows.Forms.Label DiProductNameLabel;
		public System.Windows.Forms.TextBox DeviceProductNameTextBox;
        public System.Windows.Forms.TextBox DeviceInstanceGuidTextBox;
        public System.Windows.Forms.TextBox DeviceProductGuidTextBox;
	}
}
