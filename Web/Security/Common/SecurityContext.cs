using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Collections.Generic;
using JocysCom.WebSites.Engine.Security;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security
{

	/// <summary>
	/// Summary description for SesContext
	/// </summary>
	[Serializable]
	public class SecurityContext : IDisposable
	{
		public SecurityContext()
		{
			initRefreshData();
		}

		#region App Settings

		public string prefix = "Security_";

		public int LoginRememberMinutes;
		public List<string> Administrators;
		public List<string> PowerUsers;
		public bool AllowUsersToSignUp;
		public bool AllowUsersToLogin;
		public bool AllowUsersToResetPassword;
		public UserFieldName RequiredFields;
		public UserFieldName OptionalFields;
		public string DefaultRole;

		void LoadAppSettings()
		{
			LoginRememberMinutes = ParseInt(prefix+"LoginRememberMinutes", 24*60);
			Administrators = ParseList(prefix + "Administrators", new List<string>());
			PowerUsers = ParseList(prefix + "PowerUsers", new List<string>());
			AllowUsersToResetPassword = ParseBool(prefix + "AllowUsersToResetPassword", false);
			AllowUsersToSignUp = ParseBool(prefix + "AllowUsersToSignUp", false);
			AllowUsersToLogin = ParseBool(prefix + "AllowUsersToLogin", false);
			RequiredFields = ParseEnum<UserFieldName>(prefix + "RequiredFields", 0);
			OptionalFields = ParseEnum<UserFieldName>(prefix + "OptionalFields", 0);
			DefaultRole = ParseString(prefix + "DefaultRole", null);
		}

		public T ParseEnum<T>(string name, T defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : (T)Enum.Parse(typeof(T), v);
		}

		public static bool ParseBool(string name, bool defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : bool.Parse(v);
		}

		public static List<string> ParseList(string name, List<string> defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) 
				? defaultValue
				: ParseString(name, "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		}

		public static string ParseString(string name, string defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : v;
		}

		public static int ParseInt(string name, int defaultValue)
		{
			var v = ConfigurationManager.AppSettings[name];
			return (v == null) ? defaultValue : int.Parse(v);
		}

		#endregion

		SessionList _Sessions;
		public SessionList Sessions
		{
			get { return _Sessions = _Sessions ?? new SessionList(); }
			set { _Sessions = value; }
		}

		private static string sessionKey = "SecurityContext";

		public static SecurityContext Current
		{
			set { HttpContext.Current.Session[sessionKey] = value; }
			get
			{
				SecurityContext context = null;
				if (HttpContext.Current != null)
					if (HttpContext.Current.Session != null)
					{
						object ctx = HttpContext.Current.Session[sessionKey];
						if (ctx == null)
						{
							context = new SecurityContext();
							HttpContext.Current.Session[sessionKey] = context;
						}
						else
						{
							context = (SecurityContext)ctx;
						}
					}
				return context;
			}
		}

		public string DefaultCulture = "en";

		private string _CurrentCulture;

		public string CurrentLanguage
		{
			get
			{
				if (string.IsNullOrEmpty(_CurrentCulture))
				{
					// Default site language is English.
					// Must be mapped to IP and available languages.
					_CurrentCulture = DefaultCulture;
				}
				return _CurrentCulture;
			}
			set { _CurrentCulture = value; }
		}

		public bool IsBotAgent
		{
			get
			{
				string userAgent = HttpContext.Current.Request.UserAgent.ToLower();
				string[] botKeywords = new string[] {
					"bot", "spider", "google", "yahoo",
					"search", "crawl", "slurp", "msn",
					"teoma", "ask.com", "yandex", "bing" };
				foreach (string bot in botKeywords)
				{
					if (userAgent.Contains(bot)) return true;
				}
				return false;
			}
		}

		public int ClientTimeOffset;
		public int ServerTimeOffset;
		public string CurrentUserName;

		public bool CookieRememberMe
		{
			get
			{
				bool remember = false;
				HttpCookie cookie = System.Web.HttpContext.Current.Request.Cookies["Settings.RememberMe"];
				if (cookie != null) { bool.Parse(cookie.Value); }
				return remember;
			}
		}

		/// <summary>
		/// Restore user configuration by username.
		/// </summary>
		/// <param name="username"></param>
		public string InitConfigOnValidationSuccess(string username)
		{
			System.Diagnostics.Trace.WriteLine("Restore Session Data of '" + username + "' user");
			// Set current user value.    
			CurrentUserName = username;
			string sessionId = System.Web.HttpContext.Current.Session.SessionID;
			Security.SessionData sessionData = Sessions.GetValue(sessionId);
			sessionData.UserExtraInfoRow = null;
			sessionData.LastUpdateDate = DateTime.Now;
			Sessions.Items[sessionId] = sessionData;
			//WebConfig.Current.DepartmentId = 2;
			string createStatus = Security.Check.CheckAndInitUser();
			string returnMessage = string.Empty;
			if (createStatus.IndexOf("Success") == -1)
			{
				returnMessage = "Error: Membership failed [" + createStatus + "]<br />";
			}
			else
			{
				returnMessage += "Membership: " + createStatus + "<br />";
			}

			return returnMessage;
		}

		//#region Member

		private User m_member;
		public User Member
		{
			get
			{
				if (m_member == null && IsAuthenticated)
				{
					var db = new SecurityEntities();
					m_member = db.Users.Where(x => x.UserName == HttpContext.Current.User.Identity.Name).FirstOrDefault();
					db.Dispose();
					db = null;
				}
				return m_member;
			}
		}

		public bool IsAuthenticated
		{
			get
			{
				bool success = true;
				if (success) success &= (HttpContext.Current != null);
				if (success) success &= (HttpContext.Current.User != null);
				if (success) success &= (HttpContext.Current.User.Identity.IsAuthenticated);
				return success;
			}
		}

		public bool IsAdministrator
		{
			get { return IsAuthenticated && Roles.IsUserInRole(BuildInRoles.Administrators.ToString()); }
		}

		//#endregion

		#region Reload Data


		/// <summary>
		/// This lock will protect application shared data from simultaneous access.
		/// </summary>
		private System.Threading.ReaderWriterLock m_dataLock;
		private DateTime m_dataModified;

		private void initRefreshData()
		{
			// Create properties first.
			this.m_dataLock = new System.Threading.ReaderWriterLock();
			this.m_dataModified = DateTime.Now;
			LoadAppSettings();
		}

		/// <summary>
		/// Reload data.
		/// </summary>
		public void RefreshData()
		{
			// Wait for lock 10 min maximum.
			this.m_dataLock.AcquireWriterLock(10 * 60 * 1000);
			//-----------------------------------------------------
			//refreshMember();
			//-----------------------------------------------------
			this.m_dataModified = DateTime.Now;
			this.m_dataLock.ReleaseWriterLock();
		}

		#endregion

		/// <summary>
		/// Initialize config then session starts.
		/// </summary>
		public void InitOnSessionStart(object sender, EventArgs e)
		{
		}

		#region Dispose

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources.
			}
			// Free native resources.
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(true);
		}

		#endregion

	}
}