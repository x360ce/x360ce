using Microsoft.Win32;
using System;
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
        static string HidGuardianRegistryKeyBase => @"SYSTEM\CurrentControlSet\Services\HidGuardian\Parameters";

        static readonly IEnumerable<object> ResponseOk = new[] { "OK" };
        static readonly string[] HardwareIdSplitters = { "\r\n", "\n" };

        static readonly Regex HardwareIdRegex = new Regex(@"HID\\[{(]?[0-9A-Fa-z]{8}[-]?([0-9A-Fa-z]{4}[-]?){3}[0-9A-Fa-z]{12}[)}]?|HID\\VID_[a-zA-Z0-9]{4}&PID_[a-zA-Z0-9]{4}");

        #region WhiteList

        static string HidWhitelistRegistryKeyBase => $"{HidGuardianRegistryKeyBase}\\Whitelist";

        /// <summary>
        /// Allows application to see all hidden controllers.
        /// </summary>
        public static bool AddCurrentProcessToWhiteList()
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
            return InsertToWhiteList(id);
        }

        /// <summary>
        /// Insert process ID into white list. This will allow for application to see all controllers.
        /// </summary>
        /// <param name="processId">Application process Id</param>
        public static bool InsertToWhiteList(int processId)
        {
            var subKey = Registry.LocalMachine.OpenSubKey(HidWhitelistRegistryKeyBase, true);
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
            Registry.LocalMachine.DeleteSubKey($"{HidWhitelistRegistryKeyBase}\\{processId}");
            return true;
        }

        public static int[] SelectFromWhiteList()
        {
            var list = new List<int>();
            var key = Registry.LocalMachine.OpenSubKey(HidWhitelistRegistryKeyBase);
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
        /// Remove all process IDs from whitelist.
        /// </summary>
        public static bool ClearWhiteList(bool keepCurrentProcess, bool keepRunningProcesses)
        {
            var key = Registry.LocalMachine.OpenSubKey(HidWhitelistRegistryKeyBase);
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
                Registry.LocalMachine.DeleteSubKey($"{HidWhitelistRegistryKeyBase}\\{subKeyName}");
            }
            key.Close();
            return true;
        }

        #endregion

        #region Force

        public static bool GetForce()
        {
            var key = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase);
            if (key == null)
                return false;
            var force = (int)key.GetValue("Force", 0) == 1;
            key.Close();
            return force;
        }

        public static void SetForce(bool enabled)
        {
            var key = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase);
            if (key == null)
                return;
            key.SetValue("Force", enabled ? 1 : 0);
            key.Close();
        }

        #endregion

        #region Affected

        public static bool InsertToAffected(params string[] hwIds)
        {
            // Return if invalid id found.
            if (hwIds.Any(i => !HardwareIdRegex.IsMatch(i)))
                return false;
            // Get existing Hardware IDs.
            var key = Registry.LocalMachine.CreateSubKey(HidGuardianRegistryKeyBase);
            var current = (key.GetValue("AffectedDevices", new string[0]) as string[]).ToList();
            // Combine arrays.
            current.AddRange(hwIds);
            // Get unique and sorted list.
            var newList = current
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            // Write back to registry.
            key.SetValue("AffectedDevices", newList, RegistryValueKind.MultiString);
            key.Close();
            return true;
        }

        public static bool RemoveFromAffected(params string[] hwIds)
        {
            // Return if invalid id found.
            if (hwIds.Any(i => !HardwareIdRegex.IsMatch(i)))
                return false;
            // Get existing Hardware IDs.
            var key = Registry.LocalMachine.CreateSubKey(HidGuardianRegistryKeyBase);
            var current = (key.GetValue("AffectedDevices", new string[0]) as string[]).ToList();
            // Remove values from array.
            current.RemoveAll(x => hwIds.Contains(x));
            // Get unique and sorted list.
            var newList = current
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            // Write back to registry.
            key.SetValue("AffectedDevices", newList, RegistryValueKind.MultiString);
            key.Close();
            return true;
        }

        public static string[] GetAffected()
        {
            var key = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase, false);
            if (key == null)
                return new string[0];
            var current = key.GetValue("AffectedDevices", new string[0]) as string[];
            key.Close();
            return current;
        }

        public static bool ClearAffected()
        {
            var key = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase, true);
            if (key == null)
                return true;
            key.SetValue("AffectedDevices", new string[0], RegistryValueKind.MultiString);
            key.Close();
            return true;
        }

        #endregion

        #region Helper

        /// <summary>
        /// Check if Users have right to modify programs white list.
        /// </summary>
        public static bool CanModifyWhiteList(bool fix = false)
        {
            return CanModifyRegistry(HidWhitelistRegistryKeyBase, RegistryRights.FullControl, fix); // RegistryRights.CreateSubKey | RegistryRights.Delete
        }


        /// <summary>
        /// Check if Users have right to modify hidden devices.
        /// </summary>
        public static bool CanModifyAffectedDevices(bool fix = false)
        {
            return CanModifyRegistry(HidGuardianRegistryKeyBase, RegistryRights.FullControl, fix); // RegistryRights.SetValue
        }

        static bool CanModifyRegistry(string registryName, RegistryRights rights, bool fix = false)
        {
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var subKey = Registry.LocalMachine.OpenSubKey(registryName);
            //if (subKey != null)
            //{
            //    var usersRights = JocysCom.ClassLibrary.Security.PermissionHelper.GetRights(subKey, users);
            //    if (usersRights.HasValue)
            //    {
            //        var values = (RegistryRights[])Enum.GetValues(typeof(RegistryRights));
            //        var found = values.Where(x => usersRights.Value.HasFlag(x)).ToArray();
            //    }
            //}
            if (!JocysCom.ClassLibrary.Win32.WinAPI.IsElevated())
                return false;
            var canModify = false;
            if (subKey != null)
            {
                // Check if users have right to write.
                canModify = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(subKey, users, rights);
                subKey.Close();
            }
            if (fix && !canModify && JocysCom.ClassLibrary.Win32.WinAPI.IsElevated())
            {
                // Update registry permissions, which will allow to modify affected devices in non elevated mode.
                FixPermissionsForAffectedDevices(registryName, rights);
                canModify = JocysCom.ClassLibrary.Security.PermissionHelper.HasRights(subKey, users, rights);
            }
            return canModify;
        }

        static void FixPermissionsForAffectedDevices(string registryName, RegistryRights rights)
        {
            var users = new SecurityIdentifier("S-1-5-32-545"); // Users WellKnownSidType.BuiltinUsersSid
            JocysCom.ClassLibrary.Security.PermissionHelper.SetRights(Registry.LocalMachine, registryName, users, rights);
        }

        #endregion

    }

}

