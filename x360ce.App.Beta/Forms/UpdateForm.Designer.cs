namespace x360ce.App.Forms
{
    partial class UpdateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            this.CloseButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.CheckButton = new System.Windows.Forms.Button();
            this.CheckDigitalSignatureCheckBox = new System.Windows.Forms.CheckBox();
            this.CheckVersionCheckBox = new System.Windows.Forms.CheckBox();
            this.LogPanel = new x360ce.App.Controls.LogUserControl();
            this.MainTabControl = new System.Windows.Forms.TabControl();
            this.LogTabPage = new System.Windows.Forms.TabPage();
            this.MainTabControl.SuspendLayout();
            this.LogTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(537, 226);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 4;
            this.CloseButton.Text = "Cancel";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(456, 226);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 3;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // CheckButton
            // 
            this.CheckButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CheckButton.Location = new System.Drawing.Point(12, 226);
            this.CheckButton.Name = "CheckButton";
            this.CheckButton.Size = new System.Drawing.Size(75, 23);
            this.CheckButton.TabIndex = 3;
            this.CheckButton.Text = "Check...";
            this.CheckButton.UseVisualStyleBackColor = true;
            this.CheckButton.Click += new System.EventHandler(this.CheckButton_Click);
            // 
            // CheckDigitalSignatureCheckBox
            // 
            this.CheckDigitalSignatureCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.CheckDigitalSignatureCheckBox.AutoSize = true;
            this.CheckDigitalSignatureCheckBox.Checked = true;
            this.CheckDigitalSignatureCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckDigitalSignatureCheckBox.Location = new System.Drawing.Point(93, 230);
            this.CheckDigitalSignatureCheckBox.Name = "CheckDigitalSignatureCheckBox";
            this.CheckDigitalSignatureCheckBox.Size = new System.Drawing.Size(137, 17);
            this.CheckDigitalSignatureCheckBox.TabIndex = 6;
            this.CheckDigitalSignatureCheckBox.Text = "Check Digital Signature";
            this.CheckDigitalSignatureCheckBox.UseVisualStyleBackColor = true;
            // 
            // CheckVersionCheckBox
            // 
            this.CheckVersionCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CheckVersionCheckBox.AutoSize = true;
            this.CheckVersionCheckBox.Checked = true;
            this.CheckVersionCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckVersionCheckBox.Location = new System.Drawing.Point(236, 230);
            this.CheckVersionCheckBox.Name = "CheckVersionCheckBox";
            this.CheckVersionCheckBox.Size = new System.Drawing.Size(95, 17);
            this.CheckVersionCheckBox.TabIndex = 6;
            this.CheckVersionCheckBox.Text = "Check Version";
            this.CheckVersionCheckBox.UseVisualStyleBackColor = true;
            // 
            // LogPanel
            // 
            this.LogPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogPanel.Location = new System.Drawing.Point(0, 0);
            this.LogPanel.Name = "LogPanel";
            this.LogPanel.ShowLogSize = false;
            this.LogPanel.Size = new System.Drawing.Size(592, 182);
            this.LogPanel.TabIndex = 5;
            // 
            // MainTabControl
            // 
            this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainTabControl.Controls.Add(this.LogTabPage);
            this.MainTabControl.Location = new System.Drawing.Point(12, 12);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(600, 208);
            this.MainTabControl.TabIndex = 7;
            // 
            // LogTabPage
            // 
            this.LogTabPage.Controls.Add(this.LogPanel);
            this.LogTabPage.Location = new System.Drawing.Point(4, 22);
            this.LogTabPage.Name = "LogTabPage";
            this.LogTabPage.Size = new System.Drawing.Size(592, 182);
            this.LogTabPage.TabIndex = 0;
            this.LogTabPage.Text = "Log";
            this.LogTabPage.UseVisualStyleBackColor = true;
            // 
            // UpdateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 261);
            this.Controls.Add(this.MainTabControl);
            this.Controls.Add(this.CheckVersionCheckBox);
            this.Controls.Add(this.CheckDigitalSignatureCheckBox);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.CheckButton);
            this.Controls.Add(this.OkButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UpdateForm";
            this.Text = "X360CE - Update";
            this.Load += new System.EventHandler(this.UpdateForm_Load);
            this.MainTabControl.ResumeLayout(false);
            this.LogTabPage.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button CheckButton;
        private Controls.LogUserControl LogPanel;
        private System.Windows.Forms.CheckBox CheckDigitalSignatureCheckBox;
        private System.Windows.Forms.CheckBox CheckVersionCheckBox;
        private System.Windows.Forms.TabControl MainTabControl;
        private System.Windows.Forms.TabPage LogTabPage;
    }
}
