// @under-test: App.v4/MainForm.cs
// @area: errors   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// Counting the error reports never fails because their folder is not there.
	/// </summary>
	/// <remarks>
	/// Reported against 4.22.21.0 from a program started straight from its zip: Windows unpacks it into
	/// a temporary folder, the reports folder under it went, and the next count threw
	/// DirectoryNotFoundException, a fault report about counting fault reports.
	/// </remarks>
	[TestClass]
	public class ErrorFilesCountTest
	{
		[TestMethod, TestCategory("errors")]
		public void A_missing_folder_holds_no_reports()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-no-such-folder-" + Guid.NewGuid().ToString("N"), "Errors");
			Assert.AreEqual(0, MainForm.CountErrorFiles(folder, "*.htm"));
		}

		[TestMethod, TestCategory("errors")]
		public void The_reports_in_a_folder_are_counted()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-errors-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				File.WriteAllText(Path.Combine(folder, "one.htm"), "");
				File.WriteAllText(Path.Combine(folder, "two.htm"), "");
				File.WriteAllText(Path.Combine(folder, "not-a-report.txt"), "");
				Assert.AreEqual(2, MainForm.CountErrorFiles(folder, "*.htm"));
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}
	}
}
