using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;

namespace JocysCom.ClassLibrary.Web.Services
{
	/// <summary>
	/// Wrapper class to clone read/write bytes.
	/// </summary>
	public class SoapHttpClientLogger : Stream
	{
		#region Stream Logger

		public SoapHttpClientLogger(Stream wrappedStream, HttpWebRequest request, bool isResponse = false)
		{
			_wrappedStream = wrappedStream;
			_Request = request;
			_IsResponse = isResponse;
		}

		HttpWebRequest _Request;
		bool _IsResponse;

		Stream _wrappedStream;
		MemoryStream _clonedStream = new MemoryStream();

		object clonedStreamLock = new object();
		public MemoryStream ClonedStream { get { return _clonedStream; } }

		// Pass commands to wrapped stream.
		public override bool CanRead { get { return _wrappedStream.CanRead; } }
		public override bool CanSeek { get { return _wrappedStream.CanSeek; } }
		public override bool CanWrite { get { return _wrappedStream.CanWrite; } }
		public override void Flush() { _wrappedStream.Flush(); }
		public override long Length { get { return _wrappedStream.Length; } }
		public override long Position { get { return _wrappedStream.Position; } set { _wrappedStream.Position = value; } }
		public override long Seek(long offset, SeekOrigin origin) { return _wrappedStream.Seek(offset, origin); }
		public override void SetLength(long value) { _wrappedStream.SetLength(value); }
		public override int Read(byte[] buffer, int offset, int count)
		{
			// Read data into buffer.
			int result = _wrappedStream.Read(buffer, offset, count);
			// Copy data into clone buffer so it can be retrieved later.
			lock (clonedStreamLock)
			{
				_clonedStream.Write(buffer, offset, result);
			}
			return result;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_wrappedStream.Write(buffer, offset, count);
			lock (clonedStreamLock)
			{
				_clonedStream.Write(buffer, offset, count);
			}
		}

		public override void Close()
		{
			_wrappedStream.Close();
			base.Close();
		}

		public XmlDocument GetClonedDataAsXml()
		{
			var doc = new XmlDocument();
			doc.XmlResolver = null;
			lock (clonedStreamLock)
			{
				var position = _clonedStream.Position;
				_clonedStream.Position = 0;
				var reader = XmlReader.Create(_clonedStream);
				doc.Load(reader);
				_clonedStream.Position = position;
			}
			return doc;
		}

		public string GetClonedDataAsText(Encoding encoding = null)
		{
			var enc = encoding is null ? System.Text.Encoding.UTF8 : encoding;
			string doc = null;
			lock (clonedStreamLock)
			{
				var position = _clonedStream.Position;
				_clonedStream.Position = 0;
				doc = enc.GetString(_clonedStream.ToArray());
				_clonedStream.Position = position;
			}
			return doc;
		}

		protected override void Dispose(bool disposing)
		{
			if (_wrappedStream != null)
			{
				_wrappedStream.Dispose();
				_wrappedStream = null;
				_clonedStream.Dispose();
				_clonedStream = null;
				_Request = null;
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Methods for SoapHttpClientProtocol

		/// <summary>Get Writer for Request</summary>
		public static XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize, Encoding requestEncoding, HttpWebRequest request, out SoapHttpClientLogger writerStreamLogger)
		{
			writerStreamLogger = new SoapHttpClientLogger(message.Stream, request);
			if (bufferSize < 0x200)
				bufferSize = 0x200;
			return new XmlTextWriter(new StreamWriter(writerStreamLogger, (requestEncoding != null) ? requestEncoding : new UTF8Encoding(false), bufferSize));
		}

		/// <summary>Get Reader for Response</summary>
		public static XmlReader GetReaderForMessage(SoapClientMessage message, int bufferSize, HttpWebRequest request, out SoapHttpClientLogger readerStreamLogger)
		{
			readerStreamLogger = new SoapHttpClientLogger(message.Stream, request, true);
			Encoding encoding = null;
			// Set encoding.
			var charset = GetParameter(message.ContentType, "charset");
			if (!string.IsNullOrEmpty(charset))
				encoding = Encoding.GetEncoding(charset);
			if (encoding is null && !(message.SoapVersion == SoapProtocolVersion.Soap12 && IsApplication(message.ContentType)))
				encoding = Encoding.ASCII;
			if (bufferSize < 0x200)
				bufferSize = 0x200;
			var reader = encoding is null
				? new XmlTextReader(readerStreamLogger)
				: new XmlTextReader(new StreamReader(readerStreamLogger, encoding, true, bufferSize));
			reader.DtdProcessing = DtdProcessing.Prohibit;
			reader.Normalization = true;
			reader.XmlResolver = null;
			return reader;
		}

		private static string GetParameter(string contentType, string paramName)
		{
			var arr = contentType.Split(';');
			for (var i = 1; i < arr.Length; i++)
			{
				var s = arr[i].TrimStart(null);
				if (string.Compare(s, 0, paramName, 0, paramName.Length, StringComparison.OrdinalIgnoreCase) == 0)
				{
					var index = s.IndexOf('=', paramName.Length);
					if (index >= 0)
						return s.Substring(index + 1).Trim(new char[] { ' ', '\'', '"', '\t' });
				}
			}
			return null;
		}

		internal static bool IsApplication(string contentType)
		{
			// Get Media Type.
			var index = contentType.IndexOf(';');
			var s = index >= 0
				? contentType.Substring(0, index)
				: contentType;
			index = s.IndexOf('/');
			var mediaType = index >= 0
				? s.Substring(0, index)
				: s;
			return string.Compare(mediaType, "application", StringComparison.OrdinalIgnoreCase) == 0;
		}

		#endregion

		#region Tracing

		// You can enable tracing and write raw data by adding configuration lines below.
		/*
			<configuration>
			  <system.serviceModel>
			  <!-- Enabling Tracing in Web Services -->
			  <diagnostics>
				<messageLogging
				  logMessagesAtTransportLevel="true"
				  logMessagesAtServiceLevel="true"
				  logMalformedMessages="true"
				  logEntireMessage="true"
				  maxSizeOfMessageToLog="65535000"
				  maxMessagesToLog="500"
				/>
			  </diagnostics>
			  </system.serviceModel>
			  <!-- Enabling Tracing in SoapHttpClientLogger -->
			  <system.diagnostics>
				<sources>
				  <source name="JocysCom.ClassLibrary.Web.Services.SoapHttpClientLogger" switchValue="All">
					<listeners>
					  <add name="WebServiceLogs"/>
					</listeners>
				  </source>
				</sources>
				<sharedListeners>
				  <!-- Files can be opened with SvcTraceViewer.exe. make sure that IIS_IUSRS group has write permissions on this folder. -->
				  <add name="WebServiceLogs" type="System.Diagnostics.XmlWriterTraceListener" initializeData="D:\LogFiles\WebServiceLogs.svclog" />
				</sharedListeners>
				   <trace autoflush="true" />
				 </system.diagnostics>
			</configuration>
		*/

		#endregion

		#region Helper Methods

		internal static string DumpHeaders(WebHeaderCollection headers)
		{
			var list = new List<string>();
			for (int i = 0; i < headers.Count; ++i)
			{
				if (headers[i] is null)
					continue;
				var header = headers.GetKey(i);
				foreach (string value in headers.GetValues(i))
					list.Add(string.Format("{0}: {1}", header, value));
			}
			return string.Join("\r\n", list);
		}

		NameValueCollection ToCollection(HttpWebRequest message)
		{
			if (message is null)
				return null;
			var nvc = new NameValueCollection();
			nvc.Add("Type", message.GetType().FullName);
			nvc.Add("RequestHash", string.Format("{0:X4}", message.GetHashCode()));
			nvc.Add("Method", string.Format("{0}", message.Method));
			nvc.Add("URL", string.Format("{0}", message.RequestUri));
			nvc.Add("Version", string.Format("{0}", message.ProtocolVersion));
			nvc.Add("Headers", DumpHeaders(message.Headers));
			//var content = message.Content;
			//if (content != null)
			//{
			//	nvc.Add("ContentHead", DumpHeaders(content.Headers));
			//	nvc.Add("ContentBody", content.ReadAsStringAsync().Result);
			//}
			return nvc;
		}

		NameValueCollection ToCollection(WebResponse message, int? requestHshCode = null)
		{
			if (message is null)
				return null;
			var nvc = new NameValueCollection();
			nvc.Add("Type", message.GetType().FullName);
			if (requestHshCode.HasValue)
				nvc.Add("RequestHash", string.Format("{0:X4}", requestHshCode.Value));
			//nvc.Add("StatusCode", string.Format("{0} - {1}", (int)message.StatusCode, message.StatusCode));
			//nvc.Add("StatusName", string.Format("{0}", message.ReasonPhrase));
			//nvc.Add("Version", string.Format("{0}", message.Version));
			//nvc.Add("Headers", DumpHeaders(message.Headers));
			//var content = message.Content;
			//if (content != null)
			//{
			//	nvc.Add("ContentHead", DumpHeaders(content.Headers));
			//	nvc.Add("ContentBody", content.ReadAsStringAsync().Result);
			//}
			return nvc;
		}

		#endregion

	}
}

