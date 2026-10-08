using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	public partial class DatabaseHelper
	{
		/// <summary>Execute INSERT/UPDATE commands on database.</summary>
		public static void Upsert(CloudMessage command, List<string> messages)
		{
			if (command.UserGames != null)
				messages.Add(Upsert(command.UserGames));
			if (command.UserDevices != null)
			{
				StoreInputSource(command.UserDevices);
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
				results.UserGames = FilterByChecksum(userGames, command.Checksums, out error);
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
				userDevices = FilterByInputSource(userDevices, command.Values);
				results.UserDevices = FilterByChecksum(userDevices, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
			//// Get all user computers.
			//if (command.UserComputers != null)
			//{
			//    UserComputer[] userComputers;
			//    error = Select(computerId, profileId, command.UserComputers, out userComputers);
			//    messages.Add(error);
			//    results.UserComputers = FilterByChecksum(userComputers, command.Checksums, out error);
			//    if (!string.IsNullOrEmpty(error))
			//        messages.Add(error);
			//}
			// Get all user instances.
			if (command.UserInstances != null)
			{
				UserInstance[] userInstances;
				error = Select(computerId, profileId, command.UserInstances, out userInstances);
				messages.Add(error);
				results.UserInstances = FilterByChecksum(userInstances, command.Checksums, out error);
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
				//results.UserSettings = FilterByChecksum(userSettings, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
		}

		/// <summary>
		/// Remove all unchanged items to save network bandwidth.
		/// </summary>
		public static T[] FilterByChecksum<T>(T[] items, Guid[] checksums, out string error) where T : IChecksum
		{
			error = null;
			var list = new T[0];
			if (checksums == null || checksums.Length == 0 || items == null || items.Length == 0)
				return items;
			var currentChecksums = EngineHelper.UpdateChecksums<T>(items);
			//// If last checksum is same then records have not changed.
			//if (checksums.Last().Equals(currentChecksums.Last()))
			//{
			//    return list;
			//}
			// Get only different items.
			list = items.Where(x => !checksums.Contains(x.Checksum)).ToArray();
			error = string.Format("{0} record(s) changed", list.Length);
			return list;
		}

		#region Input source of a device

		/// <summary>
		/// Gives every device sent without an input source the one the database stores for it: DirectInput.
		/// </summary>
		/// <remarks>
		/// A program before 4.25 sends no input source, and a newer one sends none for a DirectInput device, so a
		/// device that arrives without one is a DirectInput device. Called before the devices are inserted or copied
		/// over the stored rows. An older program updating a row that a newer one saved under the same InstanceGuid
		/// would set it back to DirectInput; that does not happen, because a Raw Input device takes its InstanceGuid
		/// from its HID path, and no older program knows that device.
		/// </remarks>
		public static void StoreInputSource(UserDevice[] devices)
		{
			foreach (var device in devices)
				if (device.InputSourceType == 0)
					device.InputSourceType = (int)InputSourceType.DirectInput;
		}

		/// <summary>
		/// The stored devices of the kinds the program reads, each with its input source as the program keeps it:
		/// 0 for DirectInput.
		/// </summary>
		/// <param name="devices">Devices as stored. A row at 0 counts as DirectInput.</param>
		/// <param name="values">
		/// The values of the program's message. <see cref="CloudKey.InputSourceTypes"/> names the kinds it reads; a
		/// program that sends none, as every program before 4.25, reads DirectInput only.
		/// </param>
		/// <remarks>
		/// Handing a DirectInput device back at 0 keeps its checksum the one the program took, so an unchanged
		/// device is not sent again. The devices are changed in place, so they must not be tracked by a context that
		/// is saved later; <see cref="Select{T}"/> reads them untracked.
		/// </remarks>
		public static UserDevice[] FilterByInputSource(UserDevice[] devices, KeyValueList values)
		{
			var directInput = (int)InputSourceType.DirectInput;
			var readable = values.GetValue(CloudKey.InputSourceTypes, directInput);
			if (readable == 0)
				readable = directInput;
			var list = devices.Where(x =>
			{
				var kind = x.InputSourceType == 0 ? directInput : x.InputSourceType;
				return (readable & kind) == kind;
			}).ToArray();
			foreach (var device in list)
				if (device.InputSourceType == directInput)
					device.InputSourceType = 0;
			return list;
		}

		#endregion

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

		public static string Upsert<T>(T[] items) where T : EntityObject, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.CreateObjectSet<T>();
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
					table.AddObject(item);
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
		/// <see cref="FilterByInputSource"/> and <see cref="FilterByChecksum"/> do, stays a change to the reply.
		/// </remarks>
		public static string Select<T>(Guid computerId, Guid profileId, T[] filter, out T[] items) where T : EntityObject, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.CreateObjectSet<T>();
			table.MergeOption = MergeOption.NoTracking;
			items = table.Where(x => x.ComputerId == computerId && x.ProfileId == profileId).ToArray();
			db.Dispose();
			return string.Format("{0}s: {1} selected.", items.GetType().GetElementType().Name, items.Length);
		}

		public static string Delete<T>(T[] items) where T : EntityObject, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.CreateObjectSet<T>();
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
				table.DeleteObject(dbItem);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
		}

		static T GetExistingItem<T>(ObjectSet<T> table, T item, bool updateGuid = false) where T : EntityObject, IUserRecord
		{
			var computerId = item.ComputerId;
			var profileId = item.ProfileId;
			T dbItem = null;
			// User Game.
			var ug = item as UserGame;
			if (ug != null)
			{
				dbItem = ((ObjectSet<UserGame>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.FileName == ug.FileName && x.FileProductName == ug.FileProductName) as T;
				if (dbItem == null && updateGuid)
					ug.GameId = Guid.NewGuid();
			}
			// User Device.
			var ud = item as UserDevice;
			if (ud != null)
			{
				dbItem = ((ObjectSet<UserDevice>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.InstanceGuid == ud.InstanceGuid) as T;
				if (dbItem == null && updateGuid)
					ud.InstanceGuid = Guid.NewGuid();
			}
			// User Instance.
			var ui = item as UserInstance;
			if (ui != null)
			{
				dbItem = ((ObjectSet<UserInstance>)(object)table)
					.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId &&
						x.InstanceGuid == ui.InstanceGuid) as T;
				if (dbItem == null && updateGuid)
					ui.InstanceGuid = Guid.NewGuid();
			}
			// User Instance.
			var us = item as UserSetting;
			if (us != null)
			{
				dbItem = ((ObjectSet<UserSetting>)(object)table)
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
