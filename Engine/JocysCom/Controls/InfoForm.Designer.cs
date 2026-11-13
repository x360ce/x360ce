using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls
{
	partial class InfoForm : Form
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
			this.FormNameLabel = new System.Windows.Forms.Label();
			this.FormNameValueLabel = new System.Windows.Forms.Label();
			this.FormLocationValueLabel = new System.Windows.Forms.Label();
			this.FormLocationLabel = new System.Windows.Forms.Label();
			this.FormSizeValueLabel = new System.Windows.Forms.Label();
			this.FormSizeLabel = new System.Windows.Forms.Label();
			this.ControlsDataGridView = new System.Windows.Forms.DataGridView();
			this.colControlName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ItemPropertyGrid = new System.Windows.Forms.PropertyGrid();
			this.ControlsTabControl = new System.Windows.Forms.TabControl();
			this.ControlsTabPage = new System.Windows.Forms.TabPage();
			this.PropertiesTabControl = new System.Windows.Forms.TabControl();
			this.PropertiesTabPage = new System.Windows.Forms.TabPage();
			this.HelpLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.ControlsDataGridView)).BeginInit();
			this.ControlsTabControl.SuspendLayout();
			this.ControlsTabPage.SuspendLayout();
			this.PropertiesTabControl.SuspendLayout();
			this.PropertiesTabPage.SuspendLayout();
			this.MainLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// MainLayoutPanel
			//
			this.MainLayoutPanel.ColumnCount = 2;
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.MainLayoutPanel.Controls.Add(this.FormNameLabel, 0, 0);
			this.MainLayoutPanel.Controls.Add(this.FormNameValueLabel, 1, 0);
			this.MainLayoutPanel.Controls.Add(this.FormSizeLabel, 0, 1);
			this.MainLayoutPanel.Controls.Add(this.FormSizeValueLabel, 1, 1);
			this.MainLayoutPanel.Controls.Add(this.FormLocationLabel, 0, 2);
			this.MainLayoutPanel.Controls.Add(this.FormLocationValueLabel, 1, 2);
			this.MainLayoutPanel.Controls.Add(this.HelpLabel, 0, 3);
			this.MainLayoutPanel.Controls.Add(this.ControlsTabControl, 0, 4);
			this.MainLayoutPanel.Controls.Add(this.PropertiesTabControl, 0, 5);
			this.MainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MainLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.MainLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.MainLayoutPanel.Name = "MainLayoutPanel";
			this.MainLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
			this.MainLayoutPanel.RowCount = 6;
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65F));
			this.MainLayoutPanel.Size = new System.Drawing.Size(554, 547);
			this.MainLayoutPanel.TabIndex = 0;
			// 
			// FormNameLabel
			// 
			this.FormNameLabel.AutoSize = true;
			this.FormNameLabel.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);
			this.FormNameLabel.Name = "FormNameLabel";
			this.FormNameLabel.Size = new System.Drawing.Size(64, 13);
			this.FormNameLabel.TabIndex = 0;
			this.FormNameLabel.Text = "Form Name";
			// 
			// FormNameValueLabel
			// 
			this.FormNameValueLabel.AutoSize = true;
			this.FormNameValueLabel.Margin = new System.Windows.Forms.Padding(4);
			this.FormNameValueLabel.Name = "FormNameValueLabel";
			this.FormNameValueLabel.Size = new System.Drawing.Size(117, 13);
			this.FormNameValueLabel.TabIndex = 1;
			this.FormNameValueLabel.Text = "[FormNameValueLabel]";
			// 
			// FormLocationValueLabel
			// 
			this.FormLocationValueLabel.AutoSize = true;
			this.FormLocationValueLabel.Margin = new System.Windows.Forms.Padding(4);
			this.FormLocationValueLabel.Name = "FormLocationValueLabel";
			this.FormLocationValueLabel.Size = new System.Drawing.Size(130, 13);
			this.FormLocationValueLabel.TabIndex = 7;
			this.FormLocationValueLabel.Text = "[FormLocationValueLabel]";
			// 
			// FormLocationLabel
			// 
			this.FormLocationLabel.AutoSize = true;
			this.FormLocationLabel.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);
			this.FormLocationLabel.Name = "FormLocationLabel";
			this.FormLocationLabel.Size = new System.Drawing.Size(77, 13);
			this.FormLocationLabel.TabIndex = 6;
			this.FormLocationLabel.Text = "Form Location";
			// 
			// FormSizeValueLabel
			// 
			this.FormSizeValueLabel.AutoSize = true;
			this.FormSizeValueLabel.Margin = new System.Windows.Forms.Padding(4);
			this.FormSizeValueLabel.Name = "FormSizeValueLabel";
			this.FormSizeValueLabel.Size = new System.Drawing.Size(109, 13);
			this.FormSizeValueLabel.TabIndex = 5;
			this.FormSizeValueLabel.Text = "[FormSizeValueLabel]";
			// 
			// FormSizeLabel
			// 
			this.FormSizeLabel.AutoSize = true;
			this.FormSizeLabel.Margin = new System.Windows.Forms.Padding(4, 4, 8, 4);
			this.FormSizeLabel.Name = "FormSizeLabel";
			this.FormSizeLabel.Size = new System.Drawing.Size(56, 13);
			this.FormSizeLabel.TabIndex = 4;
			this.FormSizeLabel.Text = "Form Size";
			// 
			// ControlsDataGridView
			// 
			this.ControlsDataGridView.AllowUserToAddRows = false;
			this.ControlsDataGridView.AllowUserToDeleteRows = false;
			this.ControlsDataGridView.AllowUserToResizeRows = false;
			this.ControlsDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.ControlsDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.ControlsDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
			this.ControlsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colControlName,
            this.colType});
			this.ControlsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControlsDataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.ControlsDataGridView.GridColor = System.Drawing.SystemColors.ControlLight;
			this.ControlsDataGridView.Location = new System.Drawing.Point(0, 0);
			this.ControlsDataGridView.MultiSelect = false;
			this.ControlsDataGridView.Name = "ControlsDataGridView";
			this.ControlsDataGridView.RowHeadersVisible = false;
			this.ControlsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ControlsDataGridView.Size = new System.Drawing.Size(522, 128);
			this.ControlsDataGridView.TabIndex = 8;
			this.ControlsDataGridView.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ControlsDataGridView_CellContentClick);
			this.ControlsDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ControlsDataGridView_CellContentClick);
			this.ControlsDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InfoForm_KeyDown);
			// 
			// colControlName
			// 
			this.colControlName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.colControlName.DataPropertyName = "name";
			this.colControlName.HeaderText = "Name";
			this.colControlName.Name = "colControlName";
			this.colControlName.Width = 60;
			// 
			// colType
			// 
			this.colType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colType.DataPropertyName = "type";
			this.colType.HeaderText = "Type";
			this.colType.Name = "colType";
			// 
			// ItemPropertyGrid
			// 
			this.ItemPropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ItemPropertyGrid.Location = new System.Drawing.Point(0, 0);
			this.ItemPropertyGrid.Name = "ItemPropertyGrid";
			this.ItemPropertyGrid.Size = new System.Drawing.Size(516, 259);
			this.ItemPropertyGrid.TabIndex = 16;
			// 
			// ControlsTabControl
			// 
			this.ControlsTabControl.Controls.Add(this.ControlsTabPage);
			this.MainLayoutPanel.SetColumnSpan(this.ControlsTabControl, 2);
			this.ControlsTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ControlsTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.ControlsTabControl.Name = "ControlsTabControl";
			this.ControlsTabControl.SelectedIndex = 0;
			this.ControlsTabControl.Size = new System.Drawing.Size(530, 154);
			this.ControlsTabControl.TabIndex = 17;
			// 
			// ControlsTabPage
			// 
			this.ControlsTabPage.Controls.Add(this.ControlsDataGridView);
			this.ControlsTabPage.Location = new System.Drawing.Point(4, 22);
			this.ControlsTabPage.Name = "ControlsTabPage";
			this.ControlsTabPage.Size = new System.Drawing.Size(522, 128);
			this.ControlsTabPage.TabIndex = 0;
			this.ControlsTabPage.Text = "Controls";
			// 
			// PropertiesTabControl
			// 
			this.PropertiesTabControl.Controls.Add(this.PropertiesTabPage);
			this.MainLayoutPanel.SetColumnSpan(this.PropertiesTabControl, 2);
			this.PropertiesTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PropertiesTabControl.Margin = new System.Windows.Forms.Padding(4);
			this.PropertiesTabControl.Name = "PropertiesTabControl";
			this.PropertiesTabControl.SelectedIndex = 0;
			this.PropertiesTabControl.Size = new System.Drawing.Size(530, 291);
			this.PropertiesTabControl.TabIndex = 18;
			// 
			// PropertiesTabPage
			// 
			this.PropertiesTabPage.Controls.Add(this.ItemPropertyGrid);
			this.PropertiesTabPage.Location = new System.Drawing.Point(4, 22);
			this.PropertiesTabPage.Name = "PropertiesTabPage";
			this.PropertiesTabPage.Size = new System.Drawing.Size(522, 265);
			this.PropertiesTabPage.TabIndex = 0;
			this.PropertiesTabPage.Text = "Properties";
			// 
			// HelpLabel
			// 
			this.HelpLabel.AutoSize = true;
			this.MainLayoutPanel.SetColumnSpan(this.HelpLabel, 2);
			this.HelpLabel.Margin = new System.Windows.Forms.Padding(4);
			this.HelpLabel.Name = "HelpLabel";
			this.HelpLabel.Size = new System.Drawing.Size(255, 13);
			this.HelpLabel.TabIndex = 10;
			this.HelpLabel.Text = "Double click a cell to copy the content into clipboard";
			// 
			// InfoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(554, 547);
			this.Controls.Add(this.MainLayoutPanel);
			this.DoubleBuffered = true;
			this.MinimumSize = new System.Drawing.Size(570, 530);
			this.Name = "InfoForm";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InfoForm_KeyDown);
			((System.ComponentModel.ISupportInitialize)(this.ControlsDataGridView)).EndInit();
			this.ControlsTabControl.ResumeLayout(false);
			this.ControlsTabPage.ResumeLayout(false);
			this.PropertiesTabControl.ResumeLayout(false);
			this.PropertiesTabPage.ResumeLayout(false);
			this.MainLayoutPanel.ResumeLayout(false);
			this.MainLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		internal System.Windows.Forms.Label FormNameLabel;
		internal System.Windows.Forms.Label FormNameValueLabel;
		internal System.Windows.Forms.Label FormLocationValueLabel;
		internal System.Windows.Forms.Label FormLocationLabel;
		internal System.Windows.Forms.Label FormSizeValueLabel;
		internal System.Windows.Forms.Label FormSizeLabel;
		internal DataGridView ControlsDataGridView;
		internal System.Windows.Forms.PropertyGrid ItemPropertyGrid;
		internal System.Windows.Forms.TabControl ControlsTabControl;
		internal System.Windows.Forms.TabPage ControlsTabPage;
		internal System.Windows.Forms.TabControl PropertiesTabControl;
		internal System.Windows.Forms.TabPage PropertiesTabPage;
		internal System.Windows.Forms.DataGridViewTextBoxColumn colControlName;
		internal System.Windows.Forms.DataGridViewTextBoxColumn colType;
		internal System.Windows.Forms.Label HelpLabel;
		internal System.Windows.Forms.TableLayoutPanel MainLayoutPanel;

		#endregion

	}
}
