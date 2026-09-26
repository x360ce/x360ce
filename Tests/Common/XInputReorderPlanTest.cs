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
		[Description("Controllers are brought back in the order asked for")]
		public void They_come_back_in_the_order_asked_for()
		{
			var plan = XInputReorderPlan.For(new[] { Virtual("pad", 2), Real("xbox", 0) });
			var arrivals = plan.Steps
				.Where(s => s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal)
				.ToList();
			CollectionAssert.AreEqual(new[] { "pad", "xbox" }, arrivals.Select(s => s.HardwareId).ToArray(),
				"The order things are brought back in is the order of places, so it has to match "
				+ "what was asked for.");
			CollectionAssert.AreEqual(new[] { 0, 1 }, arrivals.Select(s => s.ExpectedPlace).ToArray(),
				"Each arrival takes the lowest free place, so the places expected run upward from one.");
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
			var arrivals = plan.Steps.Where(s => s.Kind == Kind.CreateVirtual || s.Kind == Kind.EnableReal).ToList();
			CollectionAssert.AreEqual(new[] { "Controller 1", "c2", "c3", "xbox" }, Ids2(arrivals));
			CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, arrivals.Select(s => s.ExpectedPlace).ToArray());
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

		static JocysCom.ClassLibrary.IO.DeviceInfo Node(string id, string parent, System.Guid classGuid = default(System.Guid))
		{
			return new JocysCom.ClassLibrary.IO.DeviceInfo { DeviceId = id, HardwareIds = id, ParentDeviceId = parent, ClassGuid = classGuid };
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An Xbox One controller is moved by switching its input part, an Xbox 360 controller by switching itself")]
		public void Only_the_part_xinput_reads_is_switched()
		{
			// Switched off whole, an Xbox One controller turned itself off and stayed off when switched on.
			var one = Node(@"USB\VID_045E&PID_02D1\7EED", null);
			var input = Node(@"USB\VID_045E&PID_02FF&IG_00\00", one.DeviceId);
			var hid = Node(@"HID\VID_045E&PID_02FF&IG_00\9", input.DeviceId);
			var byId = new[] { one, input, hid }.ToDictionary(x => x.DeviceId, x => x);
			Assert.AreEqual(input.DeviceId, XInputReorderPlan.SwitchedPart(hid, one.DeviceId, byId),
				"The whole Xbox One controller would be switched off, and it does not come back from that.");
			// An Xbox 360 controller is read by XInput through the controller itself.
			var x360 = Node(@"USB\VID_045E&PID_028E\01", null, new System.Guid("d61ca365-5af4-4486-998b-9db4734c6ca3"));
			var x360Input = Node(@"HID\VID_045E&PID_028E&IG_00\3", x360.DeviceId);
			var byId360 = new[] { x360, x360Input }.ToDictionary(x => x.DeviceId, x => x);
			Assert.AreEqual(x360.DeviceId, XInputReorderPlan.SwitchedPart(x360Input, x360.DeviceId, byId360),
				"Switching only an Xbox 360 controller's input part leaves the place XInput reads where it was.");
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
				plan.Steps.Select(s => s.Kind).ToArray());
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

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The same controller twice is refused")]
		public void The_same_controller_twice_is_refused()
		{
			var plan = XInputReorderPlan.For(new[] { Virtual("a", 0), Virtual("a", 1) });
			Assert.IsNotNull(plan.Refusal, "One controller was asked to hold two places at once.");
		}
	}
}
