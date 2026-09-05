// @under-test: App.v4/Mcp/McpServer.cs
// @area: mcp   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>The catalogue read from a class, and the conversation with an assistant without a socket.</summary>
	[TestClass]
	public class McpServerTest
	{
		/// <summary>A stand-in catalogue, so the test depends on neither the real tools nor a window.</summary>
		public static class Sample
		{
			[McpTool(AiAccess.Read, "Reads")]
			public static string Peek() { return "peeked"; }

			[McpTool(AiAccess.Configure, "Writes")]
			public static string PokeValue([System.ComponentModel.Description("What to write.")] string value, [System.ComponentModel.Description("How many times.")] int times = 1)
			{ return string.Concat(Enumerable.Repeat("poked " + value + " ", times)).Trim(); }

			[McpTool(AiAccess.Read, "Lists")]
			public static object Rows() { return new object[] { new Dictionary<string, object> { { "Name", "A" } } }; }

			[McpTool(AiAccess.Read, "Fails")]
			public static string Boom() { throw new InvalidOperationException("no device"); }
		}

		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		public static void UseSample(AiAccess level)
		{
			McpCatalog.Load(typeof(Sample));
			McpCatalog.Level = () => level;
			McpCatalog.OnUiThread = a => a();
		}

		static Dictionary<string, object> Call(string method, object @params)
		{
			var request = Json.Serialize(new Dictionary<string, object> { { "jsonrpc", "2.0" }, { "id", 1 }, { "method", method }, { "params", @params } });
			return (Dictionary<string, object>)Json.DeserializeObject(McpServer.Handle(request));
		}

		static Dictionary<string, object> CallTool(string name, Dictionary<string, object> arguments)
		{
			return Call("tools/call", new Dictionary<string, object> { { "name", name }, { "arguments", arguments } });
		}

		static string Text(Dictionary<string, object> result)
		{
			return (string)((Dictionary<string, object>)((object[])result["content"])[0])["text"];
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The catalogue names tools in snake case and describes their arguments from the method")]
		public void Catalogue_is_read_from_the_methods()
		{
			UseSample(AiAccess.Read);
			var poke = McpCatalog.Tools.First(t => t.Name == "poke_value");
			Assert.AreEqual(AiAccess.Configure, poke.Level);
			Assert.AreEqual("Writes", poke.Description);
			var schema = poke.InputSchema();
			var properties = (Dictionary<string, object>)schema["properties"];
			Assert.AreEqual("string", ((Dictionary<string, object>)properties["value"])["type"]);
			Assert.AreEqual("integer", ((Dictionary<string, object>)properties["times"])["type"]);
			Assert.AreEqual("How many times.", ((Dictionary<string, object>)properties["times"])["description"]);
			CollectionAssert.AreEqual(new[] { "value" }, (string[])schema["required"], "Only the parameter without a default is required.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Initialize names the protocol and the program")]
		public void Initialize_names_protocol_and_program()
		{
			UseSample(AiAccess.Read);
			var r = (Dictionary<string, object>)Call("initialize", new Dictionary<string, object>())["result"];
			Assert.AreEqual(McpServer.ProtocolVersion, r["protocolVersion"]);
			Assert.IsTrue(((Dictionary<string, object>)r["serverInfo"])["name"].ToString().Contains("x360ce"));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Only tools the level allows are listed")]
		public void Tools_are_listed_by_level()
		{
			UseSample(AiAccess.Read);
			Assert.AreEqual(3, ((object[])((Dictionary<string, object>)Call("tools/list", null)["result"])["tools"]).Length);
			UseSample(AiAccess.Configure);
			Assert.AreEqual(4, ((object[])((Dictionary<string, object>)Call("tools/list", null)["result"])["tools"]).Length);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Arguments bind by name whatever their case, defaults fill in, and a tool above the level is refused naming the level needed")]
		public void Arguments_bind_and_levels_gate()
		{
			UseSample(AiAccess.Read);
			var args = new Dictionary<string, object> { { "value", "x" } };
			var refused = (Dictionary<string, object>)CallTool("poke_value", args)["error"];
			Assert.AreEqual(-32001, refused["code"]);
			StringAssert.Contains(refused["message"].ToString(), "Configure");
			UseSample(AiAccess.Configure);
			Assert.AreEqual("poked x", Text((Dictionary<string, object>)CallTool("poke_value", args)["result"]));
			var twice = new Dictionary<string, object> { { "value", "x" }, { "times", 2 } };
			Assert.AreEqual("poked x poked x", Text((Dictionary<string, object>)CallTool("poke_value", twice)["result"]));
			var mixedCase = new Dictionary<string, object> { { "VALUE", "y" } };
			Assert.AreEqual("poked y", Text((Dictionary<string, object>)CallTool("poke_value", mixedCase)["result"]), "The command line lower-cases names; binding must not care.");
			var missing = (Dictionary<string, object>)CallTool("poke_value", new Dictionary<string, object>())["error"];
			Assert.AreEqual(-32602, missing["code"]);
			var wrongType = (Dictionary<string, object>)CallTool("poke_value", new Dictionary<string, object> { { "value", "x" }, { "times", "many" } })["error"];
			Assert.AreEqual(-32602, wrongType["code"]);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A tool's object comes back as JSON; a failure comes back as a result marked as an error; protocol faults are JSON-RPC errors")]
		public void Results_and_failures_are_reported_not_thrown()
		{
			UseSample(AiAccess.Administer);
			StringAssert.Contains(Text((Dictionary<string, object>)CallTool("rows", null)["result"]), "\"Name\":\"A\"");
			Assert.AreEqual(-32601, ((Dictionary<string, object>)Call("nothing/here", null)["error"])["code"]);
			var bad = (Dictionary<string, object>)Json.DeserializeObject(McpServer.Handle("{not json"));
			Assert.AreEqual(-32700, ((Dictionary<string, object>)bad["error"])["code"]);
			var notObject = (Dictionary<string, object>)Json.DeserializeObject(McpServer.Handle("null"));
			Assert.AreEqual(-32600, ((Dictionary<string, object>)notObject["error"])["code"]);
			var failed = (Dictionary<string, object>)CallTool("boom", null)["result"];
			Assert.AreEqual(true, failed["isError"]);
			StringAssert.Contains(Text(failed), "no device");
			Assert.AreEqual("", McpServer.Handle("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}"));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The interface-thread seam runs the action on the interface thread and carries a failure back to the calling thread")]
		public void The_ui_seam_carries_exceptions_across_threads()
		{
			// ControlsHelper.Invoke hands the action to the interface thread and never looks at what
			// became of it, so a tool that threw there would answer "Done.". This calls the seam from
			// another thread, the way a request does, while the interface thread pumps messages.
			// The helper binds to the first thread that asks and keeps that binding for the life of
			// the process, so this test alone rebinds it to its own thread, as PostedFailureTest does.
			Ui.OnUiThread(() =>
			{
				Ui.ReleaseInvokeContext();
				ControlsHelper.InitInvokeContext();
				var uiThread = Thread.CurrentThread.ManagedThreadId;
				Exception carried = null;
				int ranOn = 0;
				var done = new ManualResetEventSlim(false);
				var caller = new Thread(() =>
				{
					try { McpCatalog.Marshal(() => { ranOn = Thread.CurrentThread.ManagedThreadId; throw new InvalidOperationException("inside"); }); }
					catch (Exception ex) { carried = ex; }
					finally { done.Set(); }
				}) { IsBackground = true };
				caller.Start();
				var until = DateTime.Now.AddSeconds(10);
				while (!done.IsSet && DateTime.Now < until)
				{
					Application.DoEvents();
					Thread.Sleep(10);
				}
				Assert.IsTrue(done.IsSet, "The seam never returned to the caller.");
				Assert.AreEqual(uiThread, ranOn, "The action did not run on the interface thread.");
				Assert.IsInstanceOfType(carried, typeof(InvalidOperationException), "The failure did not reach the caller.");
				Assert.AreEqual("inside", carried.Message);
			});
		}
	}
}
