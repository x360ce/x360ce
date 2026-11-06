// @under-test: Engine/JocysCom/Common/Helper.cs
// @area: update   @layer: unit
using JocysCom.ClassLibrary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace x360ce.Tests
{
	/// <summary>A version is read from text that carries it, such as a release tag or a product's version line.</summary>
	/// <remarks>
	/// Version text often carries more than the numbers: a leading "v", a "-release-..." suffix, a build note in brackets.
	/// The parse used to refuse all of it, and a version line with a suffix stopped the update check and the About page.
	/// </remarks>
	[TestClass]
	public class VersionTextTest
	{
		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[DataRow("18.11.0-release-26425-03", "18.11.0")]
		[DataRow("v1.5.230.0", "1.5.230.0")]
		[DataRow("4.29.45.0", "4.29.45.0")]
		[DataRow("10.0.26100.1 (WinBuild.160101.0800)", "10.0.26100.1")]
		[Description("The first version written in the text is read, whatever follows it")]
		public void The_first_version_in_the_text_is_read(string text, string expected)
		{
			Version version;
			Assert.IsTrue(Helper.TryParseVersion(text, out version), "No version was read from \"" + text + "\".");
			Assert.AreEqual(new Version(expected), version);
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[DataRow("1.2.3.4.5")]
		[DataRow("abc")]
		[DataRow("")]
		[DataRow(null)]
		[DataRow("99999999999.1")]
		[Description("Text with no version, a version of five parts, or a number too large for one, gives none")]
		public void Text_without_a_version_gives_none(string text)
		{
			Version version;
			Assert.IsFalse(Helper.TryParseVersion(text, out version), "A version was read from \"" + text + "\": " + version);
		}
	}
}
