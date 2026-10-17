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
			this.AiHeaderPanel = new TableLayoutPanel();
			this.AiHeaderText = new Label();
			this.AiExampleLabel = new Label();
			this.AiExampleTextBox = new TextBox();
			this.AiExampleCopyButton = new Button();
			this.AiTabControl = new TabControl();
			this.AiSkillTabPage = new TabPage();
			this.AiAccessTabPage = new TabPage();
			this.AiTunnelTabPage = new TabPage();
			this.AiAccessPanel = new TableLayoutPanel();
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
			this.AiTunnelPanel = new TableLayoutPanel();
			this.AiTunnelNoteText = new Label();
			this.AiTunnelOrganizationLabel = new Label();
			this.AiTunnelOrganizationTextBox = new TextBox();
			this.AiTunnelOrganizationButton = new Button();
			this.AiTunnelAdminKeyLabel = new Label();
			this.AiTunnelAdminKeyTextBox = new TextBox();
			this.AiTunnelAdminKeyButton = new Button();
			this.AiTunnelIdLabel = new Label();
			this.AiTunnelIdComboBox = new ComboBox();
			this.AiTunnelCreateButton = new Button();
			this.AiTunnelCopyButton = new Button();
			this.AiTunnelDeleteButton = new Button();
			this.AiTunnelApiKeyLabel = new Label();
			this.AiTunnelApiKeyTextBox = new TextBox();
			this.AiTunnelApiKeyButton = new Button();
			this.AiTunnelEnabledCheckBox = new CheckBox();
			this.AiTunnelPageButton = new Button();
			this.AiTunnelChatGptLabel = new Label();
			this.AiTunnelChatGptText = new Label();
			this.AiTunnelChatGptButton = new Button();
			this.AiTunnelFileLabel = new Label();
			this.AiTunnelFileTextBox = new TextBox();
			this.AiTunnelFileButton = new Button();
			this.AiTunnelKeysNoteText = new Label();
			this.AiTunnelStatusText = new Label();
			this.AiHeaderPanel.SuspendLayout();
			this.AiTabControl.SuspendLayout();
			this.AiSkillTabPage.SuspendLayout();
			this.AiAccessTabPage.SuspendLayout();
			this.AiTunnelTabPage.SuspendLayout();
			this.AiAccessPanel.SuspendLayout();
			this.AiTunnelPanel.SuspendLayout();
			this.AiSkillPanel.SuspendLayout();
			this.SuspendLayout();
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
			this.AiHeaderPanel.Padding = new Padding(3, 3, 9, 6);
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
			// AiTabControl
			this.AiTabControl.Controls.Add(this.AiSkillTabPage);
			this.AiTabControl.Controls.Add(this.AiAccessTabPage);
			this.AiTabControl.Controls.Add(this.AiTunnelTabPage);
			this.AiTabControl.Dock = DockStyle.Fill;
			this.AiTabControl.Name = "AiTabControl";
			this.AiTabControl.SelectedIndex = 0;
			this.AiTabControl.TabIndex = 1;
			// AiSkillTabPage
			this.AiSkillTabPage.AutoScroll = true;
			this.AiSkillTabPage.BackColor = SystemColors.Control;
			this.AiSkillTabPage.Controls.Add(this.AiSkillPanel);
			this.AiSkillTabPage.Name = "AiSkillTabPage";
			this.AiSkillTabPage.Padding = new Padding(3, 3, 9, 3);
			this.AiSkillTabPage.TabIndex = 0;
			this.AiSkillTabPage.Text = "AI Skill";
			// AiAccessTabPage
			this.AiAccessTabPage.AutoScroll = true;
			this.AiAccessTabPage.BackColor = SystemColors.Control;
			this.AiAccessTabPage.Controls.Add(this.AiAccessPanel);
			this.AiAccessTabPage.Name = "AiAccessTabPage";
			this.AiAccessTabPage.Padding = new Padding(3, 3, 9, 3);
			this.AiAccessTabPage.TabIndex = 1;
			this.AiAccessTabPage.Text = "MCP Server - AI Assistant Access";
			// AiTunnelTabPage
			this.AiTunnelTabPage.AutoScroll = true;
			this.AiTunnelTabPage.BackColor = SystemColors.Control;
			this.AiTunnelTabPage.Controls.Add(this.AiTunnelPanel);
			this.AiTunnelTabPage.Name = "AiTunnelTabPage";
			this.AiTunnelTabPage.Padding = new Padding(3, 3, 9, 3);
			this.AiTunnelTabPage.TabIndex = 2;
			this.AiTunnelTabPage.Text = "OpenAI Tunnel";
			// AiAccessPanel
			this.AiAccessPanel.AutoSize = true;
			this.AiAccessPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiAccessPanel.ColumnCount = 3;
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiAccessPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiAccessPanel.Controls.Add(this.AiAccessEnabledCheckBox, 0, 0);
			this.AiAccessPanel.Controls.Add(this.AiAccessLevelLabel, 0, 1);
			this.AiAccessPanel.Controls.Add(this.AiAccessComboBox, 1, 1);
			this.AiAccessPanel.Controls.Add(this.AiAccessAddressLabel, 0, 2);
			this.AiAccessPanel.Controls.Add(this.AiAccessAddressComboBox, 1, 2);
			this.AiAccessPanel.Controls.Add(this.AiAccessTrustLocalCheckBox, 1, 3);
			this.AiAccessPanel.Controls.Add(this.AiAccessPortLabel, 0, 4);
			this.AiAccessPanel.Controls.Add(this.AiAccessPortTextBox, 1, 4);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlLabel, 0, 5);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlTextBox, 1, 5);
			this.AiAccessPanel.Controls.Add(this.AiAccessUrlCopyButton, 2, 5);
			this.AiAccessPanel.Controls.Add(this.AiAccessTokenLabel, 0, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessTokenTextBox, 1, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessRegenerateButton, 2, 6);
			this.AiAccessPanel.Controls.Add(this.AiAccessSnippetLabel, 0, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessSnippetTextBox, 1, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessCopyButton, 2, 7);
			this.AiAccessPanel.Controls.Add(this.AiAccessPromptButton, 1, 8);
			this.AiAccessPanel.Controls.Add(this.AiAccessLogButton, 2, 8);
			this.AiAccessPanel.Controls.Add(this.AiAccessStatusText, 0, 9);
			this.AiAccessPanel.Dock = DockStyle.Top;
			this.AiAccessPanel.Name = "AiAccessPanel";
			this.AiAccessPanel.RowCount = 10;
			this.AiAccessPanel.TabIndex = 0;
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
			// AiTunnelPanel
			this.AiTunnelPanel.AutoSize = true;
			this.AiTunnelPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiTunnelPanel.ColumnCount = 3;
			this.AiTunnelPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiTunnelPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiTunnelPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiTunnelPanel.Controls.Add(this.AiTunnelNoteText, 0, 0);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelOrganizationLabel, 0, 1);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelOrganizationTextBox, 1, 1);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelOrganizationButton, 2, 1);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelAdminKeyLabel, 0, 2);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelAdminKeyTextBox, 1, 2);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelAdminKeyButton, 2, 2);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelIdLabel, 0, 3);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelIdComboBox, 1, 3);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelCreateButton, 2, 3);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelCopyButton, 1, 4);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelDeleteButton, 2, 4);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelApiKeyLabel, 0, 5);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelApiKeyTextBox, 1, 5);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelApiKeyButton, 2, 5);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelEnabledCheckBox, 0, 6);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelPageButton, 2, 6);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelChatGptLabel, 0, 7);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelChatGptText, 1, 7);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelChatGptButton, 2, 7);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelFileLabel, 0, 8);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelFileTextBox, 1, 8);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelFileButton, 2, 8);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelKeysNoteText, 1, 9);
			this.AiTunnelPanel.Controls.Add(this.AiTunnelStatusText, 0, 10);
			this.AiTunnelPanel.Dock = DockStyle.Top;
			this.AiTunnelPanel.Name = "AiTunnelPanel";
			this.AiTunnelPanel.RowCount = 11;
			this.AiTunnelPanel.TabIndex = 0;
			// AiTunnelNoteText
			this.AiTunnelNoteText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelNoteText.AutoSize = true;
			this.AiTunnelPanel.SetColumnSpan(this.AiTunnelNoteText, 3);
			this.AiTunnelNoteText.ForeColor = SystemColors.GrayText;
			this.AiTunnelNoteText.Margin = new Padding(3, 0, 0, 6);
			this.AiTunnelNoteText.Name = "AiTunnelNoteText";
			this.AiTunnelNoteText.TabIndex = 1;
			// AiTunnelOrganizationLabel
			this.AiTunnelOrganizationLabel.Anchor = AnchorStyles.Left;
			this.AiTunnelOrganizationLabel.AutoSize = true;
			this.AiTunnelOrganizationLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiTunnelOrganizationLabel.Name = "AiTunnelOrganizationLabel";
			this.AiTunnelOrganizationLabel.TabIndex = 2;
			this.AiTunnelOrganizationLabel.Text = "Organization ID";
			// AiTunnelOrganizationTextBox
			this.AiTunnelOrganizationTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelOrganizationTextBox.Name = "AiTunnelOrganizationTextBox";
			this.AiTunnelOrganizationTextBox.TabIndex = 3;
			this.AiTunnelOrganizationTextBox.Validated += new EventHandler(this.AiTunnelOrganizationTextBox_Validated);
			// AiTunnelOrganizationButton
			this.AiTunnelOrganizationButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelOrganizationButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelOrganizationButton.MinimumSize = new Size(90, 23);
			this.AiTunnelOrganizationButton.Name = "AiTunnelOrganizationButton";
			this.AiTunnelOrganizationButton.TabIndex = 4;
			this.AiTunnelOrganizationButton.Text = "Find";
			this.AiTunnelOrganizationButton.Click += new EventHandler(this.AiTunnelOrganizationButton_Click);
			// AiTunnelAdminKeyLabel
			this.AiTunnelAdminKeyLabel.Anchor = AnchorStyles.Left;
			this.AiTunnelAdminKeyLabel.AutoSize = true;
			this.AiTunnelAdminKeyLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiTunnelAdminKeyLabel.Name = "AiTunnelAdminKeyLabel";
			this.AiTunnelAdminKeyLabel.TabIndex = 5;
			this.AiTunnelAdminKeyLabel.Text = "Admin key";
			// AiTunnelAdminKeyTextBox
			this.AiTunnelAdminKeyTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelAdminKeyTextBox.Name = "AiTunnelAdminKeyTextBox";
			this.AiTunnelAdminKeyTextBox.TabIndex = 6;
			this.AiTunnelAdminKeyTextBox.UseSystemPasswordChar = true;
			// AiTunnelAdminKeyButton
			this.AiTunnelAdminKeyButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelAdminKeyButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelAdminKeyButton.MinimumSize = new Size(90, 23);
			this.AiTunnelAdminKeyButton.Name = "AiTunnelAdminKeyButton";
			this.AiTunnelAdminKeyButton.TabIndex = 7;
			this.AiTunnelAdminKeyButton.Text = "Get key";
			this.AiTunnelAdminKeyButton.Click += new EventHandler(this.AiTunnelAdminKeyButton_Click);
			// AiTunnelIdLabel
			this.AiTunnelIdLabel.Anchor = AnchorStyles.Left;
			this.AiTunnelIdLabel.AutoSize = true;
			this.AiTunnelIdLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiTunnelIdLabel.Name = "AiTunnelIdLabel";
			this.AiTunnelIdLabel.TabIndex = 8;
			this.AiTunnelIdLabel.Text = "Tunnel ID";
			// AiTunnelIdComboBox
			this.AiTunnelIdComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelIdComboBox.Name = "AiTunnelIdComboBox";
			this.AiTunnelIdComboBox.TabIndex = 9;
			this.AiTunnelIdComboBox.DropDown += new EventHandler(this.AiTunnelIdComboBox_DropDown);
			this.AiTunnelIdComboBox.SelectionChangeCommitted += new EventHandler(this.AiTunnelIdComboBox_SelectionChangeCommitted);
			this.AiTunnelIdComboBox.Validated += new EventHandler(this.AiTunnelIdComboBox_Validated);
			// AiTunnelCreateButton
			this.AiTunnelCreateButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelCreateButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelCreateButton.MinimumSize = new Size(90, 23);
			this.AiTunnelCreateButton.Name = "AiTunnelCreateButton";
			this.AiTunnelCreateButton.TabIndex = 10;
			this.AiTunnelCreateButton.Text = "Create";
			this.AiTunnelCreateButton.Click += new EventHandler(this.AiTunnelCreateButton_Click);
			// AiTunnelCopyButton
			this.AiTunnelCopyButton.Anchor = AnchorStyles.Left;
			this.AiTunnelCopyButton.AutoSize = true;
			this.AiTunnelCopyButton.MinimumSize = new Size(90, 23);
			this.AiTunnelCopyButton.Name = "AiTunnelCopyButton";
			this.AiTunnelCopyButton.TabIndex = 11;
			this.AiTunnelCopyButton.Text = "Copy tunnel ID";
			this.AiTunnelCopyButton.Click += new EventHandler(this.AiTunnelCopyButton_Click);
			// AiTunnelDeleteButton
			this.AiTunnelDeleteButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelDeleteButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelDeleteButton.MinimumSize = new Size(90, 23);
			this.AiTunnelDeleteButton.Name = "AiTunnelDeleteButton";
			this.AiTunnelDeleteButton.TabIndex = 12;
			this.AiTunnelDeleteButton.Text = "Delete...";
			this.AiTunnelDeleteButton.Click += new EventHandler(this.AiTunnelDeleteButton_Click);
			// AiTunnelApiKeyLabel
			this.AiTunnelApiKeyLabel.Anchor = AnchorStyles.Left;
			this.AiTunnelApiKeyLabel.AutoSize = true;
			this.AiTunnelApiKeyLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiTunnelApiKeyLabel.Name = "AiTunnelApiKeyLabel";
			this.AiTunnelApiKeyLabel.TabIndex = 13;
			this.AiTunnelApiKeyLabel.Text = "API key";
			// AiTunnelApiKeyTextBox
			this.AiTunnelApiKeyTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelApiKeyTextBox.Name = "AiTunnelApiKeyTextBox";
			this.AiTunnelApiKeyTextBox.TabIndex = 14;
			this.AiTunnelApiKeyTextBox.UseSystemPasswordChar = true;
			this.AiTunnelApiKeyTextBox.Validated += new EventHandler(this.AiTunnelApiKeyTextBox_Validated);
			// AiTunnelApiKeyButton
			this.AiTunnelApiKeyButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelApiKeyButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelApiKeyButton.MinimumSize = new Size(90, 23);
			this.AiTunnelApiKeyButton.Name = "AiTunnelApiKeyButton";
			this.AiTunnelApiKeyButton.TabIndex = 15;
			this.AiTunnelApiKeyButton.Text = "Get key";
			this.AiTunnelApiKeyButton.Click += new EventHandler(this.AiTunnelApiKeyButton_Click);
			// AiTunnelEnabledCheckBox
			this.AiTunnelEnabledCheckBox.Anchor = AnchorStyles.Left;
			this.AiTunnelEnabledCheckBox.AutoSize = true;
			this.AiTunnelPanel.SetColumnSpan(this.AiTunnelEnabledCheckBox, 2);
			this.AiTunnelEnabledCheckBox.Margin = new Padding(3, 3, 3, 3);
			this.AiTunnelEnabledCheckBox.Name = "AiTunnelEnabledCheckBox";
			this.AiTunnelEnabledCheckBox.TabIndex = 16;
			this.AiTunnelEnabledCheckBox.Text = "Open the tunnel";
			this.AiTunnelEnabledCheckBox.CheckedChanged += new EventHandler(this.AiTunnelEnabledCheckBox_CheckedChanged);
			// AiTunnelPageButton
			this.AiTunnelPageButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelPageButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelPageButton.MinimumSize = new Size(90, 23);
			this.AiTunnelPageButton.Name = "AiTunnelPageButton";
			this.AiTunnelPageButton.TabIndex = 17;
			this.AiTunnelPageButton.Text = "Status page";
			this.AiTunnelPageButton.Click += new EventHandler(this.AiTunnelPageButton_Click);
			// AiTunnelChatGptLabel
			this.AiTunnelChatGptLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			this.AiTunnelChatGptLabel.AutoSize = true;
			this.AiTunnelChatGptLabel.Margin = new Padding(3, 6, 8, 0);
			this.AiTunnelChatGptLabel.Name = "AiTunnelChatGptLabel";
			this.AiTunnelChatGptLabel.TabIndex = 18;
			this.AiTunnelChatGptLabel.Text = "In ChatGPT";
			// AiTunnelChatGptText
			this.AiTunnelChatGptText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelChatGptText.AutoSize = true;
			this.AiTunnelChatGptText.Margin = new Padding(3, 6, 3, 6);
			this.AiTunnelChatGptText.Name = "AiTunnelChatGptText";
			this.AiTunnelChatGptText.TabIndex = 19;
			// AiTunnelChatGptButton
			this.AiTunnelChatGptButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelChatGptButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelChatGptButton.MinimumSize = new Size(90, 23);
			this.AiTunnelChatGptButton.Name = "AiTunnelChatGptButton";
			this.AiTunnelChatGptButton.TabIndex = 20;
			this.AiTunnelChatGptButton.Text = "Open ChatGPT";
			this.AiTunnelChatGptButton.Click += new EventHandler(this.AiTunnelChatGptButton_Click);
			// AiTunnelFileLabel
			this.AiTunnelFileLabel.Anchor = AnchorStyles.Left;
			this.AiTunnelFileLabel.AutoSize = true;
			this.AiTunnelFileLabel.Margin = new Padding(3, 0, 8, 0);
			this.AiTunnelFileLabel.Name = "AiTunnelFileLabel";
			this.AiTunnelFileLabel.TabIndex = 21;
			this.AiTunnelFileLabel.Text = "Kept in";
			// AiTunnelFileTextBox
			this.AiTunnelFileTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelFileTextBox.Name = "AiTunnelFileTextBox";
			this.AiTunnelFileTextBox.ReadOnly = true;
			this.AiTunnelFileTextBox.TabIndex = 22;
			// AiTunnelFileButton
			this.AiTunnelFileButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelFileButton.Margin = new Padding(8, 3, 0, 3);
			this.AiTunnelFileButton.MinimumSize = new Size(90, 23);
			this.AiTunnelFileButton.Name = "AiTunnelFileButton";
			this.AiTunnelFileButton.TabIndex = 23;
			this.AiTunnelFileButton.Text = "Show file";
			this.AiTunnelFileButton.Click += new EventHandler(this.AiTunnelFileButton_Click);
			// AiTunnelKeysNoteText
			this.AiTunnelKeysNoteText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelKeysNoteText.AutoSize = true;
			this.AiTunnelPanel.SetColumnSpan(this.AiTunnelKeysNoteText, 2);
			this.AiTunnelKeysNoteText.ForeColor = SystemColors.GrayText;
			this.AiTunnelKeysNoteText.Margin = new Padding(3, 0, 0, 6);
			this.AiTunnelKeysNoteText.Name = "AiTunnelKeysNoteText";
			this.AiTunnelKeysNoteText.TabIndex = 24;
			// AiTunnelStatusText
			this.AiTunnelStatusText.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.AiTunnelStatusText.AutoSize = true;
			this.AiTunnelPanel.SetColumnSpan(this.AiTunnelStatusText, 3);
			this.AiTunnelStatusText.ForeColor = SystemColors.GrayText;
			this.AiTunnelStatusText.Margin = new Padding(3, 6, 0, 0);
			this.AiTunnelStatusText.Name = "AiTunnelStatusText";
			this.AiTunnelStatusText.TabIndex = 25;
			// AiSkillPanel
			this.AiSkillPanel.AutoSize = true;
			this.AiSkillPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			this.AiSkillPanel.ColumnCount = 3;
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.AiSkillPanel.ColumnStyles.Add(new ColumnStyle());
			this.AiSkillPanel.Controls.Add(this.AiSkillNoteText, 0, 0);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeLabel, 0, 1);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeFolderTextBox, 1, 1);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeButton, 2, 1);
			this.AiSkillPanel.Controls.Add(this.AiSkillClaudeStatusText, 1, 2);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsLabel, 0, 3);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsFolderTextBox, 1, 3);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsButton, 2, 3);
			this.AiSkillPanel.Controls.Add(this.AiSkillAgentsStatusText, 1, 4);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipLabel, 0, 5);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipNoteText, 1, 5);
			this.AiSkillPanel.Controls.Add(this.AiSkillZipButton, 2, 5);
			this.AiSkillPanel.Dock = DockStyle.Top;
			this.AiSkillPanel.Name = "AiSkillPanel";
			this.AiSkillPanel.RowCount = 6;
			this.AiSkillPanel.TabIndex = 0;
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
			this.Controls.Add(this.AiTabControl);
			this.Controls.Add(this.AiHeaderPanel);
			this.Name = "AiAccessUserControl";
			this.Size = new Size(600, 560);
			this.AiHeaderPanel.ResumeLayout(false);
			this.AiHeaderPanel.PerformLayout();
			this.AiAccessPanel.ResumeLayout(false);
			this.AiAccessPanel.PerformLayout();
			this.AiTunnelPanel.ResumeLayout(false);
			this.AiTunnelPanel.PerformLayout();
			this.AiSkillPanel.ResumeLayout(false);
			this.AiSkillPanel.PerformLayout();
			this.AiSkillTabPage.ResumeLayout(false);
			this.AiSkillTabPage.PerformLayout();
			this.AiAccessTabPage.ResumeLayout(false);
			this.AiAccessTabPage.PerformLayout();
			this.AiTunnelTabPage.ResumeLayout(false);
			this.AiTunnelTabPage.PerformLayout();
			this.AiTabControl.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		#endregion

		private TableLayoutPanel AiHeaderPanel;
		private Label AiHeaderText;
		private Label AiExampleLabel;
		private TextBox AiExampleTextBox;
		private Button AiExampleCopyButton;
		private TabControl AiTabControl;
		private TabPage AiSkillTabPage;
		private TabPage AiAccessTabPage;
		private TabPage AiTunnelTabPage;
		private TableLayoutPanel AiAccessPanel;
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
		private TableLayoutPanel AiTunnelPanel;
		private Label AiTunnelNoteText;
		private Label AiTunnelOrganizationLabel;
		private TextBox AiTunnelOrganizationTextBox;
		private Button AiTunnelOrganizationButton;
		private Label AiTunnelAdminKeyLabel;
		private TextBox AiTunnelAdminKeyTextBox;
		private Button AiTunnelAdminKeyButton;
		private Label AiTunnelIdLabel;
		private ComboBox AiTunnelIdComboBox;
		private Button AiTunnelCreateButton;
		private Button AiTunnelCopyButton;
		private Button AiTunnelDeleteButton;
		private Label AiTunnelApiKeyLabel;
		private TextBox AiTunnelApiKeyTextBox;
		private Button AiTunnelApiKeyButton;
		private CheckBox AiTunnelEnabledCheckBox;
		private Button AiTunnelPageButton;
		private Label AiTunnelChatGptLabel;
		private Label AiTunnelChatGptText;
		private Button AiTunnelChatGptButton;
		private Label AiTunnelFileLabel;
		private TextBox AiTunnelFileTextBox;
		private Button AiTunnelFileButton;
		private Label AiTunnelKeysNoteText;
		private Label AiTunnelStatusText;
		private TableLayoutPanel AiSkillPanel;
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
