// @under-test: App.v4/MainForm.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs, App.v4/Common/DInput/XInputPlaces.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using x360ce.App;
using x360ce.App.DInput;

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
			var text = MainForm.ControllerStateHint(1, true, false, false, true);
			StringAssert.Contains(text, "Controller 1");
			StringAssert.Contains(text, "no virtual controller",
				"The one state a person needs explaining is the one where their device works and the " +
				"game gets nothing. It has to say so.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each state says something different")]
		public void Each_of_the_four_states_says_something_different()
		{
			// Fewer lights than states, on purpose: a real controller in the place is the same colour
			// as our own with nothing driving it, because to a game it is the same fact. The words are
			// what tells them apart, so two of them reading the same would leave the person exactly
			// where the colour alone already leaves them.
			var both = MainForm.ControllerStateHint(1, true, true, true, true);
			var deviceOnly = MainForm.ControllerStateHint(1, true, false, false, true);
			var virtualOnly = MainForm.ControllerStateHint(1, false, true, true, true);
			var neither = MainForm.ControllerStateHint(1, false, false, false, true);
			// A real controller holding the place is its own state in both halves of the table: with a
			// device mapped it is the worst state there is, and with none it is simply not ours.
			var realTookIt = MainForm.ControllerStateHint(1, true, true, false, true);
			var realOnly = MainForm.ControllerStateHint(1, false, true, false, true);
			var all = new[] { both, deviceOnly, virtualOnly, neither, realTookIt, realOnly };
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
				StringAssert.Contains(MainForm.ControllerStateHint(place, true, false, false, true),
					"Controller " + place);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A taken place says once what holds it, and gives one piece of advice")]
		public void A_taken_place_names_what_holds_it_once()
		{
			var holders = new[] { XInputPlaces.HolderReal, XInputPlaces.HolderVirtual, XInputPlaces.HolderLeftover, null };
			var texts = new List<string>();
			foreach (var holder in holders)
			{
				var text = MainForm.ControllerStateHint(1, true, true, false, true, -1, true, VirtualError.PlaceTaken, holder);
				var words = XInputPlaces.HolderWords(holder);
				StringAssert.Contains(text, "XInput 1 is held by " + words, "The bus's reason does not name the holder.");
				Assert.AreEqual(1, Ui.Count(text, words), "The holder is named more than once: " + text);
				if (holder != XInputPlaces.HolderLeftover)
					Assert.AreEqual(1, Ui.Count(text.ToLowerInvariant(), "unplug"), "More than one piece of advice: " + text);
				texts.Add(text);
			}
			CollectionAssert.AllItemsAreUnique(texts, "Two different holders are described with the same words.");
			Assert.IsFalse(texts[2].Contains("a real controller"), "A virtual controller another program made is called real.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A Leftover in the place is to be removed, never moved with Auto-Order or unplugged")]
		public void A_leftover_is_to_be_removed()
		{
			// Auto-Order refuses a Leftover, and there is nothing to unplug.
			const string remove = "Remove it with [Remove Leftover Pads] on the Devices page.";
			var leftover = XInputPlaces.HolderLeftover;
			var texts = new[]
			{
				// The bus refused, with a device mapped.
				MainForm.ControllerStateHint(1, true, true, false, true, -1, true, VirtualError.PlaceTaken, leftover),
				// Held, with a device mapped, and nothing said by the bus.
				MainForm.ControllerStateHint(1, true, true, false, true, -1, true, VirtualError.None, leftover),
				// Held, with no device mapped.
				MainForm.ControllerStateHint(1, false, true, false, true, -1, true, VirtualError.None, leftover),
			};
			foreach (var text in texts)
			{
				Assert.AreEqual(1, Ui.Count(text, remove), "A Leftover is not pointed to Remove Leftover Pads once: " + text);
				Assert.IsFalse(text.Contains("Auto-Order"), "Auto-Order refuses a Leftover: " + text);
				Assert.IsFalse(text.ToLowerInvariant().Contains("unplug"), "A Leftover cannot be unplugged: " + text);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A real controller in the place keeps its advice: Auto-Order, or unplug it")]
		public void A_real_controller_keeps_its_advice()
		{
			var real = XInputPlaces.HolderReal;
			var taken = MainForm.ControllerStateHint(1, true, true, false, true, -1, true, VirtualError.PlaceTaken, real);
			StringAssert.Contains(taken, "Use Auto-Order on the Devices page to move it, or unplug it.");
			Assert.IsFalse(taken.Contains("Remove Leftover Pads"), "A real controller is pointed to Remove Leftover Pads.");
			StringAssert.Contains(MainForm.ControllerStateHint(1, true, true, false, true, -1, true, VirtualError.None, real),
				"Unplug it, or map this device on a tab whose place is free.");
			StringAssert.Contains(MainForm.ControllerStateHint(1, false, true, false, true, -1, true, VirtualError.None, real),
				"Map a device on this tab only once that controller is unplugged.");
		}

	}
}
