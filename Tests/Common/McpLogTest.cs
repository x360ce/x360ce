// @under-test: App.v4/Mcp/McpLog.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>The record of what an assistant did: one dated line per action, clipped to what a reader needs, rolled before it grows past reading.</summary>
	[TestClass]
	public class McpLogTest
	{
		static void InTempFolder(Action<string> test)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.McpLogTest." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			McpLog.Folder = folder;
			try { test(folder); }
			finally
			{
				McpLog.Folder = null;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A write appends one dated, tab-separated line, and line breaks or tabs inside the message become spaces so the line stays one line")]
		public void Write_appends_one_dated_line_with_breaks_flattened()
		{
			InTempFolder(folder =>
			{
				Assert.AreEqual(Path.Combine(folder, "x360ce.AiAccess.log"), McpLog.Path);
				McpLog.Write("first");
				McpLog.Write("two\r\nlines\tand a tab");
				var lines = File.ReadAllLines(McpLog.Path);
				Assert.AreEqual(2, lines.Length, "Each write is one line.");
				StringAssert.Matches(lines[0], new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\tfirst$"));
				StringAssert.Matches(lines[1], new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\ttwo  lines and a tab$"));
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Clip returns short text whole, cuts long text to the limit with its full length appended, and turns null into an empty string")]
		public void Clip_keeps_short_cuts_long_and_empties_null()
		{
			Assert.AreEqual("short", McpLog.Clip("short", 10));
			Assert.AreEqual("exact", McpLog.Clip("exact", 5));
			Assert.AreEqual("abcde... (26 chars)", McpLog.Clip("abcdefghijklmnopqrstuvwxyz", 5));
			Assert.AreEqual("a  b c", McpLog.Clip("a\r\nb\tc", 10), "Clipped text is flattened like a written line: each break character becomes one space.");
			Assert.AreEqual("", McpLog.Clip(null, 10));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A log past 5 MB is moved aside to .old before the next line, which starts a fresh file")]
		public void Write_rolls_the_file_to_old_when_over_5_MB()
		{
			InTempFolder(folder =>
			{
				File.WriteAllBytes(McpLog.Path, new byte[5 * 1024 * 1024 + 1]);
				McpLog.Write("after roll");
				var old = McpLog.Path + ".old";
				Assert.IsTrue(File.Exists(old), "The full log must be kept as .old.");
				Assert.AreEqual(5 * 1024 * 1024 + 1, new FileInfo(old).Length, "The .old file is the log as it was.");
				var lines = File.ReadAllLines(McpLog.Path);
				Assert.AreEqual(1, lines.Length, "The new log holds only the line written after the roll.");
				StringAssert.EndsWith(lines[0], "\tafter roll");
			});
		}
	}
}
