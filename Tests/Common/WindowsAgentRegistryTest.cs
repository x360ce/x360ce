// @under-test: App.v4/Mcp/WindowsAgentRegistry.cs
// @area: mcp   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>The bundle Windows reads to find the program, and the registering that only happens where Windows can take it.</summary>
	[TestClass]
	public class WindowsAgentRegistryTest
	{
		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		static Dictionary<string, object> Node(object value)
		{
			return (Dictionary<string, object>)value;
		}

		/// <summary>Runs the test with the registry tool stubbed and every file written into a temp folder, then puts the real tool and folders back.</summary>
		static void WithStub(Func<bool> available, Func<string, int> run, Action<string> test)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.WindowsAgentRegistryTest." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			var originalAvailable = WindowsAgentRegistry.Available;
			var originalRun = WindowsAgentRegistry.Run;
			WindowsAgentRegistry.Available = available;
			WindowsAgentRegistry.Run = run;
			WindowsAgentRegistry.Folder = folder;
			McpLog.Folder = folder;
			try { test(folder); }
			finally
			{
				WindowsAgentRegistry.Available = originalAvailable;
				WindowsAgentRegistry.Run = originalRun;
				WindowsAgentRegistry.Folder = null;
				WindowsAgentRegistry.LastError = null;
				McpLog.Folder = null;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("The manifest names the program, how to start it, every tool from the catalogue, and the same initialize and tools/list answers the running server gives")]
		public void Manifest_describes_program_tools_and_static_answers()
		{
			McpCatalog.Load(typeof(McpTools));
			var exePath = @"C:\Games\x360ce\x360ce.exe";
			var manifest = Node(Json.DeserializeObject(WindowsAgentRegistry.Manifest(exePath)));
			Assert.AreEqual("0.3", manifest["manifest_version"]);
			Assert.AreEqual("x360ce", manifest["name"]);
			Assert.AreEqual(WindowsAgentRegistry.ServerName, manifest["name"]);
			Assert.AreEqual(Application.ProductVersion, manifest["version"]);
			var server = Node(manifest["server"]);
			Assert.AreEqual("binary", server["type"]);
			var config = Node(server["mcp_config"]);
			Assert.AreEqual(exePath, config["command"]);
			CollectionAssert.AreEqual(new object[] { "/Mcp" }, (object[])config["args"]);
			var toolNames = ((object[])manifest["tools"]).Select(t => (string)Node(t)["name"]).ToArray();
			CollectionAssert.AreEqual(McpCatalog.Tools.Select(t => t.Name).ToArray(), toolNames, "The tools are the catalogue, in its order.");
			var responses = Node(Node(Node(manifest["_meta"])["com.microsoft.windows"])["static_responses"]);
			Assert.AreEqual(Json.Serialize(McpServer.ToolsList()), Json.Serialize(responses["tools/list"]), "Windows checks tools/list against the server; the two must agree.");
			Assert.AreEqual(Json.Serialize(McpServer.InitializeResult()), Json.Serialize(responses["initialize"]));
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Where Windows has no registry tool, applying either way writes nothing, runs nothing, and reports no error")]
		public void Apply_does_nothing_without_the_registry_tool()
		{
			var calls = new List<string>();
			WithStub(() => false, a => { calls.Add(a); return 0; }, folder =>
			{
				Assert.IsFalse(WindowsAgentRegistry.IsAvailable);
				WindowsAgentRegistry.Apply(true);
				WindowsAgentRegistry.Apply(false);
				Assert.AreEqual(0, calls.Count, "The tool must not be run where it does not exist.");
				Assert.IsFalse(File.Exists(WindowsAgentRegistry.ManifestPath), "No manifest is written where nothing reads it.");
				Assert.IsNull(WindowsAgentRegistry.LastError);
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Registering writes the manifest beside the settings and adds it through the tool; a tool that fails leaves the reason")]
		public void Apply_registers_through_the_tool_and_keeps_a_failure()
		{
			var calls = new List<string>();
			var exitCode = 0;
			WithStub(() => true, a => { calls.Add(a); return exitCode; }, folder =>
			{
				Assert.IsTrue(WindowsAgentRegistry.IsAvailable);
				Assert.AreEqual(Path.Combine(folder, "x360ce.mcpb.json"), WindowsAgentRegistry.ManifestPath);
				WindowsAgentRegistry.Apply(true);
				Assert.IsTrue(File.Exists(WindowsAgentRegistry.ManifestPath), "The manifest must be on disk before the tool is pointed at it.");
				var written = Node(Json.DeserializeObject(File.ReadAllText(WindowsAgentRegistry.ManifestPath)));
				Assert.AreEqual(Application.ExecutablePath, Node(Node(written["server"])["mcp_config"])["command"]);
				Assert.AreEqual(1, calls.Count);
				Assert.AreEqual("mcp add \"" + WindowsAgentRegistry.ManifestPath + "\"", calls[0]);
				Assert.IsNull(WindowsAgentRegistry.LastError);
				// Applied again at every start: an unchanged manifest means Windows already knows, so the tool is not run.
				WindowsAgentRegistry.Apply(true);
				Assert.AreEqual(1, calls.Count, "An unchanged registration must not run the tool again.");
				File.WriteAllText(WindowsAgentRegistry.ManifestPath, "{}");
				exitCode = 5;
				WindowsAgentRegistry.Apply(true);
				Assert.AreEqual(2, calls.Count, "A changed manifest is registered again.");
				Assert.IsNotNull(WindowsAgentRegistry.LastError, "A failed registration must say why.");
				StringAssert.Contains(WindowsAgentRegistry.LastError, "odr.exe");
				StringAssert.Contains(WindowsAgentRegistry.LastError, "5");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Unregistering removes the server by name through the tool, only when it was ever added, and forgets the manifest")]
		public void Apply_unregisters_by_name()
		{
			var calls = new List<string>();
			WithStub(() => true, a => { calls.Add(a); return 0; }, folder =>
			{
				WindowsAgentRegistry.Apply(false);
				Assert.AreEqual(0, calls.Count, "Nothing was ever added, so there is nothing to remove.");
				WindowsAgentRegistry.Apply(true);
				WindowsAgentRegistry.Apply(false);
				CollectionAssert.AreEqual(new[] { "mcp add \"" + WindowsAgentRegistry.ManifestPath + "\"", "mcp remove x360ce" }, calls);
				Assert.IsFalse(File.Exists(WindowsAgentRegistry.ManifestPath), "A removed registration leaves no manifest behind.");
				Assert.IsNull(WindowsAgentRegistry.LastError);
			});
		}
	}
}
