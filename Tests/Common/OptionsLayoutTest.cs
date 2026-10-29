// @under-test: App.v4/Controls/OptionsUserControl.Designer.cs, App.v4/Controls/OptionsUpdateUserControl.Designer.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs, App.v4/Controls/OptionsSettingsUserControl.Designer.cs, Engine/JocysCom/Mcp/AiAccessUserControl.Designer.cs
// @area: options-layout   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
				var boxes = MeasurePanel("VirtualDeviceGroupBox", factor);
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
		[Description("The hotkey and emulation buttons boxes sit whole to the left of their fields on the Hotkeys panel, one row under the other")]
		public void Hotkey_box_and_field_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var group = Descendants(page).FirstOrDefault(x => x.Name == "HotkeysGroupBox");
					Assert.IsNotNull(group, "HotkeysGroupBox was not found on the Options page.");
					Show(group);
					var rowBottom = 0;
					foreach (var row in new[] { "EmulationHotkey", "EmulationButtons" })
					{
						var caption = group.Controls.Find(row + "CheckBox", false).Single();
						var field = group.Controls.Find(row + "TextBox", false).Single();
						Assert.IsTrue(caption.Bounds.Right <= field.Bounds.Left,
							"The caption " + caption.Bounds + " runs into the field " + field.Bounds +
							" with the page " + factor + " times its designed width.");
						Assert.IsTrue(field.Bounds.Right <= group.ClientSize.Width,
							"The field " + field.Bounds + " runs past the panel, which is " +
							group.ClientSize.Width + " px wide.");
						Assert.IsTrue(caption.Bounds.Top >= rowBottom && field.Bounds.Top >= rowBottom,
							"The " + row + " row " + caption.Bounds + " is painted over the row above it.");
						rowBottom = Math.Max(caption.Bounds.Bottom, field.Bounds.Bottom);
					}
					var note = group.Controls.Find("EmulationOverlayCheckBox", false).Single();
					Assert.IsTrue(note.Bounds.Top >= rowBottom,
						"The note box " + note.Bounds + " is painted over the emulation buttons row.");
					Assert.IsTrue(note.Bounds.Bottom <= group.ClientSize.Height && note.Bounds.Right <= group.ClientSize.Width,
						"The note box " + note.Bounds + " runs past the panel, which is " + group.ClientSize + ".");
				});
			}
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
					foreach (var name in new[] { "AiSkillClaudeStatusText", "AiSkillAgentsStatusText" })
						ai.Controls.Find(name, true).Single().Text = "Version 4.25.300.0, from a newer program.";
					ai.Controls.Find("AiAccessTokenTextBox", true).Single().Text = McpListener.NewToken();
					ai.PerformLayout();
					foreach (var section in new[] { "AiHeaderPanel", "AiSkillPanel", "AiAccessPanel", "AiTunnelPanel" })
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
					foreach (var name in new[] { "AiExampleTextBox", "AiAccessTokenTextBox", "AiAccessSnippetTextBox" })
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
						var box = content.Controls.Find("CurrentPathTextBox", true).Single();
						var button = content.Controls.Find("OpenFolderButton", true).Single();
						Assert.AreSame(box.Parent, button.Parent);
						Assert.IsTrue(box.Right <= button.Left, string.Format(
							"At {0:P0} the folder's box {1} runs under Open Folder {2}.", zoom, box.Bounds, button.Bounds));
						foreach (var name in new[] { "LocationComboBox", "MoveModeComboBox" })
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
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsUpdateUserControl(), "LogTextBox", null, "CheckButton",
				"CheckForUpdatesCheckBox", "PrivacyLabel", "CheckButton", "CheckDigitalSignatureCheckBox", "CheckVersionCheckBox");
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The plugin list starts just below the folder row and ends just above the AutoHotkey group, which ends at the bottom of the page, and the note wraps inside the page, at every zoom")]
		public void Plugins_list_follows_the_rows_above_it_at_every_zoom()
		{
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsPluginsUserControl(), "PluginsDataGridView", "AutoHotkeyGroupBox", "OpenFolderButton",
				"AllowThirdPartyPluginsCheckBox", "NoteLabel", "FolderLabel", "FolderTextBox", "OpenFolderButton");
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
		/// topmost, leaving out the panels and tables that only place them.
		/// </summary>
		static Point Start(Control page)
		{
			var shown = Shown(page).Select(x => page.RectangleToClient(x.Parent.RectangleToScreen(x.Bounds))).ToList();
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
