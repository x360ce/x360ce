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
			var unchecked_ = x360ce.App.MainForm.ControllerStateHint(1, true, false, false);
			var missing = x360ce.App.MainForm.ControllerStateHint(1, true, false, true);
			Assert.AreNotEqual(missing, unchecked_,
				"Not having checked reads the same as having checked and found nothing.");
			StringAssert.Contains(unchecked_, "has not ",
				"A state nobody looked at has to say so.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Not having looked is never reported as broken either")]
		public void Not_having_looked_is_never_reported_as_broken()
		{
			// The mirror of the test above, and the half that was missing. Refusing to call an
			// unchecked controller working is only right if it is not called broken instead, and it
			// was: turning the read-back off lit the tab red on every controller that was working
			// perfectly well. The setting only decides where the numbers on screen come from - it
			// does not unplug anything - so red accused the emulation of a fault it did not have.
			var main = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			var light = main.Substring(main.IndexOf("var image = diOn"));
			light = light.Substring(0, light.IndexOf(";"));
			StringAssert.Contains(light, "!checking",
				"The light does not ask whether anything was checked, so with the read-back off it " +
				"reports a fault on evidence it never gathered.");
			var red = light.IndexOf("\"red\"");
			var unchecked_ = light.IndexOf("!checking");
			Assert.IsTrue(unchecked_ >= 0 && unchecked_ < red,
				"Red is reached before the unchecked case is considered, so a working controller " +
				"shows as broken whenever the read-back is switched off.");
		}

	}
}
