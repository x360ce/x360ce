using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using x360ce.Engine.Data;
using x360ce.Engine;
using x360ce.App.Forms;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using JocysCom.ClassLibrary.IO;
using System.Drawing;
using System.Threading.Tasks;
using JocysCom.ClassLibrary.Win32;

namespace x360ce.App.Controls
{
	public partial class UserDevicesUserControl : UserControl
	{
		public UserDevicesUserControl()
		{
			InitializeComponent();
			// Make font more consistent with the rest of the interface.
			Controls.OfType<ToolStrip>().ToList().ForEach(x => x.Font = Font);
			JocysCom.ClassLibrary.Controls.ControlsHelper.ApplyBorderStyle(DevicesDataGridView);
			EngineHelper.EnableDoubleBuffering(DevicesDataGridView);
		}

		/// <summary>
		/// Use this method to resolve format exception:
		///     Invalid cast from 'System.Boolean' to 'System.Drawing.Image'
		/// after list updated from the cloud with ImportAndBindItems(...) method
		/// </summary>
		public void AttachDataSource(SortableBindingList<UserDevice> data)
		{
			UpdateButtons();
			DevicesDataGridView.AutoGenerateColumns = false;
			// WORKAROUND: Remove SelectionChanged event.
			DevicesDataGridView.SelectionChanged -= ControllersDataGridView_SelectionChanged;
			DevicesDataGridView.DataSource = data;
			if (!IsHandleCreated)
			{
				DevicesDataGridView.SelectionChanged += ControllersDataGridView_SelectionChanged;
				ControllersDataGridView_SelectionChanged(DevicesDataGridView, new EventArgs());
				return;
			}
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				DevicesDataGridView.SelectionChanged += ControllersDataGridView_SelectionChanged;
				ControllersDataGridView_SelectionChanged(DevicesDataGridView, new EventArgs());
			});
		}

		public bool MapDeviceToControllerMode;

		private void ControllersUserControl_Load(object sender, EventArgs e)
		{
			if (MapDeviceToControllerMode)
			{
				var list = new SortableBindingList<UserDevice>();
				list.SynchronizingObject = ControlsHelper.MainTaskScheduler;
				var devices = SettingsManager.UserDevices.ItemsToArraySyncronized()
					.Where(x => x.ConnectionClass != DEVCLASS.SYSTEM).ToList();
				list.AddRange(devices);
				AttachDataSource(list);
			}
			else
			{
				AttachDataSource(SettingsManager.UserDevices.Items);

			}
		}

		private void DevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;

			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var item = (UserDevice)row.DataBoundItem;
			if (column == IsOnlineColumn)
			{
				e.Value = item.IsOnline
					? Properties.Resources.bullet_square_glass_green
					: Properties.Resources.bullet_square_glass_grey;
			}
			else if (column == ConnectionClassColumn)
			{
				e.Value = item.ConnectionClass == Guid.Empty
					? new Bitmap(16, 16)
					: DeviceDetector.GetClassIcon(item.ConnectionClass, 16)?.ToBitmap();
			}
			else if (column == IsHiddenColumn)
			{
				var left = row.Cells[e.ColumnIndex].OwningColumn.Width;
				// Show checkbox.
				if (item.AllowHide && e.CellStyle.Padding.Left >= 0)
					e.CellStyle.Padding = new Padding();
				// Hide checkbox (move out of the sight).
				if (!item.AllowHide && e.CellStyle.Padding.Left == 0)
					e.CellStyle.Padding = new Padding(left, 0, 0, 0);
			}
			else if (column == DeviceIdColumn)
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
			// Remove from local settings.
			foreach (var item in userDevices)
				SettingsManager.UserDevices.Items.Remove(item);
			SettingsManager.Save();
			// Remove from cloud settings.
			Task.Run(new Action(() =>
			{
				foreach (var item in userDevices)
					Global.CloudClient.Add(CloudAction.Delete, new UserDevice[] { item });
			}));
		}

		private void ControllersDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		void UpdateButtons()
		{
			var grid = DevicesDataGridView;
			ControllerDeleteButton.Enabled = grid.DataSource != null && grid.SelectedRows.Count > 0;
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
			AttachDataSource(null);
			foreach (var newItem in newItems)
			{
				// Try to find existing item inside the list.
				var existingItems = list.Where(x => x.InstanceGuid == newItem.InstanceGuid).ToArray();
				// Remove existing items.
				for (int i = 0; i < existingItems.Length; i++)
					list.Remove(existingItems[i]);
				// Add new one.
				list.Add(newItem);
			}
			MainForm.Current.SetHeaderInfo("{0} {1}(s) loaded.", items.Count(), typeof(UserDevice).Name);
			AttachDataSource(list);
			JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save();
		}

		#endregion

		private void HardwareButton_Click(object sender, EventArgs e)
		{
			var form = new HardwareForm();
			form.StartPosition = FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
		}

		private void AddDemoDevice_Click(object sender, EventArgs e)
		{
			var ud = TestDeviceHelper.NewUserDevice();
			SettingsManager.UserDevices.Items.Add(ud);
			MainForm.Current.DHelper.UpdateDevicesEnabled = true;
		}

		private void DevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			var ud = (UserDevice)row.DataBoundItem;
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				// Changed check (enabled state) of the current item.
				ud.IsEnabled = !ud.IsEnabled;
			}
			else if (column == IsHiddenColumn)
			{
				if (ud.AllowHide)
				{
					var canModify = ViGEm.HidGuardianHelper.CanModifyParameters(true);
					if (canModify)
					{
						//var ids = AppHelper.GetIdsToAffect(ud.HidDeviceId, ud.HidHardwareIds);
						var ids = new string[] { ud.DevDeviceId };
						// If parent device ID is known then...
						//if (!string.IsNullOrEmpty(parentDeviceId))
						//{
						ud.IsHidden = !ud.IsHidden;
						if (ud.IsHidden)
						{
							ViGEm.HidGuardianHelper.InsertToAffected(ids);
							//ViGEm.HidGuardianHelper.InsertToAffected(parentDeviceId, ud.HidDeviceId);
						}
						else
						{
							ViGEm.HidGuardianHelper.RemoveFromAffected(ids);
							//ViGEm.HidGuardianHelper.RemoveFromAffected(parentDeviceId, ud.HidDeviceId);
						}
						//}
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
			AppHelper.UnhideAllDevices();
		}

		private void synchronizeToHidGuardianToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var canModify = AppHelper.SynchronizeToHidGuardian();
			if (!canModify)
			{
				var form = new MessageBoxForm();
				form.StartPosition = FormStartPosition.CenterParent;
				form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		[DefaultValue(true), Browsable(true)]
		public bool IsVisibleIsHiddenColumn
		{
			get { return IsHiddenColumn.Visible; }
			set { IsHiddenColumn.Visible = false; }
		}

		private void DevicesDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
		{
		}
	}
}
