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

namespace JocysCom.WebSites.Engine.Security
{

	public class Check
	{
		public static bool CurrentUserIsAuthenticated
		{
			get
			{
				return
					HttpContext.Current.User != null &&
					HttpContext.Current.User.Identity.IsAuthenticated;
			}
		}

		public static void CreateDefault()
		{
			CreateDefaultSchema();
			CreateDefaultApplication();
		}

		public static void UpdateRole(string roleName, string description)
		{
			var db = new Data.SecurityEntities();
			var role = db.Roles.FirstOrDefault(x => x.Application.ApplicationName == System.Web.Security.Roles.ApplicationName && x.RoleName == roleName);
			if (role != null) role.Description = description;
			db.SaveChanges();
		}

		public static void CreateDefaultApplication()
		{
			var db = new Data.SecurityEntities();
			var app = db.Applications.FirstOrDefault(x => x.LoweredApplicationName == "/");
			if (app == null)
			{
				app = new Data.Application();
				app.ApplicationName = "/";
				app.LoweredApplicationName = "/";
				app.ApplicationId = new Guid("5985BB89-8015-42E3-9FE1-D7F9FD914EEC");
				app.Description = "Default Application";
				db.Applications.AddObject(app);
				db.SaveChanges();
			}
		}

		public static void CreateDefaultSchema()
		{
			var db = new Data.SecurityEntities();
			var item = db.SchemaVersions.FirstOrDefault(x => x.Feature == "common");
			if (item == null)
			{
				item = new Data.SchemaVersion();
				item.Feature = "common";
				item.CompatibleSchemaVersion = "1";
				item.IsCurrentVersion = true;
				db.SchemaVersions.AddObject(item);
				db.SaveChanges();
			}
			item = db.SchemaVersions.FirstOrDefault(x => x.Feature == "membership");
			if (item == null)
			{
				item = new Data.SchemaVersion();
				item.Feature = "membership";
				item.CompatibleSchemaVersion = "1";
				item.IsCurrentVersion = true;
				db.SchemaVersions.AddObject(item);
				db.SaveChanges();
			}
			item = db.SchemaVersions.FirstOrDefault(x => x.Feature == "role manager");
			if (item == null)
			{
				item = new Data.SchemaVersion();
				item.Feature = "role manager";
				item.CompatibleSchemaVersion = "1";
				item.IsCurrentVersion = true;
				db.SchemaVersions.AddObject(item);
				db.SaveChanges();
			}
		}

		public static void CreateDefaultRoles()
		{
			// Check for Missing roles Role.
			if (!Roles.RoleExists("Administrators"))
			{
				Roles.CreateRole("Administrators");
				UpdateRole("Administrators", "Administrators have complete and unrestricted access to all features.");
			}
			if (!Roles.RoleExists("PowerUsers"))
			{
				Roles.CreateRole("PowerUsers");
				UpdateRole("PowerUsers", "Power Users possess most administrative powers with some restrictions.");
			}
			if (!Roles.RoleExists("Users"))
			{
				Roles.CreateRole("Users");
				UpdateRole("Users", "Users are prevented from making accidental or intentional system-wide changes.");
			}
			if (!Roles.RoleExists("Guests"))
			{
				Roles.CreateRole("Guests");
				UpdateRole("Guests", "Guests have restricted access to all features.");
			}
		}

		public static bool AssignUserToRole(string userName, string roleName)
		{
			bool results;
			// If user is in role then...
			if (Roles.IsUserInRole(userName, roleName))
			{
				results = false;
			}
			else
			{
				Roles.AddUserToRole(userName, roleName);
				results = true;
			}
			return results;
		}
	}
}