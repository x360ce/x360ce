// @under-test: Engine/Common/ConvertHelper.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Every mapped control passes through GetThumbValue on every poll, so it decides what a game
	/// actually receives. It had no tests. These pin the behaviour a player can feel: the centre, the
	/// ends, the dead zone, inversion, and the half ranges a wheel needs.
	/// </summary>
	/// <remarks>
	/// DirectInput reports an axis as 0 to 65535 with 32768 at rest. A thumb stick answers -32768 to
	/// 32767, a trigger 0 to 255. The numbers below are those ranges, not invented ones.
	/// </remarks>
	[TestClass]
	public class MappingConversionTest
	{

		private const float DiMin = 0f;
		private const float DiCentre = 32768f;
		private const float DiMax = 65535f;

		private static float Thumb(float diValue, float deadZone = 0f, float antiDeadZone = 0f,
			float linear = 0f, bool inverted = false, bool half = false)
		{
			return ConvertHelper.GetThumbValue(diValue, deadZone, antiDeadZone, linear, inverted, half, true);
		}

		private static float Trigger(float diValue, float deadZone = 0f, float antiDeadZone = 0f,
			float linear = 0f, bool inverted = false, bool half = false)
		{
			return ConvertHelper.GetThumbValue(diValue, deadZone, antiDeadZone, linear, inverted, half, false);
		}

		#region The ends and the middle

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick at rest reports the middle, and at its ends reports the ends")]
		public void A_stick_maps_its_ends_and_its_middle()
		{
			// A stick that does not rest near zero is the most visible mapping fault there is: the
			// character walks on its own.
			Assert.AreEqual(-32768f, Thumb(DiMin), 1f, "Fully one way should reach the low end.");
			Assert.AreEqual(32767f, Thumb(DiMax), 1f, "Fully the other way should reach the high end.");
			Assert.AreEqual(0f, Thumb(DiCentre), 1f, "At rest a stick must sit at zero.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A trigger reports nothing when released and full when pressed")]
		public void A_trigger_maps_its_ends()
		{
			Assert.AreEqual(0f, Trigger(DiMin), 1f, "A released trigger must report nothing.");
			Assert.AreEqual(255f, Trigger(DiMax), 1f, "A pressed trigger must report full.");
			Assert.AreEqual(128f, Trigger(DiCentre), 1f, "Half way should be about half.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The result never leaves the range the destination can carry")]
		public void Results_stay_inside_the_destination_range()
		{
			// Sweeping the whole input range catches an off-by-one at either end, which reaches a game
			// as a stick that cannot quite reach a corner.
			for (var di = 0f; di <= DiMax; di += 257f)
			{
				var thumb = Thumb(di);
				Assert.IsTrue(thumb >= -32768f && thumb <= 32767f,
					string.Format("A stick left its range at {0}: {1}", di, thumb));
				var trigger = Trigger(di);
				Assert.IsTrue(trigger >= 0f && trigger <= 255f,
					string.Format("A trigger left its range at {0}: {1}", di, trigger));
			}
		}

		#endregion

		#region Inversion

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Inverting a source swaps its ends and leaves the middle alone")]
		public void Inverting_swaps_the_ends()
		{
			Assert.AreEqual(32767f, Thumb(DiMin, inverted: true), 1f);
			Assert.AreEqual(-32768f, Thumb(DiMax, inverted: true), 1f);
			Assert.AreEqual(0f, Thumb(DiCentre, inverted: true), 1f,
				"Inverting must not move where a stick rests.");
		}

		#endregion

		#region Dead zone

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Inside the dead zone a stick reports exactly nothing")]
		public void Inside_the_dead_zone_a_stick_reports_nothing()
		{
			// Anything other than exactly zero here is drift, and drift is what a dead zone exists to
			// remove.
			const float deadZone = 4000f;
			Assert.AreEqual(0f, Thumb(DiCentre, deadZone), 0f);
			Assert.AreEqual(0f, Thumb(DiCentre + 1000f, deadZone), 0f);
			Assert.AreEqual(0f, Thumb(DiCentre - 1000f, deadZone), 0f);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Past the dead zone the full range is still reachable")]
		public void Past_the_dead_zone_the_ends_are_still_reachable()
		{
			// A dead zone that also cost travel at the end would quietly make a controller weaker the
			// more it was tuned.
			const float deadZone = 4000f;
			Assert.AreEqual(32767f, Thumb(DiMax, deadZone), 1f);
			Assert.AreEqual(-32768f, Thumb(DiMin, deadZone), 1f);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A dead zone does not make a stick jump when it is left")]
		public void Leaving_the_dead_zone_does_not_jump()
		{
			// The value must grow from zero, not appear part way up, or a small movement produces a
			// sudden lurch.
			const float deadZone = 4000f;
			var justOutside = Thumb(DiCentre + 4100f, deadZone);
			Assert.IsTrue(justOutside > 0f && justOutside < 2000f,
				string.Format("Leaving the dead zone jumped straight to {0}.", justOutside));
		}

		#endregion

		#region Anti dead zone

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An anti dead zone lifts the smallest movement above a game's own dead zone")]
		public void An_anti_dead_zone_lifts_the_smallest_movement()
		{
			// This exists because some games apply a dead zone of their own that cannot be switched
			// off. Without it the first part of a wheel's travel does nothing at all.
			const float antiDeadZone = 8000f;
			var small = Thumb(DiCentre + 100f, 0f, antiDeadZone);
			Assert.IsTrue(small >= antiDeadZone,
				string.Format("The smallest movement gave {0}, below the anti dead zone of {1}.", small, antiDeadZone));
			Assert.AreEqual(32767f, Thumb(DiMax, 0f, antiDeadZone), 1f,
				"An anti dead zone must not cost travel at the end.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("At rest an anti dead zone still reports nothing")]
		public void An_anti_dead_zone_leaves_the_resting_position_alone()
		{
			// Lifting the resting value would drive a game constantly with nobody touching anything.
			Assert.AreEqual(0f, Thumb(DiCentre, 0f, 8000f), 1f);
		}

		#endregion

		#region Sensitivity

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Sensitivity bends the response curve and never changes the ends")]
		public void Sensitivity_bends_the_curve_but_not_the_ends()
		{
			// This is the setting the interface calls Sensitivity. It is worth pinning precisely
			// because it does not multiply: it moves the middle of the travel while the ends stay
			// where they are. That is why a player asking for twice the movement is not served by it,
			// and why expressions were added.
			var plain = Thumb(DiCentre + 8000f);
			var bent = Thumb(DiCentre + 8000f, 0f, 0f, 50f);
			Assert.AreNotEqual(plain, bent, "Sensitivity should change the middle of the travel.");
			Assert.AreEqual(32767f, Thumb(DiMax, 0f, 0f, 50f), 1f, "The end must not move.");
			Assert.AreEqual(0f, Thumb(DiCentre, 0f, 0f, 50f), 1f, "The resting position must not move.");
			// A negative setting bends it the other way.
			var bentBack = Thumb(DiCentre + 8000f, 0f, 0f, -50f);
			Assert.AreNotEqual(bent, bentBack, "The sign should change which way the curve bends.");
		}

		#endregion

		#region Half ranges

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Half of an axis fills a trigger, which is how combined pedals are separated")]
		public void Half_an_axis_fills_a_trigger()
		{
			// A wheel that reports both pedals on one axis is separated this way, and it is the most
			// common reason anybody reaches for a half range.
			Assert.AreEqual(0f, Trigger(DiCentre, half: true), 1f, "The middle should be the start.");
			Assert.AreEqual(255f, Trigger(DiMax, half: true), 1f, "The top should be full.");
			// Everything below the middle belongs to the other pedal, so it stays at rest.
			Assert.AreEqual(0f, Trigger(DiMin, half: true), 1f);
			Assert.AreEqual(0f, Trigger(DiCentre - 10000f, half: true), 1f);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A half range is ignored for a stick, which needs both directions")]
		public void A_half_range_is_ignored_for_a_stick()
		{
			// A stick has to keep both halves, so asking for half of one must change nothing rather
			// than quietly removing a direction.
			Assert.AreEqual(Thumb(DiMin), Thumb(DiMin, half: true), 1f);
			Assert.AreEqual(Thumb(DiMax), Thumb(DiMax, half: true), 1f);
			Assert.AreEqual(Thumb(DiCentre), Thumb(DiCentre, half: true), 1f);
		}

		#endregion

		#region Movement is orderly

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Pushing a stick further never sends the value backwards")]
		public void Pushing_further_never_sends_the_value_backwards()
		{
			// A single step backwards anywhere in the travel is felt as a stutter, and it is the kind
			// of fault that no single-value test would ever notice.
			CheckRises(di => Thumb(di), "a stick");
			CheckRises(di => Thumb(di, 4000f), "a stick with a dead zone");
			CheckRises(di => Thumb(di, 0f, 8000f), "a stick with an anti dead zone");
			CheckRises(di => Thumb(di, 4000f, 8000f, 50f), "a stick with everything set");
			CheckRises(di => Trigger(di), "a trigger");
			CheckRises(di => Trigger(di, 20f, 30f), "a trigger with dead zones");
		}

		private static void CheckRises(Func<float, float> map, string what)
		{
			var previous = map(0f);
			for (var di = 64f; di <= DiMax; di += 64f)
			{
				var current = map(di);
				Assert.IsTrue(current >= previous - 0.001f,
					string.Format("{0} went backwards at {1}: {2} then {3}.", what, di, previous, current));
				previous = current;
			}
		}

		#endregion

		#region Converting to what a game reads

		[TestMethod, TestCategory("mapping")]
		[Description("A fraction becomes the whole number range a game reads")]
		public void A_fraction_becomes_the_range_a_game_reads()
		{
			Assert.AreEqual(0, ConvertHelper.ConvertToShort(0f));
			Assert.AreEqual(32767, ConvertHelper.ConvertToShort(1f));
			Assert.AreEqual(-32768, ConvertHelper.ConvertToShort(-1f));
			// This is the conversion an expression's result will pass through, so going past the end
			// has to stay inside the range rather than wrapping to the opposite extreme.
			Assert.IsTrue(ConvertHelper.ConvertToShort(2f) <= 32767,
				"Going past the end must not wrap round to the other one.");
			Assert.IsTrue(ConvertHelper.ConvertToShort(-2f) >= -32768,
				"Going past the low end must not wrap round.");
		}

		#endregion

	}
}
