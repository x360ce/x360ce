// @under-test: App.v4/MainForm.cs, App.v4/Common/Options.cs
// @area: options   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

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

		[TestMethod, TestCategory("options")]
		[Description("A tab with no emulated controller by choice names the switch that is off, as the engine decides it: the master switch, the game's emulation or the tab's Enable")]
		public void Tab_hint_names_the_switch_that_is_off()
		{
			var o = SettingsManager.Options;
			var master = o.XInputEnabled;
			try
			{
				var game = new UserGame { FileName = "tab-off.exe", EmulationType = (int)EmulationType.Virtual, EnableMask = (int)MapToMask.Controller1 };
				o.XInputEnabled = true;
				Assert.IsNull(MainForm.EmulationOffReason(game, 1), "A switched-on tab is said to be off.");
				var tabOff = MainForm.EmulationOffReason(game, 2);
				StringAssert.Contains(tabOff, "Enable", "A tab switched off for the game is blamed on something else.");
				StringAssert.Contains(MainForm.ControllerStateHint(2, true, false, false, true, -1, false, offReason: tabOff), "Press Enable");
				game.EmulationType = (int)EmulationType.Library;
				StringAssert.Contains(MainForm.EmulationOffReason(game, 1), "Library emulation");
				o.XInputEnabled = false;
				StringAssert.Contains(MainForm.EmulationOffReason(game, 1), "Enable XInput");
			}
			finally
			{
				o.XInputEnabled = master;
			}
		}

		[TestMethod, TestCategory("options")]
		[Description("A tab whose mapped devices are all unticked says nothing is read, not that nothing is mapped")]
		public void Tab_hint_says_when_every_device_is_unticked()
		{
			StringAssert.Contains(MainForm.ControllerStateHint(1, false, false, false, true, unticked: true), "unticked");
			StringAssert.Contains(MainForm.ControllerStateHint(1, false, true, true, true, unticked: true), "unticked");
			StringAssert.Contains(MainForm.ControllerStateHint(1, false, false, false, true), "No mapped device");
		}
	}
}
