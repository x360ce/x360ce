// @under-test: App.v4/Mcp/McpClient.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using x360ce.App;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>The two switches: lines carried for an assistant, and one call printed for a script. Both ride the same post.</summary>
	[TestClass]
	public class McpClientTest
	{
		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Each line in is one request forwarded; notifications produce no line out; a failed post is an error line, not the end")]
		public void Stdio_lines_in_are_requests_and_answers_are_lines_out()
		{
			var sent = new List<string>();
			var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}\n{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"ping\"}\n");
			var output = new StringWriter();
			var code = McpClient.RunStdio(input, output, body =>
			{
				sent.Add(body);
				if (body.Contains("\"id\":7")) throw new WebException("401");
				return body.Contains("ping") ? "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}" : "";
			});
			Assert.AreEqual(0, code);
			Assert.AreEqual(3, sent.Count);
			var lines = output.ToString().Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(2, lines.Length, "A notification must not produce an output line; a failed post must.");
			StringAssert.Contains(lines[0], "\"result\"");
			StringAssert.Contains(lines[1], "\"error\"");
			StringAssert.Contains(lines[1], "\"id\":7");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A command line call sends one tools/call and prints what came back")]
		public void Command_sends_one_call_and_prints_the_answer()
		{
			string sent = null;
			var output = new StringWriter();
			var code = McpClient.RunCommand("ui_set", new Dictionary<string, string> { { "path", "A/B" }, { "value", "5" } }, output,
				body => { sent = body; return "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"Done.\"}]}}"; });
			Assert.AreEqual(0, code);
			StringAssert.Contains(sent, "\"name\":\"ui_set\"");
			StringAssert.Contains(sent, "\"path\":\"A/B\"");
			Assert.AreEqual("Done.", output.ToString().Trim());
			output = new StringWriter();
			var failed = McpClient.RunCommand("ui_set", new Dictionary<string, string>(), output,
				body => "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32602,\"message\":\"Missing argument: path.\"}}");
			Assert.AreEqual(1, failed);
			StringAssert.Contains(output.ToString(), "Missing argument");
			output = new StringWriter();
			var toolFailed = McpClient.RunCommand("boom", new Dictionary<string, string>(), output,
				body => "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"content\":[{\"type\":\"text\",\"text\":\"no device\"}],\"isError\":true}}");
			Assert.AreEqual(1, toolFailed);
			StringAssert.Contains(output.ToString(), "no device");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Usage is written from the catalogue, with every tool's level and arguments")]
		public void Usage_comes_from_the_catalogue()
		{
			McpServerTest.UseSample(AiAccess.Read);
			var usage = McpClient.Usage();
			StringAssert.Contains(usage, "poke_value");
			StringAssert.Contains(usage, "(Configure)");
			StringAssert.Contains(usage, "/times=[optional] How many times.");
			StringAssert.Contains(usage, "/value=What to write.");
		}
	}
}
