// @under-test: App.v4/Common/DInput/XInputReorderPlan.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.DInput;
using Kind = x360ce.App.DInput.XInputReorderPlan.StepKind;

namespace x360ce.Tests
{
	/// <summary>
	/// The plan that puts controllers in the places somebody asked for.
	/// </summary>
	/// <remarks>
	/// No hardware is touched here. Working out the steps is arithmetic on a list, and keeping it
	/// that way is what makes it possible to check at all - the doing needs a real controller and
	/// Administrator, and could never be checked on a build machine.
	/// </remarks>
	[TestClass]
	public class XInputReorderPlanTest
	{
		static XInputReorderPlan.Entry Virtual(string id, int place)
		{
			return new XInputReorderPlan.Entry { HardwareId = id, Name = id, IsVirtual = true, IsOurs = true, Place = place };
		}

		static XInputReorderPlan.Entry Real(string id, int place)
		{
			return new XInputReorderPlan.Entry { HardwareId = id, Name = id, IsVirtual = false, Place = place };
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An order that already holds asks for nothing")]
		public void Nothing_is_done_when_the_order_already_holds()
		{
			var plan = XInputReorderPlan.For(new[] { Virtual("a", 0), Real("b", 1) });
			Assert.IsNull(plan.Refusal);
			Assert.AreEqual(0, plan.Steps.Count,
				"A controller would be switched off to put it back where it already was.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Everything gives up its place before anything takes one")]
		public void Places_are_given_up_before_they_are_taken()
		{
			// The case that matters: a real controller in the first place, wanted second, so a made
			// one can have the first. Nothing can arrive until the place it needs is free.
			var plan = XInputReorderPlan.For(new[] { Virtual("pad", -1), Real("xbox", 0) });
			Assert.IsNull(plan.Refusal);
			var lastRemoval = plan.Steps.FindLastIndex(s =>
				s.Kind == Kind.RemoveVirtual || s.Kind == Kind.DisableReal);
			var firstArrival = plan.Steps.FindIndex(s =>
				s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal);
			Assert.IsTrue(lastRemoval < firstArrival,
				"Something is brought back before everything has given up its place, so it will be "
				+ "given whichever place happens to be free rather than the one asked for.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A virtual controller arrives with a hint, so the one for the first place is made fourth")]
		public void The_first_place_is_given_to_the_fourth_controller_to_arrive()
		{
			// The measured case: the real controller is wanted last, so the three virtual ones come first. XInput gives the
			// first of them the second place and the second the third, so the third is made as a temporary controller to hold
			// the fourth, and the one for the first place is the fourth to arrive, which is given the first free place.
			var plan = XInputReorderPlan.For(new[] { Ours("c1", 1, 1), Ours("c2", 2, 2), Ours("c3", 3, 3), Real("xbox", 0) });
			Assert.IsNull(plan.Refusal, plan.Refusal);
			CollectionAssert.AreEqual(new[]
			{
				Kind.RemoveVirtual, Kind.RemoveVirtual, Kind.RemoveVirtual, Kind.DisableReal,
				Kind.CreateVirtual, Kind.CreateVirtual, Kind.CreateDecoy, Kind.CreateVirtual, Kind.RemoveDecoy, Kind.EnableReal,
			}, plan.Steps.Select(s => s.Kind).ToArray(), plan.ToString());
			var arrivals = plan.Steps.Skip(4).ToList();
			CollectionAssert.AreEqual(new[] { "c2", "c3", null, "c1", null, "xbox" }, arrivals.Select(s => s.HardwareId).ToArray());
			CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, -1, 3 }, arrivals.Select(s => s.ExpectedPlace).ToArray());
			StringAssert.Contains(plan.ToString(), "temporary controller");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A real controller wanted first makes no temporary controller: it is given the first free place")]
		public void A_real_controller_wanted_first_needs_no_temporary_controller()
		{
			var plan = XInputReorderPlan.For(new[] { Real("xbox", 3), Ours("c2", 1, 2), Ours("c3", 2, 3), Ours("c4", -1, 4) });
			Assert.IsNull(plan.Refusal, plan.Refusal);
			Assert.IsFalse(plan.Steps.Any(s => s.Kind == Kind.CreateDecoy), "A temporary controller is made for nothing:" + System.Environment.NewLine + plan);
			var arrivals = plan.Steps.Where(s => s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal).ToList();
			CollectionAssert.AreEqual(new[] { "c2", "c3", "c4", "xbox" }, Ids2(arrivals));
			CollectionAssert.AreEqual(new[] { 1, 2, 3, 0 }, arrivals.Select(s => s.ExpectedPlace).ToArray());
		}

		/// <summary>Where each wanted controller lands when the plan's arrivals are made, by the rule XInput gives places out with.</summary>
		/// <remarks>The rule stated again here, so a plan is held to it and not to itself.</remarks>
		static int[] Land(XInputReorderPlan plan, IList<XInputReorderPlan.Entry> wanted, out int mostTemporary)
		{
			var held = new[] { "", "", "", "" };
			var hinted = 0;
			var temporary = 0;
			mostTemporary = 0;
			foreach (var step in plan.Steps)
			{
				var tag = step.Kind == Kind.CreateDecoy || step.Kind == Kind.RemoveDecoy ? "temporary " + step.Decoy : step.HardwareId;
				if (step.Kind == Kind.RemoveDecoy)
				{
					var at = System.Array.IndexOf(held, tag);
					if (at >= 0)
						held[at] = "";
					hinted--;
					temporary--;
				}
				else if (step.Kind == Kind.CreateVirtual || step.Kind == Kind.EnableReal || step.Kind == Kind.CreateDecoy)
				{
					var place = -1;
					if (step.Kind != Kind.EnableReal)
					{
						hinted++;
						if (hinted < 4 && held[hinted] == "")
							place = hinted;
					}
					if (place < 0)
						place = System.Array.IndexOf(held, "");
					if (place >= 0)
						held[place] = tag;
					if (step.Kind == Kind.CreateDecoy)
						mostTemporary = System.Math.Max(mostTemporary, ++temporary);
				}
			}
			return wanted.Select(x => System.Array.IndexOf(held, x.HardwareId)).ToArray();
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Whatever order is asked for, the arrivals put every controller where it was asked, and no temporary controller is left")]
		public void The_arrivals_put_every_controller_where_it_was_asked()
		{
			var orders = new List<XInputReorderPlan.Entry[]>
			{
				new[] { Ours("c1", -1, 1), Ours("c2", -1, 2), Ours("c3", -1, 3), Real("x", -1) },
				new[] { Ours("c1", -1, 1), Ours("c2", -1, 2), Ours("c3", -1, 3), Ours("c4", -1, 4) },
				new[] { Real("x", -1), Ours("c2", -1, 2), Ours("c3", -1, 3), Ours("c4", -1, 4) },
				new[] { Ours("c1", -1, 1), Real("x", -1), Ours("c3", -1, 3), Ours("c4", -1, 4) },
				new[] { Ours("c1", -1, 1), Ours("c2", -1, 2), Real("x", -1), Ours("c4", -1, 4) },
				new[] { Ours("c1", -1, 1), Ours("c2", -1, 2), Real("x", -1), Real("y", -1) },
				new[] { Ours("c1", -1, 1), Real("x", -1), Real("y", -1), Ours("c4", -1, 4) },
				new[] { Real("x", -1), Real("y", -1), Ours("c3", -1, 3), Ours("c4", -1, 4) },
				new[] { Real("x", -1), Ours("c2", -1, 2), Real("y", -1), Ours("c4", -1, 4) },
				new[] { Ours("c1", -1, 1), Real("x", -1), Real("y", -1), Real("z", -1) },
				new[] { Ours("c1", -1, 1), Real("x", -1) },
				new[] { Real("x", -1), Ours("c2", -1, 2) },
				new[] { Ours("c1", -1, 1) },
				new[] { Ours("c1", -1, 1), Ours("c2", -1, 2), Ours("c3", -1, 3), Real("x", -1), Real("y", -1) },
			};
			foreach (var wanted in orders)
			{
				var plan = XInputReorderPlan.For(wanted);
				Assert.IsNull(plan.Refusal, plan.Refusal);
				int most;
				var landed = Land(plan, wanted, out most);
				CollectionAssert.AreEqual(Enumerable.Range(0, wanted.Length).Select(i => i < 4 ? i : -1).ToArray(), landed,
					"Not where asked: " + string.Join(", ", Ids(wanted)) + System.Environment.NewLine + plan);
				Assert.IsTrue(most <= 3, "More than three temporary controllers at once.");
				Assert.AreEqual(plan.Steps.Count(s => s.Kind == Kind.CreateDecoy), plan.Steps.Count(s => s.Kind == Kind.RemoveDecoy),
					"A temporary controller is left on the bus:" + System.Environment.NewLine + plan);
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Ours are taken away before a real controller is switched off")]
		public void Ours_are_taken_away_first()
		{
			// Every controller of ours taken away is a place freed for nothing. A real controller
			// switched off costs Administrator and is visible to the person holding it, so it is
			// worth doing last and only if still needed.
			var plan = XInputReorderPlan.For(new[] { Real("xbox", 1), Virtual("pad", 0) });
			var lastOurs = plan.Steps.FindLastIndex(s => s.Kind == Kind.RemoveVirtual);
            var firstReal = plan.Steps.FindIndex(s => s.Kind == Kind.DisableReal);
			Assert.IsTrue(lastOurs >= 0 && firstReal >= 0 && lastOurs < firstReal,
				"A real controller is switched off before ours are taken away, so somebody's "
				+ "hardware is disturbed while a free alternative was still available.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A plan touching only our own controllers needs no Administrator")]
		public void Only_our_own_controllers_needs_no_administrator()
		{
			var ours = XInputReorderPlan.For(new[] { Virtual("a", 1), Virtual("b", 0) });
			Assert.IsFalse(ours.NeedsElevation,
				"Administrator is asked for to move controllers this program made and can take away "
				+ "by itself.");
			var withReal = XInputReorderPlan.For(new[] { Virtual("a", 1), Real("b", 0) });
			Assert.IsTrue(withReal.NeedsElevation,
				"A real controller has to be switched off, which needs Administrator, and the plan "
				+ "does not say so.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Controllers past the fourth are brought back last and expect no place")]
		public void Controllers_past_the_fourth_expect_no_place()
		{
			var plan = XInputReorderPlan.For(new[]
			{
				Ours("c1", 1, 1), Ours("c2", 0, 2), Ours("c3", 2, 3), Real("a", 3), Real("b", -1),
			});
			Assert.IsNull(plan.Refusal, "A fifth controller was refused, though it only needs to wait for a place.");
			var last = plan.Steps.Last();
			Assert.AreEqual("b", last.HardwareId, "The fifth controller is not the last one brought back.");
			Assert.AreEqual(-1, last.ExpectedPlace, "The fifth controller is expected to get a place, and there are four.");
			StringAssert.Contains(last.ToString(), "no XInput place");
		}

		static XInputReorderPlan.Entry Ours(string id, int place, int pad)
		{
			var entry = Virtual(id, place);
			entry.Pad = pad;
			return entry;
		}

		/// <summary>A controller tab set to have a virtual controller that does not exist yet.</summary>
		static XInputReorderPlan.Entry Waiting(int pad)
		{
			var entry = Ours("Controller " + pad, -1, pad);
			entry.Waiting = true;
			return entry;
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller waiting for its place is ordered with the rest, and made after the real one is out of the way")]
		public void A_waiting_controller_is_ordered_and_made()
		{
			// As reported: the real controller holds place one, so Controller 1 waits and makes nothing,
			// Controllers 2 and 3 hold their own places, and the list has no row to move the real one past.
			var found = new[] { Real("xbox", 0), Ours("c2", 1, 2), Ours("c3", 2, 3), Waiting(1) };
			var ordered = XInputReorderPlan.ByController(found);
			CollectionAssert.AreEqual(new[] { "Controller 1", "c2", "c3", "xbox" }, Ids(ordered));
			var plan = XInputReorderPlan.For(ordered);
			Assert.IsNull(plan.Refusal, plan.Refusal);
			Assert.IsFalse(plan.Steps.Any(s => s.Kind == Kind.RemoveVirtual && s.Pad == 1),
				"A controller that does not exist is planned to be taken away.");
			var places = plan.Steps.Where(s => s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal)
				.OrderBy(s => s.ExpectedPlace).ToList();
			CollectionAssert.AreEqual(new[] { "Controller 1", "c2", "c3", "xbox" }, Ids2(places));
			CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, places.Select(s => s.ExpectedPlace).ToArray());
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Each row names the controller whose place it holds, and only one holding no place names none")]
		public void Each_row_names_the_controller_whose_place_it_holds()
		{
			// XInput N belongs to Controller N, so a real controller in place one sits in Controller 1's place.
			Assert.AreEqual(1, Real("xbox", 0).Controller, "A real controller in Controller 1's place names no controller.");
			Assert.AreEqual(2, Ours("c2", 1, 2).Controller);
			Assert.AreEqual(1, Waiting(1).Controller, "A waiting virtual controller does not name the controller it waits for.");
			Assert.AreEqual(0, Real("fifth", -1).Controller, "A controller holding no place names one.");
		}

		static JocysCom.ClassLibrary.IO.DeviceInfo Node(string id, string parent)
		{
			return new JocysCom.ClassLibrary.IO.DeviceInfo { DeviceId = id, HardwareIds = id, ParentDeviceId = parent };
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A real controller is switched off and on as a whole, never by a part of it")]
		public void A_real_controller_is_switched_as_a_whole()
		{
			// Only its input part switched off, an Xbox One controller could not be switched on again: Windows answers that
			// the device is not connected, and it had no XInput place until it was unplugged and plugged in.
			var one = Node(@"USB\VID_045E&PID_02D1\7EED", null);
			var input = Node(@"USB\VID_045E&PID_02FF&IG_00\00", one.DeviceId);
			var hid = Node(@"HID\VID_045E&PID_02FF&IG_00\9", input.DeviceId);
			var machine = new[] { one, input, hid };
			var readMachine = XInputPlaces.ReadMachine;
			var game = x360ce.App.SettingsManager.CurrentGame;
			XInputPlaces.ReadMachine = () => machine;
			x360ce.App.SettingsManager.CurrentGame = null;
			List<XInputReorderPlan.Entry> entries;
			try
			{
				entries = XInputReorderPlan.ReadEntries();
			}
			finally
			{
				XInputPlaces.ReadMachine = readMachine;
				x360ce.App.SettingsManager.CurrentGame = game;
			}
			var xbox = entries.Single(x => !x.IsVirtual);
			Assert.AreEqual(one.DeviceId, xbox.HardwareId, "The controller is not read as the whole device.");
			var plan = XInputReorderPlan.For(new[] { Virtual("pad", -1), xbox });
			Assert.IsNull(plan.Refusal, plan.Refusal);
			var switched = plan.Steps.Where(s => s.Kind == Kind.DisableReal || s.Kind == Kind.EnableReal).ToArray();
			Assert.AreEqual(2, switched.Length, "The controller is not switched off and on again.");
			foreach (var step in switched)
				Assert.AreEqual(one.DeviceId, step.HardwareId,
					"Only a part of the controller is switched, and Windows does not bring that back: " + step);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A step about a virtual controller names its controller tab, not the kind of device every virtual controller is")]
		public void A_virtual_step_names_its_controller()
		{
			var plan = XInputReorderPlan.For(new[] { Waiting(1), Real("xbox", 0) });
			var make = plan.Steps.First(s => s.Kind == Kind.CreateVirtual);
			StringAssert.Contains(make.ToString(), "Controller 1", "The step does not say which controller it makes.");
		}

		static string[] Ids2(IEnumerable<XInputReorderPlan.Step> steps)
		{
			return steps.Select(x => x.HardwareId).ToArray();
		}

		static string[] Ids(IEnumerable<XInputReorderPlan.Entry> entries)
		{
			return entries.Select(x => x.HardwareId).ToArray();
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto-Order gives Controller N the virtual place N, and the real controller the place left")]
		public void Auto_order_puts_controller_N_in_virtual_N_and_real_after()
		{
			// As reported: the real Xbox controller arrived first and took place one, so Controller 1's
			// virtual went to place three and Controller 3's to place four.
			var found = new[] { Real("xbox", 0), Ours("c2", 1, 2), Ours("c1", 2, 1), Ours("c3", 3, 3) };
			CollectionAssert.AreEqual(new[] { "c1", "c2", "c3", "xbox" }, Ids(XInputReorderPlan.ByController(found)));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto-Order puts a real controller in the first place no controller tab needs")]
		public void Auto_order_fills_an_unused_place_with_a_real_controller()
		{
			// Controller 2 makes nothing, so its place is the first one free for the real controller.
			var found = new[] { Real("xbox", 0), Ours("c3", 1, 3), Ours("c1", 2, 1) };
			CollectionAssert.AreEqual(new[] { "c1", "xbox", "c3" }, Ids(XInputReorderPlan.ByController(found)));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Auto-Order leaves real controllers in their order when no tab makes a controller")]
		public void Auto_order_without_virtual_controllers_changes_nothing()
		{
			var found = new[] { Real("a", 0), Real("b", 1) };
			var ordered = XInputReorderPlan.ByController(found);
			CollectionAssert.AreEqual(new[] { "a", "b" }, Ids(ordered));
			Assert.AreEqual(0, XInputReorderPlan.For(ordered).Steps.Count, "An order already right was changed.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A real controller holding no place is switched off and on again, not only on")]
		public void A_real_controller_without_a_place_is_switched_off_and_on()
		{
			// Seen after a run that left the controller working to Windows and absent from XInput.
			// Switching on what is already on changes nothing; off and on again is what it needs.
			var plan = XInputReorderPlan.For(new[] { Ours("c1", 1, 1), Real("xbox", -1) });
			CollectionAssert.AreEqual(new[] { Kind.RemoveVirtual, Kind.DisableReal, Kind.CreateVirtual, Kind.EnableReal },
				plan.Steps.Select(s => s.Kind).Where(k => k != Kind.CreateDecoy && k != Kind.RemoveDecoy).ToArray());
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An order putting Controller N's virtual controller anywhere but XInput N is refused before anything is touched")]
		public void A_virtual_controller_out_of_its_own_place_is_refused()
		{
			// Moved by hand below the real controller, Controller 1's would be asked to take XInput 2.
			var plan = XInputReorderPlan.For(new[] { Real("xbox", 0), Ours("c1", 2, 1) });
			Assert.IsNotNull(plan.Refusal, "Controller 1's virtual controller was planned into XInput 2.");
			Assert.AreEqual(0, plan.Steps.Count, "A refused plan would still switch the real controller off.");
			StringAssert.Contains(plan.Refusal, "XInput 1");
		}

		/// <summary>A virtual controller this program did not make and that holds no place, as a run that ended badly leaves one.</summary>
		static XInputReorderPlan.Entry LeftBehind(string id)
		{
			return new XInputReorderPlan.Entry { HardwareId = id, Name = id, IsVirtual = true, IsOurs = false, Place = -1 };
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A virtual controller left behind with no place takes no part in the order, and the rest keep their places")]
		public void A_controller_left_behind_with_no_place_is_not_ordered()
		{
			// As reported: it was given a position and a step to make it, which failed at once, so the order stopped part way,
			// and every row after it was expected one place too far along.
			var plan = XInputReorderPlan.For(new[] { Ours("c1", 1, 1), LeftBehind("orphan"), Real("xbox", 0) });
			Assert.IsNull(plan.Refusal, plan.Refusal);
			Assert.IsFalse(plan.Steps.Any(s => s.HardwareId == "orphan"), "A step acts on the controller left behind:"
				+ System.Environment.NewLine + plan);
			var arrivals = plan.Steps.Where(s => s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal).ToList();
			CollectionAssert.AreEqual(new[] { "c1", "xbox" }, Ids2(arrivals));
			CollectionAssert.AreEqual(new[] { 0, 1 }, arrivals.Select(s => s.ExpectedPlace).ToArray(),
				"The controllers after the one left behind are expected a place further along than they get.");
			// Auto-Order leaves it out too, rather than giving it the place a real controller should fill.
			var found = new[] { LeftBehind("orphan"), Real("xbox", 0), Ours("c3", 1, 3), Ours("c1", 2, 1) };
			CollectionAssert.AreEqual(new[] { "c1", "xbox", "c3" }, Ids(XInputReorderPlan.ByController(found)));
			// One that holds a place is still refused: it cannot be moved out of the way.
			var holding = LeftBehind("holding");
			holding.Place = 2;
			Assert.IsNotNull(XInputReorderPlan.For(new[] { Ours("c1", 1, 1), holding }).Refusal,
				"A virtual controller this program did not make, holding a place, was planned around.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The order list leaves out a virtual controller left behind with no place, and still lists a real one")]
		public void The_list_leaves_out_a_controller_left_behind_with_no_place()
		{
			// The measured tree: a real Xbox One controller, and a pad left behind whose parent has gone, with its USB face
			// and the HID face under it. Two unnamed controllers, so neither is given a place whatever XInput says.
			var root = Node(@"HTREE\ROOT\0", null);
			var hub = Node(@"USB\ROOT_HUB30\4&2C4A1B1&0", root.DeviceId);
			var one = Node(@"USB\VID_045E&PID_02D1\7EED", hub.DeviceId);
			var input = Node(@"USB\VID_045E&PID_02FF&IG_00\00", one.DeviceId);
			var oneHid = Node(@"HID\VID_045E&PID_02FF&IG_00\9", input.DeviceId);
			var usb = Node(@"USB\VID_045E&PID_028E&IG_01\2&2A3F02C7&5&01", @"USB\VID_045E&PID_028E\04");
			var hid = Node(@"HID\VID_045E&PID_028E&IG_01\3&1B6A3C2&0&0000", usb.DeviceId);
			var machine = XInputPlaces.ReadMachine;
			var game = x360ce.App.SettingsManager.CurrentGame;
			XInputPlaces.ReadMachine = () => new[] { root, hub, one, input, oneHid, usb, hid };
			// No game, so no tab waits for a controller and every row is one on the machine.
			x360ce.App.SettingsManager.CurrentGame = null;
			try
			{
				var entries = XInputReorderPlan.ReadEntries();
				CollectionAssert.AreEqual(new[] { one.DeviceId }, Ids(entries),
					"The pad left behind is listed for ordering; the Issues tab lists it for removal. Listed: " + string.Join(", ", Ids(entries)));
				Assert.IsFalse(entries[0].IsVirtual, "The real controller is listed as virtual.");
			}
			finally
			{
				XInputPlaces.ReadMachine = machine;
				x360ce.App.SettingsManager.CurrentGame = game;
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The same controller twice is refused")]
		public void The_same_controller_twice_is_refused()
		{
			var plan = XInputReorderPlan.For(new[] { Virtual("a", 0), Virtual("a", 1) });
			Assert.IsNotNull(plan.Refusal, "One controller was asked to hold two places at once.");
		}
	}
}
