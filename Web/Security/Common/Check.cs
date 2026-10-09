using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Linq;
using System.Collections.Generic;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security
{
	/// <summary>
	/// Summary description for Members
	/// </summary>
	public class Check
	{
		public Check()
		{
			//
			// TODO: Add constructor logic here
			//
		}
       

		public static bool CurrentUserIsAuthenticated
		{
			get
			{
				bool success = true;
				if (success) success &= (HttpContext.Current.User != null);
				if (success) success &= (HttpContext.Current.User.Identity.IsAuthenticated);
				return success;
			}
		}

		public static void UpdateRole(string roleName, string description)
		{
			var db = new SecurityEntities();
            Role role = (from item in db.Roles
                              where item.Application.ApplicationName == System.Web.Security.Roles.ApplicationName
                              && item.RoleName == roleName
                              select item).FirstOrDefault();
            if (role != null) role.Description = description;
            db.SaveChanges();
		}

		public static void CreateDefaultRoles()
		{

			// Check for Missing roles Role.
			if (!System.Web.Security.Roles.RoleExists("Administrators"))
			{
				System.Web.Security.Roles.CreateRole("Administrators");
				UpdateRole("Administrators", "Administrators have complete and unrestricted access to all features.");
			}
            if (!System.Web.Security.Roles.RoleExists("PowerUsers"))
            {
				System.Web.Security.Roles.CreateRole("PowerUsers");
                UpdateRole("PowerUsers", "Power Users possess most administrative powers with some restrictions.");
            }
            if (!System.Web.Security.Roles.RoleExists("Users"))
			{
				System.Web.Security.Roles.CreateRole("Users");
				UpdateRole("Users", "Users are prevented from making accidental or intentional system-wide changes.");
			}
			if (!System.Web.Security.Roles.RoleExists("Guests"))
			{
				System.Web.Security.Roles.CreateRole("Guests");
				UpdateRole("Guests", "Guests have restricted access to all features.");
			}
		}

        public static void CheckToImportMissingUsers(string username)
        {
            // If user does not exists then...
            MembershipUserCollection users = System.Web.Security.Membership.FindUsersByName(username);
            bool userWasFound = (users.Count > 0);
            // If user was not found then...
            if (!userWasFound)
            {
                // Check and create default roles.
                Check.CreateDefaultRoles();
                // Check and import missing users.
                //Check.ImportMissingUsers();
            }
        }


		/// <summary>
		/// This function will be executed as soon as his username and password will be found inside domain 
		/// or database.
		/// </summary>
		/// <returns></returns>
		public static string CheckAndInitUser()
		{
			string results = string.Empty;
            string userName = SecurityContext.Current.CurrentUserName;
			// User logged in successfuly. Now we can add this user to list.
			//CheckToImportEmployees(userName); - this function was called before. Better logic neccessary.

			// If user is not yet "Employee" then...
			if (!System.Web.Security.Roles.IsUserInRole(userName, "Users"))
			{
				// Update user roes.
				Check.UpdateUserRoles(userName);
			}
			// Make sure that this user has required permissions.
			// Later this must be removed and administrator must be placed into web.config file.
			if (SecurityContext.Current.Administrators.Contains(userName))
			{
				Check.UpdateUserRoles(userName);
			}
			// If this user does not exist then...
			MembershipUser user = System.Web.Security.Membership.GetUser(userName, false);
			if (user != null)
			{
				// Update user password.
				//string currentPassword = user.GetPassword();
				//if (currentPassword != userPassword){}
				results += "Membership Check: Success - User Exist;";
				//results += user.ResetPassword();
				//user.ChangePassword(user.ResetPassword(), "new password");
				//results += "ChangePass: "+user.ChangePassword("password", "password1").ToString();
				//WebConfig.Current.Members.UpdateUser(user);
			}
			return results;
		}

		public static bool AssignUserToRole(string userName, string roleName)
		{
			bool results;
			// If user is in role then...
			if (System.Web.Security.Roles.IsUserInRole(userName, roleName))
			{
				results = false;
			}
			else
			{
				System.Web.Security.Roles.AddUserToRole(userName, roleName);
				results = true;
			}
			return results;
		}

		// This is not right to embed roles into code. But I am not manager yet. :)
		// This will be changed in next release.
		public static void UpdateUserRoles(string userName)
		{
			// If user is not in users group then add him...
			AssignUserToRole(userName, "Users");
			// If user is not in Level1 then make him...
			bool isAdministrator = SecurityContext.Current.Administrators.Contains(userName);
			bool isPowerUser = SecurityContext.Current.PowerUsers.Contains(userName);
			if (isAdministrator)
			{
				AssignUserToRole(userName, "Administrators");
			}
			if (isPowerUser)
			{
				AssignUserToRole(userName, "PowerUsers");
			}
		}

	}
}