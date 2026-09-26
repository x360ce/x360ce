// @under-test: App.v4/Controls/XInputDevicesUserControl.cs, App.v4/Controls/XInputDevicesUserControl.Designer.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.App.DInput;
using x360ce.Engine.UiTree;

namespace x360ce.Tests
{
	/// <summary>
	/// Putting controllers in order is asked, followed and reported under the list, not in a window of its own.
	/// </summary>
	/// <remarks>
	/// A message box stopped anything driving the program: the button that opened it did not answer
	/// until somebody closed it, and it could open behind the program. The notice is part of the page,
	/// so a press returns at once and the notice's own buttons are reached by path like any others.
	/// Cancel is the only answer given here: OK switches real controllers.
	/// </remarks>
	[TestClass]
	public class XInputOrderNoticeTest
	{
		static XInputReorderPlan.Entry Entry(string id, int place, bool isVirtual, int pad)
		{
			return new XInputReorderPlan.Entry { HardwareId = id, Name = id, IsVirtual = isVirtual, IsOurs = isVirtual, Place = place, Pad = pad };
		}

		/// <summary>The list the control shows, filled here rather than from the machine.</summary>
		/// <remarks>
		/// The control starts reading the machine as it is made, and that answer arrives as a message
		/// this thread never pumps, so what the test puts in the list is what stays there.
		/// </remarks>
		static List<XInputReorderPlan.Entry> ListOf(XInputDevicesUserControl control)
		{
			return (List<XInputReorderPlan.Entry>)typeof(XInputDevicesUserControl)
				.GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Auto-Order asks under the list, reachable by path, and Cancel puts the page back")]
		public void Auto_order_asks_under_the_list_and_cancel_puts_the_page_back()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ShowInTaskbar = false })
				{
					var control = new XInputDevicesUserControl { Dock = DockStyle.Top, Height = 180 };
					form.Controls.Add(control);
					form.Show();
					// As reported: the real controller took place one, Controller 1's virtual one place three.
					var list = ListOf(control);
					list.Add(Entry("xbox", 0, false, 0));
					list.Add(Entry("c2", 1, true, 2));
					list.Add(Entry("c1", 2, true, 1));
					var grid = (DataGridView)UiTreeWalker.Find(control, "DevicesDataGridView");
					var gridHeight = grid.Height;

					Assert.IsNull(UiTreeWalker.Invoke(UiTreeWalker.Find(control, "DevicesToolStrip/AutoOrderButton")),
						"Auto-Order could not be pressed by path.");

					Assert.AreEqual("Put controllers in this order?",
						UiTreeWalker.GetValue(UiTreeWalker.Find(control, "PlanPanel/PlanSubjectLabel")));
					StringAssert.Contains(UiTreeWalker.GetValue(UiTreeWalker.Find(control, "PlanPanel/PlanBodyLabel")),
						"Switch off xbox", "The notice does not say what will happen.");
					Assert.AreEqual(gridHeight, grid.Height, string.Format(
						"The notice took its room from the list instead of adding its own: page {0}, notice {1}.",
						control.Bounds, ((Control)UiTreeWalker.Find(control, "PlanPanel")).Bounds));
					Assert.IsTrue(control.Height > 180, "The page did not grow to hold the notice.");
					// Docked in the wrong order, the list took the whole page and the notice was drawn over
					// the bar and the list, with the room it had been given left empty below.
					var notice = (Control)UiTreeWalker.Find(control, "PlanPanel");
					var bar = (Control)UiTreeWalker.Find(control, "DevicesToolStrip");
					Assert.IsTrue(bar.Bottom <= grid.Top && grid.Bottom <= notice.Top && notice.Bottom <= control.ClientSize.Height,
						string.Format("The bar, the list and the notice are not one below the other: bar {0}, list {1}, notice {2}.",
							bar.Bounds, grid.Bounds, notice.Bounds));
					Assert.IsFalse(UiTreeWalker.Find(control, "DevicesToolStrip/ApplyButton") is ToolStripItem apply && apply.Enabled,
						"Apply can be pressed while an order is waiting for an answer.");

					Assert.IsNull(UiTreeWalker.Invoke(UiTreeWalker.Find(control, "PlanPanel/PlanCancelButton")),
						"Cancel could not be pressed by path.");
					Assert.AreEqual(180, control.Height, "Closing the notice did not give the room back.");
					Assert.IsTrue(((ToolStripItem)UiTreeWalker.Find(control, "DevicesToolStrip/ApplyButton")).Enabled,
						"Apply stayed held after the notice closed.");
				}
			});
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A virtual row names the devices mapped to its controller, and follows the mapping when it changes")]
		public void A_virtual_row_names_what_is_mapped_to_its_controller()
		{
			Ui.OnUiThread(() =>
			{
				var game = SettingsManager.CurrentGame;
				var pad = new Engine.Data.UserDevice { InstanceGuid = System.Guid.NewGuid(), ProductName = "Logitech Cordless RumblePad 2", IsOnline = true };
				var mapping = new Engine.Data.UserSetting { FileName = "x360ce.test.exe", MapTo = (int)Engine.MapTo.Controller1, InstanceGuid = pad.InstanceGuid };
				SettingsManager.CurrentGame = new Engine.Data.UserGame { FileName = "x360ce.test.exe" };
				SettingsManager.UserDevices.Items.Add(pad);
				SettingsManager.UserSettings.Items.Add(mapping);
				try
				{
					using (var form = new Form { ShowInTaskbar = false })
					{
						var control = new XInputDevicesUserControl { Dock = DockStyle.Top, Height = 180 };
						form.Controls.Add(control);
						form.Show();
						// As reported: the real controller holds Controller 1's place, and Controller 1 waits.
						var list = ListOf(control);
						list.Add(Entry("xbox", 0, false, 0));
						var waiting = Entry("Controller 1", -1, true, 1);
						waiting.Waiting = true;
						list.Add(waiting);
						Assert.IsNull(UiTreeWalker.Invoke(UiTreeWalker.Find(control, "DevicesToolStrip/AutoOrderButton")));
						var grid = (DataGridView)UiTreeWalker.Find(control, "DevicesDataGridView");
						var device = grid.Columns["NameColumn"].Index;
						var mapped = grid.Columns["MappedColumn"].Index;
						Assert.AreEqual(grid.Columns.Count - 1, mapped, "The mapped devices are not the last column.");
						Assert.AreEqual("Logitech Cordless RumblePad 2", grid.Rows[0].Cells[mapped].Value,
							"The waiting row does not say what is mapped to Controller 1.");
						Assert.AreEqual("Controller 1", grid.Rows[0].Cells[device].Value,
							"The device column no longer names the XInput device.");
						Assert.AreEqual("xbox", grid.Rows[1].Cells[device].Value, "A real row no longer names the device itself.");
						Assert.AreEqual("", grid.Rows[1].Cells[mapped].Value, "A real row names devices mapped to a controller it is not.");
						SettingsManager.UserSettings.Items.Remove(mapping);
						Assert.AreEqual("Nothing mapped", grid.Rows[0].Cells[mapped].Value,
							"The list still names a device after it was unmapped.");
					}
				}
				finally
				{
					SettingsManager.UserSettings.Items.Remove(mapping);
					SettingsManager.UserDevices.Items.Remove(pad);
					SettingsManager.CurrentGame = game;
				}
			});
		}

		[TestMethod, TestCategory("devices")]
		[Description("An order that cannot be made is said under the list and closed with OK")]
		public void A_refusal_is_said_under_the_list_and_closed_with_OK()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ShowInTaskbar = false })
				{
					var control = new XInputDevicesUserControl { Dock = DockStyle.Top, Height = 180 };
					form.Controls.Add(control);
					form.Show();
					var list = ListOf(control);
					list.Add(Entry("a", 0, true, 1));
					list.Add(Entry("a", 1, true, 2));

					Assert.IsNull(UiTreeWalker.Invoke(UiTreeWalker.Find(control, "DevicesToolStrip/ApplyButton")));

					Assert.AreEqual("Cannot put them in that order",
						UiTreeWalker.GetValue(UiTreeWalker.Find(control, "PlanPanel/PlanSubjectLabel")));
					Assert.IsFalse(((Button)UiTreeWalker.Find(control, "PlanPanel/PlanCancelButton")).Visible,
						"Cancel is offered where there is nothing to cancel.");
					Assert.IsNull(UiTreeWalker.Invoke(UiTreeWalker.Find(control, "PlanPanel/PlanOkButton")));
					Assert.AreEqual(180, control.Height, "OK did not close the notice.");
				}
			});
		}
	}
}
