using System;
using System.ComponentModel;
using System.Net;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;

namespace JocysCom.ClassLibrary.Web.Services
{

	/// <summary>
	/// Adds the ability to retrieve the SOAP request/response, Specify IP address to send from.
	/// </summary>
	/// <remarks>
	/// Declare custom spy service as public class ServiceSpy : OriginalService
	/// {
	/// }
	///
	/// How to add timeout.
	/// wsRef = new MySoapHttpClientBase
	/// {
	///     Url = url,
	///     Timeout = timeout,
	///     ServicePointIP = servicePointIP ?? IPAddress.Any
	/// };
	/// </remarks>
	public class SoapHttpClientBase : SoapHttpClientProtocol
	{
		#region Main Methods

		bool useDefaultCredentialsSetExplicitly;

		/// <remarks/>
		public SoapHttpClientBase()
		{
			// Enable TLS 1.1, 1.2 and 1.3
			var Tls11 = 0x0300; //   768
			var Tls12 = 0x0C00; //  3072
			var Tls13 = 0x3000; // 12288
			ServicePointManager.SecurityProtocol |=
				(SecurityProtocolType)Tls11 | 
				(SecurityProtocolType)Tls12 | 
				(SecurityProtocolType)Tls13;
			if (IsLocalFileSystemWebService(Url))
			{
				UseDefaultCredentials = true;
				useDefaultCredentialsSetExplicitly = false;
			}
			else
			{
				useDefaultCredentialsSetExplicitly = true;
			}
		}

		public new string Url
		{
			get { return base.Url; }
			set
			{
				if (IsLocalFileSystemWebService(base.Url) &&
					!IsLocalFileSystemWebService(value) &&
					!useDefaultCredentialsSetExplicitly)
				{
					base.UseDefaultCredentials = false;
				}
				base.Url = value;
			}
		}

		public new bool UseDefaultCredentials
		{
			get
			{
				return base.UseDefaultCredentials;
			}
			set
			{
				base.UseDefaultCredentials = value;
				useDefaultCredentialsSetExplicitly = true;
			}
		}

		public new void CancelAsync(object userState)
		{
			base.CancelAsync(userState);
		}

		bool IsLocalFileSystemWebService(string url)
		{
			if (string.IsNullOrEmpty(url))
				return false;
			var wsUri = new Uri(url);
			var isLocal = wsUri.Port >= 1024 && string.Compare(wsUri.Host, "localHost", StringComparison.OrdinalIgnoreCase) == 0;
			return isLocal;
		}

		class InvokeUserState
		{
			public InvokeUserState(object userState, EventHandler<ResultEventArgs> handler)
			{
				UserState = userState;
				Handler = handler;
			}
			public object UserState;
			public EventHandler<ResultEventArgs> Handler;
		}

		public void InvokeAsync(string method, EventHandler<ResultEventArgs> completedEvent, object userState, object[] args)
		{
			var invokeUserState = new InvokeUserState(userState, completedEvent);
			InvokeAsync(method, args, OnAsyncOperationCompleted, invokeUserState);
		}

		void OnAsyncOperationCompleted(object arg)
		{
			var invokeArgs = (InvokeCompletedEventArgs)arg;
			var invokeUserState = (InvokeUserState)invokeArgs.UserState;
			if (invokeUserState.Handler == null)
				return;
			var args = new ResultEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeUserState.UserState);
			invokeUserState.Handler(this, args);
		}

		public partial class ResultEventArgs : AsyncCompletedEventArgs
		{
			internal ResultEventArgs(object[] results, Exception exception, bool cancelled, object userState) :
				base(exception, cancelled, userState)
			{ _results = results; }

			object[] _results;
			public object Result
			{
				get
				{
					RaiseExceptionIfNecessary();
					return _results[0];
				}
			}
		}

		#endregion

		#region Copy Reader and Writer Streams

		/// <summary>Stream Spy for Request</summary>
		public StreamSpy WriterStreamSpy;
		/// <summary>Stream Spy for Response</summary>
		public StreamSpy ReaderStreamSpy;

		/// <summary>Get Writer for Request</summary>
		protected override XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize)
		{
			DisposeWriterStreamSpy();
			return StreamSpy.GetWriterForMessage(message, bufferSize, base.RequestEncoding, out WriterStreamSpy);
		}

		/// <summary>Get Reader for Response</summary>
		protected override XmlReader GetReaderForMessage(SoapClientMessage message, int bufferSize)
		{
			DisposeReaderStreamSpy();
			return StreamSpy.GetReaderForMessage(message, bufferSize, out ReaderStreamSpy);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			DisposeWriterStreamSpy();
			DisposeReaderStreamSpy();
		}

		public void DisposeWriterStreamSpy()
		{
			if (WriterStreamSpy != null)
			{
				WriterStreamSpy.Dispose();
				WriterStreamSpy = null;
			}
		}

		public void DisposeReaderStreamSpy()
		{
			if (ReaderStreamSpy != null)
			{
				ReaderStreamSpy.Dispose();
				ReaderStreamSpy = null;
			}
		}

		#endregion

		#region Specifying an originator IP Address on a WebService

		public IPAddress ServicePointIP
		{
			get { return _ServicePointIP; }
			set { _ServicePointIP = value; }
		}
		private IPAddress _ServicePointIP;

		protected override WebRequest GetWebRequest(Uri uri)
		{
			var request = (HttpWebRequest)base.GetWebRequest(uri);
			// Bind to specific local IP.
			request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
			InitPreAuthenticate(request);
			return request;
		}

		private IPEndPoint BindIPEndPointCallback(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
		{
			var point = ServicePointIP != null
				? ServicePointIP
				: remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
					? IPAddress.IPv6Any : IPAddress.Any;
			return new IPEndPoint(point, 0);
		}

		#endregion

		#region Set Credentials

		/// <summary>
		/// Create the network credentials and assign
		/// them to the service credentials
		/// </summary>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public void SetCredentials(string username, string password)
		{
			var nc = new NetworkCredential(username, password);
			var uri = new Uri(Url);
			var credentials = nc.GetCredential(uri, "Basic");
			Credentials = credentials;
			// Be sure to set PreAuthenticate to true or else authentication will not be sent.
			PreAuthenticate = true;
		}

		void InitPreAuthenticate(HttpWebRequest request)
		{
			// If pre-authenticate then...
			if (PreAuthenticate)
			{
				var credentials = Credentials.GetCredential(request.RequestUri, "Basic");
				if (credentials != null)
				{
					var bytes = System.Text.Encoding.UTF8.GetBytes(
					credentials.UserName + ":" +
					credentials.Password);
					request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(bytes);
				}
				else
				{
					throw new ApplicationException("No network credentials");
				}
			}
		}

		#endregion

		//#region Method: Test

		//public event EventHandler<ResultEventArgs> TestCompleted;

		//const string ns = "http://domain.com/AppName";

		///// <remarks/>
		//[SoapDocumentMethod(ns + "/Test",
		//	RequestNamespace = ns,
		//	ResponseNamespace = ns,
		//	Use = System.Web.Services.Description.SoapBindingUse.Literal,
		//	ParameterStyle = SoapParameterStyle.Wrapped)]
		//public string Test(string message)
		//{
		//	object[] results = this.Invoke("Test", new object[] { message });
		//	return (string)results[0];
		//}

		//public void TestAsync(string message, object userState = null)
		//{
		//	InvokeAsync("Test", TestCompleted, userState, new object[] { message });
		//}

		//#endregion

	}
	
}
