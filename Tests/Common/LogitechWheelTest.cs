// @under-test: Engine/Common/LogitechWheel.cs, Engine/Common/ForceFeedbackState.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Checks the report that sets a Logitech wheel's steering range, byte for byte, against what
	/// the wheel's Linux driver sends, and which wheels are offered it.
	/// </summary>
	[TestClass]
	public class LogitechWheelTest
	{
		/// <summary>
		/// A G27 left as it is turns the 200 degrees it powers up at, and the spring counted against 900 was four and a half
		/// times steeper than set in real degrees: the wheel swung from side to side at its centre and never settled.
		/// </summary>
		[TestMethod, TestCategory("devices")]
		[Description("Left as it is, the spring is counted against the 200 degrees a wheel that takes the range powers up at, and against 900 on any other wheel")]
		public void Spring_range_left_as_it_is_follows_the_wheel()
		{
			var g27 = new UserDevice { DevVendorId = 0x046D, DevProductId = 0xC29B };
			var other = new UserDevice { DevVendorId = 0x346E, DevProductId = 0x0002 };
			Assert.AreEqual(LogitechWheel.PowerUpRange, ForceFeedbackState.SpringRange(0, g27), "A G27 left as it is.");
			Assert.AreEqual(ForceFeedbackState.DefaultRangeDegrees, ForceFeedbackState.SpringRange(0, other), "A wheel that takes no range.");
			Assert.AreEqual(ForceFeedbackState.DefaultRangeDegrees, ForceFeedbackState.SpringRange(0, null), "No device yet.");
			Assert.AreEqual(540, ForceFeedbackState.SpringRange(540, g27), "A range chosen is the range sent.");
			Assert.AreEqual(540, ForceFeedbackState.SpringRange(540, other), "A range chosen counts on any wheel.");
		}

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

		/// <summary>
		/// The range was sent once per setting: a range the wheel did not take, or one Logitech's software replaced when the
		/// wheel was held again, stayed lost until the person chose another range and then theirs again.
		/// </summary>
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The range is sent again while the wheel will not take it, each time the wheel is held again, and when the person chooses the range the list already names")]
		public void The_range_reaches_the_wheel_again()
		{
			// A G27 at a path that opens nothing, so every send fails and is counted.
			var g27 = new UserDevice { DevVendorId = 0x046D, DevProductId = 0xC29B, HidDevicePath = @"\\?\hid#x360ce-test-no-such-wheel" };
			var ps = new PadSetting { WheelRange = "900" };
			var state = new ForceFeedbackState();
			const int t = 1000000;
			state.UpdateRange(g27, null, ps, false, t);
			state.UpdateCentering(t);
			Assert.AreEqual(1, state.Counts.RangeSends, "The range chosen is sent at once.");
			state.UpdateCentering(t + ForceFeedbackState.RangeRetryMs / 2);
			Assert.AreEqual(1, state.Counts.RangeSends, "A range the wheel did not take is not sent on every poll.");
			for (var i = 1; i <= 10; i++)
				state.UpdateCentering(t + ForceFeedbackState.RangeRetryMs * i);
			Assert.AreEqual(ForceFeedbackState.RangeTries, state.Counts.RangeSends, "A range the wheel did not take is sent again a few times, then left.");

			state.UpdateRange(g27, null, ps, false, t + 20000);
			state.UpdateCentering(t + 20000);
			Assert.AreEqual(ForceFeedbackState.RangeTries, state.Counts.RangeSends, "The same range is sent again with nothing changed.");

			// Held again: Logitech's software may have put its own range on the wheel. Sent once the device settles.
			state.UpdateRange(g27, null, ps, true, t + 30000);
			state.UpdateCentering(t + 30000);
			Assert.AreEqual(ForceFeedbackState.RangeTries, state.Counts.RangeSends, "The range is sent before the device settles.");
			state.UpdateCentering(t + 30000 + ForceFeedbackState.CenteringSettleMs);
			Assert.AreEqual(ForceFeedbackState.RangeTries + 1, state.Counts.RangeSends, "The range is not sent again to a wheel held again.");

			// The person chooses the range the list already names.
			var sent = state.Counts.RangeSends;
			state.SendRangeAgain();
			state.UpdateCentering(t + 60000);
			Assert.AreEqual(sent + 1, state.Counts.RangeSends, "Choosing the range again does not send it.");

			// Left as it is: nothing is sent, also when asked.
			var asIs = new ForceFeedbackState();
			asIs.UpdateRange(g27, null, new PadSetting { WheelRange = "0" }, true, t);
			asIs.SendRangeAgain();
			for (var i = 0; i < 10; i++)
				asIs.UpdateCentering(t + ForceFeedbackState.RangeRetryMs * i);
			Assert.AreEqual(0, asIs.Counts.RangeSends, "A range left as it is was sent.");
		}

		/// <summary>
		/// Logitech's driver puts its own range, 270 degrees on a G27, on the wheel a moment after a game comes to the front,
		/// after the range chosen was sent, so the range is sent again after that.
		/// </summary>
		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Asked to send the range again after a delay, twice before the engine's next poll, the wheel is sent it at each delay and not before")]
		public void The_range_is_sent_again_after_each_delay_asked()
		{
			// A G27 at a path that opens nothing, so every send fails and is counted.
			var g27 = new UserDevice { DevVendorId = 0x046D, DevProductId = 0xC29B, HidDevicePath = @"\\?\hid#x360ce-test-no-such-wheel" };
			var state = new ForceFeedbackState();
			const int t = 1000000;
			state.UpdateRange(g27, null, new PadSetting { WheelRange = "900" }, false, t);
			for (var i = 0; i < ForceFeedbackState.RangeTries; i++)
				state.UpdateCentering(t + ForceFeedbackState.RangeRetryMs * i);
			Assert.AreEqual(ForceFeedbackState.RangeTries, state.Counts.RangeSends, "The range chosen was not sent its tries first.");

			// Another program comes to the front: both asks arrive before the engine polls again.
			const int front = t + 60000;
			state.SendRangeAgain(ForceFeedbackState.RangeAgainSoonMs);
			state.SendRangeAgain(ForceFeedbackState.RangeAgainLateMs);
			var sentAt = new System.Collections.Generic.List<int>();
			for (var ms = 0; ms < ForceFeedbackState.RangeAgainLateMs + ForceFeedbackState.RangeRetryMs / 2; ms++)
			{
				var sent = state.Counts.RangeSends;
				state.UpdateCentering(front + ms);
				if (state.Counts.RangeSends != sent)
					sentAt.Add(ms);
			}
			// Between the two, the first send is sent again a second later, as the wheel did not take it.
			CollectionAssert.AreEqual(new[] { ForceFeedbackState.RangeAgainSoonMs, ForceFeedbackState.RangeAgainSoonMs + ForceFeedbackState.RangeRetryMs, ForceFeedbackState.RangeAgainLateMs },
				sentAt, "The range was sent at " + string.Join(", ", sentAt) + " ms after the asks.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The RGB/RPM LED strip report is 0xf8 0x12 with a 5-bit mask, matching hid-lg4ff, and RPM maps progressively with a redline flash")]
		public void Led_reports_and_rpm_progression_match_logitech_shift_strip()
		{
			Assert.IsTrue(LogitechWheel.SupportsLeds(0x046D, 0xC29B), "G27");
			Assert.IsTrue(LogitechWheel.SupportsLeds(0x046D, 0xC24F), "G29");
			Assert.IsTrue(LogitechWheel.SupportsLeds(0x046D, 0xC266), "G923 PlayStation");
			Assert.IsFalse(LogitechWheel.SupportsLeds(0x045E, 0x028E), "Xbox controller");

			CollectionAssert.AreEqual(new byte[] { 0x00, 0xF8, 0x12, 0x1F, 0x00, 0x00, 0x00, 0x00 }, LogitechWheel.LedReport(0xFF, 8));
			CollectionAssert.AreEqual(new byte[] { 0x00, 0xF8, 0x12, 0x07, 0x00, 0x00, 0x00, 0x00 }, LogitechWheel.LedReport(0x07, 8));

			Assert.AreEqual((byte)0x00, LogitechWheel.RpmToLedMask(0.40f, 0));
			Assert.AreEqual((byte)0x01, LogitechWheel.RpmToLedMask(0.55f, 0));
			Assert.AreEqual((byte)0x03, LogitechWheel.RpmToLedMask(0.68f, 0));
			Assert.AreEqual((byte)0x07, LogitechWheel.RpmToLedMask(0.78f, 0));
			Assert.AreEqual((byte)0x0F, LogitechWheel.RpmToLedMask(0.88f, 0));
			Assert.AreEqual((byte)0x1F, LogitechWheel.RpmToLedMask(0.95f, 0));
			// Redline >= 97 % alternates between all-on and all-off.
			Assert.AreEqual((byte)0x1F, LogitechWheel.RpmToLedMask(0.99f, 0));
			Assert.AreEqual((byte)0x00, LogitechWheel.RpmToLedMask(0.99f, 85));
		}
	}
}
