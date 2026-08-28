// @under-test: Tests/TestInfrastructure/Ui.cs
// @area: diagnostics   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// Clearing away the reports the suite's own injected faults leave behind.
	/// </summary>
	/// <remarks>
	/// The suite raises faults on purpose to prove a person can send a report. Those land in the same
	/// folder real ones do, so nine of them once sat in the status bar counted as the person's own,
	/// waiting to be sent to support. Clearing them is only safe while it is exact, which is what
	/// these hold: an injected report goes, a real one stays.
	/// </remarks>
	[TestClass]
	public class InjectedReportCleanupTest
	{

		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Injected reports are cleared and real ones are kept")]
		public void Injected_reports_are_cleared_and_real_ones_are_kept()
		{
			var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var folder = Path.Combine(root, "x360ce", "Errors");
			Directory.CreateDirectory(folder);
			try
			{
				var injected = Path.Combine(folder, "FCE_InvalidOperationException_1.htm");
				var real = Path.Combine(folder, "FCE_NullReferenceException_2.htm");
				File.WriteAllText(injected, "<html>" + Ui.InjectedFaultMarker + " X360CE_THROW_AFTER.</html>");
				File.WriteAllText(real, "<html>Object reference not set to an instance of an object.</html>");

				Ui.RemoveInjectedFaultReports(Path.Combine(root, "x360ce.exe"));

				Assert.IsFalse(File.Exists(injected),
					"A fault the suite raised itself is still counted against the application.");
				Assert.IsTrue(File.Exists(real),
					"A real report was deleted. Clearing up after the tests must never cost a person " +
					"the one thing they had to send.");
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

	}
}
