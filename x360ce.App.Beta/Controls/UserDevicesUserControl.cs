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
			var userDevices = GetSelected();
			foreach (var item in userDevices)
			{
				SettingsManager.UserDevices.Items.Remove(item);
			}
			MainForm.Current.CloudPanel.Add(CloudAction.Delete, userDevices, true);
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
			MainForm.Current.SetHeaderInfo("{0} {1}(s) loaded.", items.Count(), typeof(UserDevice).Name);
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

		/// <summary>
		/// Get all IDs required for HID guardian to block device.
		/// </summary>
		/// <param name="ud"></param>
		/// <returns></returns>
		string[] GetIdsToBlock(string hidDeviceId, string hidHardwareIds)
		{
			var list = new List<string>();
			var ids = ViGEm.HidGuardianHelper.ConvertToHidVidPid(hidDeviceId);
			if (ids.Length == 0)
				return list.ToArray();
			// If no hardware ids then return;
			if (string.IsNullOrEmpty(hidHardwareIds))
				return list.ToArray();
			// Extract all IDs which starts from VID and PID.
			var hwids = hidHardwareIds
				.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(x => x.StartsWith(ids[0], StringComparison.OrdinalIgnoreCase))
				.ToArray();
			// Add results to the list.
			list.AddRange(hwids);
			return list.ToArray();
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
				var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
				if (canModify)
				{
					var ids = GetIdsToBlock(ud.HidDeviceId, ud.HidHardwareIds);
					ud.IsHidden = !ud.IsHidden;
					if (ud.IsHidden)
					{
						ViGEm.HidGuardianHelper.InsertToAffected(ids);
					}
					else
					{
						ViGEm.HidGuardianHelper.RemoveFromAffected(ids);
					}
				}
				else
				{
					var form = new MessageBoxForm();
					form.StartPosition = FormStartPosition.CenterParent;
					form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

		private void synchronizeToHidGuardianToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Get all devices which must be hidden.
			var devices = SettingsManager.UserDevices.Items.Where(x=>x.IsHidden).ToList();
			// Get all Ids.
			var ids = new List<string>();
			foreach (var ud in devices)
			{
				var idsToBlock = GetIdsToBlock(ud.HidDeviceId, ud.HidHardwareIds);
				ids.AddRange(idsToBlock);
			}
			var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
			if (canModify)
			{
				var idsToBlock = ids.Distinct().ToArray();
				ViGEm.HidGuardianHelper.InsertToAffected(idsToBlock);
			}
			else
			{
				var form = new MessageBoxForm();
				form.StartPosition = FormStartPosition.CenterParent;
				form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}
}
