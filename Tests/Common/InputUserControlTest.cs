// @under-test: App.v4/Controls/PadTabPages/General/InputUserControl.cs
// @area: ui   @layer: unit
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
			ud.DiState = new CustomDiState(new JoystickState());
			for (var i = 0; i < ud.DiState.Axis.Length; i++)
				ud.DiState.Axis[i] = InputChips.AxisCentre;
			for (var i = 0; i < ud.DiState.Povs.Length; i++)
				ud.DiState.Povs[i] = InputChips.PovRest;
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

					ud.DiState.Buttons[2] = true;
					ud.DiState.Axis[1] = 65535;
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
