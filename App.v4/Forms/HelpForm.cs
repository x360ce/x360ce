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
			Text = "Help";
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
			_text.Dock = DockStyle.Fill;
			_text.ReadOnly = true;
			_text.BorderStyle = BorderStyle.None;
			_text.BackColor = SystemColors.Window;
			_text.DetectUrls = true;
			var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(6) };
			var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
			bar.Controls.Add(ok);
			Controls.Add(_text);
			Controls.Add(bar);
			AcceptButton = ok;
			CancelButton = ok;
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
