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
			this.MoveUpButton = new System.Windows.Forms.ToolStripButton();
			this.MoveDownButton = new System.Windows.Forms.ToolStripButton();
			this.ApplyButton = new System.Windows.Forms.ToolStripButton();
			this.RefreshButton = new System.Windows.Forms.ToolStripButton();
			this.DevicesDataGridView = new System.Windows.Forms.DataGridView();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.PlaceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.NameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.DevicesToolStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).BeginInit();
			this.SuspendLayout();
			//
			// DevicesToolStrip
			//
			this.DevicesToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.MoveUpButton,
			this.MoveDownButton,
			this.ApplyButton,
			this.RefreshButton});
			this.DevicesToolStrip.Location = new System.Drawing.Point(0, 0);
			this.DevicesToolStrip.Name = "DevicesToolStrip";
			this.DevicesToolStrip.Size = new System.Drawing.Size(700, 25);
			//
			// MoveUpButton
			//
			this.MoveUpButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.MoveUpButton.Image = global::x360ce.App.Properties.Resources.nav_up_16x16;
			this.MoveUpButton.Name = "MoveUpButton";
			this.MoveUpButton.Text = "Move Up";
			this.MoveUpButton.ToolTipText = "Ask for this controller to take an earlier XInput place.";
			this.MoveUpButton.Click += new System.EventHandler(this.MoveUpButton_Click);
			//
			// MoveDownButton
			//
			this.MoveDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			this.MoveDownButton.Image = global::x360ce.App.Properties.Resources.nav_down_16x16;
			this.MoveDownButton.Name = "MoveDownButton";
			this.MoveDownButton.Text = "Move Down";
			this.MoveDownButton.ToolTipText = "Ask for this controller to take a later XInput place.";
			this.MoveDownButton.Click += new System.EventHandler(this.MoveDownButton_Click);
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
			// StatusLabel
			// 
			this.StatusLabel.AutoSize = false;
			this.StatusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.StatusLabel.Size = new System.Drawing.Size(600, 19);
			this.StatusLabel.TabIndex = 2;
			this.StatusLabel.Text = "";
			this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StatusLabel.Visible = false;
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
			this.PlaceColumn,
			this.NameColumn});
			this.DevicesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DevicesDataGridView.Location = new System.Drawing.Point(0, 25);
			this.DevicesDataGridView.MultiSelect = false;
			this.DevicesDataGridView.Name = "DevicesDataGridView";
			this.DevicesDataGridView.ReadOnly = true;
			this.DevicesDataGridView.RowHeadersVisible = false;
			this.DevicesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DevicesDataGridView.Size = new System.Drawing.Size(700, 275);
			this.DevicesDataGridView.SelectionChanged += new System.EventHandler(this.DevicesDataGridView_SelectionChanged);
			//
			// PlaceColumn
			//
			this.PlaceColumn.FillWeight = 20F;
			this.PlaceColumn.HeaderText = "XInput";
			this.PlaceColumn.Name = "PlaceColumn";
			this.PlaceColumn.ReadOnly = true;
			this.PlaceColumn.ToolTipText = "Which of the four XInput places this controller holds. Blank when nothing can say.";
			//
			// NameColumn
			//
			this.NameColumn.FillWeight = 100F;
			this.NameColumn.HeaderText = "Controller";
			this.NameColumn.Name = "NameColumn";
			this.NameColumn.ReadOnly = true;
			//
			// XInputDevicesUserControl
			//
			this.Controls.Add(this.StatusLabel);
			this.Controls.Add(this.DevicesDataGridView);
			this.Controls.Add(this.DevicesToolStrip);
			this.Name = "XInputDevicesUserControl";
			this.Size = new System.Drawing.Size(700, 300);
			this.DevicesToolStrip.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.DevicesDataGridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ToolStrip DevicesToolStrip;
		private System.Windows.Forms.ToolStripButton MoveUpButton;
		private System.Windows.Forms.ToolStripButton MoveDownButton;
		private System.Windows.Forms.ToolStripButton ApplyButton;
		private System.Windows.Forms.ToolStripButton RefreshButton;
		private System.Windows.Forms.DataGridView DevicesDataGridView;
		private System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.DataGridViewTextBoxColumn PlaceColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn NameColumn;
	}
}
