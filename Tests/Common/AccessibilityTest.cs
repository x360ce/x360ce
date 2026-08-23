// @under-test: App.v4/Common/ImageInfos.cs, App.v4/Controls/PadControl.Designer.cs, App.v4/MainForm.Designer.cs
// @area: accessibility   @layer: ui-wpf
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;
using x360ce.App;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether the mapping controls can be told apart by anything other than sight.
	/// </summary>
	/// <remarks>
	/// A combo box reports its selected text as its accessible name, so each mapping control used
	/// to announce what it currently held rather than what it sets. That left DPadLeftComboBox
	/// answering to "D-Pad Right", five separate controls answering to "Stick Left", and every
	/// name changing as soon as a mapping changed. Anything driving this screen without a mouse —
	/// a screen reader, an assistant asked to set up a controller — could pick the wrong one and
	/// silently remap the wrong button. These tests hold the names to the control, not the value.
	/// </remarks>
	[TestClass]
	public class AccessibilityTest
	{

		[TestMethod, TestCategory("accessibility"), TestCategory("smoke")]
		[Description("A map code reads as the words a person would use for it")]
		public void Map_codes_read_as_words()
		{
			Assert.AreEqual("D-Pad Up", ImageInfo.GetName(MapCode.DPadUp));
			Assert.AreEqual("D-Pad", ImageInfo.GetName(MapCode.DPad));
			Assert.AreEqual("Left Thumb Axis X", ImageInfo.GetName(MapCode.LeftThumbAxisX));
			Assert.AreEqual("Right Thumb Down", ImageInfo.GetName(MapCode.RightThumbDown));
			Assert.AreEqual("Button A", ImageInfo.GetName(MapCode.ButtonA));
			Assert.AreEqual("Left Trigger", ImageInfo.GetName(MapCode.LeftTrigger));
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("smoke")]
		[Description("No two map codes produce the same name")]
		public void Every_map_code_has_its_own_name()
		{
			var byName = new Dictionary<string, MapCode>();
			foreach (MapCode code in Enum.GetValues(typeof(MapCode)))
			{
				var name = ImageInfo.GetName(code);
				Assert.IsFalse(string.IsNullOrWhiteSpace(name), code + " has no name.");
				if (byName.ContainsKey(name))
					Assert.Fail(code + " and " + byName[name] + " are both called \"" + name +
						"\", so neither can be picked by name.");
				byName.Add(name, code);
			}
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("Every mapping control announces the pad control it maps, not its current value")]
		public void Mapping_controls_announce_what_they_map()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));

				var combos = Ui.WaitFor(() =>
				{
					var found = window
						.FindAll(TreeScope.Descendants, new PropertyCondition(
							AutomationElement.ControlTypeProperty, ControlType.ComboBox))
						.Cast<AutomationElement>()
						.Select(x => new { x.Current.AutomationId, x.Current.Name })
						.Where(x => x.AutomationId != null && x.AutomationId.EndsWith("ComboBox", StringComparison.Ordinal))
						.ToArray();
					return found.Length > 20 ? found : null;
				}, TimeSpan.FromSeconds(30), "the mapping controls");

				var problems = new List<string>();
				var seen = new Dictionary<string, string>();
				foreach (var combo in combos)
				{
					// The id carries the map code, so the expected name follows from the id alone.
					var codeText = combo.AutomationId.Substring(0, combo.AutomationId.Length - "ComboBox".Length);
					MapCode code;
					if (!Enum.TryParse(codeText, out code))
						continue;
					var expected = ImageInfo.GetName(code);
					if (combo.Name != expected)
						problems.Add(combo.AutomationId + " is called \"" + combo.Name +
							"\" but maps " + expected + ".");
					if (seen.ContainsKey(combo.Name))
						problems.Add(combo.AutomationId + " and " + seen[combo.Name] +
							" are both called \"" + combo.Name + "\".");
					else
						seen.Add(combo.Name, combo.AutomationId);
				}

				Console.WriteLine("Checked " + seen.Count + " mapping controls.");
				Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}


		[TestMethod, TestCategory("accessibility"), TestCategory("ui-interactive")]
		[Description("The device list and the tab strip say what they are")]
		public void Containers_and_columns_say_what_they_are()
		{
			var exe = Ui.FindApp("App.v4");
			if (exe == null)
				Assert.Inconclusive("App.v4 is not built. Build the solution before running UI tests.");

			Process process = null;
			try
			{
				process = Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = System.IO.Path.GetDirectoryName(exe) });
				var window = Ui.WaitForMainWindow(process, TimeSpan.FromSeconds(45));

				var tabs = Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(
					AutomationElement.AutomationIdProperty, "MainTabControl")),
					TimeSpan.FromSeconds(30), "the main tab strip");

				// A control with no name of its own borrows the label in front of it, and the label
				// in front of this one is the help paragraph in the window header.
				var tabName = tabs.Current.Name ?? "";
				Assert.IsFalse(tabName.Length > 40,
					"The tab strip is called \"" + tabName + "\", which is the help text rather than a name.");

				var grid = Ui.WaitFor(() => window.FindFirst(TreeScope.Descendants, new PropertyCondition(
					AutomationElement.AutomationIdProperty, "MappedDevicesDataGridView")),
					TimeSpan.FromSeconds(30), "the mapped devices list");

				var headers = grid
					.FindAll(TreeScope.Descendants, new PropertyCondition(
						AutomationElement.ControlTypeProperty, ControlType.Header))
					.Cast<AutomationElement>()
					.Select(x => x.Current.Name ?? "")
					.ToArray();
				Console.WriteLine("Headers: " + string.Join(" | ", headers));

				// The icon columns carry no text of their own, so a blank header leaves both the
				// column and every cell under it unreadable to anything not looking at the icons.
				Assert.AreEqual(0, headers.Count(x => x.Length == 0),
					"The device list has a column with no heading, so its cells cannot be told apart.");
				foreach (var expected in new[] { "Online", "Connection", "Enabled" })
					Assert.IsTrue(headers.Contains(expected),
						"The device list has no \"" + expected + "\" column heading.");
			}
			finally
			{
				Ui.CloseApp(process);
			}
		}

	}
}
