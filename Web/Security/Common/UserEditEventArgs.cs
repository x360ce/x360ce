using System;
using System.Data;
using System.Configuration;
using System.Web;

namespace JocysCom.Web.Security
{
	public class UserEditEventArgs : System.EventArgs
	{
		#region Properties

		public System.Web.Security.MembershipUser User
		{ get { return m_user; } set { m_user = value; } }
		private System.Web.Security.MembershipUser m_user;

		#endregion

		public UserEditEventArgs(string userName)
		{
			this.m_user = System.Web.Security.Membership.GetUser(userName);
		}

		public UserEditEventArgs(System.Web.Security.MembershipUser user)
		{
			this.m_user = user;
		}
	}
}