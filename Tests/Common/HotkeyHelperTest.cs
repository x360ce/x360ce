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
