// @under-test: App.v4/Issues/ExeFileIssue.cs
// @area: diagnostics   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.App.Issues;

namespace x360ce.Tests
{
	/// <summary>
	/// A copy of the program inside a game's Program Files folder cannot save the game's settings
	/// beside itself without administrator rights. The Issues tab says so and offers to run
	/// elevated, instead of the save failing quietly or the start-up ending in an error.
	/// </summary>
	[TestClass]
	public class ExeFileIssueTest
	{
		[TestMethod, TestCategory("diagnostics"), TestCategory("critical")]
		[Description("Program Files, its 32-bit twin and the Windows folder are protected; a user folder is not")]
		public void Protected_folders_are_recognised()
		{
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			Assert.IsTrue(ExeFileIssue.IsProtectedFolder(Path.Combine(programFiles, "Some Game")));
			Assert.IsTrue(ExeFileIssue.IsProtectedFolder(Path.Combine(programFilesX86, "R.G. Mechanics", "Blur")));
			Assert.IsTrue(ExeFileIssue.IsProtectedFolder(windows));
			Assert.IsFalse(ExeFileIssue.IsProtectedFolder(Path.Combine(Path.GetTempPath(), "x360ce")));
			Assert.IsFalse(ExeFileIssue.IsProtectedFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
			// A folder that merely starts with the same letters is not inside it.
			Assert.IsFalse(ExeFileIssue.IsProtectedFolder(programFiles + " Extra"));
			Assert.IsFalse(ExeFileIssue.IsProtectedFolder(null));
		}
	}
}
