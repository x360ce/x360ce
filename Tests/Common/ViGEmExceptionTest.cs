// @under-test: App.v4/ViGEm/Client/ViGEmException.cs
// @area: crash-reporting   @layer: engine
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nefarius.ViGEm.Client;
using System;

namespace x360ce.Tests
{
	/// <summary>
	/// A crash report is only worth the mail it arrives in if it says what failed.
	/// </summary>
	/// <remarks>
	/// Half of the reports from 4.18.63.0 were failures to plug in a virtual controller, and not
	/// one said which failure. The exception carried the code but was built with no message, so the
	/// framework supplied its own - "exception of type ... was thrown", in the language of whoever
	/// hit it - and the code never reached the report.
	///
	/// That mattered because the codes mean opposite things. A bus with no free slot is a state of
	/// the machine, already left unreported. A bus that is not found is a missing driver, and a
	/// fault. Arriving identical, the two could not be told apart by anybody reading the mailbox.
	/// </remarks>
	[TestClass]
	public class ViGEmExceptionTest
	{
		[TestMethod, TestCategory("crash-reporting")]
		[Description("A virtual controller failure says which failure it was")]
		public void Failure_names_itself_in_the_text_a_report_carries()
		{
			foreach (VIGEM_ERROR code in Enum.GetValues(typeof(VIGEM_ERROR)))
			{
				var ex = new ViGEmException(code);
				Assert.AreEqual(code, ex.Code, "The exception should carry the code it was built with.");
				// ToString is what a stack trace, a log line and the crash report all print.
				var reported = ex.ToString();
				Assert.IsTrue(reported.Contains(code.ToString()), string.Format(
					"A report of {0} does not name it. It reads: {1}. Every failure to plug in a "
					+ "controller then looks alike in the mailbox.", code, ex.Message));
			}
		}

		[TestMethod, TestCategory("crash-reporting")]
		[Description("The two failures that mean opposite things read differently")]
		public void A_full_bus_and_a_missing_driver_do_not_read_alike()
		{
			// One is a state of the machine and is deliberately not reported; the other is a fault.
			// Whoever reads the mailbox has only this text to tell them apart.
			var full = new ViGEmException(VIGEM_ERROR.VIGEM_ERROR_NO_FREE_SLOT).ToString();
			var missing = new ViGEmException(VIGEM_ERROR.VIGEM_ERROR_BUS_NOT_FOUND).ToString();
			Assert.AreNotEqual(full, missing,
				"A full bus and a missing driver produce the same report, so neither can be acted on.");
		}
	}
}
