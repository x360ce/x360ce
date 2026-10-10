// @under-test: Engine/Common/SpringCalibration.cs, App.v4/Controls/PadControl.cs, App.v4/Common/DInput/DInputHelper.Step2.UpdateDiStates.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The Force Feedback page says why the centering spring does nothing, and why an Auto run stopped.
	/// </summary>
	/// <remarks>
	/// A wheel whose controller tab was switched off for the current game felt no spring at any strength, and an
	/// Auto run ended with a bare "Stopped." The page said nothing that explained either (issue #1639).
	/// </remarks>
	[TestClass]
	public class CenteringSpringNoteTest
	{
		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A run stopped by the engine says what stopped it")]
		public void A_stopped_run_says_why()
		{
			var run = new SpringCalibration();
			run.Update(SpringCalibration.Center, 0);
			run.Cancel(DInputHelper.SpringStopTabOff);
			Assert.AreEqual(0, run.Update(SpringCalibration.Center, 1));
			Assert.AreEqual(SpringCalibration.Phase.Failed, run.Step);
			Assert.AreEqual(DInputHelper.SpringStopTabOff, run.Message);
			var plain = new SpringCalibration();
			plain.Cancel();
			plain.Update(SpringCalibration.Center, 0);
			Assert.AreEqual("Stopped.", plain.Message, "A run stopped with no reason says nothing else.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("The page says the spring does nothing only while it is ticked and nothing reaches the wheel")]
		public void The_page_says_when_the_spring_reaches_nothing()
		{
			StringAssert.Contains(PadControl.SpringNote(true, true, false), "switched off for the current game");
			Assert.IsNull(PadControl.SpringNote(true, true, true), "The note shows while the spring reaches the wheel.");
			Assert.IsNull(PadControl.SpringNote(true, false, false), "The note shows with the spring unticked.");
			Assert.IsNull(PadControl.SpringNote(false, true, false), "The note shows with force feedback unticked.");
		}
	}
}
