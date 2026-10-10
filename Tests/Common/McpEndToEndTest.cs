// @under-test: Engine/JocysCom/Mcp/McpServer.cs, Engine/JocysCom/Mcp/McpClient.cs, App.v4/Mcp/McpTools.cs, App.v4/Program.cs, Engine/Common/EngineHelper.cs, App.v4/Controls/OptionsUserControl.cs
// @area: mcp   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using x360ce.App;
using x360ce.App.Mcp;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Mcp;
using JocysCom.ClassLibrary.Runtime;
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
			var previouslyEnabled = o.AiAccessEnabled;
			o.AiAccessEnabled = true;
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
				o.AiAccessEnabled = previouslyEnabled;
				SettingsManager.OptionsData.Save();
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("ui-interactive"), Timeout(180000)]
		[Description("A switch called without /Profile, as an assistant calls it, reaches a copy the person started with /Profile")]
		public void A_switch_without_a_profile_reaches_a_copy_started_with_one()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			if (Process.GetProcessesByName("x360ce").Length > 0)
				Assert.Inconclusive("x360ce is running; this test starts a copy of its own.");
			var profile = NewProfile(37362);
			var process = Process.Start(new ProcessStartInfo(exe, "\"/Profile=" + profile + "\"") { WorkingDirectory = profile, UseShellExecute = false });
			try
			{
				// The door opens once the window is built; until then the switch says the copy does not answer.
				var output = "";
				var code = -1;
				var until = DateTime.Now.AddSeconds(90);
				while (code != 0 && DateTime.Now < until)
				{
					code = RunSwitch(exe, "-Ai=ui_current", out output);
					if (code != 0)
						System.Threading.Thread.Sleep(1000);
				}
				Assert.AreEqual(0, code, "The switch did not reach the copy started with /Profile: " + output);
				StringAssert.Contains(output, "\"Windows\"");
			}
			finally
			{
				Ui.CloseApp(process);
				Directory.Delete(profile, true);
			}
		}

		[TestMethod, TestCategory("mcp"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("Describing the interface with /ExportUi opens no door and leaves the options file as it was, even where the options have AI assistant access on")]
		public void Export_opens_no_door_and_writes_no_settings()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			var profile = NewProfile(37363);
			var options = Path.Combine(profile, "Settings", "x360ce.Options.xml");
			var before = File.ReadAllBytes(options);
			var folder = Path.Combine(profile, "export");
			var process = Process.Start(new ProcessStartInfo(exe, "\"/Profile=" + profile + "\" \"/ExportUi=" + folder + "\"") { WorkingDirectory = profile, UseShellExecute = false });
			try
			{
				Assert.IsTrue(process.WaitForExit(180000), "The export did not finish.");
				Assert.IsTrue(File.Exists(Path.Combine(folder, "ui-tree-v4.md")), "The export wrote nothing, so this proves nothing.");
				var log = Path.Combine(profile, "x360ce.AiAccess.log");
				Assert.IsFalse(File.Exists(log) && File.ReadAllText(log).Contains("door opened"), "The export opened the door: " + (File.Exists(log) ? File.ReadAllText(log) : ""));
				CollectionAssert.AreEqual(before, File.ReadAllBytes(options), "The export wrote the options file.");
			}
			finally
			{
				if (!process.HasExited)
					process.Kill();
				process.WaitForExit();
				Directory.Delete(profile, true);
			}
		}

		/// <summary>A profile folder whose options have the door on at Read on the port given, trusting local connections.</summary>
		static string NewProfile(int port)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", "profile-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path.Combine(folder, "Settings"));
			var o = new Options { AiAccessEnabled = true, AiAccess = AiAccess.Read, AiAccessTrustLocal = true, AiAccessPort = port };
			o.EnsureAiAccessToken();
			var data = new XSettingsData<Options> { Items = new SortableBindingList<Options> { o } };
			File.WriteAllBytes(Path.Combine(folder, "Settings", "x360ce.Options.xml"), Serializer.SerializeToXmlBytes(data, Encoding.UTF8, true));
			return folder;
		}

		/// <summary>Runs the program with a switch that answers and stops, and returns its exit code, with what it printed.</summary>
		static int RunSwitch(string exe, string arguments, out string output)
		{
			var start = new ProcessStartInfo(exe, arguments) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
			using (var p = Process.Start(start))
			{
				var error = p.StandardError.ReadToEndAsync();
				output = p.StandardOutput.ReadToEnd() + error.Result;
				p.WaitForExit();
				return p.ExitCode;
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
