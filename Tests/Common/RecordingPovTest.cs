// @under-test: App.v4/Common/DInput/Recorder.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Recording finds the POV that moved, on a device with more than one.
	/// </summary>
	[TestClass]
	public class RecordingPovTest
	{
		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Pressing a direction on the second POV records the second POV")]
		public void The_POV_that_moved_is_recorded()
		{
			var before = new SourceState();
			for (var i = 0; i < before.Povs.Length; i++)
				before.Povs[i] = -1;
			var after = before.Clone();
			after.Povs[1] = (int)DPadEnum.Right;
			var actions = Recorder.CompareTo(before, after, MapCode.DPadRight);
			CollectionAssert.Contains(actions, "POV 2 Right", "Moving POV 2 recorded: " + string.Join(", ", actions));
		}
	}
}
