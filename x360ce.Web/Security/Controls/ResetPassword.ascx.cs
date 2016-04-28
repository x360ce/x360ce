using JocysCom.WebSites.Engine.Security.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace JocysCom.Web.Security.Controls
{
	public partial class ResetPassword : System.Web.UI.UserControl
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			var en = SecurityContext.Current.AllowUsersToResetPassword;
			HelperFunctions.EnableControl(this, en, en ? null : "Reset Password Disabled");
		}

		protected void ResetButton_Click(object sender, EventArgs e)
		{
			var user = User.GetUser(PasswordRecovery1.UserName);
			if (user == null)
			{
				user = User.GetUserByEmail(PasswordRecovery1.UserName);
			}
			if (user == null)
			{
				ResetPasswordUserNotFoundLabel.Text = string.Format("'{0}' was not found.", PasswordRecovery1.UserName);
				ResetPasswordUserNotFoundLabel.Visible = true;
				ResetPasswordSuccessPanel.Visible = false;
			}
			else
			{
				ResetPasswordUserNotFoundLabel.Visible = false;
				var subject = PasswordResetSubject.InnerHtml;
				var body = PasswordResetBody.InnerHtml;
				Membership.SendPasswordResetKey(user, subject, body);
				ResetPasswordSuccessPanel.Visible = true;
			}
		}

	}
}