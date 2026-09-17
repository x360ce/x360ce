// @under-test: App.v4/Controls, App.v4/MainForm.cs, App.v4/Forms
// @area: devices   @layer: source
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// The main window is registered with Windows for device notifications, so Windows asks it
	/// whether a device may be removed and waits for the answer. A window whose thread is inside
	/// the device tree, or waiting for the Administrator copy that is removing a device, never
	/// answers, and the removal and the program then wait on each other until somebody kills the
	/// program. That is what pressing Remove on the Issues page did. So no window code may read the
	/// device tree or wait for the elevated copy: that work belongs to the device thread or a worker.
	/// </summary>
	[TestClass]
	public class InterfaceNeverReadsDevicesTest
	{
		/// <summary>Calls that read or change the device tree, or wait for the copy that does.</summary>
		static readonly string[] Forbidden =
		{
			"DeviceDetector.GetDevices(",
			"DeviceDetector.GetInterfaces(",
			"GetLeftoverVirtualPads(",
			"RemoveLeftoverVirtualPads(",
			"RemoveLeftoverPadsElevated(",
			"XInputPlaces.Read(",
			"XInputPlaces.ReadIfStale(",
			"XInputPlaces.Resolve(",
			"XInputReorderPlan.ReadEntries(",
			"Program.RunElevated(",
			"UacHelper.RunElevated(",
		};

		/// <summary>Files whose code runs on the window's own thread.</summary>
		static IEnumerable<string> InterfaceFiles()
		{
			var app = Path.Combine(Ui.RepoRoot.FullName, "App.v4");
			var files = Directory.GetFiles(Path.Combine(app, "Controls"), "*.cs", SearchOption.AllDirectories)
				.Concat(Directory.GetFiles(Path.Combine(app, "Forms"), "*.cs"))
				.Concat(Directory.GetFiles(app, "MainForm*.cs"));
			return files.Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
				// The debug page starts a thread of its own for its device scan.
				.Where(f => !f.EndsWith("DebugUserControl.cs", StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("No window code reads the device tree or waits for the Administrator copy")]
		public void Window_code_never_touches_the_device_tree()
		{
			var offences = new List<string>();
			foreach (var file in InterfaceFiles())
			{
				var lines = File.ReadAllLines(file);
				// Inside the body handed to Task.Run the call runs on a worker, which is allowed. The
				// body is followed by its braces, so a call is on the worker while the depth is deeper
				// than where Task.Run began.
				var depth = 0;
				var workerDepth = -1;
				for (var i = 0; i < lines.Length; i++)
				{
					var code = Regex.Replace(lines[i], @"//.*$", "");
					var onWorker = code.Contains("Task.Run(") || (workerDepth >= 0 && depth > workerDepth);
					if (code.Contains("Task.Run(") && workerDepth < 0)
						workerDepth = depth;
					foreach (var call in Forbidden)
						if (code.Contains(call) && !onWorker)
							offences.Add(string.Format("{0}:{1}: {2}", file.Substring(Ui.RepoRoot.FullName.Length + 1), i + 1, lines[i].Trim()));
					depth += code.Count(c => c == '{') - code.Count(c => c == '}');
					if (workerDepth >= 0 && depth <= workerDepth && !code.Contains("Task.Run("))
						workerDepth = -1;
				}
			}
			Assert.AreEqual(0, offences.Count, "Window code reads the device tree, which can freeze the program:"
				+ Environment.NewLine + string.Join(Environment.NewLine, offences));
		}
	}
}
