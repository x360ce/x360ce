// @under-test: App.v4/MainForm.cs, App.v4/Common/Options.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The master switch is on unless somebody turns it off, and a tab whose controller is missing
	/// because of the switch must say so rather than report a fault.
	/// </summary>
	[TestClass]
	public class EmulationSwitchTest
	{
		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("A fresh set of options has emulation on, the hotkey off, and the default keys already in the field")]
		public void Switch_is_on_and_hotkey_is_off_by_default()
		{
			var options = new Options();
			Assert.IsTrue(options.XInputEnabled, "Settings files from before the switch existed must load with emulation on.");
			Assert.IsFalse(options.EmulationHotkeyEnabled, "A hotkey nobody turned on would take keys from every other program.");
			Assert.AreEqual(Options.DefaultEmulationHotkey, options.EmulationHotkey, "The field starts with the keys to tick on, so there is nothing to work out first.");
			Assert.IsTrue(options.EmulationHotkeyOverlay, "Without the note a hotkey press is silent inside a full-screen game, where Windows hides the tray balloon.");
			uint modifiers;
			System.Windows.Forms.Keys key;
			Assert.IsTrue(HotkeyHelper.TryParse(options.EmulationHotkey, out modifiers, out key), "The default keys must be ones the program can register.");
		}

		[TestMethod, TestCategory("options"), TestCategory("smoke")]
		[Description("The tab hint names the switch when it is off, and not otherwise")]
		public void Off_switch_is_said_in_the_tab_hint()
		{
			var off = MainForm.ControllerStateHint(1, true, false, false, true, -1, false);
			StringAssert.Contains(off, "switched off");
			StringAssert.Contains(off, "Enable XInput");
			var on = MainForm.ControllerStateHint(1, true, false, false, true, -1, true);
			Assert.IsFalse(on.Contains("switched off"), "With the switch on, a missing controller is a fault and must be reported as one.");
		}
	}
}
