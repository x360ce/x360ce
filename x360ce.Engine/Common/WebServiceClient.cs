using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Web.Services.Description;
using x360ce.Engine.Data;
using System.Collections.Generic;
using System.Net;

namespace x360ce.Engine
{

	/// <remarks/>
	[WebServiceBinding(Name = "x360ceSoap", Namespace = ns)]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
	public partial class WebServiceClient : SoapHttpClientProtocol, IWebService
	{

		#region Main Methods

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

		protected override WebRequest GetWebRequest(Uri uri)
		{
			var request = (HttpWebRequest)base.GetWebRequest(uri);
			if (PreAuthenticate)
			{
				var credentials = Credentials.GetCredential(uri, "Basic");
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
			return request;
		}

		const string ns = "http://x360ce.com/";
		bool useDefaultCredentialsSetExplicitly;

		/// <remarks/>
		public WebServiceClient()
		{
			if ((IsLocalFileSystemWebService(Url) == true))
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
				if ((((IsLocalFileSystemWebService(base.Url) == true)
							&& (useDefaultCredentialsSetExplicitly == false))
							&& (IsLocalFileSystemWebService(value) == false)))
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
			if (((url == null) || (url == string.Empty))) return false;
			System.Uri wsUri = new System.Uri(url);
			if (((wsUri.Port >= 1024)
						&& (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0)))
			{
				return true;
			}
			return false;
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

		void InvokeAsync(string method, EventHandler<ResultEventArgs> completedEvent, object userState, object[] args)
		{
			var invokeUserState = new InvokeUserState(userState, completedEvent);
			InvokeAsync(method, args, OnAsyncOperationCompleted, invokeUserState);
		}

		void OnAsyncOperationCompleted(object arg)
		{
			var invokeArgs = (InvokeCompletedEventArgs)arg;
			var invokeUserState = (InvokeUserState)invokeArgs.UserState;
			if (invokeUserState.Handler == null) return;
			var args = new ResultEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeUserState.UserState);
			invokeUserState.Handler(this, args);
		}

		#endregion

		#region Method: SignIn

		public event EventHandler<ResultEventArgs> SignInCompleted;

		public void SignInAsync(string username, string password, object userState = null)
		{
			InvokeAsync("SignIn", SignInCompleted, userState, new object[] { username, password });
		}

		[SoapDocumentMethod(ns + "SignIn",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public KeyValueList SignIn(string username, string password)
		{
			object[] results = Invoke("SignIn", new object[] { username, password });
			return (KeyValueList)results[0];
		}

		#endregion

		#region Method: SearchSettings

		public event EventHandler<ResultEventArgs> SearchSettingsCompleted;

		[SoapDocumentMethod(ns + "SearchSettings",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			object[] results = Invoke("SearchSettings", new object[] { args });
			return (SearchResult)results[0];
		}

		public void SearchSettingsAsync(SearchParameter[] args, object userState = null)
		{
			InvokeAsync("SearchSettings", SearchSettingsCompleted, userState, new object[] { args });
		}

		#endregion

		#region Method: SaveSetting

		public event EventHandler<ResultEventArgs> SaveSettingCompleted;

		[SoapDocumentMethod(ns + "SaveSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string SaveSetting(Setting s, PadSetting ps)
		{
			object[] results = Invoke("SaveSetting", new object[] { s, ps });
			return (string)results[0];
		}

		public void SaveSettingAsync(Setting s, PadSetting ps, object userState = null)
		{
			InvokeAsync("SaveSetting", SaveSettingCompleted, userState, new object[] { s, ps });
		}

		#endregion

		#region Method: DeleteSetting

		public event EventHandler<ResultEventArgs> DeleteSettingCompleted;

		[SoapDocumentMethod(ns + "DeleteSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string DeleteSetting(Setting s)
		{
			object[] results = Invoke("DeleteSetting", new object[] { s });
			return (string)results[0];
		}

		public void DeleteSettingAsync(Setting s, object userState = null)
		{
			InvokeAsync("DeleteSetting", DeleteSettingCompleted, userState, new object[] { s });
		}

		#endregion

		#region Method: LoadSetting

		public event EventHandler<ResultEventArgs> LoadSettingCompleted;

		[SoapDocumentMethod(ns + "LoadSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SearchResult LoadSetting(System.Guid[] checksum)
		{
			object[] results = Invoke("LoadSetting", new object[] { checksum });
			return (SearchResult)results[0];
		}

		public void LoadSettingAsync(System.Guid[] checksum, object userState = null)
		{
			InvokeAsync("LoadSetting", LoadSettingCompleted, userState, new object[] { checksum });
		}

		#endregion

		#region Method: GetPrograms

		public event EventHandler<ResultEventArgs> GetProgramsCompleted;

		[SoapDocumentMethod(ns + "GetPrograms",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount)
		{
			object[] results = Invoke("GetPrograms", new object[] { isEnabled, minInstanceCount });
			return (List<Program>)results[0];
		}

		public void GetProgramsAsync(EnabledState isEnabled, int minInstanceCount, object userState = null)
		{
			InvokeAsync("GetPrograms", GetProgramsCompleted, userState, new object[] { isEnabled, minInstanceCount });
		}

		#endregion

		#region Method: GetProgram

		public event EventHandler<ResultEventArgs> GetProgramCompleted;

		[SoapDocumentMethod(ns + "GetProgram",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public Program GetProgram(string fileName, string fileProductName)
		{
			object[] results = Invoke("GetProgram", new object[] { fileName, fileProductName });
			return (Program)results[0];
		}

		public void GetProgramsAsync(string fileName, string fileProductName, object userState = null)
		{
			InvokeAsync("GetProgram", GetProgramCompleted, userState, new object[] { fileName, fileProductName });
		}

		#endregion

		#region Method: GetVendors

		public event EventHandler<ResultEventArgs> GetVendorsCompleted;

		[SoapDocumentMethod(ns + "GetVendors",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public List<Vendor> GetVendors()
		{
			object[] results = Invoke("GetVendors", new object[] { });
			return (List<Vendor>)results[0];
		}

		public void GetVendorssAsync(object userState = null)
		{
			InvokeAsync("GetVendors", GetVendorsCompleted, userState, new object[] { });
		}

		#endregion

		#region Method: GetSettingsData

		public event EventHandler<ResultEventArgs> GetSettingsDataCompleted;

		[SoapDocumentMethod(ns + "GetSettingsData",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SettingsData GetSettingsData()
		{
			object[] results = Invoke("GetSettingsData", new object[] { });
			return (SettingsData)results[0];
		}

		public void GetSettingsDatasAsync(object userState = null)
		{
			InvokeAsync("GetSettingsData", GetSettingsDataCompleted, userState, new object[] { });
		}

		#endregion

		#region Method: SetProgram

		public event EventHandler<ResultEventArgs> SetProgramCompleted;

		[SoapDocumentMethod(ns + "SetProgram",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string SetProgram(Program p)
		{
			object[] results = Invoke("SetProgram", new object[] { p });
			return (string)results[0];
		}

		public void SetProgramAsync(Program p, object userState = null)
		{
			InvokeAsync("SetProgram", SetProgramCompleted, userState, new object[] { p });
		}

		#endregion

		#region Method: SignOut

		public event EventHandler<ResultEventArgs> SignOutCompleted;

		[SoapDocumentMethod(ns + "SignOut",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public KeyValueList SignOut()
		{
			object[] results = Invoke("SignOut", new object[] { });
			return (KeyValueList)results[0];
		}

		public void SignOutsAsync(object userState = null)
		{
			InvokeAsync("SignOut", SignOutCompleted, userState, new object[] { });
		}

		#endregion

		#region Method: Execute

		public event EventHandler<ResultEventArgs> ExecuteCompleted;

		[SoapDocumentMethod(ns + "Execute",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudResults Execute(CloudCommand command)
		{
			object[] results = Invoke("Execute", new object[] { command });
			return (CloudResults)results[0];
		}

		public void ExecuteAsync(CloudCommand command, object userState = null)
		{
			InvokeAsync("Execute", ExecuteCompleted, userState, new object[] { command });
		}

		#endregion
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


}
