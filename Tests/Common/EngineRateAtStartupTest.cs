// @under-test: App.v4/Common/DInput/DInputHelper.cs
// @area: engine   @layer: ui-interactive
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// The device thread is asked for a thousand passes a second, and the status bar shows what it
	/// manages. Reading the device tree from that thread dropped it to a few passes a second for
	/// as long as anything kept invalidating the places, and a wheel fed at that rate swings from
	/// side to side. These start the real program and read the rate it reports: once after it has
	/// settled, and once while the emulation hotkey unplugs and replugs the virtual controllers,
	/// which is the moment that used to cost the rate for ten to twenty seconds.
	/// </summary>
	[TestClass]
	public class EngineRateAtStartupTest
	{
		/// <summary>Well below the thousand asked for, so only a collapse fails, not a busy machine.</summary>
		const int MinimumHz = 300;

		[TestMethod, TestCategory("engine"), TestCategory("ui-interactive")]
		[Description("The device thread reaches its rate within a few seconds of starting and keeps it")]
		public void Device_thread_runs_at_rate_after_start()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			var process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
			try
			{
				var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));
				// A status strip item has no automation id of its own; it is known by what it says.
				var label = Ui.WaitFor(() => window.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>()
					.FirstOrDefault(x => (x.Current.Name ?? "").StartsWith("HW Hz:")), TimeSpan.FromSeconds(30), "the rate label");
				var samples = Enumerable.Range(0, 8).Select(i =>
				{
					Thread.Sleep(3000);
					var text = label.Current.Name ?? "";
					var m = Regex.Match(text, @"(\d+)");
					var hz = m.Success ? int.Parse(m.Groups[1].Value) : -1;
					Console.WriteLine("{0,3}s  {1}", (i + 1) * 3, text);
					return hz;
				}).ToArray();
				// The first samples cover start-up, when devices are still being read; the rate must
				// be there by the second half and stay.
				var settled = samples.Skip(4).ToArray();
				Assert.IsTrue(settled.All(hz => hz >= MinimumHz),
					"The device thread ran at " + string.Join(", ", settled) + " Hz after start-up, below " + MinimumHz + ".");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

		[TestMethod, TestCategory("engine"), TestCategory("ui-interactive")]
		[Description("The device thread keeps its rate while the emulation hotkey unplugs and replugs the controllers")]
		public void Device_thread_keeps_its_rate_across_an_emulation_toggle()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built.");
			// The program rewrites this file whenever it saves, so the copy goes back unconditionally. Named
			// after this process, so two test hosts running at once do not take away each other's copy.
			var backup = Path.Combine(Path.GetTempPath(), "x360ce.Options.emulation-hotkey-test." + Process.GetCurrentProcess().Id + ".xml");
			File.Copy(OptionsFile, backup, true);
			try
			{
				EnableEmulationHotkey();
				var process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
				try
				{
					var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));
					var label = Ui.WaitFor(() => window.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>()
						.FirstOrDefault(x => (x.Current.Name ?? "").StartsWith("HW Hz:")), TimeSpan.FromSeconds(30), "the rate label");
					var samples = Enumerable.Range(0, 40).Select(i =>
					{
						Thread.Sleep(1000);
						var second = i + 1;
						var text = label.Current.Name ?? "";
						var m = Regex.Match(text, @"(\d+)");
						var hz = m.Success ? int.Parse(m.Groups[1].Value) : -1;
						Console.WriteLine("{0,3}s  {1}", second, text);
						// Off, then on again. Each press unplugs or plugs every controller the game enables,
						// and each of those raises a device change that asks for the device tree again.
						if (second == 15 || second == 25)
							PressEmulationHotkey(process);
						return hz;
					}).ToArray();
					// The first seconds cover start-up, when the devices are still being read for the
					// first time. Everything after that, toggles included, has to hold the rate.
					var settled = samples.Skip(5).ToArray();
					Assert.IsTrue(settled.All(hz => hz >= MinimumHz),
						"The device thread ran at " + string.Join(", ", settled) + " Hz across the emulation toggles, below " + MinimumHz + ".");
				}
				finally
				{
					Ui.CloseApp(process);
				}
			}
			finally
			{
				File.Copy(backup, OptionsFile, true);
				File.Delete(backup);
			}
		}

		/// <summary>Where the program keeps the options the hotkey is switched on in.</summary>
		static readonly string OptionsFile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			"X360CE", "Settings", "x360ce.Options.xml");

		/// <summary>
		/// Switches the emulation hotkey on for the run. The hotkey is registered with the system
		/// only when the option is set, so without this the key press reaches nothing.
		/// </summary>
		static void EnableEmulationHotkey()
		{
			var xml = File.ReadAllText(OptionsFile);
			xml = xml.Contains("<EmulationHotkeyEnabled>")
				? Regex.Replace(xml, "<EmulationHotkeyEnabled>[^<]*</EmulationHotkeyEnabled>",
					"<EmulationHotkeyEnabled>true</EmulationHotkeyEnabled>")
				// Indentation of the element it sits next to, so the file stays readable.
				: Regex.Replace(xml, @"(\s*)<GetXInputStates>",
					"$1<EmulationHotkeyEnabled>true</EmulationHotkeyEnabled>$1<GetXInputStates>");
			File.WriteAllText(OptionsFile, xml);
		}

		/// <summary>
		/// Presses Ctrl + Alt + X the way a person does. The program claims the combination with
		/// RegisterHotKey, which only answers a real key press, so a window message would be ignored.
		/// The window is brought forward first because the press goes to whatever holds the keyboard.
		/// </summary>
		static void PressEmulationHotkey(Process process)
		{
			process.Refresh();
			NativeMethods.SetForegroundWindow(process.MainWindowHandle);
			System.Windows.Forms.SendKeys.SendWait("^%x");
			Console.WriteLine("     emulation hotkey pressed");
		}

		private static class NativeMethods
		{
			[System.Runtime.InteropServices.DllImport("user32.dll")]
			public static extern bool SetForegroundWindow(IntPtr window);
		}
	}
}
