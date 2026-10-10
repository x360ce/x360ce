#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Mcp
{
	/// <summary>
	/// The AI page in Windows Forms: the door an AI assistant or a script reaches the program by, at the level the
	/// person chooses, and the skill that teaches AI agents to use the program. It shows <see cref="Model"/> and passes
	/// every change and press to it; the WPF view AiAccessControl does the same with the same field names, so a path
	/// reads the same in both.
	/// </summary>
	/// <remarks>
	/// Each section is one table: its title, then rows of caption, value and button. Windows Forms makes every named
	/// control a segment of a path, so a section holds its rows with no panel between. Names no WPF type, so a
	/// Windows Forms program compiles it without WPF.
	/// </remarks>
	public partial class AiAccessUserControl : UserControl
	{
		bool _showing;
		Font _title;
		Font _bold;
		Font _code;

		public AiAccessUserControl()
		{
			InitializeComponent();
			Model = new AiAccessModel(SetClipboard);
			Model.Changed += (s, e) => ShowModel();
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
			Disposed += (s, e) => DisposeFonts();
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
			}
			finally
			{
				_showing = false;
			}
			// A skill button's caption can change its width.
			ShareColumns();
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

		/// <summary>Titles larger and bold, the example's caption bold, and the token and the snippet in a fixed-width font, all from the page's own font.</summary>
		void SetFonts()
		{
			var title = new Font(Font.FontFamily, Font.SizeInPoints * 1.25f, FontStyle.Bold);
			var bold = new Font(Font, FontStyle.Bold);
			var code = new Font("Consolas", Font.SizeInPoints);
			AiAccessTitleLabel.Font = title;
			AiSkillTitleLabel.Font = title;
			AiExampleLabel.Font = bold;
			AiAccessTokenTextBox.Font = code;
			AiAccessSnippetTextBox.Font = code;
			DisposeFonts();
			_title = title;
			_bold = bold;
			_code = code;
		}

		void DisposeFonts()
		{
			if (_title == null)
				return;
			_title.Dispose();
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
		/// captions, values and buttons line up down the page, as the WPF grids share theirs.
		/// </summary>
		void ShareColumns()
		{
			var tables = new[] { AiHeaderPanel, AiAccessPanel, AiSkillPanel };
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
