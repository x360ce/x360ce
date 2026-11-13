// @under-test: Engine/Input/Processors/DeviceTestSteps.cs, Engine/Input/Processors/DeviceTestGrades.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>A scripted device on a made-up clock of a tick a millisecond: a read every millisecond, the steps told the time every 100.</summary>
	internal sealed class DeviceTestScript
	{
		public DeviceTestScript(DeviceTestLayout layout, params int[] restingAtZero)
		{
			Steps = new DeviceTestSteps(layout, 1000);
			for (var i = 0; i < Values.Length; i++)
				Values[i] = restingAtZero.Contains(i) ? 0 : 32768;
			_rest = (int[])Values.Clone();
		}

		/// <summary>A DirectInput gamepad: two sticks (X, Y and X, Y Rotation), Z and four buttons.</summary>
		public static DeviceTestLayout Gamepad()
		{
			return new DeviceTestLayout { Channels = new[] { 0, 1, 2, 3, 4 }, ButtonCount = 4 };
		}

		public readonly DeviceTestSteps Steps;
		public readonly int[] Values = new int[DeviceTestSteps.Values];
		public readonly bool[] Buttons = new bool[128];
		readonly int[] _rest;
		long _reads;

		/// <summary>The time, in milliseconds.</summary>
		public long Now { get; private set; }

		public DeviceTestStep Step { get { return Steps.Step; } }

		public void Start()
		{
			Steps.Start(Now);
		}

		/// <summary>Runs for a while, one read a millisecond, each moved first by <paramref name="move"/>, which gets the milliseconds since the run began.</summary>
		public void Run(int milliseconds, Action<int> move = null)
		{
			for (var t = 1; t <= milliseconds; t++)
			{
				Now++;
				if (move != null)
					move(t);
				_reads++;
				Steps.AddReport(Now, Values, Buttons);
				if (Now % 100 == 0)
				{
					Steps.SetReads(Now, _reads);
					Steps.Tick(Now);
				}
			}
		}

		/// <summary>Puts every value back where it rests.</summary>
		public void Rest()
		{
			Array.Copy(_rest, Values, Values.Length);
		}

		/// <summary>Turns two axes round the edge, a turn every <paramref name="period"/> milliseconds, after moving out from the centre in the first 300.</summary>
		public Action<int> Circle(int x, int y, int period = 2000, double edge = 1)
		{
			return t =>
			{
				var angle = 2 * Math.PI * t / period;
				var radius = edge * Math.Min(1, t / 300.0);
				Values[x] = Clamp(32768 + radius * 32767 * Math.Cos(angle));
				Values[y] = Clamp(32768 + radius * 32767 * Math.Sin(angle));
			};
		}

		/// <summary>Swings an axis from its rest to each end and back, a swing every <paramref name="period"/> milliseconds.</summary>
		public Action<int> Sweep(int axis, int period = 2000)
		{
			return t => Values[axis] = Clamp(32768 + 32767 * Math.Sin(2 * Math.PI * t / period));
		}

		/// <summary>Presses a button for 50 milliseconds and lets it go for 150.</summary>
		public void Press(int button)
		{
			Buttons[button] = true;
			Run(50);
			Buttons[button] = false;
			Run(150);
		}

		static int Clamp(double value)
		{
			return (int)Math.Max(0, Math.Min(65535, Math.Round(value)));
		}

		/// <summary>Lies still until the sticks and triggers step: get ready, rest and pick up.</summary>
		public void ToSticks()
		{
			Start();
			Run((DeviceTestSteps.GetReadySeconds + DeviceTestSteps.RestSeconds + DeviceTestSteps.PickUpSeconds) * 1000 + 100);
			Assert.AreEqual(DeviceTestStep.Sticks, Step, "Lying still did not lead to sticks and triggers.");
		}

		/// <summary>Turns both sticks and swings Z, one at a time, until the buttons step.</summary>
		public void FillSticks()
		{
			Run(4000, Circle(0, 1));
			Rest();
			Run(4000, Circle(3, 4));
			Rest();
			Run(4000, Sweep(2));
			Rest();
			Run(200);
		}
	}

	/// <summary>The steps of a device test, on scripted streams: what restarts them, what moves them on, and what holds them.</summary>
	[TestClass]
	public class DeviceTestStepsTest
	{
		[TestMethod, TestCategory("devices")]
		[Description("Get ready counts down from 10 while the controller lies still, and starts again, saying so, when it moves more than noise or a button is pressed")]
		public void Get_ready_restarts_its_countdown_when_the_controller_moves()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.Start();
			Assert.AreEqual(DeviceTestStep.GetReady, s.Step);
			Assert.AreEqual(1, s.Steps.StepNumber);
			s.Run(5000, t => s.Values[0] = 32768 + (t % 7) * 50);
			Assert.AreEqual("5", s.Steps.Countdown, "Noise of 300 started the countdown again.");
			s.Values[0] = 40000;
			s.Run(100);
			Assert.AreEqual(DeviceTestStep.GetReady, s.Step);
			Assert.AreEqual("10", s.Steps.Countdown, "Moving did not start the countdown again.");
			StringAssert.StartsWith(s.Steps.Hint, "Still moving");
			s.Run(5000);
			s.Buttons[1] = true;
			s.Run(100);
			Assert.AreEqual("10", s.Steps.Countdown, "A button did not start the countdown again.");
			s.Run(10000);
			Assert.AreEqual(DeviceTestStep.Rest, s.Step, "Ten still seconds did not lead to the rest.");
			Assert.AreEqual("", s.Steps.Hint, "\"Still moving\" stayed once the controller lay still.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A controller that moves during the rest goes back to get ready, told why, so a rest figure is never one of a moving controller")]
		public void Rest_goes_back_to_get_ready_when_the_controller_moves()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.Start();
			s.Run(10100);
			Assert.AreEqual(DeviceTestStep.Rest, s.Step);
			Assert.AreEqual(2, s.Steps.StepNumber);
			s.Run(2000);
			s.Values[3] = 20000;
			s.Run(100);
			Assert.AreEqual(DeviceTestStep.GetReady, s.Step);
			Assert.AreEqual("The controller moved: put it down and let go.", s.Steps.Hint);
			s.Rest();
			s.Run(10100);
			Assert.AreEqual(DeviceTestStep.Rest, s.Step);
			s.Run(5000);
			Assert.AreEqual(DeviceTestStep.PickUp, s.Step);
			s.Run(5000, t => s.Values[0] = 32768 + (t % 5000) * 6);
			Assert.AreEqual(DeviceTestStep.Sticks, s.Step, "Pick up did not move on after its 5 seconds, moving or not.");
			Assert.IsTrue(s.Steps.Statistics.Channels.All(x => !x.MovedAtRest), "A rest figure is one of a moving controller.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Sticks and triggers pair X with Y and X Rotation with Y Rotation, and move on to the buttons by themselves once every bar is full")]
		public void Sticks_move_on_once_every_bar_is_full()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			CollectionAssert.AreEqual(new[] { "Left stick (X Axis, Y Axis)", "Right stick (X Rotation, Y Rotation)", "Z Axis" }, s.Steps.Controls.Select(x => x.Name).ToArray());
			s.Run(4000, s.Circle(0, 1));
			Assert.IsTrue(s.Steps.Controls[0].Complete, "Two turns round the edge did not fill the left stick.");
			Assert.AreEqual(1.0, s.Steps.Controls[0].Progress);
			Assert.IsTrue(s.Steps.Controls[0].Pair.CircleError.Value < 2, "A true circle has a circle error of " + s.Steps.Controls[0].Pair.CircleError);
			s.Rest();
			s.Run(4000, s.Circle(3, 4));
			s.Rest();
			s.Run(4000, s.Sweep(2));
			s.Rest();
			s.Run(200);
			Assert.AreEqual(DeviceTestStep.Buttons, s.Step, "Every bar was full and the step did not move on.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A control moved but not yet full holds the step for as long as it takes, with no countdown, and the hint tells the person to keep moving it")]
		public void A_control_half_done_holds_the_step()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			// A quarter turn: the left end and the top are never reached.
			s.Run(1000, s.Circle(0, 1, 4000));
			s.Run(60000);
			Assert.AreEqual(DeviceTestStep.Sticks, s.Step, "A stick half done let the step move on.");
			Assert.AreEqual("", s.Steps.Countdown, "A countdown started while a stick was half done.");
			Assert.AreEqual("Keep turning Left stick (X Axis, Y Axis) round its edge until the circle is full.", s.Steps.Hint);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Once every control moved is full, 15 seconds without movement start \"Moving on in 10\"; movement cancels it, and at its end the controls never moved count as not moved")]
		public void Controls_never_moved_count_as_absent_after_the_countdown()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.Run(4000, s.Circle(0, 1));
			s.Rest();
			s.Run(200);
			Assert.AreEqual("Left stick (X Axis, Y Axis) \u2713. Next: Right stick (X Rotation, Y Rotation).", s.Steps.Hint);
			s.Run(DeviceTestSteps.IdleSeconds * 1000);
			Assert.AreEqual("Moving on in 10", s.Steps.CountdownCaption + " " + s.Steps.Countdown);
			s.Run(3000);
			Assert.AreEqual("Moving on in 7", s.Steps.CountdownCaption + " " + s.Steps.Countdown);
			s.Values[0] = 50000;
			s.Run(100);
			Assert.AreEqual("", s.Steps.Countdown, "Moving a control did not cancel the countdown.");
			s.Rest();
			s.Run((DeviceTestSteps.IdleSeconds + DeviceTestSteps.CountdownSeconds) * 1000 + 100);
			Assert.AreEqual(DeviceTestStep.Buttons, s.Step);
			CollectionAssert.AreEqual(new[] { false, true, true }, s.Steps.Controls.Select(x => x.NotMoved).ToArray());
		}

		[TestMethod, TestCategory("devices")]
		[Description("Two axes that turn round together become a stick whatever their slots; two triggers pressed together, a wheel's X and Y, and X and Y resting at their ends, as pedals do, do not")]
		public void Stick_axes_pair_by_moving_together()
		{
			var s = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 0, 1, 2, 5 } });
			s.ToSticks();
			CollectionAssert.AreEqual(new[] { "Stick (X Axis, Y Axis)", "Z Axis", "Z Rotation" }, s.Steps.Controls.Select(x => x.Name).ToArray());
			s.Run(2000, s.Circle(2, 5));
			s.Run(100);
			CollectionAssert.AreEqual(new[] { "Left stick (X Axis, Y Axis)", "Right stick (Z Axis, Z Rotation)" }, s.Steps.Controls.Select(x => x.Name).ToArray());
			var triggers = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 2, 5 } }, 2, 5);
			triggers.ToSticks();
			triggers.Run(3000, t => triggers.Values[2] = triggers.Values[5] = (int)(65535 * Math.Abs(Math.Sin(Math.PI * t / 1000))));
			CollectionAssert.AreEqual(new[] { "Z Axis", "Z Rotation" }, triggers.Steps.Controls.Select(x => x.Name).ToArray(), "Two triggers pressed together became a stick.");
			var wheel = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 0, 1 }, Wheel = true });
			wheel.ToSticks();
			CollectionAssert.AreEqual(new[] { "X Axis", "Y Axis" }, wheel.Steps.Controls.Select(x => x.Name).ToArray(), "A wheel's X and Y were taken for a stick.");
			var pedals = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 0, 1 } }, 0, 1);
			pedals.ToSticks();
			CollectionAssert.AreEqual(new[] { "X Axis", "Y Axis" }, pedals.Steps.Controls.Select(x => x.Name).ToArray(), "Two pedals resting at their ends were taken for a stick, which they could never fill.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The buttons step ends as soon as every button is ticked, and the test is graded")]
		public void Buttons_finish_once_every_button_is_ticked()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.FillSticks();
			Assert.AreEqual(DeviceTestStep.Buttons, s.Step);
			Assert.AreEqual(5, s.Steps.StepNumber);
			for (var b = 0; b < 3; b++)
				s.Press(b);
			Assert.AreEqual("3 of 4 pressed. Next: button 4.", s.Steps.Hint);
			s.Press(3);
			Assert.AreEqual(DeviceTestStep.Done, s.Step);
			Assert.IsTrue(s.Steps.Buttons.All(x => x.Pressed && !x.Twice));
			Assert.IsNotNull(s.Steps.Results);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Ten seconds without a press start \"Finishing in 10\"; a press cancels it, and at its end the buttons never pressed count as not pressed")]
		public void Buttons_never_pressed_count_as_absent_after_the_countdown()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.FillSticks();
			s.Press(0);
			s.Press(1);
			s.Run(DeviceTestSteps.ButtonsIdleSeconds * 1000);
			Assert.AreEqual("Finishing in 10", s.Steps.CountdownCaption + " " + s.Steps.Countdown);
			s.Press(0);
			Assert.AreEqual("", s.Steps.Countdown, "A press did not cancel the countdown.");
			s.Run((DeviceTestSteps.ButtonsIdleSeconds + DeviceTestSteps.CountdownSeconds) * 1000);
			Assert.AreEqual(DeviceTestStep.Done, s.Step);
			CollectionAssert.AreEqual(new[] { false, false, true, true }, s.Steps.Buttons.Select(x => x.NotPressed).ToArray());
		}

		[TestMethod, TestCategory("devices")]
		[Description("A button let go and pressed again within 30 ms is one push registered twice; pressed again later, it is a second push")]
		public void One_push_registered_twice_is_flagged()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.FillSticks();
			s.Buttons[0] = true;
			s.Run(40);
			s.Buttons[0] = false;
			s.Run(12);
			s.Buttons[0] = true;
			s.Run(40);
			s.Buttons[0] = false;
			s.Run(100);
			s.Press(1);
			s.Press(1);
			Assert.IsTrue(s.Steps.Buttons[0].Twice);
			Assert.AreEqual(12, s.Steps.Buttons[0].TwiceAfter, 0.001);
			Assert.IsFalse(s.Steps.Buttons[1].Twice, "Two pushes 150 ms apart were taken for one.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Stop ends the test at once: before the controls were measured there are no results, after they were the measured ones are graded")]
		public void Stop_ends_the_test_and_grades_what_was_measured()
		{
			var early = new DeviceTestScript(DeviceTestScript.Gamepad());
			early.Start();
			early.Run(3000);
			early.Steps.Stop(early.Now);
			Assert.AreEqual(DeviceTestStep.Done, early.Step);
			Assert.IsTrue(early.Steps.Stopped);
			Assert.IsNull(early.Steps.Results);
			var late = new DeviceTestScript(DeviceTestScript.Gamepad());
			late.ToSticks();
			late.Run(4000, late.Circle(0, 1));
			late.Steps.Stop(late.Now);
			Assert.IsNotNull(late.Steps.Results);
			Assert.IsNotNull(late.Steps.Results.Items.Single(x => x.Criterion == DeviceTestGrades.CircleError).Grade, "The finished stick was not graded.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The whole test on a scripted gamepad: still through get ready and rest, both sticks and Z moved till full, every button pressed; every result is graded, Best for a perfect device")]
		public void The_full_sequence_runs_and_grades_a_scripted_device()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.FillSticks();
			for (var b = 0; b < 4; b++)
				s.Press(b);
			Assert.AreEqual(DeviceTestStep.Done, s.Step);
			Assert.IsFalse(s.Steps.Stopped);
			var results = s.Steps.Results;
			foreach (var item in results.Items)
				Console.WriteLine("{0}: {1} {2} {3}", item.Criterion.Name, item.ValueText, item.Grade, item.Control);
			Assert.AreEqual(DeviceTestGrades.Criteria.Length, results.Items.Count);
			Assert.IsTrue(results.Items.All(x => x.Grade.HasValue), "A result of the scripted device was not graded.");
			Assert.AreEqual(1000, results.Items[0].Value.Value, 0.5, "Updates per second.");
			Assert.AreEqual(DeviceTestGrade.Best, results.Overall);
			Assert.IsNull(results.Weakest, "A weakest result was named where every one is Best.");
			var steady = results.Items.Single(x => x.Criterion == DeviceTestGrades.SteadyTiming);
			Assert.AreEqual("Steady timing", steady.Criterion.Name);
			Assert.AreEqual("1,000 of every 1,000 updates arrive on time. Uneven timing feels like stutter.", steady.Explanation);
			Assert.AreEqual("1 ms apart, \u00B10.1 ms", steady.Detail);
			Assert.AreEqual("every 1.00 ms", results.Items[0].Detail);
			StringAssert.EndsWith(results.Items.Single(x => x.Criterion == DeviceTestGrades.StepSize).Detail, "bits");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Skip ends sticks and triggers at once, the controls not full marked skipped and not graded; in the buttons step it ends the test, the buttons not pressed marked skipped")]
		public void Skip_ends_the_step_and_marks_what_is_not_done()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			Assert.IsTrue(s.Steps.CanSkip);
			s.Run(4000, s.Circle(0, 1));
			s.Rest();
			// The right stick half done: it would hold the step for as long as it takes.
			s.Run(1000, s.Circle(3, 4, 4000));
			s.Steps.Skip(s.Now);
			Assert.AreEqual(DeviceTestStep.Buttons, s.Step);
			CollectionAssert.AreEqual(new[] { false, true, true }, s.Steps.Controls.Select(x => x.Skipped).ToArray());
			s.Press(0);
			s.Steps.Skip(s.Now);
			Assert.AreEqual(DeviceTestStep.Done, s.Step);
			Assert.IsFalse(s.Steps.CanSkip);
			CollectionAssert.AreEqual(new[] { false, true, true, true }, s.Steps.Buttons.Select(x => x.Skipped).ToArray());
			Assert.AreEqual("Left stick (X Axis, Y Axis)", s.Steps.Results.Items.Single(x => x.Criterion == DeviceTestGrades.CircleError).Control, "A skipped stick was graded.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("An axis that keeps changing for 20 s without getting fuller, as a drifting or flickering one does, is marked unstable and holds the step no longer")]
		public void A_control_changing_without_progress_turns_unstable()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.Run(4000, s.Circle(0, 1));
			s.Rest();
			s.Run(4000, s.Circle(3, 4));
			s.Rest();
			// Z jumps between two places away from both ends, again and again: it never fills.
			// It gets fuller until its 100 changes are counted, 10 s, then stays: 20 s more and it is unstable.
			s.Run((DeviceTestSteps.UnstableSeconds + 13) * 1000, t => s.Values[2] = (t / 100) % 2 == 0 ? 20000 : 45000);
			Assert.AreEqual(DeviceTestStep.Buttons, s.Step, "A flickering axis held the step.");
			var z = s.Steps.Controls.Single(x => x.Name == "Z Axis");
			Assert.IsTrue(z.Unstable);
			Assert.IsFalse(z.Complete);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A paused control is no unstable one: a stick half done and left alone still holds the step")]
		public void A_paused_control_is_not_unstable()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.Run(1000, s.Circle(0, 1, 4000));
			s.Run((DeviceTestSteps.UnstableSeconds + 10) * 1000);
			Assert.AreEqual(DeviceTestStep.Sticks, s.Step);
			Assert.IsFalse(s.Steps.Controls[0].Unstable);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A button pressed again and again by itself, with no new button ticked for 20 s, is marked unstable; its presses no longer keep the step waiting, so the buttons never pressed count as not pressed after the countdown")]
		public void A_flickering_button_turns_unstable_and_lets_the_step_end()
		{
			var s = new DeviceTestScript(DeviceTestScript.Gamepad());
			s.ToSticks();
			s.FillSticks();
			s.Press(0);
			s.Press(2);
			s.Run((DeviceTestSteps.UnstableSeconds + DeviceTestSteps.ButtonsIdleSeconds + DeviceTestSteps.CountdownSeconds + 2) * 1000, t => s.Buttons[1] = (t / 50) % 2 == 0);
			Assert.AreEqual(DeviceTestStep.Done, s.Step, "A flickering button held the step.");
			Assert.IsTrue(s.Steps.Buttons[1].Unstable);
			Assert.IsTrue(s.Steps.Buttons[3].NotPressed);
		}
	}
}
