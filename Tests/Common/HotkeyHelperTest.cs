// @under-test: App.v4/Common/HotkeyHelper.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System.Linq;
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

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("Emulation buttons are written and read by the names the mapping page gives them, in a fixed order")]
		public void Emulation_buttons_read_back_as_written()
		{
			var backStart = GamepadButtonFlags.Back | GamepadButtonFlags.Start;
			Assert.AreEqual(Options.DefaultEmulationButtons, HotkeyHelper.FormatButtons(backStart));
			Assert.AreEqual(backStart, HotkeyHelper.ParseButtons(Options.DefaultEmulationButtons));
			Assert.AreEqual(backStart, HotkeyHelper.ParseButtons(" start+BACK "), "Case, spaces and order change nothing.");
			var all = GamepadButtonFlags.Guide;
			foreach (GamepadButtonFlags flag in System.Enum.GetValues(typeof(GamepadButtonFlags)))
				all |= flag;
			Assert.AreEqual(all, HotkeyHelper.ParseButtons(HotkeyHelper.FormatButtons(all)), "A button is lost between writing and reading.");
			foreach (var text in new[] { "", null, "Back + Nothing", "Back +", "Ctrl + Alt + X" })
				Assert.AreEqual(GamepadButtonFlags.None, HotkeyHelper.ParseButtons(text), "'" + (text ?? "(null)") + "' was read as buttons.");
			Assert.IsTrue(HotkeyHelper.IsCompleteButtons(backStart));
			Assert.IsFalse(HotkeyHelper.IsCompleteButtons(GamepadButtonFlags.Guide), "One button on its own would flip emulation at every press in a game.");
		}

		[TestMethod, TestCategory("options"), TestCategory("critical")]
		[Description("Emulation buttons flip emulation once each time they are held together on one controller")]
		public void Emulation_buttons_fire_once_per_press()
		{
			var backStart = GamepadButtonFlags.Back | GamepadButtonFlags.Start;
			var states = new State[4];
			var held = false;
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "Nothing held fired.");
			states[1].Gamepad.Buttons = GamepadButtonFlags.Back;
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "Half the combination fired.");
			states[2].Gamepad.Buttons = GamepadButtonFlags.Start;
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "Halves on two controllers fired.");
			states[1].Gamepad.Buttons = backStart | GamepadButtonFlags.A;
			Assert.IsTrue(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "The combination, with another button too, did not fire.");
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "Held on, it fired again.");
			states[1].Gamepad.Buttons = GamepadButtonFlags.Start;
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(backStart, states, ref held));
			states[1].Gamepad.Buttons = backStart;
			Assert.IsTrue(HotkeyHelper.ButtonsPressed(backStart, states, ref held), "Pressed again after letting go, it did not fire.");
			held = false;
			Assert.IsFalse(HotkeyHelper.ButtonsPressed(GamepadButtonFlags.Back, states, ref held), "One button on its own fired.");
			Assert.AreEqual(backStart | GamepadButtonFlags.Start, HotkeyHelper.HeldButtons(states));
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("The emulation buttons field records the buttons held together once all are let go of, and ticks its box; one button records nothing")]
		public void Emulation_buttons_field_records_what_is_held()
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new x360ce.App.Controls.OptionsUserControl())
				{
					page.CreateControl();
					var field = page.Controls.Find("EmulationButtonsTextBox", true).Single();
					var box = (CheckBox)page.Controls.Find("EmulationButtonsCheckBox", true).Single();
					field.Text = "";
					page.RecordButtons(GamepadButtonFlags.A);
					page.RecordButtons(GamepadButtonFlags.None);
					Assert.AreEqual("", field.Text, "One button on its own was recorded.");
					Assert.IsFalse(box.Checked, "One button on its own ticked the box.");
					page.RecordButtons(GamepadButtonFlags.Start);
					page.RecordButtons(GamepadButtonFlags.Start | GamepadButtonFlags.Back);
					Assert.AreEqual("", field.Text, "The field changed before the buttons were let go of.");
					page.RecordButtons(GamepadButtonFlags.Back);
					page.RecordButtons(GamepadButtonFlags.None);
					Assert.AreEqual("Back + Start", field.Text);
					Assert.IsTrue(box.Checked, "Recording did not tick the box.");
				}
			});
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
