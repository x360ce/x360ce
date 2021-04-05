namespace x360ce.App.Controls
{
	partial class PadUserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.IContainer components = null;

		#region ■ Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.ResetPresetButton = new System.Windows.Forms.Button();
			this.MainToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.PastePresetButton = new System.Windows.Forms.Button();
			this.CopyPresetButton = new System.Windows.Forms.Button();
			this.ClearPresetButton = new System.Windows.Forms.Button();
			this.GameControllersButton = new System.Windows.Forms.Button();
			this.AutoPresetButton = new System.Windows.Forms.Button();
			this.LoadPresetButton = new System.Windows.Forms.Button();
			this.DxTweakButton = new System.Windows.Forms.Button();
			this.PadHost = new System.Windows.Forms.Integration.ElementHost();
			this.SuspendLayout();
			// 
			// ResetPresetButton
			// 
			this.ResetPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ResetPresetButton.Location = new System.Drawing.Point(1376, 1123);
			this.ResetPresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.ResetPresetButton.Name = "ResetPresetButton";
			this.ResetPresetButton.Size = new System.Drawing.Size(150, 44);
			this.ResetPresetButton.TabIndex = 66;
			this.ResetPresetButton.Text = "&Reset";
			this.ResetPresetButton.UseVisualStyleBackColor = true;
			this.ResetPresetButton.Click += new System.EventHandler(this.ResetPresetButton_Click);
			// 
			// PastePresetButton
			// 
			this.PastePresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.PastePresetButton.Image = global::x360ce.App.Properties.Resources.paste_16x16;
			this.PastePresetButton.Location = new System.Drawing.Point(632, 1123);
			this.PastePresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.PastePresetButton.Name = "PastePresetButton";
			this.PastePresetButton.Size = new System.Drawing.Size(200, 44);
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
			this.CopyPresetButton.Location = new System.Drawing.Point(420, 1123);
			this.CopyPresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.CopyPresetButton.Name = "CopyPresetButton";
			this.CopyPresetButton.Size = new System.Drawing.Size(200, 44);
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
			this.ClearPresetButton.Location = new System.Drawing.Point(1214, 1123);
			this.ClearPresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.ClearPresetButton.Name = "ClearPresetButton";
			this.ClearPresetButton.Size = new System.Drawing.Size(150, 44);
			this.ClearPresetButton.TabIndex = 66;
			this.ClearPresetButton.Text = "&Clear";
			this.ClearPresetButton.UseVisualStyleBackColor = true;
			this.ClearPresetButton.Click += new System.EventHandler(this.ClearPresetButton_Click);
			// 
			// GameControllersButton
			// 
			this.GameControllersButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.GameControllersButton.Location = new System.Drawing.Point(6, 1123);
			this.GameControllersButton.Margin = new System.Windows.Forms.Padding(6);
			this.GameControllersButton.Name = "GameControllersButton";
			this.GameControllersButton.Size = new System.Drawing.Size(212, 44);
			this.GameControllersButton.TabIndex = 66;
			this.GameControllersButton.Text = "&Game Controllers...";
			this.GameControllersButton.UseVisualStyleBackColor = true;
			this.GameControllersButton.Click += new System.EventHandler(this.GameControllersButton_Click);
			// 
			// AutoPresetButton
			// 
			this.AutoPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoPresetButton.Location = new System.Drawing.Point(1056, 1123);
			this.AutoPresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.AutoPresetButton.Name = "AutoPresetButton";
			this.AutoPresetButton.Size = new System.Drawing.Size(150, 44);
			this.AutoPresetButton.TabIndex = 66;
			this.AutoPresetButton.Text = "&Auto";
			this.AutoPresetButton.UseVisualStyleBackColor = true;
			this.AutoPresetButton.Click += new System.EventHandler(this.AutoPresetButton_Click);
			// 
			// LoadPresetButton
			// 
			this.LoadPresetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.LoadPresetButton.Location = new System.Drawing.Point(844, 1123);
			this.LoadPresetButton.Margin = new System.Windows.Forms.Padding(6);
			this.LoadPresetButton.Name = "LoadPresetButton";
			this.LoadPresetButton.Size = new System.Drawing.Size(200, 44);
			this.LoadPresetButton.TabIndex = 66;
			this.LoadPresetButton.Text = "&Load Preset...";
			this.LoadPresetButton.UseVisualStyleBackColor = true;
			this.LoadPresetButton.Click += new System.EventHandler(this.LoadPresetButton_Click);
			// 
			// DxTweakButton
			// 
			this.DxTweakButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.DxTweakButton.Location = new System.Drawing.Point(230, 1123);
			this.DxTweakButton.Margin = new System.Windows.Forms.Padding(6);
			this.DxTweakButton.Name = "DxTweakButton";
			this.DxTweakButton.Size = new System.Drawing.Size(158, 44);
			this.DxTweakButton.TabIndex = 70;
			this.DxTweakButton.Text = "&DX Tweak...";
			this.DxTweakButton.UseVisualStyleBackColor = true;
			this.DxTweakButton.Click += new System.EventHandler(this.CalibrateButton_Click);
			// 
			// PadHost
			// 
			this.PadHost.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PadHost.Location = new System.Drawing.Point(6, 6);
			this.PadHost.Margin = new System.Windows.Forms.Padding(6);
			this.PadHost.Name = "PadHost";
			this.PadHost.Size = new System.Drawing.Size(1520, 1105);
			this.PadHost.TabIndex = 73;
			this.PadHost.Child = null;
			// 
			// PadUserControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PadHost);
			this.Controls.Add(this.PastePresetButton);
			this.Controls.Add(this.CopyPresetButton);
			this.Controls.Add(this.DxTweakButton);
			this.Controls.Add(this.LoadPresetButton);
			this.Controls.Add(this.GameControllersButton);
			this.Controls.Add(this.AutoPresetButton);
			this.Controls.Add(this.ClearPresetButton);
			this.Controls.Add(this.ResetPresetButton);
			this.Margin = new System.Windows.Forms.Padding(6);
			this.Name = "PadUserControl";
			this.Size = new System.Drawing.Size(1532, 1173);
			this.ResumeLayout(false);

		}

		#endregion
		System.Windows.Forms.ToolTip MainToolTip;
        System.Windows.Forms.Button ResetPresetButton;
        private System.Windows.Forms.Button ClearPresetButton;
		private System.Windows.Forms.Button GameControllersButton;
		private System.Windows.Forms.Button AutoPresetButton;
		private System.Windows.Forms.Button LoadPresetButton;
		private System.Windows.Forms.Button DxTweakButton;
		private System.Windows.Forms.Button CopyPresetButton;
		private System.Windows.Forms.Button PastePresetButton;
		private System.Windows.Forms.Integration.ElementHost PadHost;
	}
}
