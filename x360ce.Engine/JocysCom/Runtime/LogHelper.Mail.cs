using JocysCom.ClassLibrary.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{

		#region Send Mail

		/// <summary>
		/// This is the main function. All other methods in this class must call it and all emails must be sent trough it.
		/// Central mail-sending entry point: Validates inputs, selects an <see cref="SmtpClientEx"/> instance (defaulting to Current), applies non-live environment preview logic (overridable via <paramref name="forcePreview"/>), sends the preview or original message, and disposes any preview message.
		/// </summary>
		/// <param name="message">The <see cref="MailMessage"/> to send. Throws <see cref="ArgumentNullException"/> if null.</param>
		/// <param name="client">Optional <see cref="SmtpClientEx"/> instance; if null, uses <see cref="SmtpClientEx.Current"/>.</param>
		/// <param name="forcePreview">Force preview on LIVE system. If true, forces sending as a preview even in live environments.</param>
		public virtual void SendMail(MailMessage message, SmtpClientEx client = null, bool forcePreview = false)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			var smtp = client ?? SmtpClientEx.Current;
			// If non-live environment then send preview, except if all recipients are known.
			var sendPreview = !IsLive && NonErrorRecipientsFound(message);
			// If not LIVE environment then send preview message to developers instead.
			MailMessage preview = null;
			if (forcePreview || sendPreview)
				preview = GetMailPreview(message, smtp);
			string fileName;
			// Send preview if available, otherwise send original message.
			smtp.SendMessage(preview ?? message, out fileName);
			// Dispose preview message.
			if (preview != null)
				preview.Dispose();
		}

		/// <summary>
		/// Returns true of only error recipients found.
		/// Determines whether the message has any recipients outside the configured error recipients, optionally including extraErrorRecipients in the error list.
		/// </summary>
		/// <param name="message">The <see cref="MailMessage"/> to inspect. Throws <see cref="ArgumentNullException"/> if null.</param>
		/// <param name="extraErrorRecipients">Additional <see cref="MailAddress"/> entries treated as error recipients.</param>
		/// <returns>True if at least one non-error recipient is found; otherwise false.</returns>
		public static bool NonErrorRecipientsFound(MailMessage message, List<MailAddress> extraErrorRecipients = null)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));
			var list = MailHelper.ParseEmailAddress(SmtpClientEx.Current.ErrorRecipients);
			// Add recipients who are allowed to receive original emails.
			if (extraErrorRecipients != null)
				list = list.Union(extraErrorRecipients).ToList();
			var addresses = list.Select(x => x.Address.ToUpper());
			// Merge recipients with 'Union' command and return true if non error recipients found.
			return message
				.To.Union(message.CC).Union(message.Bcc)
				.Select(x => x.Address.ToUpper())
				.Except(addresses).Any();
		}

		/// <summary>
		/// Convenience method to send an email from the default sender to a single recipient.
		/// </summary>
		/// <param name="to">Recipient email address(es) for the To field.</param>
		/// <param name="subject">Email subject text.</param>
		/// <param name="body">Email body content.</param>
		/// <param name="isBodyHtml">True to send the body as HTML; false for plain text.</param>
		/// <returns>Any exception encountered during send, or null if successful.</returns>
		public Exception SendMail(string to, string subject, string body, bool isBodyHtml = false)
		{
			return SendMailFrom(SmtpClientEx.Current.SmtpFrom, @to, null, null, subject, body, isBodyHtml);
		}

		/// <summary>
		/// Mail will be sent to error recipient if not LIVE.
		/// Constructs and sends an email with full settings: sender, recipients, attachments, HTML flag, optional delivery method override, and preview. On exception, enriches the exception Data with delivery context, disposes resources, and either rethrows or returns the exception based on rethrow flag.
		/// </summary>
		/// <param name="from">Sender address.</param>
		/// <param name="to">Comma-separated recipient addresses for the To field.</param>
		/// <param name="cc">Comma-separated addresses for the CC field.</param>
		/// <param name="bcc">Comma-separated addresses for the BCC field.</param>
		/// <param name="subject">Email subject line.</param>
		/// <param name="body">Email body content.</param>
		/// <param name="isBodyHtml">True to send the body as HTML; false for plain text.</param>
		/// <param name="rethrow">Throw exception if sending fails. Must be set to false when sending exceptions.</param>
		/// <param name="attachments">Array of file paths to attach to the email.</param>
		/// <param name="overrideDeliveryMethod">Optional override for the SMTP delivery method.</param>
		/// <param name="forcePreview">Force preview on LIVE system.</param>
		/// <returns>The exception encountered during send, or null if successful.</returns>
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
		/// Formats exception details into an HTML email and sends it based on configured error notification settings. Returns immediately if notifications are disabled or <see cref="LogToMail"/> is false.
		/// </summary>
		/// <param name="ex">Exception to generate email from.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		/// <param name="body">Extra body text above exception.</param>
		public void SendException(Exception ex, string subject = null, string body = null)
		{
			if (!(SmtpClientEx.Current.ErrorNotifications || LogToMail))
				return;
			_GroupException(mailExceptions, ex, subject, body, _SendMail);
		}

		/// <summary>
		/// Send exception details as HTML e-mail.
		/// Internal helper to apply headers for server-side grouping (ErrorSource, ErrorType, ErrorCode) and invoke the main <see cref="SendMail"/> logic.
		/// </summary>
		/// <param name="ex">Exception to generate email from.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		/// <param name="body">Extra body text above exception.</param>
		void _SendMail(Exception ex, string subject, string body)
		{
			var smtp = SmtpClientEx.Current;
			var m = new MailMessage();
			MailHelper.ApplyRecipients(m, smtp.SmtpFrom, smtp.ErrorRecipients);
			// If exception supplied then...
			if (ex != null)
			{
				// Add headers, which can be used on server side to group errors.
				if (!string.IsNullOrEmpty(ex.Source))
					m.Headers.Add(XLogHelperErrorSource, ex.Source);
				m.Headers.Add(XLogHelperErrorType, ex.GetType().FullName);
				m.Headers.Add(XLogHelperErrorCode, ex.HResult.ToString());
				body = Current.ExceptionInfo(ex, body);
			}
			m.Subject = subject;
			m.Body = body;
			m.IsBodyHtml = true;
			SendMail(m);
			m.Dispose();
		}

		public ProcessExceptionDelegate ProcessExceptionMailFailed;

		/// <summary>
		/// Send email to developers and show the exception box (optional).
		/// Executes configured extra exception actions, emails error information with guards, and returns the formatted exception message.
		/// </summary>
		/// <param name="ex">The exception to process.</param>
		/// <param name="subject">Optional custom email subject for notification.</param>
		/// <param name="body">Optional additional message text appended to the notification.</param>
		/// <param name="processExtraAction">If true, invokes extra exception actions; otherwise skips them.</param>
		/// <returns>The formatted exception message returned by <see cref="ExceptionInfo"/>.</returns>
		public string ProcessException(Exception ex, string subject = null, string body = null, bool processExtraAction = true)
		{
			// Process global (there is a chance that app settings are not available yet.
			var extra = ProcessExceptionExtraGlobal;
			// If set then execute extra exception actions
			if (processExtraAction && extra != null)
				extra(ex);
			// Show exception first, because email can fail.
			extra = ProcessExceptionExtra;
			// If set then execute extra exception actions
			if (processExtraAction && extra != null)
				extra(ex);
			// Email exception.
			if ((SmtpClientEx.Current.ErrorNotifications || LogToMail) && !SuspendError(ex))
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

		/// <summary>
		/// Builds a plain-text header block for a <see cref="MailMessage"/>, including From, To, CC, Bcc,
		/// attachments, alternate views, subject, body size, and custom headers.
		/// </summary>
		/// <param name="message">The <see cref="MailMessage"/> to generate headers for; throws <see cref="ArgumentNullException"/> if null.</param>
		/// <returns>A formatted string of mail headers suitable for preview or logging.</returns>
		public static string GetMailHeader(MailMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));
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
		/// Generates an HTML preview containing headers, live-mode notice, original body content, and re-attaches calendar items as attachments.
		/// </summary>
		/// <param name="message">original email message.</param>
		/// <param name="client">Optional <see cref="SmtpClientEx"/> instance for pickup directory info; ignored if null.</param>
		/// <returns>Preview Message.</returns>
		public static MailMessage GetMailPreview(MailMessage message, SmtpClientEx client = null)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));
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
						//var reader = new System.IO.StreamReader(view.ContentStream);
						//var calendar = reader.ReadToEnd();
						//reader.Dispose();
					}
				}
			}
			return mail;
		}
		#endregion
		/// <summary>
		/// Suspend error if error code (int) value is found inside ex.Data["ErrorCode"].
		/// </summary>
		public static bool SuspendError(Exception ex)
		{
			if (ex is null)
				throw new ArgumentNullException(nameof(ex));
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
