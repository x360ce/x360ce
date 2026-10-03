using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace JocysCom.WebSites.Engine.Security.Data
{
	public partial class Role
	{
		/// <summary>
		/// All shared queries must go here.
		/// </summary>
		/// <param name="queryName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IQueryable<Role> GetQuery(string queryName, OrderedDictionary parameters)
		{
			// Get parameters.
			string[] qa = queryName.Split('/');
			string p0 = qa[0];
			string p1 = (qa.Length > 1) ? qa[1] : string.Empty;
			string p2 = (qa.Length > 2) ? qa[2] : string.Empty;
			// Set predefined query.
			IQueryable<Role> query = null;
			Guid roleId = parameters.Contains("RoleId") ? (Guid)parameters["RoleId"] : Guid.Empty;
			var db = SecurityEntities.Current;
			RoleQueryName qne = GuidEnum.TryParse<RoleQueryName>(p0, RoleQueryName.None, true);
			switch (qne)
			{
				case RoleQueryName.All:
					query = from row in db.Roles select row;
					break;
				default:
					throw new NotImplementedException(string.Format("{0} QueryName not supported", queryName));
				//break;
			}
			// Add search condition.
			if (parameters != null)
			{
				// Apply search filter.
				string searchValue;
				searchValue = parameters.Contains("SearchName") ? (string)parameters["SearchName"] : string.Empty;
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim();
					if (GuidEnum.IsGuid(searchValue))
					{
						query = query.Where(x => x.RoleId == new Guid(searchValue));
					}
					else
					{
						// we cant use FullText index inside linq so just extend command timout in order for 
						// search not to fail.
						if (db.CommandTimeout < 120) db.CommandTimeout = 120;
						query = query.Where(x =>
							x.RoleName == searchValue);
					}
				}
			}
			return query;
		}

		public static Role GetRole(string roleName)
		{
			var db = new Security.Data.SecurityEntities();
			return GetRole(roleName, db);
		}

		public static Role GetRole(Guid roleId)
		{
			var db = new Security.Data.SecurityEntities();
			return GetRole(roleId, db);
		}

		public static Role GetRole(string roleName, SecurityEntities db)
		{
			var query = from row in db.Roles
						where row.LoweredRoleName == roleName.ToLower() &&
							row.Application.ApplicationName == System.Web.Security.Roles.ApplicationName
						select row;
			return query.FirstOrDefault();
		}

		public static Role GetRole(Guid roleId, SecurityEntities db)
		{
			var query = from row in db.Roles
						where row.RoleId == roleId &&
							row.Application.ApplicationName == System.Web.Security.Roles.ApplicationName
						select row;
			return query.FirstOrDefault();
		}


	}
}
