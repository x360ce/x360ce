using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Security;

namespace JocysCom.ClassLibrary.Mail
{
	public class WebMail
	{
		private static Regex _emailRegex;
		private readonly Regex _htmlTag = new Regex("</?\\w+((\\s+\\w+(\\s*=\\s*(?:\".*?\"|'.*?'|[^'\">\\s]+))?)+\\s*|\\s*)/?>");
		private SmtpClientEx _mailClient;

		public static Regex EmailRegex
		{
			set { _emailRegex = value; }
			get
			{
				if (_emailRegex == null)
					_emailRegex =
						new Regex(
							@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.IgnoreCase);
				return _emailRegex;
			}
		}

		public SmtpSection SmtpSettings
		{
			get
			{
				System.Configuration.Configuration config;
				if (HttpContext.Current == null)
				{
					config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				}
				else
				{
					string path = HttpContext.Current.Request.ApplicationPath;
					config = WebConfigurationManager.OpenWebConfiguration(path);
				}
				MailSettingsSectionGroup settings = NetSectionGroup.GetSectionGroup(config).MailSettings;
				return settings.Smtp;
			}
		}

		public SmtpClientEx MailClient
		{
			get
			{
				if (_mailClient == null)
					_mailClient = GetSmtpCient();
				return _mailClient;
			}
			set { _mailClient = value; }
		}

		#region GetMailAddress(...)

		public List<MailAddress> GetMailAddresses(string addresses)
		{
			var list = new List<MailAddress>();
			string[] arr = addresses.Replace(';', ',').Split(',');
			foreach (string address in arr)
			{
				try
				{
					list.Add(new MailAddress(address));
				}
				catch (Exception)
				{
				}
			}
			return list;
		}

		public virtual MailAddress[] GetMailAddress(Guid[] userIds)
		{
			return userIds.Select(x => GetMailAddress(x)).ToArray();
		}

		public virtual MailAddress GetMailAddress(Guid userId)
		{
			var user = Membership.GetUser(userId);
			if (user == null) throw new Exception("User with Id '" + userId + "' doesn't exist!");
			return new MailAddress(user.Email, user.UserName);
		}

		public virtual MailAddress[] GetMailAddress(string[] usernames)
		{
			return usernames.Select(x => GetMailAddress(x)).ToArray();
		}

		public virtual MailAddress GetMailAddress(string username)
		{
			var user = Membership.GetUser(username);
			if (user == null) throw new Exception("User '" + username + "' doesn't exist!");
			return new MailAddress(user.Email, user.UserName);
		}

		#endregion

		public bool IsHtml(string s)
		{
			return _htmlTag.IsMatch(s);
		}


		public static bool IsEmail(string s)
		{
			if (string.IsNullOrEmpty(s)) return false;
			var match = EmailRegex.Match(s);
			return match.Success && match.Value.Length == s.Length;
		}

		public virtual MailMessage GetMessage(string subject, string body)
		{
			return IsHtml(body)
					   ? GetMessage(subject, body, MailTextType.Html)
					   : GetMessage(subject, body, MailTextType.Plain);
		}

		/// <summary>
		/// Create message.
		/// </summary>
		/// <param name="subject">Message subject.</param>
		/// <param name="body">Message body.</param>
		/// <param name="bodyType">Type of body (MediaTypeNames.Text)</param>
		/// <returns>System.Net.Mail.MailMessage</returns>
		public MailMessage GetMessage(string subject, string body, MailTextType bodyType)
		{
			var message = new MailMessage { SubjectEncoding = Encoding.UTF8, Subject = subject, BodyEncoding = Encoding.UTF8 };
			// Framework selects a TransferEncoding of Base64 if you set the BodyEncoding property to UTF8, Unicode or UTF32. 
			// So we must add alternate view for some old crappy browsers which doesn't support Base64 TransferEncoding.
			string plainBody = string.Empty;
			AlternateView htmlView = null;
			switch (bodyType)
			{
				case MailTextType.Html:
					// Add HTML view.
					htmlView = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Html);
					htmlView.TransferEncoding = TransferEncoding.SevenBit; // Very important
																		   // Mark message as HTML.
					message.IsBodyHtml = true;
					// Create alternative plain body.
					plainBody = ConvertHtmlToPlain(body);
					break;
				case MailTextType.Plain:
					plainBody = body;
					break;
				case MailTextType.RichText:
					break;
				case MailTextType.Xml:
					break;
				default:
					break;
			}
			// Add Plain View.
			AlternateView plainView = AlternateView.CreateAlternateViewFromString(plainBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
			plainView.TransferEncoding = TransferEncoding.SevenBit; // Very important
																	// Add views.
			message.AlternateViews.Add(plainView);
			// "text/html" part must be last in order for gmail to display it.
			if (htmlView != null) message.AlternateViews.Add(htmlView);
			return message;
		}

		public SmtpClientEx GetSmtpCient()
		{
			var smtpClient = new SmtpClientEx
			{
				Host = SmtpSettings.Network.Host,
				Port = SmtpSettings.Network.Port,
				EnableSsl = false
			};
			// Set timeout to 5 minutes
			string smtpTimeout = ConfigurationManager.AppSettings["MailSettingsSmtpTimeout"];
			if (!string.IsNullOrEmpty(smtpTimeout))
			{
				smtpClient.Timeout = int.Parse(smtpTimeout);
			}
			smtpClient.UseDefaultCredentials = false;
			var credential = new NetworkCredential
			{
				UserName = SmtpSettings.Network.UserName,
				Password = SmtpSettings.Network.Password
			};
			smtpClient.Credentials = credential;
			// Set the method that is called back when the send operation ends.
			smtpClient.SendCompleted += SendCompletedCallback;
			return smtpClient;
		}

		public string GetExceptionMessage(Exception ex)
		{
			string errorMessage = "<span class=\"SWUI_MaiError\">";
			//errorMessage += "Warning: Email from user[UserId=" + token.UserId + "] was not send:<br />";
			//errorMessage += "Server: " + MailClient.Host + ":" + MailClient.Port.ToString() + "<br/>";
			//errorMessage += "Source: " + ex.Source + "; Message: " + ex.Message + "<br/>";
			//if (ex.InnerException != null)
			//{
			//    errorMessage += "Source: " + ex.InnerException.Source + "; Message: " + ex.InnerException.Message;
			//}
			errorMessage += "</span>";
			return errorMessage;
		}

		public void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			// Get the unique identifier for this asynchronous operation.
			var token = (UserToken)e.UserState;
			if (e.Cancelled)
			{
				//Console.WriteLine("[{0}] Send canceled.", token);
			}
			if (e.Error != null)
			{
				string errorMessage = GetExceptionMessage(e.Error);
				//string logFile = System.Web.Configuration.WebConfigurationManager.AppSettings["MailLogFile"];
				//string filePath = System.Web.HttpContext.Current.Server.MapPath(logFile);
				//System.IO.File.WriteAllText(filePath, token.ToString() + " " + e.Error.ToString());
			}
			else
			{
				//Console.WriteLine("Message sent.");
			}
			// Dispose message to unlock attachment files on the disk.
			if (token.Message != null) token.Message.Dispose();
			// Delete tem folder with attachments.
			if (token.TempDirectory != null)
			{
				if (token.TempDirectory.Exists) token.TempDirectory.Delete();
			}
		}

		public string Send(string toAddress, string toName, string subject, string body, MailTextType bodyType)
		{
			MailMessage message = GetMessage(subject, body, bodyType);
			message.From = new MailAddress(SmtpSettings.From);
			message.To.Add(new MailAddress(toAddress, toName));
			var token = new UserToken { Message = message };
			//smtp.SendAsync(message, new UserToken());
			//smtp.Send(message);
			//MailClient.SendAsync(message, token);
			MailClient.Send(message);
			return string.Empty;
		}

		#region Send(...)

		public string Send(Guid fromUser, Guid[] toUsers, string subject, string body)
		{
			return Send(GetMailAddress(fromUser), GetMailAddress(toUsers), subject, body);
		}

		public string Send(string fromUser, string[] toUsers, string subject, string body)
		{
			return Send(GetMailAddress(fromUser), GetMailAddress(toUsers), subject, body);
		}

		public string Send(Guid from, Guid toUser, string subject, string body)
		{
			return Send(from, new[] { toUser }, subject, body);
		}

		public string Send(MailFromUser fromUser, Guid[] toUsers, string subject, string body)
		{
			MailAddress from = null;
			switch (fromUser)
			{
				case MailFromUser.SmtpDefault:
					from = new MailAddress(SmtpSettings.From);
					break;
				case MailFromUser.CurrentSession:
					from = GetMailAddress(HttpContext.Current.User.Identity.Name);
					break;
				default:
					throw new NotImplementedException();
			}
			return Send(from, GetMailAddress(toUsers), subject, body);
		}

		public string Send(MailFromUser fromUser, Guid toUser, string subject, string body)
		{
			return Send(fromUser, new[] { toUser }, subject, body);
		}

		public string Send(MailAddress from, MailAddress toUser, string subject, string body)
		{
			return Send(from, new[] { toUser }, subject, body);
		}

		public string Send(MailAddress from, MailAddress[] toUsers, string subject, string body)
		{
			if (HttpContext.Current != null)
				HttpContext.Current.Trace.Write("XeniZen.Engine", "Begin WebMail.Send");

			string errorMessage = string.Empty;
			MailMessage message = GetMessage(subject, body);
			message.From = from;
			try
			{
				foreach (MailAddress address in toUsers)
				{
					message.To.Add(address);
				}
				var token = new UserToken();
				//Sending the Mail  
				Send(message, token);
			}
			catch (Exception ex)
			{
				errorMessage = GetExceptionMessage(ex);
				throw;
			}
			if (HttpContext.Current != null)
				HttpContext.Current.Trace.Write("XeniZen.Engine", "End WebMail.Send");

			return errorMessage;
		}

		public void Send(MailMessage message)
		{
			Send(message, new UserToken());
		}

		public void Send(MailMessage message, UserToken token)
		{
			try
			{
				string mailSettingsAsyncEnabled = ConfigurationManager.AppSettings["MailSettingsAsyncEnabled"];
				bool sendAsyn = string.IsNullOrEmpty(mailSettingsAsyncEnabled) ? true : bool.Parse(mailSettingsAsyncEnabled);
				if (sendAsyn)
				{
					// Don't forget to set Async="True" on ASPX @Page or send will fail. 
					MailClient.SendAsync(message, new UserToken());
				}
				else
				{
					MailClient.Send(message);
				}
			}
			catch (Exception ex)
			{
				string errorMessage = GetExceptionMessage(ex);
				//throw ex;
				// We ned to send this meessage to someone later.
			}
		}

		#endregion

		#region Text Converters

		#region Delegates

		public delegate string ConvertHtmlToPlainDelegate(string html);

		#endregion

		public ConvertHtmlToPlainDelegate ConvertHtmlToPlain = HtmlToText;

		public static string GetPattern(string tag, params string[] attributes)
		{
			string pattern = @"<" + tag + @"\b(?>\s+(?:";
			for (int i = 0; i < attributes.Length; i++)
			{
				pattern += i > 0 ? "|" : "";
				pattern += attributes[i] + @"=""([^""]*)""";
			}
			pattern += @")|[^\s>]+|\s+)*>(.*?)</" + tag + ">";
			return pattern;
		}


		private static string aHrefEvaluator(Match m)
		{
			string result = string.Empty;
			string g5 = m.Groups[2].Value;
			// Remove all reminaing html tags
			g5 = Regex.Replace(g5, @"<[^>]*>", String.Empty, RegexOptions.IgnoreCase);
			if (!string.IsNullOrEmpty(g5))
			{
				result = string.Format("{0} ({1})", m.Groups[2], m.Groups[1].Value);
			}
			return result;
		}

		public static string HtmlToText(string result)
		{
			string pattern = GetPattern("a", "href");
			MatchCollection mc = Regex.Matches(result, pattern);
			result = Regex.Replace(result, pattern, aHrefEvaluator);
			result = result.Replace("\t", "");
			// Remove formatting that will prevent regex from running reliably
			// \r - Matches a carriage return \u000D.
			// \n - Matches a line feed \u000A.
			// \f - Matches a form feed \u000C.
			// For more details see http://msdn.microsoft.com/en-us/library/4edbef7e.aspx
			result = Regex.Replace(result, @"[\r\n\f]", String.Empty, RegexOptions.IgnoreCase);
			// Replace the most commonly used special characters.
			result = Regex.Replace(result, @"&lt;", "<", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&gt;", ">", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&nbsp;", " ", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&quot;", "\"\"", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&amp;", "&", RegexOptions.IgnoreCase);
			// Remove ASCII character code sequences such as &#nn; and &#nnn;
			result = Regex.Replace(result, "&#[0-9]{2,3};", String.Empty, RegexOptions.IgnoreCase);
			// Remove all other special characters. More can be added - see the following for more details:
			// http://www.degraeve.com/reference/specialcharacters.php
			// http://www.web-source.net/symbols.htm
			result = Regex.Replace(result, @"&.{2,6};", String.Empty, RegexOptions.IgnoreCase);
			// Remove all attributes and whitespace from the <head> tag
			result = Regex.Replace(result, @"< *head[^>]*>", "<head>", RegexOptions.IgnoreCase);
			// Remove all whitespace from the </head> tag
			result = Regex.Replace(result, @"< */ *head *>", "</head>", RegexOptions.IgnoreCase);
			// Delete everything between the <head> and </head> tags
			result = Regex.Replace(result, @"<head>.*</head>", String.Empty, RegexOptions.IgnoreCase);
			// Remove all attributes and whitespace from all <script> tags
			result = Regex.Replace(result, @"< *script[^>]*>", "<script>", RegexOptions.IgnoreCase);
			// Remove all whitespace from all </script> tags
			result = Regex.Replace(result, @"< */ *script *>", "</script>", RegexOptions.IgnoreCase);
			// Delete everything between all <script> and </script> tags
			result = Regex.Replace(result, @"<script>.*</script>", String.Empty, RegexOptions.IgnoreCase);
			// Remove all attributes and whitespace from all <style> tags
			result = Regex.Replace(result, @"< *style[^>]*>", "<style>", RegexOptions.IgnoreCase);
			// Remove all whitespace from all </style> tags
			result = Regex.Replace(result, @"< */ *style *>", "</style>", RegexOptions.IgnoreCase);
			// Delete everything between all <style> and </style> tags
			result = Regex.Replace(result, @"<style>.*</style>", String.Empty, RegexOptions.IgnoreCase);
			// Insert tabs in place of <td> tags
			result = Regex.Replace(result, @"< *td[^>]*>", "\t", RegexOptions.IgnoreCase);
			// Insert single line breaks in place of <br> and <li> tags
			result = Regex.Replace(result, @"< *br[^>]*>", "\r\n", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"< *li[^>]*>", "\r\n", RegexOptions.IgnoreCase);
			// Insert double line breaks in place of <p>, <div> and <tr> tags
			result = Regex.Replace(result, @"< *div[^>]*>", "\r\n" + "\r\n", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"< *tr[^>]*>", "\r\n" + "\r\n", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"< *p[^>]*>", "\r\n" + "\r\n", RegexOptions.IgnoreCase);
			// Remove all reminaing html tags
			result = Regex.Replace(result, @"<[^>]*>", String.Empty, RegexOptions.IgnoreCase);
			// Replace repeating spaces with a single space
			result = Regex.Replace(result, " +", " ");
			// Remove any trailing spaces and tabs from the end of each line
			result = Regex.Replace(result, @"[ \t]+\r\n", "\r\n");
			// Remove any leading whitespace characters
			result = Regex.Replace(result, @"^[\s]+", String.Empty);
			// Remove any trailing whitespace characters
			result = Regex.Replace(result, @"[\s]+$", String.Empty);
			// Remove extra line breaks if there are more than two in a row
			result = Regex.Replace(result, @"\r\n\r\n(\r\n)+", "\r\n" + "\r\n");
			//System.IO.File.WriteAllText(@"D:\temp\mail.txt", result);
			return result;
		}

		#endregion
	}
}