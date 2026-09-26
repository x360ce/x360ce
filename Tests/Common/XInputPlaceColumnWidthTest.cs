// @under-test: App.v4/Controls/UserDevicesUserControl.cs, App.v4/Controls/PadControl.cs
// @area: ui   @layer: unit
using JocysCom.ClassLibrary.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The XInput column widens to what the places now say.
	/// </summary>
	/// <remarks>
	/// The place text is made while the table paints and stored in no cell, so a column told to
	/// size itself to its cells never saw a change: it kept the width of the first paint, and a
	/// device reaching several places was cut off. Refreshing the places measures the column again.
	/// </remarks>
	[TestClass]
	public class XInputPlaceColumnWidthTest
	{
		const string Game = "place-column-width-test.exe";

		[TestMethod, TestCategory("ui")]
		public void The_column_widens_when_a_device_reaches_more_places()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), ProductName = "Width test pad", IsOnline = true };
			var settings = Enumerable.Range(1, 4)
				.Select(pad => new UserSetting { InstanceGuid = device.InstanceGuid, FileName = Game, MapTo = pad })
				.ToArray();
			var game = SettingsManager.CurrentGame;
			Global.InitDHelperHelper();
			var places = (int[])Global.DHelper.XiPlaceForPad.Clone();
			try
			{
				SettingsManager.UserDevices.Items.Add(device);
				foreach (var setting in settings)
					SettingsManager.UserSettings.Items.Add(setting);
				SettingsManager.CurrentGame = new UserGame { FileName = Game };
				for (var i = 0; i < 4; i++)
					Global.DHelper.XiPlaceForPad[i] = -1;
				Ui.OnUiThread(() =>
				{
					using (var form = new Form())
					using (var panel = new UserDevicesUserControl())
					{
						form.Controls.Add(panel);
						panel.AttachDataSource(new SortableBindingList<UserDevice> { device });
						form.Show();
						Application.DoEvents();
						var column = panel.XInputPlaceColumn;
						var before = column.Width;
						// The device now reaches every place through the four tabs it is mapped to.
						for (var i = 0; i < 4; i++)
							Global.DHelper.XiPlaceForPad[i] = i;
						var text = AppHelper.GetXInputPlaces(device);
						Assert.AreEqual("Virtual 1, Virtual 2, Virtual 3, Virtual 4", text);
						panel.RefreshPlaces();
						Application.DoEvents();
						var needed = TextRenderer.MeasureText(text, panel.DevicesDataGridView.Font).Width;
						Assert.IsTrue(column.Width > before, string.Format("The column stayed {0} px wide with a longer answer to show.", before));
						Assert.IsTrue(column.Width >= needed, string.Format("The column is {0} px wide; '{1}' needs {2}.", column.Width, text, needed));
					}
				});
			}
			finally
			{
				for (var i = 0; i < 4; i++)
					Global.DHelper.XiPlaceForPad[i] = places[i];
				SettingsManager.CurrentGame = game;
				foreach (var setting in settings)
					SettingsManager.UserSettings.Items.Remove(setting);
				SettingsManager.UserDevices.Items.Remove(device);
			}
		}
	}
}
