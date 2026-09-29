// @under-test: App.v4/Common/DInput/XInputPlaces.cs, App.v4/MainForm.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>What is holding an XInput place, as the last reading of the machine found it.</summary>
	/// <remarks>
	/// A pad on Controller 1 that a game reads as player 2 is most often something else holding XInput 1,
	/// and naming what it is gives a person something to look for. The kinds are the ones the Devices page
	/// already names - Real, Virtual and Leftover - so the tab and the page agree.
	/// </remarks>
	[TestClass]
	public class XInputPlaceHolderTest
	{
		// A controller is listed under its piece of hardware and under the face XInput reads.
		const string Hardware = @"USB\VID_045E&PID_028E\1";
		const string Face = @"USB\VID_045E&PID_028E&IG_00\2&1";

		static Dictionary<string, int> Places(params object[] idThenPlace)
		{
			var places = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < idThenPlace.Length; i += 2)
				places[(string)idThenPlace[i]] = (int)idThenPlace[i + 1];
			return places;
		}

		static HashSet<string> Set(params string[] ids)
		{
			return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller somebody plugged in is named real")]
		public void A_plugged_in_controller_is_named_real()
		{
			Assert.AreEqual(XInputPlaces.HolderReal, XInputPlaces.HolderOf(0, Places(Hardware, 0, Face, 0), Set(), Set()));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("This program's own virtual controllers and other programs' are told apart")]
		public void Ours_and_other_programs_virtual_controllers_are_told_apart()
		{
			var places = Places(Hardware, 1, Face, 1, "LEFTOVER-HARDWARE", 2, "LEFTOVER-FACE", 2);
			var made = Set(Face, "LEFTOVER-FACE");
			var ours = Set(Face);
			Assert.AreEqual(XInputPlaces.HolderVirtual, XInputPlaces.HolderOf(1, places, made, ours));
			Assert.AreEqual(XInputPlaces.HolderLeftover, XInputPlaces.HolderOf(2, places, made, ours));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The hardware entry, which is never in the made lists, does not make a virtual controller look real")]
		public void The_hardware_entry_does_not_hide_a_virtual_face()
		{
			var made = Set(Face);
			Assert.AreEqual(XInputPlaces.HolderLeftover, XInputPlaces.HolderOf(0, Places(Hardware, 0, Face, 0), made, Set()));
			Assert.AreEqual(XInputPlaces.HolderLeftover, XInputPlaces.HolderOf(0, Places(Face, 0, Hardware, 0), made, Set()));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A place nothing known holds names nothing")]
		public void Nothing_known_names_nothing()
		{
			Assert.IsNull(XInputPlaces.HolderOf(0, Places(), Set(), Set()), "An empty place has no holder.");
			Assert.IsNull(XInputPlaces.HolderOf(0, Places(Hardware, XInputPlaces.Unknown), Set(), Set()),
				"A controller whose place could not be worked out holds no named place.");
			Assert.IsNull(XInputPlaces.HolderOf(4, Places(Hardware, 4), Set(), Set()), "There are four places.");
			Assert.IsNull(XInputPlaces.HolderOf(0, null, null, null));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each holder is said in its own words, and the words match the Devices page")]
		public void Every_holder_has_its_own_words()
		{
			var real = XInputPlaces.HolderWords(XInputPlaces.HolderReal);
			var ours = XInputPlaces.HolderWords(XInputPlaces.HolderVirtual);
			var leftover = XInputPlaces.HolderWords(XInputPlaces.HolderLeftover);
			var unknown = XInputPlaces.HolderWords(null);
			StringAssert.Contains(real, "real controller");
			StringAssert.Contains(ours, "this program");
			StringAssert.Contains(leftover, "DS4Windows");
			StringAssert.Contains(leftover, "Leftover", "The words must match what the Devices page calls it.");
			Assert.AreEqual("another controller", unknown);
			CollectionAssert.AllItemsAreUnique(new[] { real, ours, leftover, unknown });
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The tab hint is given the holder from the last reading and the bus error it already has")]
		public void The_tab_hint_is_given_the_holder()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.cs"));
			StringAssert.Contains(source, "XInputPlaces.HolderOf(i)", "The tab hint is not asked about what holds its place.");
			StringAssert.Contains(source, "checking, ours, enabled, busError, holder)", "The tab hint is not given the holder.");
			Assert.IsFalse(source.Contains("var errors = Global.DHelper?.VirtualErrors;"),
				"The hint reads the bus errors a second time instead of taking them from its caller.");
		}
	}
}
