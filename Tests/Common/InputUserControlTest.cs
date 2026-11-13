// @under-test: App.v4/Controls/PadTabPages/General/InputUserControl.cs, App.v4/Controls/PadTabPages/General/InputChipGroup.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using JocysCom.ClassLibrary.Controls.UiTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The input panel on the General tab shows what the selected device has and what it is
	/// doing, and lets a chip be put into a mapping box by a click. The panel is driven here the
	/// way the pad drives it, with a device built in memory, so the shape and the lighting can
	/// be checked without hardware.
	/// </summary>
	[TestClass]
	public class InputUserControlTest
	{
		static UserDevice Device(int buttons, int axisMask, int sliderMask, int povs)
		{
			var ud = new UserDevice
			{
				InstanceGuid = Guid.NewGuid(),
				CapButtonCount = buttons,
				DiAxeMask = axisMask,
				DiSliderMask = sliderMask,
				CapPovCount = povs,
			};
			ud.SourceState = new SourceState(new JoystickState());
			for (var i = 0; i < ud.SourceState.Axis.Length; i++)
				ud.SourceState.Axis[i] = InputChips.AxisCentre;
			for (var i = 0; i < ud.SourceState.Povs.Length; i++)
				ud.SourceState.Povs[i] = InputChips.PovRest;
			return ud;
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The panel shows the chips of the device it is given and swaps them when the device changes")]
		public void Chips_follow_the_selected_device()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.CreateControl();
					panel.UpdateFrom(null);
					Assert.AreEqual(0, panel.ButtonChips.Chips.Count, "No device, no chips.");
					Assert.AreEqual(InputUserControl.SourceNone, panel.SourceLabel.Text);

					var wheel = Device(10, 0x1 | 0x2 | 0x4 | 0x8, 0, 1);
					wheel.ProductName = "Test Wheel";
					panel.UpdateFrom(wheel);
					Assert.AreEqual(10, panel.ButtonChips.Chips.Count);
					Assert.AreEqual(4, panel.AxisChips.Chips.Count);
					Assert.AreEqual(0, panel.SliderChips.Chips.Count);
					Assert.AreEqual(5, panel.PovChips.Chips.Count, "One POV and its four directions.");
					Assert.AreEqual("Source: Test Wheel", panel.SourceLabel.Text);

					panel.UpdateFrom(Device(4, 0x1, 0x3, 0));
					Assert.AreEqual(4, panel.ButtonChips.Chips.Count);
					Assert.AreEqual(2, panel.SliderChips.Chips.Count);
					Assert.AreEqual(0, panel.PovChips.Chips.Count);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A pressed button and a moved axis light their chips, and the axis chip shows its reading")]
		public void Chips_light_from_the_device_state()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.CreateControl();
					var ud = Device(4, 0x1 | 0x2, 0, 0);
					panel.UpdateFrom(ud);
					Assert.IsFalse(panel.ButtonChips.Chips.Any(c => c.Lit), "Nothing pressed, nothing lit.");

					ud.SourceState.Buttons[2] = true;
					ud.SourceState.Axis[1] = 65535;
					panel.UpdateFrom(ud);
					CollectionAssert.AreEqual(new[] { false, false, true, false }, panel.ButtonChips.Chips.Select(c => c.Lit).ToArray());
					var y = panel.AxisChips.Chips.Single(c => c.Index == 1);
					Assert.IsTrue(y.Lit);
					Assert.AreEqual(65535, y.Value);
					Assert.IsFalse(panel.AxisChips.Chips.Single(c => c.Index == 0).Lit, "A centred axis stays dark.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("Clicking a chip raises the mapping text that belongs in a box")]
		public void Clicking_a_chip_announces_its_mapping()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.CreateControl();
					panel.UpdateFrom(Device(3, 0, 0, 0));
					string payload = null;
					panel.ChipClicked += (s, e) => payload = e.Data.Payload;
					var group = panel.ButtonChips;
					var second = group.Chips[1];
					var bounds = group.BoundsOf(second);
					var at = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
					Assert.AreSame(second, group.HitTest(at), "The chip must sit where the group says it is.");
					Ui.Click(group, at);
					Assert.AreEqual("Button 2", payload);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The pointer becomes a hand over a chip and is plain between chips, which is how a chip says it can be picked up")]
		public void Pointer_is_a_hand_over_a_chip()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.CreateControl();
					panel.UpdateFrom(Device(1, 0, 0, 0));
					var group = panel.ButtonChips;
					var bounds = group.BoundsOf(group.Chips[0]);
					Ui.Hover(group, new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
					Assert.AreEqual(Cursors.Hand, group.Cursor);
					Ui.Hover(group, new Point(group.Width - 1, bounds.Y + bounds.Height / 2));
					Assert.AreEqual(Cursors.Default, group.Cursor, "Past the last chip there is nothing to pick up.");
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A device with more chips than the column is tall scrolls, so its last chip can be reached")]
		public void Input_panel_scrolls_to_the_last_chip_of_a_keyboard_sized_device()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.Size = new Size(384, 300);
					panel.CreateControl();
					panel.UpdateFrom(Device(128, 0, 0, 0));
					panel.PerformLayout();
					var table = (ScrollableControl)panel.Controls.Find("LayoutTable", false).Single();
					var group = panel.ButtonChips;
					Assert.IsTrue(group.Height > table.ClientSize.Height, "The test device must be taller than the column.");
					table.AutoScrollPosition = new Point(0, int.MaxValue / 2);
					var bottom = table.PointToClient(group.PointToScreen(new Point(0, group.Height))).Y;
					Assert.IsTrue(bottom <= table.ClientSize.Height, string.Format(
						"Scrolled as far as it goes, the last chip row ends at {0} but the column shows only {1}.",
						bottom, table.ClientSize.Height));
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A list taller than the column scrolls up and down only, and every section fits the width the scroll bar leaves")]
		public void Input_panel_scrolls_up_and_down_without_scrolling_sideways()
		{
			Ui.OnUiThread(() =>
			{
				foreach (var width in new[] { 200, 260, 384 })
				{
					using (var panel = new InputUserControl())
					{
						panel.Size = new Size(width, 300);
						panel.CreateControl();
						panel.UpdateFrom(Device(128, 0xFFFFFF, 0xFF, 4));
						panel.PerformLayout();
						var table = (ScrollableControl)panel.Controls.Find("LayoutTable", false).Single();
						Assert.IsTrue(table.VerticalScroll.Visible, width + " px wide: a device taller than the column must scroll up and down.");
						Assert.IsFalse(table.HorizontalScroll.Visible, string.Format(
							"{0} px wide: the column scrolls sideways; its sections are {1} px wide in {2}.",
							width, panel.ButtonsGroupBox.Width, table.ClientSize.Width));
						foreach (var section in new Control[] { panel.ButtonsGroupBox, panel.AxesGroupBox, panel.SlidersGroupBox, panel.PovsGroupBox })
							Assert.IsTrue(section.Right <= table.ClientSize.Width, string.Format(
								"{0} px wide: {1} ends at {2} but the column shows {3}.",
								width, section.Name, section.Right, table.ClientSize.Width));
					}
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A device with few chips leaves the sections packed at the top of a tall column")]
		public void Input_panel_keeps_sections_at_the_top_of_a_tall_column()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.Size = new Size(384, 671);
					panel.CreateControl();
					panel.UpdateFrom(Device(4, 0x1, 0, 0));
					panel.PerformLayout();
					var last = Sections(panel).Last(ControlsHelper.IsVisible);
					Assert.IsTrue(last.Bottom < panel.Height / 2, string.Format(
						"The last section ends at {0} of {1}; the sections must not stretch to fill the column.",
						last.Bottom, panel.Height));
				}
			});
		}

		/// <summary>The panel's four groups, top to bottom.</summary>
		static Control[] Sections(InputUserControl panel)
		{
			return new Control[] { panel.ButtonsGroupBox, panel.AxesGroupBox, panel.SlidersGroupBox, panel.PovsGroupBox };
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("A group the device has nothing in is hidden, and the AI door says so, and the groups below it close up; with no device, before any device and after one, the four groups are hidden and the source line stands alone")]
		public void Groups_the_device_lacks_are_hidden_and_the_rest_close_up()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ClientSize = new Size(384, 700), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
				using (var panel = new InputUserControl { Dock = DockStyle.Fill })
				{
					form.Controls.Add(panel);
					form.Show();
					var devices = new[]
					{
						new { Name = "no device", Device = (UserDevice)null, Shown = new[] { false, false, false, false } },
						new { Name = "buttons and axes", Device = Device(12, 0x3, 0, 0), Shown = new[] { true, true, false, false } },
						new { Name = "pedals", Device = Device(0, 0x7, 0, 0), Shown = new[] { false, true, false, false } },
						new { Name = "a stick without axes", Device = Device(6, 0, 0x1, 1), Shown = new[] { true, false, true, true } },
						new { Name = "every kind", Device = Device(12, 0x3, 0x1, 1), Shown = new[] { true, true, true, true } },
						new { Name = "no device again", Device = (UserDevice)null, Shown = new[] { false, false, false, false } },
						new { Name = "a device after none", Device = Device(12, 0x3, 0, 0), Shown = new[] { true, true, false, false } },
					};
					foreach (var device in devices)
					{
						panel.UpdateFrom(device.Device);
						Application.DoEvents();
						var sections = Sections(panel);
						var tree = UiTreeWalker.Read(panel, false, "");
						for (var i = 0; i < sections.Length; i++)
						{
							var at = device.Name + ", " + sections[i].Name;
							Assert.AreEqual(device.Shown[i], ControlsHelper.IsVisible(sections[i]), at + (device.Shown[i] ? " is hidden." : " is shown."));
							// The group and the chips in it, each as the AI door reports it.
							foreach (var control in new[] { sections[i], sections[i].Controls[0] })
							{
								var node = Find(tree, control.Name);
								Assert.IsNotNull(node, at + ": the AI door does not list " + control.Name + ".");
								Assert.AreEqual(!device.Shown[i], node.Hidden, at + ": the AI door says " + control.Name + " is " + (node.Hidden ? "hidden." : "shown."));
							}
						}
						// The shown groups follow one another as when none is hidden: the source line, then each group under the one before.
						var above = (Control)panel.SourceLabel;
						foreach (var section in sections.Where(ControlsHelper.IsVisible))
						{
							Assert.AreEqual(above.Bottom + above.Margin.Bottom + section.Margin.Top, section.Top, string.Format(
								"{0}: {1} starts at {2}, not under {3}, which ends at {4}.", device.Name, section.Name, section.Top, above.Name, above.Bottom));
							above = section;
						}
					}
				}
			});
		}

		static UiNode Find(UiNode node, string id)
		{
			if (node.Id == id)
				return node;
			if (node.Items != null)
				foreach (var item in node.Items)
				{
					var found = Find(item, id);
					if (found != null)
						return found;
				}
			return null;
		}

		[TestMethod, TestCategory("ui")]
		[Description("A full row of chips ends at its strip's padding, and the chips keep the same room from the frame at the left and the right, and the button chips below them too, at any width, in both themes")]
		public void Chips_keep_the_same_room_from_the_frame_on_every_side()
		{
			Ui.OnUiThread(() =>
			{
				try
				{
					// The frame's lines: the dark one at the box's edges, and the light one beside it, inside at the left and
					// outside at the right and the bottom. Both are ink: a gap is the rows or columns with no ink at all.
					var line = Math.Max(1, (int)Math.Floor(ControlsHelper.DpiScale));
					foreach (var theme in new[] { ThemeType.Light, ThemeType.Dark })
						foreach (var width in new[] { 260, 384, 437 })
							using (var form = new Form { ClientSize = new Size(width, 900), StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000), ShowInTaskbar = false })
							using (var panel = new InputUserControl { Dock = DockStyle.Fill })
							{
								form.Controls.Add(panel);
								FormsTheme.SetTheme(theme);
								FormsTheme.Apply(form);
								form.Show();
								panel.UpdateFrom(Device(23, 0x3F, 0x3, 1));
								Application.DoEvents();
								foreach (var chips in new[] { panel.ButtonChips, panel.AxisChips, panel.SliderChips, panel.PovChips })
								{
									var group = chips.Parent;
									var name = string.Format("{0} {1} px {2}", theme, width, group.Name);
									// A first row followed by another is full.
									var first = chips.BoundsOf(chips.Chips[0]);
									var row = chips.Chips.Select(x => chips.BoundsOf(x)).Where(x => x.Top == first.Top).ToList();
									if (row.Count < chips.Chips.Count)
										Assert.AreEqual(chips.ClientSize.Width - chips.Padding.Right, row.Max(x => x.Right), name + ": a full row of chips stops short of its strip's padding.");
									// The chips stand inside their strip's padding, a control's margin.
									var left = chips.Left + chips.Padding.Left - line * 2;
									var right = group.ClientSize.Width - line * 2 - (chips.Right - chips.Padding.Right);
									var below = group.ClientSize.Height - line * 2 - (chips.Bottom - chips.Padding.Bottom);
									Assert.AreEqual(left, right, name + ": the chips keep " + left + " px from the frame at the left and " + right + " px at the right.");
									if (chips == panel.ButtonChips)
										Assert.AreEqual(left, below, name + ": the chips keep " + left + " px from the frame at the sides and " + below + " px below.");
								}
							}
				}
				finally
				{
					FormsTheme.SetTheme(ThemeType.Light);
				}
			});
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("No two controls of the input panel cover each other, at any width, with a full-size device")]
		public void Input_panel_keeps_every_control_clear_of_the_others()
		{
			Ui.OnUiThread(() =>
			{
				using (var panel = new InputUserControl())
				{
					panel.CreateControl();
					// A device that fills every group: 128 buttons, all axes and sliders, four POVs.
					panel.UpdateFrom(Device(128, 0xFFFFFF, 0xFF, 4));
					var drawn = panel.Width;
					foreach (var width in new[] { drawn, (int)(drawn * 0.75), (int)(drawn * 1.5) })
					{
						panel.Width = width;
						panel.PerformLayout();
						var covered = ControlOverlapTest.Covered(panel).ToList();
						Assert.AreEqual(0, covered.Count, string.Format(
							"At {0} pixels wide, {1} control(s) are covered by another:{2}{3}",
							width, covered.Count, Environment.NewLine,
							string.Join(Environment.NewLine, covered)));
						foreach (var group in new Control[] { panel.ButtonsGroupBox, panel.AxesGroupBox, panel.SlidersGroupBox, panel.PovsGroupBox })
							Assert.IsTrue(group.Height > 0 && group.Width > 0, group.Name + " has a size.");
					}
				}
			});
		}
	}
}
