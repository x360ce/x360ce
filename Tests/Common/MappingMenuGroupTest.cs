// @under-test: App.v4/Controls/PadControl.cs
// @area: ui   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A device with many buttons, such as one that reports 128, shows them in the mapping menu grouped by tens, so the
	/// menu does not fill the screen; a device with few keeps one plain list.
	/// </summary>
	[TestClass]
	public class MappingMenuGroupTest
	{
		static ToolStripMenuItem Buttons(PadControl page)
		{
			return page.DiMenuStrip.Items.OfType<ToolStripMenuItem>().First(x => x.Text == "Buttons");
		}

		static List<ToolStripMenuItem> Children(ToolStripMenuItem item)
		{
			return item.DropDownItems.OfType<ToolStripMenuItem>().ToList();
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("128 buttons are grouped as Button x, Button 1x and so on, each button once, and picking one maps it")]
		public void Many_buttons_are_grouped_by_tens()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				using (var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList })
				{
					form.Controls.Add(box);
					form.Controls.Add(page);
					page.ResetDiMenuStrip(new UserDevice { InstanceGuid = Guid.NewGuid(), CapButtonCount = 128 });
					var buttons = Children(Buttons(page));
					var groups = buttons.Where(x => x.Text != "Inverted").ToList();
					var expected = new[] { "Button x" }.Concat(Enumerable.Range(1, 12).Select(x => "Button " + x + "x")).ToArray();
					CollectionAssert.AreEqual(expected, groups.Select(x => x.Text).ToArray(), "The groups are not named by tens.");
					CollectionAssert.AreEqual(Enumerable.Range(1, 9).Select(x => "Button " + x).ToArray(),
						Children(groups[0]).Select(x => x.Text).ToArray(), "Button x does not hold Buttons 1 to 9.");
					CollectionAssert.AreEqual(Enumerable.Range(120, 9).Select(x => "Button " + x).ToArray(),
						Children(groups[12]).Select(x => x.Text).ToArray(), "Button 12x does not hold Buttons 120 to 128.");
					var all = groups.SelectMany(Children).Select(x => x.Text).ToList();
					CollectionAssert.AreEqual(Enumerable.Range(1, 128).Select(x => "Button " + x).ToList(), all, "Not every button is in the menu once, in order.");
					Assert.IsTrue(groups.All(x => x.DropDownItems.Count <= PadControl.MaxMenuItems), "A group is longer than one list may be.");
					var inverted = Children(buttons.First(x => x.Text == "Inverted"));
					Assert.AreEqual("IButton x", inverted[0].Text, "The inverted buttons are not grouped as well.");
					// Picking a button in a group maps it; picking a group itself only opens it.
					var target = typeof(PadControl).GetField("MenuTargetCbx", BindingFlags.NonPublic | BindingFlags.Instance);
					SettingsManager.Current.SetComboBoxValue(box, "Button 1");
					target.SetValue(page, box);
					groups[5].PerformClick();
					Assert.AreEqual("Button 1", box.Text, "Clicking a group changed the box.");
					target.SetValue(page, box);
					Children(groups[5]).First(x => x.Text == "Button 57").PerformClick();
					Assert.AreEqual("Button 57", box.Text, "Picking a button in its group did not map it.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A device with few buttons lists them plainly, without groups")]
		public void Few_buttons_stay_in_one_list()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form())
				using (var page = new PadControl(MapTo.Controller1))
				{
					form.Controls.Add(page);
					page.ResetDiMenuStrip(new UserDevice { InstanceGuid = Guid.NewGuid(), CapButtonCount = 12 });
					var texts = Children(Buttons(page)).Select(x => x.Text).Where(x => x != "Inverted").ToArray();
					CollectionAssert.AreEqual(Enumerable.Range(1, 12).Select(x => "Button " + x).ToArray(), texts, "Twelve buttons are not one plain list.");
				}
			});
		}
	}
}
