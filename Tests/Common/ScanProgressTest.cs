// @under-test: App.v4/Controls/PadTabPages/GamesControl.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>What the scan reports while it runs, including in a folder that has nothing to scan.</summary>
	[TestClass]
	public class ScanProgressTest
	{
		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A folder with no programs reports its update without an index into an empty list")]
		public void An_empty_folder_reports_without_failing()
		{
			var text = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 1,
				State = XInputMaskScannerState.FileUpdate,
				Files = new List<FileInfo>(),
				FileIndex = 0,
				Message = "Scan file 0 of 0. Please wait...",
			});
			StringAssert.Contains(text, "Scan file 0 of 0");
			Assert.IsFalse(text.Contains("Current File"), "There is no current file in an empty folder.");
			var folders = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 0,
				State = XInputMaskScannerState.DirectoryUpdate,
				Directories = new List<DirectoryInfo>(),
				DirectoryIndex = 0,
				Message = "Searching",
			});
			StringAssert.Contains(folders, "Skipped = 0");
			Assert.IsFalse(folders.Contains("Current Folder"));
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A file being scanned is named with its size")]
		public void The_current_file_is_named()
		{
			var file = new FileInfo(typeof(XInputMaskScanner).Assembly.Location);
			var text = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 1,
				State = XInputMaskScannerState.FileUpdate,
				Files = new List<FileInfo> { file },
				FileIndex = 0,
				Message = "Scan file 1 of 1. Please wait...",
			});
			StringAssert.Contains(text, "Current File");
			StringAssert.Contains(text, file.FullName);
		}
	}
}
