// @under-test: Engine/Common/SettingsLocation.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Asking whether settings could live in a folder leaves the disk as it was, so a program started from a writable
	/// folder, such as a game's, does not leave an empty folder beside itself.
	/// </summary>
	[TestClass]
	public class SettingsLocationTest
	{
		[TestMethod]
		public void Asking_about_a_folder_that_is_not_there_leaves_none()
		{
			var programFolder = Path.Combine(Path.GetTempPath(), "x360ce-location-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(programFolder);
			try
			{
				var portable = SettingsLocation.Portable(programFolder);
				Assert.IsTrue(portable.CanWrite, portable.WriteProblem);
				Assert.IsFalse(Directory.Exists(portable.Path), "The check left " + portable.Path + " behind.");
			}
			finally
			{
				Directory.Delete(programFolder, true);
			}
		}
	}
}
