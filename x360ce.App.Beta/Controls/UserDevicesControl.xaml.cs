using JocysCom.ClassLibrary.Collections;
using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using x360ce.App.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for UserDevicesUserControl.xaml
	/// </summary>
	public partial class UserDevicesControl : UserControl
	{
		public UserDevicesControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		SortableBindingList<UserDevice> _currentData;

		/// <summary>
		/// Use this method to resolve format exception:
		///     Invalid cast from 'System.Boolean' to 'System.Drawing.Image'
		/// after list updated from the cloud with ImportAndBindItems(...) method
		/// </summary>
		public void AttachDataSource(SortableBindingList<UserDevice> data)
		{
			UpdateButtons();
			MainDataGrid.AutoGenerateColumns = false;
			_currentData = data;
			MainDataGrid.ItemsSource = _currentData;
		}

		public bool MapDeviceToControllerMode;

		async private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			_currentData = null;
			SettingsManager.UserDevices.Items.ListChanged -= Items_ListChanged;
			SettingsManager.UserDevices.Items.ListChanged += Items_ListChanged;
			ShowSystemDevicesButton.Visibility = MapDeviceToControllerMode ? Visibility.Visible : Visibility.Collapsed;
			if (MapDeviceToControllerMode)
			{
				IsHiddenColumn.Visibility = Visibility.Collapsed;
				IsEnabledColumn.Visibility = Visibility.Collapsed;
				await RefreshMapDeviceToList().ConfigureAwait(true);
			}
			else
			{
				AttachDataSource(SettingsManager.UserDevices.Items);
			}
		}

		async Task RefreshMapDeviceToList()
		{
			var list = new SortableBindingList<UserDevice>();
			// Exclude System/Virtual devices.
			UserDevice[] devices;
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				devices = SettingsManager.UserDevices.Items
					.Where(x => ShowSystemDevices || x.ConnectionClass != DEVCLASS.SYSTEM)
					.ToArray();
			}
			list.AddRange(devices);
			list.SynchronizingObject = ControlsHelper.MainTaskScheduler;
			// If new list, item added or removed then...
			if (_currentData == null)
				AttachDataSource(list);
			else if (_currentData.Count != list.Count)
				CollectionsHelper.Synchronize(list, _currentData);
		}

		private void Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If item added or deleted from original list then...
			if (
				e.ListChangedType == ListChangedType.ItemAdded ||
				e.ListChangedType == ListChangedType.ItemDeleted
			)
				// Update list.
				RefreshMapDeviceToList().ConfigureAwait(true);
		}

		//private void MainDataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		//{
		//	if (e.RowIndex < 0 || e.ColumnIndex < 0)
		//		return;

		//	var grid = (DataGridView)sender;
		//	var row = grid.Rows[e.RowIndex];
		//	var column = grid.Columns[e.ColumnIndex];
		//	var item = (UserDevice)row.DataBoundItem;
		//	if (column == IsOnlineColumn)
		//	{
		//		e.Value = item.IsOnline
		//			? Properties.Resources.bullet_square_glass_green
		//			: Properties.Resources.bullet_square_glass_grey;
		//	}
		//	else if (column == ConnectionClassColumn)
		//	{
		//		e.Value = item.ConnectionClass == Guid.Empty
		//			? new Bitmap(16, 16)
		//			: DeviceDetector.GetClassIcon(item.ConnectionClass, 16)?.ToBitmap();
		//	}
		//	else if (column == IsHiddenColumn)
		//	{
		//		var left = row.Cells[e.ColumnIndex].OwningColumn.Width;
		//		// Show checkbox.
		//		if (item.AllowHide && e.CellStyle.Padding.Left >= 0)
		//			e.CellStyle.Padding = new Padding();
		//		// Hide checkbox (move out of the sight).
		//		if (!item.AllowHide && e.CellStyle.Padding.Left == 0)
		//			e.CellStyle.Padding = new Padding(left, 0, 0, 0);
		//	}
		//	else if (column == DeviceIdColumn)
		//	{
		//		var d = item.Device;
		//		if (d != null)
		//		{
		//		}
		//		//e.Value = item.de
		//	}
		//}

		public UserDevice[] GetSelected()
		{
			var grid = MainDataGrid;
			var items = grid.SelectedItems.Cast<UserDevice>().ToArray();
			return items;
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			//MainDataGrid.Invalidate();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var userDevices = GetSelected();
			// Remove from local settings.
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				foreach (var item in userDevices)
					SettingsManager.UserDevices.Items.Remove(item);
			}
			SettingsManager.Save();
			// Remove from cloud settings.
			Task.Run(new Action(() =>
			{
				foreach (var item in userDevices)
					Global.CloudClient.Add(CloudAction.Delete, new UserDevice[] { item });
			}));
		}

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtons();
		}


		void UpdateButtons()
		{
			var grid = MainDataGrid;
			DeleteButton.IsEnabled = grid.SelectedItems.Count > 0;
		}

		#region Import

		/// <summary>
		/// Merge supplied list of items with current settings.
		/// </summary>
		/// <param name="items">List to merge.</param>
		public void ImportAndBindItems(IList<UserDevice> items)
		{
			var grid = MainDataGrid;
			var list = SettingsManager.UserDevices.Items;
			//var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, key);
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
			MainForm.Current.SetBodyInfo("{0} {1}(s) loaded.", items.Count(), typeof(UserDevice).Name);
			AttachDataSource(list);
			//JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
			SettingsManager.Save();
		}

		#endregion

		private void HardwareButton_Click(object sender, RoutedEventArgs e)
		{
			var form = new HardwareForm();
			form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			ControlsHelper.CheckTopMost(form);
			form.ShowDialog();
			form.Dispose();
		}

		private void AddDemoDeviceButton_Click(object sender, RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				var ud = TestDeviceHelper.NewUserDevice();
				lock (SettingsManager.UserDevices.SyncRoot)
					SettingsManager.UserDevices.Items.Add(ud);
				Global.DHelper.UpdateDevicesEnabled = true;
			});
		}

		private void ShowHiddenDevicesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var devices = ViGEm.HidGuardianHelper.GetAffected();
			var form = new MessageBoxWindow();
			var text = devices.Length == 0
				? "None"
				// Join and make && visible.
				: string.Join("\r\n", devices).Replace("&", "&&");
			form.ShowDialog(text, "Affected Devices", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ShowEnumeratedDevicesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var devices = ViGEm.HidGuardianHelper.GetEnumeratedDevices();
			var form = new MessageBoxWindow();
			var text = devices.Length == 0
				? "None"
				// Join and make && visible.
				: string.Join("\r\n", devices).Replace("&", "&&");
			form.ShowDialog(text, "Enumerated Devices", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void UnhideAllDevicesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			AppHelper.UnhideAllDevices();
		}

		private void SynchronizeToHidGuardianMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var canModify = AppHelper.SynchronizeToHidGuardian();
			if (!canModify)
			{
				var form = new MessageBoxWindow();
				form.ShowDialog("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		[DefaultValue(true), Browsable(true)]
		public bool IsVisibleIsHiddenColumn
		{
			get { return IsHiddenColumn.Visibility == Visibility.Visible; }
			set { IsHiddenColumn.Visibility = value ? Visibility.Visible : Visibility.Hidden; }
		}

		//private void MainDataGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
		//{
		//}

		bool ShowSystemDevices = false;

		async private void ShowSystemDevicesButton_Click(object sender, RoutedEventArgs e)
		{
			var newValue = ShowSystemDevicesButton.IsChecked ?? false;
			// ShowSystemDevicesButton.IsChecked = newValue;
			ShowSystemDevicesContent.Content = newValue
				? Resources[Icons_Default.Icon_checkbox]
				: Resources[Icons_Default.Icon_checkbox_unchecked];
			ShowSystemDevices = newValue;
			await RefreshMapDeviceToList();
		}

	}
}
