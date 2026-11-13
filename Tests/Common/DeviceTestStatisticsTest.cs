// @under-test: Engine/Input/Processors/DeviceTestStatistics.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>Every figure of the Test Device window, worked out from made-up reads whose answers are known.</summary>
	/// <remarks>The clock ticks a million times a second, so a tick is a microsecond.</remarks>
	[TestClass]
	public class DeviceTestStatisticsTest
	{
		const long Second = 1000000;
		const long Ms = 1000;
		const int Middle = 32767;

		/// <summary>Statistics for two axes, started at <paramref name="start"/> with both at <paramref name="value"/>.</summary>
		static DeviceTestStatistics Started(int value = Middle, long start = 0)
		{
			var s = new DeviceTestStatistics(2, Second);
			s.Start(start, 0, new[] { value, value });
			return s;
		}

		static void Report(DeviceTestStatistics s, long ticks, int a, int b)
		{
			s.AddReport(ticks, new[] { a, b });
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The report rate counts reads that changed while moving, and the intervals between them give the shortest and the median")]
		public void Report_rate_and_intervals()
		{
			var s = Started();
			s.StartMoving(Second, 0);
			for (var i = 1; i <= 1000; i++)
				Report(s, Second + i * Ms, Middle + i, Middle);
			// One report half a millisecond after the last.
			Report(s, Second + 1000 * Ms + 500, Middle, Middle);
			s.Stop(2 * Second, 0);
			var summary = s.Summary(3 * Second);
			Assert.AreEqual(1001, summary.Reports);
			Assert.AreEqual(1000.0, summary.ReportRate.Value, 0.001, "One over the median interval, 1 ms.");
			Assert.AreEqual(0.5, summary.ShortestInterval.Value, 0.001);
			Assert.AreEqual(1.0, summary.MedianInterval.Value, 0.001);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The read rate counts every read, and reads under twice the report rate are too slow")]
		public void Read_rate_and_the_warning()
		{
			var s = Started();
			s.StartMoving(Second, 500);
			for (var i = 1; i <= 1000; i++)
				Report(s, Second + i * Ms, Middle + i, Middle);
			s.Stop(2 * Second, 1500);
			var slow = s.Summary(2 * Second);
			Assert.AreEqual(750.0, slow.ReadRate.Value, 0.001, "1500 reads in 2 s.");
			Assert.IsTrue(slow.TooSlow, "750 reads a second cannot see 1000 reports a second.");
			s = Started();
			s.StartMoving(Second, 2500);
			for (var i = 1; i <= 1000; i++)
				Report(s, Second + i * Ms, Middle + i, Middle);
			s.Stop(2 * Second, 5000);
			Assert.IsFalse(s.Summary(2 * Second).TooSlow, "2500 reads a second see 1000 reports a second.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Without a change buffer the values a report carries come from the reads: the average changed, and the share of reports that changed both of two moving axes")]
		public void Values_per_report_from_the_reads()
		{
			var s = Started();
			s.StartMoving(Second, 0);
			int a = Middle, b = Middle;
			for (var i = 0; i < 10; i++)
			{
				a += 10;
				// Six reports change both, four only the first.
				if (i < 6)
					b += 10;
				Report(s, Second + (i + 1) * Ms, a, b);
			}
			var summary = s.Summary(2 * Second);
			Assert.IsFalse(summary.FromSequence);
			Assert.AreEqual(1.6, summary.ValuesPerReport.Value, 0.0001);
			Assert.AreEqual(60.0, summary.TogetherPercent.Value, 0.0001, "All ten were made while both moved, six changed both.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("With a change buffer the values a report carries come from the sequence numbers: entries of one report share one")]
		public void Values_per_report_from_sequence_numbers()
		{
			var s = Started();
			s.StartMoving(Second, 0);
			var t = Second;
			// Report 1: both axes; 2: the first; 3: both and a button; 4: the first.
			s.AddEntry(t += Ms, 1, 0);
			s.AddEntry(t, 1, 1);
			s.AddEntry(t += Ms, 2, 0);
			s.AddEntry(t += Ms, 3, 0);
			s.AddEntry(t, 3, 1);
			s.AddEntry(t, 3, -1);
			s.AddEntry(t += Ms, 4, 0);
			s.Stop(2 * Second, 0);
			var summary = s.Summary(2 * Second);
			Assert.IsTrue(summary.FromSequence);
			Assert.AreEqual(7.0 / 4, summary.ValuesPerReport.Value, 0.0001, "Seven entries in four reports.");
			Assert.AreEqual(50.0, summary.TogetherPercent.Value, 0.0001, "Of the four reports made while both moved, two changed both.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The centre and the rest noise weigh each value by how long it was held; the dead zone needed covers the farthest rest value from the middle")]
		public void Centre_noise_and_dead_zone()
		{
			var s = Started(32000);
			Report(s, 1 * Second, 32010, 32000);
			Report(s, 4 * Second, 32000, 32000);
			s.StartMoving(5 * Second, 0);
			var x = s.Channels[0];
			// 32000 held for 2 s, 32010 for 3 s.
			Assert.AreEqual(32006.0, x.Centre.Value, 0.0001);
			Assert.AreEqual(32006.0 - 32767.5, x.CentreOffset.Value, 0.0001);
			Assert.AreEqual(100 * (32006.0 - 32767.5) / 65535, x.CentreOffsetPercent.Value, 0.0001, "The offset as a share of the range.");
			Assert.AreEqual(10, x.NoiseSpread.Value);
			Assert.AreEqual(Math.Sqrt(24), x.NoiseDeviation.Value, 0.0001);
			Assert.AreEqual(100 * 767.5 / 32767.5, x.DeadZonePercent.Value, 0.0001);
			var y = s.Channels[1];
			Assert.AreEqual(0, y.NoiseSpread.Value, "An axis that never moved at rest has no noise.");
			Assert.AreEqual(0.0, y.NoiseDeviation.Value, 0.0001);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Range, distinct values and bits, the smallest and largest step, the first step away from the centre, and jumps over four times the median step")]
		public void Range_resolution_and_steps()
		{
			var s = Started(32000);
			s.StartMoving(5 * Second, 0);
			var t = 5 * Second;
			foreach (var value in new[] { 32100, 32200, 32300, 33300, 33400 })
				Report(s, t += Ms, value, 32000);
			var x = s.Channels[0];
			Assert.AreEqual(32000, x.Min.Value);
			Assert.AreEqual(33400, x.Max.Value);
			Assert.AreEqual(100.0 * 1400 / 65535, x.RangePercent.Value, 0.0001);
			Assert.AreEqual(6, x.Distinct);
			Assert.AreEqual(Math.Log(6, 2), x.EffectiveBits.Value, 0.0001);
			Assert.AreEqual(100, x.SmallestStep.Value);
			Assert.AreEqual(1000, x.LargestStep.Value);
			Assert.AreEqual(100L, x.MedianStep.Value);
			Assert.AreEqual(1, x.Jumps, "One step of 1000 against a median of 100.");
			Assert.AreEqual(100.0 * 100 / 65535, x.FirstStepPercent.Value, 0.0001, "From the centre, 32000, to 32100.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An axis's own update rate counts its changes within each movement, not the pauses between movements")]
		public void Update_rate_while_moving()
		{
			var s = Started();
			s.StartMoving(Second, 0);
			var t = Second;
			for (var i = 1; i <= 11; i++)
				Report(s, t += Ms, Middle + i, Middle);
			// A second's pause, then a movement at one change every 2 ms.
			t += Second;
			for (var i = 1; i <= 5; i++)
				Report(s, t += 2 * Ms, Middle - i, Middle);
			// Ten steps of 1 ms, then four of 2 ms: the median is 1 ms, and the pause is no part of either movement.
			Assert.AreEqual(1000.0, s.Channels[0].UpdateRate.Value, 0.001);
			Assert.IsFalse(s.Channels[1].UpdateRate.HasValue, "An axis that never moved has no rate.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The report rate is one over the median interval within movements, never under any axis's own rate, and agrees with the values each report carries; a pause of the device does not lower it")]
		public void Report_rate_agrees_with_the_intervals_the_axes_and_the_values()
		{
			var s = Started();
			s.StartMoving(Second, 0);
			var t = Second;
			int a = Middle, b = Middle;
			for (var i = 1; i <= 2000; i++)
			{
				// Reports 0.9 and 1.1 ms apart in turn; the first axis changes in every report, the second in every other one.
				t += i % 2 == 0 ? 900 : 1100;
				// The device is left alone for 2 s half way through.
				if (i == 1000)
					t += 2 * Second;
				a += 10;
				if (i % 2 == 0)
					b += 10;
				Report(s, t, a, b);
			}
			s.Stop(t, 0);
			var summary = s.Summary(t);
			var rate = summary.ReportRate.Value;
			Assert.AreEqual(1000.0 / summary.MedianInterval.Value, rate, 0.0001, "The report rate is not one over the median interval.");
			Assert.IsTrue(rate >= 1000000.0 / 1100 && rate <= 1000000.0 / 900, rate + " a second lies outside the stream's jitter, 909 to 1,111.");
			var fastest = s.Channels.Max(x => x.UpdateRate.Value);
			Assert.IsTrue(rate >= fastest, "Reports came " + rate + " a second, slower than one axis changed, " + fastest + ".");
			Assert.AreEqual(500.0, s.Channels[1].UpdateRate.Value, 0.001, "The second axis changed every 2 ms.");
			Assert.AreEqual(1.5, summary.ValuesPerReport.Value, 0.001);
			var changes = s.Channels.Sum(x => x.UpdateRate.Value);
			Assert.AreEqual(changes, rate * summary.ValuesPerReport.Value, 0.1 * changes, "The values a report carries do not add up to the axes' own rates.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A rest spread over 5 % of the range means the control moved while it was to be left alone; a stick's own noise does not")]
		public void A_control_moved_at_rest_is_told_from_noise()
		{
			var s = Started(32000);
			Report(s, Second, 32300, 32000 + 4000);
			s.StartMoving(5 * Second, 0);
			Assert.IsFalse(s.Channels[0].MovedAtRest, "A spread of 300, 0.5 % of the range, is noise.");
			Assert.IsTrue(s.Channels[1].MovedAtRest, "A spread of 4,000, 6.1 % of the range, is a touch.");
		}
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Reads before Start and after Stop count for nothing, and Start forgets the test before")]
		public void Phases_bound_the_test()
		{
			var s = new DeviceTestStatistics(1, Second);
			s.AddReport(0, new[] { 1 });
			Assert.AreEqual(DeviceTestPhase.Idle, s.Phase);
			Assert.IsFalse(s.Channels[0].Min.HasValue, "A read before Start was counted.");
			s.Start(Second, 0, new[] { 100 });
			s.StartMoving(2 * Second, 0);
			s.AddReport(3 * Second, new[] { 200 });
			s.Stop(4 * Second, 0);
			s.AddReport(5 * Second, new[] { 300 });
			Assert.AreEqual(200, s.Channels[0].Max.Value, "A read after Stop was counted.");
			s.Start(6 * Second, 0, new[] { 50 });
			Assert.AreEqual(50, s.Channels[0].Max.Value, "Start kept the test before.");
			Assert.AreEqual(1, s.Channels[0].Distinct);
		}
	}
}
