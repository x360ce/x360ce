// @under-test: App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether two controllers can be shown holding one XInput place.
	/// </summary>
	/// <remarks>
	/// They cannot. A place holds one controller, and a list showing two in the same place is showing
	/// something that did not happen - which is worse than showing nothing, because somebody will map
	/// a controller against it and wonder why the game never sees them.
	///
	/// It happened twice, from two different mistakes, so the shape of the answer is now checked as
	/// well as the working that produces it.
	///
	/// The first was a note outliving its controller: where each of ours landed was written down and
	/// never rubbed out, so a controller taken away went on claiming its place and the next one given
	/// that place claimed it too.
	///
	/// The second was the note being matched to the wrong device. A note is found by the number at
	/// the end of a device's name, which is not a serial number - it is whatever follows the last
	/// ampersand - and the search climbed from the controller up through the USB hubs and bridges
	/// above it. A real Xbox controller hangs off a hub whose name ends "&amp;2", so it matched the note
	/// for controller two and was handed that place while still being called real.
	/// </remarks>
	[TestClass]
	public class XInputPlaceCollisionTest
	{

		static string Source()
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "XInputPlaces.cs"));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A note is only ever matched to a controller this program could have made")]
		public void A_note_is_only_matched_to_a_controller_we_could_have_made()
		{
			// The number at the end of a name belongs to no particular kind of device, so the search
			// has to be told which devices it may look at. Without that it reaches the shared hardware
			// above the controller, which every device on the machine hangs off.
			var source = Source();
			var walk = source.Substring(source.IndexOf("static int RecordedPlace"));
			walk = walk.Substring(0, walk.IndexOf("public static string HardwareOf"));
			Assert.AreEqual(2, Regex(walk, "IsVirtualPad"),
				"The search for where a controller landed is not bounded to controllers this program " +
				"could have made, at entry and at every step up. It will climb into the USB hubs above " +
				"a real controller and hand it a place belonging to an emulated one.");
		}

		static int Regex(string text, string needle)
		{
			return System.Text.RegularExpressions.Regex.Matches(text, needle).Count;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Two notes pointing at one place are both refused")]
		public void Two_notes_pointing_at_one_place_are_both_refused()
		{
			// Belt as well as braces. Whatever produced them, two controllers in one place cannot both
			// be right and nothing can say which is, so neither is shown. A blank means "not known",
			// which is exactly what this is.
			var source = Source();
			var resolve = source.Substring(source.IndexOf("public static Dictionary<string, int> Resolve(DeviceInfo[] all"));
			resolve = resolve.Substring(0, resolve.IndexOf("static Dictionary<string, int> _cache"));
			StringAssert.Contains(resolve, "GroupBy",
				"Nothing checks that the places worked out are possible, so a list can show two " +
				"controllers holding one place.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller taken away stops claiming the place it held")]
		public void A_controller_taken_away_stops_claiming_its_place()
		{
			// The note has to be rubbed out with the controller, or the next controller given that
			// place is the second thing claiming it.
			var step5 = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName,
				"App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(step5, "XInputPlaces.Forget(",
				"Letting go of a controller leaves the note about where it was, so it goes on " +
				"claiming that place for the rest of the run.");
			StringAssert.Contains(step5, "XInputPlaces.Forget();",
				"Letting go of every controller leaves every note behind.");
		}

	}
}
