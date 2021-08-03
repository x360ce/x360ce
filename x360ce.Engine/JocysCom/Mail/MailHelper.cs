using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Mail
{

	// Use log LogHelper to send emails.
	public class MailHelper
	{


#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework

		public virtual MailAddress[] GetMailAddress(Guid[] userIds)
		{
			return userIds.Select(x => GetMailAddress(x)).ToArray();
		}

		public virtual MailAddress GetMailAddress(Guid userId)
		{
			var user = System.Web.Security.Membership.GetUser(userId);
			if (user == null)
				throw new Exception("User with Id '" + userId + "' doesn't exist!");
			return new MailAddress(user.Email, user.UserName);
		}

		public virtual MailAddress[] GetMailAddress(string[] usernames)
		{
			return usernames.Select(x => GetMailAddress(x)).ToArray();
		}

		public virtual MailAddress GetMailAddress(string username)
		{
			var user = System.Web.Security.Membership.GetUser(username);
			if (user == null)
				throw new Exception("User '" + username + "' doesn't exist!");
			return new MailAddress(user.Email, user.UserName);
		}

#endif

		#region GetMailAddress - ASP.NET Membership Provider

		#endregion

		/// <summary>
		/// Create message.
		/// </summary>
		/// <param name="subject">Message subject.</param>
		/// <param name="body">Message body.</param>
		/// <param name="bodyType">Type of body (MediaTypeNames.Text)</param>
		/// <returns>System.Net.Mail.MailMessage</returns>
		public static MailMessage GetMessage(string subject, string body, MailTextType bodyType = MailTextType.Auto)
		{
			if (bodyType == MailTextType.Auto)
				bodyType = IsHtml(body) ? MailTextType.Html : MailTextType.Plain;
			var message = new MailMessage()
			{
				Subject = subject,
				SubjectEncoding = Encoding.UTF8,
				BodyEncoding = Encoding.UTF8,
			};
			// Framework selects a TransferEncoding of Base64 if you set the BodyEncoding property to UTF8, Unicode or UTF32. 
			// So we must add alternate view for some old crappy browsers which doesn't support Base64 TransferEncoding.
			var plainBody = string.Empty;
			AlternateView htmlView = null;
			switch (bodyType)
			{
				case MailTextType.Html:
					// Add HTML view.
					htmlView = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, MediaTypeNames.Text.Html);
					// Very important
					htmlView.TransferEncoding = TransferEncoding.SevenBit;
					// Mark message as HTML.																		   
					message.IsBodyHtml = true;
					// Create alternative plain body.
					plainBody = HtmlToText(body);
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
			var plainView = AlternateView.CreateAlternateViewFromString(plainBody, Encoding.UTF8, MediaTypeNames.Text.Plain);
			// Very important
			plainView.TransferEncoding = TransferEncoding.SevenBit;
			// Add views.
			message.AlternateViews.Add(plainView);
			// "text/html" part must be last in order for gmail to display it.
			if (htmlView != null)
				message.AlternateViews.Add(htmlView);
			return message;
		}

		#region HTML Validation

		private static readonly Regex _htmlTag = new Regex("</?\\w+((\\s+\\w+(\\s*=\\s*(?:\".*?\"|'.*?'|[^'\">\\s]+))?)+\\s*|\\s*)/?>");

		public static bool IsHtml(string s)
		{
			return _htmlTag.IsMatch(s);
		}

		#endregion

		#region Apply Recipients and Attachments

		public static void ApplyRecipients(MailMessage mail, string addFrom, string addTo, string addCc = null, string addBcc = null)
		{
			if (string.IsNullOrEmpty(addFrom))
				throw new ArgumentNullException(nameof(addFrom));
			ApplyRecipients(mail, new MailAddress(addFrom), addTo, addCc, addBcc);
		}

		public static void ApplyRecipients(ICollection<MailAddress> collection, string emails)
		{
			string[] list = null;
			if (string.IsNullOrEmpty(emails))
				return;
			list = emails.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var item in list)
			{
				// If address is empty then continue.
				if (string.IsNullOrEmpty(item.Trim()))
					continue;
				var a = new MailAddress(item.Trim());
				// If address is on the list already then continue.
				var exists = collection.Any(x =>
					string.Compare(x.Address, a.Address, true) == 0 &&
					string.Compare(x.DisplayName, a.DisplayName, true) == 0
				);
				if (exists)
					continue;
				collection.Add(a);
			}
		}

		public static void ApplyRecipients(MailMessage mail, MailAddress addFrom, string addTo, string addCc = null, string addBcc = null)
		{
			mail.From = addFrom;
			ApplyRecipients(mail.To, addTo);
			ApplyRecipients(mail.CC, addCc);
			ApplyRecipients(mail.Bcc, addBcc);
		}

		public static void ApplyAttachments(MailMessage message, params string[] files)
		{
			var list = new List<Attachment>();
			if (files == null)
				return;
			for (var i = 0; i < files.Count(); i++)
			{
				var file = files[i];
				if (string.IsNullOrEmpty(file))
					continue;
				// Specify as "application/octet-stream" so attachment will never will be embedded in body of email.
				var att = new Attachment(file, "application/octet-stream");
				list.Add(att);
			}
			ApplyAttachments(message, list.ToArray());
		}

		public static void ApplyAttachments(MailMessage message, params Attachment[] files)
		{
			if (files == null)
				return;
			for (var i = 0; i < files.Count(); i++)
				message.Attachments.Add(files[i]);
		}

		#endregion

		#region Convert Text To HTML

		/// <summary>
		/// Create alternative view from HTML.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public static string HtmlToText(string result)
		{
			var pattern = GetPattern("a", "href");
			var mc = Regex.Matches(result, pattern);
			result = Regex.Replace(result, pattern, aHrefEvaluator);
			result = result.Replace("\t", "");
			// Remove formatting that will prevent regex from running reliably
			// \r - Matches a carriage return \u000D.
			// \n - Matches a line feed \u000A.
			// \f - Matches a form feed \u000C.
			// For more details see http://msdn.microsoft.com/en-us/library/4edbef7e.aspx
			result = Regex.Replace(result, @"[\r\n\f]", string.Empty, RegexOptions.IgnoreCase);
			// Replace the most commonly used special characters.
			result = Regex.Replace(result, @"&lt;", "<", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&gt;", ">", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&nbsp;", " ", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&quot;", "\"\"", RegexOptions.IgnoreCase);
			result = Regex.Replace(result, @"&amp;", "&", RegexOptions.IgnoreCase);
			// Remove ASCII character code sequences such as &#nn; and &#nnn;
			result = Regex.Replace(result, "&#[0-9]{2,3};", string.Empty, RegexOptions.IgnoreCase);
			// Remove all other special characters. More can be added - see the following for more details:
			// http://www.degraeve.com/reference/specialcharacters.php
			// http://www.web-source.net/symbols.htm
			result = Regex.Replace(result, @"&.{2,6};", string.Empty, RegexOptions.IgnoreCase);
			// Remove all attributes and whitespace from the <head> tag
			result = Regex.Replace(result, @"< *head[^>]*>", "<head>", RegexOptions.IgnoreCase);
			// Remove all whitespace from the </head> tag
			result = Regex.Replace(result, @"< */ *head *>", "</head>", RegexOptions.IgnoreCase);
			// Delete everything between the <head> and </head> tags
			result = Regex.Replace(result, @"<head>.*</head>", string.Empty, RegexOptions.IgnoreCase);
			// Add tabs
			result = ReplaceTags(result, "\t", "li", "tr");
			// Replace blocks.
			var block = "address|article|aside|blockquote|canvas|dd|div|dl|dt|" +
			  "fieldset|figcaption|figure|footer|form|h\\d|header|hr|li|main|nav|" +
			  "noscript|output|p|pre|section|table|tfoot|video";
			var patNestedBlock = $"(\\s*?</?({block})[^>]*?>)+\\s*";
			result = Regex.Replace(result, patNestedBlock, "\r\n", RegexOptions.IgnoreCase);
			// Replace blocks with double return.
			var blockDR = "ul|ol";
			var patNestedBlockDR = $"(\\s*?</?({blockDR})[^>]*?>)+\\s*";
			result = Regex.Replace(result, patNestedBlockDR, "\r\n\r\n", RegexOptions.IgnoreCase);
			// Replace to newline.
			result = Regex.Replace(result, @"<(br)[^>]*>", "\r\n", RegexOptions.IgnoreCase);
			// Remove styles and scripts.
			result = Regex.Replace(result, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline);
			// Remove all remaining html tags
			result = Regex.Replace(result, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);
			// Replace HTML entities.
			result = System.Net.WebUtility.HtmlDecode(result);
			// Replace repeating spaces with a single space
			result = Regex.Replace(result, " +", " ");
			// Remove any trailing spaces and tabs from the end of each line
			result = Regex.Replace(result, @"[ \t]+\r\n", "\r\n");
			// Remove any leading whitespace characters
			result = Regex.Replace(result, @"^[\s]+", string.Empty);
			// Remove any trailing whitespace characters
			result = Regex.Replace(result, @"[\s]+$", string.Empty);
			// Remove extra line breaks if there are more than two in a row
			result = Regex.Replace(result, @"\r\n\r\n(\r\n)+", "\r\n" + "\r\n");
			return result;
		}

		private static string ReplaceTags(string input, string replacement, params string[] tags)
		{
			foreach (var tag in tags)
				input = Regex.Replace(input, "< *" + tag + "[^>]*>", "<" + tag + ">" + replacement, RegexOptions.IgnoreCase);
			return input;
		}


		private static string GetPattern(string tag, params string[] attributes)
		{
			var pattern = @"<" + tag + @"\b(?>\s+(?:";
			for (var i = 0; i < attributes.Length; i++)
			{
				pattern += i > 0 ? "|" : "";
				pattern += attributes[i] + @"=""([^""]*)""";
			}
			pattern += @")|[^\s>]+|\s+)*>(.*?)</" + tag + ">";
			return pattern;
		}

		private static string aHrefEvaluator(Match m)
		{
			var result = string.Empty;
			var g5 = m.Groups[2].Value;
			// Remove all reminaing html tags
			g5 = Regex.Replace(g5, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);
			if (!string.IsNullOrEmpty(g5))
			{
				result = string.Format("{0} ({1})", m.Groups[2], m.Groups[1].Value);
			}
			return result;
		}

		#endregion

		#region Email Validation

		public static EmailResult EmailValid(string email)
		{
			// evaluate email address for formal validity
			email = email.Trim();
			if (string.IsNullOrEmpty(email))
				return EmailResult.Empty;
			var emails = email.Split(';');
			// take care of list of addresses separated by semicolon
			foreach (var s in emails)
			{
				var sEmail = s.Trim();
				// The email address cannot end with a semicolon.
				if (string.IsNullOrEmpty(sEmail))
					return EmailResult.Semicolon;
				// Not a valid email address.
				if (!IsValidEmail(sEmail))
					return EmailResult.Invalid;
			}
			// If we got here, email is OK.
			return EmailResult.OK;
		}

		// General Email RegEx (RFC 5322 Official Standard): http://emailregex.com/
		private static readonly string emailRegexRFC5322 = @"(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])";

		public static Regex EmailRegex
		{
			set { _emailRegex = value; }
			get
			{
				if (_emailRegex == null)
					_emailRegex = new Regex(emailRegexRFC5322, RegexOptions.IgnoreCase);
				return _emailRegex;
			}
		}

		private static Regex _emailRegex;


		public static bool IsValidEmail(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;
			var match = EmailRegex.Match(s);
			//  Make sure that full string matched.
			return match.Success && match.Value.Length == s.Length;
		}

		public static bool IsValidEmail(string email, bool mandatory, out string message)
		{
			var result = EmailValid(email);
			message = Runtime.Attributes.GetDescription(result);
			switch (result)
			{
				case EmailResult.OK:
					return true;
				case EmailResult.Empty:
					return !mandatory;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse email address or email address list split with ';' or ','.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static List<MailAddress> ParseEmailAddress(string address)
		{
			var result = new List<MailAddress>();
			if (string.IsNullOrEmpty(address))
				return result;
			var list = address.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var item in list)
			{
				var a = item.Trim();
				if (string.IsNullOrEmpty(a))
					continue;
				if (result.Any(x => x.Address == a))
					continue;
				result.Add(new MailAddress(a));
			}
			return result;
		}

		#endregion

	}
}
