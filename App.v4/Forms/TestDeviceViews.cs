using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App.Forms
{
	/// <summary>The colours and shapes the Test Device window draws with: the theme's text roles, and the chart box the other pages draw.</summary>
	static class DeviceTestPaint
	{
		public static Color Text { get { return FormsTheme.GetColor("ForegroundBlack"); } }
		public static Color Gray { get { return FormsTheme.GetColor("ForegroundGray"); } }
		public static Color Edge { get { return FormsTheme.GetColor("BorderGray"); } }
		public static Color Filling { get { return FormsTheme.GetColor("ForegroundInformation"); } }
		public static Color Full { get { return FormsTheme.GetColor("ForegroundSuccess"); } }
		public static Color Now { get { return FormsTheme.GetColor("ForegroundActive"); } }

		/// <summary>
		/// A grade's colour on the usual quality scale, always beside its word: Best dark green, Good green, Average amber,
		/// Poor orange, Bad red. In the dark theme the theme's own hues, green to red; in the light one darker shades of
		/// them, so the word stands at 4.5:1 or more on a card in either.
		/// </summary>
		public static Color Grade(DeviceTestGrade? grade)
		{
			switch (grade)
			{
				case DeviceTestGrade.Best: return FormsTheme.GetColor("SolidColorBrush_120D", Color.FromArgb(0x0A, 0x6B, 0x2B));
				case DeviceTestGrade.Good: return FormsTheme.GetColor("SolidColorBrush_090D", Color.FromArgb(0x2D, 0x7D, 0x1A));
				case DeviceTestGrade.Average: return FormsTheme.GetColor("SolidColorBrush_060D", Color.FromArgb(0x8A, 0x65, 0x00));
				case DeviceTestGrade.Poor: return FormsTheme.GetColor("SolidColorBrush_030D", Color.FromArgb(0xB2, 0x4A, 0x00));
				case DeviceTestGrade.Bad: return FormsTheme.GetColor("SolidColorBrush_000D", Color.FromArgb(0xC0, 0x28, 0x1E));
				default: return Gray;
			}
		}

		/// <summary>A progress bar: an outlined track, filled to <paramref name="share"/> in the filling colour, or whole in the full one.</summary>
		public static void Bar(Graphics g, Rectangle r, double share)
		{
			using (var fill = new SolidBrush(share >= 1 ? Full : Filling))
				g.FillRectangle(fill, r.X, r.Y, (int)Math.Round(r.Width * Math.Max(0, Math.Min(1, share))), r.Height);
			using (var pen = new Pen(Edge))
				g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
		}

		/// <summary>A word in a rounded outline of its colour, as a grade or a button is marked; with <paramref name="measureOnly"/>, only its size, for which <paramref name="g"/> may be null.</summary>
		public static Size Chip(Graphics g, Font font, string text, Color color, Point at, bool measureOnly = false)
		{
			var size = g == null ? TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding) : TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
			var room = new Size(font.Height / 2, font.Height / 6);
			var box = new Rectangle(at, new Size(size.Width + room.Width * 2, size.Height + room.Height * 2));
			if (measureOnly)
				return box.Size;
			var smoothing = g.SmoothingMode;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			using (var pen = new Pen(color))
			using (var path = AppHelper.RoundedRectangle(new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1), font.Height / 3))
				g.DrawPath(pen, path);
			g.SmoothingMode = smoothing;
			TextRenderer.DrawText(g, text, font, new Point(box.X + room.Width, box.Y + room.Height), color, TextFormatFlags.NoPadding);
			return box.Size;
		}
	}

	/// <summary>A drawn view of the window's: double-buffered, transparent round its box, and named for the door and screen readers by what it shows.</summary>
	abstract class DeviceTestView : Control
	{
		protected DeviceTestView()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
				| ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
			// Made after the window was scaled to the screen, so scaled here as the designer's margins are.
			var margin = (int)Math.Round(4 * JocysCom.ClassLibrary.Controls.ControlsHelper.DpiScale);
			Margin = new Padding(margin);
		}

		Font _bold, _large;

		/// <summary>The font in bold, and bold half as large again, made from the font the theme gives.</summary>
		protected Font Bold { get { return _bold ?? (_bold = new Font(Font, FontStyle.Bold)); } }
		protected Font Large { get { return _large ?? (_large = new Font(Font.FontFamily, Font.SizeInPoints * 1.5f, FontStyle.Bold, GraphicsUnit.Point)); } }

		protected override void OnFontChanged(EventArgs e)
		{
			DisposeFonts();
			base.OnFontChanged(e);
			Invalidate();
		}

		void DisposeFonts()
		{
			if (_bold != null)
				_bold.Dispose();
			if (_large != null)
				_large.Dispose();
			_bold = _large = null;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				DisposeFonts();
			base.Dispose(disposing);
		}

		/// <summary>Names the view by what it shows, once it changed.</summary>
		protected void Say(string text)
		{
			if (AccessibleName != text)
				AccessibleName = text;
		}

		/// <summary>The room inside the box, as the charts on the other pages keep.</summary>
		protected Rectangle Inside { get { return AppHelper.ChartBounds(this); } }
	}

	/// <summary>The five steps in a column, each numbered in a circle: ticked once done, filled while it runs; before the test each also says what it asks.</summary>
	sealed class DeviceTestStepsView : DeviceTestView
	{
		public DeviceTestStepsView()
		{
			Name = "StepsView";
			ShowStep(0);
		}

		int _current = -1;

		/// <summary>Shows the steps with <paramref name="current"/> running, 1 to 5; 0 before the test.</summary>
		public void ShowStep(int current)
		{
			if (current == _current)
				return;
			var resize = (current == 0) != (_current == 0);
			_current = current;
			Say(string.Join("; ", DeviceTestSteps.StepNames.Select((x, i) => (i + 1) + ". " + x + (i + 1 < current ? ", done" : i + 1 == current ? ", now" : ""))));
			if (resize && Width > 0)
				Height = GetPreferredSize(new Size(Width, 0)).Height;
			Invalidate();
		}

		/// <summary>A step's height: its name, and before the test the line on what it asks.</summary>
		int RowHeight { get { return Font.Height * (_current == 0 ? 4 : 3); } }

		public override Size GetPreferredSize(Size proposedSize)
		{
			var width = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue / 2 ? proposedSize.Width : Font.Height * 40;
			return new Size(width, RowHeight * DeviceTestSteps.StepCount);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			var h = Font.Height;
			var side = h * 2;
			var smoothing = g.SmoothingMode;
			for (var i = 0; i < DeviceTestSteps.StepCount; i++)
			{
				var number = i + 1;
				var done = number < _current;
				var now = number == _current;
				var circle = new Rectangle(0, i * RowHeight, side, side);
				var color = done ? DeviceTestPaint.Full : now ? DeviceTestPaint.Filling : DeviceTestPaint.Edge;
				g.SmoothingMode = SmoothingMode.AntiAlias;
				if (now)
					using (var fill = new SolidBrush(color))
						g.FillEllipse(fill, circle);
				else
					using (var pen = new Pen(color, Math.Max(1, h / 10)))
						g.DrawEllipse(pen, circle.X, circle.Y, circle.Width - 1, circle.Height - 1);
				g.SmoothingMode = smoothing;
				var mark = done ? "\u2713" : number.ToString();
				var markColor = now ? FormsTheme.FrameFace(this) : done ? DeviceTestPaint.Full : DeviceTestPaint.Gray;
				TextRenderer.DrawText(g, mark, Large, circle, markColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
				var x = side + h / 2;
				var y = circle.Y + (side - Large.Height) / 2;
				TextRenderer.DrawText(g, DeviceTestSteps.StepNames[i], Large, new Point(x, y), done || now ? DeviceTestPaint.Text : DeviceTestPaint.Gray, TextFormatFlags.NoPadding);
				if (_current == 0)
					TextRenderer.DrawText(g, DeviceTestSteps.StepTexts[i], Font, new Point(x, y + Large.Height), DeviceTestPaint.Gray, TextFormatFlags.NoPadding);
			}
		}
	}

	/// <summary>A stick or a trigger while it is moved: its name, a live picture, its progress bar and how full it is.</summary>
	/// <remarks>
	/// A stick is a circle whose ten-degree sectors fill as it reaches the edge in each, with the shape it reached and the
	/// circle that shape is graded against; a trigger, slider or other single axis is a bar that fills over the range it
	/// covered. The dot or line is where the control is now.
	/// </remarks>
	sealed class DeviceTestControlView : DeviceTestView
	{
		public DeviceTestControlView(DeviceTestControl control)
		{
			Item = control;
			Name = new string(control.Name.Where(char.IsLetterOrDigit).ToArray()) + "View";
			Redraw();
		}

		public DeviceTestControl Item { get; }

		/// <summary>How full it is, in words: "64 %", "full", "not moved".</summary>
		string Status
		{
			get
			{
				if (Item.Unstable)
					return "unstable";
				if (Item.Skipped)
					return "skipped";
				if (Item.NotMoved)
					return "not moved";
				return Item.Complete ? "\u2713 full" : (int)Math.Floor(Item.Progress * 100) + " %";
			}
		}

		/// <summary>Draws the control as it is now.</summary>
		public void Redraw()
		{
			Say(Item.Name + ": " + Status);
			Invalidate();
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			var h = Font.Height;
			var pad = AppHelper.ChartBounds(this).X;
			return new Size(h * 10 + pad * 2, h * 12 + pad * 2);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			AppHelper.PaintChartBox(g, this);
			var r = Inside;
			var h = Font.Height;
			// The name on up to two lines, as a stick's names its two axes.
			TextRenderer.DrawText(g, Item.Name, Bold, new Rectangle(r.X, r.Y, r.Width, h * 2), DeviceTestPaint.Text, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
			var picture = new Rectangle(r.X, r.Y + h * 2 + h / 2, r.Width, h * 7);
			if (Item.IsStick)
				PaintStick(g, picture);
			else
				PaintAxis(g, picture);
			var bar = new Rectangle(r.X, picture.Bottom + h / 2, r.Width, Math.Max(4, h / 2));
			var left = Item.NotMoved || Item.Skipped;
			DeviceTestPaint.Bar(g, bar, left ? 0 : Item.Progress);
			var status = Item.Unstable ? FormsTheme.GetColor("ForegroundCaution") : left ? DeviceTestPaint.Gray : Item.Complete ? DeviceTestPaint.Full : DeviceTestPaint.Text;
			TextRenderer.DrawText(g, Status, Font, new Point(r.X, bar.Bottom + h / 4), status, TextFormatFlags.NoPadding);
		}

		void PaintStick(Graphics g, Rectangle area)
		{
			var side = Math.Min(area.Width, area.Height) - 1;
			var circle = new RectangleF(area.X + (area.Width - side) / 2f, area.Y + (area.Height - side) / 2f, side, side);
			var centre = new PointF(circle.X + side / 2f, circle.Y + side / 2f);
			var radius = side / 2f;
			var pair = Item.Pair;
			var smoothing = g.SmoothingMode;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			var full = Item.Complete;
			// Each sector reached at the edge is filled; the picture's angles go clockwise, the sectors anticlockwise from the right.
			using (var fill = new SolidBrush(Color.FromArgb(72, full ? DeviceTestPaint.Full : DeviceTestPaint.Filling)))
				for (var s = 0; s < DeviceTestSteps.Sectors; s++)
					if (pair.Radius[s] >= DeviceTestSteps.EdgeRadius)
						g.FillPie(fill, circle.X, circle.Y, side, side, -(s + 1) * 360f / DeviceTestSteps.Sectors, 360f / DeviceTestSteps.Sectors);
			using (var edge = new Pen(DeviceTestPaint.Edge))
			{
				g.DrawEllipse(edge, circle);
				g.DrawLine(edge, circle.X, centre.Y, circle.Right, centre.Y);
				g.DrawLine(edge, centre.X, circle.Y, centre.X, circle.Bottom);
			}
			// The shape reached, and the true circle it is graded against: as wide as the shape on average.
			var reached = Enumerable.Range(0, DeviceTestSteps.Sectors).Where(s => pair.Radius[s] > 0).ToArray();
			if (reached.Length > 1)
			{
				var points = reached.Select(s =>
				{
					var angle = (s + 0.5) * 2 * Math.PI / DeviceTestSteps.Sectors;
					var r = (float)Math.Min(pair.Radius[s], 1.42) * radius;
					return new PointF(centre.X + r * (float)Math.Cos(angle), centre.Y - r * (float)Math.Sin(angle));
				}).ToArray();
				using (var shape = new Pen(full ? DeviceTestPaint.Full : DeviceTestPaint.Filling, 2))
					if (reached.Length == DeviceTestSteps.Sectors)
						g.DrawPolygon(shape, points);
					else
						g.DrawLines(shape, points);
				var atEdge = pair.Radius.Where(r => r >= DeviceTestSteps.EdgeRadius).ToArray();
				if (atEdge.Length > 0)
				{
					var mean = (float)Math.Min(atEdge.Average(), 1.42) * radius;
					using (var trueCircle = new Pen(DeviceTestPaint.Gray) { DashStyle = DashStyle.Dash })
						g.DrawEllipse(trueCircle, centre.X - mean, centre.Y - mean, mean * 2, mean * 2);
				}
			}
			var x = centre.X + Item.X.Value / (float)short.MaxValue * radius;
			var y = centre.Y + Item.Y.Value / (float)short.MaxValue * radius;
			var dot = Math.Max(3f, Font.Height / 3f);
			using (var now = new SolidBrush(DeviceTestPaint.Now))
				g.FillEllipse(now, x - dot, y - dot, dot * 2, dot * 2);
			g.SmoothingMode = smoothing;
		}

		void PaintAxis(Graphics g, Rectangle area)
		{
			var width = Font.Height * 2;
			var track = new Rectangle(area.X + (area.Width - width) / 2, area.Y, width, area.Height - 1);
			// Up is the top of the range, as a trigger pressed all the way.
			Func<int, int> at = v => track.Bottom - (int)Math.Round((v - (double)short.MinValue) * track.Height / ushort.MaxValue);
			var axis = Item.X;
			var full = Item.Complete;
			using (var fill = new SolidBrush(Color.FromArgb(72, full ? DeviceTestPaint.Full : DeviceTestPaint.Filling)))
				g.FillRectangle(fill, track.X, at(axis.Max), track.Width, Math.Max(1, at(axis.Min) - at(axis.Max)));
			using (var edge = new Pen(DeviceTestPaint.Edge))
				g.DrawRectangle(edge, track);
			using (var now = new Pen(DeviceTestPaint.Now, Math.Max(2, Font.Height / 6)))
				g.DrawLine(now, track.X - Font.Height / 2, at(axis.Value), track.Right + Font.Height / 2, at(axis.Value));
		}
	}

	/// <summary>The buttons, each a chip with its number: ticked once pressed, marked when one push registered twice, and grey when never pressed.</summary>
	sealed class DeviceTestButtonsView : DeviceTestView
	{
		public DeviceTestButtonsView()
		{
			Name = "ButtonsView";
		}

		DeviceTestButton[] _buttons = new DeviceTestButton[0];

		public void SetButtons(DeviceTestButton[] buttons)
		{
			_buttons = buttons ?? new DeviceTestButton[0];
			Redraw();
			if (Parent != null)
				Parent.PerformLayout();
		}

		/// <summary>Draws the buttons as they are now.</summary>
		public void Redraw()
		{
			var pressed = _buttons.Count(x => x.Pressed);
			var twice = _buttons.Where(x => x.Twice).Select(x => x.Number).ToArray();
			Say(string.Format("{0} of {1} buttons pressed", pressed, _buttons.Length)
				+ (twice.Length > 0 ? "; one push registered twice on button " + string.Join(", ", twice) : ""));
			Invalidate();
		}

		static string ChipText(DeviceTestButton button)
		{
			if (button.Unstable)
				return button.Number + " unstable";
			if (button.Skipped)
				return button.Number + " skipped";
			if (button.Twice)
				return "\u2713 " + button.Number + " twice";
			return button.Pressed ? "\u2713 " + button.Number : button.Number.ToString();
		}

		static Color ChipColor(DeviceTestButton button)
		{
			if (button.Unstable || button.Twice)
				return FormsTheme.GetColor("ForegroundCaution");
			if (button.Pressed)
				return DeviceTestPaint.Full;
			return button.NotPressed || button.Skipped ? DeviceTestPaint.Gray : DeviceTestPaint.Text;
		}

		/// <summary>Lays the chips out in rows as wide as <paramref name="width"/>, each where it is drawn.</summary>
		Rectangle[] Place(Graphics g, int width, out int height)
		{
			var gap = Font.Height / 3;
			var boxes = new Rectangle[_buttons.Length];
			int x = 0, y = 0, row = 0;
			for (var i = 0; i < _buttons.Length; i++)
			{
				var size = DeviceTestPaint.Chip(g, Bold, ChipText(_buttons[i]), ChipColor(_buttons[i]), Point.Empty, true);
				if (x > 0 && x + size.Width > width)
				{
					x = 0;
					y += row + gap;
					row = 0;
				}
				boxes[i] = new Rectangle(new Point(x, y), size);
				x += size.Width + gap;
				row = Math.Max(row, size.Height);
			}
			height = y + row;
			return boxes;
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			var width = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue / 2 ? proposedSize.Width : Font.Height * 40;
			int height;
			Place(null, width, out height);
			return new Size(width, Math.Max(height, Font.Height));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			int height;
			var boxes = Place(e.Graphics, Width, out height);
			for (var i = 0; i < boxes.Length; i++)
				DeviceTestPaint.Chip(e.Graphics, Bold, ChipText(_buttons[i]), ChipColor(_buttons[i]), boxes[i].Location);
		}
	}

	/// <summary>A result's card: its name, its value in bold beside its grade, and one line on what it tells; or the overall grade, large.</summary>
	sealed class DeviceTestCard : DeviceTestView
	{
		public DeviceTestCard(string name)
		{
			Name = name;
		}

		public string Title { get; private set; } = "";
		public string Value { get; private set; } = "";
		public string Explanation { get; private set; } = "";
		public DeviceTestGrade? Grade { get; private set; }

		/// <summary>A smaller line under the value, of what it is made of; empty without one.</summary>
		public string Detail { get; private set; } = "";

		/// <summary>The overall grade: its word large, in its grade's colour, in place of a value and a chip.</summary>
		public bool Overall { get; set; }

		public void Set(string title, string value, DeviceTestGrade? grade, string explanation, string detail = null)
		{
			Title = title;
			Value = value;
			Grade = grade;
			Explanation = explanation;
			Detail = detail ?? "";
			Say(title + ": " + value + (grade.HasValue && !Overall ? ", " + grade.Value : "") + (Detail.Length > 0 ? ", " + Detail : "") + ". " + explanation);
			if (Parent != null)
				Parent.PerformLayout();
			Invalidate();
		}

		const TextFormatFlags Wrapped = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;

		public override Size GetPreferredSize(Size proposedSize)
		{
			var pad = AppHelper.ChartBounds(this).X;
			var width = Width;
			var text = new Size(width - pad * 2, int.MaxValue);
			var explanation = TextRenderer.MeasureText(Explanation, Font, text, Wrapped).Height;
			var detail = Detail.Length > 0 ? TextRenderer.MeasureText(Detail, Font, text, Wrapped).Height : 0;
			return new Size(width, pad + Font.Height + Font.Height / 4 + Large.Height + detail + Font.Height / 4 + explanation + pad);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;
			AppHelper.PaintChartBox(g, this);
			var r = Inside;
			var h = Font.Height;
			TextRenderer.DrawText(g, Title, Font, new Point(r.X, r.Y), DeviceTestPaint.Gray, TextFormatFlags.NoPadding);
			var y = r.Y + h + h / 4;
			if (Overall)
				TextRenderer.DrawText(g, Value, Large, new Point(r.X, y), DeviceTestPaint.Grade(Grade), TextFormatFlags.NoPadding);
			else
			{
				var color = Grade.HasValue ? DeviceTestPaint.Text : DeviceTestPaint.Gray;
				TextRenderer.DrawText(g, Value, Large, new Point(r.X, y), color, TextFormatFlags.NoPadding);
				if (Grade.HasValue)
				{
					var width = TextRenderer.MeasureText(g, Value, Large, Size.Empty, TextFormatFlags.NoPadding).Width;
					var chip = DeviceTestPaint.Chip(g, Bold, Grade.Value.ToString(), DeviceTestPaint.Grade(Grade), Point.Empty, true);
					DeviceTestPaint.Chip(g, Bold, Grade.Value.ToString(), DeviceTestPaint.Grade(Grade), new Point(r.X + width + h / 2, y + (Large.Height - chip.Height) / 2));
				}
			}
			y += Large.Height;
			if (Detail.Length > 0)
			{
				var lines = TextRenderer.MeasureText(g, Detail, Font, new Size(r.Width, int.MaxValue), Wrapped).Height;
				TextRenderer.DrawText(g, Detail, Font, new Rectangle(r.X, y, r.Width, lines), DeviceTestPaint.Gray, Wrapped);
				y += lines;
			}
			y += h / 4;
			TextRenderer.DrawText(g, Explanation, Font, new Rectangle(r.X, y, r.Width, r.Bottom - y + h), DeviceTestPaint.Text, Wrapped);
		}
	}
}
