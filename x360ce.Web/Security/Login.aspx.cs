using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using JocysCom.WebSites.Engine;

namespace JocysCom.Web.Security
{
	public partial class Login : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				if (SecurityContext.Current.IsAuthenticated)
				{
					bool logout;
					bool.TryParse(Request["Logout"], out logout);
					if (logout)
					{
						LogoutLinkButton_Click(null, null);
					}
				}
			}
			LoginBanner.DataBind();
			AnonymousPlaceHolder.DataBind();
			LoggedInPlaceHolder.DataBind();
			RegisterPanel.DataBind();
			AnonymousPlaceHolder.Visible = !SecurityContext.Current.IsAuthenticated;
			LoggedInPlaceHolder.Visible = SecurityContext.Current.IsAuthenticated;
		}

		void UserEditControl1_Created(object sender, UserEditEventArgs e)
		{
			var role = SecurityContext.Current.DefaultRole;
			if (!string.IsNullOrEmpty(role))
			{
				// Make sure user have access to Social part of website.
				System.Web.Security.Roles.AddUserToRole(e.User.UserName, role);
			}
		}

		protected void LogoutLinkButton_Click(object sender, EventArgs e)
		{
			// Abandon ASPX Session.
			System.Web.Security.FormsAuthentication.SignOut();
			Session.Abandon();
			RedirectToUrl();
		}


		public void RedirectToUrl()
		{
			if (string.IsNullOrEmpty(Request["ReturnUrl"]))
			{
				Response.Redirect("Login.aspx");
			}
			else
			{
				Response.Redirect(Request["ReturnUrl"]);
			}
		}

	}
}
