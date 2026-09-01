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
		[Description("More controllers than places is refused, not attempted")]
		public void More_controllers_than_places_is_refused()
		{
			var plan = XInputReorderPlan.For(new[]
			{
				Virtual("a", -1), Virtual("b", -1), Virtual("c", -1), Virtual("d", -1), Real("e", 0),
			});
			Assert.IsNotNull(plan.Refusal, "Five controllers were accepted for four places.");
			Assert.AreEqual(0, plan.Steps.Count, "A refused plan still has steps in it.");
			StringAssert.Contains(plan.Refusal, "four places");
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
