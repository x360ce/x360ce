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
using System.Drawing;
using x360ce.App.DInput;
using System.Threading.Tasks;
using JocysCom.ClassLibrary.Win32;
using JocysCom.ClassLibrary.Collections;

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
			// The device list is the program's and outlives this control. The Map Device window makes
			// one of these each time it opens, and left subscribed, every later device change was
			// handled for each control already gone, on whichever thread had made it.
			Disposed += (sender, e) => SettingsManager.UserDevices.Items.ListChanged -= Items_ListChanged;
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
			DevicesDataGridView.AutoGenerateColumns = false;
			// WORKAROUND: Remove SelectionChanged event.
			DevicesDataGridView.SelectionChanged -= ControllersDataGridView_SelectionChanged;
			_currentData = data;
			// Read the machine again, so a list just opened shows what is true now.
			XInputPlaces.Invalidate();
			DevicesDataGridView.DataSource = _currentData;
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
			_currentData = null;
			SettingsManager.UserDevices.Items.ListChanged -= Items_ListChanged;
			SettingsManager.UserDevices.Items.ListChanged += Items_ListChanged;
			ShowSystemDevicesButton.Visible = MapDeviceToControllerMode;
			RefreshList();
		}

		/// <summary>Brings the list shown in line with the program's devices when a device has come or gone.</summary>
		/// <remarks>
		/// The list shown is this control's own copy, on the Devices page as in the list Add opens: DirectInput devices
		/// first, then Raw Input, then plugin devices, each source in the order its devices were found, and each tinted its
		/// own colour (<see cref="AppHelper.GetInputSourceTint"/>). The program's list stays in the order devices were found,
		/// which is the order they are saved in. Only a device that comes or goes changes the copy, so a sort chosen by a
		/// click on a heading stays.
		/// </remarks>
		void RefreshList()
		{
			// Devices Windows files as system devices are left out of the list Add opens unless asked for: keyboards
			// and the like, which nobody means to map. Some game devices are filed there too, the Logitech G13 among
			// them, and the switch is how they are reached. The Devices page lists them all.
			UserDevice[] devices;
			lock (SettingsManager.UserDevices.SyncRoot)
			{
				devices = SettingsManager.UserDevices.Items
					.Where(x => !MapDeviceToControllerMode || ShowSystemDevicesButton.Checked || x.ConnectionClass != DEVCLASS.SYSTEM).ToArray();
			}
			var ordered = devices.Concat(GetOffers(devices)).OrderBy(x => x.InputSource).ToArray();
			if (_currentData == null)
			{
				var list = new SortableBindingList<UserDevice>();
				list.SynchronizingObject = ControlsHelper.MainTaskScheduler;
				list.AddRange(ordered);
				AttachDataSource(list);
			}
			else if (_currentData.Count != ordered.Length || ordered.Any(x => !_currentData.Contains(x)))
			{
				CollectionsHelper.Synchronize(ordered, _currentData);
			}
		}

		/// <summary>In the list Add opens, a new device of each kind a plugin offers, by its kind's product.</summary>
		/// <remarks>Made the first time it is offered and kept while the list is open, so the row stays the same row.</remarks>
		readonly Dictionary<Guid, UserDevice> _offers = new Dictionary<Guid, UserDevice>();

		/// <summary>In the list Add opens, a new device for each kind a loaded plugin offers that has no row yet; none on the Devices page.</summary>
		/// <remarks>
		/// A plugin device exists only once somebody adds it, so the list of devices to map had nothing to show for a plugin
		/// that had loaded. Offered here, it is mapped as any device is, and gets its row when it is added (<see cref="AddOffered"/>).
		/// A second one of a kind already listed comes from Add Plugin Device.
		/// </remarks>
		UserDevice[] GetOffers(UserDevice[] devices)
		{
			var manager = Plugins.PluginManager.Current;
			if (!MapDeviceToControllerMode || manager == null)
				return new UserDevice[0];
			var offers = new List<UserDevice>();
			foreach (var type in manager.DeviceTypes.Where(x => !x.Faulted))
			{
				if (devices.Any(x => x.ProductGuid == type.ProductGuid))
					continue;
				UserDevice offer;
				if (!_offers.TryGetValue(type.ProductGuid, out offer))
					_offers[type.ProductGuid] = offer = Plugins.PluginDevices.NewUserDevice(type);
				offers.Add(offer);
			}
			return offers.ToArray();
		}

		/// <summary>Gives each offered plugin device among these its row in the program's device list, so it can be mapped.</summary>
		public void AddOffered(IEnumerable<UserDevice> devices)
		{
			foreach (var ud in devices.Where(x => _offers.ContainsValue(x)).ToArray())
				AddPluginDevice(ud);
		}

		private void Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			// If item added or deleted from original list then... Several changes delivered together arrive as a reset.
			if (
				e.ListChangedType == ListChangedType.ItemAdded ||
				e.ListChangedType == ListChangedType.ItemDeleted ||
				e.ListChangedType == ListChangedType.Reset
			)
				// Update list.
				RefreshList();
		}

		/// <summary>Paints the places again, for when a controller has arrived or left.</summary>
		/// <remarks>
		/// The cells are filled while the table paints, so a place that has changed is not shown
		/// until something makes the table paint. Nothing does when a controller is plugged in, so
		/// the list kept saying where things were a moment ago.
		///
		/// The column is measured again as well. It sizes itself to its cells, but only when a cell's
		/// stored value changes, and these cells store nothing: the text is made while painting. So
		/// the column kept the width of the first paint, and a longer answer was cut off.
		/// </remarks>
		public void RefreshPlaces()
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action)RefreshPlaces);
				return;
			}
			DevicesDataGridView.AutoResizeColumn(XInputPlaceColumn.Index);
			DevicesDataGridView.Invalidate();
		}

		private void DevicesDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;

			var grid = (DataGridView)sender;
			// The list can have shrunk under a row the grid is still painting; such a row shows nothing.
			var item = AppHelper.BoundItem<UserDevice>(grid, e.RowIndex);
			if (item is null)
				return;
			var row = grid.Rows[e.RowIndex];
			var column = grid.Columns[e.ColumnIndex];
			e.CellStyle.BackColor = AppHelper.GetInputSourceTint(item);
			if (column == IsOnlineColumn)
			{
				e.Value = AppHelper.GetOnlineIcon(item.IsOnline);
			}
			else if (column == ConnectionClassColumn)
			{
				e.Value = AppHelper.GetConnectionClassIcon(item.ConnectionClass);
			}
			else if (column == XInputPlaceColumn)
			{
				e.Value = AppHelper.GetXInputPlaces(item);
			}
			else if (column == SourceColumn)
			{
				e.Value = AppHelper.GetInputSourceName(item);
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

		private void ShowSystemDevicesButton_CheckedChanged(object sender, EventArgs e)
		{
			ControlsHelper.SetImage(ShowSystemDevicesButton, ShowSystemDevicesButton.Checked
				? Properties.Resources.checkbox_16x16
				: Properties.Resources.checkbox_unchecked_16x16);
			if (MapDeviceToControllerMode)
				RefreshList();
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			DevicesDataGridView.Invalidate();
		}

		private void ControllerDeleteButton_Click(object sender, EventArgs e)
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
			var key = nameof(UserDevice.InstanceGuid);
			var list = SettingsManager.UserDevices.Items;
			var selection = JocysCom.ClassLibrary.Controls.ControlsHelper.GetSelection<Guid>(grid, key);
			var newItems = items.ToArray();
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
			RefreshList();
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

		/// <summary>
		/// Counts the leftovers and, once the person agrees, removes them. Both happen on a worker,
		/// because reading or changing the device tree from the thread that owns this window leaves
		/// Windows waiting on the window and the window waiting on Windows.
		/// </summary>
		private void CleanupVirtualPadsButton_Click(object sender, EventArgs e)
		{
			CleanupVirtualPadsButton.Enabled = false;
			var ui = TaskScheduler.FromCurrentSynchronizationContext();
			Task.Run(() => VirtualDriverInstaller.GetLeftoverVirtualPads().Length).ContinueWith(counted =>
			{
				var count = counted.IsFaulted ? 0 : counted.Result;
				if (count == 0)
				{
					CleanupVirtualPadsButton.Enabled = true;
					MessageBoxForm.Show("No virtual controllers have been left behind.",
						"Remove Leftover Pads", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				// Say what the trouble is before asking, because the number on its own means nothing to
				// somebody who does not know that only four XInput places exist.
				var question = string.Format(
					"{0} virtual controllers from earlier runs are still present.\r\n\r\n" +
					"Windows offers only four XInput places, so these take the places this program needs " +
					"and a controller can appear to move on its own.\r\n\r\n" +
					"Remove them? Real controllers are not touched.", count);
				var answer = MessageBoxForm.Show(question, "Remove Leftover Pads",
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (answer != DialogResult.Yes)
				{
					CleanupVirtualPadsButton.Enabled = true;
					return;
				}
				var succeeded = false;
				Task.Run(() => VirtualDriverInstaller.RemoveLeftoverPadsElevated(count, out succeeded)).ContinueWith(removed =>
				{
					CleanupVirtualPadsButton.Enabled = true;
					var result = removed.IsFaulted ? removed.Exception.GetBaseException().Message : removed.Result;
					MessageBoxForm.Show(result, "Remove Leftover Pads", MessageBoxButtons.OK,
						succeeded && !removed.IsFaulted ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
					Global.DHelper.UpdateDevicesEnabled = true;
				}, ui);
			}, ui);
		}

		/// <summary>Adds a device a plugin offers: at once when one kind is offered, from a list when there are several.</summary>
		private void AddDemoDevice_Click(object sender, EventArgs e)
		{
			var manager = Plugins.PluginManager.Current;
			var types = manager == null ? new Plugins.PluginDeviceType[0] : manager.DeviceTypes.Where(x => !x.Faulted).ToArray();
			if (types.Length == 0)
			{
				MessageBox.Show("No plugin offers a device. The Plugins page on the Options tab lists the plugins found and why any did not load.",
					"Add Plugin Device", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (types.Length == 1)
			{
				AddPluginDevice(Plugins.PluginDevices.NewUserDevice(types[0]));
				return;
			}
			var menu = new ContextMenuStrip();
			foreach (var type in types)
			{
				var item = new ToolStripMenuItem(type.Name) { Tag = type };
				item.Click += (s, a) => AddPluginDevice(Plugins.PluginDevices.NewUserDevice((Plugins.PluginDeviceType)((ToolStripMenuItem)s).Tag));
				menu.Items.Add(item);
			}
			menu.Closed += (s, a) => BeginInvoke((Action)menu.Dispose);
			var button = (ToolStripItem)sender;
			menu.Show(button.Owner, new Point(button.Bounds.Left, button.Bounds.Bottom));
		}

		static void AddPluginDevice(UserDevice ud)
		{
			lock (SettingsManager.UserDevices.SyncRoot)
				SettingsManager.UserDevices.Items.Add(ud);
			Global.DHelper.UpdateDevicesEnabled = true;
		}

		private void DevicesDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			var ud = AppHelper.BoundItem<UserDevice>(grid, e.RowIndex);
			if (ud is null)
				return;
			var column = grid.Columns[e.ColumnIndex];
			// If user clicked on the CheckBox column then...
			if (column == IsEnabledColumn)
			{
				// Changed check (enabled state) of the current item.
				ud.IsEnabled = !ud.IsEnabled;
				// An unticked device is not used by any game, so it is not hidden from one either.
				var game = SettingsManager.CurrentGame;
				if (game != null && SettingsManager.Options.HidGuardianConfigureAutomatically)
					SettingsManager.HideMappedDevices(game, ud.InstanceGuid);
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
						ud.IsHidden = !ud.IsHidden;
						// Use begin invoke which will prevent mouse multi-select rows.
						ControlsHelper.BeginInvoke(() =>
						{
							AppHelper.SynchronizeToHidGuardian(ud.InstanceGuid);
						});
					}
					else
					{
						ShowHidGuardianPermissionDenied();
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
			if (!AppHelper.UnhideAllDevices())
				ShowHidGuardianPermissionDenied();
		}

		private void synchronizeToHidGuardianToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!AppHelper.SynchronizeToHidGuardian())
				ShowHidGuardianPermissionDenied();
		}

		/// <summary>Says why HID Guardian's settings cannot be written: it is not installed, or this account may not write them yet.</summary>
		void ShowHidGuardianPermissionDenied()
		{
			var form = new MessageBoxForm();
			form.StartPosition = FormStartPosition.CenterParent;
			if (!ViGEm.HidGuardianHelper.IsInstalled())
				form.ShowForm(ViGEm.HidGuardianHelper.NotInstalledText, "HID Guardian", MessageBoxButtons.OK, MessageBoxIcon.Information);
			else
				form.ShowForm("Can't modify HID Guardian registry.\r\nPlease run this application as Administrator once in order to fix permissions.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
