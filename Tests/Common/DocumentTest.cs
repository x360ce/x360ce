// @under-test: Engine/JocysCom/Controls/ControlsHelper.Windows.cs, App.v4/Controls/AboutControl.cs
// @area: about   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The About screen shows the change log and licence in a multiline TextBox, which breaks a
	/// line only on a carriage return and line feed pair. A document saved with Unix endings
	/// renders as one unbroken paragraph, so both the documents and the loader are covered here.
	/// </summary>
	[TestClass]
	public class DocumentTest
	{

		static readonly string[] ShippedDocuments =
		{
			"App.v4/Documents/ChangeLog.txt",
			"App.v4/Documents/License.txt",
			"App.v3/Documents/ChangeLog.txt",
			"App.v3/Documents/License.txt",
		};

		[TestMethod, TestCategory("about"), TestCategory("smoke")]
		[Description("Shipped documents use carriage return and line feed pairs throughout")]
		public void Shipped_documents_use_windows_line_breaks()
		{
			foreach (var relative in ShippedDocuments)
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
				if (!File.Exists(path))
					continue;
				var bytes = File.ReadAllBytes(path);
				var lone = LoneLineFeeds(bytes);
				Assert.AreEqual(0, lone,
					relative + " has " + lone + " line feeds without a carriage return. " +
					"The About screen would show it as a single paragraph.");
			}
		}

		/// <summary>Count line feeds that are not preceded by a carriage return.</summary>
		static int LoneLineFeeds(byte[] bytes)
		{
			var count = 0;
			for (var i = 0; i < bytes.Length; i++)
				if (bytes[i] == 0x0A && (i == 0 || bytes[i - 1] != 0x0D))
					count++;
			return count;
		}

		[TestMethod, TestCategory("about"), TestCategory("smoke")]
		[Description("Any line ending style is converted to the pair a TextBox understands")]
		public void Line_breaks_are_normalised_to_windows_pairs()
		{
			// Unix, classic Mac, already-correct, and a file that mixes all three.
			Assert.AreEqual("a\r\nb", ControlsHelper.NormalizeLineBreaks("a\nb"), "line feed only");
			Assert.AreEqual("a\r\nb", ControlsHelper.NormalizeLineBreaks("a\rb"), "carriage return only");
			Assert.AreEqual("a\r\nb", ControlsHelper.NormalizeLineBreaks("a\r\nb"), "already a pair");
			Assert.AreEqual("a\r\nb\r\nc\r\nd", ControlsHelper.NormalizeLineBreaks("a\r\nb\nc\rd"), "mixed");
			Assert.AreEqual("", ControlsHelper.NormalizeLineBreaks(""), "empty");
			Assert.IsNull(ControlsHelper.NormalizeLineBreaks(null), "null");
		}

		[TestMethod, TestCategory("about"), TestCategory("smoke")]
		[Description("Normalising twice changes nothing, so a correct file is left alone")]
		public void Normalising_is_repeatable()
		{
			var once = ControlsHelper.NormalizeLineBreaks("a\nb\r\nc\rd");
			Assert.AreEqual(once, ControlsHelper.NormalizeLineBreaks(once),
				"A second pass doubled the line breaks, which would space the document out.");
		}

		[TestMethod, TestCategory("about"), TestCategory("smoke")]
		[Description("The change log opens with the newest version heading on its own line")]
		public void Change_log_starts_with_a_version_heading()
		{
			foreach (var relative in ShippedDocuments.Where(x => x.EndsWith("ChangeLog.txt")))
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
				if (!File.Exists(path))
					continue;
				var text = ControlsHelper.NormalizeLineBreaks(File.ReadAllText(path));
				var lines = text.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
				Assert.IsTrue(lines.Length > 10,
					relative + " split into only " + lines.Length + " lines, so its breaks were lost.");
				Assert.IsTrue(lines[0].TrimStart('﻿').StartsWith("v"),
					relative + " starts with '" + lines[0] + "' rather than a version heading.");
			}
		}

	}
}
