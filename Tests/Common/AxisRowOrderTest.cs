// @under-test: Engine/Common/ConvertHelper.cs, App.v4/Controls/AxisMapUserControl.Designer.cs, App.v3/Controls/ThumbUserControl.Designer.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A mapped axis's Dead Zone, Anti-Dead Zone and Sensitivity, which stand from top to bottom in the order the program
	/// applies them to the device's value, so reading down the rows is reading what happens to the value.
	/// </summary>
	[TestClass]
	public class AxisRowOrderTest
	{
		/// <summary>The settings in the order <see cref="ConvertHelper.GetThumbValue"/> applies them, by their controls' names.</summary>
		static readonly string[] Applied = { "DeadZone", "AntiDeadZone", "Sensitivity" };

		/// <summary>A stick's value for a push of <paramref name="push"/> from the centre, in the stick's own units.</summary>
		static float Stick(float push, float deadZone, float antiDeadZone, float sensitivity)
		{
			// DirectInput's centre is 32768, and a stick's value is DirectInput's less that.
			return ConvertHelper.GetThumbValue(32768f + push, deadZone, antiDeadZone, sensitivity, false, false, true);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The program applies a row's dead zone first, then its anti-dead zone to what is left, then its sensitivity above the anti-dead zone")]
		public void Settings_are_applied_dead_zone_then_anti_dead_zone_then_sensitivity()
		{
			const float max = 32767f;
			const float dead = 8000f;
			const float anti = 4000f;
			// The dead zone comes first: a push the anti-dead zone would lift past the dead zone still gives nothing.
			Assert.AreEqual(0f, Stick(6000f, dead, anti, 0f), "The anti-dead zone was applied before the dead zone.");
			// Then the anti-dead zone, on what the dead zone lets through: the smallest push past it starts at the anti-dead zone.
			Assert.AreEqual(anti, Stick(dead + 1f, dead, anti, 0f), 2f, "The anti-dead zone does not lift what the dead zone lets through.");
			// Then sensitivity, which bends the travel between the anti-dead zone and the end and leaves both where they are.
			Assert.AreEqual(anti, Stick(dead + 1f, dead, anti, 50f), 2f, "Sensitivity moved the anti-dead zone.");
			Assert.AreEqual(max, Stick(max, dead, anti, 50f), 1f, "Sensitivity moved the end.");
			var middle = (dead + max) / 2f;
			Assert.IsTrue(Stick(middle, dead, anti, 50f) < Stick(middle, dead, anti, 0f) - 100f, "Sensitivity does not bend the travel.");
		}

		[TestMethod, TestCategory("ui")]
		[Description("On the Triggers, Left Stick and Right Stick tabs, Dead Zone, Anti-Dead Zone and Sensitivity stand from top to bottom in the order the program applies them, under DInput and XInput, and Tab and the AI door go through the panel in the order it is seen")]
		public void Rows_stand_in_the_order_the_settings_are_applied()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var axis = new AxisMapUserControl { TargetType = TargetType.LeftThumbX, Dock = DockStyle.Fill })
				{
					form.ClientSize = axis.Size;
					form.Controls.Add(axis);
					form.Show();
					Application.DoEvents();
					// From top to bottom: the values, then the settings in the order they are applied.
					var labels = new[] { "DInput", "XInput" }.Concat(Applied).Select(x => Find(axis, x + "Label")).ToList();
					for (var i = 1; i < labels.Count; i++)
						Assert.IsTrue(labels[i - 1].Bottom <= labels[i].Top, labels[i - 1].Name + " is not above " + labels[i].Name + ".");
					foreach (var part in new[] { "TrackBar", "TextBox" })
					{
						var row = Applied.Select(x => Find(axis, x + part)).ToList();
						for (var i = 1; i < row.Count; i++)
							Assert.IsTrue(row[i - 1].Bottom <= row[i].Top, row[i - 1].Name + " is not above " + row[i].Name + ".");
					}
					// Tab goes through the table row by row, and each row from left to right.
					var table = (TableLayoutPanel)Find(axis, "MainTableLayoutPanel");
					var controls = table.Controls.Cast<Control>().ToList();
					var seen = controls
						.OrderBy(x => table.GetPositionFromControl(x).Row)
						.ThenBy(x => table.GetPositionFromControl(x).Column)
						.Select(x => x.Name).ToList();
					var tabbed = controls.OrderBy(x => x.TabIndex).Select(x => x.Name).ToList();
					Assert.AreEqual(controls.Count, controls.Select(x => x.TabIndex).Distinct().Count(), "Two of the panel's controls share a place in the tab order.");
					CollectionAssert.AreEqual(seen, tabbed, "Tab does not go through the panel in the order it is seen: " + string.Join(", ", tabbed));
					// The AI door lists the panel in the same order.
					var listed = new List<string>();
					Ids(UiTreeWalker.Read(axis, false, ""), listed);
					foreach (var part in new[] { "TrackBar", "NumericUpDown" })
					{
						var places = Applied.Select(x => listed.IndexOf(x + part)).ToList();
						Assert.IsTrue(places.All(x => x >= 0), "The AI door does not list every " + part + ": " + string.Join(", ", listed));
						for (var i = 1; i < places.Count; i++)
							Assert.IsTrue(places[i - 1] < places[i], "The AI door does not list the " + part + "s in the order they are applied: " + string.Join(", ", listed));
					}
				}
			});
		}

		[TestMethod, TestCategory("ui")]
		[Description("x360ce 3's stick axis panel stands Dead Zone, Anti-Dead Zone and Sensitivity from top to bottom in the order the program applies them, and Tab and the AI door go through them in that order")]
		public void Version_3_rows_stand_in_the_order_the_settings_are_applied()
		{
			// x360ce 3 cannot be referenced beside this version, so its designer file is read.
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v3", "Controls", "ThumbUserControl.Designer.cs"));
			foreach (var part in new[] { "Label", "TrackBar", "TextBox", "NumericUpDown" })
			{
				var names = Applied.Select(x => x + part).ToList();
				var tops = names.Select(x => Number(source, x, @"Location = new System\.Drawing\.Point\(\d+, (\d+)\)")).ToList();
				var tabs = names.Select(x => Number(source, x, @"TabIndex = (\d+)")).ToList();
				for (var i = 1; i < names.Count; i++)
				{
					Assert.IsTrue(tops[i - 1] < tops[i], names[i - 1] + " is not above " + names[i] + ".");
					Assert.IsTrue(tabs[i - 1] < tabs[i], "Tab reaches " + names[i] + " before " + names[i - 1] + ".");
				}
			}
		}

		/// <summary>A number the designer file sets on a control, read with a pattern whose first group is the number.</summary>
		static int Number(string source, string control, string pattern)
		{
			var match = Regex.Match(source, @"this\." + control + @"\." + pattern);
			Assert.IsTrue(match.Success, "The designer file does not set " + control + " as " + pattern + ".");
			return int.Parse(match.Groups[1].Value);
		}

		/// <summary>The ids of a branch of the AI door's tree, in the order it lists them.</summary>
		static void Ids(UiNode node, List<string> ids)
		{
			if (!string.IsNullOrEmpty(node.Id))
				ids.Add(node.Id);
			if (node.Items == null)
				return;
			foreach (var item in node.Items)
				Ids(item, ids);
		}

		static Control Find(Control root, string name)
		{
			return root.Controls.Find(name, true).Single();
		}
	}
}
