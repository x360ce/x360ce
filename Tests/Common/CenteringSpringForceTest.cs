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
			var ramp = ForceFeedbackState.SpringRampFor(strength);
			var lowest = Math.Abs(Force(Center + Band + 1, strength));
			Assert.IsTrue(lowest > 0 && lowest <= strength / ForceFeedbackState.SpringRampSteps(strength) + 1,
				"Just outside the dead band the force is one step, not full strength: " + lowest);
			var previous = 0;
			for (var distance = Band + 1; distance <= ramp; distance++)
			{
				var force = -Force(Center + distance, strength);
				Assert.IsTrue(force >= previous, "The force fell from " + previous + " to " + force + " at " + distance);
				Assert.IsTrue(force <= strength, "The force exceeded the strength at " + distance);
				previous = force;
			}
			Assert.AreEqual(strength, previous, "At the end of the ramp the force is the full strength.");
			// The same curve on the other side, pointing the other way.
			for (var distance = Band + 1; distance <= ramp; distance += 37)
				Assert.AreEqual(-Force(Center + distance, strength), Force(Center - distance, strength), "Mirrored at " + distance);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A strength of nought is no spring at all")]
		public void No_strength_no_force()
		{
			Assert.AreEqual(0, Force(0, 0));
			Assert.AreEqual(0, Force(SpringCalibration.AxisMax, -5));
		}

		[TestMethod, TestCategory("devices")]
		[Description("No step of the ramp is larger than the cap, at any strength")]
		public void No_ramp_step_is_larger_than_the_cap()
		{
			// At full strength eight steps were a twelve percent blow each; the wheel chattered on one under a finger.
			foreach (var strength in new[] { 5, 26, 50, 100 })
			{
				var previous = 0;
				for (var distance = Band + 1; distance <= ForceFeedbackState.SpringRampFor(strength); distance++)
				{
					var force = -Force(Center + distance, strength);
					Assert.IsTrue(force - previous <= ForceFeedbackState.SpringStepPercent,
						string.Format("At {0} % the force jumped from {1} to {2} at {3}.", strength, previous, force, distance));
					previous = force;
				}
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("Above the stiffness limit the ramp widens with the strength, so the force per unit of travel never grows")]
		public void The_ramp_widens_so_the_spring_gets_no_stiffer()
		{
			var limit = ForceFeedbackState.SpringStiffnessLimit;
			Assert.AreEqual(Ramp, ForceFeedbackState.SpringRampFor(26), "Auto's usual answer keeps the narrow ramp and the close rest.");
			Assert.AreEqual(Ramp, ForceFeedbackState.SpringRampFor(limit));
			Assert.AreEqual(Ramp * 2, ForceFeedbackState.SpringRampFor(limit * 2), "Twice the strength, twice the ramp.");
			// Force per unit of travel at the end of the ramp, in thousandths, never above the limit's.
			var stiffnessAtLimit = 1000L * limit / ForceFeedbackState.SpringRampFor(limit);
			foreach (var strength in new[] { 60, 75, 100 })
				Assert.IsTrue(1000L * strength / ForceFeedbackState.SpringRampFor(strength) <= stiffnessAtLimit, strength + " % is stiffer than the limit.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The damping is one value: the same at every angle and at every strength")]
		public void Damping_is_the_same_at_every_angle_and_strength()
		{
			// An edge in the damping was felt as a border in the wheel, a third of the way out, where
			// the resistance dropped for no reason a hand could see. Scaled with the strength instead,
			// full strength asked the device for everything it had and the wheel ground its way home.
			Assert.AreEqual(0, ForceFeedbackState.DamperFor(0), "No spring, no damping.");
			Assert.AreEqual(ForceFeedbackState.DamperCoefficient, ForceFeedbackState.DamperFor(28));
			Assert.AreEqual(ForceFeedbackState.DamperFor(28), ForceFeedbackState.DamperFor(100), "A stronger spring must not brake the wheel harder.");
			Assert.IsTrue(ForceFeedbackState.DamperCoefficient * 2 <= 10000, "The damping is a fraction of what the device can do, not all of it.");
		}
	}
}
