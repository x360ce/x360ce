// @under-test: App.v4/Mcp/McpServer.cs, App.v4/Mcp/McpTools.cs, App.v4/Program.cs
// @area: mcp   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using x360ce.App;
using x360ce.App.Mcp;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>What an assistant experiences: the program started, the door found, a setting changed and read back.</summary>
	[TestClass]
	public class McpEndToEndTest
	{
		static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

		[TestMethod, TestCategory("mcp"), TestCategory("ui-interactive"), Timeout(120000)]
		[Description("An assistant can list tools, find the overall force strength by reading the tree, set it and read it back")]
		public void Assistant_sets_a_slider_and_reads_it_back()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			// The program and this test each resolve their own settings folder from where they run;
			// with a settings folder beside the built program they would read two different files.
			var programSettings = SettingsLocation.Resolve(Path.GetDirectoryName(exe)).Path;
			if (!string.Equals(programSettings, EngineHelper.AppDataPath, StringComparison.OrdinalIgnoreCase))
				Assert.Inconclusive("The program reads settings from " + programSettings + " and this test from " + EngineHelper.AppDataPath + ".");
			var o = SettingsManager.Options;
			var previous = o.AiAccess;
			o.AiAccess = AiAccess.Configure;
			o.EnsureAiAccessToken();
			SettingsManager.OptionsData.Save();
			Process process = null;
			try
			{
				// Started the way the switches start it: nothing answers, so the client launches the
				// program and waits for the door, ignoring its own process, which carries the same name.
				McpClient.EnsureRunning(o.AiAccessPort, o.AiAccessToken, exe);
				var self = Process.GetCurrentProcess().Id;
				process = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)).FirstOrDefault(p => p.Id != self);
				Assert.IsNotNull(process, "The client said the program answers, but no such process is running.");
				StringAssert.Contains(Post("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}"), "ui_set");
				// The path is read from the tree, the way an assistant finds it, not written here.
				var tree = (Dictionary<string, object>)Json.DeserializeObject(Text(Post(Call(2, "ui_read", "{}"))));
				var path = PathOf(tree, "ForceOverallTrackBar", "Pad1TabPage");
				Assert.IsNotNull(path, "The overall force strength slider is not in the tree under controller 1.");
				StringAssert.Contains(Text(Post(Call(3, "ui_set", "{\"path\":\"" + path + "\",\"value\":\"35\"}"))), "Done");
				var read = (Dictionary<string, object>)Json.DeserializeObject(Text(Post(Call(4, "ui_read", "{\"path\":\"" + path + "\"}"))));
				Assert.AreEqual("35", read["Value"]);
			}
			finally
			{
				if (process != null)
					Ui.CloseApp(process);
				o.AiAccess = previous;
				SettingsManager.OptionsData.Save();
			}
		}

		static string Post(string body)
		{
			var o = SettingsManager.Options;
			return McpClient.Post(o.AiAccessPort, o.AiAccessToken, body);
		}

		static string Call(int id, string tool, string arguments)
		{
			return "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"method\":\"tools/call\",\"params\":{\"name\":\"" + tool + "\",\"arguments\":" + arguments + "}}";
		}

		static string Text(string response)
		{
			var answer = (Dictionary<string, object>)Json.DeserializeObject(response);
			Assert.IsFalse(answer.ContainsKey("error"), "The program answered with an error: " + response);
			return (string)((Dictionary<string, object>)((object[])((Dictionary<string, object>)answer["result"])["content"])[0])["text"];
		}

		/// <summary>The Path of the node with the given Id, searched only under the node with the other Id.</summary>
		static string PathOf(Dictionary<string, object> node, string id, string under, bool inside = false)
		{
			object value;
			var here = node.TryGetValue("Id", out value) && (string)value == under;
			if ((inside || here) && node.TryGetValue("Id", out value) && (string)value == id)
				return (string)node["Path"];
			object items;
			if (!node.TryGetValue("Items", out items) || !(items is object[]))
				return null;
			foreach (var child in (object[])items)
			{
				var found = PathOf((Dictionary<string, object>)child, id, under, inside || here);
				if (found != null)
					return found;
			}
			return null;
		}
	}
}
