using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Web
{
    public partial class DatabaseHelper
    {
        public static void Upsert(CloudMessage command, List<string> messages)
        {
            // Games can be inserted by using computer id only.
            messages.Add(Upsert(command.UserGames));
            messages.Add(Upsert(command.UserDevices));
            //messages.Add(Upsert(command.UserComputers));
            messages.Add(Upsert(command.UserInstances));
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
				Setting[] userSettings;
				error = Select(computerId, profileId, command.UserSettings, out userSettings);
				messages.Add(error);
				results.UserSettings = userSettings;
				//results.UserSettings = FilterByChecksum(userSettings, command.Checksums, out error);
				if (!string.IsNullOrEmpty(error))
					messages.Add(error);
			}
		}

		public static void Delete(CloudMessage command, List<string> messages)
        {
			//messages.Add(Delete(command.UserComputers));
			messages.Add(Delete(command.UserGames));
            messages.Add(Delete(command.UserDevices));
            messages.Add(Delete(command.UserInstances));
			messages.Add(Delete(command.UserSettings));
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

        public static Guid? FixComputerId(CloudMessage command, out string error)
        {
            var computerId = CloudHelper.GetGuidId(CloudKey.ComputerId, command, out error);
            if (computerId.HasValue)
            {
                // Fix computer id
                if (command.UserGames != null)
                {
                    foreach (var item in command.UserGames)
                        item.ComputerId = computerId.Value;
                }
                if (command.UserDevices != null)
                {
                    foreach (var item in command.UserDevices)
                        item.ComputerId = computerId.Value;
                }
			}
			return computerId;
        }

		#region Helper Methods

		public static string Upsert<T>(T[] items) where T : EntityObject, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.CreateObjectSet<T>();
			var created = 0;
			var updated = 0;
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				var computerId = item.ComputerId;
				var profileId = item.ProfileId;
				var itemId = item.ItemId;
				var dbItem = table.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId && x.ItemId == itemId);
				// If database item was not found then...
				if (dbItem == null)
				{
					created++;
					item.ItemId = Guid.NewGuid();
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
					Helper.CopyDataMembers(item, dbItem, true);
				}
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} created, {2} updated.", items.GetType().GetElementType().Name, created, updated);
		}

		public static string Select<T>(Guid computerId, Guid profileId, T[] filter, out T[] items) where T : EntityObject, IUserRecord
		{
			var db = new x360ceModelContainer();
			var table = db.CreateObjectSet<T>();
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
				var computerId = item.ComputerId;
				var profileId = item.ProfileId;
				var itemId = item.ItemId;
				var currentItem = table.FirstOrDefault(x => x.ComputerId == computerId && x.ProfileId == profileId && x.ItemId == itemId);
				if (currentItem == null)
					continue;
				table.DeleteObject(currentItem);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
		}

		#endregion


	}
}
