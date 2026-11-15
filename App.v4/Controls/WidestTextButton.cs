using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	/// <summary>
	/// A button as wide as the longest of the texts it shows in turn, so changing from one to another moves nothing beside it.
	/// </summary>
	/// <remarks>
	/// Windows Forms sizes a button that sizes itself to the text it has at that moment. Remap All reads STOP while it runs,
	/// and a button that narrowed to STOP would pull the list beside it along. The width is the button's own, worked out
	/// by Windows Forms from its font, picture, padding and text, with the text it shows exchanged for the longest of
	/// <see cref="Texts"/>, so it follows a change of font or scale as any button does. Texts are measured with GDI, as
	/// the program draws them.
	/// </remarks>
	public class WidestTextButton : Button
	{
		/// <summary>Every text the button shows in turn; the button is as wide as the longest of them.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string[] Texts
		{
			get { return _Texts; }
			set
			{
				_Texts = value;
				if (Parent != null)
					Parent.PerformLayout(this, nameof(Texts));
			}
		}
		string[] _Texts;

		/// <summary>The size Windows Forms gives the button, widened to the longest of <see cref="Texts"/>.</summary>
		public override Size GetPreferredSize(Size proposedSize)
		{
			var size = base.GetPreferredSize(proposedSize);
			if (_Texts == null || _Texts.Length == 0)
				return size;
			var shown = TextWidth(Text);
			var widest = _Texts.Max(TextWidth);
			if (widest > shown)
				size.Width += widest - shown;
			return size;
		}

		int TextWidth(string text)
		{
			return TextRenderer.MeasureText(text ?? "", Font).Width;
		}
	}
}
