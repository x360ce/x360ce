using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace x360ce.TestGame
{
	/// <summary>One thing the game received, or one action it made of what it received, such as a double jump.</summary>
	public sealed class InputEvent
	{
		/// <summary>Numbered from 1 in the order received, so a reader asks for everything after the last one it saw.</summary>
		public int Seq;
		/// <summary>Milliseconds since the game started, from a clock that does not jump.</summary>
		public long Ms;
		public DateTime Time;
		/// <summary>
		/// Keyboard, Mouse, XInput 1 to XInput 4, a DirectInput device by its name, Window, or Action for what the game made
		/// of its input.
		/// </summary>
		public string Source;
		/// <summary>How the game read it: RawInput, Messages, DirectInput or XInput. Each has its own limits, such as Messages
		/// reaching only the window in front.</summary>
		public string Api;
		/// <summary>Whether the window was in front when it arrived.</summary>
		public bool InFront;
		/// <summary>Down, Repeat, Up, Move, Wheel, Axis, InFront, Behind, Jump or Double jump.</summary>
		public string Kind;
		/// <summary>The key, button, axis or action: Space, Left, LeftButton, A, LX.</summary>
		public string Name;
		/// <summary>What a key types with the keyboard layout in use, when it types something.</summary>
		public string Text;
		/// <summary>A mouse move: how far, or where to on the 0 to 65535 screen scale when Absolute.</summary>
		public int X, Y;
		public bool Absolute;
		/// <summary>A wheel's turn, an axis' raw value, or a double jump's gap in milliseconds.</summary>
		public int Value;
		/// <summary>True when no device sent it: a program did, such as AutoHotkey, through SendInput.</summary>
		public bool FromProgram;
		/// <summary>One sentence for a person: "Double jump, 118 ms after the jump".</summary>
		public string Note;

		public Dictionary<string, object> ToDictionary()
		{
			var d = new Dictionary<string, object>
			{
				{ "Seq", Seq },
				{ "Ms", Ms },
				{ "Time", Time.ToString("HH:mm:ss.fff") },
				{ "Source", Source },
				{ "Kind", Kind },
				{ "Name", Name },
			};
			if (!string.IsNullOrEmpty(Api))
			{
				d["Api"] = Api;
				d["InFront"] = InFront;
			}
			if (!string.IsNullOrEmpty(Text))
				d["Text"] = Text;
			if (Kind == "Move")
			{
				d["X"] = X;
				d["Y"] = Y;
				d["Absolute"] = Absolute;
			}
			if (Kind == "Wheel" || Kind == "Axis" || Kind == "Double jump")
				d["Value"] = Value;
			if (Api == "RawInput")
				d["FromProgram"] = FromProgram;
			if (!string.IsNullOrEmpty(Note))
				d["Note"] = Note;
			return d;
		}
	}

	/// <summary>
	/// Keeps what the game received, in order, and makes the actions a game makes of it: a jump, and a double jump when
	/// the jump is pressed again soon enough. Everything runs on the window's thread, so nothing needs a lock.
	/// </summary>
	public sealed class InputRecorder
	{
		/// <summary>How many events are kept; the oldest go first.</summary>
		public const int Capacity = 2000;

		readonly List<InputEvent> _events = new List<InputEvent>();
		readonly Stopwatch _clock = Stopwatch.StartNew();
		readonly HashSet<string> _held = new HashSet<string>();
		int _next = 1;
		long _jumpMs;
		bool _canDoubleJump;

		/// <summary>The key that jumps, by its name in <see cref="System.Windows.Forms.Keys"/>. Button A on a controller jumps too.</summary>
		public string JumpKey = "Space";

		/// <summary>
		/// Which way of reading the keyboard the jump follows: RawInput, Messages or DirectInput. Every way records every key,
		/// so only one may count, or one press would jump once for each.
		/// </summary>
		public string JumpApi = "RawInput";

		/// <summary>How soon after a jump a second press makes a double jump, in milliseconds.</summary>
		public int DoubleJumpMs = 250;

		/// <summary>The newest event of any kind, and the newest action.</summary>
		public InputEvent Last, LastAction;

		/// <summary>Raised for each event kept, actions included, after it is numbered.</summary>
		public event Action<InputEvent> Added;

		/// <summary>Milliseconds since the game started.</summary>
		public long Now { get { return _clock.ElapsedMilliseconds; } }

		/// <summary>The keys and buttons held now, as Source/Name.</summary>
		public IEnumerable<string> Held { get { return _held; } }

		/// <summary>The sequence number the next event will have.</summary>
		public int Next { get { return _next; } }

		/// <summary>
		/// Keeps an event and acts on it. A press of something already held is the keyboard's own repeat, which a game
		/// does not take as a new press, so it is kept as Repeat and makes no jump.
		/// </summary>
		public InputEvent Add(InputEvent e)
		{
			var key = e.Api + "/" + e.Source + "/" + e.Name;
			if (e.Kind == "Down" && !_held.Add(key))
				e.Kind = "Repeat";
			else if (e.Kind == "Up")
				_held.Remove(key);
			Keep(e);
			if (e.Kind == "Down" && IsJump(e))
				Jump(e);
			return e;
		}

		/// <summary>Lets go of everything held: the window lost the keyboard, and the releases will not arrive.</summary>
		public void ReleaseAll()
		{
			_held.Clear();
		}

		/// <summary>A game acts only on what arrives while it is in front: the jump key read the chosen way, or button A through XInput.</summary>
		bool IsJump(InputEvent e)
		{
			if (!e.InFront)
				return false;
			if (e.Source == "Keyboard")
				return e.Api == JumpApi && string.Equals(e.Name, JumpKey, StringComparison.OrdinalIgnoreCase);
			return e.Api == "XInput" && e.Name == "A";
		}

		/// <summary>The first press jumps; a second within the window, measured from the first, is a double jump.</summary>
		void Jump(InputEvent press)
		{
			var gap = press.Ms - _jumpMs;
			var action = new InputEvent { Source = "Action", Ms = press.Ms };
			if (_canDoubleJump && gap <= DoubleJumpMs)
			{
				action.Kind = action.Name = "Double jump";
				action.Value = (int)gap;
				action.Note = "Double jump, " + gap + " ms after the jump, by " + press.Source + " " + press.Name + ".";
				_canDoubleJump = false;
			}
			else
			{
				action.Kind = action.Name = "Jump";
				action.Note = "Jump, by " + press.Source + " " + press.Name + ".";
				_jumpMs = press.Ms;
				_canDoubleJump = true;
			}
			Keep(action);
			LastAction = action;
		}

		void Keep(InputEvent e)
		{
			e.Seq = _next++;
			if (e.Ms == 0)
				e.Ms = Now;
			e.Time = DateTime.Now;
			_events.Add(e);
			if (_events.Count > Capacity)
				_events.RemoveRange(0, _events.Count - Capacity);
			Last = e;
			Added?.Invoke(e);
		}

		/// <summary>Events after a sequence number, oldest first, at most <paramref name="count"/> of them.</summary>
		public IList<InputEvent> Since(int seq, int count)
		{
			return _events.Where(x => x.Seq > seq).Take(Math.Max(0, count)).ToList();
		}

		/// <summary>Forgets every event and the last action; numbering goes on, so an old number never names a new event.</summary>
		public void Clear()
		{
			_events.Clear();
			Last = LastAction = null;
			_canDoubleJump = false;
		}
	}
}
