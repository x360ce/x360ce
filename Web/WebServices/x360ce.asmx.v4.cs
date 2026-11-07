using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
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

		[WebMethod(EnableSession = true, Description = "Get the service's version and clock, and whether its database answers. For testing the address.")]
		public ServerInfo GetServerInfo()
		{
			return EngineHelper.GetServerInfo();
		}

		[WebMethod(EnableSession = true, Description = "Get vendors of controllers.")]
		public List<Vendor> GetVendors()
		{
			var db = new x360ceModelContainer();
			// Read first: EF6 refuses to project into the entity type inside the query.
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


		/// <summary>What a program that the service cannot serve is told, with no error so that it stops sending.</summary>
		public const string OutdatedProgramMessage = "Nothing was done: this version of x360ce is too old for the online database. "
			+ "Download the latest from https://github.com/x360ce/x360ce/releases";

		[WebMethod(EnableSession = true, Description = "Update User Data")]
		//[System.Web.Services.Protocols.SoapHeader("Authentication")]
		public CloudMessage Execute(CloudMessage command)
		{
			var reply = OutdatedProgramReply(command);
			if (reply != null)
				return reply;
			var repeated = KeepNewestValues(command);
			reply = Run(command);
			// A message that repeats its keys comes from a program that sends it again until it hears no error.
			if (repeated && reply.ErrorCode != (int)CloudErrorCode.None)
			{
				reply.ErrorCode = (int)CloudErrorCode.None;
				reply.ErrorMessage = OutdatedProgramMessage + " (" + reply.ErrorMessage + ")";
			}
			return reply;
		}

		/// <summary>
		/// The answer for a message from a program older than June 2018, which the service cannot serve: success, with
		/// nothing done; null for any other message.
		/// </summary>
		/// <remarks>
		/// Every stored record belongs to a computer and a profile, and programs before June 2018 send no profile ID.
		/// Answered with an error, such a program sends the same message again at once and for ever, adding its keys
		/// to it every time, so one of them grew its message to 3 MB and sent it every few seconds for hours. It stops
		/// only when it hears no error.
		/// </remarks>
		/// <param name="command">The message as it arrived.</param>
		public static CloudMessage OutdatedProgramReply(CloudMessage command)
		{
			if (command == null || command.Values == null)
				return null;
			switch (command.Action)
			{
				case CloudAction.Insert:
				case CloudAction.Update:
				case CloudAction.Select:
				case CloudAction.Delete:
					break;
				default:
					return null;
			}
			// Such a program sends its computer ID and no profile ID; a message with neither is answered as before.
			var keys = command.Values.Select(x => x.Key as string).ToList();
			if (!keys.Contains(CloudKey.ComputerId) || keys.Contains(CloudKey.ProfileId))
				return null;
			return new CloudMessage { ErrorCode = (int)CloudErrorCode.None, ErrorMessage = OutdatedProgramMessage };
		}

		/// <summary>
		/// Keeps only the last value of each key, which the program added on its latest try, and says whether any key
		/// was there more than once.
		/// </summary>
		/// <remarks>
		/// Programs before 4.14 (2020) add their keys again each time they send a message again, and read an error as
		/// "send it again". The keys of one try belong together: the computer ID is encrypted with that try's password.
		/// </remarks>
		/// <param name="command">The message as it arrived; its values are replaced when a key repeats.</param>
		public static bool KeepNewestValues(CloudMessage command)
		{
			if (command == null || command.Values == null)
				return false;
			var newest = new Dictionary<string, KeyValue>();
			foreach (var item in command.Values)
				newest[item.Key as string ?? ""] = item;
			if (newest.Count == command.Values.Count)
				return false;
			var values = new KeyValueList();
			values.AddRange(command.Values.Where(x => newest[x.Key as string ?? ""] == x));
			command.Values = values;
			return true;
		}

		/// <summary>Does what the message asks.</summary>
		CloudMessage Run(CloudMessage command)
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
				string error;
				bool fixSuccess;
				switch (command.Action)
				{
					// The service has no user accounts: settings belong to a computer and profile ID.
					case CloudAction.LogIn:
						messages.Add("Not authorized");
						results.ErrorCode = (int)CloudErrorCode.Error;
						break;
					case CloudAction.GetPublicRsaKey:
						if (!AddRsaPublicKey(results))
						{
							messages.Add(MissingKeyMessage);
							results.ErrorCode = (int)CloudErrorCode.Error;
						}
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
					// No program calls this; the updater reads GitHub Releases (UpdateClient).
					// It echoes the client's own version as ServerVersion, so it never announces
					// an update, and it names /Files/x360ce_beta.zip, which is not in the
					// repository.
					case CloudAction.CheckUpdates:
						var clientVersion = command.Values.GetValue<string>(CloudKey.ClientVersion);
						results.Values.Add(CloudKey.ServerVersion, clientVersion);
						//results.Values.Add(CloudKey.UpdateUrl, "https://github.com/x360ce/x360ce/blob/master/x360ce.Web/Files/x360ce.zip?raw=true");
						results.Values.Add(CloudKey.UpdateUrl, new Uri(HttpContext.Current.Request.Url, HttpContext.Current.Request.ApplicationPath) + "/Files/x360ce_beta.zip");
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
					if (AddRsaPublicKey(results))
						results.ErrorCode = (int)CloudErrorCode.UnableToDecrypt;
					else
						results.ErrorMessage = MissingKeyMessage;
				}
			}
			return results;
		}

		/// <summary>What the service answers when its Web.config holds no key pair.</summary>
		public const string MissingKeyMessage = "Server: the service has no encryption key. "
			+ "Its Web.config must hold CloudRsaPublicKey and CloudRsaPrivateKey.";

		/// <summary>Adds the service's public key to a reply; false when the service has none.</summary>
		/// <remarks>
		/// The key pair is read from the application's settings and never made here. Every program keeps the key it
		/// was given. A new pair, made because a publish left the settings without one and written into a Web.config
		/// that the next publish replaces, changes the key under every program: programs from 4.14 fail once and take
		/// the new key, older ones lose the message they were sending.
		/// </remarks>
		static bool AddRsaPublicKey(CloudMessage results)
		{
			var key = new JocysCom.ClassLibrary.Security.Encryption(CloudKey.Cloud).RsaPublicKeyValue;
			if (string.IsNullOrEmpty(key))
				return false;
			results.Values.Add(CloudKey.RsaPublicKey, key);
			return true;
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
