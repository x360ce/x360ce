// @under-test: Engine/Common/SpringCalibration.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Drives the Auto button's run against a pretend wheel that only moves once the force beats
	/// its friction, and checks that the run finds that friction and adds its margin.
	/// </summary>
	[TestClass]
	public class SpringCalibrationTest
	{
		/// <summary>A wheel with friction: it moves at a fixed speed whenever the force exceeds the friction, else it stays.</summary>
		class Wheel
		{
			public int Position = SpringCalibration.Center;
			public int FrictionLow;
			public int FrictionHigh;
			/// <summary>Axis units per millisecond, fast enough to cross the wheel within one ramp step.</summary>
			public int Speed = 400;

			public void Move(int force)
			{
				// Friction differs by side of the wheel, as it does on a real one.
				var friction = Position < SpringCalibration.Center ? FrictionLow : FrictionHigh;
				if (Math.Abs(force) <= friction)
					return;
				Position = Math.Max(0, Math.Min(SpringCalibration.AxisMax, Position + Math.Sign(force) * Speed));
			}
		}

		/// <summary>Runs one millisecond at a time until the run finishes, or reaches the phase asked for, or the time runs out.</summary>
		/// <returns>The time reached.</returns>
		static long Run(SpringCalibration run, Wheel wheel, SpringCalibration.Phase until = SpringCalibration.Phase.Done, long maxMs = 60000)
		{
			long ms = 0;
			for (; ms < maxMs && !run.IsFinished && run.Step != until; ms++)
			{
				var force = run.Update(wheel.Position, ms);
				Assert.IsTrue(Math.Abs(force) <= 100, "Asked for " + force + " %, which is more than the device has.");
				wheel.Move(force);
			}
			return ms;
		}

		[TestMethod, TestCategory("devices")]
		[Description("The result is the friction found plus a margin, rounded up")]
		public void Finds_the_weakest_force_that_moves_the_wheel()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(13, run.LowLevel, "The first level above the friction is the one that moved the wheel.");
			Assert.AreEqual(13, run.HighLevel);
			Assert.AreEqual(14, run.Result, "Thirteen plus five percent, rounded up.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The stiffer side of the wheel decides the result")]
		public void Takes_the_larger_of_the_two_sides()
		{
			var wheel = new Wheel { FrictionLow = 10, FrictionHigh = 20 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(11, run.LowLevel);
			Assert.AreEqual(21, run.HighLevel);
			Assert.AreEqual(23, run.Result, "Twenty one plus five percent, rounded up.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The wheel is pushed to the low stop first, then brought home, then pushed to the high stop")]
		public void Pushes_low_first_and_ramps_towards_the_centre()
		{
			var wheel = new Wheel { FrictionLow = 5, FrictionHigh = 5 };
			var run = new SpringCalibration();
			Assert.IsTrue(run.Update(wheel.Position, 0) < 0, "The first push is towards the low end of the axis.");
			var ms = Run(run, wheel, SpringCalibration.Phase.PushToHigh);
			Assert.AreEqual(SpringCalibration.Phase.PushToHigh, run.Step,
				"After the low stop and the ramp home, the run pushes to the high stop. The wheel sits at " + wheel.Position + ".");
			Assert.IsTrue(wheel.Position >= SpringCalibration.Center - SpringCalibration.AxisMax / 100, "The ramp brought the wheel home first.");
			Assert.IsTrue(run.Update(wheel.Position, ms) > 0, "The second push is towards the high end.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel that never reaches its stop fails with a reason, and no force is left on")]
		public void A_wheel_that_will_not_move_fails_with_a_reason()
		{
			var wheel = new Wheel { FrictionLow = 100, FrictionHigh = 100 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Failed, run.Step);
			Assert.AreEqual(0, run.Result);
			StringAssert.Contains(run.Message, "stop");
			Assert.AreEqual(0, run.Update(wheel.Position, 100000), "A finished run asks for no force.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Cancel ends the run on the next poll with no force and no result")]
		public void Cancel_stops_the_run()
		{
			var wheel = new Wheel { FrictionLow = 5, FrictionHigh = 5 };
			var run = new SpringCalibration();
			run.Update(wheel.Position, 0);
			run.Cancel();
			Assert.AreEqual(0, run.Update(wheel.Position, 1));
			Assert.IsTrue(run.IsFinished);
			Assert.AreEqual(SpringCalibration.Phase.Failed, run.Step);
			Assert.AreEqual(0, run.Result);
		}
	}
}
