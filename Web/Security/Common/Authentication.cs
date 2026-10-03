using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
//using System.DirectoryServices;

namespace JocysCom.Web.Security
{
	/// <summary>
	/// Summary description for Authentication
	/// </summary>
	public class Authentication
	{
		public Authentication()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static string SetCurrentUser(string username)
		{
			string errorMessage = string.Empty;
			// Initialize FormsAuthentication, for what it's worth.
			FormsAuthentication.Initialize();
			MembershipUser user = Membership.GetUser(username, true);
			if (user == null) errorMessage = "Employee '"+username+"' was not found.";
			if (String.IsNullOrEmpty(errorMessage))
			{
				string[] roles = System.Web.Security.Roles.GetRolesForUser(username);
				string rolesString = string.Empty;
				for (int i = 0; i < roles.Length; i++)
				{
					if (i > 0) rolesString += ",";
					rolesString += roles[i];
				}
				// Create a new ticket used for authentication. Ticket lasts 30 min by default.
				double loginRememberMinutes = 30;
				if (SecurityContext.Current.CookieRememberMe)
				{
					loginRememberMinutes = SecurityContext.Current.LoginRememberMinutes;
				}

				FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(loginRememberMinutes), true, rolesString, FormsAuthentication.FormsCookiePath);
				// Encrypt the cookie using the machine key for secure transport.
				string hash = FormsAuthentication.Encrypt(ticket);
				HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, hash); // Hashed ticket
				// Set the cookie's expiration time to the tickets expiration time
				if (ticket.IsPersistent) cookie.Expires = ticket.Expiration;
				System.Web.HttpContext.Current.Response.Cookies.Add(cookie);
				// Create Identity.
				System.Security.Principal.GenericIdentity identity = new System.Security.Principal.GenericIdentity(user.UserName);
				// Create Principal.
				RolePrincipal principal = new RolePrincipal(identity);
				System.Threading.Thread.CurrentPrincipal = principal;
				// Create User.
				HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(identity, roles);
			}
			return errorMessage;
		}

		
		/// <summary>
		/// Fired from global.asax when the security module has established the current user's identity as valid. At this point, the user's credentials have been validated.
		/// Note: Authentication cookie was used for credentials validation.
		/// </summary>
		public static void AuthenticateRequest(Object sender, EventArgs e)
		{
            System.Web.HttpApplication g = (System.Web.HttpApplication)sender;
			string p = g.Request.CurrentExecutionFilePath;
			//System.Diagnostics.Trace.Indent();
			System.Diagnostics.Trace.WriteLine("Entering",p);
			System.Diagnostics.Trace.WriteLine("Sender: " + sender.GetType().FullName, p);
			// If context user exist then...
			if (System.Web.HttpContext.Current.User != null)
			{
				string userName = System.Web.HttpContext.Current.User.Identity.Name;
				System.Diagnostics.Trace.WriteLine("HttpContext.Current.User.Identity.Name='" + userName + "'", p);
				if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
				{
					System.Diagnostics.Trace.WriteLine("User is authenticated", p);
					// Get authentication and session cookies.
					HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
					// Get ticket from authentication cookie.
					FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(cookie.Value);
					System.Diagnostics.Trace.WriteLine("Authentication ticket.Name (user name) ='" + ticket.Name + "'", p);
					System.Diagnostics.Trace.WriteLine("Authentication ticket.UserData (user roles) ='" + ticket.UserData + "'", p);
				}
				else
				{
					System.Diagnostics.Trace.WriteLine("User is not authenticated.", p);
					// ...signout.
					//FormsAuthentication.SignOut();
				}
			}
			else
			{
				System.Diagnostics.Trace.WriteLine("Current.User is null.", p);
				// ...signout.
				//	FormsAuthentication.SignOut();
			}
			//System.Diagnostics.Trace.Unindent();
		}


		/// <summary>
		/// Fired from global.asax when the security module has verified that a user can access resources.
		/// </summary>
		public static void AuthorizeRequest(Object sender, EventArgs e)
		{
            System.Web.HttpApplication g = (System.Web.HttpApplication)sender;
			string p = g.Request.CurrentExecutionFilePath;
			System.Diagnostics.Trace.WriteLine("Sender: " + sender.GetType().FullName, p);
			//System.Diagnostics.Trace.WriteLine("HttpContext.Current is null = '" + (HttpContext.Current == null).ToString() + "'", p);
			//System.Diagnostics.Trace.WriteLine("HttpContext.Current.Session is null = '" + (HttpContext.Current.Session == null).ToString() + "'", p);
			// If user is authenticated then...
			if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
			{
				System.Diagnostics.Trace.WriteLine("User is authenticated", p);
				string userName = System.Web.HttpContext.Current.User.Identity.Name;
				System.Diagnostics.Trace.WriteLine("IdentityName='" + userName + "'", p);
			}
			else
			{
				System.Diagnostics.Trace.WriteLine("User is not authenticated", p);
				// ...signout.
				//System.Web.HttpContext.Current.Response.Clear();
				//System.Web.HttpContext.Current.Response.Write("Not Authenticated");
				//System.Web.HttpContext.Current.Response.End();
				//FormsAuthentication.RedirectToLoginPage();
			}
		}

		public static string GetAuthenticationInfo()
		{
			string info = string.Empty;
			info += "Current.User is null = " + (HttpContext.Current.User == null) + "<br />";
			info += "Current.User.Identity.IsAuthenticated = " + (HttpContext.Current.User.Identity.IsAuthenticated) + "<br />";
			info += "Current.User.Identity is FormsIdentity = " + (HttpContext.Current.User.Identity is FormsIdentity) + "<br />";
			info += "Current.User.Identity.AuthenticationType = " + (HttpContext.Current.User.Identity.AuthenticationType.ToString()) + "<br />";
			//info += "WebConfig.Current.Employee is null = " + (WebConfig.Current.Employee == null) + "<br />";
			info += "FormsAuthentication.FormsCookieName = " + FormsAuthentication.FormsCookieName + "<br />";
			info += "Check.CurrentUserIsAuthenticated = " + Check.CurrentUserIsAuthenticated + "<br />";
			//info += "Check.CurrentValuesExist = " + Check.CurrentValuesExist + "<br />";
			//HttpCookie cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
			//if (cookie != null) info += "cookie.Value  = " + cookie.Values["EmployeeLoginName"] + "<br />";
			//string employeeLoginName = cookie.Values["EmployeeLoginName"];
			//info += "EmployeeLoginName = " + employeeLoginName + "<br />";
			return info;
		}
	}
}