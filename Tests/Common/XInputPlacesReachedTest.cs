// @under-test: App.v4/Common/DInput/XInputPlaces.cs, App.v4/Controls/UserDevicesUserControl.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// What the XInput column on the device list says, when a device reaches a game in more than one
	/// place.
	/// </summary>
	/// <remarks>
	/// A device can be mapped to more than one controller tab, and each tab has an XInput place of
	/// its own, so one device can be felt by a game in two places at once. The column named only the
	/// place the device held itself - which for anything but an Xbox controller is no place at all,
	/// so the column was blank for the very devices this program exists to map.
	/// </remarks>
	[TestClass]
	public class XInputPlacesReachedTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device felt in two places names both of them")]
		public void A_device_felt_in_two_places_names_both()
		{
			var text = XInputPlaces.Describe(XInputPlaces.Unknown, false, false, new[] { 0, 1 });
			Assert.AreEqual("Virtual 1, Virtual 2", text,
				"Naming only the first hides the rest, and the rest are the ones somebody has " +
				"forgotten they mapped.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A place the device holds itself is told from one this program carries it to")]
		public void A_real_place_is_told_from_a_carried_one()
		{
			// They mean opposite things to somebody deciding what to change. Real is the device sitting
			// in that place, which a game reads with this program switched off. Virtual is this program
			// carrying it there, and it stops when this program does.
			Assert.AreEqual("Virtual 2, Real 3", XInputPlaces.Describe(2, false, false, new[] { 1 }));
		}

		[TestMethod, TestCategory("devices")]
		[Description("The same place reached twice is said once")]
		public void The_same_place_reached_twice_is_said_once()
		{
			// Saying it twice would read as two controllers.
			Assert.AreEqual("Virtual 1", XInputPlaces.Describe(XInputPlaces.Unknown, false, false, new[] { 0, 0 }));
			// And where both routes lead to one place, the device being there itself is the stronger
			// fact, because a game reads it either way.
			Assert.AreEqual("Real 1", XInputPlaces.Describe(0, false, false, new[] { 0 }));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Places are named in order, whatever order they were found in")]
		public void Places_are_named_in_order()
		{
			Assert.AreEqual("Virtual 1, Virtual 3",
				XInputPlaces.Describe(XInputPlaces.Unknown, false, false, new[] { 2, 0 }));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device that reaches no place says nothing rather than guessing")]
		public void A_device_that_reaches_no_place_says_nothing()
		{
			// A place stated wrongly is worse than a place left blank: somebody would map a controller
			// against it.
			Assert.AreEqual("", XInputPlaces.Describe(XInputPlaces.Unknown, false, false, new[] { XInputPlaces.Unknown }));
			Assert.AreEqual("", XInputPlaces.Describe(XInputPlaces.Unknown, false, false, new int[0]));
			Assert.AreEqual("", XInputPlaces.Describe(XInputPlaces.Unknown, false, false, null));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Every list showing this column gets its answer from the same place")]
		public void Every_list_showing_this_column_gets_one_answer()
		{
			// Three lists show which XInput places a device reaches, and each had worked it out for
			// itself. They drifted, as separate answers to one question do: the one on the controller
			// tabs asked only about the place the device holds itself, so every device that was not an
			// Xbox controller read as blank on the very page where it had just been mapped.
			foreach (var file in new[]
			{
				Path.Combine("App.v4", "Controls", "UserDevicesUserControl.cs"),
				Path.Combine("App.v4", "Controls", "PadControl.cs"),
			})
				StringAssert.Contains(Read(file), "AppHelper.GetXInputPlaces",
					"The list in " + Path.GetFileName(file) + " works out the places for itself, so it " +
					"can disagree with every other list showing the same column.");

			// And that one answer has to look at both routes in, not only the face the device owns.
			var helper = Read(Path.Combine("App.v4", "Common", "AppHelper.cs"));
			var method = helper.Substring(helper.IndexOf("public static string GetXInputPlaces"));
			method = method.Substring(0, method.IndexOf("public static Bitmap"));
			StringAssert.Contains(method, "XiPlaceForPad",
				"The answer does not ask where the controllers this device is mapped to are, so it is " +
				"blank for every device that is not an Xbox controller.");
			StringAssert.Contains(method, "MapTo",
				"The answer does not look at what the device is mapped to.");
		}

	}
}
