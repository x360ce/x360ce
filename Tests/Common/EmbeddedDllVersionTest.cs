// @under-test: Engine/Common/EngineHelper.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The version of the XInput library the program carries is read from the library's bytes, without writing it to disk,
	/// and is the version Windows reports for the same library as a file.
	/// </summary>
	[TestClass]
	public class EmbeddedDllVersionTest
	{
		[TestMethod, TestCategory("settings")]
		[Description("The version read from a library's bytes is the one Windows reads from the file, for x360ce's libraries and some of Windows' own")]
		public void Version_from_bytes_is_the_version_windows_reports()
		{
			var native = Path.Combine(Ui.RepoRoot.FullName, "Native", "x360ce");
			var libraries = new[] { "bin", "bin64" }
				.Select(x => Path.Combine(native, x))
				.Where(Directory.Exists)
				.SelectMany(x => new DirectoryInfo(x).GetFiles("xinput1_3.dll", SearchOption.AllDirectories))
				.Select(x => x.FullName)
				.ToList();
			if (libraries.Count == 0)
				Assert.Inconclusive("No x360ce XInput library is built.");
			var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
			libraries.AddRange(new[] { "kernel32.dll", "user32.dll", "xinput1_4.dll" }
				.Select(x => Path.Combine(system, x)).Where(File.Exists));
			foreach (var file in libraries)
			{
				var info = FileVersionInfo.GetVersionInfo(file);
				var expected = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
				Assert.AreEqual(expected, EngineHelper.GetFileVersion(File.ReadAllBytes(file)), file);
			}
		}

		[TestMethod, TestCategory("settings")]
		[Description("Bytes with no version block give 0.0.0.0")]
		public void Bytes_without_a_version_give_nothing()
		{
			Assert.AreEqual(new Version(0, 0, 0, 0), EngineHelper.GetFileVersion(new byte[64]));
		}
	}
}
