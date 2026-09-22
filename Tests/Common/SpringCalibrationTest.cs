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

			public virtual void Move(int force)
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
		[Description("The result is the force that started the wheel plus a little extra")]
		public void Finds_the_weakest_force_that_moves_the_wheel()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(13, run.LowLevel, "The first level above the friction is the one that moved the wheel.");
			Assert.AreEqual(13, run.HighLevel);
			Assert.AreEqual(16, run.Result, "Thirteen, plus the extra that starts the wheel every time.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel already moving home gets no more force, however long it takes to get there")]
		public void A_wheel_already_moving_home_gets_no_more_force()
		{
			// Thirty units a millisecond: over a second from the stop to the centre, four ramp steps'
			// worth, and just inside the pace that counts. The level must stay where the wheel started moving.
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12, Speed = 30 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(13, run.LowLevel, "The force rose while the wheel was already on its way.");
			Assert.AreEqual(13, run.HighLevel);
			Assert.AreEqual(16, run.Result);
			Assert.AreEqual(13, run.LowTurnsAt, "The level the wheel first turned at is told apart from the level that brings it home.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel that only creeps home under the least force that moves it is given more, until it comes home at a fair pace")]
		public void A_creeping_wheel_is_given_more_until_it_comes_home_at_pace()
		{
			// Five units a millisecond however hard it is pushed: six and a half seconds to the centre,
			// under the pace that counts. The level must go on rising while it creeps, as it did when it stood.
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12, Speed = 5 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.IsTrue(run.LowLevel >= 20, "The wheel was let creep home at level " + run.LowLevel + ".");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel that stalls on the way is given more, from where it stalled")]
		public void A_wheel_that_stalls_on_the_way_is_given_more()
		{
			// Stiffer in the middle than at the stops: the level that starts the wheel is not the one that gets it home.
			var wheel = new StickyWheel { FrictionLow = 10, FrictionHigh = 10, FrictionNearCentre = 20, Speed = 30 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(21, run.LowLevel, "The wheel stalled at the stickier part and the level did not follow.");
			Assert.AreEqual(21, run.HighLevel);
			Assert.AreEqual(11, run.LowTurnsAt, "The wheel turned at the stop long before it got home.");
		}

		/// <summary>A wheel whose friction rises near the centre, as a worn gear train's does.</summary>
		class StickyWheel : Wheel
		{
			public int FrictionNearCentre;

			public override void Move(int force)
			{
				var nearCentre = Math.Abs(Position - SpringCalibration.Center) < SpringCalibration.AxisMax / 4;
				if (nearCentre && Math.Abs(force) <= FrictionNearCentre)
					return;
				base.Move(force);
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("The second side starts from the first side's answer, since the result takes the larger anyway")]
		public void The_second_side_starts_from_the_first_sides_answer()
		{
			var wheel = new Wheel { FrictionLow = 20, FrictionHigh = 5 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(21, run.LowLevel);
			Assert.AreEqual(21, run.HighLevel, "The easier side was ramped from nought, which learns nothing the result uses.");
			Assert.AreEqual(24, run.Result);
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
			Assert.AreEqual(24, run.Result, "Twenty one plus the extra.");
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
		[Description("The answer is checked from a stop with the spring's own ramp and damping before it is handed over")]
		public void The_answer_is_checked_before_it_is_given()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration();
			var ms = Run(run, wheel, SpringCalibration.Phase.Check);
			Assert.AreEqual(SpringCalibration.Phase.Check, run.Step, "After both ramps the run goes to a stop and lets the spring bring the wheel home.");
			StringAssert.Contains(run.Status, "Checking 16 %", "The page is told what the run is doing: " + run.Status);
			Assert.IsTrue(wheel.Position <= SpringCalibration.AxisMax * 15 / 100, "The check starts from a stop, not from the centre.");
			var damped = false;
			for (; !run.IsFinished; ms++)
			{
				wheel.Move(run.Update(wheel.Position, ms));
				damped |= run.Damping > 0;
			}
			Assert.IsTrue(damped, "The check ran without the damping the spring will run with.");
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(16, run.Result);
			Assert.AreEqual(0, run.Crossings, "A wheel that stops in the ramp never passes the centre.");
			Assert.AreEqual(0, run.Damping, "A finished run leaves no damping on.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A wheel that swings through the centre at the answer is refused, with the count")]
		public void A_wheel_that_swings_at_the_answer_is_refused()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration();
			var ms = Run(run, wheel, SpringCalibration.Phase.Check);
			// From here the wheel is a pendulum: a side every 200 ms, which no spring setting caused and none can hide.
			var side = -1;
			for (var start = ms; !run.IsFinished; ms++)
			{
				if ((ms - start) % 200 == 0)
					side = -side;
				run.Update(SpringCalibration.Center + side * SpringCalibration.AxisMax / 10, ms);
			}
			Assert.AreEqual(SpringCalibration.Phase.Failed, run.Step);
			Assert.AreEqual(0, run.Result, "A refused answer is not handed over as a number.");
			Assert.IsTrue(run.Crossings > 3, "Crossed " + run.Crossings + " times.");
			StringAssert.Contains(run.Message, "swings");
			StringAssert.Contains(run.Message, run.Crossings.ToString());
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
