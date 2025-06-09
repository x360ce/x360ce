using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace x360ce.App.ViGEm
{
	// https://github.com/nefarius/ViGEm/blob/master/NET/HidCerberus.Srv/NancyFx/Modules/HidGuardianNancyModuleV1.cs

	public class HidGuardianHelper
	{

		static string HidGuardianRegistry = @"SYSTEM\CurrentControlSet\Services\HidGuardian";
		static string WhiteList = "Whitelist";

		static string ParametersRegistry => $"{HidGuardianRegistry}\\Parameters";
		static string WhitelistRegistry => $"{ParametersRegistry}\\{WhiteList}";

		static string pattern1 = @"^HID\\VID_[0-9A-F]{4}&PID_[0-9A-F]{4}";
		//static string pattern2 = @"HID\\[{(]?[0-9A-Fa-z]{8}[-]?([0-9A-Fa-z]{4}[-]?){3}[0-9A-Fa-z]{12}[)}]?";

		public static readonly Regex HardwareIdRegex = new Regex(pattern1);

		#region ■ WhiteList


		/// <summary>
		/// Allows application to see all hidden controllers.
		/// </summary>
		public static bool InsertCurrentProcessToWhiteList()
		{
			var id = System.Diagnostics.Process.GetCurrentProcess().Id;
			return InsertToWhiteList(id);
		}

		/// <summary>
		/// Denies application to see all hidden controllers.
		/// </summary>
		public static bool RemoveCurrentProcessFromWhiteList()
		{
			var id = System.Diagnostics.Process.GetCurrentProcess().Id;
			return RemoveFromWhiteList(id);
		}

		/// <summary>
		/// Insert process ID into white list. This will allow for application to see all controllers.
		/// </summary>
		/// <param name="processId">Application process Id</param>
		public static bool InsertToWhiteList(int processId)
		{
			var subKey = Registry.LocalMachine.OpenSubKey(WhitelistRegistry, true);
			if (subKey != null)
			{
				var key = subKey.CreateSubKey(processId.ToString());
				if (key != null)
					key.Close();
				subKey.Close();
			}
			return true;
		}

		/// <summary>
		/// Remove process ID from white list.
		/// </summary>
		/// <param name="processId"></param>
		public static bool RemoveFromWhiteList(int processId)
		{
			Registry.LocalMachine.DeleteSubKey($"{WhitelistRegistry}\\{processId}");
			return true;
		}

		public static int[] SelectFromWhiteList()
		{
			var list = new List<int>();
			var key = Registry.LocalMachine.OpenSubKey(WhitelistRegistry);
			if (key == null)
				return list.ToArray();
			var names = key.GetSubKeyNames();
			foreach (var name in names)
			{
				int id;
				if (int.TryParse(name, out id))
					list.Add(id);
			}
			key.Close();
			return list.ToArray();
		}

		/// <summary>
		/// Remove all process IDs from white list.
		/// </summary>
		public static bool ClearWhiteList(bool keepCurrentProcess, bool keepRunningProcesses)
		{
			var key = Registry.LocalMachine.OpenSubKey(WhitelistRegistry);
			if (key == null)
				return true;
			var keepIds = new List<int>();
			if (keepCurrentProcess)
			{
				var id = System.Diagnostics.Process.GetCurrentProcess().Id;
				keepIds.Add(id);
			}
			if (keepRunningProcesses)
			{
				var ids = System.Diagnostics.Process.GetProcesses().Select(x => x.Id);
				keepIds.AddRange(ids);
			}
			foreach (var subKeyName in key.GetSubKeyNames())
			{
				int processId;
				if (int.TryParse(subKeyName, out processId) && keepIds.Contains(processId))
					continue;
				Registry.LocalMachine.DeleteSubKey($"{WhitelistRegistry}\\{subKeyName}");
			}
			key.Close();
			return true;
		}

		#endregion

		#region ■ Force

		public static bool GetForce()
		{
			var key = Registry.LocalMachine.OpenSubKey(ParametersRegistry);
			if (key == null)
				return false;
			var force = (int)key.GetValue("Force", 0) == 1;
			key.Close();
			return force;
		}

		public static void SetForce(bool enabled)
		{
			var key = Registry.LocalMachine.OpenSubKey(ParametersRegistry);
			if (key == null)
				return;
			key.SetValue("Force", enabled ? 1 : 0);
			key.Close();
		}

		#endregion

		#region ■ Affected

		const string registryKeyName = "AffectedDevices";

		public static bool InsertToAffected(params string[] ids)
		{
			// Return if invalid id found.
			if (ids.Any(x => !HardwareIdRegex.IsMatch(x)))
				return false;
			FixCasing(ids);
			// Get existing IDs.
			var key = Registry.LocalMachine.CreateSubKey(ParametersRegistry);
			var current = (key.GetValue(registryKeyName, new string[0]) as string[]).ToList();
			// Combine arrays.
			current.AddRange(ids);
			// Get unique and sorted list.
			var newList = current
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.OrderBy(x => x)
				.ToArray();
			// Write back to registry.
			key.SetValue(registryKeyName, newList, RegistryValueKind.MultiString);
			key.Close();
			// Reset Devices.
			ResetDevices(ids);
			return true;
		}

		public static bool RemoveFromAffected(params string[] ids)
		{
			// Return if invalid id found.
			if (ids.Any(x => !HardwareIdRegex.IsMatch(x)))
				return false;
			FixCasing(ids);
			// Get existing Hardware IDs.
			var key = Registry.LocalMachine.CreateSubKey(ParametersRegistry);
			var current = (key.GetValue(registryKeyName, new string[0]) as string[]).ToList();
			// Remove values from array.
			current.RemoveAll(x => ids.Contains(x));
			// Get unique and sorted list.
			var newList = current
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.OrderBy(x => x)
				.ToArray();
			// Write back to registry.
			key.SetValue(registryKeyName, newList, RegistryValueKind.MultiString);
			key.Close();
			// Reset Devices.
			ResetDevices(ids);
			return true;
		}

		/// <summary>
		/// Must remove and re-add devices for HidGuardian filters to take effect.
		/// </summary>
		public static void ResetDevices(params string[] deviceIds)
		{
			//var hwIds = ConvertToHidVidPid(deviceIds);
			for (int i = 0; i < deviceIds.Length; i++)
				Program.RunElevated(AdminCommand.UninstallDevice, deviceIds[i]);
		}

		static readonly Regex HardwareIdSimpleRegex = new Regex(@"^HID\\VID_[0-9A-F]{4}&PID_[0-9A-F]{4}", RegexOptions.IgnoreCase);

		/// <summary>
		/// Convert device IDs into HID Hardware Ids ("HID\VID_HHHH&PID_HHHH")
		/// </summary>
		public static string[] ConvertToHidVidPid(params string[] deviceIds)
		{
			var list = new List<string>();
			foreach (var did in deviceIds)
			{
				var matches = HardwareIdSimpleRegex.Matches(did);
				if (matches.Count == 0)
					continue;
				var item = matches[0].Value;
				if (list.Contains(item))
					continue;
				list.Add(item);
			}
			return list.ToArray();
		}



		public static string[] GetAffected()
		{
			var key = Registry.LocalMachine.OpenSubKey(ParametersRegistry, false);
			if (key == null)
				return new string[0];
			var current = key.GetValue(registryKeyName, new string[0]) as string[];
			key.Close();
			return current;
		}

		public static bool ClearAffected()
		{
			var key = Registry.LocalMachine.OpenSubKey(ParametersRegistry, true);
			if (key == null)
				return true;
			key.SetValue(registryKeyName, new string[0], RegistryValueKind.MultiString);
			key.Close();
			return true;
		}

		public static string[] GetEnumeratedDevices()
		{
			var list = new List<string>();
			var key = Registry.LocalMachine.OpenSubKey($"{HidGuardianRegistry}\\Enum");
			if (key == null)
				return list.ToArray();
			var names = key.GetValueNames();
			foreach (var name in names)
			{
				int id;
				if (int.TryParse(name, out id))
				{
					var hwid = key.GetValue(name) as string;
					if (!string.IsNullOrEmpty(hwid))
						list.Add(hwid);
				}
			}
			key.Close();
			return list.ToArray();
		}

		/// <summary>
		/// Make sure that supplied values have same casing as enumerated devices, just in case HidGuardian is case sensitive.
		/// </summary>
		/// <param name="hwid"></param>
		static void FixCasing(string[] hwIds)
		{
			var list = GetEnumeratedDevices();
			for (int i = 0; i < hwIds.Length; i++)
			{
				var hwid = hwIds[i];
				var name = list.FirstOrDefault(x => string.Compare(x, hwid, true) == 0);
				if (!string.IsNullOrEmpty(name))
					hwIds[i] = name;
			}
		}

		#endregion

		#region ■ Helper

		/// <summary>
		/// Check if Users have right to modify programs white list.
		/// </summary>
		public static bool CanModifyParameters(bool fix = false)
		{
			return CanModifyRegistry(ParametersRegistry, RegistryRights.FullControl, fix);
		}

		static bool CanModifyRegistry(string registryName, RegistryRights rights, bool fix = false)
		{
			var subKey = Registry.LocalMachine.OpenSubKey(registryName);
			var canModify = false;
			var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			if (subKey != null)
			{
				// Check if users have right to write in non elevated mode.
				canModify = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(subKey, rights, users, false);
				// If can't modify, but must fix and program is running in elevated mode.
				if (!canModify && fix && JocysCom.ClassLibrary.Win32.WinAPI.IsElevated())
				{
					// Update registry by adding required permissions, which will allow users to modify affected devices in non elevated mode.
					JocysCom.ClassLibrary.Security.PermissionHelper.SetRights(Registry.LocalMachine, registryName, RegistryRights.FullControl, users);
					canModify = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(subKey, rights, users);
				}
				subKey.Close();
			}
			return canModify;
		}

		public static void FixWhiteListRegistryKey()
		{
			var subKey = Registry.LocalMachine.OpenSubKey(WhitelistRegistry, true);
			// If key exists then there is nothing to do.
			if (subKey != null)
			{
				subKey.Close();
				return;
			}
			var key = Registry.LocalMachine.OpenSubKey(ParametersRegistry, true);
			// Parameters must be present after installation of HID Guardian.
			if (key == null)
				return;
			// Create sub key.
			_ = key.CreateSubKey(WhiteList);
			key.Close();
		}

		#endregion

	}

}

