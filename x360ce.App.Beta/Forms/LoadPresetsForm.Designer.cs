namespace x360ce.App.Controls
{
	partial class LoadPresetsForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadPresetsForm));
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.SettingsTabPage = new System.Windows.Forms.TabPage();
			this.SettingsGridPanel = new x360ce.App.Controls.SettingsGridUserControl();
			this.SummariesTabPage = new System.Windows.Forms.TabPage();
			this.SummariesGridPanel = new x360ce.App.Controls.SummariesGridUserControl();
			this.PresetsTabPage = new System.Windows.Forms.TabPage();
			this.PresetsGridPanel = new x360ce.App.Controls.PresetsGridUserControl();
			this.CloseButton = new System.Windows.Forms.Button();
			this.OkButton = new System.Windows.Forms.Button();
			this.BusyLoadingCircle = new MRG.Controls.UI.LoadingCircle();
			this.MainTabControl.SuspendLayout();
			this.SettingsTabPage.SuspendLayout();
			this.SummariesTabPage.SuspendLayout();
			this.PresetsTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.SettingsTabPage);
			this.MainTabControl.Controls.Add(this.SummariesTabPage);
			this.MainTabControl.Controls.Add(this.PresetsTabPage);
			this.MainTabControl.Location = new System.Drawing.Point(12, 70);
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(600, 330);
			this.MainTabControl.TabIndex = 19;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_SelectedIndexChanged);
			// 
			// SettingsTabPage
			// 
			this.SettingsTabPage.Controls.Add(this.SettingsGridPanel);
			this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
			this.SettingsTabPage.Name = "SettingsTabPage";
			this.SettingsTabPage.Size = new System.Drawing.Size(592, 304);
			this.SettingsTabPage.TabIndex = 0;
			this.SettingsTabPage.Text = "My Settings";
			this.SettingsTabPage.UseVisualStyleBackColor = true;
			// 
			// SettingsGridPanel
			// 
			this.SettingsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SettingsGridPanel.Location = new System.Drawing.Point(0, 0);
			this.SettingsGridPanel.Name = "SettingsGridPanel";
			this.SettingsGridPanel.Size = new System.Drawing.Size(592, 304);
			this.SettingsGridPanel.TabIndex = 0;
			// 
			// SummariesTabPage
			// 
			this.SummariesTabPage.Controls.Add(this.SummariesGridPanel);
			this.SummariesTabPage.Location = new System.Drawing.Point(4, 22);
			this.SummariesTabPage.Name = "SummariesTabPage";
			this.SummariesTabPage.Size = new System.Drawing.Size(592, 304);
			this.SummariesTabPage.TabIndex = 1;
			this.SummariesTabPage.Text = "Default Settings for My Controllers";
			this.SummariesTabPage.UseVisualStyleBackColor = true;
			// 
			// SummariesGridPanel
			// 
			this.SummariesGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SummariesGridPanel.Location = new System.Drawing.Point(0, 0);
			this.SummariesGridPanel.Name = "SummariesGridPanel";
			this.SummariesGridPanel.Size = new System.Drawing.Size(592, 304);
			this.SummariesGridPanel.TabIndex = 0;
			// 
			// PresetsTabPage
			// 
			this.PresetsTabPage.Controls.Add(this.PresetsGridPanel);
			this.PresetsTabPage.Location = new System.Drawing.Point(4, 22);
			this.PresetsTabPage.Name = "PresetsTabPage";
			this.PresetsTabPage.Size = new System.Drawing.Size(592, 304);
			this.PresetsTabPage.TabIndex = 2;
			this.PresetsTabPage.Text = "Default Settings for Most Popular Controllers";
			this.PresetsTabPage.UseVisualStyleBackColor = true;
			// 
			// PresetsGridPanel
			// 
			this.PresetsGridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.PresetsGridPanel.Location = new System.Drawing.Point(0, 0);
			this.PresetsGridPanel.Name = "PresetsGridPanel";
			this.PresetsGridPanel.Size = new System.Drawing.Size(592, 304);
			this.PresetsGridPanel.TabIndex = 0;
			// 
			// CloseButton
			// 
			this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CloseButton.Location = new System.Drawing.Point(537, 406);
			this.CloseButton.Name = "CloseButton";
			this.CloseButton.Size = new System.Drawing.Size(75, 23);
			this.CloseButton.TabIndex = 20;
			this.CloseButton.Text = "Cancel";
			this.CloseButton.UseVisualStyleBackColor = true;
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.Location = new System.Drawing.Point(372, 406);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(159, 23);
			this.OkButton.TabIndex = 20;
			this.OkButton.Text = "Load Selected Preset";
			this.OkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// BusyLoadingCircle
			// 
			this.BusyLoadingCircle.Active = false;
			this.BusyLoadingCircle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BusyLoadingCircle.Color = System.Drawing.Color.SteelBlue;
			this.BusyLoadingCircle.InnerCircleRadius = 8;
			this.BusyLoadingCircle.Location = new System.Drawing.Point(597, 9);
			this.BusyLoadingCircle.Name = "BusyLoadingCircle";
			this.BusyLoadingCircle.NumberSpoke = 24;
			this.BusyLoadingCircle.OuterCircleRadius = 9;
			this.BusyLoadingCircle.RotationSpeed = 30;
			this.BusyLoadingCircle.Size = new System.Drawing.Size(48, 48);
			this.BusyLoadingCircle.SpokeThickness = 4;
			this.BusyLoadingCircle.StylePreset = MRG.Controls.UI.LoadingCircle.StylePresets.IE7;
			this.BusyLoadingCircle.TabIndex = 9;
			// 
			// LoadPresetsForm
			// 
			this.ClientSize = new System.Drawing.Size(624, 441);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.CloseButton);
			this.Controls.Add(this.MainTabControl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(640, 480);
			this.Name = "LoadPresetsForm";
			this.Text = "X360CE - Load Preset";
			this.Controls.SetChildIndex(this.MainTabControl, 0);
			this.Controls.SetChildIndex(this.CloseButton, 0);
			this.Controls.SetChildIndex(this.OkButton, 0);
			this.MainTabControl.ResumeLayout(false);
			this.SettingsTabPage.ResumeLayout(false);
			this.SummariesTabPage.ResumeLayout(false);
			this.PresetsTabPage.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage SettingsTabPage;
		private System.Windows.Forms.TabPage SummariesTabPage;
		private System.Windows.Forms.TabPage PresetsTabPage;
		private System.Windows.Forms.Button CloseButton;
		private System.Windows.Forms.Button OkButton;
		private MRG.Controls.UI.LoadingCircle BusyLoadingCircle;
		private SettingsGridUserControl SettingsGridPanel;
		private SummariesGridUserControl SummariesGridPanel;
		private PresetsGridUserControl PresetsGridPanel;
	}
}
