using JocysCom.ClassLibrary.Runtime;
using System;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Web
{
    public partial class DatabaseHelper
    {

        public static string Upsert(UserDevice[] items)
        {
            var db = new x360ceModelContainer();
            var created = 0;
            var updated = 0;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var instanceGuid = item.InstanceGuid;
                var dbItem = db.UserDevices.FirstOrDefault(x => x.InstanceGuid == instanceGuid);
                // If database item was not found then...
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

        public static string Select(Guid computerId, UserDevice[] filter, out UserDevice[] items)
        {
            var db = new x360ceModelContainer();
            items = db.UserDevices.Where(x => x.ComputerId == computerId).ToArray();
            db.SaveChanges();
            db.Dispose();
            db = null;
            return string.Format("{0}s: {1} selected.", items.GetType().GetElementType().Name, items.Length);
        }

        public static string Delete(UserDevice[] items)
        {
            var db = new x360ceModelContainer();
            var deleted = 0;
            for (int i = 0; i < items.Length; i++)
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
            return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
        }

    }
}
