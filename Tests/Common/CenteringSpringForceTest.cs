// @under-test: Engine/Common/ForceFeedbackState.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Checks the force the centering spring asks for at each wheel position: the same strength
	/// everywhere, towards the centre, nothing inside the dead band, and nothing again until a
	/// wheel at rest there has moved well away.
	/// </summary>
	[TestClass]
	public class CenteringSpringForceTest
	{
		const int Center = SpringCalibration.Center;
		const int Band = ForceFeedbackState.SpringDeadBand;

		static int Force(int position, int strength)
		{
			var resting = false;
			return ForceFeedbackState.SpringForce(position, strength, ref resting);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Left of centre pushes towards the high end, right of centre towards the low end, at full strength either way")]
		public void Pushes_towards_the_centre_at_the_same_strength_everywhere()
		{
			Assert.AreEqual(20, Force(0, 20), "At the low stop.");
			Assert.AreEqual(20, Force(Center - 2000, 20), "Near the centre, still full strength.");
			Assert.AreEqual(-20, Force(SpringCalibration.AxisMax, 20), "At the high stop.");
			Assert.AreEqual(-20, Force(Center + 2000, 20));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Inside the dead band there is no force, so the wheel rests instead of chattering across the centre")]
		public void No_force_inside_the_dead_band()
		{
			Assert.AreEqual(0, Force(Center, 50));
			Assert.AreEqual(0, Force(Center + Band, 50));
			Assert.AreEqual(0, Force(Center - Band, 50));
			Assert.AreNotEqual(0, Force(Center + Band + 1, 50));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel at rest in the dead band is let alone until it is twice the band away")]
		public void A_resting_wheel_is_let_alone_until_it_is_well_away()
		{
			var resting = false;
			Assert.AreEqual(0, ForceFeedbackState.SpringForce(Center, 30, ref resting));
			Assert.IsTrue(resting);
			Assert.AreEqual(0, ForceFeedbackState.SpringForce(Center + Band + 1, 30, ref resting), "Just over the band, a resting wheel stays let alone.");
			Assert.IsTrue(resting);
			Assert.AreEqual(-30, ForceFeedbackState.SpringForce(Center + Band * 2 + 1, 30, ref resting), "Twice the band away, the spring takes hold again.");
			Assert.IsFalse(resting);
			Assert.AreEqual(-30, ForceFeedbackState.SpringForce(Center + Band + 1, 30, ref resting), "And keeps hold until the wheel is back inside the band.");
			Assert.AreEqual(0, ForceFeedbackState.SpringForce(Center + Band, 30, ref resting));
			Assert.IsTrue(resting);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A strength of nought is no spring at all")]
		public void Nought_is_off()
		{
			Assert.AreEqual(0, Force(0, 0));
			Assert.AreEqual(0, Force(SpringCalibration.AxisMax, -5));
		}
	}
}
