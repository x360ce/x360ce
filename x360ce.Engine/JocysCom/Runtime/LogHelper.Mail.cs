using JocysCom.ClassLibrary.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{

		#region Send Mail

		/// <summary>
		/// This is the main function. All other methods in this class must call it and all emails must be sent trough it.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="client"></param>
		public virtual void SendMail(MailMessage message, SmtpClientEx client = null, bool forcePreview = false)
		{
			var smtp = client ?? SmtpClientEx.Current;
			// Send preview if forced preview or non error recipient found.
			var sendPreview = forcePreview || NonErrorRecipientsFound(message);
			// If not LIVE environment then send preview message to developers instead.
			if (sendPreview)
				message = GetMailPreview(message, smtp);
			string fileName;
			smtp.SendMessage(message, out fileName);
			// Dispose preview message.
			if (sendPreview)
				message.Dispose();
		}

		/// <summary>
		/// Returns true of only error recipients found.
		/// </summary>
		public bool NonErrorRecipientsFound(MailMessage message, List<MailAddress> extraErrorRecipients = null)
		{
			var list = MailHelper.ParseEmailAddress(SmtpClientEx.Current.ErrorRecipients);
			// Add recipients who are allowed to receive original emails.
			if (extraErrorRecipients != null)
				list = list.Union(extraErrorRecipients).ToList();
			var addresses = list.Select(x => x.Address.ToUpper());
			// Merge recipients with 'Union' command and return true if non error recipients found.
			return message
				.To.Union(message.CC).Union(message.Bcc)
				.Select(x => x.Address.ToUpper())
				.Except(addresses).Count() > 0;
		}

		public Exception SendMail(string to, string subject, string body, bool isBodyHtml = false)
		{
			return SendMailFrom(SmtpClientEx.Current.SmtpFrom, @to, null, null, subject, body, isBodyHtml);
		}

		/// <summary>
		/// Mail will be sent to error recipient if not LIVE.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="cc"></param>
		/// <param name="bcc"></param>
		/// <param name="subject"></param>
		/// <param name="body"></param>
		/// <param name="isBodyHtml"></param>
		/// <param name="preview">Force preview on LIVE system.</param>
		/// <param name="rethrow">Throw exception if sending fails. Must be set to false when sending exceptions.</param>
		/// <param name="attachments"></param>
		public Exception SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml, bool rethrow = false, string[] attachments = null, SmtpDeliveryMethod? overrideDeliveryMethod = null, bool forcePreview = false)
		{
			Exception error = null;
			// Re-throw - throw the error again to catch by a caller
			var message = new MailMessage();
			var smtp = new SmtpClientEx();
			try
			{
				MailHelper.ApplyRecipients(message, @from, @to, cc, bcc);
				MailHelper.ApplyAttachments(message, attachments);
				message.IsBodyHtml = isBodyHtml;
				message.Subject = subject;
				message.Body = body;
				// Override delivery method.
				if (overrideDeliveryMethod.HasValue)
					smtp.DeliveryMethod = overrideDeliveryMethod.Value;
				SendMail(message, smtp, forcePreview);
			}
			catch (Exception ex)
			{
				if (!ex.Data.Contains("Mail.DeliveryMethod")) ex.Data.Add("Mail.DeliveryMethod", overrideDeliveryMethod);
				if (!ex.Data.Contains("Mail.From")) ex.Data.Add("Mail.From", @from);
				if (!string.IsNullOrEmpty(@to) && !ex.Data.Contains("Mail.To")) ex.Data.Add("Mail.To", @to);
				if (!string.IsNullOrEmpty(cc) && !ex.Data.Contains("Mail.Cc")) ex.Data.Add("Mail.Cc", cc);
				if (!string.IsNullOrEmpty(bcc) && !ex.Data.Contains("Mail.Bcc")) ex.Data.Add("Mail.Bcc", bcc);
				if (!string.IsNullOrEmpty(subject) && !ex.Data.Contains("Mail.Subject")) ex.Data.Add("Mail.Subject", subject);
				// Will be processed by the caller.
				if (rethrow)
				{
					throw;
				}
				else
				{
					Current.ProcessException(ex);
				}
				error = ex;
			}
			finally
			{
				// Attachments and message will be disposed.
				message.Dispose();
				smtp.Dispose();
			}
			return error;
		}

		/// <summary>
		/// Send exception details as HTML e-mail.
		/// </summary>
		/// <param name="ex">Exception to generate email from.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		/// <param name="body">Extra body text above exception.</param>
		public void SendException(Exception ex, string subject = null, string body = null)
		{
			_GroupException(mailExceptions, ex, subject, body, _SendMail);
		}

		/// <summary>
		/// Send exception details as HTML e-mail.
		/// </summary>
		/// <param name="ex">Exception to generate email from.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		void _SendMail(Exception ex, string subject, string body)
		{
			var smtp = SmtpClientEx.Current;
			var message = new MailMessage();
			MailHelper.ApplyRecipients(message, smtp.SmtpFrom, smtp.ErrorRecipients);
			message.Subject = subject;
			message.Body = Current.ExceptionInfo(ex, body);
			message.IsBodyHtml = true;
			SendMail(message);
			message.Dispose();
		}

		public ProcessExceptionDelegate ProcessExceptionMailFailed;

		/// <summary>
		/// Send email to developers and show the exception box (optional)
		/// </summary>
		public string ProcessException(Exception ex, string subject = null, string body = null, bool processExtraAction = true)
		{
			// Show exception first, because email can fail.
			var extra = ProcessExceptionExtra;
			// If set then execute extra exception actions
			if (processExtraAction && extra != null)
				extra(ex);
			// Email exception.
			if (SmtpClientEx.Current.ErrorNotifications && !SuspendError(ex))
			{
				// If processing exception fails then it should not be re-thrown or it will go into the loop.
				try
				{
					SendException(ex, subject, body);
				}
				catch (Exception ex2)
				{
					// Run method if email fails.
					var mailFailed = ProcessExceptionMailFailed;
					if (mailFailed != null)
						mailFailed(ex2);
				}
			}
			var messageBody = ExceptionInfo(ex, body);
			return messageBody;
		}

		#endregion

		#region Mail Format and Preview

		public static string GetMailHeader(MailMessage message)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("From: {0}\r\n", message.From);
			foreach (var item in message.To)
				sb.AppendFormat("To:   {0}\r\n", item);
			foreach (var item in message.CC)
				sb.AppendFormat("CC:   {0}\r\n", item);
			foreach (var item in message.Bcc)
				sb.AppendFormat("Bcc:   {0}\r\n", item);
			// Write list of files.
			var files = message.Attachments;
			if (files != null && files.Count > 0)
			{
				var maxNumbers = files.Count.ToString().Length;
				var maxContent = files.Max(x => x.ContentStream.Length.ToString().Length);
				for (int i = 0; i < files.Count; i++)
					sb.AppendFormat("File[{0}]: {1," + maxContent + "} bytes - {2}\r\n", i, files[i].ContentStream.Length, files[i].ContentType);
			}
			// Write list of views.
			var views = message.AlternateViews;
			if (views != null && views.Count > 0)
			{
				var maxNumbers = views.Count.ToString().Length;
				var maxContent = views.Max(x => x.ContentStream.Length.ToString().Length);
				for (int i = 0; i < views.Count; i++)
					sb.AppendFormat("View[{0}]: {1," + maxContent + "} bytes - {2}\r\n", i, views[i].ContentStream.Length, views[i].ContentType);
			}
			sb.AppendFormat("Subject: {0}\r\n", message.Subject);
			sb.AppendFormat("Body Size: {0} bytes\r\n", string.Format("{0}", message.Body).Length);
			var headers = message.Headers;
			if (headers != null && headers.Count > 0)
			{
				sb.AppendLine("Headers:");
				for (int i = 0; i < headers.Count; i++)
				{
					var maxContent = headers.AllKeys.Max(x => string.Format("{0}", x).Length.ToString().Length);
					foreach (var key in headers.AllKeys)
						sb.AppendFormat("  {0,-" + maxContent + "}: {1}\r\n", key, headers[key]);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Create preview email message from original email message.
		/// </summary>
		/// <param name="message">original email message.</param>
		/// <returns>Preview Message.</returns>
		public static MailMessage GetMailPreview(MailMessage message, SmtpClientEx client = null)
		{
			var smtp = SmtpClientEx.Current;
			var mail = new MailMessage();
			mail.IsBodyHtml = true;
			Mail.MailHelper.ApplyRecipients(mail, smtp.SmtpFrom, smtp.ErrorRecipients);
			var subject = message.Subject;
			ApplyRunModeSuffix(ref subject);
			mail.Subject = subject;
			string testBody = "";
			testBody += "In LIVE mode this email would be sent:<br /><br />\r\n";
			testBody += "<pre>";
			testBody += string.Format("Source: {0}\r\n", GetSubjectPrefix().Trim(' ', ':'));
			testBody += System.Net.WebUtility.HtmlEncode(GetMailHeader(message));
			if (client != null)
			{
				if (client.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory)
				{
					testBody += string.Format("Delivery Method: {0}\r\n", client.DeliveryMethod);
					testBody += string.Format("Delivery Folder: {0}\r\n", client.PickupDirectoryLocation);
				}
			}
			testBody += "</pre>";
			testBody += "<hr />\r\n";
			if (message.IsBodyHtml)
			{
				testBody += message.Body;
			}
			else
			{
				testBody += "<pre>";
				testBody += System.Net.WebUtility.HtmlEncode(message.Body);
				testBody += "</pre>";
			}
			mail.Body = testBody;
			// Read attachments.
			var files = message.Attachments;
			if (files != null)
			{
				for (int i = 0; i < files.Count; i++)
				{
					var name = files[i].Name;
					// Re-attach calendar item.
					if (name.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
					{
						mail.Attachments.Add(files[i]);
					}
				}
			}
			// Read views
			var views = message.AlternateViews;
			if (views != null)
			{
				for (int i = 0; i < views.Count; i++)
				{
					var view = views[i];
					var content = view.ContentType;
					var name = content.Name;
					// Re-attach calendar item.
					if (name.EndsWith(".ics", StringComparison.OrdinalIgnoreCase))
					{
						// Add calendar item as attachment.
						var attachment = new Attachment(view.ContentStream, name, content.MediaType);
						mail.Attachments.Add(attachment);

						var reader = new System.IO.StreamReader(view.ContentStream);
						var calendar = reader.ReadToEnd();


					}
				}
			}
			return mail;
		}

		#endregion

		/// <summary>
		/// Suspend error if error code (int) value is found inside ex.Data["ErrorCode"].
		/// </summary>
		public bool SuspendError(Exception ex)
		{
			if (!ex.Data.Keys.Cast<object>().Contains(Mail.SmtpClientEx.ErrorCode))
				return false;
			var errorCode = ex.Data[Mail.SmtpClientEx.ErrorCode] as int?;
			if (!errorCode.HasValue)
				return false;
			var codes = SmtpClientEx.Current.ErrorCodeSuspended;
			if (string.IsNullOrEmpty(codes))
				return false;
			var codeStrings = codes.Split(new[] { ' ', ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var codeString in codeStrings)
			{
				int code;
				if (int.TryParse(codeString, out code))
				{
					if (code == errorCode.Value)
						return true;
				}
			}
			return false;
		}


	}
}
