using JocysCom.WebSites.Engine.Security.Data;
using System;

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
			ResetPasswordSuccessPanel.Visible = false;
			ResetPasswordErrorLabel.Visible = false;
			if (user == null)
				user = User.GetUserByEmail(PasswordRecovery1.UserName);
			if (user == null)
			{
				ResetPasswordErrorLabel.Text = string.Format("'{0}' was not found.", PasswordRecovery1.UserName);
				ResetPasswordErrorLabel.Visible = true;
				return;
			}
			try
			{
				var subject = PasswordResetSubject.InnerHtml;
				var body = PasswordResetBody.InnerHtml;
				Membership.SendPasswordResetKey(user, subject, body);
			}
			catch (Exception ex)
			{
				ResetPasswordErrorLabel.Text = ex.Message;
				ResetPasswordErrorLabel.Visible = true;
				return;
			}
			ResetPasswordSuccessPanel.Visible = true;
		}

	}
}
