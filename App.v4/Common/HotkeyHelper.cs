using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace x360ce.App
{
	/// <summary>Reads a hotkey written the way Windows shows one, such as Ctrl+Alt+X, and registers it with Windows.</summary>
	/// <remarks>
	/// A hotkey registered this way reaches the window whatever has the focus, which is the point: the
	/// window is minimised while a game runs. The words are the ones every other program's shortcut
	/// list uses, joined with a plus sign, so there is nothing new to learn.
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

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		/// <summary>Reads text such as "Ctrl+Alt+X" into the modifiers and the key Windows expects.</summary>
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
		/// <returns>True when Windows accepted the hotkey.</returns>
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
