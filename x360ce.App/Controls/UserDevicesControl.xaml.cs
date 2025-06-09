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
            InitHelper.InitTimer(this, InitializeComponent);
            if (ControlsHelper.IsDesignMode(this))
                return;
            MainDataGrid.AutoGenerateColumns = false;
        }

        ObservableCollectionInvoked<UserDevice> _currentData;

        public bool MapDeviceToControllerMode;

        #region Used when MapDeviceToControllerMode = true

        /// <summary>
        /// Show DInput devices for mapping to XInput virtual device.
        /// </summary>
        /// <returns></returns>
        void RefreshMapDeviceToList()
        {
            var list = SettingsManager.UserDevices.Items.ToList();
            if (MapDeviceToControllerMode)
            {
                // Exclude System/Virtual devices.
                list = list
                    .Where(x => ShowSystemDevices || x.ConnectionClass != DEVCLASS.SYSTEM)
                    .ToList();
            }
            // Synchronize list.
            CollectionsHelper.Synchronize(list, _currentData);
            MainDataGrid.ItemsSource = _currentData;
            ControlsHelper.SetSelection(MainDataGrid, nameof(UserDevice.InstanceGuid), gridSelection, 0);
        }

        private void Items_ListChanged(object sender, ListChangedEventArgs e)
        {
            // If device added removed then...
            if (e.ListChangedType == ListChangedType.ItemAdded || e.ListChangedType == ListChangedType.ItemDeleted)
            {
                RefreshMapDeviceToList();
            }
        }

        bool ShowSystemDevices = false;

        private void ShowSystemDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            var newValue = ShowSystemDevicesButton.IsChecked ?? false;
            //ShowSystemDevicesContent.Content = newValue
            //	? Icons_Default.Current[Icons_Default.Icon_checkbox]
            //	: Icons_Default.Current[Icons_Default.Icon_checkbox_unchecked];
            ShowSystemDevices = newValue;
            RefreshMapDeviceToList();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshMapDeviceToList();
        }

        #endregion

        public UserDevice[] GetSelected()
        {
            var grid = MainDataGrid;
            var items = grid.SelectedItems.Cast<UserDevice>().ToArray();
            return items;
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

        #region ■ Import

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
            // Suspend events.
            grid.ItemsSource = null;
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
            Global.HMan.SetBodyInfo("{0} {1}(s) loaded.", items.Count(), typeof(UserDevice).Name);
            // Resume
            grid.ItemsSource = _currentData;
            //JocysCom.ClassLibrary.Controls.ControlsHelper.RestoreSelection(grid, key, selection);
            SettingsManager.Save();
        }

        #endregion

        private void HardwareButton_Click(object sender, RoutedEventArgs e)
        {
            var form = new HardwareWindow();
            form.Owner = Global._MainWindow;
            form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ControlsHelper.CheckTopMost(form);
            form.ShowDialog();
            form.Owner = null;
        }

        private void AddDemoDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            ControlsHelper.BeginInvoke(() =>
            {
                var ud = TestDeviceHelper.NewUserDevice();
                lock (SettingsManager.UserDevices.SyncRoot)
                    SettingsManager.UserDevices.Items.Add(ud);
                Global.DHelper.DevicesNeedUpdating = true;
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ControlsHelper.AllowLoad(this))
                return;
            // If mapping DInput device to XInput controller.
            if (MapDeviceToControllerMode)
            {
                IsHiddenColumn.Visibility = Visibility.Collapsed;
                IsEnabledColumn.Visibility = Visibility.Collapsed;
            }
            else
            {
                RefreshButton.Visibility = Visibility.Collapsed;
                ShowSystemDevicesButton.Visibility = Visibility.Collapsed;
            }
            _currentData = new ObservableCollectionInvoked<UserDevice>();
            //MainDataGrid.ItemsSource = _currentData;
            SettingsManager.UserDevices.Items.ListChanged += Items_ListChanged;
            RefreshMapDeviceToList();
        }

        List<Guid> gridSelection = new List<Guid>();

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!ControlsHelper.AllowUnload(this))
                return;
            // Moved to MainBodyControl_Unloaded().
        }

        public void ParentWindow_Unloaded()
        {
            gridSelection = ControlsHelper.GetSelection<Guid>(MainDataGrid, nameof(UserDevice.InstanceGuid));
            SettingsManager.UserDevices.Items.ListChanged -= Items_ListChanged;
            MainDataGrid.ItemsSource = null;
            _currentData.Clear();
            _currentData = null;
        }
    }
}
