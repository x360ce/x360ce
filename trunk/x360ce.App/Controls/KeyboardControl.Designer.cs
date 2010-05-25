namespace x360ce.App.Controls
{
	partial class KeyboardControl
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
			this.components = new System.ComponentModel.Container();
			this.MapDataGridView = new System.Windows.Forms.DataGridView();
			this.KeyboardKeyColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DirectInputColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.KeyboardContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.KeyboardTextBox = new System.Windows.Forms.TextBox();
			this.AddButton = new System.Windows.Forms.Button();
			this.LeftTriggerComboBox = new System.Windows.Forms.ComboBox();
			this.KeyboardLabel = new System.Windows.Forms.Label();
			this.LoopCheckBox = new System.Windows.Forms.CheckBox();
			this.DelayNumericUpDown = new System.Windows.Forms.NumericUpDown();
			this.DelayLabel = new System.Windows.Forms.Label();
			this.ScriptTextBox = new System.Windows.Forms.TextBox();
			this.AppendButton = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.MapDataGridView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DelayNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// MapDataGridView
			// 
			this.MapDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.MapDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MapDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.KeyboardKeyColumn,
            this.DirectInputColumn,
            this.DescriptionColumn});
			this.MapDataGridView.Location = new System.Drawing.Point(3, 3);
			this.MapDataGridView.Name = "MapDataGridView";
			this.MapDataGridView.Size = new System.Drawing.Size(549, 361);
			this.MapDataGridView.TabIndex = 0;
			this.MapDataGridView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.MapDataGridView_PreviewKeyDown);
			// 
			// KeyboardKeyColumn
			// 
			this.KeyboardKeyColumn.HeaderText = "Keyboard Key";
			this.KeyboardKeyColumn.Name = "KeyboardKeyColumn";
			this.KeyboardKeyColumn.ReadOnly = true;
			// 
			// DirectInputColumn
			// 
			this.DirectInputColumn.HeaderText = "Direct Input";
			this.DirectInputColumn.Name = "DirectInputColumn";
			this.DirectInputColumn.ReadOnly = true;
			// 
			// DescriptionColumn
			// 
			this.DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.DescriptionColumn.HeaderText = "Description";
			this.DescriptionColumn.Name = "DescriptionColumn";
			this.DescriptionColumn.ReadOnly = true;
			// 
			// KeyboardContextMenuStrip
			// 
			this.KeyboardContextMenuStrip.Name = "KeyboardContextMenuStrip";
			this.KeyboardContextMenuStrip.Size = new System.Drawing.Size(61, 4);
			// 
			// KeyboardTextBox
			// 
			this.KeyboardTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.KeyboardTextBox.Location = new System.Drawing.Point(3, 410);
			this.KeyboardTextBox.Name = "KeyboardTextBox";
			this.KeyboardTextBox.Size = new System.Drawing.Size(140, 20);
			this.KeyboardTextBox.TabIndex = 1;
			this.KeyboardTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyboardTextBox_KeyDown);
			this.KeyboardTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.KeyboardTextBox_KeyPress);
			this.KeyboardTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.KeyboardTextBox_KeyUp);
			this.KeyboardTextBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.KeyboardTextBox_PreviewKeyDown);
			// 
			// AddButton
			// 
			this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AddButton.Location = new System.Drawing.Point(477, 370);
			this.AddButton.Name = "AddButton";
			this.AddButton.Size = new System.Drawing.Size(75, 23);
			this.AddButton.TabIndex = 2;
			this.AddButton.Text = "Add";
			this.AddButton.UseVisualStyleBackColor = true;
			// 
			// LeftTriggerComboBox
			// 
			this.LeftTriggerComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LeftTriggerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.LeftTriggerComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.LeftTriggerComboBox.FormattingEnabled = true;
			this.LeftTriggerComboBox.Location = new System.Drawing.Point(3, 370);
			this.LeftTriggerComboBox.Name = "LeftTriggerComboBox";
			this.LeftTriggerComboBox.Size = new System.Drawing.Size(89, 21);
			this.LeftTriggerComboBox.TabIndex = 43;
			// 
			// KeyboardLabel
			// 
			this.KeyboardLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.KeyboardLabel.AutoSize = true;
			this.KeyboardLabel.Location = new System.Drawing.Point(3, 394);
			this.KeyboardLabel.Name = "KeyboardLabel";
			this.KeyboardLabel.Size = new System.Drawing.Size(55, 13);
			this.KeyboardLabel.TabIndex = 44;
			this.KeyboardLabel.Text = "Keyboard:";
			// 
			// LoopCheckBox
			// 
			this.LoopCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoopCheckBox.AutoSize = true;
			this.LoopCheckBox.Location = new System.Drawing.Point(149, 437);
			this.LoopCheckBox.Name = "LoopCheckBox";
			this.LoopCheckBox.Size = new System.Drawing.Size(50, 17);
			this.LoopCheckBox.TabIndex = 45;
			this.LoopCheckBox.Text = "Loop";
			this.LoopCheckBox.UseVisualStyleBackColor = true;
			// 
			// DelayNumericUpDown
			// 
			this.DelayNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DelayNumericUpDown.Location = new System.Drawing.Point(149, 411);
			this.DelayNumericUpDown.Name = "DelayNumericUpDown";
			this.DelayNumericUpDown.Size = new System.Drawing.Size(75, 20);
			this.DelayNumericUpDown.TabIndex = 46;
			// 
			// DelayLabel
			// 
			this.DelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DelayLabel.AutoSize = true;
			this.DelayLabel.Location = new System.Drawing.Point(146, 395);
			this.DelayLabel.Name = "DelayLabel";
			this.DelayLabel.Size = new System.Drawing.Size(37, 13);
			this.DelayLabel.TabIndex = 44;
			this.DelayLabel.Text = "Delay:";
			// 
			// ScriptTextBox
			// 
			this.ScriptTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.ScriptTextBox.Location = new System.Drawing.Point(230, 370);
			this.ScriptTextBox.Multiline = true;
			this.ScriptTextBox.Name = "ScriptTextBox";
			this.ScriptTextBox.Size = new System.Drawing.Size(241, 84);
			this.ScriptTextBox.TabIndex = 1;
			this.ScriptTextBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBox1_PreviewKeyDown);
			// 
			// AppendButton
			// 
			this.AppendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AppendButton.Location = new System.Drawing.Point(149, 370);
			this.AppendButton.Name = "AppendButton";
			this.AppendButton.Size = new System.Drawing.Size(75, 23);
			this.AppendButton.TabIndex = 2;
			this.AppendButton.Text = ">>";
			this.AppendButton.UseVisualStyleBackColor = true;
			this.AppendButton.Click += new System.EventHandler(this.AppendButton_Click);
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBox1.Location = new System.Drawing.Point(3, 434);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(140, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyboardTextBox_KeyDown);
			this.textBox1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.KeyboardTextBox_PreviewKeyDown);
			// 
			// KeyboardControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.KeyboardLabel);
			this.Controls.Add(this.DelayNumericUpDown);
			this.Controls.Add(this.LoopCheckBox);
			this.Controls.Add(this.DelayLabel);
			this.Controls.Add(this.LeftTriggerComboBox);
			this.Controls.Add(this.AddButton);
			this.Controls.Add(this.AppendButton);
			this.Controls.Add(this.MapDataGridView);
			this.Controls.Add(this.ScriptTextBox);
			this.Controls.Add(this.KeyboardTextBox);
			this.Controls.Add(this.textBox1);
			this.Name = "KeyboardControl";
			this.Size = new System.Drawing.Size(555, 457);
			this.Load += new System.EventHandler(this.MapKeyboardControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.MapDataGridView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DelayNumericUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView MapDataGridView;
		private System.Windows.Forms.DataGridViewTextBoxColumn KeyboardKeyColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DirectInputColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
		private System.Windows.Forms.ContextMenuStrip KeyboardContextMenuStrip;
		private System.Windows.Forms.TextBox KeyboardTextBox;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.ComboBox LeftTriggerComboBox;
		private System.Windows.Forms.Label KeyboardLabel;
		private System.Windows.Forms.CheckBox LoopCheckBox;
		private System.Windows.Forms.NumericUpDown DelayNumericUpDown;
		private System.Windows.Forms.Label DelayLabel;
		private System.Windows.Forms.TextBox ScriptTextBox;
		private System.Windows.Forms.Button AppendButton;
		private System.Windows.Forms.TextBox textBox1;
	}
}
