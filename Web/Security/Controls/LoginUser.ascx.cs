using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;

namespace JocysCom.Web.Security.Controls
{
	public partial class LoginUser : System.Web.UI.UserControl
	{

		[EditorBrowsable, Category("Behavior"), DefaultValue(false)]
		protected bool AutoFocus
		{
			get { return (bool)(ViewState["AutoFocus"] ?? false); }
			set { ViewState["AutoFocus"] = value; }
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			// Bind data to make sure that layout controls are created.
			LoginPanel.DataBind();
			var en = SecurityContext.Current.AllowUsersToLogin;
			HelperFunctions.EnableControl(Login1, en, en ? null : "Login Disabled");
			ScriptPlaceHolder.Visible = AutoFocus && !IsPostBack;
		}

		/// <summary>
		/// Fix for RememberMe (persistent cookie).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void Login1_LoggedIn(object sender, EventArgs e)
		{
			System.Web.UI.HtmlControls.HtmlInputCheckBox rememberMe = (System.Web.UI.HtmlControls.HtmlInputCheckBox)this.Login1.FindControl("RememberMe");
			System.Web.UI.WebControls.Login login = (System.Web.UI.WebControls.Login)sender;
			if (rememberMe.Checked)
			{
				System.Web.Security.FormsAuthentication.SetAuthCookie(login.UserName, true);
			}
		}

		protected void Login1_LoginError(object sender, EventArgs e)
		{
			Login1.FindControl("LoginErrorPanel").Visible = true;
		}

	}
}