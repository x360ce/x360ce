using JocysCom.ClassLibrary.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.Remoting.Messaging;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Mail
{
	#region Serialize Mail Message

	[Serializable, XmlRoot(nameof(MailAddress))]
	public class MailAddressSerializable
	{
		public MailAddressSerializable() { }

		public MailAddressSerializable(string address, string displayName = null)
		{
			DisplayName = displayName;
			Address = address;
		}

		[XmlAttribute] public string DisplayName { get; set; }
		[XmlAttribute] public string Address { get; set; }
	}


	[Serializable, XmlRoot(nameof(MailMessage))]
	public class MailMessageSerializable
	{
		public MailMessageSerializable() { }
		public MailMessageSerializable(MailMessage message) { Load(message); }
		public List<KeyValue> Headers { get; set; }
		public bool IsBodyHtml { get; set; }
		public MailPriority Priority { get; set; }
		public TransferEncoding BodyTransferEncoding { get; set; }
		public DeliveryNotificationOptions DeliveryNotificationOptions { get; set; }
		public MailAddressSerializable Sender { get; set; }
		public MailAddressSerializable From { get; set; }

		[XmlElement(nameof(MailMessage.To))]
		public List<MailAddressSerializable> To { get; set; } = new List<MailAddressSerializable>();

		[XmlElement(nameof(MailMessage.CC))]
		public List<MailAddressSerializable> CC { get; set; } = new List<MailAddressSerializable>();

		[XmlElement(nameof(MailMessage.Bcc))]
		public List<MailAddressSerializable> Bcc { get; set; } = new List<MailAddressSerializable>();

		[XmlElement(nameof(MailMessage.ReplyToList))]
		public List<MailAddressSerializable> ReplyToList { get; set; } = new List<MailAddressSerializable>();

		public string Subject { get; set; }
		public string Body { get; set; }
		//public AttachmentCollection Attachments { get; }
		//public AlternateViewCollection AlternateViews { get; }

		private void Load(MailMessage m)
		{
			Headers = new List<KeyValue>();
			foreach (var key in m.Headers.AllKeys)
				Headers.Add(new KeyValue(key, m.Headers[key]));
			IsBodyHtml = m.IsBodyHtml;
			Priority = m.Priority;
			BodyTransferEncoding = m.BodyTransferEncoding;
			DeliveryNotificationOptions = m.DeliveryNotificationOptions;
			if (m.Sender != null)
				Sender = new MailAddressSerializable(m.Sender.Address, m.Sender.DisplayName);
			if (m.From != null)
				From = new MailAddressSerializable(m.From.Address, m.From.DisplayName);
			foreach (MailAddress a in m.To)
				To.Add(new MailAddressSerializable(a.Address, a.DisplayName));
			foreach (MailAddress a in m.CC)
				CC.Add(new MailAddressSerializable(a.Address, a.DisplayName));
			foreach (MailAddress a in m.Bcc)
				Bcc.Add(new MailAddressSerializable(a.Address, a.DisplayName));
			foreach (MailAddress a in m.ReplyToList)
				ReplyToList.Add(new MailAddressSerializable(a.Address, a.DisplayName));
			Subject = m.Subject;
			Body = m.Body;
			//output.AlternateViews = input.AlternateViews;
			//output.Attachments = input.Attachments;
		}

		public MailMessage ToMailMessage()
		{
			var o = new MailMessage();
			foreach (var item in Headers)
				o.Headers.Add(item.Key, item.Value);
			o.IsBodyHtml = IsBodyHtml;
			o.Priority = Priority;
			o.BodyTransferEncoding = BodyTransferEncoding;
			o.DeliveryNotificationOptions = DeliveryNotificationOptions;
			if (Sender != null)
				o.Sender = new MailAddress(Sender.Address, Sender.DisplayName);
			if (From != null)
				o.From = new MailAddress(From.Address, From.DisplayName);
			foreach (MailAddressSerializable a in To)
				o.To.Add(new MailAddress(a.Address, a.DisplayName));
			foreach (MailAddressSerializable a in CC)
				o.CC.Add(new MailAddress(a.Address, a.DisplayName));
			foreach (MailAddressSerializable a in Bcc)
				o.Bcc.Add(new MailAddress(a.Address, a.DisplayName));
			foreach (MailAddressSerializable a in ReplyToList)
				o.ReplyToList.Add(new MailAddress(a.Address, a.DisplayName));
			o.Subject = Subject;
			o.Body = Body;
			//o.AlternateViews = AlternateViews;
			//o.Attachments = Attachments;
			return o;
		}

	}

	#endregion

}
