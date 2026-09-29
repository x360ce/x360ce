using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace x360ce.Tests
{
	/// <summary>A rumble report from the bus, shared by the force-merging test classes.</summary>
	internal static class ForceFeedbackFixtures
	{
		public static Xbox360FeedbackReceivedEventArgs Rumble(byte large, byte small)
		{
			return new Xbox360FeedbackReceivedEventArgs(large, small, 0);
		}
	}
}
