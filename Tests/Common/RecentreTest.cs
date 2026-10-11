// @under-test: App.v4/Controls/PadControl.cs, Engine/Common/MapExpressionUnits.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// [Recentre] in a mapping box's menu makes where a stick rests now read as its middle (issue #1638).
	/// </summary>
	/// <remarks>
	/// It writes the formula Help suggests for a stick that rests off centre, with the offset measured rather
	/// than guessed.
	/// </remarks>
	[TestClass]
	public class RecentreTest
	{
		/// <summary>Axes with axis 1 resting a twentieth of its half travel above the middle, and axis 2 in the middle.</summary>
		static int[] Axes()
		{
			var axes = new int[SourceState.MaxAxis];
			for (var i = 0; i < axes.Length; i++)
				axes[i] = 32767;
			axes[0] = 32767 + 1638;
			return axes;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick resting off centre gets the formula that takes the offset away, the right way round")]
		public void A_stick_resting_off_centre_is_recentred()
		{
			Assert.AreEqual("=a1-0.05", PadControl.RecentredText("Axis 1", Axes()));
			Assert.AreEqual("=-a1+0.05", PadControl.RecentredText("IAxis 1", Axes()));
			Assert.AreEqual("=a1-0.05", PadControl.RecentredText("=a1+0.2", Axes()), "A formula already recentred is not measured again.");
			Assert.AreEqual(0f, MapExpressionUnits.Centred(Axes()[0]) - 0.05f, 0.0005f, "The test's stick does not rest where it says.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick in the middle keeps a plain mapping, and a box with no axis is left alone")]
		public void Only_an_axis_off_centre_gets_a_formula()
		{
			Assert.AreEqual("Axis 2", PadControl.RecentredText("Axis 2", Axes()));
			Assert.AreEqual("IAxis 2", PadControl.RecentredText("=-a2+0.1", Axes()));
			foreach (var text in new[] { "Slider 1", "Button 3", "POV 1 Up", "=a1*2", "Axis 99", "", null })
				Assert.IsNull(PadControl.RecentredText(text, Axes()), "'" + (text ?? "(null)") + "' maps no axis to recentre.");
			Assert.IsNull(PadControl.RecentredText("Axis 1", null), "A device that is not connected has nothing to measure.");
		}
	}
}
