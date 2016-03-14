using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace JocysCom.Web.Security
{
	public class HelperFunctions
	{
		public static void UpdateRole(string roleName, string description)
		{
            Security.Check.UpdateRole(roleName, description);	
        }

		private static void CreateUser(string userName, string comment){
			if (Membership.FindUsersByName(userName).Count == 0)
			{
				MembershipUser user = Membership.CreateUser(userName, "password");
				user.Comment = comment;
				Membership.UpdateUser(user);
			}
		}

		private static void AddUserToRole(string userName, string roleName){
			if (userName.Length > 0 && roleName.Length > 0)
			{
				if (!System.Web.Security.Roles.IsUserInRole(userName, roleName))
				{
					System.Web.Security.Roles.AddUserToRole(userName, roleName);
				}
			}
		}

		public static void CreateRole(string roleName, string description)
		{
			if (!System.Web.Security.Roles.RoleExists(roleName))
			{
				System.Web.Security.Roles.CreateRole(roleName);
				UpdateRole(roleName, description);
			}
		}

		/// <summary>
		/// Crete default roles. Must be executed before 'CreateDefaultUsers' method.
		/// </summary>
		public static void CreateDefaultRoles()
		{
			CreateRole("Administrators", "Administrators have complete and unrestricted access to all features.");
			CreateRole("Power Users", "Power Users possess most administrative powers with some restrictions.");
			CreateRole("Users", "Users are prevented from making accidental or intentional system-wide changes.");
			CreateRole("Guests", "Guests have restricted access to all features.");
		}

		/// <summary>
		/// Crete default users. Must be executed after 'CreateDefaultRoles' method.
		/// </summary>
		public static void CreateDefaultUsers()
		{
			CreateUser("Administrator", "Account for administering the application.");
			AddUserToRole("Administrator", "Administrators");
			CreateUser("Guest", "Account for guest access to the application.");
			AddUserToRole("Guest", "Guests");
		}



	}
}
