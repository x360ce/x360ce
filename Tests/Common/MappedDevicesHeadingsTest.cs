// @under-test: App.v4/Controls/PadControl.cs, App.v4/Controls/PadControl.Designer.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The list of devices on a controller tab heads its icon columns as the Devices page does, short or blank, while a
	/// screen reader still hears each column's full name, and shows the AHK column, before Instance ID, while AutoHotkey
	/// scripts are allowed.
	/// </summary>
	[TestClass]
	public class MappedDevicesHeadingsTest
	{
		[TestMethod, TestCategory("mapping"), TestCategory("accessibility")]
		[Description("The icon columns show On and no heading, and still give a screen reader Online and Connection")]
		public void Icon_columns_are_short_but_still_named()
		{
			Ui.OnUiThread(() =>
			{
				using (var pad = new PadControl(MapTo.Controller1))
				{
					var grid = pad.MappedDevicesDataGridView;
					var online = grid.Columns["IsOnlineColumn"];
					var connection = grid.Columns["ConnectionClassColumn"];
					Assert.AreEqual("On", online.HeaderText);
					Assert.AreEqual("", connection.HeaderText);
					Assert.AreEqual("Online", online.HeaderCell.AccessibilityObject.Name, "A screen reader does not hear the Online column's name.");
					Assert.AreEqual("Connection", connection.HeaderCell.AccessibilityObject.Name, "A screen reader does not hear the Connection column's name.");
					Assert.AreEqual("Connection", connection.ToolTipText, "The blank heading does not name its column on hover.");
					var copy = (DataGridViewColumnHeaderCell)connection.HeaderCell.Clone();
					Assert.AreEqual("Connection", copy.AccessibilityObject.Name, "A copy of the heading lost the column's name.");
				}
			});
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The AHK column sits before Instance ID and shows while AutoHotkey scripts are allowed")]
		public void AHK_column_shows_while_scripts_are_allowed()
		{
			Ui.OnUiThread(() =>
			{
				var allowed = SettingsManager.Options.AllowAutoHotkeyScripts;
				try
				{
					SettingsManager.Options.AllowAutoHotkeyScripts = false;
					using (var pad = new PadControl(MapTo.Controller1))
					{
						var grid = pad.MappedDevicesDataGridView;
						var ahk = grid.Columns["ScriptNameColumn"];
						Assert.AreEqual("AHK", ahk.HeaderText);
						Assert.AreEqual(grid.Columns["InstanceIdColumn"].DisplayIndex - 1, ahk.DisplayIndex, "The AHK column is not just before Instance ID.");
						Assert.IsFalse(ahk.Visible, "The AHK column shows while scripts are not allowed.");
						SettingsManager.Options.AllowAutoHotkeyScripts = true;
						Assert.IsTrue(ahk.Visible, "Allowing scripts did not show the AHK column.");
						SettingsManager.Options.AllowAutoHotkeyScripts = false;
						Assert.IsFalse(ahk.Visible, "Not allowing scripts did not hide the AHK column.");
					}
				}
				finally
				{
					SettingsManager.Options.AllowAutoHotkeyScripts = allowed;
				}
			});
		}
	}
}
