using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
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
	}
}
