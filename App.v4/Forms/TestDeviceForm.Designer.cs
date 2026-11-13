namespace x360ce.App.Forms
{
	partial class TestDeviceForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.RootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.StepTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.InstructionLabel = new System.Windows.Forms.Label();
			this.CountdownCaptionLabel = new System.Windows.Forms.Label();
			this.CountdownLabel = new System.Windows.Forms.Label();
			this.HintLabel = new System.Windows.Forms.Label();
			this.ContentPanel = new System.Windows.Forms.Panel();
			this.ContentTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.ControlsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.CardsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.ButtonsHeadingLabel = new System.Windows.Forms.Label();
			this.NotGradedLabel = new System.Windows.Forms.Label();
			this.ActionsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.SkipButton = new System.Windows.Forms.Button();
			this.DetailsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.SummaryLabel = new System.Windows.Forms.Label();
			this.ResultsDataGridView = new System.Windows.Forms.DataGridView();
			this.LimitsLabel = new System.Windows.Forms.Label();
			this.ButtonsFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.CloseButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.RootTableLayoutPanel.SuspendLayout();
			this.StepTableLayoutPanel.SuspendLayout();
			this.ActionsFlowLayoutPanel.SuspendLayout();
			this.ContentPanel.SuspendLayout();
			this.ContentTableLayoutPanel.SuspendLayout();
			this.DetailsTableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ResultsDataGridView)).BeginInit();
			this.ButtonsFlowLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// RootTableLayoutPanel
			//
			this.RootTableLayoutPanel.ColumnCount = 1;
			this.RootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.Controls.Add(this.StepTableLayoutPanel, 0, 0);
			this.RootTableLayoutPanel.Controls.Add(this.HintLabel, 0, 1);
			this.RootTableLayoutPanel.Controls.Add(this.ContentPanel, 0, 2);
			this.RootTableLayoutPanel.Controls.Add(this.ButtonsFlowLayoutPanel, 0, 3);
			this.RootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.RootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.RootTableLayoutPanel.Name = "RootTableLayoutPanel";
			this.RootTableLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
			this.RootTableLayoutPanel.RowCount = 4;
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.RootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.RootTableLayoutPanel.TabIndex = 0;
			//
			// StepTableLayoutPanel
			//
			this.StepTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.StepTableLayoutPanel.AutoSize = true;
			this.StepTableLayoutPanel.ColumnCount = 2;
			this.StepTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.StepTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.StepTableLayoutPanel.Controls.Add(this.InstructionLabel, 0, 0);
			this.StepTableLayoutPanel.Controls.Add(this.CountdownCaptionLabel, 1, 0);
			this.StepTableLayoutPanel.Controls.Add(this.ActionsFlowLayoutPanel, 0, 1);
			this.StepTableLayoutPanel.Controls.Add(this.CountdownLabel, 1, 1);
			this.StepTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.StepTableLayoutPanel.Name = "StepTableLayoutPanel";
			this.StepTableLayoutPanel.RowCount = 2;
			this.StepTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.StepTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.StepTableLayoutPanel.TabIndex = 0;
			//
			// InstructionLabel
			//
			this.InstructionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.InstructionLabel.AutoSize = true;
			this.InstructionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.InstructionLabel.Margin = new System.Windows.Forms.Padding(4);
			this.InstructionLabel.Name = "InstructionLabel";
			this.InstructionLabel.TabIndex = 0;
			this.InstructionLabel.Text = "Press Start to test the controller.";
			//
			// CountdownCaptionLabel
			//
			this.CountdownCaptionLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.CountdownCaptionLabel.AutoSize = true;
			this.CountdownCaptionLabel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 0);
			this.CountdownCaptionLabel.Name = "CountdownCaptionLabel";
			this.CountdownCaptionLabel.TabIndex = 1;
			//
			// CountdownLabel
			//
			this.CountdownLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.CountdownLabel.AutoSize = true;
			this.CountdownLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CountdownLabel.Margin = new System.Windows.Forms.Padding(4);
			this.CountdownLabel.Name = "CountdownLabel";
			this.CountdownLabel.TabIndex = 2;
			//
			// HintLabel
			//
			this.HintLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.HintLabel.AutoSize = true;
			this.HintLabel.Margin = new System.Windows.Forms.Padding(4);
			this.HintLabel.Name = "HintLabel";
			this.HintLabel.TabIndex = 1;
			//
			// ContentPanel
			//
			this.ContentPanel.AutoScroll = true;
			this.ContentPanel.Controls.Add(this.ContentTableLayoutPanel);
			this.ContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ContentPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ContentPanel.Name = "ContentPanel";
			this.ContentPanel.TabIndex = 2;
			//
			// ContentTableLayoutPanel
			//
			this.ContentTableLayoutPanel.AutoSize = true;
			this.ContentTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ContentTableLayoutPanel.ColumnCount = 1;
			this.ContentTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.ContentTableLayoutPanel.Controls.Add(this.ControlsFlowLayoutPanel, 0, 1);
			this.ContentTableLayoutPanel.Controls.Add(this.CardsFlowLayoutPanel, 0, 2);
			this.ContentTableLayoutPanel.Controls.Add(this.NotGradedLabel, 0, 3);
			this.ContentTableLayoutPanel.Controls.Add(this.ButtonsHeadingLabel, 0, 4);
			this.ContentTableLayoutPanel.Controls.Add(this.DetailsTableLayoutPanel, 0, 6);
			this.ContentTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.ContentTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ContentTableLayoutPanel.Name = "ContentTableLayoutPanel";
			this.ContentTableLayoutPanel.RowCount = 7;
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.ContentTableLayoutPanel.TabIndex = 0;
			//
			// ControlsFlowLayoutPanel
			//
			this.ControlsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ControlsFlowLayoutPanel.AutoSize = true;
			this.ControlsFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ControlsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ControlsFlowLayoutPanel.Name = "ControlsFlowLayoutPanel";
			this.ControlsFlowLayoutPanel.TabIndex = 1;
			this.ControlsFlowLayoutPanel.Visible = false;
			//
			// CardsFlowLayoutPanel
			//
			this.CardsFlowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.CardsFlowLayoutPanel.AutoSize = true;
			this.CardsFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CardsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.CardsFlowLayoutPanel.Name = "CardsFlowLayoutPanel";
			this.CardsFlowLayoutPanel.TabIndex = 2;
			this.CardsFlowLayoutPanel.Visible = false;
			//
			// ButtonsHeadingLabel
			//
			this.ButtonsHeadingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.ButtonsHeadingLabel.AutoSize = true;
			this.ButtonsHeadingLabel.Margin = new System.Windows.Forms.Padding(4);
			this.ButtonsHeadingLabel.Name = "ButtonsHeadingLabel";
			this.ButtonsHeadingLabel.TabIndex = 4;
			this.ButtonsHeadingLabel.Text = "Buttons, shown and not graded.";
			this.ButtonsHeadingLabel.Visible = false;
			//
			// NotGradedLabel
			//
			this.NotGradedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.NotGradedLabel.AutoSize = true;
			this.NotGradedLabel.Margin = new System.Windows.Forms.Padding(4);
			this.NotGradedLabel.Name = "NotGradedLabel";
			this.NotGradedLabel.TabIndex = 3;
			this.NotGradedLabel.Visible = false;
			//
			// ActionsFlowLayoutPanel
			//
			this.ActionsFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.ActionsFlowLayoutPanel.AutoSize = true;
			this.ActionsFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ActionsFlowLayoutPanel.Controls.Add(this.StartButton);
			this.ActionsFlowLayoutPanel.Controls.Add(this.SkipButton);
			this.ActionsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ActionsFlowLayoutPanel.Name = "ActionsFlowLayoutPanel";
			this.ActionsFlowLayoutPanel.TabIndex = 3;
			this.ActionsFlowLayoutPanel.WrapContents = false;
			//
			// SkipButton
			//
			this.SkipButton.AutoSize = true;
			this.SkipButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SkipButton.Margin = new System.Windows.Forms.Padding(4);
			this.SkipButton.Name = "SkipButton";
			this.SkipButton.Padding = new System.Windows.Forms.Padding(32, 0, 32, 0);
			this.SkipButton.TabIndex = 1;
			this.SkipButton.Text = "S&kip";
			this.SkipButton.UseVisualStyleBackColor = true;
			this.SkipButton.Visible = false;
			this.SkipButton.Click += new System.EventHandler(this.SkipButton_Click);
			//
			// DetailsTableLayoutPanel
			//
			this.DetailsTableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.DetailsTableLayoutPanel.AutoSize = true;
			this.DetailsTableLayoutPanel.ColumnCount = 1;
			this.DetailsTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.DetailsTableLayoutPanel.Controls.Add(this.SummaryLabel, 0, 0);
			this.DetailsTableLayoutPanel.Controls.Add(this.ResultsDataGridView, 0, 1);
			this.DetailsTableLayoutPanel.Controls.Add(this.LimitsLabel, 0, 2);
			this.DetailsTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.DetailsTableLayoutPanel.Name = "DetailsTableLayoutPanel";
			this.DetailsTableLayoutPanel.RowCount = 3;
			this.DetailsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.DetailsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.DetailsTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.DetailsTableLayoutPanel.TabIndex = 6;
			this.DetailsTableLayoutPanel.Visible = false;
			//
			// SummaryLabel
			//
			this.SummaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.SummaryLabel.AutoSize = true;
			this.SummaryLabel.Margin = new System.Windows.Forms.Padding(4);
			this.SummaryLabel.Name = "SummaryLabel";
			this.SummaryLabel.TabIndex = 0;
			//
			// ResultsDataGridView
			//
			this.ResultsDataGridView.AllowUserToAddRows = false;
			this.ResultsDataGridView.AllowUserToDeleteRows = false;
			this.ResultsDataGridView.AllowUserToResizeRows = false;
			this.ResultsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
			this.ResultsDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.ResultsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ResultsDataGridView.Margin = new System.Windows.Forms.Padding(4);
			this.ResultsDataGridView.Name = "ResultsDataGridView";
			this.ResultsDataGridView.ReadOnly = true;
			this.ResultsDataGridView.RowHeadersVisible = false;
			this.ResultsDataGridView.ScrollBars = System.Windows.Forms.ScrollBars.None;
			this.ResultsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ResultsDataGridView.TabIndex = 1;
			//
			// LimitsLabel
			//
			this.LimitsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.LimitsLabel.AutoSize = true;
			this.LimitsLabel.Margin = new System.Windows.Forms.Padding(4);
			this.LimitsLabel.Name = "LimitsLabel";
			this.LimitsLabel.TabIndex = 2;
			this.LimitsLabel.Text = "Distances in mm and forces in grams need measuring equipment.";
			//
			// ButtonsFlowLayoutPanel
			//
			this.ButtonsFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.ButtonsFlowLayoutPanel.AutoSize = true;
			this.ButtonsFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ButtonsFlowLayoutPanel.Controls.Add(this.CloseButton);
			this.ButtonsFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.ButtonsFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonsFlowLayoutPanel.Name = "ButtonsFlowLayoutPanel";
			this.ButtonsFlowLayoutPanel.TabIndex = 3;
			this.ButtonsFlowLayoutPanel.WrapContents = false;
			//
			// CloseButton
			//
			this.CloseButton.AutoSize = true;
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Margin = new System.Windows.Forms.Padding(4);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.TabIndex = 1;
			this.CloseButton.Text = "Close";
			this.CloseButton.UseVisualStyleBackColor = true;
			//
			// StartButton
			//
			this.StartButton.AutoSize = true;
			this.StartButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StartButton.Margin = new System.Windows.Forms.Padding(4);
			this.StartButton.Padding = new System.Windows.Forms.Padding(32, 0, 32, 0);
			this.StartButton.Name = "StartButton";
			this.StartButton.TabIndex = 0;
			this.StartButton.Text = "&Start";
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			//
			// UpdateTimer
			//
			this.UpdateTimer.Interval = 100;
			this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
			//
			// TestDeviceForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CloseButton;
			this.ClientSize = new System.Drawing.Size(1000, 680);
			this.Controls.Add(this.RootTableLayoutPanel);
			this.Icon = global::x360ce.App.Properties.Resources.app;
			this.Name = "TestDeviceForm";
			this.Text = "Test Device";
			this.Controls.SetChildIndex(this.RootTableLayoutPanel, 0);
			this.RootTableLayoutPanel.ResumeLayout(false);
			this.RootTableLayoutPanel.PerformLayout();
			this.StepTableLayoutPanel.ResumeLayout(false);
			this.StepTableLayoutPanel.PerformLayout();
			this.ActionsFlowLayoutPanel.ResumeLayout(false);
			this.ActionsFlowLayoutPanel.PerformLayout();
			this.ContentPanel.ResumeLayout(false);
			this.ContentPanel.PerformLayout();
			this.ContentTableLayoutPanel.ResumeLayout(false);
			this.ContentTableLayoutPanel.PerformLayout();
			this.DetailsTableLayoutPanel.ResumeLayout(false);
			this.DetailsTableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ResultsDataGridView)).EndInit();
			this.ButtonsFlowLayoutPanel.ResumeLayout(false);
			this.ButtonsFlowLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel RootTableLayoutPanel;
		private System.Windows.Forms.TableLayoutPanel StepTableLayoutPanel;
		internal System.Windows.Forms.Label InstructionLabel;
		internal System.Windows.Forms.Label CountdownCaptionLabel;
		internal System.Windows.Forms.Label CountdownLabel;
		internal System.Windows.Forms.Label HintLabel;
		private System.Windows.Forms.Panel ContentPanel;
		private System.Windows.Forms.TableLayoutPanel ContentTableLayoutPanel;
		internal System.Windows.Forms.FlowLayoutPanel ControlsFlowLayoutPanel;
		internal System.Windows.Forms.FlowLayoutPanel CardsFlowLayoutPanel;
		internal System.Windows.Forms.Label ButtonsHeadingLabel;
		internal System.Windows.Forms.Label NotGradedLabel;
		private System.Windows.Forms.FlowLayoutPanel ActionsFlowLayoutPanel;
		internal System.Windows.Forms.Button SkipButton;
		private System.Windows.Forms.TableLayoutPanel DetailsTableLayoutPanel;
		internal System.Windows.Forms.Label SummaryLabel;
		internal System.Windows.Forms.DataGridView ResultsDataGridView;
		internal System.Windows.Forms.Label LimitsLabel;
		private System.Windows.Forms.FlowLayoutPanel ButtonsFlowLayoutPanel;
		internal System.Windows.Forms.Button CloseButton;
		internal System.Windows.Forms.Button StartButton;
		private System.Windows.Forms.Timer UpdateTimer;
	}
}
