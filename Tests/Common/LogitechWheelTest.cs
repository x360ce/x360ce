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

		[TestMethod, TestCategory("devices")]
		[Description("The wheel's own centering is switched on with 0x14 and off with 0xf5, as the Linux driver switches it")]
		public void Centering_reports_match_the_driver()
		{
			CollectionAssert.AreEqual(new byte[] { 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, LogitechWheel.CenteringReport(true, 8));
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xF5, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, LogitechWheel.CenteringReport(false, 8));
			Assert.AreEqual(8, LogitechWheel.CenteringReport(true, 0).Length, "An interface that will not say gets the command's own length.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Every Logitech wheel the Linux driver switches the centering of is offered it, the newer protocol's wheels are not")]
		public void Every_wheel_that_takes_the_centering_report_is_offered_it()
		{
			foreach (var productId in new[] { 0xC293, 0xC294, 0xC295, 0xC298, 0xC299, 0xC29A, 0xC29B, 0xC29C, 0xC24F, 0xCA03 })
				Assert.IsTrue(LogitechWheel.SupportsCentering(0x046D, productId), productId.ToString("X4"));
			Assert.IsFalse(LogitechWheel.SupportsCentering(0x046D, 0xC262), "The G920 speaks another protocol.");
			Assert.IsFalse(LogitechWheel.SupportsCentering(0x046D, 0xC26E), "The G923 for Xbox speaks another protocol.");
			Assert.IsFalse(LogitechWheel.SupportsCentering(0x045E, 0x028E), "An Xbox controller.");
		}
	}
}
