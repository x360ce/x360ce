using JocysCom.ClassLibrary.Runtime;
using System;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Web
{
	public partial class DatabaseHelper
	{

		public static string Upsert(Setting[] items)
		{
			var db = new x360ceModelContainer();
			var created = 0;
			var updated = 0;
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				var instanceGuid = item.InstanceGuid;
				var dbItem = db.Settings.FirstOrDefault(x => x.InstanceGuid == instanceGuid);
				if (dbItem == null)
				{
					created++;
					item.InstanceGuid = Guid.NewGuid();
					item.DateCreated = DateTime.Now;
					item.DateUpdated = item.DateCreated;
					db.Settings.AddObject(item);
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
			db = null;
			return string.Format("{0}s: {1} created, {2} updated.", items.GetType().GetElementType().Name, created, updated);
		}

		public static string Select(Guid computerId, Setting[] filter, out Setting[] items)
		{
			var db = new x360ceModelContainer();
			// Select all instances.
			UserInstance[] userInstances;
			Select(computerId, null, out userInstances);
			var guids = userInstances.Select(x => x.InstanceGuid).ToArray();
			// Select all settings related to user device instances.
			items = db.Settings.Where(x => guids.Contains(x.InstanceGuid)).ToArray();
			db.Dispose();
			return string.Format("{0}s: {1} selected.", items.GetType().GetElementType().Name, items.Length);
		}

		public static string Delete(Setting[] items)
		{
			var db = new x360ceModelContainer();
			var deleted = 0;
			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];
				var instanceGuid = item.InstanceGuid;
				var currentSetting = db.Settings.FirstOrDefault(x => x.InstanceGuid == instanceGuid);
				if (currentSetting == null) continue;
				db.Settings.DeleteObject(currentSetting);
				deleted++;
			}
			db.SaveChanges();
			db.Dispose();
			return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
		}

	}
}
