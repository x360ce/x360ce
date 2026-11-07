#nullable disable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Web.Services
{
	/// <summary>A fault a SOAP web service answered with.</summary>
	public class SoapException : Exception
	{
		internal const string Envelope = "http://schemas.xmlsoap.org/soap/envelope/";

		/// <summary>The fault of a service that could not do what it was asked: asking again later may work.</summary>
		public static readonly XmlQualifiedName ServerFaultCode = new XmlQualifiedName("Server", Envelope);

		/// <summary>The fault of a request the service will not take, however often it is sent.</summary>
		public static readonly XmlQualifiedName ClientFaultCode = new XmlQualifiedName("Client", Envelope);

		public SoapException(string message, XmlQualifiedName code) : base(message)
		{
			Code = code;
		}

		public XmlQualifiedName Code { get; }
	}

	/// <summary>Calls the methods of a SOAP web service, with System.Net and XmlSerializer.</summary>
	/// <remarks>
	/// <para>
	/// A derived class declares a public method for each method of the service, under the same name. Its parameters
	/// are what is sent and its return type what is read: <c>return Invoke&lt;Answer&gt;("Ask", question);</c>
	/// </para>
	/// <para>
	/// HttpWebRequest.Timeout is the time to wait for the answer to start, 100 seconds unless set here.
	/// HttpWebRequest.ReadWriteTimeout is the time to wait for its data, 5 minutes.
	/// </para>
	/// </remarks>
	public class SoapHttpClientBase : IDisposable
	{
		/// <param name="serviceNamespace">The XML namespace of the service, which also starts the name of each of its methods.</param>
		protected SoapHttpClientBase(string serviceNamespace)
		{
			ServiceNamespace = serviceNamespace;
			// Enable TLS 1.1, 1.2 and 1.3
			var Tls11 = 0x0300; //   768
			var Tls12 = 0x0C00; //  3072
			ServicePointManager.SecurityProtocol |= (SecurityProtocolType)(Tls11 | Tls12);
			// This will allow to validate certificates and allow SSL connection if certificate have problem.
			ServicePointManager.ServerCertificateValidationCallback = Security.SecurityHelper.ValidateServerCertificate;
		}

		readonly string ServiceNamespace;

		/// <summary>The address of the service.</summary>
		public string Url { get; set; }

		/// <summary>How long to wait for the answer to start, in milliseconds.</summary>
		public int Timeout { get; set; } = 100000;

		/// <summary>What the client calls itself. The service has always been called with this name.</summary>
		static readonly string UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol " + Environment.Version + ")";

		/// <summary>Nothing is held between calls. Kept so a client can be used in a using statement.</summary>
		public void Dispose()
		{
		}

		#region Calls

		/// <summary>Calls a method of the service and waits for its answer.</summary>
		/// <exception cref="SoapException">The service answered with a fault.</exception>
		/// <exception cref="WebException">There was no answer, or the answer was an HTTP error other than 400 and 500.</exception>
		/// <exception cref="InvalidOperationException">The answer was not the SOAP message asked for, a web page for one.</exception>
		public T Invoke<T>(string method, params object[] args)
		{
			return (T)Send(method, args)[0];
		}

		/// <summary>Calls a method of the service on a worker, and raises the event on the thread that called, when it has answered.</summary>
		public void InvokeAsync(string method, EventHandler<SoapHttpClientEventArgs> completedEvent, object userState, params object[] args)
		{
			var operation = AsyncOperationManager.CreateOperation(userState);
			Task.Run(() =>
			{
				object[] results = null;
				Exception error = null;
				try { results = Send(method, args); }
				catch (Exception ex) { error = ex; }
				operation.PostOperationCompleted(_ => completedEvent?.Invoke(this, new SoapHttpClientEventArgs(results, error, false, userState)), null);
			});
		}

		object[] Send(string method, object[] args)
		{
			var serializers = SerializersOf(method);
			var request = (HttpWebRequest)WebRequest.Create(Url);
			request.Method = "POST";
			request.ContentType = "text/xml; charset=utf-8";
			request.Headers["SOAPAction"] = "\"" + ServiceNamespace + method + "\"";
			request.UserAgent = UserAgent;
			request.Timeout = Timeout;
			request.AllowAutoRedirect = false;
			using (var stream = request.GetRequestStream())
				WriteMessage(stream, serializers.Request, args);
			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex) when (ex.Response is HttpWebResponse)
			{
				response = (HttpWebResponse)ex.Response;
			}
			var body = new MemoryStream();
			// The stream and not the response is closed: a failure hands the response on, and the caller reads its status.
			using (var stream = response.GetResponseStream())
				stream.CopyTo(body);
			var status = (int)response.StatusCode;
			var type = response.ContentType ?? "";
			var isXml = type.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase);
			if (status >= 300 && status != 500 && !(status == 400 && isXml))
				throw new WebException(body.Length == 0
					? "The request failed with an empty response."
					: "The request failed with HTTP status " + status + ": " + response.StatusDescription + ".",
					null, WebExceptionStatus.ProtocolError, response);
			if (body.Length == 0)
				throw new InvalidOperationException("Client found response content type of '" + type + "', but expected 'text/xml'.\r\nThe request failed with an empty response.");
			if (!isXml)
				throw new InvalidOperationException("Client found response content type of '" + type + "', but expected 'text/xml'.\r\nThe request failed with the error message:\r\n--\r\n"
					+ Encoding.UTF8.GetString(body.ToArray()) + "\r\n--.");
			body.Position = 0;
			return ReadMessage(body, serializers.Response);
		}

		#endregion

		#region Messages

		/// <summary>Writes the message that calls a method, as the service reads it.</summary>
		static void WriteMessage(Stream stream, XmlSerializer serializer, object[] args)
		{
			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("soap", "Envelope", SoapException.Envelope);
				writer.WriteAttributeString("xmlns", "soap", null, SoapException.Envelope);
				writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
				writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
				writer.WriteStartElement("soap", "Body", SoapException.Envelope);
				serializer.Serialize(writer, args);
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
		}

		/// <summary>Reads what the service answered: what its method returned, or the fault it sent in its place.</summary>
		static object[] ReadMessage(Stream stream, XmlSerializer serializer)
		{
			const string NotXml = "Response is not well-formed XML.";
			try
			{
				using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
				{
					reader.MoveToContent();
					if (!reader.IsStartElement("Envelope", SoapException.Envelope))
						throw new InvalidOperationException(NotXml);
					reader.ReadStartElement();
					reader.MoveToContent();
					if (reader.IsStartElement("Header", SoapException.Envelope))
					{
						reader.Skip();
						reader.MoveToContent();
					}
					if (!reader.IsStartElement("Body", SoapException.Envelope))
						throw new InvalidOperationException(NotXml);
					reader.ReadStartElement();
					reader.MoveToContent();
					if (reader.IsStartElement("Fault", SoapException.Envelope))
						throw ReadFault(reader);
					return (object[])serializer.Deserialize(reader);
				}
			}
			catch (XmlException ex)
			{
				throw new InvalidOperationException(NotXml, ex);
			}
		}

		static SoapException ReadFault(XmlReader reader)
		{
			var code = new XmlQualifiedName("");
			var text = "";
			reader.ReadStartElement();
			reader.MoveToContent();
			while (reader.NodeType == XmlNodeType.Element)
			{
				if (reader.LocalName == "faultcode")
				{
					var parts = reader.ReadElementContentAsString().Trim().Split(':');
					code = parts.Length == 2
						? new XmlQualifiedName(parts[1], reader.LookupNamespace(parts[0]))
						: new XmlQualifiedName(parts[0]);
				}
				else if (reader.LocalName == "faultstring")
					text = reader.ReadElementContentAsString();
				else
					reader.Skip();
				reader.MoveToContent();
			}
			return new SoapException(text, code);
		}

		sealed class MethodSerializers
		{
			public XmlSerializer Request;
			public XmlSerializer Response;
		}

		/// <summary>What reads and writes each method's messages, made once a method: making one compiles code.</summary>
		static readonly Dictionary<string, MethodSerializers> Serializers = new Dictionary<string, MethodSerializers>();

		MethodSerializers SerializersOf(string method)
		{
			var key = GetType().FullName + "." + method;
			lock (Serializers)
			{
				if (Serializers.TryGetValue(key, out var known))
					return known;
				var info = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(x => x.Name == method);
				// Made apart, because a type a method both takes and returns may be in one set of mappings only once.
				var request = SerializerOf(method, info.GetParameters().Select(x => new XmlReflectionMember { MemberName = x.Name, MemberType = x.ParameterType }).ToArray());
				var response = SerializerOf(method + "Response", new[] { new XmlReflectionMember { MemberName = method + "Result", MemberType = info.ReturnType } });
				return Serializers[key] = new MethodSerializers { Request = request, Response = response };
			}
		}

		XmlSerializer SerializerOf(string element, XmlReflectionMember[] members)
		{
			var mapping = new XmlReflectionImporter(ServiceNamespace).ImportMembersMapping(element, ServiceNamespace, members, true);
			return XmlSerializer.FromMappings(new XmlMapping[] { mapping })[0];
		}

		#endregion
	}
}
