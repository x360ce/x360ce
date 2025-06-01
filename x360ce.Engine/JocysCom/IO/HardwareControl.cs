using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JocysCom.ClassLibrary.IO
{
	public partial class HardwareControl : UserControl
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public HardwareControl()
		{
			InitializeComponent();
			ControlsHelper.InitInvokeContext();
		}

		private DeviceDetector detector;

		/// <summary>
		/// In the form load we take an initial hardware inventory,
		/// then hook the notifications so we can respond if any
		/// device is added or removed.
		/// </summary>
		private void HardwareControl_Load(object sender, EventArgs e)
		{
			if (IsDesignMode)
				return;
			ControlsHelper.ApplyBorderStyle(MainToolStrip);
			ControlsHelper.ApplyImageStyle(MainTabControl);
			ControlsHelper.ApplyBorderStyle(DeviceDataGridView);
			UpdateButtons();
			detector = new DeviceDetector(false);
			detector.DeviceChanged += Detector_DeviceChanged;
			RefreshHardwareList();
		}

		/// <summary>
		/// DeviceDetector event handler: triggers a list refresh and logs details if viewing the Logs tab.
		/// </summary>
		private void Detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			RefreshHardwareList();
			if (MainTabControl.SelectedTab != LogsTabPage)
				return;
			AddLogLine("{0}: {1} {2}", e.ChangeType, e.DeviceType, e.DeviceInfo);
		}

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

		/// <summary>
		/// Handles DBT_DEVNODES_CHANGED notifications by refreshing the hardware list.
		/// </summary>
		private void detector_DeviceChanged(object sender, DeviceDetectorEventArgs e)
		{
			if (e.ChangeType == Win32.DBT.DBT_DEVNODES_CHANGED)
			{
				RefreshHardwareList();
			}
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				// Whenever the form closes we need to unregister the
				// hardware notifier.  Failure to do so could cause
				// the system not to release some resources.  Calling
				// this method if you are not currently hooking the
				// hardware events has no ill effects so better to be
				// safe than sorry.
				if (detector != null)
				{
					detector.Dispose();
					detector = null;
				}
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void EnableCurrentDevice(bool enable)
		{
			var row = DeviceDataGridView.SelectedRows.Cast<DataGridViewRow>().First();
			if (row != null)
			{
				var device = (DeviceInfo)row.DataBoundItem;
				DeviceDetector.SetDeviceState(device.DeviceId, enable);
				UpdateButtons();
			}
		}

		/// <summary>
		/// Enables or disables toolbar buttons based on the current device selection and its state.
		/// </summary>
		private void UpdateButtons()
		{
			DeviceInfo di = null;
			var devices = false;
			if (MainTabControl.SelectedTab == DeviceTreeTabPage)
			{
				di = (DeviceInfo)DevicesTreeView.SelectedNode?.Tag;
				devices = true;
			}
			if (MainTabControl.SelectedTab == DeviceListTabPage)
			{
				di = (DeviceInfo)DeviceDataGridView.SelectedRows
					.Cast<DataGridViewRow>()
					.FirstOrDefault()?.DataBoundItem;
				devices = true;
			}
			bool? isDisabled = null;
			if (di != null)
			{
				var value = DeviceDetector.IsDeviceDisabled(di.DeviceId);
				isDisabled = value.HasValue && value.Value;
			}
			EnableButton.Enabled = isDisabled.HasValue && isDisabled.Value;
			DisableButton.Enabled = isDisabled.HasValue && !isDisabled.Value;
			// Update buttons.
			RemoveButton.Enabled = di != null && di.IsRemovable;
			CleanButton.Enabled = devices;
		}

		private void DeviceDataGridView_SelectionChanged(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		private void DeviceDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var item = (DeviceInfo)DeviceDataGridView.Rows[e.RowIndex].DataBoundItem;
			if (item != null)
				e.CellStyle.ForeColor = GetForeColor(item);
		}

		private void ScanButton_Click(object sender, EventArgs e)
		{
			DeviceDetector.ScanForHardwareChanges();
		}

		private void FilterTextBox_TextChanged(object sender, EventArgs e)
		{
			RefreshFilterTimer();
		}

		#region Refresh Timer

		private readonly object RefreshTimerLock = new object();
		private System.Timers.Timer RefreshTimer;

		/// <summary>
		/// Schedules a hardware list refresh after a short delay (520ms) to debounce rapid device change events.
		/// </summary>
		private void RefreshHardwareList()
		{
			lock (RefreshTimerLock)
			{
				if (RefreshTimer is null)
				{
					RefreshTimer = new System.Timers.Timer
					{
						SynchronizingObject = this,
						AutoReset = false,
						Interval = 520
					};
					RefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(_RefreshTimer_Elapsed);
				}
			}
			RefreshTimer.Stop();
			RefreshTimer.Start();
		}

		private List<DeviceInfo> interfaces = new List<DeviceInfo>();
		private List<DeviceInfo> devices = new List<DeviceInfo>();
		private List<DeviceInfo> allDevices = new List<DeviceInfo>();

		/// <summary>
		/// Timer elapsed handler that triggers a full device list and tree update.
		/// </summary>
		private void _RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateListAndTree(true);
		}

		private readonly object updateGridLock = new object();

		/// <summary>
		/// Refreshes UI by optionally re-scanning hardware, logging changes, and applying the current filter.
		/// </summary>
		/// <remarks>
		/// When updateDevices is true, re-enumerates devices and interfaces and logs additions/removals.
		/// Then filters and binds the resulting list to the grid and tree view.
		/// </remarks>
		private void UpdateListAndTree(bool updateDevices)
		{
			lock (updateGridLock)
			{
				if (updateDevices)
				{
					var newDevices = DeviceDetector.GetDevices().ToList();
					var newInterfaces = DeviceDetector.GetInterfaces().ToList();
					if (devices.Count > 0)
					{
						var addedDevices = newDevices.Where(n => !devices.Any(o => o.DeviceId == n.DeviceId)).ToList();
						var removedDevices = devices.Where(n => !newDevices.Any(o => o.DeviceId == n.DeviceId)).ToList();
						AddLog("Added", addedDevices);
						AddLog("Removed", removedDevices);
					}
					if (interfaces.Count > 0)
					{
						var addedInterfaces = newInterfaces.Where(n => !interfaces.Any(o => o.DeviceId == n.DeviceId)).ToList();
						var removedInterfaces = interfaces.Where(n => !newInterfaces.Any(o => o.DeviceId == n.DeviceId)).ToList();
						AddLog("Added", addedInterfaces);
						AddLog("Removed", removedInterfaces);
					}
					// Store new lists.
					devices = newDevices;
					interfaces = newInterfaces;
					// Note: 'devices' and 'interfaces' share the same DeviceId.
					// Don't just select by DeviceID from 'devices'.
					allDevices = newDevices;
					allDevices.AddRange(newInterfaces);
				}
				var filter = FilterStripTextBox.Text.Trim();
				var filtered = JocysCom.ClassLibrary.Data.Linq.ApplySearch(allDevices, filter, (x) =>
				{
					return string.Join(" ",
					x.ClassDescription,
					x.Description,
					x.Manufacturer,
					x.DeviceId);
				}).ToList();
				BindDeviceList(filtered);
				BindDeviceTree(filtered);
			}
		}

		private void AddLog(string prefix, IEnumerable<DeviceInfo> list)
		{
			foreach (var item in list)
			{
				AddLogLine("{0}:", prefix);
				AddLogLine("{0}: {1}", nameof(item.ClassDescription), item.ClassDescription);
				AddLogLine("{0}: {1}", nameof(item.Manufacturer), item.Manufacturer);
				AddLogLine("{0}: {1}", nameof(item.Description), item.Description);
				AddLogLine("{0}: {1}", nameof(item.DeviceId), item.DeviceId);
				AddLogLine("");
			}
		}

		/// <summary>
		/// Binds the filtered device list to the DataGridView with a workaround to prevent redundant selection events.
		/// </summary>
		/// <remarks>
		/// Temporarily detaches SelectionChanged, sets DataSource, then re-attaches it asynchronously.
		/// </remarks>
		private void BindDeviceList(List<DeviceInfo> filtered)
		{
			// WORKAROUND: Remove SelectionChanged event.
			DeviceDataGridView.SelectionChanged -= DeviceDataGridView_SelectionChanged;
			DeviceDataGridView.DataSource = filtered;
			// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
			ControlsHelper.BeginInvoke(() =>
			{
				DeviceDataGridView.SelectionChanged += DeviceDataGridView_SelectionChanged;
				DeviceDataGridView_SelectionChanged(DeviceDataGridView, new EventArgs());
			});
			DeviceListTabPage.Text = string.Format("Device List [{0}]", filtered.Count);
		}

		/// <summary>
		/// Binds the filtered devices to the TreeView, including parent hierarchy and class icons.
		/// </summary>
		/// <remarks>
		/// Clears existing nodes and icons, loads class icons, adds TreeNode entries recursively, and expands all.
		/// </remarks>
		private void BindDeviceTree(List<DeviceInfo> filtered)
		{
			var filteredWithParents = new List<DeviceInfo>();
			foreach (var item in filtered)
				DeviceDetector.FillParents(item, allDevices, filteredWithParents);
			// Fill icons.
			var classes = filteredWithParents.Select(x => x.ClassGuid).Distinct();
			// Suppress repainting the TreeView until all the objects have been created.
			DevicesTreeView.Nodes.Clear();
			TreeImageList.Images.Clear();
			foreach (var cl in classes)
			{
				var icon = DeviceDetector.GetClassIcon(cl, 16);
				if (icon != null)
					TreeImageList.Images.Add(cl.ToString(), icon.ToBitmap());
			}
			DevicesTreeView.BeginUpdate();
			// Get top devices with no parent (only one device).
			var topNodes = filteredWithParents.Where(x => string.IsNullOrEmpty(x.ParentDeviceId)).ToArray();
			AddChildNodes(DevicesTreeView.Nodes, topNodes, filteredWithParents, System.Environment.MachineName);
			DevicesTreeView.EndUpdate();
			DevicesTreeView.ExpandAll();
			DeviceTreeTabPage.Text = string.Format("Device Tree [{0}]", filteredWithParents.Count);
		}

		/// <summary>
		/// Determines the text color for a device row: dark red for hidden, default for present, or gray for absent devices.
		/// </summary>
		Color GetForeColor(DeviceInfo di)
		{
			return di.IsHidden
					? Color.DarkRed
					: di.IsPresent
						? ForeColor
						: SystemColors.ControlDarkDark;
		}

		/// <summary>
		/// Recursively adds tree nodes for DeviceInfo items under the specified collection.
		/// </summary>
		/// <remarks>
		/// 'overrideName' is used for root nodes (e.g., machine name); child nodes use the device description.
		/// </remarks>
		void AddChildNodes(TreeNodeCollection nodes, DeviceInfo[] dis, List<DeviceInfo> allDevices, string overrideName = null)
		{
			foreach (var di in dis)
			{
				var tn = new TreeNode()
				{
					Tag = di,
					Text = overrideName ?? di.Description,
					ImageKey = di.ClassGuid.ToString(),
					SelectedImageKey = di.ClassGuid.ToString(),
					ForeColor = GetForeColor(di),
				};
				nodes.Add(tn);
				var dis2 = allDevices
					.Where(x => x.ParentDeviceId == di.DeviceId)
					//.Where(x => x.IsPresent)
					.OrderBy(x => x.Description).ToArray();
				AddChildNodes(tn.Nodes, dis2, allDevices);
			}
		}

		#endregion

		#region Filter Timer

		private System.Timers.Timer FilterTimer;
		private readonly object FilterTimerLock = new object();

		/// <summary>
		/// Schedules a filter update after a short delay (520ms) to debounce text input.
		/// </summary>
		private void RefreshFilterTimer()
		{
			lock (FilterTimerLock)
			{
				if (FilterTimer is null)
				{
					FilterTimer = new System.Timers.Timer
					{
						AutoReset = false,
						Interval = 520,
						SynchronizingObject = this
					};
					FilterTimer.Elapsed += FilterTimer_Elapsed;
				}
			}
			FilterTimer.Stop();
			FilterTimer.Start();
		}

		/// <summary>
		/// Timer elapsed handler that applies the current filter without re-scanning hardware.
		/// </summary>
		private void FilterTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateListAndTree(false);
		}

		#endregion endregion

		private void EnableFilderCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateListAndTree(false);
		}

		private void DevicesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var di = (DeviceInfo)e.Node.Tag;
			ClassDescriptionTextBox.Text = di.ClassDescription;
			ClassGuidTextBox.Text = di.ClassGuid.ToString();
			VendorIdTextBox.Text = "0x" + di.VendorId.ToString("X4");
			RevisionTextBox.Text = "0x" + di.Revision.ToString("X4");
			ProductIdTextBox.Text = "0x" + di.ProductId.ToString("X4");
			DescriptionTextBox.Text = di.Description;
			ManufacturerTextBox.Text = di.Manufacturer;
			DevicePathTextBox.Text = di.DevicePath;
			DeviceIdTextBox.Text = di.DeviceId;
			DeviceStatusTextBox.Text = di.Status.ToString();
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			RefreshHardwareList();
		}

		#region Clear

		/// <summary>
		/// Enumerates offline, problem, and unknown devices, logs findings, and optionally removes them upon confirmation.
		/// </summary>
		/// <remarks>
		/// Runs on a background thread and marshals UI updates via the main task scheduler and Invoke.
		/// </remarks>
		private async Task CheckAndClean(bool clean)
		{
			LogTextBox.Clear();
			MainTabControl.SelectedTab = LogsTabPage;
			var cancellationToken = new CancellationToken(false);
			var so = ControlsHelper.MainTaskScheduler;
			await Task.Factory.StartNew(() =>
			  {
				AddLogLine("Enumerating Devices...");
				var devices = DeviceDetector.GetDevices();
				var offline = devices.Where(x => !x.IsPresent && x.IsRemovable && !x.Description.Contains("RAS Async Adapter")).ToArray();
				var problem = devices.Where(x => x.Status.HasFlag(DeviceNodeStatus.DN_HAS_PROBLEM)).Except(offline).ToArray();
				var unknown = devices.Where(x => x.Description.Contains("Unknown")).Except(offline).Except(problem).ToArray();
				var list = new List<string>();
				if (offline.Length > 0)
					list.Add(string.Format("{0} offline devices.", offline.Length));
				if (problem.Length > 0)
					list.Add(string.Format("{0} problem devices.", problem.Length));
				if (unknown.Length > 0)
					list.Add(string.Format("{0} unknown devices.", unknown.Length));
				var message = string.Join("\r\n", list);
				if (list.Count == 0)
				{
					AddLogLine("No offline, problem or unknown devices found.");
				}
				else if (clean)
				{
					foreach (var item in list)
						AddLogLine(item);
					var result = System.Windows.MessageBoxResult.No;
					ControlsHelper.Invoke(new Action(() =>
					{
						result = System.Windows.MessageBox.Show(
							"Do you want to remove offline, problem or unknown devices?\r\n\r\n" + message,
							"Do you want to remove devices?",
							System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

					}));
					if (result != System.Windows.MessageBoxResult.Yes)
						return;
					var devList = new List<DeviceInfo>();
					devList.AddRange(offline);
					devList.AddRange(problem);
					devList.AddRange(unknown);
					for (var i = 0; i < devList.Count; i++)
					{
						var item = devList[i];
						AddLogLine("Removing Device: {0}/{1} - {2}", i + 1, devList.Count, item.Description);
						try
						{
							var exception = DeviceDetector.RemoveDevice(item.DeviceId);
							if (exception != null)
								AddLogLine(exception.Message);
							//System.Windows.Forms.Application.DoEvents();
						}
						catch (Exception ex)
						{
							AddLogLine(ex.Message);
						}
					}
				}
				AddLogLine("Done");
			  }, CancellationToken.None, TaskCreationOptions.LongRunning, so).ConfigureAwait(true);
		}

		private void AddLogLine(string format, params object[] args) =>
			AddLog(format + "\r\n", args);

		/// <summary>
		/// Appends formatted log text to LogTextBox, trimming oldest entries to respect MaxLength.
		/// </summary>
		/// <remarks>
		/// Locks the TextBox, removes full lines if overflow, appends new text, and scrolls to the end.
		/// </remarks>
		private void AddLog(string format, params object[] args)
		{
			ControlsHelper.Invoke(new Action(() =>
			{
				var box = LogTextBox;
				lock (box)
				{
					var nl = Environment.NewLine;
					var oldText = box.Text;
					var addText = string.Format(format, args);
					// Get size which will go over the maximum.
					var delSize = oldText.Length + addText.Length - box.MaxLength;
					// If must remove then...
					if (delSize > 0)
					{
						// Try to remove with next new line.
						var index = oldText.IndexOf(nl, delSize);
						if (index > 0)
							delSize = index + nl.Length;
						box.Select(0, delSize);
						box.SelectedText = string.Empty;
					}
					// Append new text.
					box.Select(box.TextLength + 1, 0);
					box.SelectedText = addText;
					if (box.Visible)
					{
						box.SelectionStart = box.TextLength;
						box.ScrollToCaret();
					}
				}
			}));
		}

		#endregion

		#region Device commands

		private void RemoveButton_Click(object sender, EventArgs e)
		{
			if (!IsElevated())
				return;
			var row = DeviceDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row != null)
			{
				var device = (DeviceInfo)row.DataBoundItem;
				if (device.IsRemovable)
					DeviceDetector.RemoveDevice(device.DeviceId);
			}
		}

		private void EnableButton_Click(object sender, EventArgs e)
		{
			if (!IsElevated())
				return;
			EnableCurrentDevice(true);
		}

		private void DisableButton_Click(object sender, EventArgs e)
		{
			if (!IsElevated())
				return;
			EnableCurrentDevice(false);
		}

		private async void CleanButton_Click(object sender, EventArgs e)
		{
			if (!IsElevated())
				return;
			await CheckAndClean(true).ConfigureAwait(true);
			RefreshHardwareList();
		}

		/// <summary>
		/// Checks if the process is running with elevated permissions; shows a warning if not.
		/// </summary>
		static bool IsElevated()
		{
			var isElevated = JocysCom.ClassLibrary.Security.PermissionHelper.IsElevated;
			if (!isElevated)

				System.Windows.MessageBox.Show("You must run this program as administrator for this feature to work.");
			return isElevated;
		}

		#endregion

		/// <summary>
		/// Updates the Logs tab title to reflect the current log length.
		/// </summary>
		private void LogTextBox_TextChanged(object sender, EventArgs e)
		{
			LogsTabPage.Text = string.Format("Logs [{0}]", LogTextBox.TextLength);
		}
	}
}