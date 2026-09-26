namespace x360ce.App.Controls
{
	partial class XInputDevicesUserControl
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		private void InitializeComponent()
		{
			this.DevicesToolStrip = new System.Windows.Forms.ToolStrip();
			this.ApplyButton = new System.Windows.Forms.ToolStripButton();
			this.AutoOrderButton = new System.Windows.Forms.ToolStripButton();
			this.RefreshButton = new System.Windows.Forms.ToolStripButton();
			this.DevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.PlanPanel = new System.Windows.Forms.TableLayoutPanel();
			this.PlanSubjectLabel = new System.Windows.Forms.Label();
			this.PlanBodyLabel = new System.Windows.Forms.Label();
			this.PlanOkButton = new System.Windows.Forms.Button();
			this.PlanCancelButton = new System.Windows.Forms.Button();
			this.MoveUpColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.MoveDownColumn = new System.Windows.Forms.DataGridViewImageColumn();
			this.PlaceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PadColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MappedColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DevicesToolStrip.SuspendLayout();
			this.PlanPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).BeginInit();
			this.SuspendLayout();
			//
			// DevicesToolStrip
			//
			this.DevicesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ApplyButton,
			this.AutoOrderButton,
			this.RefreshButton});
			this.DevicesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.DevicesToolStrip.Name = "DevicesToolStrip";
			this.DevicesToolStrip.Size = new System.Drawing.Size(700, 25);
			//
			// ApplyButton
			//
			this.ApplyButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.ApplyButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.ApplyButton.Image = global::x360ce.App.Properties.Resources.ok_16x16;
			this.ApplyButton.Name = "ApplyButton";
			this.ApplyButton.Text = "Apply";
			this.ApplyButton.ToolTipText = "Show what would be done to put the controllers in this order, then do it.";
			this.ApplyButton.Click += new System.EventHandler(this.ApplyButton_Click);
			//
			// AutoOrderButton
			//
			this.AutoOrderButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.AutoOrderButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.AutoOrderButton.Image = global::x360ce.App.Properties.Resources.map_to_16x16;
			this.AutoOrderButton.Name = "AutoOrderButton";
			this.AutoOrderButton.Text = "Auto-Order";
			this.AutoOrderButton.ToolTipText = "Put the virtual controller of Controller 1 in XInput 1, of Controller 2 in XInput 2, and so on. Real controllers take the places left.";
			this.AutoOrderButton.Click += new System.EventHandler(this.AutoOrderButton_Click);
			//
			// RefreshButton
			//
			this.RefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.RefreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.RefreshButton.Image = global::x360ce.App.Properties.Resources.refresh_16x16;
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.ToolTipText = "Read the places again.";
			this.RefreshButton.Click += new System.EventHandler(this.Refresh_Click);
			//
			// PlanPanel
			//
			this.PlanPanel.AutoSize = true;
			this.PlanPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.PlanPanel.BackColor = System.Drawing.SystemColors.Info;
			this.PlanPanel.ColumnCount = 3;
			this.PlanPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.PlanPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
			this.PlanPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
			this.PlanPanel.Controls.Add(this.PlanSubjectLabel, 0, 0);
			this.PlanPanel.Controls.Add(this.PlanBodyLabel, 0, 1);
			this.PlanPanel.Controls.Add(this.PlanOkButton, 1, 2);
			this.PlanPanel.Controls.Add(this.PlanCancelButton, 2, 2);
			this.PlanPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.PlanPanel.Name = "PlanPanel";
			this.PlanPanel.Padding = new System.Windows.Forms.Padding(3);
			this.PlanPanel.RowCount = 3;
			this.PlanPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.PlanPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.PlanPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.PlanPanel.TabIndex = 2;
			this.PlanPanel.Visible = false;
			//
			// PlanSubjectLabel
			//
			this.PlanSubjectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.PlanSubjectLabel.AutoSize = true;
			this.PlanPanel.SetColumnSpan(this.PlanSubjectLabel, 3);
			this.PlanSubjectLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
			this.PlanSubjectLabel.Name = "PlanSubjectLabel";
			this.PlanSubjectLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.PlanSubjectLabel.TabIndex = 0;
			//
			// PlanBodyLabel
			//
			this.PlanBodyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.PlanBodyLabel.AutoSize = true;
			this.PlanPanel.SetColumnSpan(this.PlanBodyLabel, 3);
			this.PlanBodyLabel.Name = "PlanBodyLabel";
			this.PlanBodyLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3);
			this.PlanBodyLabel.TabIndex = 1;
			//
			// PlanOkButton
			//
			this.PlanOkButton.AutoSize = true;
			this.PlanOkButton.Image = global::x360ce.App.Properties.Resources.ok_16x16;
			this.PlanOkButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.PlanOkButton.Name = "PlanOkButton";
			this.PlanOkButton.TabIndex = 2;
			this.PlanOkButton.Text = "OK";
			this.PlanOkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.PlanOkButton.UseVisualStyleBackColor = true;
			this.PlanOkButton.Click += new System.EventHandler(this.PlanOkButton_Click);
			//
			// PlanCancelButton
			//
			this.PlanCancelButton.AutoSize = true;
			this.PlanCancelButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.PlanCancelButton.Name = "PlanCancelButton";
			this.PlanCancelButton.TabIndex = 3;
			this.PlanCancelButton.Text = "Cancel";
			this.PlanCancelButton.UseVisualStyleBackColor = true;
			this.PlanCancelButton.Click += new System.EventHandler(this.PlanCancelButton_Click);
			//
			// DevicesDataGridView
			//
			this.DevicesDataGridView.AccessibleName = "XInput devices";
			this.DevicesDataGridView.AllowUserToAddRows = false;
			this.DevicesDataGridView.AllowUserToResizeColumns = false;
			this.DevicesDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.DevicesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.DevicesDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
			this.DevicesDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
			this.DevicesDataGridView.EnableHeadersVisualStyles = false;
			this.DevicesDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.DevicesDataGridView.AllowUserToDeleteRows = false;
			this.DevicesDataGridView.AllowUserToResizeRows = false;
			this.DevicesDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.DevicesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.DevicesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.MoveUpColumn,
			this.MoveDownColumn,
			this.PlaceColumn,
			this.PadColumn,
			this.NameColumn,
			this.MappedColumn});
			this.DevicesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.DevicesDataGridView.MultiSelect = false;
			this.DevicesDataGridView.Name = "DevicesDataGridView";
			this.DevicesDataGridView.ReadOnly = true;
			this.DevicesDataGridView.RowHeadersVisible = false;
			this.DevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DevicesDataGridView.Size = new System.Drawing.Size(700, 275);
			this.DevicesDataGridView.SelectionChanged += new System.EventHandler(this.DevicesDataGridView_SelectionChanged);
			this.DevicesDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DevicesDataGridView_CellContentClick);
			//
			// MoveUpColumn
			//
			this.MoveUpColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.MoveUpColumn.DefaultCellStyle.NullValue = null;
			this.MoveUpColumn.HeaderText = "";
			this.MoveUpColumn.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Normal;
			this.MoveUpColumn.MinimumWidth = 24;
			this.MoveUpColumn.Name = "MoveUpColumn";
			this.MoveUpColumn.ReadOnly = true;
			this.MoveUpColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.MoveUpColumn.ToolTipText = "Ask for this controller to take an earlier XInput place.";
			this.MoveUpColumn.Width = 24;
			//
			// MoveDownColumn
			//
			this.MoveDownColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			this.MoveDownColumn.DefaultCellStyle.NullValue = null;
			this.MoveDownColumn.HeaderText = "";
			this.MoveDownColumn.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Normal;
			this.MoveDownColumn.MinimumWidth = 24;
			this.MoveDownColumn.Name = "MoveDownColumn";
			this.MoveDownColumn.ReadOnly = true;
			this.MoveDownColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.MoveDownColumn.ToolTipText = "Ask for this controller to take a later XInput place.";
			this.MoveDownColumn.Width = 24;
			//
			// PlaceColumn
			//
			this.PlaceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PlaceColumn.HeaderText = "XInput";
			this.PlaceColumn.Name = "PlaceColumn";
			this.PlaceColumn.ReadOnly = true;
			this.PlaceColumn.ToolTipText = "Which of the four XInput places this controller holds. Blank when nothing can say.";
			//
			// PadColumn
			//
			this.PadColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.PadColumn.HeaderText = "Controller";
			this.PadColumn.Name = "PadColumn";
			this.PadColumn.ReadOnly = true;
			this.PadColumn.ToolTipText = "The controller tab whose place this is: XInput N belongs to Controller N. A waiting virtual controller shows the tab it waits for. Blank for a controller that holds no place.";
			//
			// NameColumn
			//
			this.NameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.NameColumn.HeaderText = "Device";
			this.NameColumn.Name = "NameColumn";
			this.NameColumn.ReadOnly = true;
			//
			// MappedColumn
			//
			this.MappedColumn.FillWeight = 100F;
			this.MappedColumn.HeaderText = "Product Names";
			this.MappedColumn.Name = "MappedColumn";
			this.MappedColumn.ReadOnly = true;
			this.MappedColumn.ToolTipText = "The devices mapped to the controller tab this virtual controller is made for. Blank for a real controller.";
			//
			// XInputDevicesUserControl
			//
			this.Controls.Add(this.DevicesDataGridView);
			this.Controls.Add(this.PlanPanel);
			this.Controls.Add(this.DevicesToolStrip);
			this.Name = "XInputDevicesUserControl";
			this.Size = new System.Drawing.Size(700, 300);
			this.DevicesToolStrip.ResumeLayout(false);
			this.PlanPanel.ResumeLayout(false);
			this.PlanPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ToolStrip DevicesToolStrip;
		private System.Windows.Forms.ToolStripButton ApplyButton;
		private System.Windows.Forms.ToolStripButton AutoOrderButton;
		private System.Windows.Forms.ToolStripButton RefreshButton;
		private System.Windows.Forms.DataGridView DevicesDataGridView;
		private System.Windows.Forms.TableLayoutPanel PlanPanel;
		private System.Windows.Forms.Label PlanSubjectLabel;
		private System.Windows.Forms.Label PlanBodyLabel;
		private System.Windows.Forms.Button PlanOkButton;
		private System.Windows.Forms.Button PlanCancelButton;
		private System.Windows.Forms.DataGridViewImageColumn MoveUpColumn;
		private System.Windows.Forms.DataGridViewImageColumn MoveDownColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PlaceColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn PadColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn MappedColumn;
	}
}
