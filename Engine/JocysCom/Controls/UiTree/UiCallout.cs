#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.Controls.UiTree
{
	/// <summary>
	/// Points at a control for a person: a frame around it and a balloon with words beside it,
	/// gone again after a while, or once the person acts. What an assistant uses to say "this one,
	/// here", and what a screenshot for a help page shows. Drawn on a Windows Forms overlay in
	/// screen pixels, so it points at a WPF element as readily as at a control; the WPF overloads
	/// are in UiCallout.WPF.cs, which a Windows Forms program leaves out.
	/// </summary>
	public static partial class UiCallout
	{
		static Frame _frame;
		static Timer _timer;
		static ActWatch _watch;

		/// <summary>How often a callout kept until the person acts looks whether they have, in milliseconds.</summary>
		const int WatchMs = 200;

		/// <summary>The longest a callout kept until the person acts stays, in seconds: the person may have gone on to another program, and the frame is drawn over every program.</summary>
		const int UntilActedLimit = 600;

		/// <summary>How long a callout kept until the person acts stays when their clicks and keys cannot be watched, in seconds.</summary>
		const int UnwatchedSeconds = 60;

		/// <summary>The element being pointed at - a control or a WPF element - or null.</summary>
		public static object Target { get; private set; }

		/// <summary>The screen rectangle the frame is drawn around, before its margin.</summary>
		public static Rectangle Around { get; private set; }

		/// <summary>Where a control is on screen for a person: a tab page by its tab, which is what they click, any other control by itself.</summary>
		public static Rectangle AroundOf(Control target)
		{
			var page = target as TabPage;
			var tabs = page == null ? null : page.Parent as TabControl;
			var index = tabs == null ? -1 : tabs.TabPages.IndexOf(page);
			return index >= 0
				? tabs.RectangleToScreen(tabs.GetTabRect(index))
				: target.RectangleToScreen(target.ClientRectangle);
		}

		/// <summary>Frames the control and shows the words beside it for the given seconds; 0 keeps them until the person acts.</summary>
		public static void Show(Control target, string text, int seconds)
		{
			Show(target, AroundOf(target), text, seconds);
		}

		/// <summary>
		/// Where a part of a control is on screen: an entry on a tool bar, or a row or a cell of a list; null for anything
		/// else, and for a part whose control is out of sight.
		/// </summary>
		public static Rectangle? AroundOfPart(object element)
		{
			var item = element as ToolStripItem;
			if (item != null)
				return item.Owner == null || !item.Owner.Visible || !item.Visible ? (Rectangle?)null : item.Owner.RectangleToScreen(item.Bounds);
			var row = element as DataGridViewRow;
			if (row != null)
				return row.DataGridView == null || !row.DataGridView.Visible || row.Index < 0 ? (Rectangle?)null
					: row.DataGridView.RectangleToScreen(row.DataGridView.GetRowDisplayRectangle(row.Index, true));
			var cell = element as DataGridViewCell;
			if (cell != null)
				return cell.DataGridView == null || !cell.DataGridView.Visible || cell.RowIndex < 0 ? (Rectangle?)null
					: cell.DataGridView.RectangleToScreen(cell.DataGridView.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, true));
			return null;
		}

		/// <summary>Frames a part of a control, as <see cref="AroundOfPart"/> finds it, and shows the words beside it; false when it is not such a part, or not on screen.</summary>
		public static bool ShowPart(object element, string text, int seconds)
		{
			var around = AroundOfPart(element);
			if (!around.HasValue || around.Value.Width <= 0 || around.Value.Height <= 0)
				return false;
			Show(element, around.Value, text, seconds);
			return true;
		}

		/// <summary>
		/// Frames a screen rectangle and shows the words beside it for the given seconds, or, with 0,
		/// until the person acts: presses a mouse button or a key in the program, or the element moves
		/// or goes out of sight, when the frame would point at the wrong place. A new call replaces the
		/// last. The overlay is made afresh each time and disposed when hidden, so it always belongs to
		/// the thread that asked; a callout is rare, so that costs nothing.
		/// </summary>
		static void Show(object target, Rectangle around, string text, int seconds)
		{
			Hide();
			Target = target;
			_frame = new Frame();
			_timer = new Timer();
			Around = around;
			around.Inflate(4, 4);
			var holder = HolderOf(target);
			var window = holder == null ? (Form)null : holder.FindForm();
			var screen = Screen.FromRectangle(around).WorkingArea;
			var bounds = window == null ? screen : Rectangle.Intersect(window.RectangleToScreen(window.ClientRectangle), screen);
			var obstacles = new List<Rectangle>();
			if (window != null)
				AddObstacles(window, bounds, obstacles);
			_frame.Point(around, text, bounds, obstacles);
			Balloon = _frame.Balloon;
			if (seconds > 0)
			{
				_timer.Tick += (s, e) => Hide();
				_timer.Interval = seconds * 1000;
			}
			else
			{
				_watch = new ActWatch();
				var until = Environment.TickCount + (_watch.Watching ? UntilActedLimit : UnwatchedSeconds) * 1000;
				_timer.Tick += (s, e) => { if (_watch.Acted || Moved() || Environment.TickCount - until >= 0) Hide(); };
				_timer.Interval = WatchMs;
			}
			_timer.Start();
		}

		/// <summary>The screen rectangle of the balloon with the words, as it was last placed.</summary>
		public static Rectangle Balloon { get; private set; }

		/// <summary>The control an element is drawn in: itself, the bar an entry is on, or the list a row or cell is in; null for anything else.</summary>
		static Control HolderOf(object target)
		{
			var control = target as Control;
			if (control != null)
				return control;
			var item = target as ToolStripItem;
			if (item != null)
				return item.Owner;
			var row = target as DataGridViewRow;
			if (row != null)
				return row.DataGridView;
			var cell = target as DataGridViewCell;
			return cell == null ? null : cell.DataGridView;
		}

		/// <summary>
		/// Adds where the window shows words or values a balloon must not hide: every visible control but the plain holders
		/// of others, a tab strip's tabs, and a list's headers and rows rather than its empty space; each cut to what its
		/// holders show.
		/// </summary>
		static void AddObstacles(Control parent, Rectangle clip, List<Rectangle> obstacles)
		{
			foreach (Control control in parent.Controls)
			{
				if (!control.Visible)
					continue;
				var shown = Rectangle.Intersect(parent.RectangleToScreen(control.Bounds), clip);
				if (shown.Width <= 0 || shown.Height <= 0)
					continue;
				var tabs = control as TabControl;
				if (tabs != null)
					for (var i = 0; i < tabs.TabCount; i++)
						obstacles.Add(Rectangle.Intersect(tabs.RectangleToScreen(tabs.GetTabRect(i)), shown));
				var grid = control as DataGridView;
				if (grid != null)
				{
					if (grid.ColumnHeadersVisible)
						obstacles.Add(Rectangle.Intersect(grid.RectangleToScreen(new Rectangle(0, 0, grid.ClientSize.Width, grid.ColumnHeadersHeight)), shown));
					for (var i = grid.FirstDisplayedScrollingRowIndex; i >= 0 && i < grid.Rows.Count && grid.Rows[i].Displayed; i++)
						obstacles.Add(Rectangle.Intersect(grid.RectangleToScreen(grid.GetRowDisplayRectangle(i, true)), shown));
				}
				else if (control is Panel || control is TabControl || control is GroupBox || control is ContainerControl || control is SplitContainer)
					AddObstacles(control, shown, obstacles);
				else
					obstacles.Add(shown);
			}
		}

		/// <summary>How much of the obstacles a rectangle covers, in square pixels.</summary>
		static long Covered(Rectangle rectangle, IList<Rectangle> obstacles)
		{
			long covered = 0;
			foreach (var obstacle in obstacles)
			{
				var both = Rectangle.Intersect(rectangle, obstacle);
				if (both.Width > 0 && both.Height > 0)
					covered += (long)both.Width * both.Height;
			}
			return covered;
		}

		/// <summary>
		/// Where a balloon of the size goes beside the framed rectangle: the first of below, above, right and left where it
		/// stays inside the bounds and covers none of the obstacles; otherwise the side where it covers the least, one inside
		/// the bounds before one that is not. Below and above it starts at the frame's left edge, right and left at its top,
		/// moved along that edge only as far as the bounds need.
		/// </summary>
		/// <param name="side">0 below, 1 above, 2 right, 3 left.</param>
		public static Rectangle PlaceBalloon(Rectangle around, Size size, Rectangle bounds, IList<Rectangle> obstacles, out int side)
		{
			var best = Rectangle.Empty;
			var bestCover = long.MaxValue;
			var bestInside = false;
			side = 0;
			for (var i = 0; i < 4; i++)
			{
				var r = new Rectangle(around.Left, around.Bottom + Stem, size.Width, size.Height);
				if (i == 1)
					r.Y = around.Top - Stem - size.Height;
				else if (i >= 2)
					r = new Rectangle(i == 2 ? around.Right + Stem : around.Left - Stem - size.Width, around.Top, size.Width, size.Height);
				if (i < 2)
					r.X = Math.Max(bounds.Left, Math.Min(r.X, bounds.Right - r.Width));
				else
					r.Y = Math.Max(bounds.Top, Math.Min(r.Y, bounds.Bottom - r.Height));
				var inside = bounds.Contains(r);
				var cover = Covered(r, obstacles);
				if (inside && cover == 0)
				{
					side = i;
					return r;
				}
				if ((inside && !bestInside) || (inside == bestInside && cover < bestCover))
				{
					best = r;
					bestCover = cover;
					bestInside = inside;
					side = i;
				}
			}
			return best;
		}

		/// <summary>The gap between the frame and the balloon, which the balloon's pointer crosses.</summary>
		const int Stem = 12;

		/// <summary>True when the element is no longer where the frame is drawn: its window moved, changed size, was minimised or hidden, or the element went out of sight.</summary>
		static bool Moved()
		{
			var control = Target as Control;
			if (control != null)
				return control.IsDisposed || !control.Visible || AroundOf(control) != Around;
			var part = AroundOfPart(Target);
			if (part.HasValue)
				return part.Value != Around;
			// An element this file cannot look at is taken as moved, so the frame goes rather than stays wrong.
			var moved = true;
			MovedWpf(ref moved);
			return moved;
		}

		/// <summary>Written in UiCallout.WPF.cs: whether a WPF element moved. Leaves the answer it is given for anything else.</summary>
		static partial void MovedWpf(ref bool moved);

		const int WM_KEYDOWN = 0x0100;
		const int WM_SYSKEYDOWN = 0x0104;
		const int WM_LBUTTONDOWN = 0x0201;
		const int WM_RBUTTONDOWN = 0x0204;
		const int WM_MBUTTONDOWN = 0x0207;
		const int WM_XBUTTONDOWN = 0x020B;
		const int WM_NCLBUTTONDOWN = 0x00A1;
		const int WM_NCRBUTTONDOWN = 0x00A4;
		const int WM_NCMBUTTONDOWN = 0x00A7;
		const int WM_NCXBUTTONDOWN = 0x00AB;

		/// <summary>Shift, Ctrl and Alt, each side of them, the Windows keys and Print Screen: the keys a screenshot is taken with.</summary>
		static readonly int[] ScreenshotKeys = { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C, 0x2C };

		/// <summary>
		/// True for a window message that is the person acting in the program: a mouse button pressed,
		/// in a window or on its frame, or a key pressed. The keys a screenshot is taken with are not,
		/// because a screenshot is what a callout kept until the person acts waits for.
		/// </summary>
		/// <param name="message">The message number.</param>
		/// <param name="key">The message's wParam: the virtual key, for a key message.</param>
		public static bool IsAct(int message, long key)
		{
			switch (message)
			{
				case WM_LBUTTONDOWN:
				case WM_RBUTTONDOWN:
				case WM_MBUTTONDOWN:
				case WM_XBUTTONDOWN:
				case WM_NCLBUTTONDOWN:
				case WM_NCRBUTTONDOWN:
				case WM_NCMBUTTONDOWN:
				case WM_NCXBUTTONDOWN:
					return true;
				case WM_KEYDOWN:
				case WM_SYSKEYDOWN:
					return Array.IndexOf(ScreenshotKeys, (int)key) < 0;
				default:
					return false;
			}
		}

		public static void Hide()
		{
			if (Target == null)
				return;
			if (_watch != null)
				_watch.Dispose();
			_watch = null;
			_timer.Stop();
			_timer.Dispose();
			_timer = null;
			_frame.Dispose();
			_frame = null;
			Target = null;
		}

		/// <summary>
		/// Watches the messages this thread takes from its queue, which is where the person's clicks
		/// and keys reach the program's windows, Windows Forms and WPF alike. Each message goes on to
		/// the program as it would anyway.
		/// </summary>
		/// <remarks>Its own hook, as in Default.Forms.cs, because Processes\BaseHook is built for .NET Framework only.</remarks>
		sealed class ActWatch : IDisposable
		{
			const int WH_GETMESSAGE = 3;
			const int PM_REMOVE = 1;

			delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll", SetLastError = true)]
			static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool UnhookWindowsHookEx(IntPtr hhk);

			[DllImport("user32.dll")]
			static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

			[DllImport("kernel32.dll")]
			static extern int GetCurrentThreadId();

			/// <summary>Kept in a field: Windows calls it for as long as the hook is set.</summary>
			readonly HookProc _proc;
			IntPtr _hook;

			/// <summary>True once the person pressed a mouse button or a key in the program.</summary>
			public bool Acted;

			/// <summary>False when Windows refused the hook, so the person's clicks and keys go unseen.</summary>
			public bool Watching { get { return _hook != IntPtr.Zero; } }

			public ActWatch()
			{
				_proc = Watch;
				_hook = SetWindowsHookEx(WH_GETMESSAGE, _proc, IntPtr.Zero, GetCurrentThreadId());
			}

			IntPtr Watch(int code, IntPtr wParam, IntPtr lParam)
			{
				// A message only looked at is still to come; one taken off the queue is what the program acts on.
				if (code >= 0 && (wParam.ToInt64() & PM_REMOVE) != 0 && IsAct(Marshal.ReadInt32(lParam, IntPtr.Size), Marshal.ReadIntPtr(lParam, IntPtr.Size * 2).ToInt64()))
					Acted = true;
				return CallNextHookEx(_hook, code, wParam, lParam);
			}

			public void Dispose()
			{
				if (_hook != IntPtr.Zero)
					UnhookWindowsHookEx(_hook);
				_hook = IntPtr.Zero;
			}
		}

		/// <summary>Draws the frame and the balloon on an overlay, which is what keeps it out of the way.</summary>
		sealed class Frame : OverlayForm
		{
			const int Pad = 8;
			static readonly Color Ink = Color.OrangeRed;
			static readonly Color Paper = Color.FromArgb(255, 255, 225);

			Rectangle _around;
			Rectangle _balloon;
			Point[] _stem;
			string _text;

			/// <summary>The balloon on the screen, as <see cref="PlaceBalloon"/> placed it.</summary>
			public Rectangle Balloon { get; private set; }

			/// <summary>Lays the frame around the screen rectangle and the balloon on the side <see cref="PlaceBalloon"/> chooses, with its pointer to the frame.</summary>
			/// <param name="bounds">The window's area on the screen, which the balloon stays inside where it can.</param>
			/// <param name="obstacles">The words and values on screen the balloon should not hide.</param>
			public void Point(Rectangle around, string text, Rectangle bounds, IList<Rectangle> obstacles)
			{
				_text = string.IsNullOrWhiteSpace(text) ? "Here" : text.Trim();
				var size = TextRenderer.MeasureText(_text, Font, new Size(320, 0), TextFormatFlags.WordBreak);
				int side;
				var balloon = PlaceBalloon(around, new Size(size.Width + Pad * 2, size.Height + Pad * 2), bounds, obstacles, out side);
				Balloon = balloon;
				var all = Rectangle.Union(around, balloon);
				all.Inflate(2, 2);
				Bounds = all;
				_around = new Rectangle(around.X - all.X, around.Y - all.Y, around.Width, around.Height);
				_balloon = new Rectangle(balloon.X - all.X, balloon.Y - all.Y, balloon.Width, balloon.Height);
				if (side < 2)
				{
					// Below or above: from the balloon's edge to the frame, near the frame's left end.
					var tipX = Math.Min(Math.Max(_around.Left + Math.Min(_around.Width / 2, 24), _balloon.Left + 12), _balloon.Right - 12);
					var edge = side == 0 ? _balloon.Top : _balloon.Bottom;
					var tip = side == 0 ? _around.Bottom : _around.Top;
					_stem = new[] { new Point(tipX - 6, edge), new Point(tipX, tip), new Point(tipX + 6, edge) };
				}
				else
				{
					// Right or left: from the balloon's edge to the frame, at the middle of what they share.
					var top = Math.Max(_around.Top, _balloon.Top + 8);
					var bottom = Math.Min(_around.Bottom, _balloon.Bottom - 8);
					var tipY = bottom >= top ? (top + bottom) / 2 : Math.Min(Math.Max(_around.Top + _around.Height / 2, _balloon.Top + 8), _balloon.Bottom - 8);
					var edge = side == 2 ? _balloon.Left : _balloon.Right;
					var tip = side == 2 ? _around.Right : _around.Left;
					_stem = new[] { new Point(edge, tipY - 6), new Point(tip, tipY), new Point(edge, tipY + 6) };
				}
				Show();
				Invalidate();
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
