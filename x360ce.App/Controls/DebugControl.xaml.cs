using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for DebugControl.xaml
	/// </summary>
	public partial class DebugControl : UserControl
	{
		public DebugControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			ProgressBarPanel.UpdateProgress();
			_TestTimer = new HiResTimer(1, "TestTimer");
			_TestTimer.AutoReset = true;
			CpuTimer = new System.Timers.Timer();
			CpuTimer.Interval = 1000;
			CpuTimer.AutoReset = false;
			CpuTimer.Elapsed += CpuTimer_Elapsed;
			LoadSettings();
			CheckTimer();
		}

		public bool IsDesignMode { get { return JocysCom.ClassLibrary.Controls.ControlsHelper.IsDesignMode(this); } }

		public void LoadSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.IsChecked = o.TestEnabled;
			GetDInputStatesCheckBox.IsChecked = o.TestGetDInputStates;
			SetXInputStatesCheckBox.IsChecked = o.TestSetXInputStates;
			UpdateInterfaceCheckBox.IsChecked = o.TestUpdateInterface;
			TestCheckIssuesCheckBox.IsChecked = o.TestCheckIssues;
			// Attach events.
			EnableCheckBox.Checked += EnableCheckBox_CheckedChanged;
			GetDInputStatesCheckBox.Checked += GetDInputStatesCheckBox_CheckedChanged;
			SetXInputStatesCheckBox.Checked += SetXInputStatesCheckBox_CheckedChanged;
			UpdateInterfaceCheckBox.Checked += UpdateInterfaceCheckBox_CheckedChanged;
			TestCheckIssuesCheckBox.Checked += TestCheckIssuesCheckBox_CheckedChanged;
			// Load Settings and enable events.
			UpdateGetXInputStatesWithNoEvents();
		}

		private void Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			var pd = e.PropertyDescriptor;
			if (pd != null)
			{
				// If GetXInputStates property value changed.
				if (pd.Name == nameof(Options.GetXInputStates))
				{
					UpdateGetXInputStatesWithNoEvents();
				}
			}
		}

		public object GetXInputStatesCheckBoxLock = new object();

		public void UpdateGetXInputStatesWithNoEvents()
		{
			lock (GetXInputStatesCheckBoxLock)
			{
				// Disable events.
				GetXInputStatesCheckBox.Checked -= GetXInputStatesCheckBox_CheckedChanged;
				ControlsHelper.SetChecked(GetXInputStatesCheckBox, SettingsManager.Options.GetXInputStates);
				// Enable events.
				GetXInputStatesCheckBox.Checked += GetXInputStatesCheckBox_CheckedChanged;
			}
		}

		// Must trigger only by the user input.
		private void GetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.GetXInputStates = !SettingsManager.Options.GetXInputStates;
		}

		private void UpdateInterfaceCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestUpdateInterface = UpdateInterfaceCheckBox.IsChecked ?? false;
		}

		private void TestCheckIssuesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestCheckIssues = TestCheckIssuesCheckBox.IsChecked ?? false;
		}

		private void SetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestSetXInputStates = SetXInputStatesCheckBox.IsChecked ?? false;
		}

		private void GetDInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestGetDInputStates = GetDInputStatesCheckBox.IsChecked ?? false;
		}

		private void EnableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestEnabled = EnableCheckBox.IsChecked ?? false;
			CheckTimer();
		}

		void CheckTimer()
		{
			if (SettingsManager.Options.TestEnabled && !Program.IsClosing)
			{

				CpuTimer.Start();
			}
			else
			{
				if (!IsHandleCreated)
					return;
				ControlsHelper.BeginInvoke(() =>
				{
					CpuTextBox.Text = "";
				});
			}
		}

		public void SaveSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.IsChecked = o.TestEnabled;
			o.TestGetDInputStates = GetDInputStatesCheckBox.IsChecked ?? false;
			o.TestSetXInputStates = SetXInputStatesCheckBox.IsChecked ?? false;
			o.TestUpdateInterface = UpdateInterfaceCheckBox.IsChecked ?? false;
		}

		#region ■ Performance Counter

		CpuUsage _Counter;

		object counterLock = new object();

		System.Timers.Timer CpuTimer;

		private void CpuTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock (counterLock)
			{
				if (IsHandleCreated)
				{
					if (_Counter == null)
					{
						_Counter = new CpuUsage();
					}
					var process_cpu_usage = _Counter.GetUsage();
					ControlsHelper.BeginInvoke(() =>
					{
						CpuTextBox.Text = process_cpu_usage.HasValue
							? string.Format("{0:0.000} %", Math.Round(process_cpu_usage.Value, 3))
							: "";
					});
				}
				CheckTimer();
			}
		}

		#endregion

		/// <summary>
		/// Class Source.
		///  http://www.philosophicalgeek.com/2009/01/03/determine-cpu-usage-of-current-process-c-and-c/
		/// </summary>
		class CpuUsage
		{

			internal class NativeMethods
			{

				[DllImport("kernel32.dll", SetLastError = true)]
				internal static extern bool GetSystemTimes(
							out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
							out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
							out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime
							);

			}

			System.Runtime.InteropServices.ComTypes.FILETIME _prevSysKernel;
			System.Runtime.InteropServices.ComTypes.FILETIME _prevSysUser;

			TimeSpan _prevProcTotal;

			float? _cpuUsage;
			DateTime _lastRun;
			long _runCount;

			public CpuUsage()
			{
				_lastRun = DateTime.MinValue;
				_prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
				_prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
				_prevProcTotal = TimeSpan.MinValue;
				_runCount = 0;
			}

			public float? GetUsage()
			{
				var cpuCopy = _cpuUsage;
				if (Interlocked.Increment(ref _runCount) == 1)
				{
					if (!EnoughTimePassed)
					{
						Interlocked.Decrement(ref _runCount);
						return cpuCopy;
					}

					System.Runtime.InteropServices.ComTypes.FILETIME sysIdle, sysKernel, sysUser;
					TimeSpan procTime;
					var process = Process.GetCurrentProcess();
					procTime = process.TotalProcessorTime;

					if (!NativeMethods.GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
					{
						Interlocked.Decrement(ref _runCount);
						return cpuCopy;
					}

					if (!IsFirstRun)
					{
						float sysKernelDiff = SubtractTimes(sysKernel, _prevSysKernel);
						float sysUserDiff = SubtractTimes(sysUser, _prevSysUser);
						float sysTotal = sysKernelDiff + sysUserDiff;
						float procTotal = procTime.Ticks - _prevProcTotal.Ticks;
						if (sysTotal > 0f)
						{
							_cpuUsage = (100f * procTotal) / sysTotal;
						}
					}

					_prevProcTotal = procTime;
					_prevSysKernel = sysKernel;
					_prevSysUser = sysUser;

					_lastRun = JocysCom.ClassLibrary.HiResDateTime.Current.Now;

					cpuCopy = _cpuUsage;
				}
				Interlocked.Decrement(ref _runCount);
				return cpuCopy;
			}

			private ulong SubtractTimes(System.Runtime.InteropServices.ComTypes.FILETIME a, System.Runtime.InteropServices.ComTypes.FILETIME b)
			{
				var aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
				var bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;
				return aInt - bInt;
			}

			private bool EnoughTimePassed
			{
				get
				{
					const int minimumElapsedMS = 250;
					TimeSpan sinceLast = JocysCom.ClassLibrary.HiResDateTime.Current.Now - _lastRun;
					return sinceLast.TotalMilliseconds > minimumElapsedMS;
				}
			}

			private bool IsFirstRun
			{
				get
				{
					return (_lastRun == DateTime.MinValue);
				}
			}
		}

		HiResTimer _TestTimer;

		private void TestButton_Click(object sender, EventArgs e)
		{
			_TestTimer.TestFinished += _timer_TestFinished;
			_TestTimer.BeginTest();
		}

		private void _timer_TestFinished(object sender, EventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				MessageBox.Show(_TestTimer.TestResults);
			});
		}

		#region ■ Clean-up/Remove Devices

		private void CleanupDevicesButton_Click(object sender, RoutedEventArgs e)
		{
			LogTextBox.Text = "Please Wait...";
			var ts = new ThreadStart(CheckDevices);
			var t = new Thread(ts);
			t.IsBackground = false;
			t.Start();
		}

		void GetDevices(out DeviceInfo[] offline, out DeviceInfo[] problem, out DeviceInfo[] unknown)
		{
			var devices = DeviceDetector.GetDevices();
			offline = devices.Where(x => !x.IsPresent && x.IsRemovable && !x.Description.Contains("RAS Async Adapter")).ToArray();
			problem = devices.Where(x => x.Status.HasFlag(DeviceNodeStatus.DN_HAS_PROBLEM)).Except(offline).ToArray();
			unknown = devices.Where(x => x.Description.Contains("Unknown")).Except(offline).Except(problem).ToArray();
		}

		void CheckDevices()
		{
			DeviceInfo[] offline;
			DeviceInfo[] problem;
			DeviceInfo[] unknown;
			GetDevices(out offline, out problem, out unknown);
			var list = new List<string>();
			list.Add(string.Format("{0,4} offline devices", offline.Length));
			list.Add(string.Format("{0,4} problem devices", problem.Length));
			list.Add(string.Format("{0,4} unknown devices", unknown.Length));
			if (!IsHandleCreated)
				return;
			ControlsHelper.BeginInvoke(() =>
			{
				LogTextBox.Text = "";
				var result = MessageBoxWindow.Show("Do you want to remove:\r\n\r\n" + string.Join("\r\n", list), "Remove Devices", MessageBoxButton.YesNo);
				if (result != MessageBoxResult.Yes)
					return;
				var ts = new ThreadStart(CleanupDevices);
				var t = new Thread(ts);
				t.IsBackground = false;
				t.Start();
			});
		}

		void CleanupDevices()
		{
			DeviceInfo[] offline;
			DeviceInfo[] problem;
			DeviceInfo[] unknown;
			GetDevices(out offline, out problem, out unknown);
			var list = new List<DeviceInfo>();
			list.AddRange(offline);
			list.AddRange(problem);
			list.AddRange(unknown);
			for (int i = 0; i < list.Count; i++)
			{
				if (!IsHandleCreated)
					return;
				var item = list[i];
				ControlsHelper.BeginInvoke(() =>
				{
					LogTextBox.Text = string.Format("{0}/{1} - {2}", i + 1, list.Count, item.Description);
				});
				DeviceDetector.RemoveDevice(item.DeviceId);
			}
			ControlsHelper.BeginInvoke(() =>
			{
				LogTextBox.Text = string.Format("{0} devices removed", list.Count);
			});

		}

		#endregion

		void ExceptionMethod()
		{
			try
			{
				throw new Exception("Test Exception");
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				throw;
			}
		}

		bool IsHandleCreated
			=> ((HwndSource)PresentationSource.FromVisual(this)) != null;


		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			if (oldParent != null)
			{
				if (_TestTimer != null)
					_TestTimer.Dispose();
			}
			base.OnVisualParentChanged(oldParent);
		}

		private void ThrowExceptionButton_Click(object sender, RoutedEventArgs e)
		{
			ExceptionMethod();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
			// Monitor option changes.
			SettingsManager.OptionsData.Items.ListChanged += Items_ListChanged;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			SettingsManager.OptionsData.Items.ListChanged -= Items_ListChanged;
		}

	}
}
