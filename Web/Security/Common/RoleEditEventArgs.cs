using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Linq;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security
{
	public class RoleEditEventArgs : System.EventArgs
	{
		#region Properties

        public Role Role
		{ get { return m_role; } set { m_role = value; } }
        private Role m_role;

		#endregion

		public RoleEditEventArgs(string roleName)
		{
            var db = new SecurityEntities();
            this.m_role = (from item in db.Roles
                              where item.Application.ApplicationName == System.Web.Security.Roles.ApplicationName
                              && item.RoleName == roleName
                              select item).FirstOrDefault();
         		}

        public RoleEditEventArgs(Role role)
		{
			this.m_role = role;
		}
	}
}
