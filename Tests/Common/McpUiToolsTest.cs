// @under-test: Engine/Mcp/McpUiTools.cs
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
using x360ce.Engine.Mcp;
using x360ce.Engine.UiTree;

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
				McpTools.Register();
				McpCatalog.Level = () => level;
				McpCatalog.OnUiThread = a => a();
				using (var form = new Form { Name = "Main" })
				{
					McpUiTools.Root = form;
					try { test(form); }
					finally { McpUiTools.Root = null; }
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
				var tree = Serializer.DeserializeFromJson<UiNode>(McpUiTools.UiRead());
				Assert.IsNull(tree.Path, "The window is not a segment of any path.");
				Assert.IsTrue(tree.Items[0].Items.Any(x => x.Path == "Box/Slider" && x.Value == "10"));
				var branch = Serializer.DeserializeFromJson<UiNode>(McpUiTools.UiRead("Box"));
				Assert.AreEqual("Box", branch.Path);
				Assert.IsTrue(branch.Items.Any(x => x.Path == "Box/Slider"), "A branch read must carry paths from the window.");
				Assert.IsFalse(McpUiTools.UiRead().Contains("\\/"), "Paths must come out with plain slashes, or they cannot be pasted back.");
				Assert.IsNull(McpUiTools.UiSet("Box/Slider", "55"));
				Assert.AreEqual(55, slider.Value);
				Assert.IsNull(McpUiTools.UiInvoke("Box/Go"));
				Assert.AreEqual(1, pressed);
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiSet("Nowhere", "1")).Message, "No element");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiSet("Box/Slider", "500")).Message, "0 to 100");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("What the person is looking at is reported by path: the pages shown, the element with focus and its row")]
		public void The_current_page_and_focus_are_reported_by_path()
		{
			WithWindow(AiAccess.Read, form =>
			{
				var tabs = new TabControl { Name = "Tabs" };
				var first = new TabPage { Name = "First", Text = "First" };
				var second = new TabPage { Name = "Second", Text = "Second" };
				var grid = new DataGridView { Name = "Grid", AllowUserToAddRows = false };
				grid.Columns.Add("A", "A");
				grid.Rows.Add("one");
				grid.Rows.Add("two");
				second.Controls.Add(grid);
				tabs.TabPages.AddRange(new[] { first, second });
				form.Controls.Add(tabs);
				form.Show();
				tabs.SelectedTab = second;
				form.ActiveControl = grid;
				grid.CurrentCell = grid.Rows[1].Cells[0];
				var current = (System.Collections.Generic.Dictionary<string, object>)McpUiTools.UiCurrent();
				Assert.AreEqual("", current["Window"], "The main window is named, though no path starts with it.");
				var windows = (object[])current["Windows"];
				var mainWindow = (System.Collections.Generic.Dictionary<string, object>)windows[0];
				Assert.AreEqual("", mainWindow["Path"], "The open windows do not start with the main one.");
				Assert.AreEqual(true, mainWindow["Active"], "The window in front is not said to be.");
				CollectionAssert.AreEqual(new[] { "Tabs/Second" }, (string[])current["Tabs"], "The page shown is not reported.");
				var focus = (System.Collections.Generic.Dictionary<string, object>)current["Focus"];
				Assert.AreEqual("Tabs/Second/Grid", focus["Path"], "The element with focus is not reported by its path.");
				Assert.AreEqual("Grid", focus["Role"]);
				var row = (System.Collections.Generic.Dictionary<string, object>)current["Row"];
				Assert.AreEqual("Tabs/Second/Grid/rows/1", row["Path"], "The selected row is not reported by its path.");
				Assert.IsNotNull(UiTreeWalker.Find(form, (string)row["Path"]), "The reported path does not lead back to the row.");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Pointing at an element brings its page to the front and frames it, with the words beside it")]
		public void Showing_points_at_the_element()
		{
			WithWindow(AiAccess.Read, form =>
			{
				var tabs = new TabControl { Name = "Tabs" };
				var one = new TabPage { Name = "One" };
				var two = new TabPage { Name = "Two" };
				var go = new Button { Name = "Go", Text = "Go" };
				two.Controls.Add(go);
				tabs.Controls.Add(one);
				tabs.Controls.Add(two);
				form.Controls.Add(tabs);
				form.Show();
				try
				{
					Assert.IsNull(McpUiTools.UiShow("Tabs/Two/Go", "Press this to start", 1));
					Assert.AreSame(two, tabs.SelectedTab, "The page holding the element must come to the front.");
					Assert.AreSame(go, UiCallout.Target, "The frame is not around the element.");
					StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiShow("Nowhere", null, 1)).Message, "No element");
					go.Visible = false;
					StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiShow("Tabs/Two/Go", null, 1)).Message, "hidden");
				}
				finally
				{
					UiCallout.Hide();
				}
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("A walk opens and frames each tab on the way by its header, outermost first, then points at the element")]
		public void Walking_opens_each_tab_on_the_way()
		{
			WithWindow(AiAccess.Read, form =>
			{
				var tabs = new TabControl { Name = "Tabs" };
				var other = new TabPage { Name = "Other", Text = "Options" };
				var pad = new TabPage { Name = "Pad", Text = "Controller 1" };
				var pages = new TabControl { Name = "Pages", Dock = DockStyle.Fill };
				var general = new TabPage { Name = "General", Text = "General" };
				var force = new TabPage { Name = "Force", Text = "Force Feedback" };
				var go = new Button { Name = "Go", Text = "Go" };
				force.Controls.Add(go);
				pages.TabPages.AddRange(new[] { general, force });
				pad.Controls.Add(pages);
				tabs.TabPages.AddRange(new[] { other, pad });
				form.Controls.Add(tabs);
				form.Show();
				CollectionAssert.AreEqual(new[] { pad, force }, McpUiTools.TabsOnTheWay(go), "The tabs on the way are not outermost first.");
				// Each step on the UI thread leaves a frame; recording them gives the walk as the person saw it.
				var seen = new System.Collections.Generic.List<Control>();
				var selected = new System.Collections.Generic.List<TabPage>();
				var around = new System.Collections.Generic.List<System.Drawing.Rectangle>();
				McpCatalog.OnUiThread = a =>
				{
					a();
					if (UiCallout.Target == null)
						return;
					seen.Add(UiCallout.Target);
					selected.Add(tabs.SelectedTab);
					around.Add(UiCallout.Around);
				};
				var stepMs = McpUiTools.WalkStepMs;
				McpUiTools.WalkStepMs = 1;
				try
				{
					Assert.IsNull(McpUiTools.UiShow("Tabs/Pad/Pages/Force/Go", "Strength is here", 1, walk: true));
					CollectionAssert.AreEqual(new Control[] { pad, force, go }, seen, "The walk did not frame the tabs in turn, then the element.");
					Assert.AreSame(pad, selected[0], "The outer tab was not open while it was pointed at.");
					Assert.AreEqual(tabs.RectangleToScreen(tabs.GetTabRect(1)), around[0], "A tab page is not framed by its tab.");
					Assert.AreEqual(pages.RectangleToScreen(pages.GetTabRect(1)), around[1], "An inner tab page is not framed by its tab.");
					Assert.AreSame(force, pages.SelectedTab, "The walk did not end on the element's page.");
					UiCallout.Hide();
					tabs.SelectedTab = other;
					pages.SelectedTab = general;
					seen.Clear();
					Assert.IsNull(McpUiTools.UiShow("Tabs/Pad/Pages/Force/Go", "Strength is here", 1));
					CollectionAssert.AreEqual(new Control[] { go }, seen, "Without walk only the element is framed.");
					UiCallout.Hide();
					tabs.SelectedTab = other;
					seen.Clear();
					StringAssert.Contains(McpUiTools.UiScript("walk Tabs/Pad/Pages/Force/Go | Strength is here | 1"), "1 step(s)");
					CollectionAssert.AreEqual(new Control[] { pad, force, go }, seen, "A script's walk step does not walk.");
				}
				finally
				{
					McpUiTools.WalkStepMs = stepMs;
					UiCallout.Hide();
				}
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Finding by words lists matching paths, and a script runs its steps in order and names the line that fails")]
		public void Finding_and_scripting_work_by_path()
		{
			WithWindow(AiAccess.Read, form =>
			{
				var tabs = new TabControl { Name = "Tabs" };
				var page = new TabPage { Name = "Page", Text = "Page" };
				var slider = new TrackBar { Name = "Strength", Maximum = 100, AccessibleName = "Overall strength", AccessibleDescription = "Scales all vibration." };
				var pressed = 0;
				var go = new Button { Name = "Go", Text = "Go" };
				go.Click += (s, e) => pressed++;
				page.Controls.AddRange(new Control[] { slider, go });
				tabs.Controls.Add(page);
				form.Controls.Add(tabs);
				form.Show();
				try
				{
					var hits = ((object[])McpUiTools.UiFind("vibration")).Cast<System.Collections.Generic.Dictionary<string, object>>().ToList();
					Assert.AreEqual(1, hits.Count, "One element speaks of vibration.");
					Assert.AreEqual("Tabs/Page/Strength", hits[0]["Path"]);
					// Each word is looked for on its own, anywhere in the element, so a page name narrows a search.
					Assert.AreEqual(1, ((object[])McpUiTools.UiFind("Page all VIBRATION")).Length, "Words in any order, case and field.");
					Assert.AreEqual(0, ((object[])McpUiTools.UiFind("Other vibration")).Length, "Every word must be there.");
					StringAssert.Contains(McpUiTools.UiScript("# a walkthrough\nshow Tabs/Page/Strength | This one | 1\nwait 1"), "2 step(s)");
					Assert.AreSame(slider, UiCallout.Target);
					// Doing needs Configure; pointing does not. The failing line is named.
					var refused = Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiScript("show Tabs/Page/Go | Then press | 1\nclick Tabs/Page/Go"));
					StringAssert.Contains(refused.Message, "Line 2");
					StringAssert.Contains(refused.Message, "Configure");
					McpCatalog.Level = () => AiAccess.Configure;
					StringAssert.Contains(McpUiTools.UiScript("set Tabs/Page/Strength | 40\nclick Tabs/Page/Go"), "2 step(s)");
					Assert.AreEqual(40, slider.Value);
					// A sentence may carry a '|' of its own; it is not a fourth part.
					StringAssert.Contains(McpUiTools.UiScript("show Tabs/Page/Go | Press A | B, then wait | 1"), "1 step(s)");
					Assert.AreEqual(1, pressed);
					StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiScript("jump Tabs")).Message, "Unknown step");
				}
				finally
				{
					UiCallout.Hide();
				}
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Finding puts an element named by the words before one that only mentions them, and says how many it left out")]
		public void Finding_ranks_names_first_and_says_what_it_left_out()
		{
			WithWindow(AiAccess.Read, form =>
			{
				var tabs = new TabControl { Name = "Tabs" };
				var page = new TabPage { Name = "Page", Text = "Page" };
				// First in the tree, and the word only in its purpose.
				var calm = new Button { Name = "Calm", Text = "Calm", TabIndex = 0, AccessibleDescription = "Stops the dead zone drifting." };
				var zone = new TrackBar { Name = "Zone", TabIndex = 1, AccessibleName = "Dead zone", AccessibleDescription = "How far before anything moves." };
				page.Controls.AddRange(new Control[] { calm, zone });
				for (var i = 0; i < McpUiTools.FindLimit + 5; i++)
					page.Controls.Add(new CheckBox { Name = "Option" + i, Text = "Option " + i, TabIndex = 2 + i, AccessibleDescription = "One of many." });
				tabs.Controls.Add(page);
				form.Controls.Add(tabs);
				form.Show();
				var hits = ((object[])McpUiTools.UiFind("dead zone")).Cast<System.Collections.Generic.Dictionary<string, object>>().ToList();
				Assert.AreEqual(2, hits.Count);
				Assert.AreEqual("Tabs/Page/Zone", hits[0]["Path"], "The element named by the words comes first.");
				var many = ((object[])McpUiTools.UiFind("many")).Cast<System.Collections.Generic.Dictionary<string, object>>().ToList();
				Assert.AreEqual(McpUiTools.FindLimit + 1, many.Count, "The first ones, and a note.");
				Assert.AreEqual("Note", many.Last()["Role"]);
				StringAssert.Contains((string)many.Last()["Name"], McpUiTools.FindLimit + " of " + (McpUiTools.FindLimit + 5));
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
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiInvoke(install.Name)).Message, "Administer");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiSet(debug.Name, "true")).Message, "Administer");
				McpCatalog.Level = () => AiAccess.Administer;
				Assert.IsNull(McpUiTools.UiInvoke(install.Name));
				Assert.IsNull(McpUiTools.UiSet(debug.Name, "true"));
				// Even at the top level: the level is a person's choice, never the caller's.
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiSet(level.Name, "Off")).Message, "Options tab");
				StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => McpUiTools.UiInvoke(regenerate.Name)).Message, "Options tab");
			});
		}

		[TestMethod, TestCategory("mcp"), TestCategory("critical")]
		[Description("Every administering control named in the guard is a button or check box of the Options page, and the catalogue holds the tools")]
		public void Admin_controls_exist_and_tools_are_catalogued()
		{
			// The list is what keeps a caller from installing a driver. A name that no longer matches
			// an element guards nothing, so each name is checked against the designer fields of the
			// page it belongs to, which a rename changes the same day.
			foreach (var name in McpUiTools.AdminControls.Where(x => x != "CleanupVirtualPadsButton"))
				AssertField(name, typeof(Button), typeof(CheckBox));
			// A bar entry is reached by path like any other element now, so the guard has to name it
			// and the name has to be a real one.
			var cleanup = typeof(x360ce.App.Controls.UserDevicesUserControl)
				.GetField("CleanupVirtualPadsButton", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.IsNotNull(cleanup, "CleanupVirtualPadsButton is not an element of the Devices page.");
			Assert.AreEqual(typeof(ToolStripButton), cleanup.FieldType,
				"CleanupVirtualPadsButton is not the kind of element the guard expects.");
			McpTools.Register();
			var names = McpCatalog.Tools.Select(t => t.Name).ToArray();
			CollectionAssert.IsSubsetOf(new[] { "ui_read", "ui_set", "ui_invoke", "ui_show", "ui_find", "ui_script", "help", "ui_tree" }, names);
			Assert.IsTrue(new[] { "ui_read", "ui_show", "ui_find", "ui_script", "help", "ui_tree" }.All(n => McpCatalog.Tools.First(t => t.Name == n).Level == AiAccess.Read));
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
