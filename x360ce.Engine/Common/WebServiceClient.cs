namespace x360ce.Engine
{
	using System;
	using System.Web.Services;
	using System.Diagnostics;
	using System.Web.Services.Protocols;
	using System.Xml.Serialization;
	using System.ComponentModel;
	using System.Threading;

	/// <remarks/>
	[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	[System.Diagnostics.DebuggerStepThroughAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	[System.Web.Services.WebServiceBindingAttribute(Name = "x360ceSoap", Namespace = "http://x360ce.com/")]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
	public partial class x360ce : System.Web.Services.Protocols.SoapHttpClientProtocol
	{

		#region Main Methods

		private bool useDefaultCredentialsSetExplicitly;

		/// <remarks/>
		public x360ce()
		{
			if ((this.IsLocalFileSystemWebService(this.Url) == true))
			{
				this.UseDefaultCredentials = true;
				this.useDefaultCredentialsSetExplicitly = false;
			}
			else
			{
				this.useDefaultCredentialsSetExplicitly = true;
			}
		}

		public new string Url
		{
			get { return base.Url; }
			set
			{
				if ((((this.IsLocalFileSystemWebService(base.Url) == true)
							&& (this.useDefaultCredentialsSetExplicitly == false))
							&& (this.IsLocalFileSystemWebService(value) == false)))
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
				this.useDefaultCredentialsSetExplicitly = true;
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

		#endregion

		#region SignIn Method

		private SendOrPostCallback SignInOperationCompleted;
		public event SignInCompletedEventHandler SignInCompleted;

		[SoapDocumentMethodAttribute("http://x360ce.com/SignIn", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		public KeyValue[] SignIn(string username, string password)
		{
			object[] results = this.Invoke("SignIn", new object[] { username, password });
			return ((KeyValue[])(results[0]));
		}

		public void SignInAsync(string username, string password, object userState = null)
		{
			if ((SignInOperationCompleted == null)) SignInOperationCompleted = new System.Threading.SendOrPostCallback(OnSignInOperationCompleted);
			InvokeAsync("SignIn", new object[] { username, password }, SignInOperationCompleted, userState);
		}

		private void OnSignInOperationCompleted(object arg)
		{
			if ((SignInCompleted != null))
			{
				var invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
				var args = new ResultEventArgs<KeyValue[]>(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState);
				SignInCompleted(this, args);
			}
		}

		#endregion

		#region Other Methods

		//private System.Threading.SendOrPostCallback SaveSettingOperationCompleted;
		//private System.Threading.SendOrPostCallback SearchSettingsOperationCompleted;
		//private System.Threading.SendOrPostCallback DeleteSettingOperationCompleted;
		//private System.Threading.SendOrPostCallback LoadSettingOperationCompleted;
		//private System.Threading.SendOrPostCallback GetVendorsOperationCompleted;
		//private System.Threading.SendOrPostCallback GetSettingsDataOperationCompleted;
		//private System.Threading.SendOrPostCallback GetProgramsOperationCompleted;
		//private System.Threading.SendOrPostCallback GetProgramOperationCompleted;
		//private System.Threading.SendOrPostCallback SetProgramOperationCompleted;
		//private System.Threading.SendOrPostCallback SignOutOperationCompleted;

		//public event SaveSettingCompletedEventHandler SaveSettingCompleted;
		//public event SearchSettingsCompletedEventHandler SearchSettingsCompleted;
		//public event DeleteSettingCompletedEventHandler DeleteSettingCompleted;
		//public event LoadSettingCompletedEventHandler LoadSettingCompleted;
		//public event GetVendorsCompletedEventHandler GetVendorsCompleted;
		//public event GetSettingsDataCompletedEventHandler GetSettingsDataCompleted;
		//public event GetProgramsCompletedEventHandler GetProgramsCompleted;
		//public event GetProgramCompletedEventHandler GetProgramCompleted;
		//public event SetProgramCompletedEventHandler SetProgramCompleted;
		//public event SignOutCompletedEventHandler SignOutCompleted;


		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SaveSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public string SaveSetting(Setting s, PadSetting ps)
		//    {
		//        object[] results = this.Invoke("SaveSetting", new object[] {
		//                    s,
		//                    ps});
		//        return ((string)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void SaveSettingAsync(Setting s, PadSetting ps)
		//    {
		//        this.SaveSettingAsync(s, ps, null);
		//    }

		//    /// <remarks/>
		//    public void SaveSettingAsync(Setting s, PadSetting ps, object userState)
		//    {
		//        if ((this.SaveSettingOperationCompleted == null))
		//        {
		//            this.SaveSettingOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSaveSettingOperationCompleted);
		//        }
		//        this.InvokeAsync("SaveSetting", new object[] {
		//                    s,
		//                    ps}, this.SaveSettingOperationCompleted, userState);
		//    }

		//    private void OnSaveSettingOperationCompleted(object arg)
		//    {
		//        if ((this.SaveSettingCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.SaveSettingCompleted(this, new SaveSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SearchSettings", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public SearchResult SearchSettings(SearchParameter[] args)
		//    {
		//        object[] results = this.Invoke("SearchSettings", new object[] {
		//                    args});
		//        return ((SearchResult)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void SearchSettingsAsync(SearchParameter[] args)
		//    {
		//        this.SearchSettingsAsync(args, null);
		//    }

		//    /// <remarks/>
		//    public void SearchSettingsAsync(SearchParameter[] args, object userState)
		//    {
		//        if ((this.SearchSettingsOperationCompleted == null))
		//        {
		//            this.SearchSettingsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSearchSettingsOperationCompleted);
		//        }
		//        this.InvokeAsync("SearchSettings", new object[] {
		//                    args}, this.SearchSettingsOperationCompleted, userState);
		//    }

		//    private void OnSearchSettingsOperationCompleted(object arg)
		//    {
		//        if ((this.SearchSettingsCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.SearchSettingsCompleted(this, new SearchSettingsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/DeleteSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public string DeleteSetting(Setting s)
		//    {
		//        object[] results = this.Invoke("DeleteSetting", new object[] {
		//                    s});
		//        return ((string)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void DeleteSettingAsync(Setting s)
		//    {
		//        this.DeleteSettingAsync(s, null);
		//    }

		//    /// <remarks/>
		//    public void DeleteSettingAsync(Setting s, object userState)
		//    {
		//        if ((this.DeleteSettingOperationCompleted == null))
		//        {
		//            this.DeleteSettingOperationCompleted = new System.Threading.SendOrPostCallback(this.OnDeleteSettingOperationCompleted);
		//        }
		//        this.InvokeAsync("DeleteSetting", new object[] {
		//                    s}, this.DeleteSettingOperationCompleted, userState);
		//    }

		//    private void OnDeleteSettingOperationCompleted(object arg)
		//    {
		//        if ((this.DeleteSettingCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.DeleteSettingCompleted(this, new DeleteSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/LoadSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public SearchResult LoadSetting(System.Guid[] checksum)
		//    {
		//        object[] results = this.Invoke("LoadSetting", new object[] {
		//                    checksum});
		//        return ((SearchResult)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void LoadSettingAsync(System.Guid[] checksum)
		//    {
		//        this.LoadSettingAsync(checksum, null);
		//    }

		//    /// <remarks/>
		//    public void LoadSettingAsync(System.Guid[] checksum, object userState)
		//    {
		//        if ((this.LoadSettingOperationCompleted == null))
		//        {
		//            this.LoadSettingOperationCompleted = new System.Threading.SendOrPostCallback(this.OnLoadSettingOperationCompleted);
		//        }
		//        this.InvokeAsync("LoadSetting", new object[] {
		//                    checksum}, this.LoadSettingOperationCompleted, userState);
		//    }

		//    private void OnLoadSettingOperationCompleted(object arg)
		//    {
		//        if ((this.LoadSettingCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.LoadSettingCompleted(this, new LoadSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetVendors", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public Vendor[] GetVendors()
		//    {
		//        object[] results = this.Invoke("GetVendors", new object[0]);
		//        return ((Vendor[])(results[0]));
		//    }

		//    /// <remarks/>
		//    public void GetVendorsAsync()
		//    {
		//        this.GetVendorsAsync(null);
		//    }

		//    /// <remarks/>
		//    public void GetVendorsAsync(object userState)
		//    {
		//        if ((this.GetVendorsOperationCompleted == null))
		//        {
		//            this.GetVendorsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetVendorsOperationCompleted);
		//        }
		//        this.InvokeAsync("GetVendors", new object[0], this.GetVendorsOperationCompleted, userState);
		//    }

		//    private void OnGetVendorsOperationCompleted(object arg)
		//    {
		//        if ((this.GetVendorsCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.GetVendorsCompleted(this, new GetVendorsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetSettingsData", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public SettingsData GetSettingsData()
		//    {
		//        object[] results = this.Invoke("GetSettingsData", new object[0]);
		//        return ((SettingsData)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void GetSettingsDataAsync()
		//    {
		//        this.GetSettingsDataAsync(null);
		//    }

		//    /// <remarks/>
		//    public void GetSettingsDataAsync(object userState)
		//    {
		//        if ((this.GetSettingsDataOperationCompleted == null))
		//        {
		//            this.GetSettingsDataOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetSettingsDataOperationCompleted);
		//        }
		//        this.InvokeAsync("GetSettingsData", new object[0], this.GetSettingsDataOperationCompleted, userState);
		//    }

		//    private void OnGetSettingsDataOperationCompleted(object arg)
		//    {
		//        if ((this.GetSettingsDataCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.GetSettingsDataCompleted(this, new GetSettingsDataCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetPrograms", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public Program[] GetPrograms([System.Xml.Serialization.XmlElementAttribute(IsNullable = true)] System.Nullable<bool> isEnabled, [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)] System.Nullable<int> minInstanceCount)
		//    {
		//        object[] results = this.Invoke("GetPrograms", new object[] {
		//                    isEnabled,
		//                    minInstanceCount});
		//        return ((Program[])(results[0]));
		//    }

		//    /// <remarks/>
		//    public void GetProgramsAsync(System.Nullable<bool> isEnabled, System.Nullable<int> minInstanceCount)
		//    {
		//        this.GetProgramsAsync(isEnabled, minInstanceCount, null);
		//    }

		//    /// <remarks/>
		//    public void GetProgramsAsync(System.Nullable<bool> isEnabled, System.Nullable<int> minInstanceCount, object userState)
		//    {
		//        if ((this.GetProgramsOperationCompleted == null))
		//        {
		//            this.GetProgramsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetProgramsOperationCompleted);
		//        }
		//        this.InvokeAsync("GetPrograms", new object[] {
		//                    isEnabled,
		//                    minInstanceCount}, this.GetProgramsOperationCompleted, userState);
		//    }

		//    private void OnGetProgramsOperationCompleted(object arg)
		//    {
		//        if ((this.GetProgramsCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.GetProgramsCompleted(this, new GetProgramsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public Program GetProgram(string fileName, string fileProductName)
		//    {
		//        object[] results = this.Invoke("GetProgram", new object[] {
		//                    fileName,
		//                    fileProductName});
		//        return ((Program)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void GetProgramAsync(string fileName, string fileProductName)
		//    {
		//        this.GetProgramAsync(fileName, fileProductName, null);
		//    }

		//    /// <remarks/>
		//    public void GetProgramAsync(string fileName, string fileProductName, object userState)
		//    {
		//        if ((this.GetProgramOperationCompleted == null))
		//        {
		//            this.GetProgramOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetProgramOperationCompleted);
		//        }
		//        this.InvokeAsync("GetProgram", new object[] {
		//                    fileName,
		//                    fileProductName}, this.GetProgramOperationCompleted, userState);
		//    }

		//    private void OnGetProgramOperationCompleted(object arg)
		//    {
		//        if ((this.GetProgramCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.GetProgramCompleted(this, new GetProgramCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public string SetProgram(Program p)
		//    {
		//        object[] results = this.Invoke("SetProgram", new object[] {
		//                    p});
		//        return ((string)(results[0]));
		//    }

		//    /// <remarks/>
		//    public void SetProgramAsync(Program p)
		//    {
		//        this.SetProgramAsync(p, null);
		//    }

		//    /// <remarks/>
		//    public void SetProgramAsync(Program p, object userState)
		//    {
		//        if ((this.SetProgramOperationCompleted == null))
		//        {
		//            this.SetProgramOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetProgramOperationCompleted);
		//        }
		//        this.InvokeAsync("SetProgram", new object[] {
		//                    p}, this.SetProgramOperationCompleted, userState);
		//    }

		//    private void OnSetProgramOperationCompleted(object arg)
		//    {
		//        if ((this.SetProgramCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.SetProgramCompleted(this, new SetProgramCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		//    /// <remarks/>
		//    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SignOut", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
		//    public KeyValue[] SignOut()
		//    {
		//        object[] results = this.Invoke("SignOut", new object[0]);
		//        return ((KeyValue[])(results[0]));
		//    }

		//    /// <remarks/>
		//    public void SignOutAsync()
		//    {
		//        this.SignOutAsync(null);
		//    }

		//    /// <remarks/>
		//    public void SignOutAsync(object userState)
		//    {
		//        if ((this.SignOutOperationCompleted == null))
		//        {
		//            this.SignOutOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSignOutOperationCompleted);
		//        }
		//        this.InvokeAsync("SignOut", new object[0], this.SignOutOperationCompleted, userState);
		//    }

		//    private void OnSignOutOperationCompleted(object arg)
		//    {
		//        if ((this.SignOutCompleted != null))
		//        {
		//            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
		//            this.SignOutCompleted(this, new SignOutCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
		//        }
		//    }

		#endregion

	}

	public delegate void SignInCompletedEventHandler(object sender, ResultEventArgs<KeyValue[]> e);

	[System.Diagnostics.DebuggerStepThroughAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute("code")]
	public partial class ResultEventArgs<T> : System.ComponentModel.AsyncCompletedEventArgs
	{

		internal ResultEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
			base(exception, cancelled, userState)
		{
			_results = results;
		}

		object[] _results;
		public T Result
		{
			get
			{
				this.RaiseExceptionIfNecessary();
				return ((T)(_results[0]));
			}
		}
	}

	#region Other Classes

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void SaveSettingCompletedEventHandler(object sender, SaveSettingCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class SaveSettingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal SaveSettingCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public string Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((string)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void SearchSettingsCompletedEventHandler(object sender, SearchSettingsCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class SearchSettingsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal SearchSettingsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public SearchResult Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((SearchResult)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void DeleteSettingCompletedEventHandler(object sender, DeleteSettingCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class DeleteSettingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal DeleteSettingCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public string Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((string)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void LoadSettingCompletedEventHandler(object sender, LoadSettingCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class LoadSettingCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal LoadSettingCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public SearchResult Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((SearchResult)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void GetVendorsCompletedEventHandler(object sender, GetVendorsCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class GetVendorsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal GetVendorsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public Vendor[] Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((Vendor[])(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void GetSettingsDataCompletedEventHandler(object sender, GetSettingsDataCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class GetSettingsDataCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal GetSettingsDataCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public SettingsData Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((SettingsData)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void GetProgramsCompletedEventHandler(object sender, GetProgramsCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class GetProgramsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal GetProgramsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public Program[] Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((Program[])(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void GetProgramCompletedEventHandler(object sender, GetProgramCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class GetProgramCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal GetProgramCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public Program Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((Program)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void SetProgramCompletedEventHandler(object sender, SetProgramCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class SetProgramCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal SetProgramCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public string Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((string)(this.results[0]));
	//        }
	//    }
	//}

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//public delegate void SignOutCompletedEventHandler(object sender, SignOutCompletedEventArgs e);

	///// <remarks/>
	//[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.0.30319.17929")]
	//[System.Diagnostics.DebuggerStepThroughAttribute()]
	//[System.ComponentModel.DesignerCategoryAttribute("code")]
	//public partial class SignOutCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
	//{

	//    private object[] results;

	//    internal SignOutCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) :
	//        base(exception, cancelled, userState)
	//    {
	//        this.results = results;
	//    }

	//    /// <remarks/>
	//    public KeyValue[] Result
	//    {
	//        get
	//        {
	//            this.RaiseExceptionIfNecessary();
	//            return ((KeyValue[])(this.results[0]));
	//        }
	//    }
	//}

	#endregion


}
