using Microsoft.Win32;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace JocysCom.ClassLibrary.Security
{
    /// <summary>
    /// View and edit user/group access on files, directories and registry.
    ///
    /// Requires References to:
    ///     System.Security.dll
    ///     System.DirectoryServices.dll
    ///     System.DirectoryServices.AccountManagement.dll
    /// </summary>
    public class PermissionHelper
    {
        public static bool IsElevated
        {
            get
            {
                return WindowsIdentity
                .GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }

        #region Users and Groups

        /// <summary>
        /// Return all groups.
        /// </summary>
        /// <param name="contextType">Specify local machine or domain context.</param>
        /// <returns></returns>
        public static GroupPrincipal[] GetAllGroups(ContextType contextType)
        {
            var context = new PrincipalContext(contextType);
            var gp = new GroupPrincipal(context, "*");
            var ps = new PrincipalSearcher(gp);
            return ps.FindAll().Cast<GroupPrincipal>().ToArray();
        }

        /// <summary>
        /// Returns machine and domain groups user is a member of.
        /// </summary>
        /// <param name="sid">User SID</param>
        public static List<GroupPrincipal> GetUserGroups(SecurityIdentifier sid)
        {
            var groups = new List<GroupPrincipal>();
            if (sid == null)
                return groups;
            if (!sid.IsAccountSid())
                return groups;
            // If local user then...
            if (sid.AccountDomainSid == null)
            {
                var machineContext = new PrincipalContext(ContextType.Machine);
                var principal = Principal.FindByIdentity(machineContext, sid.Value);
                groups = principal.GetGroups().Cast<GroupPrincipal>().ToList();
            }
            // If domain user then...
            else
            {
                var domainContext = new PrincipalContext(ContextType.Domain);
                var principal = Principal.FindByIdentity(domainContext, sid.Value);
                // Can take 1-2 seconds.
                groups = principal.GetGroups().Cast<GroupPrincipal>().ToList();
                // Domain user can be a member of local machine group.
                var localGroups = GetAllGroups(ContextType.Machine);
                foreach (var lg in localGroups)
                {
                    // Add group to the list if user is a member.
                    if (principal.IsMemberOf(lg))
                        groups.Add(lg);
                }
            }
            return groups;
        }

        /// <summary>
        /// Return true if current user is domain user, false if Local Machine user..
        /// </summary>
        public static bool IsDomainUser()
        {
            var currentIdentity = WindowsIdentity.GetCurrent();
            if (currentIdentity.User.AccountDomainSid == null)
                return false;
            var domainUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, currentIdentity.User.AccountDomainSid);
            var principal = new WindowsPrincipal(currentIdentity);
            return principal != null && principal.IsInRole(domainUsers);
        }

        #endregion

        #region Registry

        /// <summary>
        /// Check if user have all specified rights to the registry key.
        /// </summary>
        /// <param name="key">Registry key to check.</param>
        /// <param name="wksid">User to check.</param>
        /// <param name="rights">Rights to check.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(RegistryKey key, WellKnownSidType wksid, RegistryRights rights)
        {
            var sid = new SecurityIdentifier(wksid, null);
            return HasRights(key, rights, sid);
        }

        /// <summary>
        /// Check if user have all specified rights to the registry key.
        /// </summary>
        /// <param name="key">Registry key to check.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="identity">Windows identity to check. If null then check current user.</param>
        /// <param name="isElevated">Override elevated option. If set to false then return rights available when user is not running as Administrator.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(RegistryKey key, RegistryRights rights, WindowsIdentity identity = null, bool? isElevated = null)
        {
            if (identity == null)
                identity = WindowsIdentity.GetCurrent();
            var isAdmin = isElevated.HasValue
                        ? isElevated.Value
                        : identity.Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            return HasRights(key, rights, identity.User, isAdmin);
        }

        /// <summary>
        /// Check if user have all specified rights to the registry key.
        /// </summary>
        /// <param name="key">Registry key to check.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="sid">User to check.</param>
        /// <param name="isElevated">If set to false then return rights available when user is not running as Administrator.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(RegistryKey key, RegistryRights rights, SecurityIdentifier sid, bool? isElevated = null)
        {
            if (key == null)
                return false;
            if (sid == null)
                return false;
            var allowRights = GetRights(key, sid, true, AccessControlType.Allow);
            var groups = GetUserGroups(sid);
            var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            // Process group allow rights.
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].Sid;
                // If simulate non elevated mode then remove administrators.
                if (isElevated.HasValue && !isElevated.Value && group == admins)
                    continue;
                var groupAllowRights = GetRights(key, group, true, AccessControlType.Allow);
                // Merge group rights.
                allowRights |= groupAllowRights;
            }
            var denyRights = GetRights(key, sid, true, AccessControlType.Deny);
            // Process group deny rights.
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].Sid;
                var groupDenyRights = GetRights(key, group, true, AccessControlType.Deny);
                // Merge group rights.
                denyRights |= groupDenyRights;
            }
            // Remove denied rights.
            allowRights &= ~denyRights;
            // Return true if all specified rights are allowed.
            return (allowRights & rights) == rights;
        }

        /// <summary>
        /// Get registry rights for specific user.
        /// </summary>
        /// <param name="key">Registry key.</param>
        /// <param name="sid">User Identifier.</param>
        /// <returns>Registry rights.</returns>
        public static RegistryRights GetRights(RegistryKey key, SecurityIdentifier sid, bool includeInherited = false, AccessControlType accessType = AccessControlType.Allow)
        {
            var rights = default(RegistryRights);
            if (key == null)
                return rights;
            if (sid == null)
                return rights;
            var security = key.GetAccessControl();
            var rules = security.GetAccessRules(true, true, sid.GetType());
            foreach (RegistryAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType != accessType)
                    continue;
                //  Merge permissions.
                rights |= rule.RegistryRights;
            }
            return rights;
        }

        /// <summary>
        /// Set specified rights to the file or directory.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="sid">User to check.</param>
        /// <param name="inheritance">Inheritance options.</param>
        /// <param name="propagation">Propagation options.</param>
        /// <returns>True if success, False if failed to set the rights.</returns>
        public static bool SetRights(
            RegistryKey baseKey,
            string registryName,
            RegistryRights rights,
            SecurityIdentifier sid,
            // Applies to keys and sub keys by default.
            InheritanceFlags? inheritance = null,
            PropagationFlags? propagation = null
        )
        {
            var key = baseKey.OpenSubKey(registryName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            if (key == null)
                return false;
            if (sid == null)
                return false;
            var security = key.GetAccessControl();
            RegistryAccessRule sidRule = null;
            // Do not include inherited permissions, because.
            var rules = security.GetAccessRules(true, false, sid.GetType());
            foreach (RegistryAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    sidRule = rule;
                    break;
                }
            }
            if (sidRule == null)
            {
                sidRule = new RegistryAccessRule(
                    sid,
                    // Set new permissions.
                    rights,
                    inheritance ?? InheritanceFlags.ContainerInherit,
                    propagation ?? PropagationFlags.None,
                    AccessControlType.Allow
                );
                security.AddAccessRule(sidRule);
            }
            else
            {
                var newRule = new RegistryAccessRule(
                    sid,
                    // Append missing permissions.
                    sidRule.RegistryRights | rights,
                    inheritance ?? sidRule.InheritanceFlags,
                    propagation ?? sidRule.PropagationFlags,
                    AccessControlType.Allow
                );
                security.SetAccessRule(newRule);
            }
            key.SetAccessControl(security);
            key.Close();
            return true;
        }

        #endregion

        #region File System

        /// <summary>
        /// Check if user have all specified rights to the file or directory.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="wksid">User to check.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(string path, FileSystemRights rights, WellKnownSidType wksid)
        {
            var sid = new SecurityIdentifier(wksid, null);
            return HasRights(path, rights, sid);
        }

        /// <summary>
        /// Check if user have all specified rights to the file or directory.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="identity">Windows identity to check. If null then check current user.</param>
        /// <param name="isElevated">Override elevated option. If set to false then return rights available when user is not running as Administrator.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(string path, FileSystemRights rights, WindowsIdentity identity = null, bool? isElevated = null)
        {
            if (identity == null)
                identity = WindowsIdentity.GetCurrent();
            var isAdmin = isElevated.HasValue
                ? isElevated.Value
                : identity.Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            return HasRights(path, rights, identity.User, isAdmin);
        }

        /// <summary>
        /// Check if user have all specified rights to the file or directory.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="sid">User to check.</param>
        /// <param name="isElevated">If set to false then return rights available when user is not running as Administrator.</param>
        /// <returns>True if user has rights, false if user don't have rights or rights not found.</returns>
        public static bool HasRights(string path, FileSystemRights rights, SecurityIdentifier sid, bool? isElevated = null)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (sid == null)
                return false;
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;
            var allowRights = GetRights(path, sid, true, AccessControlType.Allow);
            var groups = GetUserGroups(sid);
            var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            // Process group allow rights.
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].Sid;
                // If simulate non elevated mode then remove administrators.
                if (isElevated.HasValue && !isElevated.Value && group == admins)
                    continue;
                var groupAllowRights = GetRights(path, group, true, AccessControlType.Allow);
                // Merge group rights.
                allowRights |= groupAllowRights;
            }
            var denyRights = GetRights(path, sid, true, AccessControlType.Deny);
            // Process group deny rights.
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].Sid;
                var groupDenyRights = GetRights(path, group, true, AccessControlType.Deny);
                // Merge group rights.
                denyRights |= groupDenyRights;
            }
            // Remove denied rights.
            allowRights &= ~denyRights;
            // Return true if all specified rights are allowed.
            return (allowRights & rights) == rights;
        }

        /// <summary>
        /// Get file or directory rights for specific user.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="sid">User Identifier.</param>
        /// <returns>File or directory rights.</returns>
        public static FileSystemRights GetRights(string path, SecurityIdentifier sid, bool includeInherited = false, AccessControlType accessType = AccessControlType.Allow)
        {
            var rights = default(FileSystemRights);
            if (string.IsNullOrEmpty(path))
                return rights;
            if (sid == null)
                return rights;
            if (!File.Exists(path) && !Directory.Exists(path))
                return rights;
            var attributes = File.GetAttributes(path);
            var isDirectory = attributes.HasFlag(FileAttributes.Directory);
            var security = isDirectory
                ? (FileSystemSecurity)Directory.GetAccessControl(path)
                : (FileSystemSecurity)File.GetAccessControl(path);
            var rules = security.GetAccessRules(true, true, sid.GetType());
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType != accessType)
                    continue;
                //  Merge permissions.
                rights |= rule.FileSystemRights;
            }
            return rights;
        }

        /// <summary>
        /// Set specified rights to the file or directory.
        /// </summary>
        /// <param name="path">The path to a file or directory.</param>
        /// <param name="rights">Rights to check.</param>
        /// <param name="sid">User to check.</param>
        /// <param name="inheritance">Inheritance options.</param>
        /// <param name="propagation">Propagation options.</param>
        /// <returns>True if success, False if failed to set the rights.</returns>
        public static bool SetRights(
            string path,
            FileSystemRights rights,
            SecurityIdentifier sid,
            // Applies to directories and sub directories by default.
            InheritanceFlags? inheritance = null,
            PropagationFlags? propagation = null)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (sid == null)
                return false;
            if (!File.Exists(path) && !Directory.Exists(path))
                return false;
            var attributes = File.GetAttributes(path);
            var isDirectory = attributes.HasFlag(FileAttributes.Directory);
            var security = isDirectory
                ? (FileSystemSecurity)Directory.GetAccessControl(path)
                : (FileSystemSecurity)File.GetAccessControl(path);
            FileSystemAccessRule sidRule = null;
            // Do not include inherited permissions, because.
            var rules = security.GetAccessRules(true, false, sid.GetType());
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference != sid)
                    continue;
                if (rule.AccessControlType == AccessControlType.Allow)
                {
                    sidRule = rule;
                    break;
                }
            }
            if (sidRule == null)
            {
                sidRule = new FileSystemAccessRule(
                    sid,
                    // Set new permissions.
                    rights,
                    inheritance ?? (
                        isDirectory
                            ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit
                            : InheritanceFlags.None
                    ),
                    propagation ?? PropagationFlags.None,
                    AccessControlType.Allow
                );
                security.AddAccessRule(sidRule);
            }
            else
            {
                var newRule = new FileSystemAccessRule(
                    sid,
                    // Append missing permissions.
                    sidRule.FileSystemRights | rights,
                    inheritance ?? sidRule.InheritanceFlags,
                    propagation ?? sidRule.PropagationFlags,
                    AccessControlType.Allow
                );
                security.SetAccessRule(newRule);
            }
            if (isDirectory)
                Directory.SetAccessControl(path, (DirectorySecurity)security);
            else
                File.SetAccessControl(path, (FileSecurity)security);
            return true;
        }

        #endregion

    }
}
