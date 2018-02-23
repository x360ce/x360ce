using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using JocysCom.ClassLibrary;
using System.ComponentModel;

namespace x360ce.App.Controls
{
	public partial class PerformanceTestUserControl : UserControl
	{
		public PerformanceTestUserControl()
		{
			InitializeComponent();
			if (IsDesignMode) return;
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
			EnableCheckBox.Checked = o.TestEnabled;
			GetDInputStatesCheckBox.Checked = o.TestGetDInputStates;
			SetXInputStatesCheckBox.Checked = o.TestSetXInputStates;
			UpdateInterfaceCheckBox.Checked = o.TestUpdateInterface;
			TestCheckIssuesCheckBox.Checked = o.TestCheckIssues;

			// Attach events.
			EnableCheckBox.CheckedChanged += EnableCheckBox_CheckedChanged;
			GetDInputStatesCheckBox.CheckedChanged += GetDInputStatesCheckBox_CheckedChanged;
			SetXInputStatesCheckBox.CheckedChanged += SetXInputStatesCheckBox_CheckedChanged;
			UpdateInterfaceCheckBox.CheckedChanged += UpdateInterfaceCheckBox_CheckedChanged;
			TestCheckIssuesCheckBox.CheckedChanged += TestCheckIssuesCheckBox_CheckedChanged;


			// Load Settings and enable events.
			UpdateGetXInputStatesWithNoEvents();
			// Monitor option changes.
			SettingsManager.OptionsData.Items.ListChanged += Items_ListChanged;
		}

		private void Items_ListChanged(object sender, ListChangedEventArgs e)
		{
			var pd = e.PropertyDescriptor;
			if (pd != null)
			{
				var o = SettingsManager.Options;
				// Update values only if different.
				if (e.PropertyDescriptor.Name == Options.GetName(x => x.GetXInputStates))
				{
					UpdateGetXInputStatesWithNoEvents();
				}
			}
		}

		public void UpdateGetXInputStatesWithNoEvents()
		{
			lock (GetXInputStatesCheckBox)
			{
				// Disable events.
				GetXInputStatesCheckBox.CheckedChanged -= GetXInputStatesCheckBox_CheckedChanged;
				var o = SettingsManager.Options;
				AppHelper.SetChecked(GetXInputStatesCheckBox, o.GetXInputStates);
				// Enable events.
				GetXInputStatesCheckBox.CheckedChanged += GetXInputStatesCheckBox_CheckedChanged;
			}
		}

		// Must trigger only by the user input.
		private void GetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.GetXInputStates = !SettingsManager.Options.GetXInputStates;
		}

		private void UpdateInterfaceCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestUpdateInterface = UpdateInterfaceCheckBox.Checked;
		}

		private void TestCheckIssuesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestCheckIssues = TestCheckIssuesCheckBox.Checked;
		}

		private void SetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestSetXInputStates = SetXInputStatesCheckBox.Checked;
		}

		private void GetDInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestGetDInputStates = GetDInputStatesCheckBox.Checked;
		}

		private void EnableCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestEnabled = EnableCheckBox.Checked;
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
				BeginInvoke((MethodInvoker)delegate ()
				{
					CpuTextBox.Text = "";
				});
			}
		}

		public void SaveSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.Checked = o.TestEnabled;
			o.TestGetDInputStates = GetDInputStatesCheckBox.Checked;
			o.TestSetXInputStates = SetXInputStatesCheckBox.Checked;
			o.TestUpdateInterface = UpdateInterfaceCheckBox.Checked;
		}

		#region Performace Counter

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
					BeginInvoke((MethodInvoker)delegate ()
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
			[DllImport("kernel32.dll", SetLastError = true)]
			static extern bool GetSystemTimes(
						out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
						out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
						out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime
						);

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
					Process process = Process.GetCurrentProcess();
					procTime = process.TotalProcessorTime;

					if (!GetSystemTimes(out sysIdle, out sysKernel, out sysUser))
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

			private UInt64 SubtractTimes(System.Runtime.InteropServices.ComTypes.FILETIME a, System.Runtime.InteropServices.ComTypes.FILETIME b)
			{
				UInt64 aInt = ((UInt64)(a.dwHighDateTime << 32)) | (UInt64)a.dwLowDateTime;
				UInt64 bInt = ((UInt64)(b.dwHighDateTime << 32)) | (UInt64)b.dwLowDateTime;
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

		HiResTimer _timer;

		private void TestButton_Click(object sender, EventArgs e)
		{
			_timer = new HiResTimer();
			_timer.Interval = 1;
			_timer.AutoReset = true;
			_timer.TestFinished += _timer_TestFinished;
			_timer.BeginTest();
		}

		private void _timer_TestFinished(object sender, EventArgs e)
		{
			BeginInvoke((MethodInvoker)delegate ()
			{
				MessageBox.Show(_timer.TestResults);
			});
		}

	}
}
