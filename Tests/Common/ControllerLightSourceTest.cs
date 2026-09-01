// @under-test: App.v4/MainForm.cs, App.v4/Controls/PadControl.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The light on a controller tab and the panel behind it describe the same fact.
	/// </summary>
	/// <remarks>
	/// They did not. The panel drew itself from the controller Windows hands back through XInput, and
	/// showed nothing when there was none. The light asked the virtual bus, which says yes the moment
	/// it accepts a controller - and it accepts one even when Windows never finishes building it. The
	/// result was a green light over a dead panel, with no entry in Windows Game Controllers and
	/// nothing for a game to read.
	///
	/// Two sources for one fact will always drift, and the one that drifted was the one a person looks
	/// at first. This holds them to a single source.
	/// </remarks>
	[TestClass]
	public class ControllerLightSourceTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The tab light is drawn from the controller XInput hands back")]
		public void The_tab_light_is_drawn_from_the_controller_xinput_hands_back()
		{
			// The whole file, not a slice of it. The window that draws the light has no business
			// asking the bus anywhere, and a slice would only prove the one place I happened to cut.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			StringAssert.Contains(main, "LiveXiConnected",
				"The light is not drawn from the controller XInput hands back, which is the only " +
				"evidence that one exists and the only thing a game can read.");
			Assert.IsFalse(main.Contains("IsControllerConnected"),
				"The window is asking the virtual bus again. The bus says yes as soon as it accepts " +
				"a controller, whether or not Windows ever finishes building one, which is how a " +
				"green light ended up over a panel with nothing in it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The light and the panel read the same fields")]
		public void The_light_and_the_panel_read_the_same_fields()
		{
			// Named side by side so that moving one and not the other is caught here rather than by
			// somebody staring at a green light with a dead panel under it.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			var pad = Read(Path.Combine("App.v4", "Controls", "PadControl.cs"));
			foreach (var field in new[] { "LiveXiConnected", "XiPlaceForPad" })
			{
				StringAssert.Contains(pad, field, "The panel no longer reads " + field + ".");
				StringAssert.Contains(main, field, "The light no longer reads " + field + ".");
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Not having looked is never reported as working")]
		public void Not_having_looked_is_never_reported_as_working()
		{
			// With the read-back switched off there is no evidence either way, and the words have to say
			// that rather than pick the comfortable answer.
			var unchecked_ = x360ce.App.MainForm.ControllerStateHint(1, true, false, false, false);
			var missing = x360ce.App.MainForm.ControllerStateHint(1, true, false, false, true);
			Assert.AreNotEqual(missing, unchecked_,
				"Not having checked reads the same as having checked and found nothing.");
			StringAssert.Contains(unchecked_, "has not ",
				"A state nobody looked at has to say so.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Not having looked is never reported as broken either")]
		public void Not_having_looked_is_never_reported_as_broken()
		{
			// The mirror of the test above, and the half that was missing. Refusing to call an unchecked
			// controller working is only right if it is not called broken instead, and it was: turning the
			// read-back off lit the tab red on every controller that was working perfectly well. The
			// setting only decides where the numbers on screen come from - it does not unplug anything.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			var light = main.Substring(main.IndexOf("string left, right;"));
			light = light.Substring(0, light.IndexOf("var bullet = StatusImageKey"));
			var unchecked_ = light.IndexOf("if (!checking)");
			var mixed = light.IndexOf("StatusColor");
			Assert.IsTrue(unchecked_ >= 0,
				"The light does not ask whether anything was checked, so with the read-back off it " +
				"reports a fault on evidence it never gathered.");
			Assert.IsTrue(unchecked_ < mixed,
				"How much is wrong is worked out before anyone asks whether it was looked at, so a " +
				"working controller reddens whenever the read-back is switched off.");
			StringAssert.Contains(light.Substring(unchecked_, mixed - unchecked_), "StatusBlue",
				"An unchecked controller is not shown as unchecked.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Whether anything was checked is asked of the reading, not of the setting")]
		public void Whether_anything_was_checked_is_asked_of_the_reading()
		{
			// The setting says the states are wanted. It does not say they arrived: the read also needs
			// the XInput library to be loaded, and when it is not, nothing is read while the setting
			// still says everything is being watched. Every place then reports empty and every working
			// controller is called broken - on evidence nobody gathered.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			var line = main.Substring(main.IndexOf("var checking = "));
			line = line.Substring(0, line.IndexOf(";"));
			StringAssert.Contains(line, "XiStatesRead",
				"The light asks whether the read-back was wanted rather than whether it happened.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A place held by a real controller is never shown as empty")]
		public void A_place_held_by_a_real_controller_is_never_shown_as_empty()
		{
			// Whether a controller sits in this tab's place, and whether it is the one we made, are two
			// questions. One variable answered both, so a tab whose place a real controller was holding
			// showed the light for an empty place - the one thing it certainly was not.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			var line = main.Substring(main.IndexOf("var xiOn = "));
			line = line.Substring(0, line.IndexOf(";"));
			Assert.IsFalse(line.Contains("XiPlaceForPad"),
				"Whether anything holds this tab's place is being answered by whether WE hold it, so " +
				"a real controller sitting there reads as an empty place and the tab goes dark.");
			StringAssert.Contains(main, "var xiOurs = ",
				"Whose controller holds the place is no longer asked, so a real controller in the " +
				"place cannot be told from the one this program made.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A real controller in the place is not called working")]
		public void A_real_controller_in_the_place_is_not_called_working()
		{
			// The state that looks most like working and is furthest from it: a game finds a controller at
			// this place and reads it, so nothing appears wrong, while every mapping on the tab goes
			// nowhere. Counting it as nothing wrong would be the whole fault, stated as success.
			var main = Read(Path.Combine("App.v4", "MainForm.cs"));
			var light = main.Substring(main.IndexOf("var wrong = 0;"));
			light = light.Substring(0, light.IndexOf("StatusColor"));
			StringAssert.Contains(light, "if (!xiOurs && !xiOn)",
				"Nothing separates an empty place from one holding a controller of ours.");
			StringAssert.Contains(light, "else if (!xiOurs)",
				"A place held by a controller this program did not make counts as nothing wrong, so a tab " +
				"whose mappings reach no game reports success.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Where our controllers are is known, not deduced")]
		public void Where_our_controllers_are_is_known_not_deduced()
		{
			// A controller for a pad is only ever made when that pad's own place is free, and taken
			// away again unless Windows puts it exactly there. So the answer is known by construction.
			//
			// It was worked out a second time all the same, by counting the places reporting a
			// controller and counting the ones we believed we had made. That second answer was wrong
			// whenever a real controller held a place of its own - the counts disagreed, it gave up,
			// and a controller that was working showed a red light on its tab.
			var step6 = Read(Path.Combine("App.v4", "Common", "DInput", "DInputHelper.Step6.RetrieveXiStates.cs"));
			foreach (var counting in new[] { "occupied.Count", "ours.Count" })
				Assert.IsFalse(step6.Contains(counting),
					"The place a controller holds is being counted out again beside the answer that " +
					"is already known, and the counting one gives up whenever a real controller is " +
					"plugged in.");
		}

	}
}
