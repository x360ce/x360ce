using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.Data;
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
			RefreshHardwareList();
		}

		internal bool IsDesignMode => JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this);

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

		/// <summary>
		/// We are using this button to demonstrate enabling a
		/// hardware device.  There are several things worth
		/// noting.  First, just to be safe we are disabling
		/// hardware notifications until we are done.  The UI
		/// thread in .NET won't let the WndProc method run
		/// to my knowledge while you are in here but if you 
		/// were invoking these methods on different callers it
		/// would be worthwhile to disable the notifications
		/// during.  The call to SetDeviceState is designed 
		/// to allow you to pass in multiple devices in an
		/// array to disable, even though we are currently just
		/// doing the selected one.  Also the search is a
		/// sub-string search so be careful not to use something
		/// so generic that it will affect more devices than
		/// the one(s) you intended.  See the notes for the
		/// SetDeviceState method in the library for some
		/// important info.
		/// </summary>
		private void EnableButton_Click(object sender, EventArgs e)
		{
			EnableCurrentDevice(true);
		}

		/// <summary>
		/// We are using this button to disable a device.
		/// See remarks above.
		/// </summary>
		private void DisableButton_Click(object sender, EventArgs e)
		{
			EnableCurrentDevice(false);
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

		private void RemoveButton_Click(object sender, EventArgs e)
		{
			var row = DeviceDataGridView.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
			if (row != null)
			{
				var device = (DeviceInfo)row.DataBoundItem;
				if (device.IsRemovable)
					DeviceDetector.RemoveDevice(device.DeviceId);
			}
		}

		private void DeviceDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
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

		private void RefreshHardwareList()
		{
			lock (RefreshTimerLock)
			{
				if (RefreshTimer == null)
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

		private List<DeviceInfo> devices = new List<DeviceInfo>();
		private List<DeviceInfo> interfaces = new List<DeviceInfo>();

		private void _RefreshTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateGrid(true);
		}

		private readonly object updateGridLock = new object();

		private void UpdateGrid(bool updateDevices)
		{
			lock (updateGridLock)
			{
				if (updateDevices)
				{
					devices = DeviceDetector.GetDevices().ToList();
					interfaces = DeviceDetector.GetInterfaces().ToList();
					devices.AddRange(interfaces);
				}
				var filter = FilterStripTextBox.Text.Trim();
				var view = devices;
				if (!string.IsNullOrEmpty(filter))
				{
					view = devices.Where(x =>
						comp(x.ClassDescription, filter) ||
						comp(x.Description, filter) ||
						comp(x.Manufacturer, filter) ||
						comp(x.DeviceId, filter))
						.ToList();
				}
				// WORKAROUND: Remove SelectionChanged event.
				DeviceDataGridView.SelectionChanged -= DeviceDataGridView_SelectionChanged;
				DeviceDataGridView.DataSource = view;
				// WORKAROUND: Use BeginInvoke to prevent SelectionChanged firing multiple times.
				ControlsHelper.BeginInvoke(() =>
				{
					DeviceDataGridView.SelectionChanged += DeviceDataGridView_SelectionChanged;
					DeviceDataGridView_SelectionChanged(DeviceDataGridView, new EventArgs());
				});
				DeviceListTabPage.Text = string.Format("Device List [{0}]", view.Count);
				BindDeviceTree();
			}
		}

		private void BindDeviceTree()
		{
			var classes = devices.Select(x => x.ClassGuid).Distinct();
			// Suppress repainting the TreeView until all the objects have been created.
			DevicesTreeView.Nodes.Clear();
			TreeImageList.Images.Clear();
			foreach (var cl in classes)
			{
				var icon = DeviceDetector.GetClassIcon(cl);
				if (icon != null)
				{
					var img = new Icon(icon, 16, 16).ToBitmap();
					TreeImageList.Images.Add(cl.ToString(), img);
				}
			}
			DevicesTreeView.BeginUpdate();
			// Get top devices with no parent (only one device).
			var dis = devices.Where(x => string.IsNullOrEmpty(x.ParentDeviceId)).ToArray();
			AddChildNodes(DevicesTreeView.Nodes, dis, System.Environment.MachineName);
			DevicesTreeView.EndUpdate();
			DevicesTreeView.ExpandAll();
		}

		Color GetForeColor(DeviceInfo di)
		{
			return di.IsHidden
					? Color.DarkRed
					: di.IsPresent
						? ForeColor
						: SystemColors.ControlDarkDark;
		}

		void AddChildNodes(TreeNodeCollection nodes, DeviceInfo[] dis, string overrideName = null)
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
				var dis2 = devices
					.Where(x => x.ParentDeviceId == di.DeviceId)
					//.Where(x => x.IsPresent)
					.OrderBy(x => x.Description).ToArray();
				AddChildNodes(tn.Nodes, dis2);
			}
		}

		private bool comp(string source, string value)
		{
			return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		#endregion

		#region Filter Timer

		private System.Timers.Timer FilterTimer;
		private readonly object FilterTimerLock = new object();

		private void RefreshFilterTimer()
		{
			lock (FilterTimerLock)
			{
				if (FilterTimer == null)
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

		private void FilterTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateGrid(false);
		}

		#endregion endregion

		private void EnableFilderCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateGrid(false);
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

		private async Task CheckAncClean(bool clean)
		{
			LogTextBox.Clear();
			MainTabControl.SelectedTab = LogsTabPage;
			var cancellationToken = new CancellationToken(false);
			var so = ControlsHelper.MainTaskScheduler;
			var unused = Task.Factory.StartNew(() =>
			  {
				  AddLog("Enumerating Devices...");
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
					  AddLog("No offline, problem or unknown devices found.");
				  }
				  else if (clean)
				  {
					  foreach (var item in list)
						  AddLog(item);
					  var result = DialogResult.No;
					  ControlsHelper.Invoke(new Action(() =>
					  {
						  var form = new JocysCom.ClassLibrary.Controls.MessageBoxForm
						  {
							  StartPosition = FormStartPosition.CenterParent
						  };
						  ControlsHelper.CheckTopMost(form);
						  result = form.ShowForm(
								  "Do you want to remove offline, problem or unknown devices?\r\n\r\n" + message,
								  "Do you want to remove devices?",
								  MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
						  form.Dispose();

					  }));
					  if (result != DialogResult.Yes)
						  return;
					  var devList = new List<DeviceInfo>();
					  devList.AddRange(offline);
					  devList.AddRange(problem);
					  devList.AddRange(unknown);
					  for (var i = 0; i < devList.Count; i++)
					  {
						  var item = devList[i];
						  AddLog("Removing Device: {0}/{1} - {2}", i + 1, list.Count, item.Description);
						  try
						  {
							  var exception = DeviceDetector.RemoveDevice(item.DeviceId);
							  if (exception != null)
								  AddLog(exception.Message);
							  //System.Windows.Forms.Application.DoEvents();
						  }
						  catch (Exception ex)
						  {
							  AddLog(ex.Message);
						  }
					  }
				  }
				  AddLog("Done");
			  }, CancellationToken.None, TaskCreationOptions.LongRunning, so).ConfigureAwait(true);
		}

		private void AddLog(string format, params object[] args)
		{
			ControlsHelper.Invoke(new Action(() =>
			{
				//LogTextBox.AddLog(format, args);
				LogTextBox.AppendText(string.Format(format + "\r\n", args));
			}));
		}

		#endregion

		private async void CleanButton_Click(object sender, EventArgs e)
		{
			await CheckAncClean(true).ConfigureAwait(true);
			RefreshHardwareList();
		}
	}
}
