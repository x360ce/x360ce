// @under-test: Engine/Common/MapExpression.cs, Engine/Data/PadSetting.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.CompilerServices;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether playing for a long time makes the program hold more and more.
	/// </summary>
	/// <remarks>
	/// A controller is polled about a thousand times a second, so anything the polling path keeps
	/// hold of is kept a thousand times a second. A leak of a hundred bytes a poll is six megabytes
	/// a minute, which nobody notices in a test lasting a second and everybody notices in an evening
	/// of playing.
	///
	/// Two different things are measured and they are not the same question. How much is handed to
	/// the collector says whether the path is wasteful. How much is still held after collecting says
	/// whether it leaks. A path can be wasteful without leaking, and a leak can be almost silent.
	///
	/// These need the optimiser on, which this project sets in every configuration. Without it the
	/// compiler keeps things alive for the debugger long after the code has finished with them, and
	/// a test like this passes whatever the truth is.
	/// </remarks>
	[TestClass]
	public class MapExpressionMemoryTest
	{

		private const int Polls = 20000;

		private static PadSetting TunedPad()
		{
			return new PadSetting
			{
				RightTrigger = "=a5*2",
				LeftTrigger = "=a1*abs(a1)",
				LeftThumbAxisX = "a1",
				LeftThumbAxisY = "a2",
				ButtonA = "2",
			};
		}

		/// <summary>One poll's worth of work: read the mappings and work out every formula.</summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static float PollOnce(PadSetting pad, CustomDiState state, float[] values)
		{
			var total = 0f;
			foreach (var map in pad.Maps)
			{
				if (map.Expression == null)
					continue;
				if (MapExpressionUnits.TryFill(map.Expression, state, values, false))
					total += map.Expression.Evaluate(values);
			}
			return total;
		}

		private static CustomDiState Resting()
		{
			return new CustomDiState(new SharpDX.DirectInput.JoystickState());
		}

		private static long Collected()
		{
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
			return GC.GetTotalMemory(true);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Polling hands almost nothing to the collector, per poll")]
		public void Polling_hands_almost_nothing_to_the_collector()
		{
			AppDomain.MonitoringIsEnabled = true;
			var pad = TunedPad();
			var state = Resting();
			var values = new float[MapExpression.MaxReferences];
			// Warmed up first, so what is measured is playing rather than starting.
			PollOnce(pad, state, values);
			var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			for (int i = 0; i < Polls; i++)
				PollOnce(pad, state, values);
			var perPoll = (AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before) / (double)Polls;
			Assert.IsTrue(perPoll < 512,
				"Each poll handed the collector " + perPoll.ToString("N0") + " bytes. At a thousand "
				+ "polls a second that is " + (perPoll * 1000 / 1024 / 1024).ToString("N1")
				+ " MB every second, which the collector then has to keep clearing up.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A long run does not leave the program holding more than a short one")]
		public void A_long_run_does_not_leave_more_held()
		{
			var pad = TunedPad();
			var state = Resting();
			var values = new float[MapExpression.MaxReferences];
			// A short run first, so anything created once is already created and not counted as growth.
			for (int i = 0; i < 2000; i++)
				PollOnce(pad, state, values);
			var before = Collected();
			for (int i = 0; i < Polls; i++)
				PollOnce(pad, state, values);
			var after = Collected();
			var grew = after - before;
			Assert.IsTrue(grew < 256 * 1024,
				"After " + Polls.ToString("N0") + " more polls the program held "
				+ (grew / 1024.0).ToString("N0") + " KB more than before ("
				+ (before / 1024.0).ToString("N0") + " KB to " + (after / 1024.0).ToString("N0")
				+ " KB). Something in the polling path is being kept.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A formula that is wrong is no more wasteful than one that is right")]
		public void A_wrong_formula_is_no_more_wasteful()
		{
			// Text somebody is part way through typing is wrong on almost every keystroke, and a
			// reading that fails used to raise and catch an error. Errors are expensive to make.
			AppDomain.MonitoringIsEnabled = true;
			var pad = new PadSetting { RightTrigger = "=a5*" };
			var state = Resting();
			var values = new float[MapExpression.MaxReferences];
			PollOnce(pad, state, values);
			var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
			for (int i = 0; i < Polls; i++)
				PollOnce(pad, state, values);
			var perPoll = (AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before) / (double)Polls;
			Assert.IsTrue(perPoll < 512,
				"Each poll of a row holding a formula that will not read handed the collector "
				+ perPoll.ToString("N0") + " bytes. The failure is being worked out again every time.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Changing a mapping lets go of the formula it replaced")]
		public void Changing_a_mapping_lets_go_of_the_old_formula()
		{
			// A compiled formula cannot be unloaded once it is alive, so anything still pointing at an
			// old one keeps it for the life of the program. Changing a row all evening would then
			// leave every formula ever typed still in memory.
			var pad = new PadSetting { RightTrigger = "=a5*2" };
			var held = FormulaOf(pad);
			Assert.IsTrue(held.IsAlive, "The formula was not there to begin with, so this proves nothing.");
			pad.RightTrigger = "=a5*3";
			var replacement = FormulaOf(pad);
			Assert.IsTrue(replacement.IsAlive, "The new formula was not read.");
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
			Assert.IsFalse(held.IsAlive,
				"The formula that was replaced is still being held, so every one ever typed stays.");
		}

		/// <summary>
		/// A weak hold on a pad's current trigger formula.
		/// </summary>
		/// <remarks>
		/// In its own method, never inlined, so that nothing the caller can see is left pointing at
		/// the formula. Left inline, the local that held it keeps it alive and the test always passes.
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference FormulaOf(PadSetting pad)
		{
			foreach (var map in pad.Maps)
				if (map.Target == TargetType.RightTrigger && map.Expression != null)
					return new WeakReference(map.Expression);
			return new WeakReference(null);
		}

	}
}
