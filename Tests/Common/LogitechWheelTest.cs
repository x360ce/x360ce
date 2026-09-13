// @under-test: Engine/Common/LogitechWheel.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Checks the report that sets a Logitech wheel's steering range, byte for byte, against what
	/// the wheel's Linux driver sends, and which wheels are offered it.
	/// </summary>
	[TestClass]
	public class LogitechWheelTest
	{
		[TestMethod, TestCategory("devices")]
		[Description("900 degrees is 0xf8 0x81 with the range in two bytes, low byte first, after the report identifier")]
		public void Range_report_matches_the_driver()
		{
			var report = LogitechWheel.RangeReport(900, 8);
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xF8, 0x81, 0x84, 0x03, 0x00, 0x00, 0x00 }, report);
		}

		[TestMethod, TestCategory("devices")]
		[Description("The report is as long as the interface asks, and never shorter than the command")]
		public void Report_length_follows_the_interface()
		{
			Assert.AreEqual(8, LogitechWheel.RangeReport(540, 0).Length, "An interface that will not say gets the command's own length.");
			Assert.AreEqual(16, LogitechWheel.RangeReport(540, 16).Length);
		}

		[TestMethod, TestCategory("devices")]
		[Description("A range beyond what the wheel takes is held to its limits")]
		public void Range_is_held_within_the_wheel()
		{
			var wide = LogitechWheel.RangeReport(1080, 8);
			Assert.AreEqual(900, wide[3] | (wide[4] << 8));
			var narrow = LogitechWheel.RangeReport(10, 8);
			Assert.AreEqual(40, narrow[3] | (narrow[4] << 8));
		}

		[TestMethod, TestCategory("devices")]
		[Description("Only the Logitech wheels that take the report are offered it")]
		public void Only_the_wheels_that_take_the_report_are_offered_it()
		{
			Assert.IsTrue(LogitechWheel.SupportsRange(0x046D, 0xC29B), "G27");
			Assert.IsTrue(LogitechWheel.SupportsRange(0x046D, 0xC24F), "G29");
			Assert.IsFalse(LogitechWheel.SupportsRange(0x046D, 0xC294), "A wheel still in its compatibility mode takes a different command.");
			Assert.IsFalse(LogitechWheel.SupportsRange(0x045E, 0x028E), "An Xbox controller.");
		}
	}
}
