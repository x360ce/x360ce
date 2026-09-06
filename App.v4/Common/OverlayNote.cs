using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>A short note drawn over whatever is on the screen, gone again after a moment.</summary>
	/// <remarks>
	/// What a hotkey press answers with while the window is out of sight. The tray balloon it replaces
	/// is a Windows notification, and Windows silences those by itself while a full-screen game runs,
	/// so the answer never arrived where the hotkey is used. The note is a plain topmost window, the
	/// way the system's own volume display is: it shows over windowed, borderless and
	/// fullscreen-optimised games, never touches the game itself, and is not seen over exclusive
	/// fullscreen. Interface thread only; nothing here is reached from the engine.
	/// </remarks>
	public static class OverlayNote
	{
		public const int DefaultSeconds = 2;
		const int Pad = 14;
		const int Mark = 12;

		static Note _note;
		static Timer _timer;

		/// <summary>The note on screen, or null.</summary>
		public static Form Window { get { return _note; } }

		/// <summary>The words on the note, or null when there is none.</summary>
		public static string Text { get { return _note == null ? null : _note.Words; } }

		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		/// <summary>Shows the note for the given seconds on the monitor holding the foreground window. A new call replaces the last.</summary>
		/// <param name="text">The words.</param>
		/// <param name="mark">The colour of the mark beside them: the colours the controller tabs use, so on and off read the same everywhere.</param>
		/// <param name="seconds">How long it stays.</param>
		public static void Show(string text, Color mark, int seconds = DefaultSeconds)
		{
			Hide();
			_note = new Note();
			_note.Place(ScreenOfForeground(), text, mark);
			_timer = new Timer();
			_timer.Interval = Math.Max(1, seconds) * 1000;
			_timer.Tick += (s, e) => Hide();
			_timer.Start();
		}

		public static void Hide()
		{
			if (_note == null)
				return;
			_timer.Stop();
			_timer.Dispose();
			_timer = null;
			_note.Dispose();
			_note = null;
		}

		/// <summary>The monitor a person is looking at: the one holding the foreground window, or the main one when nothing is in front.</summary>
		static Rectangle ScreenOfForeground()
		{
			var handle = GetForegroundWindow();
			var screen = handle == IntPtr.Zero ? Screen.PrimaryScreen : Screen.FromHandle(handle);
			return screen.Bounds;
		}

		/// <summary>Where the note goes: centred across the monitor, a tenth of the way down, sized to its words plus the mark and the padding.</summary>
		public static Rectangle Place(Rectangle screen, Size words)
		{
			var width = Pad + Mark + Pad + words.Width + Pad;
			var height = Math.Max(words.Height, Mark) + Pad * 2;
			return new Rectangle(screen.Left + (screen.Width - width) / 2, screen.Top + screen.Height / 10, width, height);
		}

		/// <summary>The note itself: a dark rounded panel, a coloured mark, white words.</summary>
		sealed class Note : OverlayForm
		{
			static readonly Color Panel = Color.FromArgb(28, 28, 30);
			static readonly Font Face = new Font(SystemFonts.MessageBoxFont.FontFamily, 12f, FontStyle.Bold);

			public string Words { get; private set; }
			Color _mark;

			public Note()
			{
				Font = Face;
			}

			public void Place(Rectangle screen, string text, Color mark)
			{
				Words = string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
				_mark = mark;
				var size = TextRenderer.MeasureText(Words, Font);
				Bounds = OverlayNote.Place(screen, size);
				Show();
				Invalidate();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				var g = e.Graphics;
				// The corners are cut by the see-through colour, so their edge must not be blended with it.
				g.SmoothingMode = SmoothingMode.None;
				using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 10))
				using (var panel = new SolidBrush(Panel))
					g.FillPath(panel, path);
				g.SmoothingMode = SmoothingMode.AntiAlias;
				using (var mark = new SolidBrush(_mark))
					g.FillEllipse(mark, Pad, (Height - Mark) / 2, Mark, Mark);
				var textBox = new Rectangle(Pad + Mark + Pad, 0, Width - (Pad + Mark + Pad) - Pad, Height);
				TextRenderer.DrawText(g, Words, Font, textBox, Color.White,
					TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
			}

			static GraphicsPath Rounded(Rectangle r, int radius)
			{
				var d = radius * 2;
				var path = new GraphicsPath();
				path.AddArc(r.Left, r.Top, d, d, 180, 90);
				path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
				path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
				path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
				path.CloseFigure();
				return path;
			}
		}
	}
}
