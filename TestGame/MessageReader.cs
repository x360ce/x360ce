using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace x360ce.TestGame
{
	/// <summary>When the message being handled was made, on the recorder's clock.</summary>
	/// <remarks>
	/// Windows stamps every message with the time it was made, so a window busy drawing, or answering the AI door, does
	/// not shift the time an event says, and the gap of a double jump is the gap the keys had.
	/// </remarks>
	static class MessageClock
	{
		[DllImport("user32.dll")]
		static extern int GetMessageTime();

		/// <param name="now">The recorder's clock now.</param>
		public static long Of(long now)
		{
			var age = unchecked(Environment.TickCount - GetMessageTime());
			return age < 0 || age > 10000 ? now : now - age;
		}
	}

	/// <summary>
	/// The keyboard and mouse as window messages bring them, the way a program without its own input code reads them.
	/// Windows sends them to the window in front only: the keys to the focused control, the mouse to the control under the
	/// pointer. Read here before any control sees them, and passed on unchanged.
	/// </summary>
	public sealed class MessageReader : IMessageFilter
	{
		const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
		const int WM_MOUSEMOVE = 0x200, WM_MOUSEWHEEL = 0x20A, WM_MOUSEHWHEEL = 0x20E;
		const int WM_XBUTTONDOWN = 0x20B, WM_XBUTTONUP = 0x20C, WM_XBUTTONDBLCLK = 0x20D;
		/// <summary>How long pointer moves are watched before the position they reached is one event.</summary>
		const int MoveMs = 100;

		readonly Action<InputEvent> _add;
		readonly Func<long> _now;
		System.Drawing.Point _moveTo;
		System.Drawing.Point _shown = new System.Drawing.Point(int.MinValue, int.MinValue);
		bool _moved;
		long _moveSince;

		public MessageReader(Action<InputEvent> add, Func<long> now)
		{
			_add = add;
			_now = now;
		}

		public bool PreFilterMessage(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_KEYDOWN:
				case WM_SYSKEYDOWN:
				case WM_KEYUP:
				case WM_SYSKEYUP:
					var vkey = (ushort)m.WParam.ToInt64();
					var down = m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN;
					var scan = (ushort)((m.LParam.ToInt64() >> 16) & 0xFF);
					var packet = vkey == (ushort)Keys.Packet;
					Add(new InputEvent
					{
						Source = "Keyboard",
						Kind = down ? "Down" : "Up",
						Name = packet ? "Packet" : ((Keys)vkey).ToString(),
						Text = down ? RawInput.KeyText(vkey, scan) : null,
					});
					break;
				case WM_MOUSEMOVE:
					// Windows sends a move again when nothing moved, such as when a window under the pointer changes.
					var position = Cursor.Position;
					if (position == _shown && !_moved)
						break;
					if (!_moved)
						_moveSince = MessageClock.Of(_now());
					_moved = true;
					_moveTo = position;
					break;
				case 0x201: case 0x203: Button("LeftButton", true); break;
				case 0x202: Button("LeftButton", false); break;
				case 0x204: case 0x206: Button("RightButton", true); break;
				case 0x205: Button("RightButton", false); break;
				case 0x207: case 0x209: Button("MiddleButton", true); break;
				case 0x208: Button("MiddleButton", false); break;
				case WM_XBUTTONDOWN:
				case WM_XBUTTONDBLCLK:
				case WM_XBUTTONUP:
					var which = (m.WParam.ToInt64() >> 16) & 0xFFFF;
					Button(which == 1 ? "XButton1" : "XButton2", m.Msg != WM_XBUTTONUP);
					break;
				case WM_MOUSEWHEEL:
				case WM_MOUSEHWHEEL:
					Add(new InputEvent
					{
						Source = "Mouse",
						Kind = "Wheel",
						Name = m.Msg == WM_MOUSEWHEEL ? "Wheel" : "HorizontalWheel",
						Value = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF),
					});
					break;
			}
			return false;
		}

		void Button(string name, bool down)
		{
			Add(new InputEvent { Source = "Mouse", Kind = down ? "Down" : "Up", Name = name });
		}

		/// <summary>Called every tick: the position the pointer moves reached, once they have gone on for a tenth of a second.</summary>
		public void Flush(bool force = false)
		{
			if (!_moved || (!force && _now() - _moveSince < MoveMs))
				return;
			_moved = false;
			_shown = _moveTo;
			Keep(new InputEvent { Source = "Mouse", Kind = "Move", Name = "Move", X = _moveTo.X, Y = _moveTo.Y, Absolute = true, Ms = _moveSince });
		}

		void Add(InputEvent e)
		{
			Flush(true);
			e.Ms = MessageClock.Of(_now());
			Keep(e);
		}

		void Keep(InputEvent e)
		{
			e.Api = "Messages";
			// Windows sends these to the window in front only.
			e.InFront = true;
			_add(e);
		}
	}
}
