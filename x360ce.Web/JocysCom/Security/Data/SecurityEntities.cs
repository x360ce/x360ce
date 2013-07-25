using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JocysCom.WebSites.Engine.Security.Data
{
	public partial class SecurityEntities
	{
		public static SecurityEntities Current
		{
			get
			{
                var db = DataContextFactory<SecurityEntities>.Instance<SecurityEntities>();
				// AppendOnly - Objects that already exist in the object context are not loaded from the data source (Default).
				// OverwriteChanges - Objects are always loaded from the data source.
				// PreserveChanges - Unmodified properties of objects in the object context are overwritten with server values.
				// NoTracking - Objects are maintained in a EntityState.Detached state and are not tracked in the ObjectStateManager.
				return db;
			}
		}

		private Guid _ApplicationId;
		public Guid ApplicationId
		{
			get
			{
				if (_ApplicationId.Equals(Guid.Empty))
				{
					_ApplicationId = (from t
									  in Current.Applications
									  where t.ApplicationName == System.Web.Security.Roles.ApplicationName
									  select t.ApplicationId).SingleOrDefault<Guid>();
				}
				return _ApplicationId;
			}
		}

		public void CreateRole(string roleName, string description, Guid roleId)
		{
			var db = new SecurityEntities();
			var role = new Role();
			role.ApplicationId = ApplicationId;
			role.RoleName = roleName;
			role.RoleId = roleId;
			role.Description = description;
			role.LoweredRoleName = roleName.ToLower();
			db.Roles.AddObject(role);
			db.SaveChanges();
		}

		public System.Web.Security.MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, out System.Web.Security.MembershipCreateStatus status)
		{
			return System.Web.Security.Membership.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, out status);
		}

		public bool DeleteUser(string username, bool deleteAllRelatedData)
		{
			// Perform pre-delete actions here.
			return System.Web.Security.Membership.DeleteUser(username, deleteAllRelatedData);
		}
	}
}
