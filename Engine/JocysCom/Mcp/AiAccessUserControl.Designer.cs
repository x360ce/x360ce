#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Mcp
{
	partial class AiAccessUserControl
	{
		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.AiPanel = new Panel();
			this.AiHeaderPanel = new TableLayoutPanel();
			this.AiHeaderText = new Label();
			this.AiExampleLabel = new Label();
			this.AiExampleTextBox = new TextBox();
			this.AiExampleCopyButton = new Button();
			this.AiAccessPanel = new TableLayoutPanel();
			this.AiAccessTitleLabel = new Label();
			this.AiAccessEnabledCheckBox = new CheckBox();
			this.AiAccessLevelLabel = new Label();
			this.AiAccessComboBox = new ComboBox();
			this.AiAccessAddressLabel = new Label();
			this.AiAccessAddressComboBox = new ComboBox();
			this.AiAccessTrustLocalCheckBox = new CheckBox();
			this.AiAccessPortLabel = new Label();
			this.AiAccessPortTextBox = new TextBox();
			this.AiAccessUrlLabel = new Label();
			this.AiAccessUrlTextBox = new TextBox();
			this.AiAccessUrlCopyButton = new Button();
			this.AiAccessTokenLabel = new Label();
			this.AiAccessTokenTextBox = new TextBox();
			this.AiAccessRegenerateButton = new Button();
			this.AiAccessSnippetLabel = new Label();
			this.AiAccessSnippetTextBox = new TextBox();
			this.AiAccessCopyButton = new Button();
			this.AiAccessPromptButton = new Button();
			this.AiAccessLogButton = new Button();
			this.AiAccessStatusText = new Label();
			this.AiSkillPanel = new TableLayoutPanel();
			this.AiSkillTitleLabel = new Label();
			this.AiSkillNoteText = new Label();
			this.AiSkillClaudeLabel = new Label();
			this.AiSkillClaudeFolderTextBox = new TextBox();
			this.AiSkillClaudeButton = new Button();
			this.AiSkillClaudeStatusText = new Label();
			this.AiSkillAgentsLabel = new Label();
			this.AiSkillAgentsFolderTextBox = new TextBox();
			this.AiSkillAgentsButton = new Button();
			this.AiSkillAgentsStatusText = new Label();
			this.AiSkillZipLabel = new Label();
			this.AiSkillZipNoteText = new Label();
			this.AiSkillZipButton = new Button();
			this.AiPanel.SuspendLayout();
			this.AiHeaderPanel.SuspendLayout();
			this.AiAccessPanel.SuspendLayout();
			this.AiSkillPanel.SuspendLayout();
			this.SuspendLayout();
			// AiPanel
			this.AiPanel.AutoScroll = true;
			this.AiPanel.Controls.Add(this.AiSkillPanel);
			this.AiPanel.Controls.Add(this.AiAccessPanel);
			this.AiPanel.Controls.Add(this.AiHeaderPanel);
			this.AiPanel.Dock = DockStyle.Fill;
			this.AiPanel.Name = "AiPanel";
			this.AiPanel.Padding = new Padding(3, 3, 9, 3);
			this.AiPanel.TabIndex = 0;
			// AiHeaderPanel
			this.AiHeaderPanel.AutoSize = true;
			this.AiHeaderPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiHeaderPanel.ColumnCount = 3;
			this.AiHeaderPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiHeaderPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiHeaderPanel.Controls.Add(this.AiHeaderText, 0, 0);
			this.AiHeaderPanel.Controls.Add(this.AiExampleLabel, 0, 1);
			this.AiHeaderPanel.Controls.Add(this.AiExampleTextBox, 1, 1);
			this.AiHeaderPanel.Controls.Add(this.AiExampleCopyButton, 2, 1);
			this.AiHeaderPanel.Dock = DockStyle.Top;
			this.AiHeaderPanel.Name = "AiHeaderPanel";
			this.AiHeaderPanel.Padding = new Padding(0, 0, 0, 12);
			this.AiHeaderPanel.RowCount = 2;
			this.AiHeaderPanel.TabIndex = 0;
			// AiHeaderText
			this.AiHeaderText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiHeaderText.AutoSize = true;
			this.AiHeaderPanel.SetColumnSpan(this.AiHeaderText, 3);
			this.AiHeaderText.Margin = new Padding(3, 0, 0, 6);
			this.AiHeaderText.Name = "AiHeaderText";
			this.AiHeaderText.TabIndex = 0;
			// AiExampleLabel
			this.AiExampleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			this.AiExampleLabel.AutoSize = true;
			this.AiExampleLabel.Margin = new Padding(3, 6, 8, 0);
			this.AiExampleLabel.Name = "AiExampleLabel";
			this.AiExampleLabel.TabIndex = 1;
			this.AiExampleLabel.Text = "Example:";
			// AiExampleTextBox
			this.AiExampleTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiExampleTextBox.Multiline = true;
			this.AiExampleTextBox.Name = "AiExampleTextBox";
			this.AiExampleTextBox.ReadOnly = true;
			this.AiExampleTextBox.TabIndex = 2;
			// AiExampleCopyButton
			this.AiExampleCopyButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiExampleCopyButton.Margin = new Padding(8, 3, 0, 3);
			this.AiExampleCopyButton.MinimumSize = new Size(90, 23);
			this.AiExampleCopyButton.Name = "AiExampleCopyButton";
			this.AiExampleCopyButton.TabIndex = 3;
			this.AiExampleCopyButton.Text = "Copy";
			this.AiExampleCopyButton.Click += new EventHandler(this.AiExampleCopyButton_Click);
			// AiAccessPanel
			this.AiAccessPanel.AutoSize = true;
			this.AiAccessPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiAccessPanel.ColumnCount = 3;
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiAccessPanel.Controls.Add(this.AiAccessTitleLabel, 0, 0);
			this.AiAccessPanel.Controls.Add(this.AiAccessEnabledCheckBox, 0, 1);
			this.AiAccessPanel.Controls.Add(this.AiAccessLevelLabel, 0, 2);
			this.AiAccessPanel.Controls.Add(this.AiAccessComboBox, 1, 2);
			this.AiAccessPanel.Controls.Add(this.AiAccessAddressLabel, 0, 3);
			this.AiAccessPanel.Controls.Add(this.AiAccessAddressComboBox, 1, 3);
			this.AiAccessPanel.Controls.Add(this.AiAccessTrustLocalCheckBox, 1, 4);
			this.AiAccessPanel.Controls.Add(this.AiAccessPortLabel, 0, 5);
			this.AiAccessPanel.Controls.Add(this.AiAccessPortTextBox, 1, 5);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlLabel, 0, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlTextBox, 1, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlCopyButton, 2, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessTokenLabel, 0, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessTokenTextBox, 1, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessRegenerateButton, 2, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessSnippetLabel, 0, 8);
			this.AiAccessPanel.Controls.Add(this.AiAccessSnippetTextBox, 1, 8);
			this.AiAccessPanel.Controls.Add(this.AiAccessCopyButton, 2, 8);
			this.AiAccessPanel.Controls.Add(this.AiAccessPromptButton, 1, 9);
			this.AiAccessPanel.Controls.Add(this.AiAccessLogButton, 2, 9);
			this.AiAccessPanel.Controls.Add(this.AiAccessStatusText, 0, 10);
			this.AiAccessPanel.Dock = DockStyle.Top;
			this.AiAccessPanel.Name = "AiAccessPanel";
			this.AiAccessPanel.Padding = new Padding(0, 0, 0, 12);
			this.AiAccessPanel.RowCount = 11;
			this.AiAccessPanel.TabIndex = 1;
			// AiAccessTitleLabel
			this.AiAccessTitleLabel.Anchor = AnchorStyles.Left;
			this.AiAccessTitleLabel.AutoSize = true;
			this.AiAccessPanel.SetColumnSpan(this.AiAccessTitleLabel, 3);
			this.AiAccessTitleLabel.Margin = new Padding(3, 0, 0, 5);
			this.AiAccessTitleLabel.Name = "AiAccessTitleLabel";
			this.AiAccessTitleLabel.TabIndex = 0;
			this.AiAccessTitleLabel.Text = "AI assistant access (MCP server)";
			// AiAccessEnabledCheckBox
			this.AiAccessEnabledCheckBox.Anchor = AnchorStyles.Left;
			this.AiAccessEnabledCheckBox.AutoSize = true;
			this.AiAccessPanel.SetColumnSpan(this.AiAccessEnabledCheckBox, 3);
			this.AiAccessEnabledCheckBox.Margin = new Padding(3, 0, 3, 6);
			this.AiAccessEnabledCheckBox.Name = "AiAccessEnabledCheckBox";
			this.AiAccessEnabledCheckBox.TabIndex = 1;
			this.AiAccessEnabledCheckBox.Text = "AI assistant access";
			this.AiAccessEnabledCheckBox.CheckedChanged += new EventHandler(this.AiAccessEnabledCheckBox_CheckedChanged);
			// AiAccessLevelLabel
			this.AiAccessLevelLabel.Anchor = AnchorStyles.Left;
			this.AiAccessLevelLabel.AutoSize = true;
			this.AiAccessLevelLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiAccessLevelLabel.Name = "AiAccessLevelLabel";
			this.AiAccessLevelLabel.TabIndex = 2;
			this.AiAccessLevelLabel.Text = "Level";
			// AiAccessComboBox
			this.AiAccessComboBox.Anchor = AnchorStyles.Left;
			this.AiAccessComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			this.AiAccessComboBox.Name = "AiAccessComboBox";
			this.AiAccessComboBox.Size = new Size(160, 21);
			this.AiAccessComboBox.TabIndex = 3;
			this.AiAccessComboBox.SelectedIndexChanged += new EventHandler(this.AiAccessComboBox_SelectedIndexChanged);
			// AiAccessAddressLabel
			this.AiAccessAddressLabel.Anchor = AnchorStyles.Left;
			this.AiAccessAddressLabel.AutoSize = true;
			this.AiAccessAddressLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiAccessAddressLabel.Name = "AiAccessAddressLabel";
			this.AiAccessAddressLabel.TabIndex = 4;
			this.AiAccessAddressLabel.Text = "Address";
			// AiAccessAddressComboBox
			this.AiAccessAddressComboBox.Anchor = AnchorStyles.Left;
			this.AiAccessAddressComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
			this.AiAccessAddressComboBox.Name = "AiAccessAddressComboBox";
			this.AiAccessAddressComboBox.Size = new Size(160, 21);
			this.AiAccessAddressComboBox.TabIndex = 5;
			this.AiAccessAddressComboBox.SelectedIndexChanged += new EventHandler(this.AiAccessAddressComboBox_SelectedIndexChanged);
			// AiAccessTrustLocalCheckBox
			this.AiAccessTrustLocalCheckBox.Anchor = AnchorStyles.Left;
			this.AiAccessTrustLocalCheckBox.AutoSize = true;
			this.AiAccessTrustLocalCheckBox.Name = "AiAccessTrustLocalCheckBox";
			this.AiAccessTrustLocalCheckBox.TabIndex = 6;
			this.AiAccessTrustLocalCheckBox.Text = "Trust local connections (no token)";
			this.AiAccessTrustLocalCheckBox.CheckedChanged += new EventHandler(this.AiAccessTrustLocalCheckBox_CheckedChanged);
			// AiAccessPortLabel
			this.AiAccessPortLabel.Anchor = AnchorStyles.Left;
			this.AiAccessPortLabel.AutoSize = true;
			this.AiAccessPortLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiAccessPortLabel.Name = "AiAccessPortLabel";
			this.AiAccessPortLabel.TabIndex = 7;
			this.AiAccessPortLabel.Text = "Port";
			// AiAccessPortTextBox
			this.AiAccessPortTextBox.Anchor = AnchorStyles.Left;
			this.AiAccessPortTextBox.Name = "AiAccessPortTextBox";
			this.AiAccessPortTextBox.Size = new Size(72, 20);
			this.AiAccessPortTextBox.TabIndex = 8;
			this.AiAccessPortTextBox.Validated += new EventHandler(this.AiAccessPortTextBox_Validated);
			// AiAccessUrlLabel
			this.AiAccessUrlLabel.Anchor = AnchorStyles.Left;
			this.AiAccessUrlLabel.AutoSize = true;
			this.AiAccessUrlLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiAccessUrlLabel.Name = "AiAccessUrlLabel";
			this.AiAccessUrlLabel.TabIndex = 9;
			this.AiAccessUrlLabel.Text = "URL";
			// AiAccessUrlTextBox
			this.AiAccessUrlTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessUrlTextBox.Name = "AiAccessUrlTextBox";
			this.AiAccessUrlTextBox.ReadOnly = true;
			this.AiAccessUrlTextBox.TabIndex = 10;
			// AiAccessUrlCopyButton
			this.AiAccessUrlCopyButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessUrlCopyButton.Margin = new Padding(8, 3, 0, 3);
			this.AiAccessUrlCopyButton.MinimumSize = new Size(90, 23);
			this.AiAccessUrlCopyButton.Name = "AiAccessUrlCopyButton";
			this.AiAccessUrlCopyButton.TabIndex = 11;
			this.AiAccessUrlCopyButton.Text = "Copy";
			this.AiAccessUrlCopyButton.Click += new EventHandler(this.AiAccessUrlCopyButton_Click);
			// AiAccessTokenLabel
			this.AiAccessTokenLabel.Anchor = AnchorStyles.Left;
			this.AiAccessTokenLabel.AutoSize = true;
			this.AiAccessTokenLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiAccessTokenLabel.Name = "AiAccessTokenLabel";
			this.AiAccessTokenLabel.TabIndex = 12;
			this.AiAccessTokenLabel.Text = "Token";
			// AiAccessTokenTextBox
			this.AiAccessTokenTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessTokenTextBox.Multiline = true;
			this.AiAccessTokenTextBox.Name = "AiAccessTokenTextBox";
			this.AiAccessTokenTextBox.ReadOnly = true;
			this.AiAccessTokenTextBox.TabIndex = 13;
			// AiAccessRegenerateButton
			this.AiAccessRegenerateButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessRegenerateButton.Margin = new Padding(8, 3, 0, 3);
			this.AiAccessRegenerateButton.MinimumSize = new Size(90, 23);
			this.AiAccessRegenerateButton.Name = "AiAccessRegenerateButton";
			this.AiAccessRegenerateButton.TabIndex = 14;
			this.AiAccessRegenerateButton.Text = "Regenerate";
			this.AiAccessRegenerateButton.Click += new EventHandler(this.AiAccessRegenerateButton_Click);
			// AiAccessSnippetLabel
			this.AiAccessSnippetLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			this.AiAccessSnippetLabel.AutoSize = true;
			this.AiAccessSnippetLabel.Margin = new Padding(3, 6, 8, 0);
			this.AiAccessSnippetLabel.Name = "AiAccessSnippetLabel";
			this.AiAccessSnippetLabel.TabIndex = 15;
			this.AiAccessSnippetLabel.Text = "Assistant snippet";
			// AiAccessSnippetTextBox
			this.AiAccessSnippetTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessSnippetTextBox.Multiline = true;
			this.AiAccessSnippetTextBox.Name = "AiAccessSnippetTextBox";
			this.AiAccessSnippetTextBox.ReadOnly = true;
			this.AiAccessSnippetTextBox.TabIndex = 16;
			// AiAccessCopyButton
			this.AiAccessCopyButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessCopyButton.Margin = new Padding(8, 3, 0, 3);
			this.AiAccessCopyButton.MinimumSize = new Size(90, 23);
			this.AiAccessCopyButton.Name = "AiAccessCopyButton";
			this.AiAccessCopyButton.TabIndex = 17;
			this.AiAccessCopyButton.Text = "Copy";
			this.AiAccessCopyButton.Click += new EventHandler(this.AiAccessCopyButton_Click);
			// AiAccessPromptButton
			this.AiAccessPromptButton.Anchor = AnchorStyles.Left;
			this.AiAccessPromptButton.AutoSize = true;
			this.AiAccessPromptButton.MinimumSize = new Size(90, 23);
			this.AiAccessPromptButton.Name = "AiAccessPromptButton";
			this.AiAccessPromptButton.TabIndex = 18;
			this.AiAccessPromptButton.Text = "Copy prompt";
			this.AiAccessPromptButton.Click += new EventHandler(this.AiAccessPromptButton_Click);
			// AiAccessLogButton
			this.AiAccessLogButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessLogButton.Margin = new Padding(8, 3, 0, 3);
			this.AiAccessLogButton.MinimumSize = new Size(90, 23);
			this.AiAccessLogButton.Name = "AiAccessLogButton";
			this.AiAccessLogButton.TabIndex = 19;
			this.AiAccessLogButton.Text = "Open log";
			this.AiAccessLogButton.Click += new EventHandler(this.AiAccessLogButton_Click);
			// AiAccessStatusText
			this.AiAccessStatusText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiAccessStatusText.AutoSize = true;
			this.AiAccessPanel.SetColumnSpan(this.AiAccessStatusText, 3);
			this.AiAccessStatusText.ForeColor = SystemColors.GrayText;
			this.AiAccessStatusText.Margin = new Padding(3, 6, 0, 0);
			this.AiAccessStatusText.Name = "AiAccessStatusText";
			this.AiAccessStatusText.TabIndex = 20;
			// AiSkillPanel
			this.AiSkillPanel.AutoSize = true;
			this.AiSkillPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiSkillPanel.ColumnCount = 3;
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiSkillPanel.Controls.Add(this.AiSkillTitleLabel, 0, 0);
			this.AiSkillPanel.Controls.Add(this.AiSkillNoteText, 0, 1);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeLabel, 0, 2);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeFolderTextBox, 1, 2);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeButton, 2, 2);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeStatusText, 1, 3);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsLabel, 0, 4);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsFolderTextBox, 1, 4);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsButton, 2, 4);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsStatusText, 1, 5);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipLabel, 0, 6);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipNoteText, 1, 6);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipButton, 2, 6);
			this.AiSkillPanel.Dock = DockStyle.Top;
			this.AiSkillPanel.Name = "AiSkillPanel";
			this.AiSkillPanel.RowCount = 7;
			this.AiSkillPanel.TabIndex = 2;
			// AiSkillTitleLabel
			this.AiSkillTitleLabel.Anchor = AnchorStyles.Left;
			this.AiSkillTitleLabel.AutoSize = true;
			this.AiSkillPanel.SetColumnSpan(this.AiSkillTitleLabel, 3);
			this.AiSkillTitleLabel.Margin = new Padding(3, 0, 0, 5);
			this.AiSkillTitleLabel.Name = "AiSkillTitleLabel";
			this.AiSkillTitleLabel.TabIndex = 0;
			this.AiSkillTitleLabel.Text = "AI skill";
			// AiSkillNoteText
			this.AiSkillNoteText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillNoteText.AutoSize = true;
			this.AiSkillPanel.SetColumnSpan(this.AiSkillNoteText, 3);
			this.AiSkillNoteText.ForeColor = SystemColors.GrayText;
			this.AiSkillNoteText.Margin = new Padding(3, 0, 0, 6);
			this.AiSkillNoteText.Name = "AiSkillNoteText";
			this.AiSkillNoteText.TabIndex = 1;
			// AiSkillClaudeLabel
			this.AiSkillClaudeLabel.Anchor = AnchorStyles.Left;
			this.AiSkillClaudeLabel.AutoSize = true;
			this.AiSkillClaudeLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiSkillClaudeLabel.Name = "AiSkillClaudeLabel";
			this.AiSkillClaudeLabel.TabIndex = 2;
			this.AiSkillClaudeLabel.Text = "Claude Code:";
			// AiSkillClaudeFolderTextBox
			this.AiSkillClaudeFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillClaudeFolderTextBox.Name = "AiSkillClaudeFolderTextBox";
			this.AiSkillClaudeFolderTextBox.ReadOnly = true;
			this.AiSkillClaudeFolderTextBox.TabIndex = 3;
			// AiSkillClaudeButton
			this.AiSkillClaudeButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillClaudeButton.Margin = new Padding(8, 3, 0, 3);
			this.AiSkillClaudeButton.MinimumSize = new Size(90, 23);
			this.AiSkillClaudeButton.Name = "AiSkillClaudeButton";
			this.AiSkillClaudeButton.TabIndex = 4;
			this.AiSkillClaudeButton.Text = "Install";
			this.AiSkillClaudeButton.Click += new EventHandler(this.AiSkillClaudeButton_Click);
			// AiSkillClaudeStatusText
			this.AiSkillClaudeStatusText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillClaudeStatusText.AutoSize = true;
			this.AiSkillClaudeStatusText.Margin = new Padding(3, 0, 3, 6);
			this.AiSkillClaudeStatusText.Name = "AiSkillClaudeStatusText";
			this.AiSkillClaudeStatusText.TabIndex = 5;
			// AiSkillAgentsLabel
			this.AiSkillAgentsLabel.Anchor = AnchorStyles.Left;
			this.AiSkillAgentsLabel.AutoSize = true;
			this.AiSkillAgentsLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiSkillAgentsLabel.Name = "AiSkillAgentsLabel";
			this.AiSkillAgentsLabel.TabIndex = 6;
			this.AiSkillAgentsLabel.Text = "Other agents:";
			// AiSkillAgentsFolderTextBox
			this.AiSkillAgentsFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillAgentsFolderTextBox.Name = "AiSkillAgentsFolderTextBox";
			this.AiSkillAgentsFolderTextBox.ReadOnly = true;
			this.AiSkillAgentsFolderTextBox.TabIndex = 7;
			// AiSkillAgentsButton
			this.AiSkillAgentsButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillAgentsButton.Margin = new Padding(8, 3, 0, 3);
			this.AiSkillAgentsButton.MinimumSize = new Size(90, 23);
			this.AiSkillAgentsButton.Name = "AiSkillAgentsButton";
			this.AiSkillAgentsButton.TabIndex = 8;
			this.AiSkillAgentsButton.Text = "Install";
			this.AiSkillAgentsButton.Click += new EventHandler(this.AiSkillAgentsButton_Click);
			// AiSkillAgentsStatusText
			this.AiSkillAgentsStatusText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillAgentsStatusText.AutoSize = true;
			this.AiSkillAgentsStatusText.Margin = new Padding(3, 0, 3, 6);
			this.AiSkillAgentsStatusText.Name = "AiSkillAgentsStatusText";
			this.AiSkillAgentsStatusText.TabIndex = 9;
			// AiSkillZipLabel
			this.AiSkillZipLabel.Anchor = AnchorStyles.Left;
			this.AiSkillZipLabel.AutoSize = true;
			this.AiSkillZipLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiSkillZipLabel.Name = "AiSkillZipLabel";
			this.AiSkillZipLabel.TabIndex = 10;
			this.AiSkillZipLabel.Text = "Claude app:";
			// AiSkillZipNoteText
			this.AiSkillZipNoteText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillZipNoteText.AutoSize = true;
			this.AiSkillZipNoteText.Name = "AiSkillZipNoteText";
			this.AiSkillZipNoteText.TabIndex = 11;
			// AiSkillZipButton
			this.AiSkillZipButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiSkillZipButton.Margin = new Padding(8, 3, 0, 3);
			this.AiSkillZipButton.MinimumSize = new Size(90, 23);
			this.AiSkillZipButton.Name = "AiSkillZipButton";
			this.AiSkillZipButton.TabIndex = 12;
			this.AiSkillZipButton.Text = "Save as ZIP...";
			this.AiSkillZipButton.Click += new EventHandler(this.AiSkillZipButton_Click);
			// AiAccessUserControl
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.Controls.Add(this.AiPanel);
			this.Name = "AiAccessUserControl";
			this.Size = new Size(600, 560);
			this.AiPanel.ResumeLayout(false);
			this.AiPanel.PerformLayout();
			this.AiHeaderPanel.ResumeLayout(false);
			this.AiHeaderPanel.PerformLayout();
			this.AiAccessPanel.ResumeLayout(false);
			this.AiAccessPanel.PerformLayout();
			this.AiSkillPanel.ResumeLayout(false);
			this.AiSkillPanel.PerformLayout();
			this.ResumeLayout(false);
		}

		#endregion

		private Panel AiPanel;
		private TableLayoutPanel AiHeaderPanel;
		private Label AiHeaderText;
		private Label AiExampleLabel;
		private TextBox AiExampleTextBox;
		private Button AiExampleCopyButton;
		private TableLayoutPanel AiAccessPanel;
		private Label AiAccessTitleLabel;
		private CheckBox AiAccessEnabledCheckBox;
		private Label AiAccessLevelLabel;
		private ComboBox AiAccessComboBox;
		private Label AiAccessAddressLabel;
		private ComboBox AiAccessAddressComboBox;
		private CheckBox AiAccessTrustLocalCheckBox;
		private Label AiAccessPortLabel;
		private TextBox AiAccessPortTextBox;
		private Label AiAccessUrlLabel;
		private TextBox AiAccessUrlTextBox;
		private Button AiAccessUrlCopyButton;
		private Label AiAccessTokenLabel;
		private TextBox AiAccessTokenTextBox;
		private Button AiAccessRegenerateButton;
		private Label AiAccessSnippetLabel;
		private TextBox AiAccessSnippetTextBox;
		private Button AiAccessCopyButton;
		private Button AiAccessPromptButton;
		private Button AiAccessLogButton;
		private Label AiAccessStatusText;
		private TableLayoutPanel AiSkillPanel;
		private Label AiSkillTitleLabel;
		private Label AiSkillNoteText;
		private Label AiSkillClaudeLabel;
		private TextBox AiSkillClaudeFolderTextBox;
		private Button AiSkillClaudeButton;
		private Label AiSkillClaudeStatusText;
		private Label AiSkillAgentsLabel;
		private TextBox AiSkillAgentsFolderTextBox;
		private Button AiSkillAgentsButton;
		private Label AiSkillAgentsStatusText;
		private Label AiSkillZipLabel;
		private Label AiSkillZipNoteText;
		private Button AiSkillZipButton;
	}
}
