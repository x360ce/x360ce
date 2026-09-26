// @under-test: App.v3/Common/SettingManager.cs, App.v3/Controls/NewDeviceForm.cs
// @area: settings   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace x360ce.Tests
{
	/// <summary>
	/// Version 3's question about a new controller, answered either way, leaves x360ce.ini as the
	/// emulator library can use it.
	/// </summary>
	/// <remarks>
	/// Version 3 asks about each controller it has no settings for. Answered No, it saved no
	/// settings for the controller but still pointed the controller's place in x360ce.ini at
	/// them. The library then found a controller with no identity: it said so in a message box
	/// every time it loaded, and tried to open the controller again on every read, twenty times
	/// over, which held the window at one or two passes a second for about twelve seconds.
	///
	/// Answered with a preset, it lost mappings. A bare number means a control of the field's own
	/// kind, as the library reads it: an axis on a stick axis, a POV on the D-Pad, a button
	/// elsewhere. Version 3 read every bare number as a button, so a preset's stick axis 3 and
	/// button 3 looked the same, and showing the second emptied the first, the rule for a control
	/// picked by hand. The D-Pad went the same way beside the left stick.
	/// </remarks>
	[TestClass]
	public class V3NewDeviceTest
	{
		/// <summary>Title of the message box the emulator library shows about a controller it cannot open.</summary>
		const string LibraryMessageTitle = "x360ce";

		[TestMethod, TestCategory("settings"), TestCategory("ui-interactive")]
		[Description("Answering No to the new controller question points no controller at settings that do not exist")]
		public void Answering_No_to_a_new_controller_leaves_no_mapping_to_missing_settings()
		{
			var exe = Ui.FindApp("App.v3");
			if (exe == null)
				Assert.Inconclusive("App.v3 is not built. Build it before running UI tests.");
			var app = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
			var closed = new List<string>();
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				var rate = Ui.WaitFor(() => Ui.FindByName(window, RateText), TimeSpan.FromSeconds(60),
					"the status bar never reported an interface rate");
				// Every question is answered No until the program reads the controllers at its usual rate,
				// which it does only once it has asked everything and loaded the library.
				Ui.WaitFor(() =>
				{
					closed.AddRange(Ui.CloseDialogs(app, window));
					return Ui.ReadNumber(rate, RateText) >= 7 ? rate : null;
				}, TimeSpan.FromSeconds(60), "the program to read the controllers after its questions were answered");
			}
			finally
			{
				Ui.CloseApp(app);
			}
			Console.WriteLine("closed: " + string.Join(" | ", closed.ToArray()));
			if (!closed.Any(x => x.StartsWith("New Device Detected")))
				Assert.Inconclusive("No new controller question was asked: every connected controller has settings, "
					+ "or none is connected, so answering No was not tried.");

			Assert.IsFalse(closed.Contains(LibraryMessageTitle),
				"The emulator library showed a message box about a controller it could not open, after the "
				+ "question about a new controller was answered No.");
			var ini = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(exe), "x360ce.ini"));
			var sections = new HashSet<string>(ini.Where(x => x.StartsWith("[")).Select(x => x.Trim('[', ']')),
				StringComparer.OrdinalIgnoreCase);
			var mappings = ini.SkipWhile(x => x != "[Mappings]").Skip(1).TakeWhile(x => !x.StartsWith("["))
				.Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2 && x[1].Length > 0).ToArray();
			var missing = mappings.Where(x => !sections.Contains(x[1])).Select(x => x[0] + "=" + x[1]).ToArray();
			Assert.AreEqual(0, missing.Length, "x360ce.ini points controllers at settings it does not contain: "
				+ string.Join(", ", missing));
		}

		/// <summary>A preset as the library reads it: stick axes and buttons that share numbers, and a D-Pad.</summary>
		static readonly string[] PresetMappings =
		{
			"A=2", "B=3", "X=1", "Y=4", "D-pad POV=1",
			"Left Analog X=1", "Left Analog Y=-2", "Right Analog X=3", "Right Analog Y=-6",
		};

		[TestMethod, TestCategory("settings"), TestCategory("ui-interactive")]
		[Description("A preset chosen for a new controller is saved with every mapping it holds")]
		public void A_preset_chosen_for_a_new_controller_keeps_every_mapping()
		{
			var exe = Ui.FindApp("App.v3");
			if (exe == null)
				Assert.Inconclusive("App.v3 is not built. Build it before running UI tests.");
			var folder = Path.GetDirectoryName(exe);
			var ini = Path.Combine(folder, "x360ce.ini");
			var tmp = Path.Combine(folder, "x360ce.tmp");
			// Found by the question's own search, which looks through the program's folder.
			var preset = Path.Combine(folder, "V3NewDeviceTest.preset.ini");
			var original = File.Exists(ini) ? File.ReadAllBytes(ini) : null;
			var app = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = folder });
			string section = null;
			try
			{
				var window = Ui.WaitForMainWindow(app, TimeSpan.FromSeconds(60));
				AutomationElement question;
				try
				{
					question = Ui.WaitFor(() => window.FindFirst(TreeScope.Children, new PropertyCondition(
						AutomationElement.ControlTypeProperty, ControlType.Window)), TimeSpan.FromSeconds(60), "a new controller question");
				}
				catch (TimeoutException)
				{
					Assert.Inconclusive("No new controller question was asked: every connected controller has settings, or none is connected.");
					return;
				}
				// The question names the controller's instance, which is the section a preset for it is kept in.
				var instance = question.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>()
					.Select(x => Regex.Match(x.Current.Name ?? "", "[0-9a-f]{32}")).First(x => x.Success).Value;
				section = "IG_" + instance;
				File.WriteAllLines(preset, new[] { "[" + section + "]", "InstanceGUID=" + new Guid(instance).ToString("D") }
					.Concat(PresetMappings), System.Text.Encoding.Unicode);
				var online = question.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Search the Internet"));
				if (online != null && ((TogglePattern)online.GetCurrentPattern(TogglePattern.Pattern)).Current.ToggleState == ToggleState.On)
					((TogglePattern)online.GetCurrentPattern(TogglePattern.Pattern)).Toggle();
				Assert.IsTrue(Ui.Press(question, "Next >"), "The question has no Next button.");
				Ui.WaitFor(() => question.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty,
					"Searching locally for settings... Done")), TimeSpan.FromSeconds(30), "the local search for settings to finish");
				// The same button, which now finishes.
				Assert.IsTrue(Ui.Press(question, "Next >"), "The question has no Finish button.");
				var rate = Ui.WaitFor(() => Ui.FindByName(window, RateText), TimeSpan.FromSeconds(60),
					"the status bar never reported an interface rate");
				Ui.WaitFor(() =>
				{
					Ui.CloseDialogs(app, window);
					return Ui.ReadNumber(rate, RateText) >= 7 ? rate : null;
				}, TimeSpan.FromSeconds(60), "the program to read the controllers after the preset was loaded");

				var lines = File.ReadAllLines(ini);
				var saved = lines.SkipWhile(x => x != "[" + section + "]").Skip(1).TakeWhile(x => !x.StartsWith("[")).ToArray();
				var lost = PresetMappings.Where(x => !saved.Contains(x)).ToArray();
				Assert.AreEqual(0, lost.Length, "Mappings the preset holds were not saved as it holds them: "
					+ string.Join(", ", lost) + ". Saved: " + string.Join(", ", saved.Where(x =>
					PresetMappings.Any(m => m.Split('=')[0] + "=" == x.Split('=')[0] + "=")).ToArray()));
				// Closing asks about changes made since the program started; the test made them, and puts them back itself.
				File.Copy(ini, tmp, true);
			}
			finally
			{
				Ui.CloseApp(app);
				File.Delete(preset);
				File.Delete(tmp);
				if (original != null)
					File.WriteAllBytes(ini, original);
			}
		}

		static readonly Regex RateText = new Regex(@"^UI Hz:\s*(\d+)");
	}
}
