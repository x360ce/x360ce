using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.IO;
using JocysCom.ClassLibrary.Runtime;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JocysCom.ClassLibrary.Mail
{

	/// <summary>
	/// Workaround for BUG.
	/// http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=94378
	/// 94378. SmtpClient should use FQDN for HELO/EHLO commands
	/// The current implementation of the System.Net.Mail.SmtpClient uses the NetBIOS name
	/// of the computer in the HELO/EHLO commands. Many anti-spam systems require the FQDN instead.
	/// As a result, email sent with the SmtpClient class is often blocked.
	/// </summary>
	public class SmtpClientEx : SmtpClient
	{

		private static SmtpClientEx _Current;
		private static object currentLock = new object();
		public static SmtpClientEx Current
		{
			get
			{
				lock (currentLock)
				{
					return _Current = _Current ?? new SmtpClientEx();
				}
			}
		}

		#region FQDN Fix

		private static readonly FieldInfo localHostName = GetLocalHostNameField();

		/// <summary>
		/// Returns the price "localHostName" field.
		/// </summary>
		/// <returns>
		/// The <see cref="FieldInfo"/> for the private
		/// "localHostName" field.
		/// </returns>
		private static FieldInfo GetLocalHostNameField()
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
			return typeof(SmtpClient).GetField("localHostName", flags);
		}


		/// <summary>
		/// Gets or sets the local host name used in SMTP transactions.
		/// </summary>
		/// <value>
		/// The local host name used in SMTP transactions.
		/// This should be the FQDN of the local machine.
		/// </value>
		/// <exception cref="ArgumentNullException">
		/// The property is set to a value which is
		/// <see langword="null"/> or <see cref="String.Empty"/>.
		/// </exception>
		public string LocalHostName
		{
			get
			{
				if (null == localHostName) return null;
				return (string)localHostName.GetValue(this);
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException("value");
				}
				if (null != localHostName)
				{
					localHostName.SetValue(this, value);
				}
			}
		}

		#endregion

		public SmtpClientEx(string host, int port) : base(host, port) { Initialize(); }

		public SmtpClientEx(string host) : base(host) { Initialize(); }

		public SmtpClientEx() { Initialize(); }


		public string SmtpUsername;
		public string SmtpPassword;
		public string SmtpDomain;
		public string SmtpServer;
		public bool SmtpEnableSsl;
		public string SmtpFrom;
		public string SmtpSendCopyTo;
		public string ErrorRecipients;
		public bool ErrorNotifications;
		public SmtpDeliveryMethod ErrorDeliveryMethod;
		public string ErrorPickupDirectory;


		private void Initialize()
		{
			// Load configuration
			SmtpUsername = LogHelper.ParseString("SmtpUsername", "");
			SmtpPassword = LogHelper.ParseString("SmtpPassword", "");
			SmtpDomain = LogHelper.ParseString("SmtpDomain", "");
			SmtpServer = LogHelper.ParseString("SmtpServer", "");
			SmtpEnableSsl = LogHelper.ParseBool("SmtpEnableSsl", false);
			SmtpFrom = LogHelper.ParseString("SmtpFrom", "");
			SmtpSendCopyTo = LogHelper.ParseString("SmtpSendCopyTo", "");
			ErrorRecipients = LogHelper.ParseString("ErrorRecipients", "");
			ErrorNotifications = LogHelper.ParseBool("ErrorNotifications", true);
			ErrorDeliveryMethod = LogHelper.ParseEnum("ErrorDeliveryMethod", SmtpDeliveryMethod.Network);
			ErrorPickupDirectory = LogHelper.ParseString("ErrorPickupDirectory", "Logs\\Errors");
			// FQDN Fix
			IPGlobalProperties ip = IPGlobalProperties.GetIPGlobalProperties();
			if (!string.IsNullOrEmpty(ip.HostName) && !string.IsNullOrEmpty(ip.DomainName))
			{
				LocalHostName = ip.HostName + "." + ip.DomainName;
			}
		}

		/// <summary>
		/// Same message as *.eml file.
		/// </summary>
		public static void Save(MailMessage message, string fileName)
		{
			var assembly = typeof(SmtpClient).Assembly;
			var _mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");
			using (var _fileStream = new FileStream(fileName, FileMode.Create))
			{
				var flags = BindingFlags.Instance | BindingFlags.NonPublic;
				// Get reflection info for MailWriter constructor
				var _mailWriterContructor = _mailWriterType.GetConstructor(flags, null, new Type[] { typeof(Stream) }, null);
				// Construct MailWriter object with our FileStream
				var _mailWriter = _mailWriterContructor.Invoke(new object[] { _fileStream });
				// Get reflection info for Send() method on MailMessage
				var _sendMethod = typeof(MailMessage).GetMethod("Send", flags);
				// Call method passing in MailWriter
				_sendMethod.Invoke(message, flags, null, new object[] { _mailWriter, true }, null);
				// Finally get reflection info for Close() method on our MailWriter
				var _closeMethod = _mailWriter.GetType().GetMethod("Close", flags);
				// Call close method
				_closeMethod.Invoke(_mailWriter, flags, null, new object[] { }, null);
			}
		}

		public void SendMessage(MailMessage message)
		{
			if (!string.IsNullOrEmpty(SmtpSendCopyTo))
			{
				AddSmtpSendCopyToRecipients(message, SmtpSendCopyTo);
			}
			if (ErrorDeliveryMethod == SmtpDeliveryMethod.Network)
			{

				// Send message to SMTP server.
				var client = new SmtpClient(SmtpServer);
				if (SmtpEnableSsl)
				{
					client.Port = 465;
					client.EnableSsl = true;
				}
				if (!string.IsNullOrEmpty(SmtpUsername))
				{
					client.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword, SmtpDomain);
				}
				client.Send(message);
			}
			else
			{
				// Save message to folder.
				var di = new DirectoryInfo(ErrorPickupDirectory);
				if (!di.Exists) di.Create();
				var fileTime = DateTime.Now;
				var fileName = string.Format("{0:yyyyMMdd_HHmmss_ffffff}.eml", fileTime);
				FileInfo fi = new FileInfo(Path.Combine(di.FullName, fileName));
				Save(message, fi.FullName);
			}
		}

		static List<MailAddress> ParseEmailAddress(string address)
		{
			var result = new List<MailAddress>();
			if (string.IsNullOrEmpty(address)) return result;
			var list = address.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in list)
			{
				var addres = item.Trim();
				if (string.IsNullOrEmpty(address)) continue;
				if (result.Any(x => x.Address == address)) continue;
				result.Add(new MailAddress(address));
			}
			return result;
		}

		/// <summary>
		/// Make sure that email copy was sent to listed recipients.
		/// </summary>
		/// <param name="message"></param>
		public void AddSmtpSendCopyToRecipients(MailMessage message, string recipients)
		{
			var addresses = ParseEmailAddress(recipients);
			var hosts = addresses.Select(x => x.Host.ToLower()).Distinct().ToArray();
			if (hosts.Length == 0) return;
			var anyTo = message.To.Any(x => hosts.Contains(x.Host.ToLower()));
			var anyCc = message.CC.Any(x => hosts.Contains(x.Host.ToLower()));
			var anyBcc = message.Bcc.Any(x => hosts.Contains(x.Host.ToLower()));
			var haveDomain = anyTo || anyCc || anyBcc;
			// If email with required domain is present or recipients are not specified then return.
			if (haveDomain) return;
			foreach (MailAddress item in addresses)
			{
				message.CC.Add(item);
			}
		}

		static readonly Regex RxBreaks = new Regex("[\r\n]", RegexOptions.Multiline);
		static readonly Regex RxMultiSpace = new Regex("[ \u00A0]+");

		/// <summary>
		/// Send exception details.
		/// </summary>
		/// <param name="ex">Exception to generate email from.</param>
		/// <param name="subject">Use custom subject instead of generated from exception</param>
		public MailMessage SendErrorEmail(Exception ex, string subject = null, string body = null)
		{
			var message = new MailMessage();
			message.From = new MailAddress(SmtpFrom);
			//------------------------------------------------------
			// Recipients
			//------------------------------------------------------
			string[] recipients = null;
			recipients = ErrorRecipients.Replace(",", ";").Split(';');
			foreach (string addr in recipients)
			{
				message.To.Add(new MailAddress(addr));
			}
			//------------------------------------------------------
			// Subject
			//------------------------------------------------------
			if (ex.Data != null)
			{
				var key = ex.Data.Keys.Cast<object>().FirstOrDefault(x => object.ReferenceEquals(x, "StackTrace"));
				if (key != null && ex.Data[key] is StackTrace)
				{
					ex.Data.Remove(key);
				}
			}
			// If subject was not specified and exception found then...
			if (string.IsNullOrEmpty(subject) && ex != null)
			{
				subject = LogHelper.GetSubjectPrefix(ex) + ex.Message;
			}
			if (string.IsNullOrEmpty(subject))
			{
				subject = "null";
			}
			subject = RxBreaks.Replace(subject, " ");
			subject = RxMultiSpace.Replace(subject, " ");
			message.Body = body;
			try
			{
				// Cut subject because some mail servers refuse to deliver messages when subject is too large.
				var maxLength = 255;
				message.Subject = (subject.Length > maxLength)
					? subject.Substring(0, maxLength - 3) + "..."
					: subject;
			}
			catch (Exception)
			{
				message.Subject = "Bad subject";
				message.Body += "<div>Subject:" + subject + "</div>\r\n";
			}
			message.IsBodyHtml = true;
			//------------------------------------------------------
			SendMessage(message);
			return message;
		}

	}
}
