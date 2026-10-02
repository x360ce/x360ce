// @under-test: App.v4/Controls/PadControl.cs, App.v3/Controls/PadControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A POV's own item in the mapping menu maps the whole POV only into the D-Pad box.</summary>
	/// <remarks>
	/// On the way to "POV 1 Up" a person clicks "POV 1", which opens its directions. The click wrote "POV 1" into the box
	/// the menu was for, and on a button, a stick or a trigger a whole POV maps to nothing (reported in #1334). The check
	/// for a POV's own item looked for a name the items stopped having in 2017.
	/// </remarks>
	[TestClass]
	public class MappingMenuPovTest
	{
		static ToolStripMenuItem Pov(PadControl page)
		{
			var povs = page.DiMenuStrip.Items.OfType<ToolStripMenuItem>().First(x => x.Text == "POVs");
			return povs.DropDownItems.OfType<ToolStripMenuItem>().First(x => x.Text == "POV 1");
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Clicking a POV's own item leaves a button box as it was, and the D-Pad box takes the whole POV")]
		public void A_pov_item_maps_the_whole_pov_only_into_the_d_pad_box()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				using (var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList })
				{
					form.Controls.Add(box);
					form.Controls.Add(page);
					page.ResetDiMenuStrip(new UserDevice { InstanceGuid = Guid.NewGuid(), CapButtonCount = 4, CapPovCount = 1 });
					SettingsManager.Current.SetComboBoxValue(box, "POV 1 Up");
					var target = typeof(PadControl).GetField("MenuTargetCbx", BindingFlags.NonPublic | BindingFlags.Instance);
					target.SetValue(page, box);
					Pov(page).PerformClick();
					Assert.AreEqual("POV 1 Up", box.Text,
						"Clicking the POV on the way to one of its directions mapped the whole POV into a box that cannot use it.");
					var dPad = (ComboBox)typeof(PadControl).GetField("DPadComboBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(page);
					target.SetValue(page, dPad);
					Pov(page).PerformClick();
					Assert.AreEqual("POV 1", dPad.Text, "The D-Pad box does not take the whole POV.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("In both programs the check for a POV's own item matches the name the menu gives it")]
		public void The_pov_check_matches_the_pov_item_name()
		{
			foreach (var app in new[] { "App.v4", "App.v3" })
			{
				var text = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, app, "Controls", "PadControl.cs"));
				var name = Regex.Match(text, "var dPadItem = CreateItem\\(\"([^\"]+)\"").Groups[1].Value;
				var click = Ui.Between(text, "void DiMenuStrip_Click(", "public void EnableDPadMenu(");
				var pattern = Regex.Match(click, "new Regex\\(\"([^\"]+)\"\\)").Groups[1].Value;
				Assert.IsTrue(name.Length > 0 && pattern.Length > 0, app + ": the POV item or its check was not found.");
				Assert.IsTrue(Regex.IsMatch(string.Format(name, 1), pattern),
					app + ": the click looks for '" + pattern + "', which no item named '" + name + "' matches, so the whole POV goes into any box.");
			}
		}
	}
}
