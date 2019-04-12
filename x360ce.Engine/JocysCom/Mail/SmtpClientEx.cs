using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Web;

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

		private static object currentLock = new object();
		public static SmtpClientEx Current
		{
			get
			{
				lock (currentLock)
				{
					if (_Current == null)
						_Current = new SmtpClientEx();
					return _Current;
				}
			}
		}
		private static SmtpClientEx _Current;

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
			var flags = BindingFlags.Instance | BindingFlags.NonPublic;
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
				if (localHostName == null)
					return null;
				return (string)localHostName.GetValue(this);
			}
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException("value");
				if (localHostName != null)
					localHostName.SetValue(this, value);
			}
		}

		#endregion

		public SmtpClientEx() { Initialize(); }

		public SmtpClientEx(string host) : base(host) { Initialize(); }

		public SmtpClientEx(string host, int port) : base(host, port) { Initialize(); }

		public IPAddress SmtpLocalAddress;
		public int SmtpLocalPort;
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
				ex.Data[ErrorCode] = errorCode;
			else
				ex.Data.Add(ErrorCode, errorCode);
		}

		public string ErrorCodeSuspended;
		public bool ErrorNotifications;
		//public SmtpDeliveryMethod SmtpDeliveryMethod;

		private void Initialize()
		{
			// Configuration will be loaded automatically from <configuration>\<system.net>\<mailSettings>
			// <configuration>
			//   <system.net>
			//     <mailSettings>
			//       <smtp from="info@domain.com">
			//         <network host="server.domain.com" port="25" userName="automail@domain.com" password="********" enableSsl="true" />
			//       </smtp>
			//     </mailSettings>
			//   </system.net>
			// </configuration>
			//
			// Port for: TLS/STARTTLS: 587, SSL: 465
			//
			// Override settings from <configuration>\<appSettings>
			//
			System.Configuration.Configuration config;
			var settings = GetCurrentSmtpSettings(out config);
			SmtpFrom = Runtime.LogHelper.ParseString("SmtpFrom", settings.From);
			var credentials = (NetworkCredential)Credentials ?? new NetworkCredential();
			credentials.Domain = Runtime.LogHelper.ParseString("SmtpDomain", credentials.Domain);
			credentials.UserName = Runtime.LogHelper.ParseString("SmtpUsername", credentials.UserName);
			credentials.Password = Runtime.LogHelper.ParseString("SmtpPassword", credentials.Password);
			if (!string.IsNullOrEmpty(credentials.UserName))
				Credentials = credentials;
			Host = Runtime.LogHelper.ParseString("SmtpServer", Host);
			Port = Runtime.LogHelper.ParseInt("SmtpPort", Port);
			EnableSsl = Runtime.LogHelper.ParseBool("SmtpEnableSsl", EnableSsl);
			DeliveryMethod = Runtime.LogHelper.ParseEnum("ErrorDeliveryMethod", DeliveryMethod);
			PickupDirectoryLocation = Runtime.LogHelper.ParseString("SmtpPickupFolder", PickupDirectoryLocation);
			Timeout = Runtime.LogHelper.ParseInt("SmtpTimeout", Timeout);
			// Set local IP address and port.
			SmtpLocalAddress = IPAddress.Any;
			var localAddress = Runtime.LogHelper.ParseString("SmtpLocalAddress", "");
			if (!string.IsNullOrEmpty(localAddress))
				IPAddress.TryParse(localAddress, out SmtpLocalAddress);
			SmtpLocalPort = Runtime.LogHelper.ParseInt("SmtpLocalPort", 0);
			// Other settings.
			SmtpSendCopyTo = Runtime.LogHelper.ParseString("SmtpSendCopyTo", "");
			ErrorRecipients = Runtime.LogHelper.ParseString("ErrorRecipients", "");
			var errorNotifications = !string.IsNullOrEmpty(ErrorRecipients);
			ErrorNotifications = Runtime.LogHelper.ParseBool("ErrorNotifications", errorNotifications);
			ErrorCodeSuspended = Runtime.LogHelper.ParseString("ErrorCodeSuspended", "");
			SmtpFixHostName = Runtime.LogHelper.ParseBool("SmtpFixHostName", false);
			if (SmtpFixHostName)
			{
				// Fully qualified domain name (FQDN) fix.
				var ip = IPGlobalProperties.GetIPGlobalProperties();
				if (!string.IsNullOrEmpty(ip.HostName) && !string.IsNullOrEmpty(ip.DomainName))
					LocalHostName = ip.HostName + "." + ip.DomainName;
			}
		}

		/// <summary>Get configuration settings from current web.config or app.config.</summary>
		static System.Configuration.Configuration GetCurrentConfiguration()
		{
			// If executable then...
			if (HttpRuntime.IISVersion == null)
				return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
			// If web request then...
			if (HttpContext.Current != null && HttpContext.Current.Request == null)
				return System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
			// If web application then...
			return System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(HttpRuntime.AppDomainAppVirtualPath);
		}

		public static SmtpSection GetCurrentSmtpSettings(out System.Configuration.Configuration config)
		{
			config = GetCurrentConfiguration();
			// Get Mail settings.
			var settings = NetSectionGroup.GetSectionGroup(config).MailSettings;
			return settings.Smtp;
		}

		static long _WriteMailCount = 0;

		public void SendMessage(MailMessage message, out string fileName)
		{
			fileName = null;
			if (!string.IsNullOrEmpty(SmtpSendCopyTo))
				AddSmtpSendCopyToRecipients(message, SmtpSendCopyTo);
			// Override sending.
			if (DeliveryMethod == SmtpDeliveryMethod.Network)
			{
				// Send message to SMTP server.
				if (EnableSsl)
				{
					// Enable TLS 1.1, 1.2 and 1.3
					var Tls11 = 0x0300; //   768
					var Tls12 = 0x0C00; //  3072
					ServicePointManager.SecurityProtocol |= (SecurityProtocolType)(Tls11 | Tls12);
				}
				// Bind to local IP address.
				ServicePoint.BindIPEndPointDelegate =
					delegate (ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
					{
						var endpoint = new IPEndPoint(SmtpLocalAddress, SmtpLocalPort);
						return endpoint;
					};
			}
			// Override file name.
			else if (DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory)
			{
				// Save message to folder.
				var di = new DirectoryInfo(PickupDirectoryLocation);
				if (!di.Exists)
					di.Create();
				System.Threading.Interlocked.Increment(ref _WriteMailCount);
				// Add date prefix in front. Default SmtpClient would use random GUID.
				fileName = string.Format("{0:yyyyMMdd_HHmmss_fffffff}_{1}.eml", DateTime.Now, _WriteMailCount);
				var fi = new FileInfo(Path.Combine(di.FullName, fileName));
				SaveMessage(message, fi.FullName);
				return;
			}
			// Send Email.
			// CWE-201: Information Exposure Through Send Data
			// CWE-209: Information Exposure Through an Error Message
			// Note: Generic shared method. Mitigated by design.
			Send(message);
		}

		/// <summary>
		/// Save message as *.eml file.
		/// </summary>
		public static void SaveMessage(MailMessage message, string fileName)
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
				var parameters = _sendMethod.GetParameters().Length == 2
					? new object[] { _mailWriter, true }
					: new object[] { _mailWriter, true, true };
				_sendMethod.Invoke(message, flags, null, parameters, null);
				// Finally get reflection info for Close() method on our MailWriter
				var _closeMethod = _mailWriter.GetType().GetMethod("Close", flags);
				// Call close method
				_closeMethod.Invoke(_mailWriter, flags, null, new object[] { }, null);
			}
		}


		/// <summary>
		/// Make sure that email copy was sent to listed recipients.
		/// </summary>
		/// <param name="message"></param>
		public static void AddSmtpSendCopyToRecipients(MailMessage message, string recipients)
		{
			var addresses = MailHelper.ParseEmailAddress(recipients);
			var hosts = addresses.Select(x => x.Host.ToLower()).Distinct().ToArray();
			if (hosts.Length == 0)
				return;
			var anyTo = message.To.Any(x => hosts.Contains(x.Host.ToLower()));
			var anyCc = message.CC.Any(x => hosts.Contains(x.Host.ToLower()));
			var anyBcc = message.Bcc.Any(x => hosts.Contains(x.Host.ToLower()));
			var haveDomain = anyTo || anyCc || anyBcc;
			// If email with required domain is present or recipients are not specified then return.
			if (haveDomain)
				return;
			foreach (MailAddress item in addresses)
				message.CC.Add(item);
		}

	}
}
