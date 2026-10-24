// @under-test: App.v4/Controls/PadTabPages/DirectInputControl.cs, App.v4/Controls/PadTabPages/DirectInputControl.Designer.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.App.Plugins;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The Direct Input tab numbers buttons as the mapping boxes do (issue #1498).
	/// </summary>
	/// <remarks>
	/// Windows names a device's buttons from 0, and the mapping boxes number them from 1. The tab showed pressed
	/// buttons and the device's list in Windows' numbering only, so the button Windows calls Button 0 looked as if no
	/// box could take it: the boxes offer Button 1 onwards. The tab now numbers pressed buttons as the boxes do, and
	/// says beside each button Windows lists what a box shows for it.
	/// </remarks>
	[TestClass]
	public class ButtonNumberingTest
	{
		[TestMethod, TestCategory("devices"), TestCategory("ui")]
		[Description("The Direct Input tab numbers buttons as the mapping boxes do, beside the names Windows gives them")]
		public void The_direct_input_tab_numbers_buttons_as_the_mapping_boxes_do()
		{
			Ui.OnUiThread(() =>
			{
				using (var form = new Form { ShowInTaskbar = false })
				using (var tab = new DirectInputUserControl { Dock = DockStyle.Fill })
				{
					form.Controls.Add(tab);
					form.Show();
					// As reported: fourteen buttons, which Windows names Button 0 to Button 13, with an axis and a hat beside them.
					var ud = new UserDevice
					{
						InstanceGuid = Guid.NewGuid(),
						ProductName = "Riffmaster",
						CapButtonCount = 14,
						DeviceObjects = PluginDevices.GetDeviceObjects(1, 0, 1, 14),
						JoState = new JoystickState(),
					};
					ud.JoState.Buttons[0] = true;
					tab.UpdateFrom(ud);
					Application.DoEvents();
					// What lands in a mapping box for each button, as the General tab and the boxes' menu say it.
					var boxes = InputChips.Create(14, 0, 0, 0).Select(x => x.Payload).ToArray();
					Assert.AreEqual("Button 1", boxes[0]);

					var pressed = tab.Controls.Find("DiButtonsTextBox", true).Single();
					Assert.AreEqual("01", pressed.Text, "The first button, pressed, is not numbered as the box that takes it: " + boxes[0] + ".");

					var grid = (DataGridView)tab.Controls.Find("DiObjectsDataGridView", true).Single();
					var mapsAs = grid.Columns["MapsAsColumn"].Index;
					Assert.AreEqual(grid.Columns["ObjectsInstanceColumn"].Index + 1, mapsAs, "What a box shows is not beside the number Windows gives.");
					var rows = grid.Rows.Cast<DataGridViewRow>().ToArray();
					Assert.AreEqual(16, rows.Length);
					foreach (var row in rows)
					{
						var item = (DeviceObjectItem)row.DataBoundItem;
						var shown = row.Cells[mapsAs].FormattedValue;
						if (item.Type == ObjectGuid.Button)
							Assert.AreEqual(boxes[item.Instance], shown, item.Name + " does not say what a mapping box shows for it.");
						else
							Assert.AreEqual("", shown, item.Name + " is not a button, and no button number belongs beside it.");
					}
					var named0 = rows.Single(x => ((DeviceObjectItem)x.DataBoundItem).Name == "Button 0");
					Assert.AreEqual("Button 1", named0.Cells[mapsAs].FormattedValue, "The button Windows calls Button 0 is the one the boxes call Button 1.");
				}
			});
		}
	}
}
