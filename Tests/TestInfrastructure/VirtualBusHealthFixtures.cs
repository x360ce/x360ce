namespace x360ce.Tests
{
	/// <summary>Ticks shared by the virtual-bus-health test classes, chosen so their arithmetic cannot wrap by accident.</summary>
	internal static class VirtualBusHealthFixtures
	{
		/// <summary>A tick far from both ends of the range, so arithmetic in the tests cannot wrap by accident.</summary>
		internal const int Now = 1000000;

		/// <summary>A tick 2^31 ms and one second before <see cref="Now"/>, about 24.9 days, where the difference turns negative.</summary>
		internal static readonly int LongAgo = unchecked(Now - (int.MinValue + 1000));
	}
}
