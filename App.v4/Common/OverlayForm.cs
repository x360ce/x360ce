using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>A see-through window drawn over other programs: never takes the focus, lets clicks through, and shows only what is painted on it.</summary>
	/// <remarks>
	/// Drawn by hand, because the built-in balloon tip will not show for a window that is not the
	/// active one, and a window drawn over a game never is. The callout an assistant draws and the
	/// note a hotkey answers with are both this, so the styles that keep a window out of the way
	/// live in one place.
	/// </remarks>
	public class OverlayForm : Form
	{
		public const int WS_EX_TRANSPARENT = 0x20;
		public const int WS_EX_TOOLWINDOW = 0x80;
		public const int WS_EX_NOACTIVATE = 0x08000000;

		/// <summary>The styles that keep the window out of the way: no focus, no taskbar button, clicks fall through.</summary>
		public const int PassiveStyles = WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;

		public OverlayForm()
		{
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			TopMost = true;
			StartPosition = FormStartPosition.Manual;
			BackColor = Color.Magenta;
			TransparencyKey = Color.Magenta;
			Font = SystemFonts.MessageBoxFont;
		}

		protected override bool ShowWithoutActivation { get { return true; } }

		protected override CreateParams CreateParams
		{
			get
			{
				var p = base.CreateParams;
				p.ExStyle |= PassiveStyles;
				return p;
			}
		}
	}
}
