using JocysCom.ClassLibrary.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
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
		public int SmtpPort;
		public IPAddress SmtpLocalAddress;
		public int SmtpLocalPort;
		public string SmtpPickupFolder;
		public bool SmtpEnableSsl;
		public bool SmtpFixHostName;
		public string SmtpFrom;
		public string SmtpSendCopyTo;
		public string ErrorRecipients;

		public const string ErrorCode = "ErrorCode";

		public static void SetErrorCode(Exception ex, int errorCode)
		{
			if (errorCode == 0)
				return;
			if (ex.Data.Keys.OfType<string>().Contains(ErrorCode))
			{
				ex.Data[ErrorCode] = errorCode;
			}
			else
			{
				ex.Data.Add(ErrorCode, errorCode);
			}
		}

		public string ErrorCodeSuspended;
		public bool ErrorNotifications;
		public SmtpDeliveryMethod SmtpDeliveryMethod;

		private void Initialize()
		{
			// Load configuration
			SmtpUsername = LogHelper.ParseString("SmtpUsername", "");
			SmtpPassword = LogHelper.ParseString("SmtpPassword", "");
			SmtpDomain = LogHelper.ParseString("SmtpDomain", "");
			SmtpServer = LogHelper.ParseString("SmtpServer", "");
			SmtpPort = LogHelper.ParseInt("SmtpPort", 25);
			SmtpLocalAddress = IPAddress.Any;
			var localAddress = LogHelper.ParseString("SmtpLocalAddress", "");
			if (!string.IsNullOrEmpty(localAddress))
				IPAddress.TryParse(localAddress, out SmtpLocalAddress);
			SmtpLocalPort = LogHelper.ParseInt("SmtpLocalPort", 0);
			SmtpPickupFolder = LogHelper.ParseString("SmtpPickupFolder", "");
			SmtpEnableSsl = LogHelper.ParseBool("SmtpEnableSsl", false);
			SmtpFrom = LogHelper.ParseString("SmtpFrom", "");
			SmtpSendCopyTo = LogHelper.ParseString("SmtpSendCopyTo", "");
			SmtpDeliveryMethod = LogHelper.ParseEnum("ErrorDeliveryMethod", SmtpDeliveryMethod.Network);
			// Error reporting.
			ErrorRecipients = LogHelper.ParseString("ErrorRecipients", "");
			var errorNotifications = !string.IsNullOrEmpty(ErrorRecipients);
			ErrorNotifications = LogHelper.ParseBool("ErrorNotifications", errorNotifications);
			ErrorCodeSuspended = LogHelper.ParseString("ErrorCodeSuspended", "");
			SmtpFixHostName = LogHelper.ParseBool("SmtpFixHostName", false);
			if (SmtpFixHostName)
			{
				// FQDN Fix
				IPGlobalProperties ip = IPGlobalProperties.GetIPGlobalProperties();
				if (!string.IsNullOrEmpty(ip.HostName) && !string.IsNullOrEmpty(ip.DomainName))
				{
					LocalHostName = ip.HostName + "." + ip.DomainName;
				}
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

				if (_sendMethod.GetParameters().Length == 2)
				{
					_sendMethod.Invoke(message, flags, null, new object[] { _mailWriter, true }, null);
				}
				else
				{
					_sendMethod.Invoke(message, flags, null, new object[] { _mailWriter, true, true }, null);
				}
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
			if (SmtpDeliveryMethod == SmtpDeliveryMethod.Network)
			{

				// Send message to SMTP server.
				var client = new SmtpClient(SmtpServer);
				if (SmtpEnableSsl)
				{
					client.EnableSsl = true;
					// Enable TLS 1.1 and 1.2
					var Tls11 = 768;
					var Tls12 = 3072;
					ServicePointManager.SecurityProtocol |= (SecurityProtocolType)Tls11 | (SecurityProtocolType)Tls12;
				}
				if (!string.IsNullOrEmpty(SmtpUsername))
				{
					client.Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword, SmtpDomain);
				}
				client.Port = SmtpPort;
				client.ServicePoint.BindIPEndPointDelegate = new System.Net.BindIPEndPoint(BindIPEndPointCallback);
				// Send Email.
				client.Send(message);
			}
			else
			{
				// Save message to folder.
				var di = new DirectoryInfo(SmtpPickupFolder);
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
				var a = item.Trim();
				if (string.IsNullOrEmpty(a)) continue;
				if (result.Any(x => x.Address == a)) continue;
				result.Add(new MailAddress(a));
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
			ApplyRecipients(message, SmtpFrom, ErrorRecipients);
			//------------------------------------------------------
			// Subject
			//------------------------------------------------------
			// If exception found then...
			if (ex != null)
			{
				if (ex.Data != null)
				{
					var key = ex.Data.Keys.Cast<object>().FirstOrDefault(x => object.ReferenceEquals(x, "StackTrace"));
					if (key != null && ex.Data[key] is StackTrace)
					{
						ex.Data.Remove(key);
					}
				}
				// If subject was not specified
				if (string.IsNullOrEmpty(subject))
				{
					subject = LogHelper.GetSubjectPrefix(ex) + ex.Message;
				}
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

		#region Bind to Local IP

		public delegate IPEndPoint BindIPEndPoint(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount);

		private IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
		{
			var endpoint = new IPEndPoint(SmtpLocalAddress, SmtpLocalPort);
			return endpoint;
		}

		#endregion

		public static void ApplyRecipients(MailMessage mail, string addFrom, string addTo, string addCc = null, string addBcc = null)
		{
			ApplyRecipients(mail, new MailAddress(addFrom), addTo, addCc, addBcc);
		}

		public static void ApplyRecipients(MailAddressCollection collection, string emails)
		{
			string[] list = null;
			if (string.IsNullOrEmpty(emails)) return;
			list = emails.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in list)
			{
				// If address is empty then continue.
				if (string.IsNullOrEmpty(item.Trim())) continue;
				var a = new MailAddress(item.Trim());
				// If address is on the list already then continue.
				var exists = collection.Any(x =>
					string.Compare(x.Address, a.Address, true) == 0 &&
					string.Compare(x.DisplayName, a.DisplayName, true) == 0
				);
				if (exists) continue;
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

		public static void ApplyAttachments(MailMessage message, params Attachment[] files)
		{

			if (files == null) return;
			for (int i = 0; i < files.Count(); i++)
			{
				//    string file = files[i].Name;
				//    if (string.IsNullOrEmpty(file)) continue;
				//    if (System.IO.File.Exists(file))
				//    {
				//        // Specify as "application/octet-stream" so attachment will never will be embedded in body of email.
				//        System.Net.Mail.Attachment att = new System.Net.Mail.Attachment(file, "application/octet-stream");
				message.Attachments.Add(files[i]);
				//}
			}
		}

		public static EmailResult EmailValid(string email)
		{
			// evaluate email address for formal validity
			email = email.Trim();
			if (string.IsNullOrEmpty(email)) return EmailResult.Empty;
			string[] emails = email.Split(';');
			// take care of list of addresses separated by semicolon
			foreach (string s in emails)
			{
				var sEmail = s.Trim();
				if (string.IsNullOrEmpty(sEmail))
				{
					// The email address cannot end with a semicolon.
					return EmailResult.Semicolon;
				}
				if (!IsValidEmail(sEmail))
				{
					// Not a valid email address.
					return EmailResult.Invalid;
				}
			}
			// If we got here, email is OK.
			return EmailResult.OK;
		}

		public static bool IsValidEmail(string email, bool mandatory, out string message)
		{
			var result = EmailValid(email);
			message = Attributes.GetDescription(result);
			switch (result)
			{
				case EmailResult.OK: return true;
				case EmailResult.Empty: return !mandatory;
				default: return false;
			}
		}

		#region MSDN
		//https://msdn.microsoft.com/en-us/library/01escwtf%28v=vs.100%29.aspx

		static bool invalid = false;

		public static bool IsValidEmail(string strIn)
		{
			invalid = false;
			if (string.IsNullOrEmpty(strIn))
			{
				return false;
			}
			// Use IdnMapping class to convert Unicode domain names.
			strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper);
			if (invalid)
			{
				return false;
			}
			// Return true if strIn is in valid e-mail format.
			return Regex.IsMatch(strIn,
				   @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
				   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
				   RegexOptions.IgnoreCase);
		}

		static string DomainMapper(Match match)
		{
			// IdnMapping class with default property values.
			IdnMapping idn = new IdnMapping();
			string domainName = match.Groups[2].Value;
			try
			{
				domainName = idn.GetAscii(domainName);
			}
			catch (ArgumentException)
			{
				invalid = true;
			}
			return match.Groups[1].Value + domainName;
		}

		#endregion

	}
}
