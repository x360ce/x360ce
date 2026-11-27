using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>The web service's records in the database. Each belongs to one computer and profile.</summary>
	public partial class DatabaseHelper
	{
		/// <summary>Execute INSERT/UPDATE commands on database.</summary>
		public static void Upsert(CloudMessage command, List<string> messages)
		{
			if (command.UserGames != null)
				messages.Add(Upsert(command.UserGames));
			if (command.UserDevices != null)
			{
				UserDevice.StoreInputSource(command.UserDevices);
				messages.Add(Upsert(command.UserDevices));
			}
			if (command.UserInstances != null)
				messages.Add(Upsert(command.UserInstances));
			// UPSERT settings last, because it depends on other records.
			if (command.UserSettings != null)
				messages.Add(Upsert(command.UserSettings));
		}


		/// <summary>Execute DELETE commands on database.</summary>
		public static void Delete(CloudMessage command, List<string> messages)
		{
			// Delete user settings first, because it links to other records.
			if (command.UserSettings != null)
				messages.Add(Delete(command.UserSettings));
			// Delete user instances because it links to used devices.
			if (command.UserInstances != null)
				messages.Add(Delete(command.UserInstances));
			// Delete other records.
			if (command.UserGames != null)
				messages.Add(Delete(command.UserGames));
			if (command.UserDevices != null)
				messages.Add(Delete(command.UserDevices));
		}

		public static void Select(CloudMessage command, CloudMessage results, List<string> messages, out string error)
		{
			var computerId = CloudHelper.GetGuidId(CloudKey.ComputerId, command, out error).Value;
			var profileId = CloudHelper.GetGuidId(CloudKey.ProfileId, command, out error).Value;
			// Get all user games.
			if (command.UserGames != null)
			{
				UserGame[] userGames;
				error = Select(computerId, profileId, command.UserGames, out userGames);
				messages.Add(error);
				results.UserGames = EngineHelper.FilterByChecksum(userGames, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
			// Get all user devices.
			if (command.UserDevices != null)
			{
				UserDevice[] userDevices;
				error = Select(computerId, profileId, command.UserDevices, out userDevices);
				messages.Add(error);
				// Before the checksums, which the program takes with a DirectInput device at 0.
				userDevices = UserDevice.FilterByInputSource(userDevices, command.Values);
				results.UserDevices = EngineHelper.FilterByChecksum(userDevices, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
			//// Get all user computers.
			//if (command.UserComputers != null)
			//{
			//    UserComputer[] userComputers;
			//    error = Select(computerId, profileId, command.UserComputers, out userComputers);
			//    messages.Add(error);
			//    results.UserComputers = EngineHelper.FilterByChecksum(userComputers, command.Checksums, out error);
			//    if (!string.IsNullOrEmpty(error))
			//        messages.Add(error);
			//}
			// Get all user instances.
			if (command.UserInstances != null)
			{
				UserInstance[] userInstances;
				error = Select(computerId, profileId, command.UserInstances, out userInstances);
				messages.Add(error);
				results.UserInstances = EngineHelper.FilterByChecksum(userInstances, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
			// Get all user instances.
			if (command.UserSettings != null)
			{
				UserSetting[] userSettings;
				error = Select(computerId, profileId, command.UserSettings, out userSettings);
				messages.Add(error);
				results.UserSettings = userSettings;
				//results.UserSettings = EngineHelper.FilterByChecksum(userSettings, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
		}

		#region Helper Methods

		static void FixData<T>(T item)
		{
			var props = typeof(T).GetProperties();
			foreach (var p in props)
			{
				if (p.PropertyType == typeof(string))
				{
					var value = (string)p.GetValue(item);
					if (value == null)
						p.SetValue(item, "");
				}
				else if (p.PropertyType == typeof(DateTime))
				{
					var value = (DateTime)p.GetValue(item);
					if (value == new DateTime())
						p.SetValue(item, DateTime.Now);
				}
			}
		}

		public static string Upsert<T>(T[] items) where T : class, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.Set<T>();
			var created = 0;
			var updated = 0;
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				// Fix nulls.
				FixData(item);
				var computerId = item.ComputerId;
				var profileId = item.ProfileId;
				IUserRecord dbItem = GetExistingItem(table, item, true);
				// If database item was not found then...
				if (dbItem == null)
				{
					// Use supplied item as new item.
					created++;
					item.Id = Guid.NewGuid();
					item.DateCreated = DateTime.Now;
					item.DateUpdated = item.DateCreated;
					table.Add(item);
				}
				else
				{
					updated++;
					// Use created date from db.
					item.DateCreated = dbItem.DateCreated;
					item.DateUpdated = DateTime.Now;
					RuntimeHelper.CopyDataMembers(item, dbItem, true);
				}
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} created, {2} updated.", items.GetType().GetElementType().Name, created, updated);
		}

		/// <summary>Reads the records of one computer and profile.</summary>
		/// <remarks>
		/// The records are read to be sent, never saved, so they are not tracked: one changed on its way out, as
		/// <see cref="UserDevice.FilterByInputSource"/> and <see cref="EngineHelper.FilterByChecksum"/> do, stays a change to
		/// the reply.
		/// </remarks>
		public static string Select<T>(Guid computerId, Guid profileId, T[] filter, out T[] items) where T : class, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.Set<T>();
			items = table.AsNoTracking().Where(x => x.ComputerId == computerId && x.ProfileId == profileId).ToArray();
			db.Dispose();
			return string.Format("{0}s: {1} selected.", items.GetType().GetElementType().Name, items.Length);
		}

		public static string Delete<T>(T[] items) where T : class, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.Set<T>();
			var deleted = 0;
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				// Fix nulls.
				FixData(item);
				var computerId = item.ComputerId;
				var profileId = item.ProfileId;
				var dbItem = GetExistingItem(table, item);
				if (dbItem == null)
					continue;
				table.Remove(dbItem);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
		}

		/// <summary>The stored record of the message's computer and profile that the given record is, or null.</summary>
		/// <remarks>
		/// A record is found by its own keys, never by its ID: the program can hold a record it sent without the ID the
		/// service gave it. A device or controller instance keeps its instance GUID when it is stored, because that is
		/// how the next upload, a Delete and the program's Download From Cloud find it again.
		/// </remarks>
		/// <param name="updateGuid">True when a record that is not found is to be stored: a game or setting then gets a new ID.</param>
		static T GetExistingItem<T>(DbSet<T> table, T item, bool updateGuid = false) where T : class, IUserRecord
		{
			var computerId = item.ComputerId;
			var profileId = item.ProfileId;
			T dbItem = null;
			// User Game.
			var ug = item as UserGame;
			if (ug != null)
			{
				dbItem = ((DbSet<UserGame>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.FileName == ug.FileName && x.FileProductName == ug.FileProductName) as T;
				if (dbItem == null && updateGuid)
					ug.GameId = Guid.NewGuid();
			}
			// User Device.
			var ud = item as UserDevice;
			if (ud != null)
			{
				dbItem = ((DbSet<UserDevice>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.InstanceGuid == ud.InstanceGuid) as T;
			}
			// User Instance.
			var ui = item as UserInstance;
			if (ui != null)
			{
				dbItem = ((DbSet<UserInstance>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.InstanceGuid == ui.InstanceGuid) as T;
			}
			// User Instance.
			var us = item as UserSetting;
			if (us != null)
			{
				dbItem = ((DbSet<UserSetting>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.SettingId == us.SettingId) as T;
				if (dbItem == null && updateGuid)
					us.SettingId = Guid.NewGuid();
			}
			return dbItem;
		}

		#endregion


	}
}
