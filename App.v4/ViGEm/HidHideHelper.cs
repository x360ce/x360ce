using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace x360ce.App.ViGEm
{
	/// <summary>
	/// Helper for Nefarius' HidHide (modern replacement for HidGuardian).
	/// Provides device hiding and process whitelisting to eliminate double-input issues in games.
	/// </summary>
	public static class HidHideHelper
	{
		private const string ServiceKey = @"SYSTEM\CurrentControlSet\Services\HidHide";
		private const string ParametersKey = @"SYSTEM\CurrentControlSet\Services\HidHide\Parameters";
		private const string WhitelistValue = "Whitelist";
		private const string BlacklistValue = "Blacklist";
		private const string ActiveValue = "Active";

		/// <summary>
		/// Returns true if the modern HidHide filter driver service is installed.
		/// </summary>
		public static bool IsInstalled()
		{
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey))
				{
					return key != null;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Checks whether HidHide device hiding is currently active.
		/// </summary>
		public static bool IsActive()
		{
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey))
				{
					if (key == null)
						return false;
					var val = key.GetValue(ActiveValue);
					if (val is int)
						return (int)val == 1;
					return false;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the list of whitelisted application paths.
		/// </summary>
		public static string[] GetWhitelist()
		{
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey))
				{
					if (key == null)
						return new string[0];
					var val = key.GetValue(WhitelistValue) as string[];
					return val ?? new string[0];
				}
			}
			catch
			{
				return new string[0];
			}
		}

		/// <summary>
		/// Gets the list of blacklisted (hidden) device instance paths.
		/// </summary>
		public static string[] GetBlacklist()
		{
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey))
				{
					if (key == null)
						return new string[0];
					var val = key.GetValue(BlacklistValue) as string[];
					return val ?? new string[0];
				}
			}
			catch
			{
				return new string[0];
			}
		}

		/// <summary>
		/// Checks whether an application is whitelisted in HidHide.
		/// </summary>
		public static bool IsAppWhitelisted(string appPath)
		{
			if (string.IsNullOrWhiteSpace(appPath))
				return false;
			var list = GetWhitelist();
			for (int i = 0; i < list.Length; i++)
			{
				if (string.Equals(list[i], appPath, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Whitelists the current process in HidHide so x360ce can see physical controllers even when hidden from games.
		/// </summary>
		public static bool WhitelistCurrentProcess()
		{
			var exePath = Application.ExecutablePath;
			return WhitelistApplication(exePath);
		}

		/// <summary>
		/// Adds an application path to the HidHide whitelist.
		/// </summary>
		public static bool WhitelistApplication(string appPath)
		{
			if (string.IsNullOrWhiteSpace(appPath) || !IsInstalled())
				return false;

			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey, true))
				{
					if (key == null)
						return false;
					var current = (key.GetValue(WhitelistValue) as string[]) ?? new string[0];
					if (current.Any(x => string.Equals(x, appPath, StringComparison.OrdinalIgnoreCase)))
						return true;

					var updated = new List<string>(current) { appPath };
					key.SetValue(WhitelistValue, updated.ToArray(), RegistryValueKind.MultiString);
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Adds a physical controller device instance ID to HidHide's hidden blacklist to prevent double-input.
		/// </summary>
		public static bool HideDevice(string deviceInstanceId)
		{
			if (string.IsNullOrWhiteSpace(deviceInstanceId) || !IsInstalled())
				return false;

			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey, true))
				{
					if (key == null)
						return false;
					var current = (key.GetValue(BlacklistValue) as string[]) ?? new string[0];
					if (current.Any(x => string.Equals(x, deviceInstanceId, StringComparison.OrdinalIgnoreCase)))
						return true;

					var updated = new List<string>(current) { deviceInstanceId };
					key.SetValue(BlacklistValue, updated.ToArray(), RegistryValueKind.MultiString);
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Unhides a physical controller device instance ID in HidHide.
		/// </summary>
		public static bool UnhideDevice(string deviceInstanceId)
		{
			if (string.IsNullOrWhiteSpace(deviceInstanceId) || !IsInstalled())
				return false;

			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(ParametersKey, true))
				{
					if (key == null)
						return false;
					var current = (key.GetValue(BlacklistValue) as string[]) ?? new string[0];
					var updated = current.Where(x => !string.Equals(x, deviceInstanceId, StringComparison.OrdinalIgnoreCase)).ToArray();
					key.SetValue(BlacklistValue, updated, RegistryValueKind.MultiString);
					return true;
				}
			}
			catch
			{
				return false;
			}
		}
	}
}
