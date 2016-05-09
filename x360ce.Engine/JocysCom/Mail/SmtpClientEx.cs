using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.IO;

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

		private static readonly FieldInfo _localHostName = GetLocalHostNameField();

		public SmtpClientEx(string host, int port)
			: base(host, port)
		{
			Initialize();
		}

		public SmtpClientEx(string host)
			: base(host)
		{
			Initialize();
		}

		public SmtpClientEx()
		{
			Initialize();
		}

		public string LocalHostName
		{
			get
			{
				if (null == _localHostName) return null;
				return (string)_localHostName.GetValue(this);
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException("value");
				}
				if (null != _localHostName)
				{
					_localHostName.SetValue(this, value);
				}
			}
		}

		private static FieldInfo GetLocalHostNameField()
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
			return typeof(SmtpClient).GetField("localHostName", flags);
		}

		private void Initialize()
		{
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
				// Get reflection info for MailWriter contructor
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

	}
}
