using JocysCom.ClassLibrary.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App.Forms
{
	/// <summary>Shows one of the help documents the program carries, rendered the way the Help tab renders them.</summary>
	/// <remarks>
	/// The documents are Markdown under <c>docs</c>, embedded at build. A page that wants to explain
	/// itself at length opens one here rather than repeating the text in a label, so there is one
	/// copy of it, readable in the repository as it is on screen.
	/// </remarks>
	public class HelpForm : Form
	{
		readonly RichTextBox _text = new RichTextBox();

		public HelpForm()
		{
			// Laid out as a designer lays a window out, so Windows Forms scales it to the screen when the layout resumes.
			SuspendLayout();
			Name = nameof(HelpForm);
			Text = "Help";
			Icon = Properties.Resources.app;
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = false;
			MaximizeBox = false;
			// The size is in 96-dpi pixels and grows with the screen's scale, as designer forms do;
			// without this a document that fits at 100 % scrolls at 150 %.
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96F, 96F);
			ClientSize = new Size(900, 560);
			MinimumSize = new Size(480, 320);
			_text.Name = "HelpRichTextBox";
			_text.Dock = DockStyle.Fill;
			_text.ReadOnly = true;
			_text.BorderStyle = BorderStyle.None;
			_text.BackColor = SystemColors.Window;
			// The document, and under it its button: each one gap from the window's edges and from the other.
			Padding = new Padding(0, 0, 8, 8);
			_text.Margin = new Padding(8, 8, 0, 0);
			var layout = new TableLayoutPanel { Name = "RootLayoutPanel", Dock = DockStyle.Fill, Margin = new Padding(0), ColumnCount = 1, RowCount = 2 };
			layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			// As wide as a dialog's button at least: Windows Forms' button, which the other windows' buttons keep as their smallest size.
			var ok = new Button { Name = "OkButton", Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(8, 8, 0, 0), MinimumSize = new Size(75, 23) };
			layout.Controls.Add(_text, 0, 0);
			layout.Controls.Add(ok, 0, 1);
			Controls.Add(layout);
			AcceptButton = ok;
			CancelButton = ok;
			ResumeLayout(false);
			PerformLayout();
		}

		/// <summary>Opens the document with this title, in front of the owner, and returns when it is closed.</summary>
		/// <param name="resourceName">The embedded resource, as <c>Documents.Help.Something.md</c>.</param>
		public static void Show(IWin32Window owner, string title, string resourceName)
		{
			using (var form = new HelpForm())
			{
				form.Text = title;
				AppHelper.LoadHelp(form._text, resourceName);
				ControlsHelper.CheckTopMost(form);
				form.ShowDialog(owner);
			}
		}
	}
}
