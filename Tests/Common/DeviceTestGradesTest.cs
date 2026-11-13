// @under-test: Engine/Input/Processors/DeviceTestGrades.cs, Engine/Input/Processors/DeviceTestSteps.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>The grading table, the overall grade, one scale for every input method, and the results an input method limits.</summary>
	[TestClass]
	public class DeviceTestGradesTest
	{
		static void Grades(DeviceTestCriterion criterion, params object[] valueThenGrade)
		{
			for (var i = 0; i < valueThenGrade.Length; i += 2)
			{
				var value = (double)valueThenGrade[i];
				Assert.AreEqual((DeviceTestGrade)valueThenGrade[i + 1], criterion.Grade(value), criterion.Name + " " + value);
			}
		}

		[TestMethod, TestCategory("devices")]
		[Description("Each result's value earns its grade at the table's limits, graded as shown: rounded to the decimals it is shown with")]
		public void Each_result_grades_at_its_limits()
		{
			Grades(DeviceTestGrades.UpdatesPerSecond, 1000.0, DeviceTestGrade.Best, 999.0, DeviceTestGrade.Good, 500.0, DeviceTestGrade.Good,
				499.0, DeviceTestGrade.Average, 250.0, DeviceTestGrade.Average, 125.0, DeviceTestGrade.Poor, 124.0, DeviceTestGrade.Bad, 999.6, DeviceTestGrade.Best);
			Grades(DeviceTestGrades.SteadyTiming, 99.0, DeviceTestGrade.Best, 98.9, DeviceTestGrade.Good, 95.0, DeviceTestGrade.Good,
				90.0, DeviceTestGrade.Average, 80.0, DeviceTestGrade.Poor, 79.9, DeviceTestGrade.Bad);
			Grades(DeviceTestGrades.StepSize, 0.0015, DeviceTestGrade.Best, 0.025, DeviceTestGrade.Best, 0.026, DeviceTestGrade.Good,
				0.1, DeviceTestGrade.Good, 0.4, DeviceTestGrade.Average, 0.8, DeviceTestGrade.Poor, 0.801, DeviceTestGrade.Bad);
			Grades(DeviceTestGrades.DeadZone, 1.0, DeviceTestGrade.Best, 3.0, DeviceTestGrade.Good, 6.0, DeviceTestGrade.Average,
				10.0, DeviceTestGrade.Poor, 10.1, DeviceTestGrade.Bad, 1.04, DeviceTestGrade.Best);
			Grades(DeviceTestGrades.CircleError, 2.0, DeviceTestGrade.Best, 5.0, DeviceTestGrade.Good, 8.0, DeviceTestGrade.Average,
				12.0, DeviceTestGrade.Poor, 12.1, DeviceTestGrade.Bad);
			Grades(DeviceTestGrades.JumpFromCentre, 1.0, DeviceTestGrade.Best, 3.0, DeviceTestGrade.Good, 6.0, DeviceTestGrade.Average,
				10.0, DeviceTestGrade.Poor, 10.1, DeviceTestGrade.Bad);
			Grades(DeviceTestGrades.FullRange, 100.0, DeviceTestGrade.Best, 99.97, DeviceTestGrade.Best, 99.9, DeviceTestGrade.Good,
				98.0, DeviceTestGrade.Good, 95.0, DeviceTestGrade.Average, 90.0, DeviceTestGrade.Poor, 89.9, DeviceTestGrade.Bad);
			CollectionAssert.AreEqual(new[] { "Updates per second", "Steady timing", "Step size", "Dead zone needed", "Circle error", "Jump from centre", "Full range" },
				DeviceTestGrades.Criteria.Select(x => x.Name).ToArray(), "The results, in the order they are shown.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The overall grade is the average of the graded results rounded toward the worse; a result limited or not measured counts toward nothing")]
		public void Overall_is_the_average_rounded_toward_the_worse()
		{
			var results = new List<DeviceTestResult>
			{
				new DeviceTestResult { Grade = DeviceTestGrade.Best },
				new DeviceTestResult { Grade = DeviceTestGrade.Good },
				new DeviceTestResult { LimitedBy = "XInput" },
				new DeviceTestResult(),
			};
			Assert.AreEqual(DeviceTestGrade.Good, DeviceTestGrades.Overall(results), "Best and Good average 0.5.");
			results.Add(new DeviceTestResult { Grade = DeviceTestGrade.Bad });
			Assert.AreEqual(DeviceTestGrade.Average, DeviceTestGrades.Overall(results), "Best, Good and Bad average 1.67.");
			var mostlyBest = new[] { DeviceTestGrade.Best, DeviceTestGrade.Best, DeviceTestGrade.Best, DeviceTestGrade.Good }.Select(x => new DeviceTestResult { Grade = x });
			Assert.AreEqual(DeviceTestGrade.Good, DeviceTestGrades.Overall(mostlyBest), "An average of 0.25 is rounded toward the worse.");
			Assert.IsNull(DeviceTestGrades.Overall(new[] { new DeviceTestResult { LimitedBy = "XInput" } }));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Every input method's values go on one scale, -32,768 to 32,767: DirectInput, Raw Input and plugins from 0 to 65,535, XInput's sticks as they are, and its triggers from 0 to 255")]
		public void Every_input_method_goes_on_one_scale()
		{
			foreach (var method in new[] { InputSourceType.DirectInput, InputSourceType.RawInput, InputSourceType.Plugin, InputSourceType.Unknown })
			{
				Assert.AreEqual(-32768, DeviceTestLayout.Normalise(method, 0, 0), method.ToString());
				Assert.AreEqual(0, DeviceTestLayout.Normalise(method, 0, 32768), method.ToString());
				Assert.AreEqual(32767, DeviceTestLayout.Normalise(method, 0, 65535), method.ToString());
				Assert.AreEqual(32767, DeviceTestLayout.Normalise(method, 2, 65535), method + " Z, an Xbox controller's triggers.");
			}
			Assert.AreEqual(-32768, DeviceTestLayout.Normalise(InputSourceType.XInput, 0, -32768));
			Assert.AreEqual(0, DeviceTestLayout.Normalise(InputSourceType.XInput, 1, 0));
			Assert.AreEqual(32767, DeviceTestLayout.Normalise(InputSourceType.XInput, 4, 32767));
			Assert.AreEqual(-32768, DeviceTestLayout.Normalise(InputSourceType.XInput, 2, 0), "A trigger let go.");
			Assert.AreEqual(32767, DeviceTestLayout.Normalise(InputSourceType.XInput, 5, 255), "A trigger pressed all the way.");
			Assert.AreEqual(128, DeviceTestLayout.Normalise(InputSourceType.XInput, 2, 128), "A trigger's 256 steps spread over the scale.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A control is named as the device names it, else as DirectInput does, never by a bare letter; an Xbox controller's one trigger axis is the combined triggers")]
		public void Controls_take_the_device_s_own_names()
		{
			var layout = new DeviceTestLayout { Names = new string[DeviceTestSteps.Values] };
			layout.Names[2] = "Throttle";
			layout.Names[5] = "Rudder";
			layout.Names[24] = "Slider";
			Assert.AreEqual("X Axis", layout.ChannelName(0));
			Assert.AreEqual("Y Rotation", layout.ChannelName(4));
			Assert.AreEqual("Throttle", layout.ChannelName(2));
			Assert.AreEqual("Rudder", layout.ChannelName(5));
			Assert.AreEqual("Slider", layout.ChannelName(24));
			Assert.AreEqual("Slider 2", layout.ChannelName(25));
			Assert.AreEqual("Axis 7", layout.ChannelName(6));
			layout.XInputCapable = true;
			Assert.AreEqual("Triggers (combined)", layout.ChannelName(2), "The name DirectInput gives an Xbox controller's Z hides that it is both triggers.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A result the input method limits says so in place of a grade: XInput's triggers have 256 steps, and DirectInput and Raw Input give an Xbox controller's two triggers one axis")]
		public void The_input_method_limits_what_it_cannot_give()
		{
			var xinput = new DeviceTestLayout { Method = InputSourceType.XInput };
			Assert.AreEqual("XInput", xinput.LimitsStep(2));
			Assert.AreEqual("XInput", xinput.LimitsStep(5));
			Assert.IsNull(xinput.LimitsStep(0), "XInput's sticks have 65,536 steps.");
			Assert.IsNull(xinput.LimitsControl(2), "XInput's triggers are measured, only not their steps.");
			Assert.AreEqual("Left trigger", xinput.ChannelName(2));
			foreach (var method in new[] { InputSourceType.DirectInput, InputSourceType.RawInput })
			{
				var xbox = new DeviceTestLayout { Method = method, XInputCapable = true };
				Assert.AreEqual(DeviceTestLayout.MethodName(method), xbox.LimitsControl(2));
				Assert.AreEqual("Triggers (combined)", xbox.ChannelName(2));
				Assert.IsNull(new DeviceTestLayout { Method = method }.LimitsControl(2), "Z is limited only on an Xbox controller.");
			}
			// An Xbox controller through DirectInput: its triggers are measured and shown, and left out of every grade.
			var s = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 0, 1, 2, 3, 4 }, XInputCapable = true });
			s.ToSticks();
			s.FillSticks();
			Assert.AreEqual(DeviceTestStep.Done, s.Step, "A device without buttons is done after its sticks and triggers.");
			var range = s.Steps.Results.Items.Single(x => x.Criterion == DeviceTestGrades.FullRange);
			Assert.AreEqual("Triggers (combined): limited by DirectInput", range.Limited);
			Assert.AreNotEqual("Triggers (combined)", range.Control);
			Assert.IsTrue(range.Grade.HasValue, "The sticks were not graded beside the limited triggers.");
			var step = s.Steps.Results.Items.Single(x => x.Criterion == DeviceTestGrades.StepSize);
			StringAssert.Contains(step.Limited, "Triggers (combined): limited by DirectInput");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A result whose every control is limited by the input method shows \"limited by\" it, has no grade, and counts toward nothing")]
		public void A_result_only_limited_controls_give_shows_the_limit()
		{
			var s = new DeviceTestScript(new DeviceTestLayout { Channels = new[] { 2 }, XInputCapable = true });
			s.ToSticks();
			s.Run(4000, s.Sweep(2));
			s.Run(200);
			Assert.AreEqual(DeviceTestStep.Done, s.Step);
			var range = s.Steps.Results.Items.Single(x => x.Criterion == DeviceTestGrades.FullRange);
			Assert.AreEqual("limited by DirectInput", range.ValueText);
			Assert.IsNull(range.Grade);
			Assert.IsFalse(s.Steps.Results.Items.Where(x => x.Criterion != DeviceTestGrades.UpdatesPerSecond && x.Criterion != DeviceTestGrades.SteadyTiming).Any(x => x.Grade.HasValue),
				"A limited control was graded.");
		}
	}
}
