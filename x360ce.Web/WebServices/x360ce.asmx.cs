using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Linq.Expressions;
using System.Web.Security;
using x360ce.Engine.Data;
using x360ce.Engine;
using x360ce.App;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using JocysCom.ClassLibrary.Data;
using JocysCom.ClassLibrary.Runtime;

namespace x360ce.Web.WebServices
{
	/// <summary>
	/// Summary description for x360ce
	/// </summary>
	[WebService(Namespace = "http://x360ce.com/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	[System.Web.Script.Services.ScriptService]
	public class x360ce : System.Web.Services.WebService, IWebService
	{

		#region Settings

		/// <summary>
		/// Save controller settings.
		/// </summary>
		/// <param name="s">Setting object which contains information about DirectInput device and file/game name it is used for.</param>
		/// <param name="ps">PAD settings which contains mapping between DirectInput device and virtual XBox controller.</param>
		/// <returns>Status of operation. Empty if success.</returns>
		[WebMethod(EnableSession = true, Description = "Save controller settings.")]
		public string SaveSetting(Setting s, PadSetting ps)
		{
			var db = new x360ceModelContainer();
			var checksum = ps.CleanAndGetCheckSum();
			// Update checksum.
			ps.PadSettingChecksum = checksum;
			// Look for existing PadSetting.
			var pDB = db.PadSettings.FirstOrDefault(x => x.PadSettingChecksum == checksum);
			// If PadSetting doesn't exists then...
			if (pDB == null)
			{
				pDB = ps;
				pDB.EntityKey = null;
				db.PadSettings.AddObject(pDB);
			}
			// Look for existing setting.
			var sDB = db.Settings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			var n = DateTime.Now;
			if (sDB == null)
			{
				sDB = s;
				sDB.EntityKey = null;
				// Assign brand new ID.
				s.SettingId = Guid.NewGuid();
				s.DateCreated = n;
				// Link PadSetting with setting.
				s.PadSettingChecksum = pDB.PadSettingChecksum;
				db.Settings.AddObject(sDB);
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return "";
		}

		/// <summary>
		/// Search controller settings.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		[WebMethod(EnableSession = true, Description = "Search controller settings.")]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			// Create default value to reply.
			var sr = new SearchResult();
			sr.Presets = new Preset[0];
			sr.PadSettings = new PadSetting[0];
			sr.Summaries = new Summary[0];
			sr.Settings = new Setting[0];
			// Create database.
			var db = new x360ceModelContainer();
			// Get user instances.
			var p = SearchParameterCollection.GetSqlParameter(args);
			// Get most popular settings for user controllers.
			var products = args.Where(x => x.ProductGuid != Guid.Empty).Select(x => x.ProductGuid).Distinct().ToArray();
			// Get all device instances of the user.
			var hasInstances = args.Any(x => x.InstanceGuid != Guid.Empty);
			if (hasInstances)
			{
				var ds = EngineHelper.GetSettings(args);
				sr.Settings = SqlHelper.ConvertToList<Setting>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			else if (products.Length > 0)
			{
				// Get presets.
				var ds = EngineHelper.GetPresets(args);
				sr.Summaries = SqlHelper.ConvertToList<Summary>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			else if (args != null && args.Length > 0)
			{
				// Get presets.
				var ds = EngineHelper.GetPresets(new SearchParameter[0]);
				sr.Presets = SqlHelper.ConvertToList<Preset>(ds.Tables[0]).ToArray();
				sr.PadSettings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToArray();
			}
			db.Dispose();
			db = null;
			return sr;
		}

		[WebMethod(EnableSession = true, Description = "Delete controller settings.")]
		public string DeleteSetting(Setting s)
		{
			var db = new x360ceModelContainer();
			var setting = db.Settings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			if (setting == null) return "Setting not found";
			db.Settings.DeleteObject(setting);
			db.SaveChanges();
			db.Dispose();
			db = null;
			return "";
		}

		/// <summary>
		/// Load controller settings.
		/// </summary>
		/// <param name="checksum">List of unique identifiers of PAD setting</param>
		/// <returns>List of PAD settings.</returns>
		[WebMethod(EnableSession = true, Description = "Load controller settings.")]
		public SearchResult LoadSetting(Guid[] checksum)
		{
			var sr = new SearchResult();
			var db = new x360ceModelContainer();
			sr.PadSettings = db.PadSettings.Where(x => checksum.Contains(x.PadSettingChecksum)).ToArray();
			db.Dispose();
			db = null;
			return sr;
		}

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

		#endregion

		#region Vendors

		[WebMethod(EnableSession = true, Description = "Get vendors of controllers.")]
		public List<Vendor> GetVendors()
		{
			var db = new x360ceModelContainer();
			var q = from row in db.Vendors
					select new Vendor
					{
						VendorId = row.VendorId,
						VendorName = row.VendorName,
						ShortName = row.ShortName,
						WebSite = row.WebSite
					};
			var vendors = q.ToList();
			db.Dispose();
			db = null;
			return vendors;
		}

		#endregion

		#region Programs

		[WebMethod(EnableSession = true, Description = "Search games.")]
		public Program GetProgram(string fileName, string fileProductName)
		{
			var db = new x360ceModelContainer();
			var o = db.Programs.FirstOrDefault(x => x.FileName == fileName && x.FileProductName == fileProductName);
			if (o != null) return o;
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
			List<Program> programs = new List<Program>();
			var key = "OverridePrograms";
			var settings = System.Web.Configuration.WebConfigurationManager.AppSettings;
			if (settings.AllKeys.Contains(key))
			{
				var path = settings[key];
				var fileName = Server.MapPath(path);
				if (System.IO.File.Exists(fileName))
				{
					var xml = System.IO.File.ReadAllText(fileName);
					programs = Serializer.DeserializeFromXmlString<List<Program>>(xml, System.Text.Encoding.UTF8);
				}
			}
			return programs;
		}

		[WebMethod(EnableSession = true, Description = "Get list of games.")]
		public List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount)
		{
			var programs = GetOverridePrograms();
			if (programs == null)
			{
				var db = new x360ceModelContainer();
				IQueryable<Program> list = db.Programs;
				if (isEnabled == EnabledState.Enabled) list = list.Where(x => x.IsEnabled);
				else if (isEnabled == EnabledState.Disabled) list = list.Where(x => !x.IsEnabled);
				if (minInstanceCount > 0) list = list.Where(x => x.InstanceCount == minInstanceCount);
				programs = list.ToList();
				db.Dispose();
				db = null;
			}
			return programs;
		}


		#endregion

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
			if (string.IsNullOrEmpty(password)) errorMessage = "Please enter password";
			if (string.IsNullOrEmpty(username)) errorMessage = "Please enter user name";
			if (string.IsNullOrEmpty(errorMessage))
			{
				// Here must be validation with password. You can add third party validation here;
				bool success = Membership.ValidateUser(username, password);
				if (!success) errorMessage = string.Format("Validation failed. User name '{0}' was not found.", username);
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
				if (i > 0) rolesString += ",";
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

		[WebMethod(EnableSession = true, Description = "Update User Data")]
		//[System.Web.Services.Protocols.SoapHeader("Authentication")]
		public CloudMessage Execute(CloudMessage command)
		{
			var results = new CloudMessage();
			var messages = new List<string>();
			try
			{
				JocysCom.WebSites.Engine.Security.Data.User user;
				string error;
				Guid? computerId;
				switch (command.Action)
				{
					case CloudAction.LogIn:
						// Action requires valid user.
						user = CloudHelper.GetUser(command, out error);
						if (user == null)
						{
							messages.Add("Not authorised");
							results.ErrorCode = 2;
						}
						break;
					case CloudAction.GetPublicRsaKey:
						var rsa = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud);
						if (string.IsNullOrEmpty(rsa.RsaPublicKeyValue))
						{
							rsa.RsaNewKeysSave(2048);
						}
						results.Values = new KeyValueList();
						results.Values.Add(CloudKey.RsaPublicKey, rsa.RsaPublicKeyValue);
						break;
					case CloudAction.Delete:
						// Action requires valid user.
						computerId = CloudHelper.GetComputerId(command, out error);
						if (computerId.HasValue)
						{
							foreach (var item in command.UserGames)
								item.ComputerId = computerId.Value;
							foreach (var item in command.UserDevices)
								item.ComputerId = computerId.Value;
							messages.Add(Delete(command.UserDevices));
							messages.Add(Delete(command.UserGames));
						}
						else
						{
							messages.Add(error);
							results.ErrorCode = 2;
						}
						break;
					case CloudAction.Insert:
					case CloudAction.Update:
						computerId = CloudHelper.GetComputerId(command, out error);
						if (computerId.HasValue)
						{
							// Fix computer id
							foreach (var item in command.UserGames)
								item.ComputerId = computerId.Value;
							foreach (var item in command.UserDevices)
								item.ComputerId = computerId.Value;
							// Games can be inserted by using computer id only.
							messages.Add(Upsert(command.UserGames));
							messages.Add(Upsert(command.UserDevices));
						}
						else
						{
							messages.Add(error);
							results.ErrorCode = 2;
						}
						break;
					default:
						break;
				}
				results.ErrorMessage = string.Join("\r\n", messages.Where(x => !string.IsNullOrEmpty(x)));
			}
			catch (Exception ex)
			{
				results.ErrorCode = 1;
				results.ErrorMessage = "Server: " + ex.Message;
			}
			return results;
		}

		#region Maintain: User Controllers

		string Delete(List<UserDevice> items)
		{
			var db = new x360ceModelContainer();
			var deleted = 0;
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var instanceGuid = item.InstanceGuid;
				var currentItem = db.UserDevices.FirstOrDefault(x => x.InstanceGuid == instanceGuid);
				if (currentItem == null) continue;
				db.UserDevices.DeleteObject(currentItem);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return string.Format("{0}s: {1} deleted.", items.GetType().GetGenericArguments()[0].Name, deleted);
		}

		string Upsert(List<UserDevice> items)
		{
			var db = new x360ceModelContainer();
			var created = 0;
			var updated = 0;
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var instanceGuid = item.InstanceGuid;
				var dbItem = db.UserDevices.FirstOrDefault(x => x.InstanceGuid == instanceGuid);
				if (dbItem == null)
				{
					created++;
					item.Id = Guid.NewGuid();
					item.DateCreated = DateTime.Now;
					item.DateUpdated = item.DateCreated;
					db.UserDevices.AddObject(item);
				}
				else
				{
					updated++;
					// Fix dates.
					item.DateCreated = dbItem.DateCreated;
					Helper.CopyDataMembers(item, dbItem, true);
					dbItem.DateUpdated = DateTime.Now;
				}
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return string.Format("{0}s: {1} created, {2} updated.", items.GetType().GetGenericArguments()[0].Name, created, updated);
		}

		#endregion

		#region Maintain: User Games

		string Delete(List<UserGame> items)
		{

			var db = new x360ceModelContainer();
			var deleted = 0;
			for (int i = 0; i < items.Count; i++)
			{
				var game = items[i];
				var computerId = game.ComputerId;
				var fileName = game.FileName;
				var currentGame = db.UserGames.FirstOrDefault(x => x.ComputerId == computerId && x.FileName == fileName);
				if (currentGame == null) continue;
				db.UserGames.DeleteObject(currentGame);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return string.Format("{0}s: {1} deleted.", items.GetType().GetGenericArguments()[0].Name, deleted);
		}

		string Upsert(List<UserGame> items)
		{
			var db = new x360ceModelContainer();
			var created = 0;
			var updated = 0;
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var computerId = item.ComputerId;
				var fileName = item.FileName;
				var dbItem = db.UserGames.FirstOrDefault(x => x.ComputerId == computerId && x.FileName == fileName);
				if (dbItem == null)
				{
					created++;
					item.GameId = Guid.NewGuid();
					item.DateCreated = DateTime.Now;
					item.DateUpdated = item.DateCreated;
					db.UserGames.AddObject(item);
				}
				else
				{
					updated++;
					// Fix dates.
					item.DateCreated = dbItem.DateCreated;
					Helper.CopyDataMembers(item, dbItem, true);
					dbItem.DateUpdated = DateTime.Now;
				}
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return string.Format("{0}s: {1} created, {2} updated.", items.GetType().GetGenericArguments()[0].Name, created, updated);
		}

		#endregion

	}

}
