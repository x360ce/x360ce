// @under-test: Engine/Common/ForceFeedbackState.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Checks the force the centering spring asks for at each wheel position: full strength beyond
	/// the ramp, towards the centre; nothing inside the dead band; and in between a force that grows
	/// with the distance in steps, so there is no edge at which it switches between nothing and full.
	/// </summary>
	[TestClass]
	public class CenteringSpringForceTest
	{
		const int Center = SpringCalibration.Center;
		const int Band = ForceFeedbackState.SpringDeadBand;
		const int Ramp = ForceFeedbackState.SpringRamp;

		static int Force(int position, int strength)
		{
			return ForceFeedbackState.SpringForce(position, strength);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Beyond the ramp, left of centre pushes towards the high end and right of centre towards the low end, at full strength either way")]
		public void Full_strength_beyond_the_ramp()
		{
			Assert.AreEqual(20, Force(0, 20), "At the low stop.");
			Assert.AreEqual(20, Force(Center - Ramp, 20), "At the edge of the ramp.");
			Assert.AreEqual(-20, Force(SpringCalibration.AxisMax, 20), "At the high stop.");
			Assert.AreEqual(-20, Force(Center + Ramp, 20));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Inside the dead band there is no force")]
		public void Nothing_inside_the_dead_band()
		{
			Assert.AreEqual(0, Force(Center, 50));
			Assert.AreEqual(0, Force(Center + Band, 50));
			Assert.AreEqual(0, Force(Center - Band, 50));
			Assert.AreNotEqual(0, Force(Center + Band + 1, 50));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Inside the ramp the force grows with the distance, never falls, never exceeds the strength, and starts small")]
		public void The_force_grows_over_the_ramp()
		{
			var strength = 80;
			var lowest = Math.Abs(Force(Center + Band + 1, strength));
			Assert.IsTrue(lowest > 0 && lowest <= strength / ForceFeedbackState.SpringRampSteps + 1,
				"Just outside the dead band the force is one step, not full strength: " + lowest);
			var previous = 0;
			for (var distance = Band + 1; distance <= Ramp; distance++)
			{
				var force = -Force(Center + distance, strength);
				Assert.IsTrue(force >= previous, "The force fell from " + previous + " to " + force + " at " + distance);
				Assert.IsTrue(force <= strength, "The force exceeded the strength at " + distance);
				previous = force;
			}
			Assert.AreEqual(strength, previous, "At the end of the ramp the force is the full strength.");
			// The same curve on the other side, pointing the other way.
			for (var distance = Band + 1; distance <= Ramp; distance += 37)
				Assert.AreEqual(-Force(Center + distance, strength), Force(Center - distance, strength), "Mirrored at " + distance);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A strength of nought is no spring at all")]
		public void No_strength_no_force()
		{
			Assert.AreEqual(0, Force(0, 0));
			Assert.AreEqual(0, Force(SpringCalibration.AxisMax, -5));
		}
	}
}
