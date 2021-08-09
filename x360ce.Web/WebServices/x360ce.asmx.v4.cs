using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Security;
using x360ce.Engine.Data;
using x360ce.Engine;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Mail;
using System.Net;
using System.Runtime.CompilerServices;

namespace x360ce.Web.WebServices
{
	public partial class x360ce
	{
		#region Security

		public void SignOut(out int errorCode, out string errorMessage)
		{
			FormsAuthentication.SignOut();
			var values = new KeyValueList();
			errorCode = 0;
			errorMessage = "Good bye!";
		}

		public void SignIn(string username, string password, out int errorCode, out string errorMessage)
		{
			errorCode = 0;
			errorMessage = null;
			if (string.IsNullOrEmpty(password))
				errorMessage = "Please enter password";
			if (string.IsNullOrEmpty(username))
				errorMessage = "Please enter user name";
			if (string.IsNullOrEmpty(errorMessage))
			{
				// Here must be validation with password. You can add third party validation here;
				bool success = Membership.ValidateUser(username, password);
				if (!success)
					errorMessage = string.Format("Validation failed. User name '{0}' was not found.", username);
			}
			var values = new KeyValueList();
			if (!string.IsNullOrEmpty(errorMessage))
			{
				errorCode = 1;
				return;
			}
			FormsAuthentication.Initialize();
			var user = Membership.GetUser(username, true);
			if (user == null)
			{
				errorCode = 1;
				errorMessage = string.Format("'{0}' was not found.", username);
				return;
			}
			var roles = Roles.GetRolesForUser(username);
			string rolesString = string.Empty;
			for (int i = 0; i < roles.Length; i++)
			{
				if (i > 0)
					rolesString += ",";
				rolesString += roles[i];
			}
			var loginRememberMinutes = 30;
			var ticket = new FormsAuthenticationTicket(1, user.UserName, DateTime.Now, DateTime.Now.AddMinutes(loginRememberMinutes), true, rolesString, FormsAuthentication.FormsCookiePath);
			// Encrypt the cookie using the machine key for secure transport.
			var hash = FormsAuthentication.Encrypt(ticket);
			var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, hash); // Hashed ticket
			if (ticket.IsPersistent)
			{
				cookie.Expires = ticket.Expiration;
			}
			HttpContext.Current.Response.Cookies.Add(cookie);
			// Create Identity.
			var identity = new System.Security.Principal.GenericIdentity(user.UserName);
			// Create Principal.
			var principal = new RolePrincipal(identity);
			System.Threading.Thread.CurrentPrincipal = principal;
			// Create User.
			HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(identity, roles);
			errorMessage = "Welcome!";
		}

		#endregion

		[WebMethod(EnableSession = true, Description = "Get default list of games.")]
		public SettingsData GetSettingsData()
		{
			var data = new SettingsData();
			var db = new x360ceModelContainer();
			data.Programs = db.Programs.Where(x => x.IsEnabled && x.InstanceCount > 1).ToList();
			db.Dispose();
			db = null;
			return data;
		}

		[WebMethod(EnableSession = true, Description = "Get vendors of controllers.")]
		public List<Vendor> GetVendors()
		{
			var db = new x360ceModelContainer();
			var q = db.Vendors.ToList()
				.Select(x => new Vendor
			{
				VendorId = x.VendorId,
				VendorName = x.VendorName,
				ShortName = x.ShortName,
				WebSite = x.WebSite
			});
			var vendors = q.ToList();
			db.Dispose();
			db = null;
			return vendors;
		}

		public class LogEntry
		{
			public string Address;
			public DateTime Date;
		}

		public static List<LogEntry> IpDate = new List<LogEntry>();


		[WebMethod(EnableSession = true, Description = "Update User Data")]
		//[System.Web.Services.Protocols.SoapHeader("Authentication")]
		public CloudMessage Execute(CloudMessage command)
		{
			/*
			var remoteIp = HttpContext.Current.Request.UserHostAddress;
			var now = DateTime.Now;
			lock (IpDate)
			{
				IpDate.Add(new LogEntry() { Address = remoteIp, Date = now });
				IpDate.RemoveAll(x => x.Date < now.AddMinutes(-5));
				var count = IpDate.Count(x => x.Address == remoteIp);
				// If 30 connections per 5 minutes.
				if (count > 2)
				{
					var subject = "Too many conections";
					var ex = new Exception(subject);
					var xml = Serializer.SerializeToXmlString(command);
					ex.Data.Add("Execute CloudMessage command", xml);
					LogHelper.Current.ProcessException(ex, subject);
					IpDate.RemoveAll(x => x.Address == remoteIp);
				}
			}
			*/

			var results = new CloudMessage();
			// Output messages.
			var messages = new List<string>();
			try
			{
				JocysCom.WebSites.Engine.Security.Data.User user;
				string error;
				bool fixSuccess;
				switch (command.Action)
				{
					case CloudAction.LogIn:
						// Action requires valid user.
						user = CloudHelper.GetUser(command, out error);
						if (user == null)
						{
							messages.Add("Not authorized");
							results.ErrorCode = (int)CloudErrorCode.Error;
						}
						break;
					case CloudAction.GetPublicRsaKey:
						AddRsaPublicKey(results);
						break;
					case CloudAction.Insert:
					case CloudAction.Update:
						// Insert or update user records.
						fixSuccess = command.FixComputerId(out error);
						if (fixSuccess)
						{
							DatabaseHelper.Upsert(command, messages);
						}
						else
						{
							messages.Add(error);
							results.ErrorCode = (int)CloudErrorCode.Error;
						}
						break;
					case CloudAction.Select:
						// Select user records.
						fixSuccess = command.FixComputerId(out error);
						if (fixSuccess)
						{
							DatabaseHelper.Select(command, results, messages, out error);
						}
						else
						{
							messages.Add(error);
							results.ErrorCode = (int)CloudErrorCode.Error;
						}
						break;
					case CloudAction.Delete:
						// Delete user records.
						fixSuccess = command.FixComputerId(out error);
						if (fixSuccess)
						{
							DatabaseHelper.Delete(command, messages);
						}
						else
						{
							messages.Add(error);
							results.ErrorCode = (int)CloudErrorCode.Error;
						}
						break;
					case CloudAction.CheckUpdates:
						var clientVersion = command.Values.GetValue<string>(CloudKey.ClientVersion);
						results.Values.Add(CloudKey.ServerVersion, clientVersion);
						//results.Values.Add(CloudKey.UpdateUrl, "https://github.com/x360ce/x360ce/blob/master/x360ce.Web/Files/x360ce.zip?raw=true");
						results.Values.Add(CloudKey.UpdateUrl, JocysCom.ClassLibrary.Security.TokenHelper.GetApplicationUrl() + "/Files/x360ce_beta.zip");
						break;
					case CloudAction.SendMailMessage:
						foreach (var message in command.MailMessages)
						{
							var mm = message.ToMailMessage();
							mm.Bcc.Clear();
							mm.CC.Clear();
							mm.Sender = null;
							mm.To.Clear();
							mm.To.Add("support@x360ce.com");
							SmtpClientEx.Current.Send(mm);
						}
						break;
					default:
						break;
				}
				results.ErrorMessage = string.Join("\r\n", messages.Where(x => !string.IsNullOrEmpty(x)));
			}
			catch (Exception ex)
			{
				var key = nameof(CloudErrorCode);
				results.ErrorMessage = "Server: " + ex.Message;
				results.ErrorCode = (int)CloudErrorCode.Error;
				if (ex.Data.Contains(key) && Equals(ex.Data[key], CloudErrorCode.UnableToDecrypt))
				{
					results.ErrorCode = (int)CloudErrorCode.UnableToDecrypt;
					AddRsaPublicKey(results);
				}
			}
			return results;
		}

		private void AddRsaPublicKey(CloudMessage results)
		{
			var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
			if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
				rsa.RsaNewKeysSave(2048);
			results.Values.Add(CloudKey.RsaPublicKey, rsa.RsaPublicKeyValue);
		}

		#region Programs

		[WebMethod(EnableSession = true, Description = "Search games.")]
		public Program GetProgram(string fileName, string fileProductName)
		{
			var db = new x360ceModelContainer();
			var o = db.Programs.FirstOrDefault(x => x.FileName == fileName && x.FileProductName == fileProductName);
			if (o != null)
				return o;
			o = db.Programs.FirstOrDefault(x => x.FileName == fileName);
			db.Dispose();
			db = null;
			return o;
		}

		[WebMethod(EnableSession = true, Description = "Save program settings.")]
		public string SetProgram(Program p)
		{
			if (HttpContext.Current.User.Identity.IsAuthenticated)
			{
				var db = new x360ceModelContainer();
				var o = db.Programs.FirstOrDefault(x => x.FileName == p.FileName && x.FileProductName == p.FileProductName);
				if (o == null)
				{
					o = new Program();
					o.ProgramId = Guid.NewGuid();
					o.DateCreated = DateTime.Now;
					o.FileName = p.FileName;
					o.FileProductName = p.FileProductName;
					db.Programs.AddObject(o);
				}
				else
				{
					o.DateUpdated = DateTime.Now;
				}
				o.HookMask = p.HookMask;
				o.InstanceCount = p.InstanceCount;
				o.IsEnabled = p.IsEnabled;
				o.XInputMask = p.XInputMask;
				db.SaveChanges();
				db.Dispose();
				db = null;
				return "";
			}
			else
			{
				return "User was not authenticated.";
			}
		}

		[WebMethod(EnableSession = true, Description = "Get list of games.")]
		public List<Program> GetProgramsDefault()
		{
			return GetPrograms(EnabledState.Enabled, 2);
		}

		List<Program> GetOverridePrograms()
		{
			List<Program> programs = null;
			var key = "OverridePrograms";
			var settings = System.Web.Configuration.WebConfigurationManager.AppSettings;
			if (settings.AllKeys.Contains(key))
			{
				var path = settings[key];
				var fileName = Server.MapPath(path);
				if (System.IO.File.Exists(fileName))
				{
					programs = Serializer.DeserializeFromXmlFile<List<Program>>(fileName);
				}
			}
			return programs;
		}

		[WebMethod(EnableSession = true, Description = "Get list of games.")]
		public List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount)
		{
			List<Program> programs = null;
			try
			{
				programs = GetOverridePrograms();
			}
			catch (Exception ex)
			{
				var x = ex;
			}
			if (programs == null)
			{
				var db = new x360ceModelContainer();
				IQueryable<Program> list = db.Programs;
				if (isEnabled == EnabledState.Enabled)
					list = list.Where(x => x.IsEnabled);
				else if (isEnabled == EnabledState.Disabled)
					list = list.Where(x => !x.IsEnabled);
				if (minInstanceCount > 0)
					list = list.Where(x => x.InstanceCount == minInstanceCount);
				programs = list.ToList();
				db.Dispose();
				db = null;
			}
			return programs;
		}


		#endregion


	}
}
