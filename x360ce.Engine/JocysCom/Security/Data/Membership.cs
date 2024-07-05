using JocysCom.ClassLibrary.Runtime;
using System.Linq;

namespace JocysCom.WebSites.Engine.Security.Data
{
	public partial class Membership
	{

		#region Generate / Check Tokens

		public const string PasswordResetKey = "Key";

		public string GetRedirectToken()
		{
			return JocysCom.ClassLibrary.Security.TokenHelper.GetSecurityToken(UserId, Password, JocysCom.ClassLibrary.TimeUnitType.Minute);
		}

		public string GetPasswordResetToken()
		{
			return JocysCom.ClassLibrary.Security.TokenHelper.GetSecurityToken(UserId, Password, JocysCom.ClassLibrary.TimeUnitType.Hour);
		}

		public bool CheckRedirectToken(string key)
		{
			var isValid = JocysCom.ClassLibrary.Security.TokenHelper.CheckSecurityToken(key, UserId, Password, JocysCom.ClassLibrary.TimeUnitType.Minute, 10);
			return isValid;
		}

		public bool CheckPasswordResetToken(string key)
		{
			var isValid = JocysCom.ClassLibrary.Security.TokenHelper.CheckSecurityToken(key, UserId, Password, JocysCom.ClassLibrary.TimeUnitType.Hour, 24);
			return isValid;
		}


		public static void SendPasswordResetKey(User user, string subject, string body)
		{
			var db = new SecurityEntities();
			var userId = user.UserId;
			var m = db.Memberships.FirstOrDefault(x => x.UserId == userId);
			var resetKey = m.GetPasswordResetToken();
			var resetUrl = JocysCom.ClassLibrary.Security.TokenHelper.GetUrl(PasswordResetKey, resetKey);
			var u = System.Web.HttpContext.Current.Request.Url;
			subject = subject.Replace("{Host}", u.Host);
			body = JocysCom.ClassLibrary.Text.Helper.Replace(body, user, false);
			body = body.Replace("{Host}", u.Host).Replace("{ResetKey}", resetUrl.AbsoluteUri);
			LogHelper.Current.SendMailFrom(ClassLibrary.Mail.SmtpClientEx.Current.SmtpFrom, m.Email, "", "", subject, body, true);
		}

		#endregion


	}
}
