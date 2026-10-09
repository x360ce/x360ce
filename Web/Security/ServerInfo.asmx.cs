using System;
using System.Data;
using System.Web;
using System.Linq;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Security;
using JocysCom.WebSites.Engine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using x360ce.Engine;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security
{

	/// <summary>
	/// Summary description for ServerInfo
	/// </summary>
	[WebService(Namespace = "JocysCom.WebSites.Website.WebServices", Name = "ServiceInfo")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.Web.Script.Services.ScriptService]
	public class ServerInfo : System.Web.Services.WebService
	{

		public ServerInfo()
		{
			//Uncomment the following line if using designed components 
			//InitializeComponent(); 
		}

		//[WebMethod(EnableSession = true, Description = "Get message of the day.")]
		//public string GetMessageOfTheDay()
		//{
		//	string results = string.Empty;
		//	if (AppSettings.GetAppValue(AppSettings.Keys.ModEnabled) == "True")
		//	{
		//		results = AppSettings.GetAppValue(AppSettings.Keys.ModText);
		//	}
		//	return results;
		//}

		///// <summary>
		///// Get list of available domains.
		///// </summary>
		///// <returns>Data set with domain names.</returns>
		//[WebMethod(EnableSession = true, Description = "Get list of available domains.")]
		//[WebGet(ResponseFormat = WebMessageFormat.Json)]
		//public KeyValueList GetDomainsList()
		//{
		//	var results = new KeyValueList();
		//	var domains = new JocysCom.ClassLibrary.Network.DomainCollection();
		//	domains.Refresh();
		//	for (int i = 0; i < domains.Count; i++)
		//	{
		//		domains[i].DomainControllers.Refresh();
		//		//    myDomains[i].DialinServers.Refresh();
		//		//    myDomains[i].PrintServers.Refresh();
		//		//    myDomains[i].TerminalServers.Refresh();
		//		//    myDomains[i].TimeServers.Refresh();
		//		//    myDomains[i].Workstations.Refresh();
		//		//    myDomains[i].SQLServers.Refresh();
		//		// If domain controllers was found for domain then add to list.
		//		if (domains[i].DomainControllers.Count > 0)
		//		{
		//			results.Add("Domain", domains[i].Name);
		//		}
		//	}
		//	return results;
		//}


		//[WebMethod(EnableSession = true, Description = "Authenticate user.")]
		//public bool UserExists(string username)
		//{
		//	var db = OurDomainsEntities.Current;
		//	Engine.Data.OurDomains.User user;
		//	user = db.Users.FirstOrDefault(x => x.UserName == username);
		//	return (user != null);
		//}

		/// <summary>
		/// Test new user registration info if it valid for registration.
		/// </summary>
		/// <param name="field">Validation parameters.</param>
		/// <returns>validation results</returns>
		[WebMethod(EnableSession = true, Description = "Test new user registration info if it valid for registration.")]
		public WebSites.Engine.Security.ValidationField[] ValidateUserRegistration(
			string firstName,
			string lastName,
			string email,
			string username,
			string password,
			string birthday,
			string gender,
			bool terms,
			bool news
		)
		{
			return JocysCom.WebSites.Engine.Security.Data.User.ValidateMemberRegistration(
				firstName,
				lastName,
				email,
				username,
				password,
				birthday,
				gender,
				terms,
				news
			);
		}

		[WebMethod(EnableSession = true, Description = "Authenticate user.")]
		public KeyValueList LoginToDomain(string domain, string username, string password)
		{
			//string errorMessage = string.Empty;
			//if (password.Length == 0) errorMessage = "Please enter password";
			//if (username.Length == 0) errorMessage = "Please enter user name";
			//if (domain.Length == 0) errorMessage = "Please provide domain name.";
			//if (AppSettings.GetAppValue(AppSettings.Keys.AllowUsersToLogin) == "False") errorMessage = "All logins was restricted by administrator.";
			//string authenticationType = AppContext.Current.Settings.AuthenticationType;
			//if (authenticationType != "Domain") errorMessage = "Use " + authenticationType + " authentiation";
			//if (String.IsNullOrEmpty(errorMessage))
			//{
			//	DirectoryEntry directoryEntry;
			//	// Add Lightweight Directory Access Protocol prefix.
			//	string domainAddress = "LDAP://" + domain;
			//	directoryEntry = new DirectoryEntry(domainAddress, username, password, AuthenticationTypes.Secure);
			//	// If user is verified then it will welcome then...
			//	try
			//	{
			//		// Bind to the native AdsObject to force authentication.
			//		// This line will throw error: "Logon failure: unknown user name or bad password."
			//		Object obj = directoryEntry.NativeObject;
			//	}
			//	catch (Exception exp)
			//	{
			//		errorMessage = exp.Message;
			//	}
			//	directoryEntry.Dispose();
			//}
			var results = new KeyValueList();
			//if (String.IsNullOrEmpty(errorMessage))
			//{
			//	// If we are here then it means that we authenticated thru domain.
			//	// Now lets check if employee exist inside aspnet_ tables and import new users if required.
			//	Engine.Security.Check.CheckToImportMissingUsers(username);
			//	errorMessage = Engine.Security.Authentication.SetCurrentUser(username);
			//}
			//if (String.IsNullOrEmpty(errorMessage))
			//{
			//	SessionContext.Current.InitConfigOnValidationSuccess(username);
			//	results.Add("Status", true);
			//	results.Add("Message", "Welcome to " + domain + "!");
			//}
			//else
			//{
			//	// We are here because error happened.
			//	results.Add("Status", false);
			//	results.Add("Message", errorMessage);
			//}
			return results;
		}

		[WebMethod(EnableSession = true, Description = "Authenticate user.")]
		public KeyValueList LoginToDatabase(string database, string username, string password)
		{
			string errorMessage = string.Empty;
			//if (password.Length == 0) errorMessage = "Please enter password";
			//if (username.Length == 0) errorMessage = "Please enter user name";
			//if (database.Length == 0) errorMessage = "Please provide domain name.";
			//if (!AppContext.Current.Settings.AllowUsersToLogin) errorMessage = "All logins were restricted by administrator.";
			//string authenticationType = AppContext.Current.Settings.AuthenticationType;
			//if (authenticationType != "Database") errorMessage = "Use " + authenticationType + " authentiation";
			//if (errorMessage.Length == 0)
			//{
			//	// Now lets check if employee exist inside aspnet_ tables and import new users if required.
			//	Engine.Security.Check.CheckToImportMissingUsers(username);
			//	// Here must be validation with password. You can add third party validation here;
			//	bool success = System.Web.Security.Membership.ValidateUser(username, password);
			//	if (!success) errorMessage = "Validation failed. User name '" + username + "' was not found.";
			//}
			var results = new KeyValueList();
			//if (errorMessage.Length > 0)
			//{
			//	results.Add("Status", false);
			//	results.Add("Message", errorMessage);
			//}
			//else
			//{
			//	Engine.Security.Authentication.SetCurrentUser(username);
			//	SessionContext.Current.InitConfigOnValidationSuccess(username);
			//	results.Add("Status", true);
			//	results.Add("Message", "Welcome to " + database + "!");
			//}
			return results;
		}

		[WebMethod(EnableSession = true, Description = "Restore login information if possible.")]
		public KeyValueList LoginRestore()
		{
			System.Web.Security.FormsAuthentication.SignOut();
			System.Web.Security.FormsAuthentication.RedirectToLoginPage();
			var results = new KeyValueList();
			results.Add("Status", true);
			return results;
		}


		[WebMethod(EnableSession = true, Description = "Check login status.")]
		public KeyValueList LoginCheck()
		{

			var results = new KeyValueList();
			// If context user exist then...
			string currentUser = string.Empty;
			if (System.Web.HttpContext.Current.User != null)
			{
				if (System.Web.HttpContext.Current.User.Identity.IsAuthenticated)
				{
					currentUser = System.Web.HttpContext.Current.User.Identity.Name;
				}
			}
			results.Add("FormsCookieName", FormsAuthentication.FormsCookieName);
			//FormsAuthentication.GetAuthCookie();
			HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
			if (cookie.Value.Length > 0)
			{
				var ticket = FormsAuthentication.Decrypt(cookie.Value);
			}

			string v = string.Empty;
			for (int i = 0; i < cookie.Values.AllKeys.Length; i++)
			{
				v += cookie.Values.AllKeys[i] + ", ";
			}
			results.Add("values", cookie.Value.ToString());
			//System.Web.HttpContext.Current.Response.Cookies;
			results.Add("IdentityName", System.Threading.Thread.CurrentPrincipal.Identity.Name);
			results.Add("CurrentUser", currentUser);
			return results;
		}

		[WebMethod(EnableSession = true, Description = "Logout and abandon session.")]
		public KeyValueList Logout()
		{
			//// Abandon ASP Session first.
			//JocysCom.WebSites.WebApp.ServerVariables.Exchanger exchanger;
			//exchanger = new JocysCom.WebSites.WebApp.ServerVariables.Exchanger();
			//exchanger.AbandonAspSession();
			// Abandon ASPX Session.
			System.Web.Security.FormsAuthentication.SignOut();
			Session.Abandon();
			var results = new KeyValueList();
			results.Add("Status", true);
			return results;
		}

		//[WebMethod(EnableSession = true, Description = "Supply aditional user information.")]
		//public KeyValueList SetClientInfo(string name, string value)
		//{
		//	switch (name)
		//	{
		//		case "ClientDate":
		//			setClientDate(value);
		//			break;
		//		default:
		//			break;
		//	}
		//	var results = new KeyValueList();
		//	results.Add("Status", true);
		//	return results;
		//}

		//private bool SetClientDate(string clientDate)
		//{
		//	//=================================================
		//	// Set client time offset in minutes.
		//	//-------------------------------------------------
		//	System.Text.RegularExpressions.Regex regEx = new System.Text.RegularExpressions.Regex(".*([ +-])([0-9][0-9]):([0-9][0-9])$");
		//	System.Text.RegularExpressions.MatchCollection mc = regEx.Matches(clientDate);
		//	int offsetMin = 0;
		//	if (mc.Count > 0)
		//	{
		//		int p = int.Parse(regEx.Replace(mc[0].Value, "$1") + "1");
		//		int h = int.Parse(regEx.Replace(mc[0].Value, "$2"));
		//		int m = int.Parse(regEx.Replace(mc[0].Value, "$3"));
		//		offsetMin = p * (h * 60 + m);
		//		//Page.Controls.Add(new LiteralControl(clientDate + " / " + p + " " + h + ":" + m + "<br />"));
		//		//Page.Controls.Add(new LiteralControl(offsetMin + "<br />"));
		//	}
		//	SessionContext.Current.ClientTimeOffset = offsetMin;
		//	//=================================================
		//	// Set Server time offset in minutes.
		//	//-------------------------------------------------
		//	DateTime serverTime = DateTime.Now;
		//	TimeZone localZone = TimeZone.CurrentTimeZone;
		//	SessionContext.Current.ServerTimeOffset = int.Parse(localZone.GetUtcOffset(serverTime).TotalMinutes.ToString());
		//	return true;
		//}


	}

}