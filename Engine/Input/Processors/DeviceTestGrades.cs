using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace x360ce.Engine
{
	/// <summary>A result's grade, from the best to the worst.</summary>
	public enum DeviceTestGrade
	{
		Best,
		Good,
		Average,
		Poor,
		Bad,
	}

	/// <summary>One thing a device test grades: what it tells a person, and the four limits between its five grades.</summary>
	public sealed class DeviceTestCriterion
	{
		/// <param name="decimals">The decimals the value is shown with, and graded at.</param>
		/// <param name="limits">The worst value that still earns Best, Good, Average and Poor; anything worse is Bad.</param>
		public DeviceTestCriterion(string name, string explanation, bool higherIsBetter, int decimals, string unit, params double[] limits)
		{
			Name = name;
			Explanation = explanation;
			HigherIsBetter = higherIsBetter;
			Decimals = decimals;
			Unit = unit;
			Limits = limits;
		}

		public string Name { get; }
		public string Explanation { get; }
		public bool HigherIsBetter { get; }
		public int Decimals { get; }
		public string Unit { get; }
		public double[] Limits { get; }

		/// <summary>The grade of a value as shown: rounded to <see cref="Decimals"/> first, so a value is graded as a person reads it.</summary>
		public DeviceTestGrade Grade(double value)
		{
			value = Math.Round(value, Decimals);
			for (var i = 0; i < Limits.Length; i++)
				if (HigherIsBetter ? value >= Limits[i] : value <= Limits[i])
					return (DeviceTestGrade)i;
			return DeviceTestGrade.Bad;
		}

		/// <summary>A value as shown: "1,000", "99.5 %".</summary>
		public string Text(double value)
		{
			var format = Decimals == 0 ? "N0" : "0." + new string('#', Decimals);
			return Math.Round(value, Decimals).ToString(format, CultureInfo.CurrentCulture) + Unit;
		}

		/// <summary>Of two values, whether the first is the worse.</summary>
		internal bool Worse(double a, double b)
		{
			return HigherIsBetter ? a < b : a > b;
		}
	}

	/// <summary>One graded result: its value and grade, the control it came from, or why it has no grade.</summary>
	public sealed class DeviceTestResult
	{
		public DeviceTestCriterion Criterion;

		/// <summary>The worst value of the controls graded; null when none was.</summary>
		public double? Value;
		public DeviceTestGrade? Grade;

		/// <summary>A smaller line of what the value is made of: "every 1.00 ms", "about 2,000 steps, 11 bits"; null without one.</summary>
		public string Detail;

		/// <summary>What the result tells, in a person's words: the criterion's, or one made from the value.</summary>
		public string Explanation;

		/// <summary>The control the value came from; null for the whole device.</summary>
		public string Control;

		/// <summary>The input method that limits every control measured for this result, which then has no grade.</summary>
		public string LimitedBy;

		/// <summary>The controls left out because their input method limits them, named with it: "Triggers (Z): limited by DirectInput".</summary>
		public string Limited;

		/// <summary>The value as shown, or why there is none: "limited by XInput", "not measured".</summary>
		public string ValueText
		{
			get
			{
				if (Value.HasValue)
					return Criterion.Text(Value.Value);
				return LimitedBy != null ? "limited by " + LimitedBy : "not measured";
			}
		}
	}

	/// <summary>A device test's graded results, its overall grade and its weakest result.</summary>
	public sealed class DeviceTestResults
	{
		public List<DeviceTestResult> Items = new List<DeviceTestResult>();
		public DeviceTestGrade? Overall;
		/// <summary>The result graded worst, when one is worse than Best.</summary>
		public DeviceTestResult Weakest;
	}

	/// <summary>
	/// The grading table, and the grading of a finished test: each result's worst value across the controls measured, its
	/// grade, and the overall grade.
	/// </summary>
	/// <remarks>
	/// The limits are starting values, to be tuned on real controllers. A control that was not finished, or never moved, is
	/// not graded; a result the input method itself limits shows that in place of a grade and counts toward nothing.
	/// </remarks>
	public static class DeviceTestGrades
	{
		public static readonly DeviceTestCriterion UpdatesPerSecond = new DeviceTestCriterion("Updates per second",
			"How often the controller sends its state.", true, 0, "", 1000, 500, 250, 125);
		public static readonly DeviceTestCriterion SteadyTiming = new DeviceTestCriterion("Steady timing",
			"How evenly updates arrive: within 10 % of their usual interval. Uneven timing feels like stutter.", true, 1, " %", 99, 95, 90, 80);
		public static readonly DeviceTestCriterion StepSize = new DeviceTestCriterion("Step size",
			"An axis's smallest change, as a share of its travel: smaller is finer.", false, 3, " %", 0.025, 0.1, 0.4, 0.8);
		public static readonly DeviceTestCriterion DeadZone = new DeviceTestCriterion("Dead zone needed",
			"Drift and rest off centre: the dead zone that hides them.", false, 1, " %", 1, 3, 6, 10);
		public static readonly DeviceTestCriterion CircleError = new DeviceTestCriterion("Circle error",
			"How far a stick's edge is from a true circle.", false, 1, " %", 2, 5, 8, 12);
		public static readonly DeviceTestCriterion JumpFromCentre = new DeviceTestCriterion("Jump from centre",
			"The first change as a stick leaves its centre, of the way to its edge.", false, 1, " %", 1, 3, 6, 10);
		public static readonly DeviceTestCriterion FullRange = new DeviceTestCriterion("Full range",
			"How much of each axis's travel was reached.", true, 1, " %", 100, 98, 95, 90);

		/// <summary>The grading table, in the order the results are shown.</summary>
		public static readonly DeviceTestCriterion[] Criteria = { UpdatesPerSecond, SteadyTiming, StepSize, DeadZone, CircleError, JumpFromCentre, FullRange };

		/// <summary>The overall grade: the average of the graded results, rounded toward the worse; null with none graded.</summary>
		public static DeviceTestGrade? Overall(IEnumerable<DeviceTestResult> results)
		{
			var grades = results.Where(x => x.Grade.HasValue).Select(x => (int)x.Grade.Value).ToList();
			if (grades.Count == 0)
				return null;
			return (DeviceTestGrade)(int)Math.Ceiling(grades.Average() - 1e-9);
		}

		/// <summary>Grades a finished test.</summary>
		public static DeviceTestResults Grade(DeviceTestSteps steps)
		{
			var summary = steps.Statistics.Summary(0);
			var channels = steps.Statistics.Channels;
			var layout = steps.Layout;
			var graded = steps.Controls.Where(x => x.Complete).ToList();
			var c = CultureInfo.CurrentCulture;
			var results = new DeviceTestResults();
			var rate = Device(UpdatesPerSecond, summary.ReportRate);
			var steady = Device(SteadyTiming, summary.OnTimePercent);
			if (summary.MedianInterval.HasValue)
			{
				var interval = summary.MedianInterval.Value;
				rate.Detail = string.Format(c, "every {0:0.00} ms", interval);
				steady.Detail = string.Format(c, "{0:0.##} ms apart, \u00B1{1:0.##} ms", interval, interval / 10);
			}
			if (summary.OnTimePercent.HasValue)
				steady.Explanation = string.Format(c, "{0:N0} of every 1,000 updates arrive on time. Uneven timing feels like stutter.", Math.Round(summary.OnTimePercent.Value * 10));
			results.Items.Add(rate);
			results.Items.Add(steady);
			var step = new List<Candidate>();
			var deadZone = new List<Candidate>();
			var circle = new List<Candidate>();
			var jump = new List<Candidate>();
			var range = new List<Candidate>();
			foreach (var control in graded)
			{
				var axes = control.IsStick ? new[] { control.X, control.Y } : new[] { control.X };
				foreach (var axis in axes)
				{
					var fine = channels[axis.Channel].FineStep;
					if (fine.HasValue && fine.Value > 0)
					{
						var count = Rounded(65536.0 / fine.Value);
						step.Add(new Candidate(control.Name, 100.0 * fine.Value / ushort.MaxValue, layout.LimitsStep(axis.Channel),
							string.Format(c, "about {0:N0} steps \u00B7 {1:0} bits", count, Math.Log(count, 2))));
					}
				}
				var narrowest = axes.OrderBy(x => x.RangePercent).First();
				range.Add(new Candidate(control.Name, narrowest.RangePercent, control.LimitedBy, string.Format(c, "{0:N0} to {1:N0}", narrowest.Min, narrowest.Max)));
				if (control.IsStick)
				{
					var reached = control.Pair.Radius.Where(x => x >= DeviceTestSteps.EdgeRadius).ToArray();
					var mean = reached.Length > 0 ? reached.Average() : 1;
					var edge = reached.Length > 0 ? string.Format(c, "edge {0:+0.0;-0.0;0} % to {1:+0.0;-0.0;0} % of a true circle", 100 * (reached.Min() / mean - 1), 100 * (reached.Max() / mean - 1)) : null;
					circle.Add(new Candidate(control.Name, control.Pair.CircleError, control.LimitedBy, edge));
				}
				if (!control.Centred)
					continue;
				// A rest's reach and a first step are percents of the range; from the centre the way to the edge is half of it.
				var stats = axes.Select(x => channels[x.Channel]).ToArray();
				var drift = stats.Max(x => x.NoiseSpread ?? 0) / 2.0;
				var off = stats.Max(x => Math.Abs(x.CentreOffset ?? 0));
				deadZone.Add(new Candidate(control.Name, Worst(stats.Select(x => x.DeadZonePercent), DeadZone), control.LimitedBy,
					string.Format(c, "drift \u00B1{0:N0} \u00B7 centre off by {1:N0}", drift, off)));
				var first = stats.Max(x => x.FirstStep ?? 0);
				jump.Add(new Candidate(control.Name, Worst(stats.Select(x => x.FirstStepPercent * 2), JumpFromCentre), control.LimitedBy,
					string.Format(c, "first change {0:N0} of 32,767", first)));
			}
			results.Items.Add(Controls(StepSize, step));
			results.Items.Add(Controls(DeadZone, deadZone));
			results.Items.Add(Controls(CircleError, circle));
			results.Items.Add(Controls(JumpFromCentre, jump));
			results.Items.Add(Controls(FullRange, range));
			results.Overall = Overall(results.Items);
			// The weakest is the first of the worst grade, when any is worse than Best.
			foreach (var result in results.Items.Where(x => x.Grade.HasValue && x.Grade.Value != DeviceTestGrade.Best))
				if (results.Weakest == null || result.Grade.Value > results.Weakest.Grade.Value)
					results.Weakest = result;
			return results;
		}

		/// <summary>A count to two figures, as "about" one says it: 2,048 is 2,000, 65,536 is 66,000.</summary>
		static double Rounded(double count)
		{
			var scale = Math.Pow(10, Math.Max(0, Math.Floor(Math.Log10(count)) - 1));
			return Math.Round(count / scale) * scale;
		}

		/// <summary>A value of one control for one result, the input method that limits it, if any, and what it is made of.</summary>
		sealed class Candidate
		{
			public Candidate(string control, double? value, string limitedBy, string detail = null)
			{
				Control = control;
				Value = value;
				LimitedBy = limitedBy;
				Detail = detail;
			}

			public readonly string Control;
			public readonly double? Value;
			public readonly string LimitedBy;
			public readonly string Detail;
		}

		static DeviceTestResult Device(DeviceTestCriterion criterion, double? value)
		{
			return new DeviceTestResult { Criterion = criterion, Explanation = criterion.Explanation, Value = value, Grade = value.HasValue ? criterion.Grade(value.Value) : (DeviceTestGrade?)null };
		}

		/// <summary>The worst value of the controls the input method does not limit; the limited ones are named apart.</summary>
		static DeviceTestResult Controls(DeviceTestCriterion criterion, List<Candidate> candidates)
		{
			var result = new DeviceTestResult { Criterion = criterion, Explanation = criterion.Explanation };
			var limited = candidates.Where(x => x.LimitedBy != null).ToList();
			if (limited.Count > 0)
				result.Limited = string.Join("; ", limited.Select(x => x.Control + ": limited by " + x.LimitedBy));
			foreach (var candidate in candidates.Where(x => x.LimitedBy == null && x.Value.HasValue))
				if (!result.Value.HasValue || criterion.Worse(candidate.Value.Value, result.Value.Value))
				{
					result.Value = candidate.Value;
					result.Control = candidate.Control;
					result.Detail = candidate.Detail;
				}
			if (result.Value.HasValue)
				result.Grade = criterion.Grade(result.Value.Value);
			else if (limited.Count > 0)
				result.LimitedBy = limited[0].LimitedBy;
			return result;
		}

		static double? Worst(IEnumerable<double?> values, DeviceTestCriterion criterion)
		{
			double? worst = null;
			foreach (var value in values.Where(x => x.HasValue))
				if (!worst.HasValue || criterion.Worse(value.Value, worst.Value))
					worst = value;
			return worst;
		}
	}
}
