// @under-test: App.v4/Common/UiTree/UiTreeWalker.cs, App.v4/Common/UiTree/UiNode.cs
// @area: accessibility   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.UiTree;

namespace x360ce.Tests
{
	/// <summary>The tree as an assistant uses it: every element named by a path, carrying its value, and settable by that path.</summary>
	[TestClass]
	public class UiTreeStateTest
	{
		/// <summary>A bound item: the list shows Name, and ToString says something else, as the program's own lists do.</summary>
		sealed class Item { public string Name { get; set; } public override string ToString() { return "Item"; } }

		static Form Build()
		{
			var form = new Form { Name = "Main" };
			var tabs = new TabControl { Name = "Tabs" };
			var page = new TabPage { Name = "Page1", Text = "One" };
			var other = new TabPage { Name = "Page2", Text = "Two" };
			var slider = new TrackBar { Name = "Slider", Minimum = 0, Maximum = 100, Value = 40 };
			var box = new CheckBox { Name = "Box", Text = "Enable" };
			var list = new ComboBox { Name = "Kind", DisplayMember = "Name" };
			list.Items.Add(new Item { Name = "Constant" }); list.Items.Add(new Item { Name = "Periodic" }); list.SelectedIndex = 0;
			var secret = new TextBox { Name = "Secret", Text = "hunter2", UseSystemPasswordChar = true };
			var button = new Button { Name = "Go", Text = "Go" };
			page.Controls.AddRange(new Control[] { slider, box, list, secret });
			other.Controls.Add(button);
			tabs.Controls.Add(page);
			tabs.Controls.Add(other);
			form.Controls.Add(tabs);
			return form;
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("critical")]
		[Description("Every node carries its full path and its value when asked; the window has none; the export has neither")]
		public void Nodes_carry_path_and_value()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = Build())
				{
					var root = UiTreeWalker.Read(form, false, "");
					Assert.IsNull(root.Path, "The window itself is not a segment of any path.");
					var slider = Find(root, "Tabs/Page1/Slider");
					Assert.IsNotNull(slider, "The slider is not in the tree by path.");
					Assert.AreEqual("40", slider.Value);
					Assert.AreEqual("False", Find(root, "Tabs/Page1/Box").Value);
					Assert.AreEqual("Constant", Find(root, "Tabs/Page1/Kind").Value, "A bound list reads by the text shown, not the item's type.");
					Assert.IsNull(Find(root, "Tabs/Page1/Secret").Value, "A password box must not be read out.");
					var branch = UiTreeWalker.Read(UiTreeWalker.Find(form, "Tabs/Page1"), false, "Tabs/Page1");
					Assert.AreEqual("Tabs/Page1", branch.Path);
					Assert.IsNotNull(Find(branch, "Tabs/Page1/Slider"), "A branch read must still carry paths from the window, or ui_set cannot use them.");
					Assert.IsNull(UiTreeWalker.Read(form).Items[0].Path, "The export must not gain paths; docs/ui-tree.json would change.");
				}
			});
		}

		[TestMethod, TestCategory("accessibility"), TestCategory("critical")]
		[Description("A path resolves to its control, sets it, and presses a button on another page")]
		public void A_path_finds_sets_and_presses()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = Build())
				{
					form.Show();
					var slider = (TrackBar)UiTreeWalker.Find(form, "Tabs/Page1/Slider");
					Assert.IsNull(UiTreeWalker.SetValue(slider, "75"));
					Assert.AreEqual(75, slider.Value);
					Assert.IsNotNull(UiTreeWalker.SetValue(slider, "500"), "Out of range must be refused, not clamped silently.");
					var list = UiTreeWalker.Find(form, "Tabs/Page1/Kind");
					Assert.IsNull(UiTreeWalker.SetValue(list, "Periodic"), "A bound list is set by the text shown.");
					Assert.AreEqual("Periodic", UiTreeWalker.GetValue(list));
					Assert.IsNotNull(UiTreeWalker.SetValue(list, "Square"), "An item the list does not have must be refused.");
					var pressed = false;
					var button = (Button)UiTreeWalker.Find(form, "Tabs/Page2/Go");
					button.Click += (s, e) => pressed = true;
					// The button sits on the page that is not selected. A click on a hidden button does
					// nothing in Windows Forms, so the walker has to bring its page to the front first.
					Assert.IsNull(UiTreeWalker.Invoke(button));
					Assert.IsTrue(pressed, "The button on the other page was not pressed.");
					var tabs = (TabControl)UiTreeWalker.Find(form, "Tabs");
					Assert.AreEqual("Page2", UiTreeWalker.GetValue(tabs));
					Assert.IsNotNull(UiTreeWalker.Invoke(slider), "A slider is set, not pressed.");
					tabs.SelectedTab = (TabPage)UiTreeWalker.Find(form, "Tabs/Page1");
					button.Enabled = false;
					Assert.IsNotNull(UiTreeWalker.Invoke(button), "A disabled button cannot be pressed and must say so.");
					Assert.AreEqual("Page1", UiTreeWalker.GetValue(tabs), "A refused press must not switch the visible tab.");
					button.Enabled = true;
					form.Hide();
					Assert.IsNotNull(UiTreeWalker.Invoke(button), "A button in a hidden window cannot be pressed and must say so.");
					Assert.IsNull(UiTreeWalker.Find(form, "Tabs/Nowhere"));
					form.Controls.Add(new Panel { Name = "Twin" });
					form.Controls.Add(new Panel { Name = "Twin" });
					Assert.IsNull(UiTreeWalker.Find(form, "Twin"), "Two siblings with one name must resolve to neither.");
					var unnamed = new Panel { Name = "" };
					unnamed.Controls.Add(new CheckBox { Name = "Inside", Text = "Inside" });
					form.Controls.Add(unnamed);
					var read = UiTreeWalker.Read(form, false, "");
					Assert.IsTrue(read.Items.Any(x => x.Id == "Inside" && x.Path == null), "A control inside one without a name has no path.");
					Assert.IsFalse(read.Items.Any(x => x.Path == ""), "An empty path would match nothing and mislead.");
				}
			});
		}

		static UiNode Find(UiNode node, string path)
		{
			if (node.Path == path) return node;
			if (node.Items == null) return null;
			foreach (var child in node.Items)
			{
				var hit = Find(child, path);
				if (hit != null) return hit;
			}
			return null;
		}
	}
}
