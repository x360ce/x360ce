// @under-test: Engine/JocysCom/Configuration/AssemblyInfo.cs, App.v4/x360ce.App.v4.csproj
// @area: about   @layer: unit
using JocysCom.ClassLibrary.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace x360ce.Tests
{
	/// <summary>
	/// The build date in the title and in error reports is the day the program was built.
	/// </summary>
	/// <remarks>
	/// Both read the time the linker writes into the file's header. A deterministic build writes
	/// a hash of the content there instead, so identical sources give identical bytes, and the
	/// title then showed whatever date that hash happened to spell, 1914 in one release. The
	/// application projects build non-deterministically so the header holds the real time.
	/// </remarks>
	[TestClass]
	public class BuildDateTest
	{
		[TestMethod, TestCategory("about")]
		public void The_title_date_is_when_the_program_was_built()
		{
			var path = typeof(x360ce.App.MainForm).Assembly.Location;
			var built = AssemblyInfo.GetBuildDateTime(path);
			var written = File.GetLastWriteTime(path);
			// The link and the file write are seconds apart; a day allows for any clock or zone slip.
			Assert.IsTrue(Math.Abs((built - written).TotalDays) < 1, string.Format(
				"{0} says it was built {1:yyyy-MM-dd HH:mm} and was written {2:yyyy-MM-dd HH:mm}: the header holds something other than the build time.",
				Path.GetFileName(path), built, written));
		}
	}
}
