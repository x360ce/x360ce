// @under-test: App.v4/Common/HotkeyHelper.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The hotkey is typed as text on the Options page and read on every keystroke, so what the words
	/// mean, and which half-typed states register nothing, are pinned here.
	/// </summary>
	[TestClass]
	public class HotkeyHelperTest
	{
		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("Modifiers and the key are read from the words Windows uses for them")]
		public void Words_parse_to_modifiers_and_key()
		{
			AssertParses("Ctrl+Alt+X", HotkeyHelper.ModControl | HotkeyHelper.ModAlt, Keys.X);
			AssertParses("shift+f12", HotkeyHelper.ModShift, Keys.F12);
			AssertParses("Win+1", HotkeyHelper.ModWin, Keys.D1);
			AssertParses("Control + Alt + Shift + Delete", HotkeyHelper.ModControl | HotkeyHelper.ModAlt | HotkeyHelper.ModShift, Keys.Delete);
			AssertParses("F5", 0, Keys.F5);
			AssertParses(" Ctrl+Space ", HotkeyHelper.ModControl, Keys.Space);
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("Empty, half-typed and modifier-only text names no hotkey rather than a wrong one")]
		public void Empty_or_incomplete_text_is_no_hotkey()
		{
			AssertRejects(null);
			AssertRejects("");
			AssertRejects("   ");
			AssertRejects("Ctrl");
			AssertRejects("Ctrl+Alt");
			AssertRejects("Ctrl+");
			AssertRejects("+X");
			AssertRejects("Ctrl+ControlKey");
			AssertRejects("Ctrl+X+Y");
			AssertRejects("Ctrl+Nope");
			// A number parses as whatever key carries that value, which nobody typed.
			AssertRejects("123");
			AssertRejects("Ctrl+None");
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("Pressed keys are written the way the Windows shortcut field writes them, and read back the same")]
		public void Pressed_keys_are_written_and_read_back()
		{
			Assert.AreEqual("Ctrl + Alt + X", HotkeyHelper.Format(Keys.Control | Keys.Alt | Keys.X));
			Assert.AreEqual("Shift + F5", HotkeyHelper.Format(Keys.Shift | Keys.F5));
			Assert.AreEqual("Ctrl + 1", HotkeyHelper.Format(Keys.Control | Keys.D1));
			Assert.AreEqual("Ctrl + Alt + Shift + Delete", HotkeyHelper.Format(Keys.Control | Keys.Alt | Keys.Shift | Keys.Delete));
			AssertParses(HotkeyHelper.Format(Keys.Control | Keys.Alt | Keys.X), HotkeyHelper.ModControl | HotkeyHelper.ModAlt, Keys.X);
			AssertParses(HotkeyHelper.Format(Keys.Control | Keys.D1), HotkeyHelper.ModControl, Keys.D1);
			// A modifier held on its own is nothing yet, and must not be written as if it were something.
			Assert.AreEqual("", HotkeyHelper.Format(Keys.Control | Keys.ControlKey));
			Assert.AreEqual("", HotkeyHelper.Format(Keys.None));
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("A hotkey is complete only with a modifier and a key that is not one, and never with F12")]
		public void Only_a_modified_key_is_a_whole_hotkey()
		{
			Assert.IsTrue(HotkeyHelper.IsComplete(Keys.Control | Keys.Alt | Keys.X));
			Assert.IsTrue(HotkeyHelper.IsComplete(Keys.Shift | Keys.F5));
			// A plain letter would be taken from every program on the machine.
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.X));
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.F5));
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.Control | Keys.ControlKey));
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.Alt | Keys.Menu));
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.None));
			// Windows keeps F12 for the debugger at all times.
			Assert.IsFalse(HotkeyHelper.IsComplete(Keys.Control | Keys.F12));
		}

		static void AssertParses(string text, uint modifiers, Keys key)
		{
			uint m;
			Keys k;
			Assert.IsTrue(HotkeyHelper.TryParse(text, out m, out k), "'" + text + "' was not read as a hotkey.");
			Assert.AreEqual(modifiers, m, "'" + text + "' modifiers");
			Assert.AreEqual(key, k, "'" + text + "' key");
		}

		static void AssertRejects(string text)
		{
			uint m;
			Keys k;
			Assert.IsFalse(HotkeyHelper.TryParse(text, out m, out k), "'" + text + "' was read as a hotkey.");
			Assert.AreEqual(Keys.None, k, "'" + text + "' left a key behind.");
		}
	}
}
