using System;
using System.Net;
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

		// This class can generate "WebServiceBindingAttribute is required on proxy classes" error during build.
		// Solution: Set "Generate Serialization assembly:" to "Auto" or "Off" in the [Build] tab of project properties.

		// HttpWebRequest.Timeout - Time to wait until server accepts the client's request (TCP ACK). Default value: 100 seconds.
		//   This doesn't include the DNS resolution time, which is managed by the ServicePointManager.
		// HttpWebRequest.ReadWriteTimeout - Time to wait until server sends all the data. Default value: 5 minutes.
		//   This timeout starts only after the server accepts the request.
		// HttpWebRequest.ContinueTimeout - Time to wait until the 100-Continue is received from the server.
		//   ContinueTimeout used when HttpWebRequest.ServicePoint.Expect100Continue = true and only header is sent with the initial request.
		// Note: These timeouts have no effect on asynchronous requests.

		#region Main Methods

		bool useDefaultCredentialsSetExplicitly;

		/// <remarks/>
		public SoapHttpClientBase()
		{
			// Enable TLS 1.1, 1.2 and 1.3
			var Tls11 = 0x0300; //   768
			var Tls12 = 0x0C00; //  3072
			ServicePointManager.SecurityProtocol |= (SecurityProtocolType)(Tls11 | Tls12);
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
			public InvokeUserState(object userState, EventHandler<SoapHttpClientEventArgs> handler)
			{
				UserState = userState;
				Handler = handler;
			}
			public object UserState;
			public EventHandler<SoapHttpClientEventArgs> Handler;
		}

		public T Invoke<T>(string method, params object[] args)
		{
			var results = Invoke(method, args);
			return (T)results[0];
		}

		public void InvokeAsync(string method, EventHandler<SoapHttpClientEventArgs> completedEvent, object userState, params object[] args)
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
			var args = new SoapHttpClientEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeUserState.UserState);
			invokeUserState.Handler(this, args);
		}

		#endregion

		#region Copy Reader and Writer Streams

		/// <summary>Stream Spy for Request</summary>
		public SoapHttpClientSpy WriterStreamSpy;
		/// <summary>Stream Spy for Response</summary>
		public SoapHttpClientSpy ReaderStreamSpy;

		/// <summary>Web Request</summary>
		public HttpWebRequest CurrentWebRequest;


		/// <summary>Get Writer for Request</summary>
		protected override XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize)
		{
			DisposeWriterStreamSpy();
			// Dispose reader too in case it was created by previous call.
			DisposeReaderStreamSpy();
			var writer = SoapHttpClientSpy.GetWriterForMessage(message, bufferSize, base.RequestEncoding, CurrentWebRequest, out WriterStreamSpy);
			return writer;
		}

		/// <summary>Get Reader for Response</summary>
		protected override XmlReader GetReaderForMessage(SoapClientMessage message, int bufferSize)
		{
			DisposeReaderStreamSpy();
			var reader = SoapHttpClientSpy.GetReaderForMessage(message, bufferSize, CurrentWebRequest, out ReaderStreamSpy);
			return reader;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			// Remove all events.
			WebRequestCreated = null;
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
			CurrentWebRequest = (HttpWebRequest)base.GetWebRequest(uri);
			// Bind to specific local IP.
			CurrentWebRequest.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(BindIPEndPointCallback);
			InitPreAuthenticate(CurrentWebRequest);
			var ev = WebRequestCreated;
			if (ev != null)
				ev(this, new EventArgs());
			return CurrentWebRequest;
		}

		public EventHandler<EventArgs> WebRequestCreated;

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
					request.Headers["Authorization"] = "Basic " + System.Convert.ToBase64String(bytes);
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
