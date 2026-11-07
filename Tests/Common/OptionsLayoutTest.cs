// @under-test: App.v4/Controls/OptionsUserControl.Designer.cs, App.v4/Controls/OptionsUpdateUserControl.Designer.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs, App.v4/Controls/OptionsSettingsUserControl.Designer.cs, Engine/JocysCom/Mcp/AiAccessUserControl.Designer.cs
// @area: options-layout   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Mcp;

namespace x360ce.Tests
{
	/// <summary>
	/// Whether the controls on the Options page can be read and clicked.
	/// </summary>
	/// <remarks>
	/// A designer file records the size a control had while the form was being drawn, but an
	/// AutoSize label sizes itself to its text at run time. When the two disagree the label grows
	/// past where it was placed, and because these labels are anchored to the right they carry that
	/// error to every window size. That is what hid the Virtual Device <c>Refresh</c> button behind
	/// the ViGEm link: the link needs 137 px and the designer had recorded 89, so it painted 42 px
	/// into the button and left it reading "sh". None of that is visible in a diff, only in a
	/// rendered window — so it is measured here instead of being looked at.
	/// </remarks>
	[TestClass]
	public class OptionsLayoutTest
	{
		public TestContext TestContext { get; set; }

		/// <summary>
		/// The page at the size it was designed at, and half as wide again to move the anchors.
		/// </summary>
		/// <remarks>
		/// Multiples of the page's own width rather than pixel counts: the page scales itself to the
		/// display, so a fixed number of pixels means a different amount of room on every machine.
		/// </remarks>
		static readonly double[] WidthFactors = { 1.0, 1.5 };

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Nothing a user reads, types in or clicks on the Virtual Device panel is painted over")]
		public void Virtual_device_controls_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				var boxes = MeasurePanel(nameof(OptionsUserControl.VirtualDeviceGroupBox), factor);
				Assert.IsTrue(boxes.Count >= 9,
					"Expected the whole Virtual Device panel, measured " + boxes.Count + " controls.");
				AssertWhole(boxes, factor);
				AssertNoOverlap(boxes, factor);
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Nothing a user reads, types in or clicks on the HID Guardian panel is painted over")]
		public void Hid_guardian_controls_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				var boxes = MeasurePanel("groupBox1", factor);
				Assert.IsTrue(boxes.Count >= 4,
					"Expected the whole HID Guardian panel, measured " + boxes.Count + " controls.");
				AssertWhole(boxes, factor);
				AssertNoOverlap(boxes, factor);
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Nothing a user reads, types in or clicks on the HID Hide panel is painted over, the release page link included")]
		public void Hid_hide_controls_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				var boxes = MeasurePanel(nameof(OptionsUserControl.HidHideGroupBox), factor);
				Assert.IsTrue(boxes.Count >= 7,
					"Expected the whole HID Hide panel, measured " + boxes.Count + " controls.");
				AssertWhole(boxes, factor);
				AssertNoOverlap(boxes, factor);
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The hotkey and emulation buttons boxes sit whole to the left of their fields on the Hotkeys panel, one row under the other")]
		public void Hotkey_box_and_field_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var group = Descendants(page).FirstOrDefault(x => x.Name == nameof(OptionsUserControl.HotkeysGroupBox));
					Assert.IsNotNull(group, "HotkeysGroupBox was not found on the Options page.");
					Show(group);
					// Measured on the group, wherever its table puts each control.
					Func<string, Rectangle> on = name =>
					{
						var control = group.Controls.Find(name, true).Single();
						return group.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
					};
					var rowBottom = 0;
					foreach (var row in new[] { "EmulationHotkey", "EmulationButtons" })
					{
						var caption = on(row + "CheckBox");
						var field = on(row + "TextBox");
						Assert.IsTrue(caption.Right <= field.Left,
							"The caption " + caption + " runs into the field " + field +
							" with the page " + factor + " times its designed width.");
						Assert.IsTrue(field.Right <= group.ClientSize.Width,
							"The field " + field + " runs past the panel, which is " +
							group.ClientSize.Width + " px wide.");
						Assert.IsTrue(caption.Top >= rowBottom && field.Top >= rowBottom,
							"The " + row + " row " + caption + " is painted over the row above it.");
						rowBottom = Math.Max(caption.Bottom, field.Bottom);
					}
					var note = on(nameof(OptionsUserControl.EmulationOverlayCheckBox));
					Assert.IsTrue(note.Top >= rowBottom,
						"The note box " + note + " is painted over the emulation buttons row.");
					Assert.IsTrue(note.Bottom <= group.ClientSize.Height && note.Right <= group.ClientSize.Width,
						"The note box " + note + " runs past the panel, which is " + group.ClientSize + ".");
				});
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The groups down each side of the General page are one width a side, each under the one above it, the left column ends with Hotkeys where the right one starts, and Developer Tools stands in the Developing group")]
		public void General_page_groups_stand_in_two_columns()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var columns = new[] { nameof(OptionsUserControl.GeneralLeftColumnPanel), nameof(OptionsUserControl.GeneralRightColumnPanel) }
						.Select(name => Descendants(page).FirstOrDefault(x => x.Name == name)).ToList();
					Assert.IsTrue(columns.All(x => x != null), "The two columns of the General page were not found on the Options page.");
					Show(columns[0]);
					// The left column holds Operation, Testing, Direct Input Devices, Guide Button and Hotkeys; the right one the
					// scan locations, Appearance, Developing and Configuration.
					var least = new[] { 5, 4 };
					for (var c = 0; c < columns.Count; c++)
					{
						var items = columns[c].Controls.Cast<Control>().Where(x => x is GroupBox || x is TabControl).OrderBy(x => x.Top).ToList();
						Assert.IsTrue(items.Count >= least[c], "Expected " + least[c] + " groups in " + columns[c].Name + ", found " + items.Count + ".");
						var widths = items.Select(x => x.Name + " " + x.Width).ToList();
						Assert.AreEqual(1, items.Select(x => x.Width).Distinct().Count(),
							"The groups of " + columns[c].Name + " are not one width: " + string.Join(", ", widths) + ".");
						for (var i = 1; i < items.Count; i++)
							Assert.IsTrue(items[i].Top >= items[i - 1].Bottom,
								items[i].Name + " " + items[i].Bounds + " is painted over " + items[i - 1].Name + " " + items[i - 1].Bounds + ".");
					}
					Assert.AreSame(columns[0].Parent, columns[1].Parent, "The two columns are not in one table.");
					Assert.IsTrue(columns[0].Right <= columns[1].Left,
						"The left column " + columns[0].Bounds + " runs under the right one " + columns[1].Bounds + ".");
					Assert.AreEqual(nameof(OptionsUserControl.HotkeysGroupBox), columns[0].Controls.Cast<Control>().OrderBy(x => x.Top).Last().Name,
						"The left column does not end with Hotkeys.");
					var tools = Descendants(page).Single(x => x.Name == nameof(OptionsUserControl.DeveloperToolsButton));
					Assert.IsTrue(Holders(tools, page).Any(x => x.Name == nameof(OptionsUserControl.DevelopingGroupBox)), "Developer Tools is not in the Developing group.");
				});
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The Appearance group stands in the right column of the General page, directly under the scan locations and above Developing, and holds Theme, Font and Size, each name to the left of its drop-down list, one under another")]
		public void Theme_and_font_stand_in_the_appearance_group_under_the_scan_locations()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var column = Descendants(page).OfType<TableLayoutPanel>().FirstOrDefault(x => x.Name == nameof(OptionsUserControl.GeneralRightColumnPanel));
					Assert.IsNotNull(column, "The right column of the General page was not found on the Options page.");
					var appearance = Descendants(page).FirstOrDefault(x => x.Name == nameof(OptionsUserControl.AppearanceGroupBox));
					Assert.IsNotNull(appearance, "AppearanceGroupBox was not found on the Options page.");
					Assert.AreSame(column, appearance.Parent, "The Appearance group is not in the right column of the General page.");
					var scan = column.Controls[nameof(OptionsUserControl.ProgramScanLocationsTabControl)];
					var developing = column.Controls[nameof(OptionsUserControl.DevelopingGroupBox)];
					Assert.AreEqual(column.GetRow(scan) + 1, column.GetRow(appearance), "The Appearance group is not directly under the scan locations.");
					Assert.AreEqual(column.GetRow(appearance) + 1, column.GetRow(developing), "Developing is not directly under the Appearance group.");
					Show(appearance);
					Assert.IsTrue(appearance.Top >= scan.Bottom, "The Appearance group " + appearance.Bounds + " is painted over the scan locations " + scan.Bounds + ".");
					Assert.IsTrue(developing.Top >= appearance.Bottom, "Developing " + developing.Bounds + " is painted over the Appearance group " + appearance.Bounds + ".");
					// Measured on the group, wherever its table puts each control.
					Func<string, Rectangle> on = name =>
					{
						var found = appearance.Controls.Find(name, true);
						Assert.AreEqual(1, found.Length, name + " is not in the Appearance group.");
						return appearance.RectangleToClient(found[0].Parent.RectangleToScreen(found[0].Bounds));
					};
					var rowBottom = 0;
					foreach (var row in new[] { "Theme", "Font", "Size" })
					{
						var caption = on(row + "Label");
						var list = on(row + "ComboBox");
						Assert.IsTrue(caption.Right <= list.Left, "The caption " + caption + " runs into the list " + list + ".");
						Assert.IsTrue(caption.Top >= rowBottom && list.Top >= rowBottom, "The " + row + " row " + list + " is painted over the row above it.");
						Assert.IsTrue(list.Right <= appearance.ClientSize.Width && list.Bottom <= appearance.ClientSize.Height,
							"The list " + list + " runs past the group, which is " + appearance.ClientSize + ".");
						rowBottom = Math.Max(caption.Bottom, list.Bottom);
					}
					foreach (var name in new[] { nameof(OptionsUserControl.FontComboBox), nameof(OptionsUserControl.SizeComboBox) })
					{
						var list = (ComboBox)appearance.Controls.Find(name, true).Single();
						Assert.AreEqual(ComboBoxStyle.DropDownList, list.DropDownStyle, name + " is typed in rather than chosen from the list.");
					}
				});
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("In the main window at its default size the General page shows all it holds without a scroll bar, at 100 % and 150 %, in both themes")]
		public void General_page_fits_the_main_window_at_its_default_size()
		{
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => MainWindowAtDefaultSize(null) }, (window, when) =>
			{
				var panel = (ScrollableControl)window.Controls.Find(nameof(OptionsUserControl.GeneralPanel), true).Single();
				var end = Shown(panel).Max(x => panel.RectangleToClient(x.Parent.RectangleToScreen(x.Bounds)).Bottom);
				var line = string.Format("{0}: the General page is {1} and what it holds ends {2} px down{3}.", when, panel.ClientSize, end,
					panel.VerticalScroll.Visible || panel.HorizontalScroll.Visible ? ", with a scroll bar" : "");
				TestContext.WriteLine(line);
				if (panel.VerticalScroll.Visible || panel.HorizontalScroll.Visible || end > panel.ClientSize.Height)
					faults.Add(line);
			});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("On the General page no group covers another, every group holds what it shows inside its frame, and every button is as wide as its text, in the program's font, in Windows' own and in a wider one, at 100 % and 150 %, in the main window at its default size")]
		public void General_page_groups_hold_their_controls_and_stand_apart_in_every_font()
		{
			var faults = new List<string>();
			foreach (var font in Fonts)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => MainWindowAtDefaultSize(font()) }, (window, when) =>
				{
					var page = window.Controls.Find(nameof(OptionsUserControl.GeneralTabPage), true).Single();
					when += " " + page.Font.Name;
					Func<Control, Control, Rectangle> on = (control, place) => place.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
					// What stands on the page in a place of its own: the groups, the scan locations and the button below the groups.
					var items = Shown(page).Where(x => (x is GroupBox || x is TabControl || x is ButtonBase)
						&& !Holders(x, page).Any(h => h is GroupBox || h is TabControl)).ToList();
					TestContext.WriteLine("{0}: {1}", when, string.Join(", ", items.Select(x => x.Name + " " + on(x, page))));
					for (var i = 0; i < items.Count; i++)
						for (var j = i + 1; j < items.Count; j++)
						{
							var shared = Rectangle.Intersect(on(items[i], page), on(items[j], page));
							if (!shared.IsEmpty)
								faults.Add(string.Format("{0}: {1} {2} and {3} {4} cover each other by {5}x{6} px.", when,
									items[i].Name, on(items[i], page), items[j].Name, on(items[j], page), shared.Width, shared.Height));
						}
					// Inside the room the group keeps for what it holds: below its caption and clear of its frame on every side.
					foreach (var group in items.OfType<GroupBox>())
						foreach (var child in Shown(group).Where(x => !group.DisplayRectangle.Contains(on(x, group))))
							faults.Add(string.Format("{0}: {1} {2} is not inside the frame of {3}, which keeps {4} for what it holds.", when,
								child.Name, on(child, group), group.Name, group.DisplayRectangle));
					foreach (var button in Shown(page).OfType<Button>())
					{
						var needs = button.GetPreferredSize(Size.Empty).Width;
						if (button.Width < needs)
							faults.Add(string.Format("{0}: {1} is {2} px wide and its text needs {3}.", when, button.Name, button.Width, needs));
					}
				});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Theme, Font and Size are one width, which the column does not stretch, and show a long family name whole, in the program's font, in Windows' own and in a wider one, at 100 % and 150 %")]
		public void Theme_and_font_lists_keep_one_width_that_shows_a_long_family_name()
		{
			var faults = new List<string>();
			foreach (var font in Fonts)
				ButtonIconTest.OnEverySurface(new Func<Control>[] { () => MainWindowAtDefaultSize(font()) }, (window, when) =>
				{
					var theme = (ComboBox)window.Controls.Find(nameof(OptionsUserControl.ThemeComboBox), true).Single();
					var list = (ComboBox)window.Controls.Find(nameof(OptionsUserControl.FontComboBox), true).Single();
					var size = (ComboBox)window.Controls.Find(nameof(OptionsUserControl.SizeComboBox), true).Single();
					when += " " + list.Font.Name;
					if (theme.Width != list.Width || size.Width != list.Width)
						faults.Add(string.Format("{0}: Theme is {1} px wide, Font {2} and Size {3}.", when, theme.Width, list.Width, size.Width));
					list.Items.Add(LongFamilyName);
					var needs = FormsTheme.GetComboBoxWidth(list);
					if (list.Width < needs)
						faults.Add(string.Format("{0}: Font is {1} px wide and \"{2}\" needs {3}.", when, list.Width, LongFamilyName, needs));
					// A window a quarter wider gives the column more room, which the lists leave to it.
					var before = list.Width;
					window.Width += window.Width / 4;
					Application.DoEvents();
					if (list.Width != before)
						faults.Add(string.Format("{0}: Font grows from {1} to {2} px with the column.", when, before, list.Width));
				});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>A usual family name, as long as the Font list should show whole in its field.</summary>
		const string LongFamilyName = "Segoe UI Variable Display";

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The Tab key moves through the General page as the eye reads it: down the left column, row by row and left to right in a row, then down the right column the same way")]
		public void General_page_tab_order_follows_the_eye()
		{
			var faults = new List<string>();
			ButtonIconTest.OnEverySurface(new Func<Control>[] { () => MainWindowAtDefaultSize(null) }, (window, when) =>
			{
				var page = window.Controls.Find(nameof(OptionsUserControl.GeneralTabPage), true).Single();
				var right = window.Controls.Find(nameof(OptionsUserControl.GeneralRightColumnPanel), true).Single();
				// The places the Tab key stops at, in the order it stops at them.
				var stops = new List<Control>();
				for (var c = page.GetNextControl(page, true); c != null; c = page.GetNextControl(c, true))
					if (c.TabStop && c.Visible && (c is ButtonBase || c is ComboBox || c is TextBoxBase || c is ListControl || c is TabControl))
						stops.Add(c);
				Func<Control, Rectangle> at = c => page.RectangleToClient(c.Parent.RectangleToScreen(c.Bounds));
				Func<Control, int> column = c => Holders(c, page).Contains(right) ? 1 : 0;
				TestContext.WriteLine("{0}: {1}", when, string.Join(", ", stops.Select(x => x.Name)));
				for (var i = 1; i < stops.Count; i++)
				{
					var a = stops[i - 1];
					var b = stops[i];
					var ra = at(a);
					var rb = at(b);
					// Two controls whose heights overlap stand in one row and are read left to right; otherwise the lower is read later.
					var oneRow = rb.Top < ra.Bottom && ra.Top < rb.Bottom;
					var back = column(b) < column(a)
						|| column(b) == column(a) && (oneRow ? rb.Left <= ra.Left : rb.Top < ra.Top);
					if (back)
						faults.Add(string.Format("{0}: Tab goes from {1} {2} back to {3} {4}.", when, a.Name, ra, b.Name, rb));
				}
			});
			Assert.AreEqual(0, faults.Count, Environment.NewLine + string.Join(Environment.NewLine, faults));
		}

		/// <summary>The fonts the General page is drawn in: the program's, Windows' own, which is narrower, and a wider one, at the size of the interface font.</summary>
		static readonly Func<Font>[] Fonts = { () => null, () => new Font(SystemFonts.DefaultFont.FontFamily, Control.DefaultFont.SizeInPoints), WiderFont };

		/// <summary>The Options page in a window held as the main window holds it at its default size.</summary>
		/// <param name="font">The font the page is drawn in, as a font chosen on the Appearance group draws it; null for the program's.</param>
		static Form MainWindowAtDefaultSize(Font font)
		{
			var options = new OptionsUserControl();
			if (font != null)
				options.Font = font;
			return MainWindowAtDefaultSize(options, "Options");
		}

		/// <summary>
		/// A page of the main window in a window held as the main window holds it at its default size: under the header and a
		/// games tool bar, above a status bar, on a page of a tab strip with pictures, inside the main window's padding.
		/// </summary>
		/// <remarks>
		/// The main window starts the engine and reads the settings as it is made, so the parts around its pages are made here
		/// as its designer makes them, and its size is read from its designer file.
		/// </remarks>
		/// <param name="content">The page's control, which fills its tab.</param>
		/// <param name="caption">The tab's caption.</param>
		internal static Form MainWindowAtDefaultSize(Control content, string caption)
		{
			content.Dock = DockStyle.Fill;
			// The controller pictures, which set the height of the tab strip, are Windows' small icons at the screen's scale, as
			// the main window makes them.
			var side = (int)Math.Round(16 * ControlsHelper.DpiScale);
			var pictures = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(side, side) };
			pictures.Images.Add(new Bitmap(side, side));
			// Padded as the main window's designer pads its pages: one that holds a list, every page but Options, has no padding
			// at its sides, so the list meets the page's edges.
			var padding = content is OptionsUserControl ? new Padding(3) : new Padding(0, 3, 0, 3);
			var page = new TabPage(caption) { Padding = padding, ImageIndex = 0 };
			// The control that fills its page with a list keeps the page's inset at its sides, which the theme gives to what
			// stands above and below the list.
			if (content.Controls.OfType<DataGridView>().Any(x => x.Dock == DockStyle.Fill))
				content.Padding = new Padding(3, 0, 3, 0);
			page.Controls.Add(content);
			var tabs = new TabControl { Dock = DockStyle.Fill, ImageList = pictures };
			tabs.TabPages.Add(page);
			var holder = new Panel { Dock = DockStyle.Fill, Padding = new Padding(3) };
			holder.Controls.Add(tabs);
			var tools = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4, 2, 4, 0) };
			tools.Items.Add(new ToolStripLabel("Game:"));
			tools.Items.Add(new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
			tools.Items.Add(new ToolStripButton("Save All", App.Properties.Resources.save_32x32)
			{
				Alignment = ToolStripItemAlignment.Right,
				ImageScaling = ToolStripItemImageScaling.None,
				Margin = new Padding(1),
				Padding = new Padding(0, 0, 0, 2),
			});
			var status = new StatusStrip { SizingGrip = false };
			status.Items.Add(new ToolStripStatusLabel("Ready"));
			status.Items.Add(new ToolStripButton("UI Hz: 0") { DisplayStyle = ToolStripItemDisplayStyle.Text });
			status.Items.Add(new ToolStripStatusLabel("Errors: 0 | 0", App.Properties.Resources.error_32x32)
			{
				BorderSides = ToolStripStatusLabelBorderSides.Left,
				ImageAlign = ContentAlignment.MiddleRight,
				ImageScaling = ToolStripItemImageScaling.None,
				Padding = new Padding(4, 0, 0, 0),
			});
			var window = new BaseFormWithHeader();
			window.SuspendLayout();
			window.AutoScaleDimensions = new SizeF(6F, 13F);
			window.AutoScaleMode = AutoScaleMode.Font;
			window.ClientSize = MainWindowSize();
			window.Controls.Add(holder);
			window.Controls.Add(tools);
			window.Controls.Add(status);
			// Docked as the main window docks them: the header, the status bar, the tool bar, then the tabs in what is left.
			window.Controls.SetChildIndex(status, 0);
			window.Controls.SetChildIndex(tools, 0);
			window.Controls.SetChildIndex(holder, 0);
			window.ResumeLayout(false);
			return window;
		}

		/// <summary>The main window's default size, as its designer file sets it.</summary>
		static Size MainWindowSize()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "MainForm.Designer.cs"));
			var match = Regex.Match(source, @"this\.ClientSize = new System\.Drawing\.Size\((\d+), (\d+)\);");
			Assert.IsTrue(match.Success, "The main window's size was not found in its designer file.");
			return new Size(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
		}

		/// <summary>
		/// A font wider than Windows', at the size of the interface font: Verdana, which Windows carries, or the interface font a
		/// quarter larger where it is missing.
		/// </summary>
		static Font WiderFont()
		{
			var size = Control.DefaultFont.SizeInPoints;
			using (var installed = new InstalledFontCollection())
				return installed.Families.Any(x => x.Name == "Verdana")
					? new Font("Verdana", size)
					: new Font(Control.DefaultFont.FontFamily, size * 1.25f);
		}

		/// <summary>The controls a control stands in, from its own parent up to, and not including, <paramref name="top"/>.</summary>
		static IEnumerable<Control> Holders(Control control, Control top)
		{
			for (var parent = control.Parent; parent != null && parent != top; parent = parent.Parent)
				yield return parent;
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Every caption, box and button of the AI page is whole and inside its section, with the longest texts the page writes, and every line of the example, the token and the snippet is inside its box")]
		public void Ai_page_rows_stay_inside_their_sections()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var ai = Descendants(page).OfType<AiAccessUserControl>().SingleOrDefault();
					Assert.IsNotNull(ai, "The AI page was not found on the Options page.");
					Show(ai);
					ai.CreateControl();
					// The program's settings keep a tunnel, so its tab is shown and measured too.
					ai.Bind(new AiAccessSettings());
					// The longest status the page writes: a copy from a newer program, with a version of full width.
					foreach (var name in new[] { nameof(AiAccessUserControl.AiSkillClaudeStatusText), nameof(AiAccessUserControl.AiSkillAgentsStatusText) })
						ai.Controls.Find(name, true).Single().Text = "Version 4.25.300.0, from a newer program.";
					ai.Controls.Find(nameof(AiAccessUserControl.AiAccessTokenTextBox), true).Single().Text = McpListener.NewToken();
					ai.PerformLayout();
					foreach (var section in new[] { nameof(AiAccessUserControl.AiHeaderPanel), nameof(AiAccessUserControl.AiSkillPanel), nameof(AiAccessUserControl.AiAccessPanel), nameof(AiAccessUserControl.AiTunnelPanel) })
					{
						var table = (TableLayoutPanel)ai.Controls.Find(section, true).Single();
						// A section is measured on its tab, in front, as a person sees it.
						var tab = table.Parent as TabPage;
						if (tab != null)
							((TabControl)tab.Parent).SelectedTab = tab;
						table.PerformLayout();
						var boxes = table.Controls.Cast<Control>().Where(x => x.Visible && (ReadOrClicked(x) || x is TextBoxBase))
							.ToDictionary(x => x.Name, x => x.Bounds);
						Assert.AreEqual(table.Controls.Count, boxes.Count, "Expected the whole of " + section + ".");
						AssertNoOverlap(boxes, factor);
						foreach (var box in boxes)
							Assert.IsTrue(box.Value.Right <= table.ClientSize.Width && box.Value.Bottom <= table.ClientSize.Height,
								box.Key + " " + box.Value + " runs past " + section + ", which is " + table.ClientSize +
								", with the page " + factor + " times its designed width.");
					}
					// A box that wraps grows to its lines; a line below its bottom edge is cut off.
					foreach (var name in new[] { nameof(AiAccessUserControl.AiExampleTextBox), nameof(AiAccessUserControl.AiAccessTokenTextBox), nameof(AiAccessUserControl.AiAccessSnippetTextBox) })
					{
						var box = (TextBox)ai.Controls.Find(name, true).Single();
						var last = box.GetPositionFromCharIndex(box.TextLength - 1);
						var line = TextRenderer.MeasureText("Ag", box.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
						Assert.IsTrue(last.Y + line <= box.ClientSize.Height, string.Format(
							"The last line of {0} ends at {1} px, below the box, which shows {2} px, with the page {3} times its designed width.",
							name, last.Y + line, box.ClientSize.Height, factor));
						Assert.IsTrue(box.ClientSize.Height < last.Y + 2 * line, string.Format(
							"{0} shows {1} px, an empty line below its last one, which ends at {2} px, with the page {3} times its designed width.",
							name, box.ClientSize.Height, last.Y + line, factor));
					}
				});
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("On the Settings page the folder's box ends before Open Folder starts, and each list is as wide as its widest choice needs, as the theme draws it")]
		public void Settings_rows_hold_their_boxes_whole()
		{
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				foreach (var zoom in new[] { 1f, 1.5f })
				{
					var content = new OptionsSettingsUserControl();
					using (var page = PageAt(content, zoom))
					{
						FormsTheme.Apply(page);
						page.CreateControl();
						page.PerformLayout();
						var box = content.Controls.Find(nameof(OptionsSettingsUserControl.CurrentPathTextBox), true).Single();
						var button = content.Controls.Find("OpenFolderButton", true).Single();
						Assert.AreSame(box.Parent, button.Parent);
						Assert.IsTrue(box.Right <= button.Left, string.Format(
							"At {0:P0} the folder's box {1} runs under Open Folder {2}.", zoom, box.Bounds, button.Bounds));
						foreach (var name in new[] { nameof(OptionsSettingsUserControl.LocationComboBox), nameof(OptionsSettingsUserControl.MoveModeComboBox) })
						{
							var list = (ComboBox)content.Controls.Find(name, true).Single();
							Assert.AreEqual(FormsTheme.GetComboBoxWidth(list), list.Width, string.Format(
								"At {0:P0} {1} is not as wide as its widest choice needs as it is drawn, so its text is cut.", zoom, name));
						}
					}
				}
			});
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The update log starts just below the Check now row and ends at the bottom of the page, at every zoom")]
		public void Update_log_follows_the_rows_above_it_at_every_zoom()
		{
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsUpdateUserControl(), nameof(OptionsUpdateUserControl.LogTextBox), null, nameof(OptionsUpdateUserControl.CheckButton),
				nameof(OptionsUpdateUserControl.CheckForUpdatesCheckBox), nameof(OptionsUpdateUserControl.PrivacyLabel), nameof(OptionsUpdateUserControl.CheckButton), nameof(OptionsUpdateUserControl.CheckDigitalSignatureCheckBox), nameof(OptionsUpdateUserControl.CheckVersionCheckBox));
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The plugin list starts just below the folder row and ends just above the AutoHotkey group, which ends at the bottom of the page, and the note wraps inside the page, at every zoom")]
		public void Plugins_list_follows_the_rows_above_it_at_every_zoom()
		{
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsPluginsUserControl(), nameof(OptionsPluginsUserControl.PluginsDataGridView), nameof(OptionsPluginsUserControl.AutoHotkeyGroupBox), "OpenFolderButton",
				nameof(OptionsPluginsUserControl.AllowThirdPartyPluginsCheckBox), nameof(OptionsPluginsUserControl.NoteLabel), nameof(OptionsPluginsUserControl.FolderLabel), nameof(OptionsPluginsUserControl.FolderTextBox), "OpenFolderButton");
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("Each Options page starts its content as far inside it as the General page does, a page's padding and a control's margin, and the pages' tab strip stands as far inside the Options tab, at every zoom for what the Options page lays out itself and at 100 % for the pages that hold a control of their own")]
		public void Options_pages_start_their_content_as_far_inside_as_General()
		{
			Ui.OnUiThread(() =>
			{
				FormsTheme.SetTheme(ThemeType.Light);
				// The zooms Windows offers; the page was drawn at 100 %.
				foreach (var zoom in new[] { 1f, 1.25f, 1.5f, 2f })
					using (var form = new Form { ClientSize = new Size(1600, 1000), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
					{
						var options = new OptionsUserControl { Dock = DockStyle.Fill };
						// The Options tab of the main window: a page with the padding every tab of the window has.
						var tab = new TabPage("Options") { Padding = new Padding(3) };
						var window = new UserControl { Dock = DockStyle.Fill };
						window.SuspendLayout();
						window.Font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * zoom);
						var tabs = new TabControl { Dock = DockStyle.Fill };
						tab.Controls.Add(options);
						tabs.TabPages.Add(tab);
						window.Controls.Add(tabs);
						window.AutoScaleDimensions = new SizeF(6F, 13F);
						window.AutoScaleMode = AutoScaleMode.Font;
						window.ResumeLayout(false);
						form.Controls.Add(window);
						FormsTheme.Apply(form);
						form.Show();
						var pages = options.MainTabControl;
						var strip = tab.PointToClient(pages.Parent.PointToScreen(pages.Location));
						Point? general = null;
						foreach (TabPage page in pages.TabPages)
						{
							pages.SelectedTab = page;
							Application.DoEvents();
							var start = Start(page);
							if (general == null)
							{
								general = start;
								Assert.IsTrue(AsFar(general.Value, strip), string.Format(
									"At {0:P0} the pages' tab strip stands at {1} inside the Options tab, and {2} starts at {3}.", zoom, strip, page.Text, start));
								continue;
							}
							// A page that holds a control of its own, which scales itself from the font it is made with: zoomed
							// by its font once made, it is scaled again, which no screen does, so it is measured at 100 % only,
							// as a screen at 100 % makes it.
							if (zoom != 1f && page.Controls.OfType<UserControl>().Any())
								continue;
							Assert.IsTrue(AsFar(general.Value, start), string.Format(
								"At {0:P0} {1} starts at {2}, and General at {3}.", zoom, page.Text, start, general.Value));
							// A table squeezes a control that cannot grow, even to nothing, as the page is scaled.
							foreach (var control in Shown(page).Where(x => x.Bounds.Width <= 0 || x.Bounds.Height <= 0))
								Assert.Fail(string.Format("At {0:P0} {1} on {2} is {3}, so it cannot be seen.", zoom, control.Name, page.Text, control.Bounds));
						}
					}
			});
		}

		/// <summary>
		/// Where a page's content starts, from the page's own edge: the left of its leftmost control and the top of its
		/// topmost, leaving out the panels and tables that only place them, and the lists, which stand edge to edge.
		/// </summary>
		static Point Start(Control page)
		{
			var shown = Shown(page).Where(x => !(x is DataGridView) && !(x.Parent is DataGridView))
				.Select(x => page.RectangleToClient(x.Parent.RectangleToScreen(x.Bounds))).ToList();
			return new Point(shown.Min(x => x.Left), shown.Min(x => x.Top));
		}

		/// <summary>The controls on a page a person sees, leaving out the panels and tables that only place them.</summary>
		static IEnumerable<Control> Shown(Control page)
		{
			return Descendants(page)
				.Where(x => x.Visible && !(x is TableLayoutPanel || x is FlowLayoutPanel || x is UserControl || x.GetType() == typeof(Panel)));
		}

		/// <summary>
		/// Whether two insets are the same within a pixel: a page drawn at another zoom rounds its spacing to whole pixels
		/// its own way.
		/// </summary>
		static bool AsFar(Point a, Point b)
		{
			return Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1;
		}

		/// <summary>
		/// A page of rows above one control that takes the rest, and maybe one more control below it: at each zoom Windows
		/// offers, the rows are whole and inside the page, nothing covers anything, the control that takes the rest starts
		/// just below the rows, and it, or the control below it, ends at the bottom.
		/// </summary>
		/// <param name="make">Makes the page.</param>
		/// <param name="fillName">The control that takes the rest of the page.</param>
		/// <param name="belowName">The control below it, at the bottom of the page, or null when it is the last.</param>
		/// <param name="rowName">A control of one row's height: the most space allowed between rows, and below the last.</param>
		/// <param name="aboveNames">The controls in the rows above it.</param>
		static void FollowsTheRowsAboveItAtEveryZoom(Func<UserControl> make, string fillName, string belowName, string rowName, params string[] aboveNames)
		{
			Ui.OnUiThread(() =>
			{
				// The zooms Windows offers; the page was drawn at 100 %.
				foreach (var zoom in new[] { 1f, 1.25f, 1.5f, 2f })
				{
					var content = make();
					using (var page = PageAt(content, zoom))
					{
						page.CreateControl();
						// What a window does when it is shown: the page fills its place, then lays out what it holds.
						page.PerformLayout();
						content.PerformLayout();
						// Measured on the page, wherever each control is held.
						Func<string, Rectangle> on = name =>
						{
							var control = content.Controls.Find(name, true).Single();
							return content.RectangleToClient(control.Parent.RectangleToScreen(control.Bounds));
						};
						var fill = on(fillName);
						var above = aboveNames.Max(x => on(x).Bottom);
						// The space between rows is the controls' own margins: less than a row of controls.
						var room = on(rowName).Height;
						Assert.IsTrue(fill.Top >= above && fill.Top - above < room, string.Format(
							"At {0:P0} {1} starts at {2}, and the rows above it end at {3}.", zoom, fillName, fill.Top, above));
						var lastName = fillName;
						var last = fill;
						if (belowName != null)
						{
							last = on(belowName);
							lastName = belowName;
							Assert.IsTrue(last.Top >= fill.Bottom && last.Top - fill.Bottom < room, string.Format(
								"At {0:P0} {1} ends at {2}, and {3} below it starts at {4}.", zoom, fillName, fill.Bottom, belowName, last.Top));
						}
						var below = content.ClientSize.Height - last.Bottom;
						Assert.IsTrue(below >= 0 && below < room, string.Format(
							"At {0:P0} {1} ends {2} px from the bottom of the page.", zoom, lastName, below));
						foreach (var name in aboveNames)
							Assert.IsTrue(on(name).Right <= content.ClientSize.Width, string.Format(
								"At {0:P0} {1} {2} runs past the page, which is {3} px wide.", zoom, name, on(name), content.ClientSize.Width));
						var covered = ControlOverlapTest.Covered(content).ToList();
						Assert.AreEqual(0, covered.Count, string.Format("At {0:P0}:{1}{2}",
							zoom, Environment.NewLine, string.Join(Environment.NewLine, covered)));
					}
				}
			});
		}

		/// <summary>
		/// An Options page held the way the Options page's designer code holds it, at a zoom.
		/// </summary>
		/// <remarks>
		/// The screen's zoom reaches the controls as a larger font, which is what auto-scaling reads.
		/// On a screen at that zoom the page scales its own controls as it is made, from the font, and
		/// the holder then scales the page's place, size and padding from 100 % units, as the designer
		/// recorded them. The update log was once held down by a padding sized to the rows above it: scaled by
		/// the page and again by the holder, it left a gap the height of the rows.
		/// </remarks>
		static UserControl PageAt(UserControl content, float zoom)
		{
			var font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * zoom);
			content.Font = font;
			var page = new UserControl();
			page.SuspendLayout();
			page.Font = font;
			page.Controls.Add(content);
			content.Location = new Point(3, 3);
			content.Size = new Size(644, 410);
			content.Dock = DockStyle.Fill;
			page.AutoScaleDimensions = new SizeF(6F, 13F);
			page.AutoScaleMode = AutoScaleMode.Font;
			page.Size = new Size(650, 416);
			page.ResumeLayout(false);
			return page;
		}

		/// <summary>
		/// Bounds, in the group's own place, of every caption, command and box inside one group box on the Options page,
		/// at whatever depth the tables put it.
		/// </summary>
		/// <remarks>Labels, links, buttons, drop-downs and text boxes are what a user reads, types in and clicks, and every one of them has to be whole.</remarks>
		static Dictionary<string, Rectangle> MeasurePanel(string groupName, double widthFactor)
		{
			var boxes = new Dictionary<string, Rectangle>();
			WithOptionsPage(widthFactor, page =>
			{
				var group = Descendants(page).FirstOrDefault(x => x.Name == groupName);
				Assert.IsNotNull(group, groupName + " was not found on the Options page.");
				Show(group);
				foreach (var child in Descendants(group).Where(x => x.Visible && (ReadOrClicked(x) || x is TextBoxBase)))
					boxes[child.Name] = group.RectangleToClient(child.Parent.RectangleToScreen(child.Bounds));
			});
			return boxes;
		}

		static bool ReadOrClicked(Control control)
		{
			return control is Label || control is LinkLabel || control is ButtonBase || control is ComboBox;
		}

		/// <summary>Every control has room: a table squeezes one that cannot grow, even to nothing, which no overlap shows.</summary>
		static void AssertWhole(Dictionary<string, Rectangle> boxes, double widthFactor)
		{
			foreach (var box in boxes)
				Assert.IsTrue(box.Value.Width > 0 && box.Value.Height > 0, box.Key + " " + box.Value
					+ " has no room with the page " + widthFactor + " times its designed width, so it cannot be seen.");
		}

		static void AssertNoOverlap(Dictionary<string, Rectangle> boxes, double widthFactor)
		{
			var names = boxes.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
			for (var i = 0; i < names.Length; i++)
				for (var j = i + 1; j < names.Length; j++)
				{
					var a = boxes[names[i]];
					var b = boxes[names[j]];
					var shared = Rectangle.Intersect(a, b);
					Assert.IsTrue(shared.IsEmpty,
						names[i] + " " + a + " and " + names[j] + " " + b + " overlap by " +
						shared.Width + "x" + shared.Height + " px with the page " + widthFactor +
						" times its designed width, so one of them is painted over the other.");
				}
		}

		/// <summary>Bring a control on to the screen by selecting every tab page above it.</summary>
		/// <remarks>
		/// A tab page that is not the selected one is hidden, and a hidden parent makes every
		/// control below it report itself hidden too — which is how this test first measured an
		/// empty panel. Selecting the pages is also what a user does to reach the panel.
		/// </remarks>
		static void Show(Control control)
		{
			for (var parent = control.Parent; parent != null; parent = parent.Parent)
			{
				var page = parent as TabPage;
				var tabs = page == null ? null : page.Parent as TabControl;
				if (tabs != null)
					tabs.SelectedTab = page;
			}
			control.PerformLayout();
		}

		/// <summary>Every control below this one, at any depth.</summary>
		static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var grandChild in Descendants(child))
					yield return grandChild;
			}
		}

		/// <summary>
		/// Build the Options page at a multiple of its designed width and hand it to the assertion.
		/// </summary>
		/// <remarks>
		/// The page is laid out but never shown: what is asserted is where the layout engine puts
		/// things, which it decides without a visible window, so this stays out of the interactive
		/// set. It is also why the page is sized directly rather than docked into a Form —
		/// <see cref="Control.Visible"/> reports the whole parent chain, and every control below a
		/// form that was never shown reads as hidden. With the page as the root, each control
		/// answers for itself. Windows Forms needs a single-threaded apartment, which MSTest does
		/// not provide.
		/// </remarks>
		static void WithOptionsPage(double widthFactor, Action<Control> assert)
		{
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					// A tab strip hands out its pages only once it has a window, and until then
					// every control below it reads as hidden.
					page.CreateControl();
					page.Width = (int)Math.Round(page.Width * widthFactor);
					page.PerformLayout();
					assert(page);
				}
			});
		}


	}
}
