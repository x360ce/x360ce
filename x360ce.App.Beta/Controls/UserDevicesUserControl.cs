using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine.Data;
using x360ce.Engine;
using x360ce.App.Forms;

namespace x360ce.App.Controls
{
	public partial class UserDevicesUserControl : UserControl
	{
		public UserDevicesUserControl()
		{
			InitializeComponent();
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(DevicesDataGridView);
			EngineHelper.EnableDoubleBuffering(DevicesDataGridView);
		}

		private void ControllersUserControl_Load(object sender, EventArgs e)
		{
			UpdateButtons();
			DevicesDataGridView.AutoGenerateColumns = false;
			// WORKAROUND: Remove SelectionChanged event.
			DevicesDataGridView.SelectionChanged -= ControllersDataGridView_SelectionChanged;
			DevicesDataGridView.DataSource = SettingsManager.UserDevices.Items;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			BeginInvoke((MethodInvoker)delegate ()
			{
				DevicesDataGridView.SelectionChanged += ControllersDataGridView_SelectionChanged;
				ControllersDataGridView_SelectionChanged(DevicesDataGridView, new EventArgs());
			});
		}

		private void ControllersDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			var grid = (DataGridView)sender;
			var item = ((UserDevice)grid.Rows[e.RowIndex].DataBoundItem);
			if (e.ColumnIndex == grid.Columns[IsOnlineColumn.Name].Index)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			else if (e.ColumnIndex == grid.Columns[DeviceIdColumn.Name].Index)
			{
				var d = item.Device;
				if (d != null)
				{
				}
				//e.Value = item.de
			}
		}

		public UserDevice[] GetSelected()
		{
			var grid = DevicesDataGridView;
			var items = grid.SelectedRows.Cast<DataGridViewRow>().Select(x => (UserDevice)x.DataBoundItem).ToArray();
			return items;
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			DevicesDataGridView.Invalidate();
		}

		private void ControllerDeleteButton_Click(object sender, EventArgs e)
		{
			var items = GetSelected();
			foreach (var item in items)
			{
				SettingsManager.UserDevices.Items.Remove(item);
			}
			MainForm.Current.CloudPanel.Add(CloudAction.Delete, items, true);
		}

		private void ControllersDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		void UpdateButtons()
		{
			var grid = DevicesDataGridView;
			ControllerDeleteButton.Enabled = grid.SelectedRows.Count > 0;
		}

		#region Import

		/// <summary>
		/// Merge supplied list of items with current settings.
		/// </summary>
		/// <param name="items">List to merge.</param>
		public void ImportAndBindItems(IList<UserDevice> items)
		{
			var grid = DevicesDataGridView;
			var key = "InstanceGuid";
			var list = SettingsManager.UserDevices.Items;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, key);
			var newItems = items.ToArray();
			grid.DataSource = null;
			foreach (var newItem in newItems)
			{
				// Try to find existing item inside the list.
				var existingItems = list.Where(x => x.InstanceGuid == newItem.InstanceGuid).ToArray();
				// Remove existing items.
				for (int i = 0; i < existingItems.Length; i++)
				{
					list.Remove(existingItems[i]);
				}
				// Add new one.
				list.Add(newItem);
			}
			MainForm.Current.SetHeaderBody("{0} {1}(s) loaded.", items.Count(), typeof(UserDevice).Name);
			grid.DataSource = list;
			JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save(true);
		}

		#endregion

		private void HardwareButton_Click(object sender, EventArgs e)
		{
			var form = new HardwareForm();
			form.ShowDialog();
			form.Dispose();
		}

		private void AddDemoDevice_Click(object sender, EventArgs e)
		{
			var ud = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(ud);
			MainForm.Current.DHelper.UpdateDevicesEnabled = true;
		}

		List<string> GetIds(UserDevice ud)
		{
			var list = new List<string>();
			var ids = ViGEm.HidGuardianHelper.GetHardwareIds(ud.HidDeviceId);
			if (ids.Length == 0)
				return list;
			// Add hardware ids to the list.
			if (!string.IsNullOrEmpty(ud.HidHardwareIds))
			{
				var hids = ud.HidHardwareIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < hids.Length; i++)
				{
					// If hardware id have VID and PID then...
					if (hids[i].StartsWith(ids[0], StringComparison.OrdinalIgnoreCase))
						list.Add(hids[i]);
				}
			}
			return list;
		}

		private void DevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var ud = (UserDevice)row.DataBoundItem;
			// If user clicked on the CheckBox column then...
			if (e.ColumnIndex == grid.Columns[IsEnabledColumn.Name].Index)
			{
				// Changed check (enabled state) of the current item.
				ud.IsEnabled = !ud.IsEnabled;
			}
			else if (e.ColumnIndex == grid.Columns[IsHiddenColumn.Name].Index)
			{
				//var guardianHardwareId = ViGEm.HidGuardianHelper.GetHardwareId(item.HidDevicePath);
				var hidDeviceId = ud.HidDeviceId;
				if (!string.IsNullOrEmpty(hidDeviceId))
				{
					var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
					if (canModify)
					{

						ud.IsHidden = !ud.IsHidden;
						if (ud.IsHidden)
						{
							ViGEm.HidGuardianHelper.InsertToAffected(hidDeviceId);
						}
						else
						{
							ViGEm.HidGuardianHelper.RemoveFromAffected(hidDeviceId);
						}
					}
					else
					{
						MessageBoxForm form = new MessageBoxForm();
						form.StartPosition = FormStartPosition.CenterParent;
						form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
			}
		}

		private void ShowHiddenDevicesMenuItem_Click(object sender, EventArgs e)
		{
			var devices = ViGEm.HidGuardianHelper.GetAffected();
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var text = devices.Length == 0
				? "None"
				// Join and make && visible.
				: string.Join("\r\n", devices).Replace("&", "&&");
			form.ShowForm(text, "Affected Devices", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ShowEnumeratedDevicesMenuItem_Click(object sender, EventArgs e)
		{
			var devices = ViGEm.HidGuardianHelper.GetEnumeratedDevices();
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			var text = devices.Length == 0
				? "None"
				// Join and make && visible.
				: string.Join("\r\n", devices).Replace("&", "&&");
			form.ShowForm(text, "Enumerated Devices", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void UnhideAllDevicesMenuItem_Click(object sender, EventArgs e)
		{
			ViGEm.HidGuardianHelper.ClearAffected();
			var devices = SettingsManager.UserDevices.Items.ToArray();
			for (int i = 0; i < devices.Length; i++)
				devices[i].IsHidden = false;
		}
	}
}
