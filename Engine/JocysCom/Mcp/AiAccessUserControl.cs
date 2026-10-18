#nullable disable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// The AI page in Windows Forms: the skill that teaches AI agents to use the program, the door an AI assistant or a
	/// script reaches the program by, at the level the person chooses, and the tunnel that lets ChatGPT on the web reach
	/// the door. It shows <see cref="Model"/> and passes every change and press to it; the WPF view AiAccessControl does
	/// the same with the same field names.
	/// </summary>
	/// <remarks>
	/// What the page is for and an example to ask come first, above the tabs, so a person sees what an assistant can do
	/// before how to connect one. Each tab holds one section in one table: rows of caption, value and button. Windows
	/// Forms makes every named control a segment of a path, so a path here goes through the section's tab, and a section
	/// holds its rows with no panel between. Names no WPF type, so a Windows Forms program compiles it without WPF.
	/// </remarks>
	public partial class AiAccessUserControl : UserControl
	{
		bool _showing;
		Font _bold;
		Font _code;

		public AiAccessUserControl()
		{
			InitializeComponent();
			Model = new AiAccessModel(SetClipboard);
			// The tunnel can report its state after the page is gone, while the program closes.
			Model.Changed += (s, e) =>
			{
				if (!IsDisposed)
					ShowModel();
			};
			AiAccessComboBox.Items.AddRange(AiAccessModel.Levels);
			AiAccessAddressComboBox.Items.AddRange(AiAccessModel.Addresses);
			// A box that wraps is as tall as the lines it holds at its width, as the WPF box is.
			foreach (var box in new[] { AiExampleTextBox, AiAccessTokenTextBox, AiAccessSnippetTextBox })
			{
				box.HandleCreated += (s, e) => FitLines((TextBox)s);
				box.TextChanged += (s, e) => FitLines((TextBox)s);
				box.SizeChanged += (s, e) => FitLines((TextBox)s);
			}
			// While this program runs, another one can install or update the skill, and the door can be opened elsewhere.
			VisibleChanged += (s, e) =>
			{
				if (!Visible)
					return;
				Model.Refresh();
				Model.CheckSkills();
			};
			Disposed += (s, e) =>
			{
				Model.Close();
				DisposeFonts();
				// Off the tabs, the tunnel's tab is not disposed with them.
				if (!AiTunnelTabPage.IsDisposed)
					AiTunnelTabPage.Dispose();
			};
			SetFonts();
			ShowModel();
		}

		/// <summary>What the page shows and does.</summary>
		public AiAccessModel Model { get; private set; }

		/// <summary>Shows the program's settings, and changes them from now on.</summary>
		public void Bind(IAiAccessSettings settings)
		{
			Model.Bind(settings);
		}

		/// <summary>Copies the model's state into the controls, without passing the changes it makes back.</summary>
		void ShowModel()
		{
			_showing = true;
			try
			{
				AiHeaderText.Text = AiAccessModel.Header;
				AiExampleTextBox.Text = AiAccessModel.Example;
				AiAccessEnabledCheckBox.Checked = Model.Enabled;
				AiAccessComboBox.SelectedItem = Model.Level;
				AiAccessAddressComboBox.SelectedItem = Model.Address;
				AiAccessTrustLocalCheckBox.Checked = Model.TrustLocal;
				AiAccessTrustLocalCheckBox.Enabled = Model.TrustLocalAllowed;
				AiAccessPortTextBox.Text = Model.Port;
				AiAccessUrlTextBox.Text = Model.Url;
				AiAccessTokenTextBox.Text = Model.Token;
				AiAccessSnippetTextBox.Text = AiAccessModel.Snippet;
				AiAccessStatusText.Text = Model.Status;
				AiSkillNoteText.Text = AiAccessModel.SkillNote;
				ShowRow(Model.Claude, AiSkillClaudeFolderTextBox, AiSkillClaudeStatusText, AiSkillClaudeButton);
				ShowRow(Model.Agents, AiSkillAgentsFolderTextBox, AiSkillAgentsStatusText, AiSkillAgentsButton);
				AiSkillZipNoteText.Text = Model.ZipNote;
				ShowTunnel();
				ShowDoorOnly();
			}
			finally
			{
				_showing = false;
			}
			// A skill button's caption can change its width.
			ShareColumns();
		}

		/// <summary>Shows the tunnel's tab, for a program whose settings keep a tunnel.</summary>
		void ShowTunnel()
		{
			var shown = AiTunnelTabPage.Parent != null;
			if (Model.Tunnel == null)
			{
				if (shown)
					AiTabControl.TabPages.Remove(AiTunnelTabPage);
				return;
			}
			if (!shown)
				AiTabControl.TabPages.Add(AiTunnelTabPage);
			AiTunnelNoteText.Text = AiAccessModel.TunnelNote;
			ShowText(AiTunnelOrganizationTextBox, Model.TunnelOrganization);
			ShowChoices(AiTunnelIdComboBox, Model.TunnelChoices);
			ShowText(AiTunnelIdComboBox, Model.TunnelId);
			ShowText(AiTunnelApiKeyTextBox, Model.TunnelApiKey);
			AiTunnelCreateButton.Enabled = !Model.TunnelBusy;
			AiTunnelDeleteButton.Enabled = Model.TunnelDeleteAllowed;
			AiTunnelCopyButton.Enabled = Model.TunnelId.Length > 0;
			AiTunnelEnabledCheckBox.Checked = Model.TunnelEnabled;
			AiTunnelPageButton.Enabled = Model.TunnelPageAllowed;
			AiTunnelChatGptText.Text = AiAccessModel.TunnelChatGpt;
			AiTunnelFileTextBox.Text = Model.TunnelFile;
			AiTunnelKeysNoteText.Text = AiAccessModel.TunnelKeysNote;
			AiTunnelStatusText.Text = Model.TunnelStatus;
		}

		/// <summary>
		/// For a door other than the program's own, shows its rows alone, on the control itself, which the page holding it
		/// names: the header, the skill and the snippet are about the program, the tunnel leads to the program's door, and
		/// the command line the snippet starts reaches only the program's door.
		/// </summary>
		void ShowDoorOnly()
		{
			if (Model.Door == null || AiAccessPanel.Parent == this)
				return;
			AiHeaderPanel.Visible = false;
			AiTabControl.Visible = false;
			AiAccessSnippetLabel.Visible = false;
			AiAccessSnippetTextBox.Visible = false;
			AiAccessCopyButton.Visible = false;
			Padding = AiAccessTabPage.Padding;
			AutoScroll = true;
			Controls.Add(AiAccessPanel);
		}

		/// <summary>Shows a value in a box the person can type in, unless they are typing in it: the tunnel's state changes on its own while they do.</summary>
		static void ShowText(Control box, string text)
		{
			if (!box.Focused)
				box.Text = text;
		}

		/// <summary>Lists the organization's tunnels under the Tunnel ID box when the list changed, keeping what the box shows.</summary>
		static void ShowChoices(ComboBox box, McpTunnel.Info[] choices)
		{
			var lines = choices.Select(x => x.ToString()).ToArray();
			if (box.Items.Cast<string>().SequenceEqual(lines))
				return;
			var text = box.Text;
			box.Items.Clear();
			box.Items.AddRange(lines);
			if (box.Text != text)
				box.Text = text;
		}

		static void ShowRow(AiAccessModel.SkillRow row, TextBox folder, Label status, Button button)
		{
			folder.Text = row.Folder;
			status.Text = row.Status;
			button.Text = row.Caption;
			button.Enabled = row.Enabled;
			button.AccessibleName = row.AccessibleName;
		}

		/// <summary>Puts text on the clipboard, which throws when another program holds it. No text clears it, which Windows Forms will not set.</summary>
		static void SetClipboard(string text)
		{
			if (string.IsNullOrEmpty(text))
				Clipboard.Clear();
			else
				Clipboard.SetText(text);
		}

		#region Layout

		/// <summary>The example's caption bold, and the token and the snippet in a fixed-width font, all from the page's own font.</summary>
		void SetFonts()
		{
			var bold = new Font(Font, FontStyle.Bold);
			var code = new Font("Consolas", Font.SizeInPoints);
			AiExampleLabel.Font = bold;
			AiAccessTokenTextBox.Font = code;
			AiAccessSnippetTextBox.Font = code;
			AiTunnelIdComboBox.Font = code;
			DisposeFonts();
			_bold = bold;
			_code = code;
		}

		void DisposeFonts()
		{
			if (_bold == null)
				return;
			_bold.Dispose();
			_code.Dispose();
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			SetFonts();
			ShareColumns();
		}

		/// <summary>
		/// Gives the caption column and the button column of every section the width of the widest in any section, so
		/// captions, values and buttons stay in place from tab to tab, as the WPF grids share theirs.
		/// </summary>
		void ShareColumns()
		{
			var tables = new[] { AiHeaderPanel, AiAccessPanel, AiTunnelPanel, AiSkillPanel };
			foreach (var column in new[] { 0, 2 })
			{
				var width = 0;
				foreach (var table in tables)
					foreach (Control control in table.Controls)
						if (table.GetColumn(control) == column && table.GetColumnSpan(control) == 1)
							width = Math.Max(width, control.GetPreferredSize(Size.Empty).Width + control.Margin.Horizontal);
				foreach (var table in tables)
				{
					var style = table.ColumnStyles[column];
					if (style.SizeType != SizeType.Absolute || style.Width != width)
						table.ColumnStyles[column] = new ColumnStyle(SizeType.Absolute, width);
				}
			}
		}

		/// <summary>Makes a box that wraps as tall as the lines the box itself breaks its text into.</summary>
		static void FitLines(TextBox box)
		{
			if (!box.IsHandleCreated)
				return;
			var lines = box.GetLineFromCharIndex(box.TextLength) + 1;
			// The box spaces its lines as Windows draws text, which can be a pixel more than the font's height. Its
			// border, a pixel above the first line and two below the last keep the last line whole.
			var line = TextRenderer.MeasureText("Ag", box.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
			var height = lines * line + box.Height - box.ClientSize.Height + 3;
			if (box.Height != height)
				box.Height = height;
		}

		#endregion

		#region What the person does

		void AiExampleCopyButton_Click(object sender, EventArgs e) { Model.CopyExample(); }

		void AiAccessEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_showing)
				Model.SetEnabled(AiAccessEnabledCheckBox.Checked);
		}

		void AiAccessComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_showing)
				Model.SetLevel(AiAccessComboBox.SelectedItem as string);
		}

		void AiAccessAddressComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_showing)
				Model.SetAddress(AiAccessAddressComboBox.SelectedItem as string);
		}

		void AiAccessTrustLocalCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_showing)
				Model.SetTrustLocal(AiAccessTrustLocalCheckBox.Checked);
		}

		void AiAccessPortTextBox_Validated(object sender, EventArgs e) { Model.SetPort(AiAccessPortTextBox.Text); }

		void AiAccessUrlCopyButton_Click(object sender, EventArgs e) { Model.CopyUrl(); }

		void AiAccessRegenerateButton_Click(object sender, EventArgs e) { Model.Regenerate(); }

		void AiAccessCopyButton_Click(object sender, EventArgs e) { Model.CopySnippet(); }

		void AiAccessPromptButton_Click(object sender, EventArgs e) { Model.CopyPrompt(); }

		void AiAccessLogButton_Click(object sender, EventArgs e) { Model.OpenLog(); }

		void AiTunnelEnabledCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_showing)
				Model.SetTunnelEnabled(AiTunnelEnabledCheckBox.Checked);
		}

		void AiTunnelOrganizationTextBox_Validated(object sender, EventArgs e) { Model.SetTunnelOrganization(AiTunnelOrganizationTextBox.Text); }

		void AiTunnelIdComboBox_Validated(object sender, EventArgs e) { Model.SetTunnelId(AiTunnelIdComboBox.Text); }

		/// <summary>Opening the list asks OpenAI for the organization's tunnels, with the admin key in its box.</summary>
		void AiTunnelIdComboBox_DropDown(object sender, EventArgs e) { Model.ListTunnels(AiTunnelAdminKeyTextBox.Text.Trim()); }

		void AiTunnelIdComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			Model.PickTunnel(AiTunnelIdComboBox.SelectedIndex);
			// The box shows the picked line until the list has closed; then it holds the ID alone, as a typed one is.
			BeginInvoke((Action)(() => AiTunnelIdComboBox.Text = Model.TunnelId));
		}

		void AiTunnelApiKeyTextBox_Validated(object sender, EventArgs e) { Model.SetTunnelApiKey(AiTunnelApiKeyTextBox.Text); }

		void AiTunnelOrganizationButton_Click(object sender, EventArgs e) { Model.OpenOrganizationPage(); }

		void AiTunnelAdminKeyButton_Click(object sender, EventArgs e) { Model.OpenAdminKeysPage(); }

		void AiTunnelApiKeyButton_Click(object sender, EventArgs e) { Model.OpenApiKeysPage(); }

		void AiTunnelCreateButton_Click(object sender, EventArgs e) { Model.CreateTunnel(AiTunnelAdminKeyTextBox.Text.Trim()); }

		void AiTunnelCopyButton_Click(object sender, EventArgs e) { Model.CopyTunnelId(); }

		void AiTunnelDeleteButton_Click(object sender, EventArgs e)
		{
			var answer = MessageBox.Show(this, Model.TunnelDeleteQuestion, "Delete tunnel", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
			if (answer == DialogResult.Yes)
				Model.DeleteTunnel(AiTunnelAdminKeyTextBox.Text.Trim());
		}

		void AiTunnelPageButton_Click(object sender, EventArgs e) { Model.OpenTunnelStatusPage(); }

		void AiTunnelChatGptButton_Click(object sender, EventArgs e) { Model.OpenChatGpt(); }

		void AiTunnelFileButton_Click(object sender, EventArgs e) { Model.ShowTunnelFile(); }

		void AiSkillClaudeButton_Click(object sender, EventArgs e) { Model.InstallClaude(); }

		void AiSkillAgentsButton_Click(object sender, EventArgs e) { Model.InstallAgents(); }

		void AiSkillZipButton_Click(object sender, EventArgs e)
		{
			using (var dialog = new SaveFileDialog
			{
				FileName = AiAccessModel.ZipFileName,
				Filter = AiAccessModel.ZipFilter,
				DefaultExt = AiAccessModel.ZipExtension,
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
					Model.SaveZip(dialog.FileName);
			}
		}

		#endregion
	}
}
