using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;

namespace JocysCom.ClassLibrary.Security
{
    public class PermissionHelper
    {

        public void ExportToFile(string filename)
        {
            var permissionSet = new PermissionSet(PermissionState.None);
            var writePermission = new FileIOPermission(FileIOPermissionAccess.Write, filename);
            permissionSet.AddPermission(writePermission);
            if (permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet))
            {
                //using (FileStream fstream = new FileStream(filename, FileMode.Create))
                //using (TextWriter writer = new StreamWriter(fstream))
                //{
                //	// try catch block for write permissions 
                //	writer.WriteLine("sometext");
                //}
            }
            else
            {
                //perform some recovery action here

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
        /// <param name="sid">User sid</param>
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

        public static bool IsDomainUser()
        {
            var currentIdentity = WindowsIdentity.GetCurrent();
            if (currentIdentity.User.AccountDomainSid == null)
                return false;
            var domainUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, currentIdentity.User.AccountDomainSid);
            var principal = new WindowsPrincipal(currentIdentity);
            return principal != null && (principal.IsInRole(domainUsers));
        }

        #endregion

        #region Registry

        /// <summary>
        /// Check if user have all specified rights to the registry key.
        /// </summary>
        /// <param name="key">Registry key to check.</param>
        /// <param name="wksid">User to check.</param>
        /// <param name="rights">Rights to check.</param>
        public static bool HasRights(RegistryKey key, WellKnownSidType wksid, RegistryRights rights)
        {
            var sid = new SecurityIdentifier(wksid, null);
            return HasRights(key, sid, rights);
        }

        public static bool HasRights(RegistryKey key, WindowsIdentity principal, RegistryRights rights)
        {
            var isAdmin = principal.User != principal.Owner;
            return HasRights(key, principal.User, rights, isAdmin);
        }

        /// <summary>
        /// Check if user have all specified rights to the registry key.
        /// </summary>
        /// <param name="key">Registry key to check.</param>
        /// <param name="sid">User to check.</param>
        /// <param name="rights">Rights to check.</param>
        /// <returns></returns>
        public static bool HasRights(RegistryKey key, SecurityIdentifier sid, RegistryRights rights, bool? isElevated = null)
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
                // If simulate non elevated mode then remove admin.
                if (isElevated.HasValue && !isElevated.Value && group == admins)
                    continue;
                var groupAllowRights = GetRights(key, group, true, AccessControlType.Allow);
                // Merge group rigths.
                allowRights |= groupAllowRights;
            }
            var denyRights = GetRights(key, sid, true, AccessControlType.Deny);
            // Process group deny rights.
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].Sid;
                var groupDenyRights = GetRights(key, group, true, AccessControlType.Deny);
                // Merge group rigths.
                denyRights |= groupDenyRights;
            }
            // Remove denied rights.
            allowRights &= ~denyRights;
            // Return true if all specifeid rights are allowed.
            return (allowRights & rights) == rights;
        }

        /// <summary>
        /// Get registry rights for specific user.
        /// </summary>
        /// <param name="key">Registry key.</param>
        /// <param name="sid">User Identifier.</param>
        /// <returns>Null if rights not found.</returns>
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
        /// </summary>
        /// <param name="baseKey"></param>
        /// <param name="registryName"></param>
        /// <param name="sid"></param>
        /// <param name="rights"></param>
        /// <returns></returns>
        public static bool SetRights(
            RegistryKey baseKey, string registryName, SecurityIdentifier sid, RegistryRights rights,
            // Applies to keys and subkeys by default.
            InheritanceFlags inheritance = InheritanceFlags.ContainerInherit,
            PropagationFlags propagation = PropagationFlags.None)
        {
            var key = baseKey.OpenSubKey(registryName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            if (key == null)
                return false;
            if (sid == null)
                return false;
            var rs = new RegistrySecurity();
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
                    inheritance,
                    propagation,
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
                    sidRule.InheritanceFlags,
                    sidRule.PropagationFlags,
                    AccessControlType.Allow
                );
                security.SetAccessRule(sidRule);
            }
            key.SetAccessControl(security);
            key.Close();
            return true;
        }

        #endregion
    }
}
