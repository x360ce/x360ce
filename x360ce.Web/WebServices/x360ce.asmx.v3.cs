using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Security;
using x360ce.Engine.Data;
using x360ce.Engine;
using System.Data;
using JocysCom.ClassLibrary.Data;
using JocysCom.ClassLibrary.Runtime;
using JocysCom.ClassLibrary.Web.Services;

namespace x360ce.Web.WebServices
{
	public partial class x360ce
	{
		/// <summary>
		/// Save controller settings.
		/// </summary>
		/// <param name="s">Setting object which contains information about DirectInput device and file/game name it is used for.</param>
		/// <param name="ps">PAD settings which contains mapping between DirectInput device and virtual XBox controller.</param>
		/// <returns>Status of operation. Empty if success.</returns>
		[WebMethod(EnableSession = true, Description = "Save controller settings."), TraceExtension]
		public string SaveSetting(UserSetting s, PadSetting ps)
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
			var sDB = db.UserSettings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			var n = DateTime.Now;
			if (sDB == null)
			{
				sDB = s;
				sDB.EntityKey = null;
				// Assign brand new ID.
				s.SettingId = Guid.NewGuid();
				s.DateCreated = n;
				s.DateUpdated = n;
				s.DateSelected = n;
				// Link PadSetting with setting.
				s.PadSettingChecksum = pDB.PadSettingChecksum;
				db.UserSettings.AddObject(sDB);
			}
			db.SaveChanges();
			db.Dispose();
			db = null;
			return "";
		}

		[WebMethod(EnableSession = true, Description = "Delete controller settings.")]
		public string DeleteSetting(UserSetting s)
		{
			var db = new x360ceModelContainer();
			var setting = db.UserSettings.FirstOrDefault(x => x.InstanceGuid == s.InstanceGuid && x.FileName == s.FileName && x.FileProductName == s.FileProductName);
			if (setting == null) return "Setting not found";
			db.UserSettings.DeleteObject(setting);
			db.SaveChanges();
			db.Dispose();
			db = null;
			return "";
		}

	}
}
