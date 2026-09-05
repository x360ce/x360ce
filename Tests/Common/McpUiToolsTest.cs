// @under-test: App.v4/Mcp/McpTools.cs
// @area: mcp   @layer: unit
using JocysCom.ClassLibrary.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Mcp;
using x360ce.App.UiTree;

namespace x360ce.Tests
{
	/// <summary>The three tools that make the whole interface reachable, and the lines a caller may not cross.</summary>
	[TestClass]
	public class McpUiToolsTest
	{
		static void WithWindow(AiAccess level, Action<Form> test)
		{
			Ui.OnUiThread(() =>
			{
				McpCatalog.Load(typeof(McpTools));
				McpCatalog.Level = () => level;
				McpCatalog.OnUiThread = a => a();
				using (var form = new Form { Name = "Main" })
				{
					McpTools.Root = form;
					try { test(form); }
					finally { McpTools.Root = null; }
				}
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Read, set and press work by path against a window, and a branch read carries full paths")]
		public void Read_set_and_press_by_path()
		{
			WithWindow(AiAccess.Configure, form =>
			{
				var group = new GroupBox { Name = "Box" };
				var slider = new TrackBar { Name = "Slider", Maximum = 100, Value = 10 };
				var pressed = 0;
				var button = new Button { Name = "Go" };
				button.Click += (s, e) => pressed++;
				group.Controls.AddRange(new Control[] { slider, button });
				form.Controls.Add(group);
				form.Show();
				var tree = Serializer.DeserializeFromJson<UiNode>(McpTools.UiRead());
				Assert.IsNull(tree.Path, "The window is not a segment of any path.");
				Assert.IsTrue(tree.Items[0].Items.Any(x => x.Path == "Box/Slider" && x.Value == "10"));
				var branch = Serializer.DeserializeFromJson<UiNode>(McpTools.UiRead("Box"));
				Assert.AreEqual("Box", branch.Path);
				Assert.IsTrue(branch.Items.Any(x => x.Path == "Box/Slider"), "A branch read must carry paths from the window.");
				Assert.IsFalse(McpTools.UiRead().Contains("\\/"), "Paths must come out with plain slashes, or they cannot be pasted back.");
				Assert.IsNull(McpTools.UiSet("Box/Slider", "55"));
				Assert.AreEqual(55, slider.Value);
				Assert.IsNull(McpTools.UiInvoke("Box/Go"));
				Assert.AreEqual(1, pressed);
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiSet("Nowhere", "1")).Message, "No element");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiSet("Box/Slider", "500")).Message, "0 to 100");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A control that administers is refused below Administer, and the door's own controls at every level")]
		public void Administering_and_door_controls_are_refused()
		{
			WithWindow(AiAccess.Configure, form =>
			{
				var install = new Button { Name = "ViGEmBusInstallButton" };
				var debug = new CheckBox { Name = "DebugModeCheckBox" };
				var level = new ComboBox { Name = "AiAccessComboBox" };
				level.Items.AddRange(Enum.GetNames(typeof(AiAccess)));
				var regenerate = new Button { Name = "AiAccessRegenerateButton" };
				form.Controls.AddRange(new Control[] { install, debug, level, regenerate });
				form.Show();
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiInvoke(install.Name)).Message, "Administer");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiSet(debug.Name, "true")).Message, "Administer");
				McpCatalog.Level = () => AiAccess.Administer;
				Assert.IsNull(McpTools.UiInvoke(install.Name));
				Assert.IsNull(McpTools.UiSet(debug.Name, "true"));
				// Even at the top level: the level is a person's choice, never the caller's.
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiSet(level.Name, "Off")).Message, "Options page");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpTools.UiInvoke(regenerate.Name)).Message, "Options page");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Every administering control named in the guard is a button or check box of the Options page, and the catalogue holds the tools")]
		public void Admin_controls_exist_and_tools_are_catalogued()
		{
			// The list is what keeps a caller from installing a driver. A name that no longer matches
			// a control guards nothing, and a toolbar item cannot be reached at all, so each name is
			// checked against the page's own designer fields, which a rename changes the same day.
			foreach (var name in McpTools.AdminControls)
				AssertField(name, typeof(Button), typeof(CheckBox));
			McpCatalog.Load(typeof(McpTools));
			var names = McpCatalog.Tools.Select(t => t.Name).ToArray();
			CollectionAssert.IsSubsetOf(new[] { "ui_read", "ui_set", "ui_invoke", "help", "ui_tree" }, names);
			Assert.IsTrue(new[] { "ui_read", "help", "ui_tree" }.All(n => McpCatalog.Tools.First(t => t.Name == n).Level == AiAccess.Read));
		}

		/// <summary>The Options page has a designer field of that name, of one of those kinds.</summary>
		public static void AssertField(string name, params Type[] kinds)
		{
			var field = typeof(x360ce.App.Controls.OptionsUserControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.IsNotNull(field, name + " is not a control of the Options page.");
			CollectionAssert.Contains(kinds, field.FieldType, name + " is not the kind of element the guard expects.");
		}
	}
}
