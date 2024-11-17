#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
#else // .NET Framework

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Linq;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public class RawDataLogHttpModule : IHttpModule
	{
		/*

		If you are running Web Application in:
			Classic mode then add node inside <configuration><system.web><httpModules> section.
			Integrated mode then add node inside "<configuration><system.webServer><modules> section.

		<!-- Enabling Tracing: JocysCom.ClassLibrary.Web.Services.RawDataLogHttpModule -->
		<add name="JocysComHttpModule" type="JocysCom.ClassLibrary.Diagnostics.RawDataLogHttpModule,JocysCom.ClassLibrary" />

		Add source inside <configuration><system.diagnostics><sources> node:

		<!-- Enabling Tracing: JocysCom.ClassLibrary.Diagnostics.RawDataLogHttpModule -->
		<source name="RawDataLogHttpModule" switchValue="Verbose">
		  <listeners>
		    <add name="SvcFileTraceListener" />
		  </listeners>
		</source>

		Add listener inside <configuration><system.diagnostics><sharedListeners> node:

		<!-- Files can be opened with SvcTraceViewer.exe.make sure that IIS_IUSRS group has write permissions on this folder. -->
		<add name="SvcFileTraceListener" type="System.Diagnostics.XmlWriterTraceListener" initializeData="c:\inetpub\logs\LogFiles\WebServiceLogs.svclog" />

		*/

		public RawDataLogHttpModule()
		{
		}

		public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			// Capture request.
			context.BeginRequest += context_BeginRequest;
			// Capture response.
			context.PreRequestHandlerExecute += context_PreRequestHandlerExecute;
			context.PreSendRequestContent += context_PreSendRequestContent;
		}

		private void context_BeginRequest(object sender, EventArgs e)
		{
			var application = sender as HttpApplication;
			if (application is null)
				return;
			var request = application.Request;
			if (request is null)
				return;
			// Process.
			var col = new NameValueCollection();
			col.Add("HashCode", string.Format("{0:X4}", request.GetHashCode()));
			col.Add("Event", "BeginRequest");
			col.Add("UserHostAddress", request.UserHostAddress);
			col.Add("Url", string.Format("{0}", request.Url));
			col.Add("HttpMethod", request.HttpMethod);
			// Add headers.
			var headerLines = request.Headers.AllKeys.ToArray()
				.Select(x => string.Format("{0}: {1}", x, request.Headers[x]));
			col.Add("Headers", string.Join("\r\n", headerLines));
			// Get raw body.
			request.InputStream.Position = 0;
			var sr = new StreamReader(request.InputStream, request.ContentEncoding);
			var body = sr.ReadToEnd();
			request.InputStream.Position = 0;
			col.Add("Body", body);
			TraceHelper.AddLog(GetType().Name, TraceEventType.Information, col);
			// Add custom information to IIS log.
			//	response.AppendToLog(body);
		}

		void context_PreRequestHandlerExecute(object sender, EventArgs e)
		{
			var application = sender as HttpApplication;
			var context = application.Context;
			var response = context.Response;
			// Add a filter to capture response stream
			response.Filter = new ResponseCaptureStream(response.Filter, response.ContentEncoding);
		}

		void context_PreSendRequestContent(object sender, EventArgs e)
		{
			var application = sender as HttpApplication;
			var context = application.Context;
			var request = context.Request;
			var response = context.Response;
			var filter = response.Filter as ResponseCaptureStream;
			if (filter is null)
				return;
			// Process.
			var col = new NameValueCollection();
			col.Add("HashCode", string.Format("{0:X4}", request.GetHashCode()));
			col.Add("Event", "PreSendRequestContent");
			col.Add("StatusCode", response.StatusCode.ToString());
			var sb = new StringBuilder();
			foreach (var key in response.Headers.AllKeys)
				sb.AppendFormat("{0}: {1}\r\n", key, response.Headers[key]);
			col.Add("Headers", sb.ToString());
			// Get raw body.
			var body = filter.StreamContent;
			if (body != null)
			{
				col.Add("Body.Length", body.Length.ToString());
				// Add body if text only.
				if (response.ContentType.StartsWith("text"))
					col.Add("Body", body);
			}

			TraceHelper.AddLog(GetType().Name, TraceEventType.Information, col);
		}

		private class ResponseCaptureStream : Stream
		{
			private readonly Stream _streamToCapture;
			private readonly Encoding _responseEncoding;

			private string _streamContent;
			public string StreamContent
			{
				get { return _streamContent; }
				private set
				{
					_streamContent = value;
				}
			}

			public ResponseCaptureStream(Stream streamToCapture, Encoding responseEncoding)
			{
				_responseEncoding = responseEncoding;
				_streamToCapture = streamToCapture;

			}

			public override bool CanRead
			{
				get { return _streamToCapture.CanRead; }
			}

			public override bool CanSeek
			{
				get { return _streamToCapture.CanSeek; }
			}

			public override bool CanWrite
			{
				get { return _streamToCapture.CanWrite; }
			}

			public override void Flush()
			{
				_streamToCapture.Flush();
			}

			public override long Length
			{
				get { return _streamToCapture.Length; }
			}

			public override long Position
			{
				get
				{
					return _streamToCapture.Position;
				}
				set
				{
					_streamToCapture.Position = value;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _streamToCapture.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _streamToCapture.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_streamToCapture.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_streamContent += _responseEncoding.GetString(buffer);
				// SUPPRESS: CWE-201: Information Exposure Through Sent Data
				// Note: Data being logged and not exposed to end user.
				_streamToCapture.Write(buffer, offset, count);
			}

			public override void Close()
			{
				_streamToCapture.Close();
				base.Close();
			}
		}
	}
}
#endif
