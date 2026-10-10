using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace x360ce.Tests
{
	/// <summary>The machine's power plan, power source and processor clock while a benchmark ran, to print beside its numbers.</summary>
	/// <remarks>
	/// A laptop on a quiet plan runs at a third of its clock, so the same code takes three times as long. A timing
	/// without the plan and the clock beside it cannot be compared with another, and a budget missed at 1 GHz says
	/// nothing about the code. Make one when the measuring starts; each <see cref="ToString"/> describes the time since
	/// then, or since the description before.
	/// </remarks>
	public sealed class MachinePower : IDisposable
	{
		const string Category = "Processor Information";
		readonly PerformanceCounter _performance;
		readonly PerformanceCounter _busy;

		public MachinePower()
		{
			if (!PerformanceCounterCategory.Exists(Category))
				return;
			_performance = new PerformanceCounter(Category, "% Processor Performance", "_Total", true);
			_busy = new PerformanceCounter(Category, "% Processor Time", "_Total", true);
			// The first value of a rate counter is always zero; it only marks where the interval starts.
			_performance.NextValue();
			_busy.NextValue();
		}

		/// <summary>The clock Windows names for the processor, in MHz.</summary>
		public static int NominalMHz
		{
			get
			{
				using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
					return key == null ? 0 : (int)key.GetValue("~MHz", 0);
			}
		}

		/// <summary>The name of the active power plan, such as Balanced.</summary>
		public static string PlanName
		{
			get
			{
				IntPtr scheme;
				if (PowerGetActiveScheme(IntPtr.Zero, out scheme) != 0)
					return "unknown";
				try
				{
					var guid = (Guid)Marshal.PtrToStructure(scheme, typeof(Guid));
					var buffer = new byte[512];
					var size = (uint)buffer.Length;
					return PowerReadFriendlyName(IntPtr.Zero, ref guid, IntPtr.Zero, IntPtr.Zero, buffer, ref size) == 0
						? Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0')
						: guid.ToString();
				}
				finally
				{
					LocalFree(scheme);
				}
			}
		}

		/// <summary>Plan, power source, average clock and load since the start or the last description, on one line.</summary>
		public override string ToString()
		{
			var source = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online ? "mains" : "battery";
			var clock = "clock unknown";
			if (_performance != null)
			{
				var nominal = NominalMHz;
				var percent = _performance.NextValue();
				clock = string.Format("CPU {0:0} MHz ({1:0}% of {2} MHz), {3:0}% busy", nominal * percent / 100, percent, nominal, _busy.NextValue());
			}
			return string.Format("{0} plan, {1}, {2}", PlanName, source, clock);
		}

		public void Dispose()
		{
			if (_performance == null)
				return;
			_performance.Dispose();
			_busy.Dispose();
		}

		[DllImport("powrprof.dll")]
		static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, out IntPtr activePolicyGuid);

		[DllImport("powrprof.dll")]
		static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, byte[] buffer, ref uint bufferSize);

		[DllImport("kernel32.dll")]
		static extern IntPtr LocalFree(IntPtr memory);
	}
}
