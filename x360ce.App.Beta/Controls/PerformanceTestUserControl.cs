using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace x360ce.App.Controls
{
	public partial class PerformanceTestUserControl : UserControl
	{
		public PerformanceTestUserControl()
		{
			InitializeComponent();
			CpuTimer = new System.Timers.Timer();
			CpuTimer.Interval = 1000;
			CpuTimer.AutoReset = false;
			CpuTimer.Elapsed += CpuTimer_Elapsed;
			LoadSettings();
			CheckTimer();
		}

		public void LoadSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.Checked = o.TestEnabled;
			GetDInputStatesCheckBox.Checked = o.TestGetDInputStates;
			SetXInputStatesCheckBox.Checked = o.TestSetXInputStates;
			GetXInputStatesCheckBox.Checked = o.TestGetXInputStates;
			UpdateInterfaceCheckBox.Checked = o.TestUpdateInterface;
			// Attach events.
			EnableCheckBox.CheckedChanged += EnableCheckBox_CheckedChanged;
			GetDInputStatesCheckBox.CheckedChanged += GetDInputStatesCheckBox_CheckedChanged;
			SetXInputStatesCheckBox.CheckedChanged += SetXInputStatesCheckBox_CheckedChanged;
			GetXInputStatesCheckBox.CheckedChanged += GetXInputStatesCheckBox_CheckedChanged;
			UpdateInterfaceCheckBox.CheckedChanged += UpdateInterfaceCheckBox_CheckedChanged;
		}

		private void UpdateInterfaceCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestUpdateInterface = UpdateInterfaceCheckBox.Checked;
		}

		private void GetXInputStatesCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SettingsManager.Options.TestGetXInputStates = GetXInputStatesCheckBox.Checked;
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
				CpuTimer.Start();
		}

		public void SaveSettings()
		{
			var o = SettingsManager.Options;
			EnableCheckBox.Checked = o.TestEnabled;
			o.TestGetDInputStates = GetDInputStatesCheckBox.Checked;
			o.TestSetXInputStates = SetXInputStatesCheckBox.Checked;
			o.TestGetXInputStates = GetXInputStatesCheckBox.Checked;
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
				if (_Counter == null)
				{
					_Counter = new CpuUsage();
				}
				if (_Counter != null)
				{
					var process_cpu_usage = _Counter.GetUsage();
					BeginInvoke((MethodInvoker)delegate ()
					{
						CpuTextBox.Text = string.Format("{0:0.0} %", Math.Round(process_cpu_usage, 1));
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

			float _cpuUsage;
			DateTime _lastRun;
			long _runCount;

			public CpuUsage()
			{
				_cpuUsage = -1f;
				_lastRun = DateTime.MinValue;
				_prevSysUser.dwHighDateTime = _prevSysUser.dwLowDateTime = 0;
				_prevSysKernel.dwHighDateTime = _prevSysKernel.dwLowDateTime = 0;
				_prevProcTotal = TimeSpan.MinValue;
				_runCount = 0;
			}

			public float GetUsage()
			{
				float cpuCopy = _cpuUsage;
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

					_lastRun = DateTime.Now;

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
					TimeSpan sinceLast = DateTime.Now - _lastRun;
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


	}
}
