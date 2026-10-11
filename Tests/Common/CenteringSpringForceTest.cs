// @under-test: Engine/Common/SpringCurve.cs, Engine/Common/ForceFeedbackState.cs, Engine/Data/UserDevice.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Checks the force the centering spring asks for at each wheel position: a car's steering, through
	/// nothing at the centre, half the strength at Centre, all of it at Peak and Lightening of it at the
	/// stop, smooth and never overshooting between them, the same either side, read at the car's speed,
	/// and never steeper at the centre than the wheel can follow.
	/// </summary>
	[TestClass]
	public class CenteringSpringForceTest
	{
		const int Center = SpringCalibration.Center;
		const int Band = ForceFeedbackState.SpringDeadBand;
		/// <summary>The farthest a position can be from the centre: the high stop.</summary>
		const int Stop = SpringCalibration.AxisMax - Center;

		/// <summary>The family car at 100 km/h: the settings left empty.</summary>
		static SpringCurve Curve(int strength, int centre = 12, int peak = 60, int lightening = 40, int range = 0, int speed = 100)
		{
			var curve = new SpringCurve();
			curve.Set(strength, centre, peak, lightening, range, speed);
			return curve;
		}

		/// <summary>The force towards the centre at a distance from it, towards the high end, so positive.</summary>
		static int At(SpringCurve curve, int distance)
		{
			return -curve.Force(Center + distance);
		}

		/// <summary>A steering angle as a distance from the centre in axis units, on a wheel of this range lock to lock.</summary>
		static int Units(double degrees, int range = ForceFeedbackState.DefaultRangeDegrees)
		{
			return (int)Math.Round(degrees * SpringCalibration.AxisMax / range);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The curve passes through its points: half the strength at Centre, all of it at Peak, Lightening of it at the stop")]
		public void The_curve_passes_through_its_points()
		{
			var curve = Curve(60);
			Assert.AreEqual(30, At(curve, Units(12)), "Half the strength at Centre.");
			Assert.AreEqual(60, At(curve, Units(60)), "All of it at Peak.");
			Assert.AreEqual(24, At(curve, Stop), "Lightening, 40 %, of it at the stop.");
			Assert.AreEqual(Units(12), curve.CentreUnits, "Centre was moved though the curve is not too steep.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Between the points the curve rises to Peak and falls after it, never above the strength")]
		public void The_curve_rises_to_its_peak_and_falls_after_it()
		{
			foreach (var strength in new[] { 25, 60, 100 })
			{
				var curve = Curve(strength);
				var peak = Units(60);
				var previous = 0;
				for (var distance = 0; distance <= Stop; distance++)
				{
					var force = At(curve, distance);
					if (distance <= peak)
						Assert.IsTrue(force >= previous, string.Format("At {0} % the force fell from {1} to {2} at {3}, before the peak.", strength, previous, force, distance));
					else
						Assert.IsTrue(force <= previous, string.Format("At {0} % the force rose from {1} to {2} at {3}, after the peak.", strength, previous, force, distance));
					Assert.IsTrue(force <= strength, string.Format("At {0} % the force was {1} at {2}.", strength, force, distance));
					previous = force;
				}
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The other side of the centre is the mirror, pointing the other way")]
		public void The_curve_is_mirrored()
		{
			var curve = Curve(80);
			for (var distance = 0; distance <= Center; distance++)
				Assert.AreEqual(-curve.Force(Center + distance), curve.Force(Center - distance), "Mirrored at " + distance);
			Assert.IsTrue(curve.Force(0) > 0, "Left of the centre the force pushes towards the high end.");
			Assert.IsTrue(curve.Force(SpringCalibration.AxisMax) < 0, "Right of the centre the force pushes towards the low end.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Inside the dead band there is no force, and just outside it the force is small")]
		public void Nothing_inside_the_dead_band()
		{
			var curve = Curve(100);
			Assert.AreEqual(0, curve.Force(Center));
			Assert.AreEqual(0, curve.Force(Center + Band));
			Assert.AreEqual(0, curve.Force(Center - Band));
			Assert.IsTrue(At(curve, Band + 1) < 10, "Just outside the dead band the force jumps to " + At(curve, Band + 1) + " %.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A strength of nought is no spring at all")]
		public void No_strength_no_force()
		{
			var curve = Curve(0);
			Assert.AreEqual(0, curve.Force(0));
			Assert.AreEqual(0, curve.Force(SpringCalibration.AxisMax));
			curve.Set(-5, 12, 60, 40, 0, 100);
			Assert.AreEqual(0, curve.Force(0));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Too steep at the centre for the wheel to follow, Centre is widened until the slope there is the limit")]
		public void The_steepness_cap_widens_Centre()
		{
			// The slope at the centre is half the strength over Centre; the limit is the stiffness limit over the ramp.
			var limitCentre = 100 / 2.0 * ForceFeedbackState.SpringRamp / ForceFeedbackState.SpringStiffnessLimit;
			var strong = Curve(100);
			Assert.IsTrue(strong.CentreUnits > Units(12), "At full strength 12 degrees is steeper than the limit and was kept.");
			Assert.AreEqual(limitCentre, strong.CentreUnits, 1, "Centre was widened further than the limit needs.");
			Assert.AreEqual(50, At(strong, strong.CentreUnits), "The widened Centre is still where half the strength is.");
			Assert.AreEqual(100, At(strong, Units(60)), "Widening Centre moved the peak.");
			// The secant from the centre to Centre is the slope at the centre, and it is never steeper than the limit.
			foreach (var strength in new[] { 30, 60, 100 })
			{
				var curve = Curve(strength, centre: 2);
				Assert.IsTrue(1000L * strength / 2 / curve.CentreUnits <= 1000L * ForceFeedbackState.SpringStiffnessLimit / ForceFeedbackState.SpringRamp + 1,
					string.Format("At {0} % with Centre at 2 degrees the curve is steeper at the centre than the limit.", strength));
			}
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The curve is read at the angle times the speed over 100, so a slower car's force builds over a wider angle")]
		public void The_speed_stretches_the_curve()
		{
			var town = Curve(60, speed: 40);
			Assert.AreEqual(Units(30), town.CentreUnits, 1, "At 40 km/h half the strength is at 12 / 0.4 = 30 degrees.");
			Assert.AreEqual(30, At(town, Units(30)));
			Assert.AreEqual(60, At(town, Units(150)), "At 40 km/h the peak is at 60 / 0.4 = 150 degrees.");
			// Below 10 km/h the axis inclination still centres the wheel, so 10 is the least speed used.
			var parked = Curve(60, speed: 10);
			Assert.AreEqual(Units(120), parked.CentreUnits, 1);
			var standing = Curve(60, speed: 0);
			for (var distance = 0; distance <= Stop; distance += 97)
				Assert.AreEqual(At(parked, distance), At(standing, distance), "Standing still is not the least speed at " + distance);
			// Faster, the curve is read beyond the stop, where it stays at Lightening.
			var fast = Curve(60, speed: 250);
			Assert.AreEqual(24, At(fast, Units(180)), "At 250 km/h the stop's 450 degrees are read at 180.");
			Assert.AreEqual(24, At(fast, Units(300)), "Beyond the stop the curve stays at Lightening.");
			Assert.AreEqual(24, At(fast, Stop));
		}

		[TestMethod, TestCategory("devices")]
		[Description("The steepness cap applies after the speed: a fast car's Centre is widened to the same slope")]
		public void The_steepness_cap_applies_after_the_speed()
		{
			// At 250 km/h, 12 degrees is read as 4.8, steeper than the limit at 60 %.
			var fast = Curve(60, speed: 250);
			var limitCentre = 60 / 2.0 * ForceFeedbackState.SpringRamp / ForceFeedbackState.SpringStiffnessLimit;
			Assert.AreEqual(limitCentre, fast.CentreUnits, 1, "The cap was applied before the speed.");
			Assert.AreEqual(30, At(fast, fast.CentreUnits));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Peak at nought, or at or beyond the stop, puts all the strength at the stop, with Lightening playing no part")]
		public void Peak_at_the_stop()
		{
			foreach (var peak in new[] { 0, 450, 600 })
			{
				var curve = Curve(60, peak: peak, lightening: 0);
				Assert.AreEqual(30, At(curve, Units(12)), "Peak " + peak + ": half at Centre.");
				Assert.AreEqual(60, At(curve, Stop), "Peak " + peak + ": all of it at the stop.");
				var previous = 0;
				for (var distance = 0; distance <= Stop; distance++)
				{
					var force = At(curve, distance);
					Assert.IsTrue(force >= previous, string.Format("Peak {0}: the force fell from {1} to {2} at {3}.", peak, previous, force, distance));
					previous = force;
				}
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("Degrees are counted against the wheel range, and a range left as it is counts as 900 degrees")]
		public void Degrees_are_counted_against_the_wheel_range()
		{
			Assert.AreEqual(Curve(60, range: 900).CentreUnits, Curve(60, range: 0).CentreUnits, "A range left as it is is not counted as 900 degrees.");
			var narrow = Curve(60, range: 200);
			Assert.AreEqual(Units(12, 200), narrow.CentreUnits, 1, "12 degrees of a 200 degree wheel is a wider part of its travel.");
			Assert.AreEqual(30, At(narrow, Units(12, 200)));
			Assert.AreEqual(60, At(narrow, Units(60, 200)));
			Assert.AreEqual(24, At(narrow, Stop), "Lightening at the 100 degree stop.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The settings left empty are the family car at 100 km/h, and the engine starts with them")]
		public void The_defaults_are_the_family_car()
		{
			var ps = new PadSetting();
			Assert.AreEqual(12, ps.GetForceSpringCentre(), "Centre");
			Assert.AreEqual(60, ps.GetForceSpringPeak(), "Peak");
			Assert.AreEqual(40, ps.GetForceSpringLightening(), "Lightening");
			Assert.AreEqual(5, ps.GetForceSpringDamping(), "Damping");
			Assert.AreEqual(100, ps.GetForceSpringSpeed(), "Speed");
			// A force state the settings have not reached yet builds its curve at the default speed.
			var ff = new ForceFeedbackState();
			ff.UpdateSpring(null, Center, null, 0, null);
			Assert.AreEqual(100, ff.Curve.Speed);
		}

		[TestMethod, TestCategory("devices")]
		[Description("The damping is the setting's share of what the device can do, the same at every angle")]
		public void The_damping_is_a_share_of_the_device()
		{
			Assert.AreEqual(500, ForceFeedbackState.SpringDamper(5), "The default damping.");
			Assert.AreEqual(0, ForceFeedbackState.SpringDamper(0));
			Assert.AreEqual(10000, ForceFeedbackState.SpringDamper(100));
			Assert.AreEqual(10000, ForceFeedbackState.SpringDamper(150), "More than everything the device can do.");
			Assert.AreEqual(0, ForceFeedbackState.SpringDamper(-5));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto sets the strength so the curve beats the wheel's friction from the stop to Centre, and a tenth more")]
		public void Auto_strength_beats_the_friction_from_the_stop_to_Centre()
		{
			// The lowest of the curve beyond Centre is half the strength, or Lightening of it at the stop.
			Assert.AreEqual(44, SpringCurve.AutoStrength(16, 40, 60, 900), "16 / 0.4 x 1.1 = 44.");
			Assert.AreEqual(36, SpringCurve.AutoStrength(16, 60, 60, 900), "16 / 0.5 x 1.1 = 35.2, rounded up.");
			Assert.AreEqual(36, SpringCurve.AutoStrength(16, 0, 0, 900), "With the peak at the stop Lightening plays no part.");
			Assert.AreEqual(36, SpringCurve.AutoStrength(16, 0, 450, 0), "A peak at the stop of a range left as it is.");
			Assert.AreEqual(100, SpringCurve.AutoStrength(16, 0, 60, 900), "Nothing left at the stop: the most there is.");
			Assert.AreEqual(100, SpringCurve.AutoStrength(60, 40, 60, 900), "More than the device has.");
			Assert.AreEqual(0, SpringCurve.AutoStrength(0, 40, 60, 900), "No friction found, no strength.");
			// And the curve at that strength does beat the friction from the stop to Centre.
			var curve = Curve(SpringCurve.AutoStrength(16, 40, 60, 900));
			for (var distance = curve.CentreUnits; distance <= Stop; distance++)
				Assert.IsTrue(At(curve, distance) > 16, "The curve is below the friction at " + distance);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A live speed set on the device replaces the Speed setting in the engine's curve, and nothing set leaves the setting")]
		public void A_live_speed_replaces_the_speed_setting()
		{
			var ud = new UserDevice();
			Assert.IsNull(ud.ForceSpringSpeedOverride, "A device starts with no live speed.");
			ud.ForceSpringSpeedOverride = 40;
			Assert.AreEqual(40, ud.ForceSpringSpeedOverride);
			var ff = new ForceFeedbackState();
			ff.UpdateSpring(null, Center, null, 0, ud.ForceSpringSpeedOverride);
			Assert.AreEqual(40, ff.Curve.Speed, "The engine's curve was not built at the live speed.");
			ud.ForceSpringSpeedOverride = null;
			Assert.IsNull(ud.ForceSpringSpeedOverride);
			ff.UpdateSpring(null, Center, null, 0, ud.ForceSpringSpeedOverride);
			Assert.AreEqual(100, ff.Curve.Speed, "With the live speed gone the curve was not built at the setting again.");
			ud.ForceSpringSpeedOverride = -20;
			Assert.AreEqual(0, ud.ForceSpringSpeedOverride, "A speed below nought is nought.");
		}
	}
}
