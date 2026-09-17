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
				_Columns = Columns(_Chips);
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

		const int ChipsPerRow = 8;
		const int ChipsPerRowWide = 6;
		const int Gap = 3;
		const int Radius = 5;
		const int PadX = 5;
		const int PadY = 3;

		int _Columns = ChipsPerRow;
		Font _ValueFont;
		int _ChipHeight;
		int _ValueHeight;
		InputChip _Pressed;
		Point _PressedAt;
		InputChip _Dragging;

		/// <summary>Eight chips a row; six once a caption reaches three characters.</summary>
		static int Columns(IList<InputChip> chips)
		{
			foreach (var chip in chips)
				if (chip.Caption.Length > 2)
					return ChipsPerRowWide;
			return ChipsPerRow;
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

		/// <summary>Sets the height the chips need at the current width, so the section above grows with them.</summary>
		void Relayout()
		{
			_ChipHeight = Font.Height + PadY * 2;
			_ValueHeight = ShowsValues ? ValueFont.Height + 2 : 0;
			var rows = (_Chips.Count + _Columns - 1) / _Columns;
			var needed = rows == 0 ? 0 : rows * (_ChipHeight + _ValueHeight) + (rows - 1) * Gap;
			if (Height != needed)
				Height = needed;
		}

		/// <summary>Repaints only when a reading changed, which the model reports.</summary>
		public void Refresh(Engine.CustomDiState state)
		{
			if (InputChips.Update(_Chips, state))
				Invalidate();
		}

		int CellWidth
		{
			get { return Math.Max(1, (ClientSize.Width - Gap * (_Columns - 1)) / _Columns); }
		}

		Rectangle CellBounds(int index)
		{
			var row = index / _Columns;
			var column = index % _Columns;
			var width = CellWidth;
			return new Rectangle(column * (width + Gap), row * (_ChipHeight + _ValueHeight + Gap), width, _ChipHeight + _ValueHeight);
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
				var back = chip == _Dragging ? DragColor : chip.Lit ? LitColor : SystemColors.ControlLight;
				using (var path = Rounded(face, Radius))
				using (var brush = new SolidBrush(back))
					g.FillPath(brush, path);
				TextRenderer.DrawText(g, chip.Caption, Font, face, SystemColors.ControlText, flags);
				if (ShowsValues)
				{
					var strip = new Rectangle(cell.X, cell.Y + _ChipHeight, cell.Width, _ValueHeight);
					TextRenderer.DrawText(g, chip.Value.ToString(), ValueFont, strip, SystemColors.GrayText, flags);
				}
			}
		}

		static GraphicsPath Rounded(Rectangle r, int radius)
		{
			var d = radius * 2;
			var path = new GraphicsPath();
			path.AddArc(r.X, r.Y, d, d, 180, 90);
			path.AddArc(r.Right - d - 1, r.Y, d, d, 270, 90);
			path.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
			path.AddArc(r.X, r.Bottom - d - 1, d, d, 90, 90);
			path.CloseFigure();
			return path;
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
