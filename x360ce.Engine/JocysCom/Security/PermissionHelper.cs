using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

// .NET Core: requires "System.IO.FileSystem.AccessControl" NuGet package.

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

		/// <summary>
		/// Returns true if current user can rename the file.
		/// </summary>
		/// <param name="fileFullName">Path to the file.</param>
		/// <param name="user">Optional. Default current windows user.</param>
		/// <returns>true if current user can rename the file. false if elevated permissions required.</returns>
		public static bool CanRenameFile(string fileFullName, SecurityIdentifier user = null)
		{
			user = user ?? WindowsIdentity.GetCurrent().User;
			var fileDirectory = new FileInfo(fileFullName).Directory.FullName;
			// Check for Write permissions on the directory.
			var hasDirectoryWriteRights = HasRights(fileDirectory, FileSystemRights.Write, user);
			// Check for Delete permissions on the file itself.
			var hasFileDeleteRights = HasRights(fileFullName, FileSystemRights.Delete, user);
			// Alternative: Check for Modify permissions on the file itself.
			var hasFileModifyRights = HasRights(fileFullName, FileSystemRights.Modify, user);
			return (hasDirectoryWriteRights && hasFileDeleteRights) || hasFileModifyRights;
		}

		#region Users and Groups

		/// <summary>
		/// Returns all groups in the specified context.
		/// </summary>
		/// <param name="contextType">Specifies whether the context is for the local machine or a domain.</param>
		/// <param name="server">The server to connect to. If null, defaults to the logon server for domain contexts.</param>
		/// <param name="container">The container within the directory to search for groups. If null, defaults to an appropriate container based on the context.</param>
		/// <param name="samAccountName">The SAM account name to use as a filter for group search. Defaults to "*", meaning all groups.</param>
		/// <returns>An array of <see cref="GroupPrincipal"/> objects representing the groups found.</returns>
		/// <remarks>
		/// The method supports both local machine and domain contexts. For domain contexts, if the <paramref name="server"/> 
		/// is not provided, it defaults to the logon server. The <paramref name="container"/> parameter allows specifying 
		/// a particular organizational unit (OU) to search within. If not specified, it defaults to an appropriate container 
		/// based on the user’s DNS domain.
		/// </remarks>
		public static GroupPrincipal[] GetAllGroups(
			ContextType contextType,
			string server = null, string container = null,
			string samAccountName = "*")
		{
			PrincipalContext context;
			if (contextType == ContextType.ApplicationDirectory)
			{
				server = server ?? Environment.GetEnvironmentVariable("LOGONSERVER")?.Trim('\\');
				container = container ?? ""; // "OU=Users and Groups";
				var userDnsDomain = Environment.GetEnvironmentVariable("USERDNSDOMAIN");
				var groups = new List<string>();
				var dcParts = string.Join(",", userDnsDomain.Split('.').Select(part => $"DC={part}"));
				if (!string.IsNullOrEmpty(container))
					container += ",";
				container += dcParts;
				context = new PrincipalContext(contextType, server, dcParts);
			}
			else
			{
				context = new PrincipalContext(contextType);
				if (string.IsNullOrEmpty(context.UserName))
					return Array.Empty<GroupPrincipal>();
			}
			var gp = new GroupPrincipal(context, samAccountName);
			var ps = new PrincipalSearcher(gp);
			return ps.FindAll().Cast<GroupPrincipal>()
				.OrderBy(x => x.Name)
				.ToArray();
		}

		/// <summary>
		/// Returns machine and domain groups user is a member of.
		/// </summary>
		/// <param name="sid">User SID</param>
		public static List<GroupPrincipal> GetUserGroups(SecurityIdentifier sid)
		{
			var groups = new List<GroupPrincipal>();
			if (sid is null)
				return groups;
			if (!sid.IsAccountSid())
				return groups;
			var isLocal = IsLocalUser(sid) || IsLocalGroup(sid);
			// If local user or group then...
			if (isLocal)
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
			return groups.OrderBy(x => x.Name).ToList();
		}

		public static bool IsLocalUser()
		{
			var currentIdentity = WindowsIdentity.GetCurrent();
			var sid = currentIdentity.User;
			var isLocal = IsLocalUser(sid) || IsLocalGroup(sid);
			return isLocal;
		}

		/// <summary>
		/// Return true if current user is domain user, false if Local Machine user..
		/// </summary>
		public static bool IsDomainUser()
		{
			var currentIdentity = WindowsIdentity.GetCurrent();
			var sid = currentIdentity.User;
			var isLocal = IsLocalUser(sid) || IsLocalGroup(sid);
			// If local user or group then...
			if (isLocal)
				return false;
			var domainUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, sid.AccountDomainSid);
			var principal = new WindowsPrincipal(currentIdentity);
			return principal != null && principal.IsInRole(domainUsers);
		}

		public static bool IsLocalUser(SecurityIdentifier sid)
		{
			using (var context = new PrincipalContext(ContextType.Machine))
			using (var principal = new UserPrincipal(context))
			using (var searcher = new PrincipalSearcher(principal))
			{
				var users = searcher.FindAll().ToArray();
				return users.Any(x => x.Sid.Equals(sid));
			}
		}

		public static bool IsLocalGroup(SecurityIdentifier sid)
		{
			using (var context = new PrincipalContext(ContextType.Machine))
			using (var principal = new GroupPrincipal(context))
			using (var searcher = new PrincipalSearcher(principal))
			{
				var groups = searcher.FindAll().ToArray();
				return groups.Any(x => x.Sid.Equals(sid));
			}
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
			if (identity is null)
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
			if (key is null)
				return false;
			if (sid is null)
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
			if (key is null)
				return rights;
			if (sid is null)
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
			if (key is null)
				return false;
			if (sid is null)
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
			if (sidRule is null)
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
			if (identity is null)
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
			if (sid is null)
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
				// If non elevated then and group is "Administrators" then...
				if (isElevated.HasValue && !isElevated.Value && group == admins)
				{
					// Simulate non elevated mode by replacing "Administrators" group with "Users" group.
					group = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
				}
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
			if (sid is null)
				return rights;
			if (!File.Exists(path) && !Directory.Exists(path))
				return rights;
			var attributes = File.GetAttributes(path);
			var isDirectory = attributes.HasFlag(FileAttributes.Directory);
			var security = isDirectory
				? new DirectoryInfo(path).GetAccessControl()
				: (FileSystemSecurity)new FileInfo(path).GetAccessControl();
			Console.WriteLine($"Access control information for the directory '{path}':");
			Console.WriteLine(security);
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
			if (sid is null)
				return false;
			if (!File.Exists(path) && !Directory.Exists(path))
				return false;
			var attributes = File.GetAttributes(path);
			var isDirectory = attributes.HasFlag(FileAttributes.Directory);
			var security = isDirectory
				? new DirectoryInfo(path).GetAccessControl()
				: (FileSystemSecurity)new FileInfo(path).GetAccessControl();
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
			if (sidRule is null)
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
				new DirectoryInfo(path).SetAccessControl((DirectorySecurity)security);
			else
				new FileInfo(path).SetAccessControl((FileSecurity)security);
			return true;
		}

		#endregion

	}
}
