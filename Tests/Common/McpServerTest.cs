// @under-test: Engine/JocysCom/Mcp/McpServer.cs
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
using JocysCom.ClassLibrary.Mcp;

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

			[McpTool(AiAccess.Configure, "Sets, as the real ui_set does")]
			public static string UiSet(string path, string value) { return null; }
		}

		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		/// <summary>Where the tests' own calls are logged: a temp folder, so the machine's log stays a record of real assistants.</summary>
		public static readonly string LogFolder = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "x360ce-tests")).FullName;

		public static void UseSample(AiAccess level)
		{
			McpLog.Folder = LogFolder;
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
		[Description("A password or the token box's value sent through the door is masked in the log, and the token never appears in it")]
		public void The_log_keeps_no_secret()
		{
			UseSample(AiAccess.Configure);
			var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "x360ce-log-" + Guid.NewGuid().ToString("N"));
			System.IO.Directory.CreateDirectory(folder);
			McpLog.Folder = folder;
			try
			{
				CallTool("ui_set", new Dictionary<string, object> { { "path", "Options/RemotePasswordTextBox" }, { "value", "hunter2" } });
				CallTool("ui_set", new Dictionary<string, object> { { "path", "Tabs/Options/AiAccessTokenTextBox" }, { "value", "typed-secret" } });
				var token = new string('a', 64);
				CallTool("poke_value", new Dictionary<string, object> { { "value", token } });
				var log = System.IO.File.ReadAllText(McpLog.Path);
				StringAssert.Contains(log, "RemotePasswordTextBox");
				StringAssert.Contains(log, "AiAccessTokenTextBox");
				Assert.IsFalse(log.Contains("hunter2"), "The password was written to the log.");
				Assert.IsFalse(log.Contains("typed-secret"), "The value given to the token box was written to the log.");
				Assert.IsFalse(log.Contains(token), "The token was written to the log.");
				StringAssert.Contains(log, "<token>");
			}
			finally
			{
				McpLog.Folder = LogFolder;
				System.IO.Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Initialize names the protocol, the program and its version, and passes on the program's instructions followed by the door-only rule, which is sent even when the program has none")]
		public void Initialize_names_protocol_and_program()
		{
			UseSample(AiAccess.Read);
			var saved = McpServer.Instructions;
			try
			{
				McpServer.Instructions = null;
				var r = (Dictionary<string, object>)Call("initialize", new Dictionary<string, object>())["result"];
				Assert.AreEqual(McpServer.ProtocolVersion, r["protocolVersion"]);
				var serverInfo = (Dictionary<string, object>)r["serverInfo"];
				Assert.IsTrue(serverInfo["name"].ToString().Contains("x360ce"));
				Assert.AreEqual(Application.ProductVersion, serverInfo["version"], "The version is not the program's.");
				Assert.AreEqual(McpServer.DoorOnly, r["instructions"], "Without instructions of the program's own, the door-only rule is sent alone.");
				McpServer.Instructions = "Start with devices_list.";
				r = (Dictionary<string, object>)Call("initialize", new Dictionary<string, object>())["result"];
				Assert.AreEqual("Start with devices_list. " + McpServer.DoorOnly, r["instructions"]);
			}
			finally { McpServer.Instructions = saved; }
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Each listed tool says whether it changes anything: Read tools are read-only, the rest may replace what was set, and nothing leaves the computer")]
		public void Tools_carry_hints_from_their_level()
		{
			UseSample(AiAccess.Read);
			var tools = ((object[])((Dictionary<string, object>)Call("tools/list", null)["result"])["tools"]).Cast<Dictionary<string, object>>().ToList();
			var peek = (Dictionary<string, object>)tools.Single(t => (string)t["name"] == "peek")["annotations"];
			Assert.AreEqual(true, peek["readOnlyHint"]);
			Assert.AreEqual(false, peek["destructiveHint"]);
			Assert.AreEqual(false, peek["openWorldHint"]);
			var poke = (Dictionary<string, object>)tools.Single(t => (string)t["name"] == "poke_value")["annotations"];
			Assert.AreEqual(false, poke["readOnlyHint"]);
			Assert.AreEqual(true, poke["destructiveHint"]);
			Assert.IsFalse(new McpToolInfo { Level = AiAccess.Read, Changes = true }.ReadOnly, "A Read tool that can still change things, such as a script, is not read-only.");
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Only a page on this computer is a local origin; anything else, or an origin that is no address, is not")]
		public void Only_pages_on_this_computer_are_local_origins()
		{
			foreach (var local in new[] { "http://localhost", "http://localhost:6274", "http://127.0.0.1:3000", "http://[::1]:8080" })
				Assert.IsTrue(McpListener.IsLocalOrigin(local), local);
			foreach (var foreign in new[] { "http://example.com", "https://attacker.example:37360", "http://192.168.1.2", "null", "" })
				Assert.IsFalse(McpListener.IsLocalOrigin(foreign), foreign);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Every tool is listed whatever the level, so the list never changes; the level is enforced when a tool is called")]
		public void Tools_are_listed_whatever_the_level()
		{
			// The list does not depend on a setting, so an assistant sees what more access would allow.
			// What the level gates is the call, tested below.
			UseSample(AiAccess.Read);
			Assert.AreEqual(5, ((object[])((Dictionary<string, object>)Call("tools/list", null)["result"])["tools"]).Length);
			UseSample(AiAccess.Configure);
			Assert.AreEqual(5, ((object[])((Dictionary<string, object>)Call("tools/list", null)["result"])["tools"]).Length);
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Arguments bind by name whatever their case, defaults fill in, one the tool does not take is refused, and a tool above the level is refused naming the level needed")]
		public void Arguments_bind_and_levels_gate()
		{
			UseSample(AiAccess.Read);
			var args = new Dictionary<string, object> { { "value", "x" } };
			// A result the model reads, not a protocol error: many clients hide those from the model.
			var refused = (Dictionary<string, object>)CallTool("poke_value", args)["result"];
			Assert.AreEqual(true, refused["isError"]);
			StringAssert.Contains(Text(refused), "needs Configure access");
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
			// Dropped, an invented argument looks as if it worked: one caller asked for depth 3 and got the whole branch.
			var unknown = (Dictionary<string, object>)CallTool("poke_value", new Dictionary<string, object> { { "value", "x" }, { "depth", "3" } })["error"];
			Assert.AreEqual(-32602, unknown["code"]);
			StringAssert.Contains((string)unknown["message"], "depth");
			StringAssert.Contains((string)unknown["message"], "value, times", "The refusal names the arguments the tool does take.");
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
