using System;
using System.Linq;

namespace x360ce.Web.Security.Data
{
	public partial class SecurityEntities
	{

		private static Guid _ApplicationId;
		public static Guid ApplicationId
		{
			get
			{
				if (_ApplicationId.Equals(Guid.Empty))
				{
					var db = new SecurityEntities();
					_ApplicationId = db.Applications
						.Where(x => x.ApplicationName == System.Web.Security.Roles.ApplicationName)
						.Select(x => x.ApplicationId).FirstOrDefault();
					db.Dispose();
					db = null;
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
