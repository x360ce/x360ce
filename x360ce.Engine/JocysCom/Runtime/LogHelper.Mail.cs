using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Mail;
using JocysCom.ClassLibrary.Mail;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class LogHelper
	{
		public SmtpClientEx Smtp
		{
			get { return SmtpClientEx.Current; }
		}

		public System.Net.Mail.MailMessage GetMailPreview(MailMessage message)
		{
			MailMessage mail = new MailMessage();
			mail.IsBodyHtml = true;
			SmtpClientEx.ApplyRecipients(mail, message.From, Smtp.ErrorRecipients);
			var subject = message.Subject;
			ApplyRunModeSuffix(ref subject);
			mail.Subject = subject;
			string testBody = "";
			testBody += "In LIVE mode this email would be sent:<br />\r\n";
			foreach (var item in message.To)
			{
				testBody += "To:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}
			foreach (var item in message.CC)
			{
				testBody += "Cc:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}
			foreach (var item in message.Bcc)
			{
				testBody += "Bcc:&nbsp;" + System.Web.HttpUtility.HtmlEncode(item.ToString()) + "<br />\r\n";
			}

			testBody += "<hr />\r\n";
			var attachments = message.Attachments;
			if (attachments != null && attachments.Count() > 0)
			{
				testBody += "These files would be attached:<br />\r\n";
				if (attachments != null && attachments.Count() > 0)
				{
					for (int ctr = 0; ctr <= attachments.Count() - 1; ctr++)
					{
						string fileName = attachments[ctr].Name;
						if (fileName.Length > 3 && fileName.ToLower().Substring(fileName.Length - 4) == ".ics")
						{
							mail.Attachments.Add(attachments[ctr]);
						}
						testBody += "&nbsp;&nbsp;&nbsp;&nbsp;" + System.Web.HttpUtility.HtmlEncode(fileName);
						testBody += "<br />\r\n";
					}
				}
			}
			if (message.IsBodyHtml)
			{
				testBody += message.Body;
			}
			else
			{
				testBody += "<pre>";
				testBody += System.Web.HttpUtility.HtmlEncode(message.Body);
				testBody += "</pre>";
			}
			mail.Body = testBody;
			return mail;
		}


		public static string GetSubjectPrefix(Exception ex, string suffix = "Error")
		{
			Assembly asm = Assembly.GetEntryAssembly();
			string a = "Unknown Entry Assembly";
			if (asm == null)
			{
				if (ex != null)
				{
					StackFrame[] frames = new StackTrace(ex).GetFrames();
					if (frames != null && frames.Length > 0)
					{
						asm = frames[0].GetMethod().DeclaringType.Assembly;
					}
				}
			}
			if (asm == null)
			{
				asm = Assembly.GetCallingAssembly();
			}
			if (asm != null)
			{
				var last2Nodes = asm.GetName().Name.Split('.').Reverse().Take(2).Reverse();
				a = string.Join(".", last2Nodes);
			}
			string s = string.Format("{0} {1}", a, suffix);
			ApplyRunModeSuffix(ref s);
			s += ": ";
			return s;
		}

		#region Send Mail

		public void SendWarningMail(string subject, string body, bool isBodyHtml = false)
		{
			Smtp.SendErrorEmail(null, subject, body);
		}

		public Exception SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml = false, bool preview = false, bool rethrow = false, string[] attachments = null, SmtpDeliveryMethod DeliveryMethod = SmtpDeliveryMethod.Network)
		{
			Exception ex = null;
			var att = GetAttachments(attachments);
			ex = SendMailFrom(@from, @to, cc, bcc, subject, body, isBodyHtml, preview, rethrow, att, DeliveryMethod);
			if (att != null)
			{
				foreach (var item in att)
				{
					item.Dispose();
				}
			}
			return ex;
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
		public Exception SendMailFrom(string @from, string @to, string cc, string bcc, string subject, string body, bool isBodyHtml, bool preview, bool rethrow, Attachment[] attachments, SmtpDeliveryMethod DeliveryMethod = SmtpDeliveryMethod.Network)
		{
			// Re-throw - throw the error again to catch by a caller
			try
			{
				var mail = new MailMessage();
				SmtpClientEx.ApplyRecipients(mail, @from, @to, cc, bcc);
				SmtpClientEx.ApplyAttachments(mail, attachments);
				mail.IsBodyHtml = isBodyHtml;
				mail.Subject = subject;
				mail.Body = body;
				if (!IsLive || preview)
				{
					mail = GetMailPreview(mail);
				}
				if (DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory)
				{
					Smtp.SmtpDeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
					Smtp.SmtpPickupFolder = SmtpClientEx.Current.SmtpPickupFolder;
				}
				Smtp.SendMessage(mail);
				return null;
			}
			catch (Exception ex)
			{
				if (!ex.Data.Contains("Mail.DeliveryMethod")) ex.Data.Add("Mail.DeliveryMethod", DeliveryMethod);
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
					ProcessException(ex);
				}
				return ex;
			}
		}

		public static Attachment[] GetAttachments(string[] files)
		{
			if (files == null) return null;
			var attachments = new List<Attachment>();
			for (int i = 0; i < files.Count(); i++)
			{
				string file = files[i];
				if (string.IsNullOrEmpty(file)) continue;
				if (System.IO.File.Exists(file))
				{
					// Specify as "application/octet-stream" so attachment will never will be embedded in body of email.
					var att = new System.Net.Mail.Attachment(file, "application/octet-stream");
					attachments.Add(att);
				}
			}
			return attachments.ToArray();
		}

		/// <summary>
		/// Mail will be sent to error recipient if not LIVE.
		/// </summary>
		/// <param name="message"></param>
		public void SendEmailWithCopyToErrorRecipients(MailMessage message)
		{
			var mail = IsLive ? message : GetMailPreview(message);
			Smtp.SendMessage(mail);
		}

		/// <summary>
		/// Suspend error if error code (int) value is found inside ex.Data["ErrorCode"].
		/// </summary>
		public bool SuspendError(Exception ex)
		{
			if (!ex.Data.Keys.Cast<object>().Contains(SmtpClientEx.ErrorCode))
				return false;
			var errorCode = ex.Data[SmtpClientEx.ErrorCode] as int?;
			if (!errorCode.HasValue)
				return false;
			var codes = Smtp.ErrorCodeSuspended;
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

		public ProcessExceptionDelegate ProcessExceptionMailFailed;

		/// <summary>
		/// Send email to developers and show the exception box
		/// </summary>
		public string ProcessException(Exception ex, string subject = null, bool processExtraAction = true)
		{
			var body = ExceptionInfo(ex, "");
			// Show exception first, because email can fail.
			var extra = ProcessExceptionExtra;
			// If set then execute extra exception actions
			if (processExtraAction && extra != null)
				extra(ex);
			// Email exception.
			var allowToReport = AllowReportExceptionToMail(ex);
			if (allowToReport && Smtp.ErrorNotifications && !SuspendError(ex))
			{
				// If processing exception fails then it should not be re-thrown or it will go into the loop.
				try
				{
					Smtp.SendErrorEmail(ex, subject, body);
				}
				catch (Exception ex2)
				{
					// Run method if email fails.
					var mailFailed = ProcessExceptionMailFailed;
					if (mailFailed != null)
						mailFailed(ex2);
				}
			}
			return body;
		}

		#endregion

		#region SPAM Prevention

		int? ErrorMailLimitMax;
		TimeSpan? ErrorMailLimitAge;
		Dictionary<Type, List<DateTime>> ErrorMailList = new Dictionary<Type, List<DateTime>>();

		public bool AllowReportExceptionToMail(Exception error)
		{
			// Maximum 10 errors of same type per 5 minutes (2880 per day).
			if (!ErrorMailLimitMax.HasValue)
				ErrorMailLimitMax = ParseInt("ErrorMailLimitMax", 5);
			if (!ErrorMailLimitAge.HasValue)
				ErrorMailLimitAge = ParseSpan("ErrorMailLimitAge", new TimeSpan(0, 5, 0));
			return AllowToReportException(error, ErrorMailList, ErrorMailLimitMax.Value, ErrorMailLimitAge.Value);
		}

		#endregion

	}
}
