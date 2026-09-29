using System;

namespace x360ce.Tests
{
	/// <summary>Counts what code hands the collector, for the tests that require the engine's work to make nothing.</summary>
	/// <remarks>
	/// <see cref="AppDomain.MonitoringTotalAllocatedMemorySize"/> counts whole 8 KB blocks taken by any thread in the
	/// domain, so a thread an earlier test left running can add a block to one window. What the work itself makes shows
	/// in every window, so the smallest count is the work's own. One object per call, 24 bytes at the least, fills more
	/// than a block in any window of more than 342 calls, so it shows in every one of them.
	/// </remarks>
	public static class Allocations
	{
		/// <summary>The fewest bytes any of <paramref name="windows"/> runs of <paramref name="work"/> handed the collector.</summary>
		/// <remarks>Makes nothing between the counts, so a window whose work makes nothing reads 0.</remarks>
		public static long FewestBytes(int windows, Action work)
		{
			AppDomain.MonitoringIsEnabled = true;
			var fewest = long.MaxValue;
			for (var w = 0; w < windows; w++)
			{
				var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
				work();
				fewest = Math.Min(fewest, AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize - before);
			}
			return fewest;
		}
	}
}
