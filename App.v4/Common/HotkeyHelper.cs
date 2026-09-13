using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>Records, names and registers a hotkey the way every other program's shortcut field does.</summary>
	/// <remarks>
	/// The field records what is pressed rather than being typed into, shows it as "Ctrl + Alt + X",
	/// treats a modifier on its own as nothing yet, and needs at least one modifier so that a plain
	/// letter cannot be taken from every program on the machine. The hotkey itself goes through
	/// RegisterHotKey, which reaches the window whatever has the focus, needs no hook in any other
	/// process, and is what the established hotkey programs on Windows use.
	/// </remarks>
	public static class HotkeyHelper
	{
		/// <summary>The message Windows sends the window when a registered hotkey is pressed.</summary>
		public const int WM_HOTKEY = 0x0312;

		public const uint ModAlt = 0x0001;
		public const uint ModControl = 0x0002;
		public const uint ModShift = 0x0004;
		public const uint ModWin = 0x0008;
		/// <summary>Holding the keys down fires once, not once per key repeat.</summary>
		const uint ModNoRepeat = 0x4000;

		/// <summary>The words between the keys, the way the Windows shortcut field and the common hotkey programs write them.</summary>
		const string Joiner = " + ";

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

		const int EM_SETCUEBANNER = 0x1501;

		/// <summary>Shows grey guidance text in an empty text box, also while it has the focus.</summary>
		public static void SetCue(TextBox box, string text)
		{
			SendMessage(box.Handle, EM_SETCUEBANNER, new IntPtr(1), text);
		}

		/// <summary>Whether the keys pressed make a whole hotkey: one key that is not a modifier, with at least one modifier held.</summary>
		/// <remarks>
		/// F12 is refused in any combination, because Windows keeps it for the debugger at all times.
		/// </remarks>
		public static bool IsComplete(Keys keyData)
		{
			var key = keyData & Keys.KeyCode;
			if (key == Keys.None || key == Keys.F12 || IsModifierKey(key))
				return false;
			return (keyData & (Keys.Control | Keys.Alt | Keys.Shift)) != 0;
		}

		/// <summary>Writes keys as "Ctrl + Alt + X". A modifier on its own, or nothing, writes an empty string.</summary>
		public static string Format(Keys keyData)
		{
			var key = keyData & Keys.KeyCode;
			if (key == Keys.None || IsModifierKey(key))
				return "";
			var text = new StringBuilder();
			if ((keyData & Keys.Control) != 0)
				text.Append("Ctrl").Append(Joiner);
			if ((keyData & Keys.Alt) != 0)
				text.Append("Alt").Append(Joiner);
			if ((keyData & Keys.Shift) != 0)
				text.Append("Shift").Append(Joiner);
			text.Append(KeyName(key));
			return text.ToString();
		}

		/// <summary>The name of one key: a digit as itself, everything else by its name in the Keys enumeration, which reads back the same.</summary>
		static string KeyName(Keys key)
		{
			if (key >= Keys.D0 && key <= Keys.D9)
				return ((char)('0' + (key - Keys.D0))).ToString();
			return key.ToString();
		}

		/// <summary>Reads text such as "Ctrl + Alt + X" into the modifiers and the key Windows expects.</summary>
		/// <returns>False when the text is empty, names no key, names two keys, or names only modifiers.</returns>
		public static bool TryParse(string text, out uint modifiers, out Keys key)
		{
			modifiers = 0;
			key = Keys.None;
			if (string.IsNullOrWhiteSpace(text))
				return false;
			var parts = text.Split('+');
			for (var i = 0; i < parts.Length; i++)
			{
				var part = parts[i].Trim();
				if (part.Length == 0)
					return false;
				var last = i == parts.Length - 1;
				uint modifier;
				if (TryModifier(part, out modifier))
				{
					// A modifier on its own is not a key that can be pressed, so it is not a hotkey.
					if (last)
						return false;
					modifiers |= modifier;
					continue;
				}
				// Only the last word may be the key. Two keys is not a combination Windows understands.
				if (!last)
					return false;
				// A digit is written as itself, and the enumeration spells it D1.
				if (part.Length == 1 && char.IsDigit(part[0]))
					part = "D" + part;
				// A number would parse as whatever key happens to carry that value.
				if (part.All(char.IsDigit))
					return false;
				Keys parsed;
				if (!Enum.TryParse(part, true, out parsed))
					return false;
				parsed &= Keys.KeyCode;
				if (parsed == Keys.None || IsModifierKey(parsed))
					return false;
				key = parsed;
			}
			return key != Keys.None;
		}

		static bool TryModifier(string word, out uint modifier)
		{
			switch (word.ToLowerInvariant())
			{
				case "ctrl":
				case "control":
					modifier = ModControl;
					return true;
				case "alt":
					modifier = ModAlt;
					return true;
				case "shift":
					modifier = ModShift;
					return true;
				case "win":
				case "windows":
					modifier = ModWin;
					return true;
				default:
					modifier = 0;
					return false;
			}
		}

		static bool IsModifierKey(Keys key)
		{
			switch (key)
			{
				case Keys.ControlKey:
				case Keys.LControlKey:
				case Keys.RControlKey:
				case Keys.ShiftKey:
				case Keys.LShiftKey:
				case Keys.RShiftKey:
				case Keys.Menu:
				case Keys.LMenu:
				case Keys.RMenu:
				case Keys.LWin:
				case Keys.RWin:
					return true;
				default:
					return false;
			}
		}

		/// <summary>Registers the hotkey the text names on the window. Text that names none registers nothing.</summary>
		/// <returns>True when Windows accepted the hotkey. False when the text names none, or another program already holds the combination.</returns>
		public static bool Register(IntPtr handle, int id, string text)
		{
			uint modifiers;
			Keys key;
			if (!TryParse(text, out modifiers, out key))
				return false;
			return RegisterHotKey(handle, id, modifiers | ModNoRepeat, (uint)key);
		}

		/// <summary>Lets go of a hotkey. Asking for one that was never registered is not a fault.</summary>
		public static void Unregister(IntPtr handle, int id)
		{
			UnregisterHotKey(handle, id);
		}
	}
}
