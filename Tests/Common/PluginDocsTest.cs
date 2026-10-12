// @under-test: App.v4/Plugins/PluginDocs.cs, App.v4/Plugins/PluginKit.cs, App.v4/Mcp/McpTools.cs, Plugins/Build-Plugins.ps1, Plugins/Directory.Build.props
// @area: plugins   @layer: unit
using JocysCom.ClassLibrary.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using x360ce.App;
using x360ce.App.Mcp;
using x360ce.App.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// What AI agents and plugin authors are given: one document for each plugin, the plugin kit, and plugins_list. And
	/// the promise all of it rests on: a plugin is one .cs file, built into one DLL, that x360ce loads.
	/// </summary>
	[TestClass]
	public class PluginDocsTest
	{
		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("A plugin's document names it, says where it came from, and describes each device with its controls and its help")]
		public void A_plugin_document_describes_its_devices()
		{
			var file = TestControllerDevice.Type.File;
			var text = PluginDocs.Markdown(file);
			StringAssert.StartsWith(text, "# TestController\n");
			StringAssert.Contains(text, "- Source: embedded in x360ce.exe");
			StringAssert.Contains(text, "- State: loaded");
			StringAssert.Contains(text, "## Test Controller");
			StringAssert.Contains(text, "- Product GUID: " + TestControllerDevice.ProductGuid);
			StringAssert.Contains(text, "axes 5, sliders 0, hat switches 1, buttons 10");
			StringAssert.Contains(text, TestControllerDevice.Type.Description);
			StringAssert.Contains(text, "\n### Its tab\n");
			StringAssert.Contains(text, "- Move the axes (Movement, check box, starts at True): Moves the axes by themselves.");
			StringAssert.Contains(text, "- Shape (Movement, one of: Swing and ramp, Circle, starts at Swing and ramp): ");
			StringAssert.Contains(text, "- Moving time (s) (Movement, number, 0.5 to 20, starts at 4): ");
			StringAssert.Contains(text, "- Reads per second (Status, read only): ");
			StringAssert.Contains(text, "- The plugin's own control, TestControllerPad, under the settings.");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The skill lists the plugin guide and one document for each plugin, and reads each")]
		public void The_skill_lists_each_plugin()
		{
			var before = AiSkill.References;
			try
			{
				PluginDocs.UpdateReferences(PluginDocs.EmbeddedManager());
				Assert.AreEqual(AppHelper.HelpPluginsResource, AiSkill.References["references/plugins.md"]);
				var key = PluginDocs.Folder + "TestController.md";
				Assert.IsTrue(AiSkill.References.ContainsKey(key), "The Test Controller has no document in the skill.");
				StringAssert.StartsWith(AiSkill.ReadResource(AiSkill.References[key]), "# TestController");
				StringAssert.Contains(AiSkill.ReadResource(AiSkill.References["references/plugins.md"]), "IPluginDevice");
			}
			finally
			{
				AiSkill.References = before;
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("plugins_list names each plugin, its state, its devices and its help")]
		public void Plugins_list_describes_each_plugin()
		{
			var manager = TestControllerDevice.Manager;
			var list = ((object[])McpTools.PluginsList()).Cast<Dictionary<string, object>>().ToArray();
			var test = list.Single(x => (string)x["Name"] == "TestController");
			Assert.AreEqual("Loaded", test["State"]);
			Assert.AreEqual("Embedded", test["Source"]);
			var device = ((object[])test["Devices"]).Cast<Dictionary<string, object>>().Single();
			Assert.AreEqual("Test Controller", device["Name"]);
			Assert.AreEqual(5, device["Axes"]);
			StringAssert.StartsWith((string)test["Help"], "# TestController");
		}

		[TestMethod, TestCategory("plugins"), TestCategory("critical")]
		[Description("The plugin kit is written, its script builds the sample from one .cs file into one DLL in a folder of its own, and the DLL loads as a plugin")]
		public void The_plugin_kit_builds_a_plugin_that_loads()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.PluginKitTest." + Guid.NewGuid().ToString("N"));
			try
			{
				var output = new StringWriter();
				var error = new StringWriter();
				Assert.AreEqual(0, PluginKit.Write(folder, output, error), error.ToString());
				foreach (var name in new[] { "x360ce.Plugins.dll", "Directory.Build.props", "Build-Plugins.ps1", @"TestController\TestController.cs", "README.md" })
					Assert.IsTrue(File.Exists(Path.Combine(folder, name)), name + " was not written.");

				if (Run("dotnet", "--version", folder) == null)
					Assert.Inconclusive("The .NET SDK (dotnet) is not installed here.");
				// Built straight into a plugins folder, the layout the script writes is the one x360ce loads.
				var plugins = Path.Combine(folder, "Plugins");
				var built = Run("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File Build-Plugins.ps1 -Output \"" + plugins + "\"", folder);
				Assert.AreEqual(0, built.Item1, "Build-Plugins.ps1 failed: " + built.Item2);
				var files = Directory.GetFiles(plugins, "*", SearchOption.AllDirectories).Select(x => x.Substring(plugins.Length + 1)).ToArray();
				CollectionAssert.AreEqual(new[] { @"TestController\TestController.dll" }, files,
					"A plugin builds into one file in a folder of its own; the contract, symbols and the rest stay out.");
				using (var manager = new PluginManager(plugins, null, () => true, null))
				{
					manager.Scan();
					var file = manager.Files.Single();
					Assert.AreEqual(PluginFileState.Loaded, file.State, file.Reason);
					var type = file.DeviceTypes.Single();
					Assert.AreEqual("Test Controller", type.Name);
					Assert.IsFalse(type.Faulted, type.FaultReason);
				}
			}
			finally
			{
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("plugins")]
		[Description("In the x360ce source, the script builds the plugins beside it with no contract written there: it builds the contract from its project")]
		public void The_script_builds_the_plugins_in_the_source()
		{
			var source = Path.Combine(Ui.RepoRoot.FullName, "Plugins");
			var plugins = Path.Combine(Path.GetTempPath(), "x360ce.PluginSourceTest." + Guid.NewGuid().ToString("N"));
			try
			{
				if (Run("dotnet", "--version", source) == null)
					Assert.Inconclusive("The .NET SDK (dotnet) is not installed here.");
				// Run as a person runs it, from its own folder, naming only where the plugins go.
				var built = Run("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File Build-Plugins.ps1 -Output \"" + plugins + "\"", source);
				Assert.AreEqual(0, built.Item1, "Build-Plugins.ps1 failed: " + built.Item2);
				var files = Directory.GetFiles(plugins, "*", SearchOption.AllDirectories).Select(x => x.Substring(plugins.Length + 1)).ToArray();
				CollectionAssert.AreEqual(new[] { @"TestController\TestController.dll" }, files,
					"A plugin builds into one file in a folder of its own; the contract stays out.");
			}
			finally
			{
				if (Directory.Exists(plugins))
					Directory.Delete(plugins, true);
			}
		}

		/// <summary>Runs a program to its end: its exit code and all it wrote, or null when it is not installed.</summary>
		static Tuple<int, string> Run(string program, string arguments, string folder)
		{
			var start = new ProcessStartInfo(program, arguments)
			{
				WorkingDirectory = folder,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			Process process;
			try
			{
				process = Process.Start(start);
			}
			catch (System.ComponentModel.Win32Exception)
			{
				return null;
			}
			using (process)
			{
				var error = process.StandardError.ReadToEndAsync();
				var text = process.StandardOutput.ReadToEnd() + error.Result;
				process.WaitForExit();
				return Tuple.Create(process.ExitCode, text);
			}
		}
	}
}
