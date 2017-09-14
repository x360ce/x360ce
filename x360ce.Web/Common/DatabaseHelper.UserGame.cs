using JocysCom.ClassLibrary.Runtime;
using System;
using System.Linq;
using x360ce.Engine.Data;

namespace x360ce.Web
{
    public partial class DatabaseHelper
    {
        public static string Upsert(UserGame[] items)
        {
            var db = new x360ceModelContainer();
            var created = 0;
            var updated = 0;
            for (int i = 0; i < items.Length; i++)
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

        public static string Select(Guid computerId, UserGame[] filter, out UserGame[] items)
        {
            var db = new x360ceModelContainer();
            items = db.UserGames.Where(x => x.ComputerId == computerId).ToArray();
            db.SaveChanges();
            db.Dispose();
            db = null;
            return string.Format("{0}s: {1} selected.", items.GetType().GetElementType().Name, items.Length);
        }

        public static string Delete(UserGame[] items)
        {

            var db = new x360ceModelContainer();
            var deleted = 0;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var computerId = item.ComputerId;
                var fileName = item.FileName;
                var currentGame = db.UserGames.FirstOrDefault(x => x.ComputerId == computerId && x.FileName == fileName);
                if (currentGame == null) continue;
                db.UserGames.DeleteObject(currentGame);
                deleted++;
            }
            db.SaveChanges();
            db.Dispose();
            db = null;
            return string.Format("{0}s: {1} deleted.", items.GetType().GetElementType().Name, deleted);
        }

    }
}
