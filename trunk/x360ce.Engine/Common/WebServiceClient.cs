namespace x360ce.Engine
{
	using System;
	using System.Web.Services;
	using System.Diagnostics;
	using System.Web.Services.Protocols;
	using System.Xml.Serialization;
	using System.ComponentModel;
	using System.Threading;
	using System.Web.Services.Description;
	using System.Windows.Forms;
	using x360ce.Engine.Data;
	using System.Collections.Generic;

	/// <remarks/>
	[System.Web.Services.WebServiceBindingAttribute(Name = "x360ceSoap", Namespace = "http://x360ce.com/")]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
	public partial class WebServiceClient : System.Web.Services.Protocols.SoapHttpClientProtocol, IWebService
	{

		#region Main Methods

		private bool useDefaultCredentialsSetExplicitly;

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

		private bool IsLocalFileSystemWebService(string url)
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

		void InvokeAsync(string method, EventHandler<ResultEventArgs> completedEvent, object userState, params object[] args)
		{
			var invokeUserState = new InvokeUserState(userState, completedEvent);
			InvokeAsync(method, new object[] { args }, OnAsyncOperationCompleted, invokeUserState);
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
			InvokeAsync("SignIn", SignInCompleted, userState, username, password);
		}

		[SoapDocumentMethodAttribute("http://x360ce.com/SignIn", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public KeyValueList SignIn(string username, string password)
		{
			object[] results = Invoke("SignIn", new object[] { username, password });
			return (KeyValueList)results[0];
		}

		#endregion

		#region Method: SearchSettings

		public event EventHandler<ResultEventArgs> SearchSettingsCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SearchSettings", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			object[] results = this.Invoke("SearchSettings", new object[] { args });
			return (SearchResult)results[0];
		}

		public void SearchSettingsAsync(SearchParameter[] args, object userState = null)
		{
			InvokeAsync("SearchSettings", SearchSettingsCompleted, userState, args);
		}

		#endregion

		#region Method: SaveSetting

		public event EventHandler<ResultEventArgs> SaveSettingCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SaveSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public string SaveSetting(Setting s, PadSetting ps)
		{
			object[] results = this.Invoke("SaveSetting", new object[] { s, ps });
			return (string)results[0];
		}

		public void SaveSettingAsync(Setting s, PadSetting ps, object userState = null)
		{
			InvokeAsync("SaveSetting", SaveSettingCompleted, userState, s, ps);
		}

		#endregion

		#region Method: DeleteSetting

		public event EventHandler<ResultEventArgs> DeleteSettingCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/DeleteSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public string DeleteSetting(Setting s)
		{
			object[] results = this.Invoke("DeleteSetting", new object[] { s });
			return (string)results[0];
		}

		public void DeleteSettingAsync(Setting s, object userState = null)
		{
			InvokeAsync("DeleteSetting", DeleteSettingCompleted, userState, s);
		}

		#endregion

		#region Method: LoadSetting

		public event EventHandler<ResultEventArgs> LoadSettingCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/LoadSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public SearchResult LoadSetting(System.Guid[] checksum)
		{
			object[] results = this.Invoke("LoadSetting", new object[] { checksum });
			return (SearchResult)results[0];
		}

		public void LoadSettingAsync(System.Guid[] checksum, object userState = null)
		{
			InvokeAsync("LoadSetting", LoadSettingCompleted, userState, checksum);
		}

		#endregion

		#region Method: GetPrograms

		public event EventHandler<ResultEventArgs> GetProgramsCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetPrograms", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public List<Program> GetPrograms(bool? isEnabled, int? minInstanceCount)
		{
			object[] results = this.Invoke("GetPrograms", new object[] { isEnabled, minInstanceCount });
			return (List<Program>)results[0];
		}

		public void GetProgramsAsync(bool? isEnabled, int? minInstanceCount, object userState = null)
		{
			InvokeAsync("GetPrograms", GetProgramsCompleted, userState, isEnabled, minInstanceCount);
		}

		#endregion

		#region Method: GetProgram

		public event EventHandler<ResultEventArgs> GetProgramCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public Program GetProgram(string fileName, string fileProductName)
		{
			object[] results = this.Invoke("GetProgram", new object[] { fileName, fileProductName });
			return (Program)results[0];
		}

		public void GetProgramsAsync(string fileName, string fileProductName, object userState = null)
		{
			InvokeAsync("GetProgram", GetProgramCompleted, userState, fileName, fileProductName);
		}

		#endregion

		#region Method: GetVendors

		public event EventHandler<ResultEventArgs> GetVendorsCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetVendors", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public List<Vendor> GetVendors()
		{
			object[] results = this.Invoke("GetVendors", new object[] { });
			return (List<Vendor>)results[0];
		}

		public void GetVendorssAsync(object userState = null)
		{
			InvokeAsync("GetVendors", GetVendorsCompleted, userState);
		}

		#endregion

		#region Method: GetSettingsData

		public event EventHandler<ResultEventArgs> GetSettingsDataCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetSettingsData", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public SettingsData GetSettingsData()
		{
			object[] results = this.Invoke("GetSettingsData", new object[] { });
			return (SettingsData)results[0];
		}

		public void GetSettingsDatasAsync(object userState = null)
		{
			InvokeAsync("GetSettingsData", GetSettingsDataCompleted, userState);
		}

		#endregion

		#region Method: SetProgram

		public event EventHandler<ResultEventArgs> SetProgramCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public string SetProgram(Program p)
		{
			object[] results = this.Invoke("SetProgram", new object[] { p });
			return (string)results[0];
		}

		public void SetProgramAsync(Program p, object userState = null)
		{
			InvokeAsync("SetProgram", SetProgramCompleted, userState, p);
		}

		#endregion

		#region Method: SignOut

		public event EventHandler<ResultEventArgs> SignOutCompleted;

		[System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SignOut", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public KeyValueList SignOut()
		{
			object[] results = this.Invoke("SignOut", new object[] { });
			return (KeyValueList)results[0];
		}

		public void SignOutsAsync(object userState = null)
		{
			InvokeAsync("SignOut", SignOutCompleted, userState);
		}

		#endregion
	}

	public partial class ResultEventArgs : AsyncCompletedEventArgs
	{
		internal ResultEventArgs(object[] results, Exception exception, bool cancelled, object userState) :
			base(exception, cancelled, userState) { _results = results; }

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
