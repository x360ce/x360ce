using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;
using JocysCom.ClassLibrary;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security
{
	public partial class LoginReset : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			string username = Request["User"];
			string key = Request[Membership.PasswordResetKey];
			if (!string.IsNullOrEmpty(username))
			{
				//WebSites.Engine.Security.Data.Membership.SendPasswordResetKey(JocysCom.WebSites.Engine.Security.Data.User.GetUser(username));
				//ResetPasswordPanel.Visible = true;
			}
			else if (!string.IsNullOrEmpty(key))
			{
				Regex r = new Regex("^[a-fA-F0-9]+$");
				if (!r.IsMatch(key))
				{
					ErrorLabel.Text = "Error 1: Password reset key format is not valid!";
					ErrorLabel.Visible = true;
					return;
				}
				Guid userId = JocysCom.ClassLibrary.Security.TokenHelper.GetData<Guid>(key);
				var user = JocysCom.WebSites.Engine.Security.Data.User.GetUser(userId);
				if (user == null)
				{
					ErrorLabel.Text = "Error 2: Password reset key is not valid!";
					ErrorLabel.Visible = true;
					return;
				}
				// Key will expire after 5 minutes.
				if (!JocysCom.ClassLibrary.Security.TokenHelper.CheckSecurityToken(key, user.UserId, user.Membership.Password, TimeUnitType.Minutes, 5))
				{
					ErrorLabel.Text = "Error 3: Password reset key expired!";
					ErrorLabel.Visible = true;
					return;
				}
				// Show reset form.
				ResetKeyLabel.Text = key;
				ChangePasswordPanel.Visible = true;
			}
		}
		
		protected void ChangePasswordPushButton_Click(object sender, EventArgs e)
		{
			Guid userId = JocysCom.ClassLibrary.Security.TokenHelper.GetData<Guid>(ResetKeyLabel.Text);
			var user = JocysCom.WebSites.Engine.Security.Data.User.GetUser(userId);
			var muser = System.Web.Security.Membership.GetUser(user.UserName);
			// Reset password: Start
			string tempPassword = muser.ResetPassword();
			muser.ChangePassword(tempPassword, NewPassword.Text);
			System.Web.Security.Membership.UpdateUser(muser);
			// Reset password: End
			SuccessPanel.Visible = true;
			ChangePasswordPanel.Visible = false;
			RedirectionPanel.Visible = true;
		}
	}

}
