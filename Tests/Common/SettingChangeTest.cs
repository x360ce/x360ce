// @under-test: App.v4/MainForm.cs, App.v4/Common/DInput/DInputHelper.cs
// @area: engine   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace x360ce.Tests
{
	/// <summary>A change of Windows settings leaves the engine running.</summary>
	/// <remarks>
	/// Any program can tell every window that a Windows setting changed: a theme or accent colour, the language, the
	/// environment, a policy refresh. The window used to stop the engine on each one and start it again a second
	/// later, so a game read no controller for that second and every device was taken again. Here the message is sent
	/// to the program's window alone, as Windows delivers it to each window, and nothing on the computer changes; the
	/// engine's own trace says whether it stopped.
	/// </remarks>
	[TestClass]
	public class SettingChangeTest
	{
		const int WM_SETTINGCHANGE = 0x001A;
		const int SMTO_ABORTIFHUNG = 0x0002;

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr SendMessageTimeout(IntPtr window, int message, IntPtr wParam, string lParam, int flags, int timeoutMs, out IntPtr result);

		/// <summary>The engine's lines in its trace, one a second: the milliseconds since it started, and its passes in that second.</summary>
		static long[][] EngineLines(string log)
		{
			if (!File.Exists(log))
				return new long[0][];
			string[] lines;
			using (var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			using (var reader = new StreamReader(stream))
				lines = reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
			return lines.Where(x => !x.StartsWith("startup,", StringComparison.Ordinal) && x.Contains("read="))
				.Select(x => x.Split(','))
				.Select(x => new[] { long.Parse(x[0]), long.Parse(x[1]) })
				.ToArray();
		}

		[TestMethod, TestCategory("engine"), TestCategory("ui-interactive"), Timeout(240000)]
		[Description("A change of Windows settings told to the window neither stops nor starts the engine again")]
		public void A_settings_change_leaves_the_engine_running()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			if (Process.GetProcessesByName("x360ce").Length > 0)
				Assert.Inconclusive("x360ce is running; this test starts a copy of its own.");
			// A copy in a folder of its own, with a profile of its own, so no settings of the person's are read or written.
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", "setting-change-" + Guid.NewGuid().ToString("N"));
			var profile = Path.Combine(folder, "profile");
			Directory.CreateDirectory(profile);
			var copy = Path.Combine(folder, "x360ce.exe");
			File.Copy(exe, copy);
			var log = Path.Combine(folder, "engine.log");
			var info = new ProcessStartInfo(copy, "\"/Profile=" + profile + "\"") { WorkingDirectory = folder, UseShellExecute = false };
			info.EnvironmentVariables["X360CE_ENGINE_LOG"] = log;
			var process = Process.Start(info);
			long[][] lines;
			int sentAfter;
			try
			{
				Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(60));
				Ui.WaitFor(() => EngineLines(log).Length >= 3 ? "running" : null, TimeSpan.FromSeconds(60), "the engine to run for three seconds");
				process.Refresh();
				sentAfter = EngineLines(log).Length;
				IntPtr result;
				// To this window only, the way a broadcast reaches each window, with the area a theme change names.
				Assert.AreNotEqual(IntPtr.Zero, SendMessageTimeout(process.MainWindowHandle, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet", SMTO_ABORTIFHUNG, 10000, out result),
					"The window did not take the message.");
				Ui.WaitFor(() => EngineLines(log).Length >= sentAfter + 4 ? "running" : null, TimeSpan.FromSeconds(30), "four more seconds of the engine");
			}
			finally
			{
				Ui.CloseApp(process);
			}
			lines = EngineLines(log);
			try { Directory.Delete(folder, true); }
			catch (IOException) { }
			Console.WriteLine(string.Join(Environment.NewLine, lines.Select(x => x[0] + " ms: " + x[1] + " passes")));
			// The engine's clock starts again with the engine, so a stop and a start shows as a clock that went back.
			for (var i = 1; i < lines.Length; i++)
				Assert.IsTrue(lines[i][0] > lines[i - 1][0], "The engine stopped and started again: its clock went from " + lines[i - 1][0] + " ms to " + lines[i][0] + " ms.");
			// A second without a line, or with no passes, is a second no game got a report.
			for (var i = sentAfter; i < lines.Length; i++)
			{
				Assert.IsTrue(lines[i][0] - lines[i - 1][0] < 1500, "The engine wrote nothing for " + (lines[i][0] - lines[i - 1][0]) + " ms after the settings change.");
				Assert.IsTrue(lines[i][1] > 0, "The engine ran no pass in the second ending at " + lines[i][0] + " ms.");
			}
		}
	}
}
