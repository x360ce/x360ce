using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JocysCom.ClassLibrary.Security
{
	public class Helper
	{

		static int securityHashSize = 16;
		static int unlockHashSize = 6;

		/// <summary>
		/// Get current time unit value.
		/// </summary>
		/// <param name="unit">Time unit type.</param>
		/// <returns>Time unit value.</returns>
		static double GetTimeUnitValue(TimeUnitType unit)
		{
			switch (unit)
			{
				case TimeUnitType.Seconds: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds);
				case TimeUnitType.Minutes: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalMinutes);
				case TimeUnitType.Hours: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalHours);
				case TimeUnitType.Days: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalDays);
			}
			return 0;
		}

		/// <summary>
		/// Generate password reset/token key which is unique to user. Reset key won't change until user logs in or change password.
		/// </summary>
		/// <param name="username">Username.</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <returns></returns>
		public static string GetSecurityToken(object userId, string password, TimeUnitType unit)
		{
			double u = GetTimeUnitValue(unit);
			return GetSecurityToken(userId, password, u);
		}

		/// <summary>
		/// Get security token.
		/// </summary>
		/// <param name="userId">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unitValue">Unit value.</param>
		/// <returns></returns>
		public static string GetSecurityToken(object userId, string password, double unitValue)
		{
			string user = userId is Guid ? ((Guid)userId).ToString("N").ToUpper() : userId.ToString();
			string passString = string.Format("{0}_{1}_{2}", user, unitValue, password);
			return Encryption.Current.ComputeHash(passString).ToString("N").Substring(0, securityHashSize).ToUpper() + user;
		}

		/// <summary>
		/// Check if token key is valid.
		/// </summary>
		/// <param name="token">Token to check.</param>
		/// <param name="userId">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <param name="count">How many units in past mus be checked.</param>
		/// <returns>True if token is valid, False if not valid.</returns>
		public static bool CheckSecurityToken(string token, object userId, string password, TimeUnitType unit, int count)
		{
			// Time which passed.
			double u = GetTimeUnitValue(unit);
			// Check keys for last units. (-5 solves the issue when token generator time is inaccurate and is set up to 5 [seconds|minutes|hours|days] in future).
			for (int i = -5; i < count; i++)
			{
				// If resetKey matches to key for given day then...
				if (token == GetSecurityToken(userId, password, u - i)) return true;
			}
			return false;
		}

		#region Unlock Token (Decimal)

		/// <summary>Get decimal unlock code.</summary>
		/// <param name="value">Value which will be used to make token.</param>
		/// <param name="unitType">Time unit type. Can be minutes, hours or days.</param>
		/// <returns></returns>
		public static string GetUnlockToken(string value, TimeUnitType unitType, string hmacHashKey = null)
		{
			var u = GetTimeUnitValue(unitType);
			return GetUnlockTokenByValue(value, u, hmacHashKey);
		}

		/// <summary>Get decimal unlock code.</summary>
		/// <param name="value">Value to hash.</param>
		/// <param name="unitValue">Time value which equals to total Seconds, Minutes, Hours or Days passed from zero date to specific point in time.</param>
		/// <returns></returns>
		private static string GetUnlockTokenByValue(string value, double unitValue, string hmacHashKey = null)
		{
			string passString = string.Format("{0}_{1}", value, unitValue);
			var hash = Encryption.Current.ComputeHash(passString, hmacHashKey).ToByteArray();
			var numb = BitConverter.ToUInt32(hash, 0);
			var text = numb.ToString().PadRight(unlockHashSize, '0').Substring(0, unlockHashSize);
			return text;
		}

		/// <summary>
		/// Check if token is valid.
		/// </summary>
		/// <param name="token">Token to check.</param>
		/// <param name="userId">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <param name="count">How many units in past mus be checked.</param>
		/// <returns>True if token is valid, False if not valid.</returns>
		public static bool CheckUnlockToken(string token, string value, TimeUnitType unit, int count, string hmacHashKey = null)
		{
			// Time which passed.
			double u = GetTimeUnitValue(unit);
			// Check keys for last units and return if token match.
			for (int i = 0; i < count; i++) if (token == GetUnlockTokenByValue(value, u - i, hmacHashKey)) return true;
			// -5 solves the issue when token generator time is inaccurate and is set up to 5 [seconds|minutes|hours|days] in future
			for (int i = -5; i < 0; i++) if (token == GetUnlockTokenByValue(value, u - i, hmacHashKey)) return true;
			return false;
		}

		#endregion

		/// <summary>
		/// Get URL to page.
		/// </summary>
		/// <param name="token">Token.</param>
		/// <param name="page">Page name. Like "/Login.aspx" or "/LoginReset.aspx"</param>
		/// <returns>Url.</returns>
		public static string GetUrl(string token, string page)
		{
			Uri u = System.Web.HttpContext.Current.Request.Url;
			var port = u.IsDefaultPort ? "" : ":" + u.Port.ToString();
			return string.Format("{0}://{1}{2}{3}?Key={4}", u.Scheme, u.Host, port, page, token);
		}

		/// <summary>
		/// Get URL to page.
		/// </summary>
		/// <param name="token">Token.</param>
		/// <returns>Url.</returns>
		public static string GetUrl(string token)
		{
			Uri u = System.Web.HttpContext.Current.Request.Url;
			var port = u.IsDefaultPort ? "" : ":" + u.Port.ToString();
			return string.Format("{0}://{1}{2}{3}?Key={4}", u.Scheme, u.Host, port, u.AbsolutePath, token);
		}

		/// <summary>
		/// Get user Id from token.
		/// </summary>
		/// <typeparam name="T">User Id type (Integer or GUID).</typeparam>
		/// <param name="token">Token.</param>
		/// <returns>User Id.</returns>
		public static T GetUserId<T>(string token)
		{
			string userId = token.Substring(securityHashSize);
			if (typeof(T) == typeof(int)) return (T)(object)int.Parse(userId);
			if (typeof(T) == typeof(Guid)) return (T)(object)new Guid(userId);
			return (T)(object)userId;
		}

		//public static void SendPasswordResetKey(string username, string password, TimeUnitType unit)
		//{
		//    Uri u = System.Web.HttpContext.Current.Request.Url;
		//    string resetUrl = GetUrl(username, password, unit);
		//    string template = Helper.GetTranslation(TranslationKey.PasswordResetTemplate);
		//    string body = template.Replace("{Username}", user.FullName).Replace("{Host}", u.Host).Replace("{ResetKey}", resetUrl);
		//    string subject = string.Format("Reset your {0} password", u.Host);
		//    Engine.Mail.Current.Send(user.Membership.Email, user.FullName, subject, body, JocysCom.ClassLibrary.Mail.MailTextType.Plain);
		//}

	}
}
