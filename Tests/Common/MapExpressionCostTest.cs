// @under-test: Engine/Common/MapExpression.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// What reading a formula costs, at the rate a controller is actually polled.
	/// </summary>
	/// <remarks>
	/// A controller is polled about a thousand times a second, and every mapped row is read on every
	/// poll, so a reading that costs a millisecond costs thirty seconds of processor time per second.
	/// That is not a slow program, it is a stopped one.
	///
	/// What keeps that from happening is the pad keeping its mappings until one of them changes, so a
	/// formula is read once when it is set rather than once per poll. That is what is measured here.
	/// </remarks>
	[TestClass]
	public class MapExpressionCostTest
	{

		private const int Polls = 30000;

		private static double MillisecondsFor(string text, int times)
		{
			MapExpression parsed;
			string error;
			int position;
			// Once first, so the measurement is of the steady state rather than of the first reading.
			MapExpression.TryParse(text, out parsed, out error, out position);
			var clock = Stopwatch.StartNew();
			for (int i = 0; i < times; i++)
				MapExpression.TryParse(text, out parsed, out error, out position);
			clock.Stop();
			return clock.Elapsed.TotalMilliseconds;
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Reading the same text twice gives the same answer both times")]
		public void Reading_the_same_text_twice_gives_the_same_answer()
		{
			MapExpression parsed;
			string error;
			int position;
			for (int i = 0; i < 3; i++)
			{
				Assert.IsTrue(MapExpression.TryParse("=a1+1", out parsed, out error, out position));
				Assert.IsNotNull(parsed);
				Assert.IsNull(error);
				Assert.IsFalse(MapExpression.TryParse("=a1+", out parsed, out error, out position));
				Assert.IsNull(parsed);
				Assert.IsFalse(string.IsNullOrEmpty(error), "The reason has to be given every time.");
				Assert.IsTrue(position >= 0, "So does where the fault is.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Reading a pad's mappings at polling speed costs almost nothing")]
		public void Reading_the_mappings_costs_almost_nothing_at_polling_speed()
		{
			// Every poll asks a pad for its mappings, about a thousand times a second. Building them
			// afresh each time makes thirty objects a poll, thirty thousand a second, each one reading
			// its text again and all of them thrown away immediately.
			var pad = new x360ce.Engine.Data.PadSetting
			{
				RightTrigger = "=a5*2",
				LeftTrigger = "a4",
				ButtonA = "2",
				LeftThumbAxisX = "a1",
			};
			var first = pad.Maps;
			Assert.IsNotNull(first);
			var clock = Stopwatch.StartNew();
			for (int i = 0; i < 1000; i++)
			{
				var maps = pad.Maps;
				if (maps == null)
					Assert.Fail("A pad stopped reporting its mappings.");
			}
			clock.Stop();
			var cost = clock.Elapsed.TotalMilliseconds;
			Assert.IsTrue(cost < 50,
				"A second of polling spent " + cost.ToString("N0") + " ms just fetching mappings that "
				+ "did not change. They are being built again on every poll.");
			Assert.AreSame(first, pad.Maps,
				"The same unchanged pad handed out a second set of mappings, so nothing is being kept.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Changing a mapping is noticed, so keeping them does not make them stale")]
		public void Changing_a_mapping_is_still_noticed()
		{
			// Keeping the mappings is only safe while a change is certain to be seen. This is what
			// makes that true, and it is the reason the cheaper path is allowed at all.
			var pad = new x360ce.Engine.Data.PadSetting { RightTrigger = "a5" };
			var before = pad.Maps.First(x => x.Target == TargetType.RightTrigger);
			Assert.AreEqual(5, before.Index);
			Assert.IsNull(before.Expression);
			pad.RightTrigger = "=a5*2";
			var after = pad.Maps.First(x => x.Target == TargetType.RightTrigger);
			Assert.IsNotNull(after.Expression, "The change was not noticed, so the old mapping is still live.");
		}

	}
}
