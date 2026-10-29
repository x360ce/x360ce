// @under-test: Engine/Common/SpringCalibration.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs, App.v4/Controls/PadControl.cs, App.v4/Common/DInput/DeviceRouting.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

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

		/// <summary>Runs one millisecond at a time from <paramref name="startMs"/> until the run finishes, or reaches the phase asked for, or the time runs out.</summary>
		/// <returns>The time reached.</returns>
		static long Run(SpringCalibration run, Wheel wheel, SpringCalibration.Phase until = SpringCalibration.Phase.Done, long maxMs = 60000, long startMs = 0)
		{
			var ms = startMs;
			for (; ms - startMs < maxMs && !run.IsFinished && run.Step != until; ms++)
			{
				var force = run.Update(wheel.Position, ms);
				Assert.IsTrue(Math.Abs(force) <= 100, "Asked for " + force + " %, which is more than the device has.");
				wheel.Move(force);
			}
			return ms;
		}

		[TestMethod, TestCategory("devices")]
		[Description("The result is the force that started the wheel and a tenth more, rounded up")]
		public void Finds_the_weakest_force_that_moves_the_wheel()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration();
			Run(run, wheel);
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(13, run.LowLevel, "The first level above the friction is the one that moved the wheel.");
			Assert.AreEqual(13, run.HighLevel);
			Assert.AreEqual(15, run.Result, "Thirteen and a tenth more, 14.3, rounded up: the extra that starts the wheel every time.");
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
			Assert.AreEqual(15, run.Result);
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
			Assert.AreEqual(24, run.Result, "Twenty one and a tenth more, 23.1, rounded up.");
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
		[Description("The answer is checked from a stop with the spring the engine plays, the friction and the pad's Strength on top, and its damping, before it is handed over")]
		public void The_answer_is_checked_before_it_is_given()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration(new PadSetting { ForceSpringStrength = "30" });
			var ms = Run(run, wheel, SpringCalibration.Phase.Check);
			Assert.AreEqual(SpringCalibration.Phase.Check, run.Step, "After both ramps the run goes to a stop and lets the spring bring the wheel home.");
			Assert.AreEqual(15, run.Result, "The friction checked is not the one the ramps found.");
			Assert.AreEqual(30, run.Strength, "The check runs with another Strength than the pad's.");
			StringAssert.Contains(run.Status, "Checking 15 % friction with Strength 30 %", "The page is told what the run is doing: " + run.Status);
			Assert.IsTrue(wheel.Position <= SpringCalibration.AxisMax * 15 / 100, "The check starts from a stop, not from the centre.");
			var damped = false;
			for (; !run.IsFinished; ms++)
			{
				wheel.Move(run.Update(wheel.Position, ms));
				damped |= run.Damping > 0;
			}
			Assert.IsTrue(damped, "The check ran without the damping the spring will run with.");
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.AreEqual(15, run.Result);
			Assert.AreEqual(30, run.Strength, "The run changed Strength.");
			Assert.AreEqual(0, run.Crossings, "A wheel pushed home by just over its friction stops at the centre, it does not pass it.");
			Assert.AreEqual(0, run.Damping, "A finished run leaves no damping on.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The friction found does not depend on the pad's curve, and the check runs with the pad's own Strength and damping")]
		public void The_run_uses_the_pads_own_curve_and_damping()
		{
			var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
			var run = new SpringCalibration(new PadSetting { ForceSpringStrength = "50", ForceSpringLightening = "20", ForceSpringDamping = "0" });
			var ms = Run(run, wheel, SpringCalibration.Phase.Check);
			Assert.AreEqual(15, run.Result, "The friction does not depend on the curve.");
			Assert.AreEqual(50, run.Strength, "The check runs with another Strength than the pad's.");
			var damped = false;
			for (; !run.IsFinished; ms++)
			{
				wheel.Move(run.Update(wheel.Position, ms));
				damped |= run.Damping > 0;
			}
			Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, run.Message);
			Assert.IsFalse(damped, "The check ran with damping the pad does not have.");
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

		[TestMethod, TestCategory("devices")]
		[Description("The engine times the run with a clock that is never negative and never goes back, read only while a run is under way")]
		public void The_engine_times_the_run_at_any_uptime()
		{
			// The run marks a time not yet taken with -1, so it needs times from 0 up. Environment.TickCount
			// is negative from 24.9 to 49.7 days after Windows starts: timed by it, no phase ends, and the
			// wheel is pushed against its stop until the program closes. The engine's own pass clock starts
			// again whenever the update thread does, which a run under way would read as time going back.
			// Every force feedback device takes this call on every poll, so the clock is read only for a run,
			// and the run is read once, so one started between two reads is never handed time 0.
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs");
			// The call itself, not its name in quotes, which the engine gives the call for error reports and a recording.
			var call = Regex.Match(File.ReadAllText(path), @"(?<!"")ud\.FFState\.UpdateSpring\((?<args>[^;]*)\);");
			Assert.IsTrue(call.Success, "The engine's call to UpdateSpring was not found.");
			var args = call.Groups["args"].Value;
			Assert.IsTrue(Regex.IsMatch(args, @",\s*(?<run>\w+),\s*\k<run> != null \? SpringCalibrationClock\.ElapsedMilliseconds : 0,\s*ud\.ForceSpringSpeedOverride,\s*ud\.GetForceSpringFriction\(\)$"),
				"The engine times the run with a clock that can be negative or go back, or reads it for every device: " + args);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A run finishes the same whatever the time the engine's clock stands at when it starts")]
		public void A_run_finishes_the_same_at_any_time_the_engine_passes()
		{
			// The engine's clock counts up from the program's start and never goes back, so a run can start
			// seconds in or long past the 24.9 days a tick count holds before it turns negative.
			foreach (var startMs in new[] { 1000L, 40L * 24 * 60 * 60 * 1000 })
			{
				var wheel = new Wheel { FrictionLow = 12, FrictionHigh = 12 };
				var run = new SpringCalibration();
				Run(run, wheel, startMs: startMs);
				Assert.AreEqual(SpringCalibration.Phase.Done, run.Step, "Started at " + startMs + " ms: " + run.Message);
				Assert.AreEqual(15, run.Result, "Started at " + startMs + " ms.");
			}
		}

		/// <summary>The current game, with Controller 1 switched on or off.</summary>
		static UserGame Game(bool tabOn)
		{
			return new UserGame { FileName = "spring.exe", EnableMask = tabOn ? (int)MapToMask.Controller1 : 0 };
		}

		/// <summary>A wheel's row on Controller 1 with force feedback on, ticked or not in the tab's list.</summary>
		static UserSetting Row(PadSetting ps, bool ticked)
		{
			return new UserSetting
			{
				InstanceGuid = Guid.NewGuid(),
				FileName = "spring.exe",
				MapTo = (int)MapTo.Controller1,
				IsEnabled = ticked,
				PadSettingChecksum = ps.PadSettingChecksum,
			};
		}

		/// <summary>Whether the Auto button starts a run, asked the way it asks: the engine drives the run only for a device whose spring the routing takes from this tab.</summary>
		/// <param name="device">The row's device as the devices list holds it, or null for none listed.</param>
		static bool AutoStarts(UserGame game, UserSetting row, PadSetting ps, UserDevice device = null)
		{
			DeviceForce route;
			return DeviceRouting.Build(game, new[] { row }, new[] { ps }, device == null ? null : new[] { device }).TryGetForce(row.InstanceGuid, out route)
				&& route.SettingsPad == row.MapTo - 1;
		}

		/// <summary>Checks that the Auto button asks the routing, and says why, before it starts a run.</summary>
		static void AssertAutoAsksTheRouting()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadControl.cs");
			var source = File.ReadAllText(path);
			var click = source.IndexOf("void ForceSpringAutoButton_Click(");
			var routed = source.IndexOf("if (!SpringFromThisTab(ud))", click);
			var reason = source.IndexOf("WheelDescriptionLabel.Text = \"Auto needs this controller tab switched on", click);
			var start = source.IndexOf("driven.SpringCalibration = new SpringCalibration(GetSelectedPadSetting(), driven.ForceSpringSpeedOverride, driven);", click);
			Assert.IsTrue(click > 0 && routed > click && reason > routed && start > reason,
				"Auto starts a run the engine never drives, and the button waits for it forever.");
			// The engine runs it on the device whose motors it drives, which for a Raw Input device is its DirectInput twin.
			var driven = source.IndexOf("var driven = ud == null ? null : DInput.DeviceRouting.Current.ForceDevice(ud);", click);
			Assert.IsTrue(driven > click && driven < start, "Auto puts its run on a device the engine does not drive.");
			var rule = Ui.Between(source, "bool SpringFromThisTab(UserDevice ud)", "public static string SpringNote(");
			StringAssert.Contains(rule, "routing.TryGetForce(driven.InstanceGuid, out route)", "Auto does not ask the routing about the device it drives.");
			StringAssert.Contains(rule, "route.SettingsPad == (int)MappedTo - 1", "Auto does not ask whether the routing takes the spring from this tab.");
			Assert.IsFalse(source.Contains("(game.EnableMask & (int)AppHelper.GetMapFlag(MappedTo)) == 0"),
				"Auto repeats part of the routing rule instead of asking the routing.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto is not started on a controller tab that is switched off")]
		public void Auto_does_not_start_on_a_tab_switched_off()
		{
			// A tab switched off drives nothing, so the engine never drives the run, and the button would
			// wait, greyed, for a run that can never end. The reason is shown where the other reasons are.
			var ps = new PadSetting { ForceEnable = "1", PadSettingChecksum = Guid.NewGuid() };
			Assert.IsTrue(AutoStarts(Game(true), Row(ps, true), ps), "Auto does not start where the engine drives it.");
			Assert.IsFalse(AutoStarts(Game(false), Row(ps, true), ps), "Auto starts on a tab switched off.");
			AssertAutoAsksTheRouting();
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The spring works without force feedback's Enable, which passes on the game's vibration: Auto starts, and the engine drives the wheel for the spring alone, with no tab sending it vibration")]
		public void The_spring_needs_no_Enable()
		{
			var ps = new PadSetting { ForceEnable = "0", ForceSpringEnable = "1", PadSettingChecksum = Guid.NewGuid() };
			var row = Row(ps, true);
			Assert.IsTrue(AutoStarts(Game(true), row, ps), "Auto does not start with Enable unticked, though the spring is ticked.");
			DeviceForce route;
			Assert.IsTrue(DeviceRouting.Build(Game(true), new[] { row }, new[] { ps }).TryGetForce(row.InstanceGuid, out route));
			Assert.IsTrue(route.Drives, "The engine leaves the wheel alone while only the spring is ticked.");
			Assert.AreEqual(0, route.ForcePads.Length, "The game's vibration reaches the wheel with Enable unticked.");
			ps.ForceSpringEnable = "0";
			Assert.IsFalse(DeviceRouting.Build(Game(true), new[] { row }, new[] { ps }).TryGetForce(row.InstanceGuid, out route) && route.Drives,
				"The engine drives a wheel with neither Enable nor the spring ticked.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A wheel's own centering is on while the spring is off and off while it holds; a pad's is left as its driver has it")]
		public void Only_a_device_that_centres_itself_gets_its_centering_switched_on()
		{
			Func<SharpDX.DirectInput.DeviceType, int, bool, bool?> rule = (type, motors, spring) => DInputHelper.AutoCenterFor((int)type, motors, spring);
			Assert.AreEqual(true, rule(SharpDX.DirectInput.DeviceType.Driving, 1, false), "A wheel loses its own centering while the spring is off.");
			Assert.AreEqual(false, rule(SharpDX.DirectInput.DeviceType.Driving, 1, true), "A wheel's own centering fights the spring.");
			Assert.AreEqual(true, rule(SharpDX.DirectInput.DeviceType.Joystick, 1, false), "A wheel that calls itself a joystick loses its own centering.");
			Assert.AreEqual(true, rule(SharpDX.DirectInput.DeviceType.Flight, 2, false), "A flight stick loses its own centering.");
			// A pad with two rumble motors, reported either way, is left as versions before the spring left it.
			Assert.IsNull(rule(SharpDX.DirectInput.DeviceType.Joystick, 2, false), "A two-motor pad's centering is switched.");
			Assert.IsNull(rule(SharpDX.DirectInput.DeviceType.Gamepad, 2, false), "A gamepad's centering is switched.");
			Assert.AreEqual(false, rule(SharpDX.DirectInput.DeviceType.Gamepad, 2, true), "The spring holds a pad against the pad's own centering.");
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "DInputHelper.Step2.UpdateDiStates.cs"));
			StringAssert.Contains(source, "AutoCenterFor(ud.CapType, ud.DiActuatorCount, mapped && route.Spring)", "The engine holds a device without the rule.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto is not started for a device unticked in the tab's list")]
		public void Auto_does_not_start_for_an_unticked_row()
		{
			// An unticked row reaches nothing, so the engine never drives the run either.
			var ps = new PadSetting { ForceEnable = "1", PadSettingChecksum = Guid.NewGuid() };
			Assert.IsFalse(AutoStarts(Game(true), Row(ps, false), ps), "Auto starts for a device unticked in the tab's list.");
			AssertAutoAsksTheRouting();
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto is not started for a device unticked on the Devices page, and says so")]
		public void Auto_does_not_start_for_a_device_unticked_on_the_Devices_page()
		{
			var ps = new PadSetting { ForceEnable = "1", PadSettingChecksum = Guid.NewGuid() };
			var row = Row(ps, true);
			var device = new UserDevice { InstanceGuid = row.InstanceGuid };
			Assert.IsTrue(AutoStarts(Game(true), row, ps, device), "Auto does not start for a device ticked on the Devices page.");
			device.IsEnabled = false;
			Assert.IsFalse(AutoStarts(Game(true), row, ps, device), "Auto starts for a device unticked on the Devices page.");
			AssertAutoAsksTheRouting();
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadControl.cs"));
			StringAssert.Contains(Ui.Between(source, "WheelDescriptionLabel.Text = \"Auto needs this controller tab switched on", "\";"), "Devices page",
				"The reason Auto gives leaves out the tick on the Devices page.");
		}
	}
}
