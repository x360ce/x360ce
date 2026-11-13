using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace x360ce.App.Controls
{
	/// <summary>
	/// One row-wrapped grid of chips, drawn as a single control: the buttons, the axes, the
	/// sliders or the POVs of a device, each lit while it is used, with its reading beneath it.
	/// </summary>
	/// <remarks>
	/// Drawn rather than built from one control per chip, the way the controller picture is: a
	/// device with thirty buttons would otherwise add forty controls to the tab and forty entries
	/// to the navigation tree for what is one thing. The whole grid repaints only when a reading
	/// changed, which the model reports, so an idle device costs nothing per tick.
	/// </remarks>
	public class InputChipGroup : Control
	{
		public InputChipGroup()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
				| ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = SystemColors.Control;
			TabStop = false;
			Height = 0;
		}

		/// <summary>The chips shown, in order. Replaced whole when the device changes.</summary>
		public IList<InputChip> Chips
		{
			get { return _Chips; }
			set
			{
				_Chips = value ?? new List<InputChip>();
				Relayout();
				Invalidate();
			}
		}
		IList<InputChip> _Chips = new List<InputChip>();

		/// <summary>Whether a reading is drawn under each chip. Buttons carry none.</summary>
		public bool ShowsValues { get; set; }

		/// <summary>Raised when a chip is clicked; the chip's payload is the text that maps it.</summary>
		public event EventHandler<EventArgs<InputChip>> ChipClicked;

		/// <summary>Raised when a chip is picked up to be dragged, and when the drag is over.</summary>
		public event EventHandler DragStarted;
		public event EventHandler DragEnded;

		/// <summary>The green of the controller tab lights, so "in use" reads the same everywhere.</summary>
		static readonly Color LitColor = ColorTranslator.FromHtml(AppHelper.StatusGreen);
		static readonly Color DragColor = ColorTranslator.FromHtml(AppHelper.StatusRed);

		/// <summary>At most eight chips a row, as x360ce 5 shows them.</summary>
		const int ChipsPerRow = 8;

		/// <summary>
		/// The room between chips, the rounding of a chip's corners, and the room round its caption across and above and below,
		/// in device-independent pixels.
		/// </summary>
		const int Gap = 4;
		const int Radius = 4;
		const int PadX = 4;
		const int PadY = 4;

		int _Columns = ChipsPerRow;
		Font _ValueFont;
		int _ChipHeight;
		int _ValueHeight;

		/// <summary><see cref="Gap"/>, <see cref="Radius"/> and <see cref="PadX"/> in this screen's pixels, set as the chips are laid out.</summary>
		int _Gap, _Radius, _PadX;
		InputChip _Pressed;
		Point _PressedAt;
		InputChip _Dragging;

		/// <summary>The widest reading a chip shows: an axis at its lowest.</summary>
		const string WidestReading = "-32768";

		/// <summary>
		/// As many chips a row as fit the width, each as wide as the widest caption needs, and the widest reading where readings
		/// are shown, in the fonts they are drawn in; eight at most.
		/// </summary>
		int Columns()
		{
			var flags = TextFormatFlags.NoPadding;
			var widest = ShowsValues ? TextRenderer.MeasureText(WidestReading, ValueFont, Size.Empty, flags).Width : 0;
			foreach (var chip in _Chips)
				widest = Math.Max(widest, TextRenderer.MeasureText(chip.Caption, Font, Size.Empty, flags).Width + _PadX * 2);
			return Math.Max(1, Math.Min(ChipsPerRow, (ClientSize.Width - Padding.Horizontal + _Gap) / (widest + _Gap)));
		}

		Font ValueFont
		{
			get
			{
				if (_ValueFont == null)
					_ValueFont = new Font(Font.FontFamily, Font.Size * 0.8f, Font.Style);
				return _ValueFont;
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			if (_ValueFont != null)
			{
				_ValueFont.Dispose();
				_ValueFont = null;
			}
			Relayout();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Relayout();
		}

		protected override void OnPaddingChanged(EventArgs e)
		{
			base.OnPaddingChanged(e);
			Relayout();
		}

		/// <summary>
		/// Sets the chips a row and the height they need at the current width, with the strip's padding round them, so the
		/// section above grows with them.
		/// </summary>
		void Relayout()
		{
			var scale = JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale;
			_Gap = (int)Math.Round(Gap * scale);
			_Radius = (int)Math.Round(Radius * scale);
			_PadX = (int)Math.Round(PadX * scale);
			_Columns = Columns();
			_ChipHeight = Font.Height + (int)Math.Round(PadY * scale) * 2;
			_ValueHeight = ShowsValues ? ValueFont.Height + 2 : 0;
			var rows = (_Chips.Count + _Columns - 1) / _Columns;
			var needed = rows == 0 ? 0 : rows * (_ChipHeight + _ValueHeight) + (rows - 1) * _Gap + Padding.Vertical;
			if (Height != needed)
				Height = needed;
		}

		/// <summary>Repaints only when a reading changed, which the model reports.</summary>
		public void Refresh(Engine.SourceState state)
		{
			if (InputChips.Update(_Chips, state, InputChips.Now))
				Invalidate();
		}

		/// <summary>
		/// A chip's place, inside the strip's padding. The chips of a row share what the gaps leave of the width, a pixel that does
		/// not divide evenly going to each of some of them, so a full row ends at the padding's right edge and the padding sets the
		/// room beside it.
		/// </summary>
		Rectangle CellBounds(int index)
		{
			var row = index / _Columns;
			var column = index % _Columns;
			var room = Math.Max(_Columns, ClientSize.Width - Padding.Horizontal - _Gap * (_Columns - 1));
			var left = Padding.Left + column * room / _Columns + column * _Gap;
			var right = Padding.Left + (column + 1) * room / _Columns + column * _Gap;
			return new Rectangle(left, Padding.Top + row * (_ChipHeight + _ValueHeight + _Gap), right - left, _ChipHeight + _ValueHeight);
		}

		/// <summary>Where the chip is drawn, in client coordinates; empty when it is not shown here.</summary>
		public Rectangle BoundsOf(InputChip chip)
		{
			var index = _Chips.IndexOf(chip);
			return index < 0 ? Rectangle.Empty : CellBounds(index);
		}

		/// <summary>The chip under the point, or null.</summary>
		public InputChip HitTest(Point location)
		{
			for (var i = 0; i < _Chips.Count; i++)
				if (CellBounds(i).Contains(location))
					return _Chips[i];
			return null;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			var g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
			for (var i = 0; i < _Chips.Count; i++)
			{
				var chip = _Chips[i];
				var cell = CellBounds(i);
				var face = new Rectangle(cell.X, cell.Y, cell.Width, _ChipHeight);
				var marked = chip == _Dragging || chip.Lit;
				var back = chip == _Dragging ? DragColor : chip.Lit ? LitColor : SystemColors.ControlLight;
				using (var path = AppHelper.RoundedRectangle(new Rectangle(face.X, face.Y, face.Width - 1, face.Height - 1), _Radius))
				using (var brush = new SolidBrush(back))
					g.FillPath(brush, path);
				// The status colours are as light in both themes, so the text on them is the light theme's.
				var text = marked ? JocysCom.ClassLibrary.Controls.Themes.FormsTheme.LightPalette["ForegroundBlack"] : SystemColors.ControlText;
				TextRenderer.DrawText(g, chip.Caption, Font, face, text, flags);
				if (ShowsValues)
				{
					var strip = new Rectangle(cell.X, cell.Y + _ChipHeight, cell.Width, _ValueHeight);
					TextRenderer.DrawText(g, chip.Value.ToString(), ValueFont, strip, SystemColors.GrayText, flags);
				}
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button != MouseButtons.Left)
				return;
			_Pressed = HitTest(e.Location);
			_PressedAt = e.Location;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			// The hand says a chip can be picked up, which is all the instruction it needs.
			Cursor = HitTest(e.Location) == null ? Cursors.Default : Cursors.Hand;
			if (_Pressed == null || _Dragging != null || (e.Button & MouseButtons.Left) == 0)
				return;
			var drag = SystemInformation.DragSize;
			if (Math.Abs(e.X - _PressedAt.X) < drag.Width / 2 && Math.Abs(e.Y - _PressedAt.Y) < drag.Height / 2)
				return;
			// Past the drag threshold: the chip travels to a mapping box. The chip is painted in
			// the recording colour while it is in the air, and DoDragDrop returns when it lands.
			_Dragging = _Pressed;
			_Pressed = null;
			Invalidate();
			var started = DragStarted;
			if (started != null)
				started(this, EventArgs.Empty);
			try
			{
				DoDragDrop(_Dragging.Payload, DragDropEffects.Copy);
			}
			finally
			{
				_Dragging = null;
				Invalidate();
				var ended = DragEnded;
				if (ended != null)
					ended(this, EventArgs.Empty);
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			var pressed = _Pressed;
			_Pressed = null;
			if (pressed == null || e.Button != MouseButtons.Left)
				return;
			if (HitTest(e.Location) != pressed)
				return;
			var handler = ChipClicked;
			if (handler != null)
				handler(this, new EventArgs<InputChip>(pressed));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _ValueFont != null)
			{
				_ValueFont.Dispose();
				_ValueFont = null;
			}
			base.Dispose(disposing);
		}
	}
}
