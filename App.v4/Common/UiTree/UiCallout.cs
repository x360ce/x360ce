using System;
using System.Drawing;
using System.Windows.Forms;

namespace x360ce.App.UiTree
{
	/// <summary>
	/// Points at a control for a person: a frame around it and a balloon with words beside it,
	/// gone again after a while. What an assistant uses to say "this one, here".
	/// </summary>
	public static class UiCallout
	{
		static Frame _frame;
		static Timer _timer;

		/// <summary>The control being pointed at, or null.</summary>
		public static Control Target { get; private set; }

		/// <summary>Frames the control and shows the words beside it for the given seconds. A new call replaces the last.</summary>
		public static void Show(Control target, string text, int seconds)
		{
			Hide();
			Target = target;
			if (_frame == null)
				_frame = new Frame();
			if (_timer == null)
			{
				_timer = new Timer();
				_timer.Tick += (s, e) => Hide();
			}
			var around = target.RectangleToScreen(target.ClientRectangle);
			around.Inflate(4, 4);
			_frame.Point(around, text);
			_timer.Interval = seconds * 1000;
			_timer.Start();
		}

		public static void Hide()
		{
			if (Target == null)
				return;
			_timer.Stop();
			_frame.Hide();
			Target = null;
		}

		/// <summary>
		/// A see-through window that draws only the frame and the balloon, never takes focus, and
		/// lets clicks through. Drawn by hand, because the built-in balloon tip will not show for a
		/// window that is not the active one, and an assistant's window rarely is.
		/// </summary>
		sealed class Frame : Form
		{
			const int Stem = 12;
			const int Pad = 8;
			static readonly Color Ink = Color.OrangeRed;
			static readonly Color Paper = Color.FromArgb(255, 255, 225);

			Rectangle _around;
			Rectangle _balloon;
			Point[] _stem;
			string _text;

			public Frame()
			{
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				TopMost = true;
				StartPosition = FormStartPosition.Manual;
				BackColor = Color.Magenta;
				TransparencyKey = Color.Magenta;
				Font = SystemFonts.MessageBoxFont;
			}

			/// <summary>Lays the frame around the screen rectangle and the balloon below it, or above when there is no room below.</summary>
			public void Point(Rectangle around, string text)
			{
				_text = string.IsNullOrWhiteSpace(text) ? "Here" : text.Trim();
				var size = TextRenderer.MeasureText(_text, Font, new Size(320, 0), TextFormatFlags.WordBreak);
				var balloon = new Rectangle(around.Left, around.Bottom + Stem, size.Width + Pad * 2, size.Height + Pad * 2);
				var screen = Screen.FromRectangle(around).WorkingArea;
				if (balloon.Right > screen.Right)
					balloon.X = Math.Max(screen.Left, screen.Right - balloon.Width);
				var below = balloon.Bottom <= screen.Bottom;
				if (!below)
					balloon.Y = around.Top - Stem - balloon.Height;
				var bounds = Rectangle.Union(around, balloon);
				bounds.Inflate(2, 2);
				Bounds = bounds;
				_around = new Rectangle(around.X - bounds.X, around.Y - bounds.Y, around.Width, around.Height);
				_balloon = new Rectangle(balloon.X - bounds.X, balloon.Y - bounds.Y, balloon.Width, balloon.Height);
				var tipX = Math.Min(Math.Max(_around.Left + Math.Min(_around.Width / 2, 24), _balloon.Left + 12), _balloon.Right - 12);
				_stem = below
					? new[] { new Point(tipX - 6, _balloon.Top), new Point(tipX, _around.Bottom), new Point(tipX + 6, _balloon.Top) }
					: new[] { new Point(tipX - 6, _balloon.Bottom), new Point(tipX, _around.Top), new Point(tipX + 6, _balloon.Bottom) };
				Show();
				Invalidate();
			}

			protected override bool ShowWithoutActivation { get { return true; } }

			protected override CreateParams CreateParams
			{
				get
				{
					const int WS_EX_TRANSPARENT = 0x20;
					const int WS_EX_TOOLWINDOW = 0x80;
					const int WS_EX_NOACTIVATE = 0x08000000;
					var p = base.CreateParams;
					p.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
					return p;
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				if (_text == null)
					return;
				var g = e.Graphics;
				using (var pen = new Pen(Ink, 3))
				using (var paper = new SolidBrush(Paper))
				using (var ink = new SolidBrush(Ink))
				{
					g.DrawRectangle(pen, _around.X + 1, _around.Y + 1, _around.Width - 3, _around.Height - 3);
					g.FillRectangle(paper, _balloon);
					g.DrawRectangle(pen, _balloon);
					g.FillPolygon(ink, _stem);
				}
				var textBox = _balloon;
				textBox.Inflate(-Pad, -Pad);
				TextRenderer.DrawText(g, _text, Font, textBox, Color.Black, TextFormatFlags.WordBreak);
			}
		}
	}
}
