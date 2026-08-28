// @under-test: App.v4/MainForm.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// What the light on a controller tab claims, and whether a person can act on it.
	/// </summary>
	/// <remarks>
	/// The light showed green with a device mapped and no virtual controller behind it. Green there
	/// means the game is receiving what the person presses, and nothing was: the emulator looked on
	/// and did nothing. Worse, when the bus refused to make the controller the answer was thrown away,
	/// so there was no message, no mark, and nothing to look at.
	/// </remarks>
	[TestClass]
	public class ControllerStateHintTest
	{

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A missing virtual controller is said plainly, not hidden")]
		public void A_missing_virtual_controller_is_said_plainly()
		{
			// The case a person actually hits: their controller is plugged in and mapped, and no virtual
			// controller was made. The words have to name which half is missing.
			var text = MainForm.ControllerStateHint(1, true, false, true);
			StringAssert.Contains(text, "Controller 1");
			StringAssert.Contains(text, "no virtual controller",
				"The one state a person needs explaining is the one where their device works and the " +
				"game gets nothing. It has to say so.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each of the four states says something different")]
		public void Each_of_the_four_states_says_something_different()
		{
			// Four lights, four meanings. Two of them reading the same would leave the person guessing
			// which one they are looking at, which is the position the colour alone already puts them in.
			var both = MainForm.ControllerStateHint(1, true, true, true);
			var deviceOnly = MainForm.ControllerStateHint(1, true, false, true);
			var virtualOnly = MainForm.ControllerStateHint(1, false, true, true);
			var neither = MainForm.ControllerStateHint(1, false, false, true);
			var all = new[] { both, deviceOnly, virtualOnly, neither };
			for (var i = 0; i < all.Length; i++)
				for (var j = i + 1; j < all.Length; j++)
					Assert.AreNotEqual(all[i], all[j],
						"Two different states are described with the same words.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The words name the controller they belong to")]
		public void The_words_name_the_controller_they_belong_to()
		{
			// Four tabs, four lights. A message that does not say which one it is about is no use on the
			// tab beside three others.
			for (var place = 1; place <= 4; place++)
				StringAssert.Contains(MainForm.ControllerStateHint(place, true, false, true),
					"Controller " + place);
		}

	}
}
