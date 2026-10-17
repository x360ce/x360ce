// @under-test: App.v4/Controls/OptionsUserControl.Designer.cs, App.v4/Controls/OptionsUpdateUserControl.Designer.cs, App.v4/Controls/OptionsPluginsUserControl.Designer.cs, Engine/JocysCom/Mcp/AiAccessUserControl.Designer.cs
// @area: options-layout   @layer: unit
using JocysCom.ClassLibrary.Controls;
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
		[Description("Nothing a user reads or clicks on the Virtual Device panel is painted over")]
		public void Virtual_device_controls_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				var boxes = MeasurePanel("VirtualDeviceGroupBox", factor);
				Assert.IsTrue(boxes.Count >= 7,
					"Expected the whole Virtual Device panel, measured " + boxes.Count + " controls.");
				AssertNoOverlap(boxes, factor);
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The hotkey box sits whole to the left of its field on the Hotkeys panel")]
		public void Hotkey_box_and_field_never_overlap()
		{
			foreach (var factor in WidthFactors)
			{
				WithOptionsPage(factor, page =>
				{
					var group = Descendants(page).FirstOrDefault(x => x.Name == "HotkeysGroupBox");
					Assert.IsNotNull(group, "HotkeysGroupBox was not found on the Options page.");
					Show(group);
					var caption = group.Controls.Find("EmulationHotkeyCheckBox", false).Single();
					var field = group.Controls.Find("EmulationHotkeyTextBox", false).Single();
					Assert.IsTrue(caption.Bounds.Right <= field.Bounds.Left,
						"The caption " + caption.Bounds + " runs into the field " + field.Bounds +
						" with the page " + factor + " times its designed width.");
					Assert.IsTrue(field.Bounds.Right <= group.ClientSize.Width,
						"The field " + field.Bounds + " runs past the panel, which is " +
						group.ClientSize.Width + " px wide.");
					var note = group.Controls.Find("EmulationOverlayCheckBox", false).Single();
					Assert.IsTrue(note.Bounds.Top >= field.Bounds.Bottom && note.Bounds.Top >= caption.Bounds.Bottom,
						"The note box " + note.Bounds + " is painted over the hotkey row.");
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
					}
				});
			}
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The update log starts just below the Check now row and ends at the bottom of the page, at every zoom")]
		public void Update_log_follows_the_rows_above_it_at_every_zoom()
		{
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsUpdateUserControl(), "LogTextBox", "CheckButton",
				"CheckForUpdatesCheckBox", "PrivacyLabel", "CheckButton", "CheckDigitalSignatureCheckBox", "CheckVersionCheckBox");
		}

		[TestMethod, TestCategory("options-layout"), TestCategory("smoke")]
		[Description("The plugin list starts just below the folder row and ends at the bottom of the page, and the note wraps inside the page, at every zoom")]
		public void Plugins_list_follows_the_rows_above_it_at_every_zoom()
		{
			FollowsTheRowsAboveItAtEveryZoom(() => new OptionsPluginsUserControl(), "PluginsDataGridView", "OpenFolderButton",
				"AllowThirdPartyPluginsCheckBox", "NoteLabel", "FolderLabel", "FolderTextBox", "OpenFolderButton");
		}

		/// <summary>
		/// A page of rows above one control that takes the rest: at each zoom Windows offers, the rows are whole and inside
		/// the page, nothing covers anything, and the last control starts just below the rows and ends at the bottom.
		/// </summary>
		/// <param name="make">Makes the page.</param>
		/// <param name="fillName">The control that takes the rest of the page.</param>
		/// <param name="rowName">A control of one row's height: the most space allowed between rows, and below the last.</param>
		/// <param name="aboveNames">The controls in the rows above it.</param>
		static void FollowsTheRowsAboveItAtEveryZoom(Func<UserControl> make, string fillName, string rowName, params string[] aboveNames)
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
						var below = content.ClientSize.Height - fill.Bottom;
						Assert.IsTrue(below >= 0 && below < room, string.Format(
							"At {0:P0} {1} ends {2} px from the bottom of the page.", zoom, fillName, below));
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
		/// Bounds of every caption and command inside one group box on the Options page.
		/// </summary>
		/// <remarks>
		/// Labels, links, buttons and drop-downs are what a user reads and clicks, and every one of
		/// them has to be whole. A read-only status TextBox is not included: the panel is drawn at a
		/// design-time width of 186 px, which is narrower than the buttons on its own right-hand
		/// edge, so <c>ViGEmBusTextBox</c> stretches 63 px underneath <c>ViGEmBusInstallButton</c>
		/// and the button paints over the empty tail of it. Nothing is hidden by that today, and
		/// putting it right means laying the panel out again at a width it is really used at, which
		/// is a bigger change than the overlap this test was written for.
		/// </remarks>
		static Dictionary<string, Rectangle> MeasurePanel(string groupName, double widthFactor)
		{
			var boxes = new Dictionary<string, Rectangle>();
			WithOptionsPage(widthFactor, page =>
			{
				var group = Descendants(page).FirstOrDefault(x => x.Name == groupName);
				Assert.IsNotNull(group, groupName + " was not found on the Options page.");
				Show(group);
				foreach (var child in group.Controls.Cast<Control>().Where(x => x.Visible && ReadOrClicked(x)))
					boxes[child.Name] = child.Bounds;
			});
			return boxes;
		}

		static bool ReadOrClicked(Control control)
		{
			return control is Label || control is LinkLabel || control is ButtonBase || control is ComboBox;
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
