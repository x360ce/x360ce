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

    /// <remarks/>
    [System.Web.Services.WebServiceBindingAttribute(Name = "x360ceSoap", Namespace = "http://x360ce.com/")]
    //[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
    //[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
    public partial class WebServiceClient : System.Web.Services.Protocols.SoapHttpClientProtocol
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
            InvokeAsync(method, new object[]{ args }, OnAsyncOperationCompleted, invokeUserState);
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
        public KeyValue[] SignIn(string username, string password)
        {
            object[] results = Invoke("SignIn", new object[] { username, password });
            return ((KeyValue[])(results[0]));
        }

        #endregion

        #region Method: SearchSettings

        public event EventHandler<ResultEventArgs> SearchSettingsCompleted;

        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SearchSettings", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public SearchResult SearchSettings(SearchParameter[] args)
        {
            object[] results = this.Invoke("SearchSettings", new object[] { args });
            return ((SearchResult)(results[0]));
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
            return ((string)(results[0]));
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
            return ((string)(results[0]));
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
            return ((SearchResult)(results[0]));
        }

        public void LoadSettingAsync(System.Guid[] checksum, object userState = null)
        {
            InvokeAsync("LoadSetting", LoadSettingCompleted, userState, checksum);
        }

        #endregion

        #region Method: GetPrograms

        public event EventHandler<ResultEventArgs> GetProgramsCompleted;

        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetPrograms", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public SearchResult GetPrograms(bool? isEnabled, int? minInstanceCount)
        {
            object[] results = this.Invoke("GetPrograms", new object[] { isEnabled, minInstanceCount });
            return ((SearchResult)(results[0]));
        }

        public void GetProgramsAsync(bool? isEnabled, int? minInstanceCount, object userState = null)
        {
            InvokeAsync("GetPrograms", GetProgramsCompleted, userState, isEnabled, minInstanceCount);
        }

        #endregion

        #region Other Methods

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SaveSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public string SaveSetting(Setting s, PadSetting ps)
        //    {
        //        object[] results = Invoke("SaveSetting", new object[] {
        //                    s,
        //                    ps});
        //        return ((string)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void SaveSettingAsync(Setting s, PadSetting ps)
        //    {
        //        SaveSettingAsync(s, ps, null);
        //    }

        //    /// <remarks/>
        //    public void SaveSettingAsync(Setting s, PadSetting ps, object userState)
        //    {
        //        if ((SaveSettingOperationCompleted == null))
        //        {
        //            SaveSettingOperationCompleted = new System.Threading.SendOrPostCallback(OnSaveSettingOperationCompleted);
        //        }
        //        InvokeAsync("SaveSetting", new object[] {
        //                    s,
        //                    ps}, SaveSettingOperationCompleted, userState);
        //    }

        //    private void OnSaveSettingOperationCompleted(object arg)
        //    {
        //        if ((SaveSettingCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            SaveSettingCompleted(this, new SaveSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SearchSettings", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public SearchResult SearchSettings(SearchParameter[] args)
        //    {
        //        object[] results = Invoke("SearchSettings", new object[] {
        //                    args});
        //        return ((SearchResult)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void SearchSettingsAsync(SearchParameter[] args)
        //    {
        //        SearchSettingsAsync(args, null);
        //    }

        //    /// <remarks/>
        //    public void SearchSettingsAsync(SearchParameter[] args, object userState)
        //    {
        //        if ((SearchSettingsOperationCompleted == null))
        //        {
        //            SearchSettingsOperationCompleted = new System.Threading.SendOrPostCallback(OnSearchSettingsOperationCompleted);
        //        }
        //        InvokeAsync("SearchSettings", new object[] {
        //                    args}, SearchSettingsOperationCompleted, userState);
        //    }

        //    private void OnSearchSettingsOperationCompleted(object arg)
        //    {
        //        if ((SearchSettingsCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            SearchSettingsCompleted(this, new SearchSettingsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/DeleteSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public string DeleteSetting(Setting s)
        //    {
        //        object[] results = Invoke("DeleteSetting", new object[] {
        //                    s});
        //        return ((string)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void DeleteSettingAsync(Setting s)
        //    {
        //        DeleteSettingAsync(s, null);
        //    }

        //    /// <remarks/>
        //    public void DeleteSettingAsync(Setting s, object userState)
        //    {
        //        if ((DeleteSettingOperationCompleted == null))
        //        {
        //            DeleteSettingOperationCompleted = new System.Threading.SendOrPostCallback(OnDeleteSettingOperationCompleted);
        //        }
        //        InvokeAsync("DeleteSetting", new object[] {
        //                    s}, DeleteSettingOperationCompleted, userState);
        //    }

        //    private void OnDeleteSettingOperationCompleted(object arg)
        //    {
        //        if ((DeleteSettingCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            DeleteSettingCompleted(this, new DeleteSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/LoadSetting", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public SearchResult LoadSetting(System.Guid[] checksum)
        //    {
        //        object[] results = Invoke("LoadSetting", new object[] {
        //                    checksum});
        //        return ((SearchResult)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void LoadSettingAsync(System.Guid[] checksum)
        //    {
        //        LoadSettingAsync(checksum, null);
        //    }

        //    /// <remarks/>
        //    public void LoadSettingAsync(System.Guid[] checksum, object userState)
        //    {
        //        if ((LoadSettingOperationCompleted == null))
        //        {
        //            LoadSettingOperationCompleted = new System.Threading.SendOrPostCallback(OnLoadSettingOperationCompleted);
        //        }
        //        InvokeAsync("LoadSetting", new object[] {
        //                    checksum}, LoadSettingOperationCompleted, userState);
        //    }

        //    private void OnLoadSettingOperationCompleted(object arg)
        //    {
        //        if ((LoadSettingCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            LoadSettingCompleted(this, new LoadSettingCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetVendors", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public Vendor[] GetVendors()
        //    {
        //        object[] results = Invoke("GetVendors", new object[0]);
        //        return ((Vendor[])(results[0]));
        //    }

        //    /// <remarks/>
        //    public void GetVendorsAsync()
        //    {
        //        GetVendorsAsync(null);
        //    }

        //    /// <remarks/>
        //    public void GetVendorsAsync(object userState)
        //    {
        //        if ((GetVendorsOperationCompleted == null))
        //        {
        //            GetVendorsOperationCompleted = new System.Threading.SendOrPostCallback(OnGetVendorsOperationCompleted);
        //        }
        //        InvokeAsync("GetVendors", new object[0], GetVendorsOperationCompleted, userState);
        //    }

        //    private void OnGetVendorsOperationCompleted(object arg)
        //    {
        //        if ((GetVendorsCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            GetVendorsCompleted(this, new GetVendorsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetSettingsData", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public SettingsData GetSettingsData()
        //    {
        //        object[] results = Invoke("GetSettingsData", new object[0]);
        //        return ((SettingsData)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void GetSettingsDataAsync()
        //    {
        //        GetSettingsDataAsync(null);
        //    }

        //    /// <remarks/>
        //    public void GetSettingsDataAsync(object userState)
        //    {
        //        if ((GetSettingsDataOperationCompleted == null))
        //        {
        //            GetSettingsDataOperationCompleted = new System.Threading.SendOrPostCallback(OnGetSettingsDataOperationCompleted);
        //        }
        //        InvokeAsync("GetSettingsData", new object[0], GetSettingsDataOperationCompleted, userState);
        //    }

        //    private void OnGetSettingsDataOperationCompleted(object arg)
        //    {
        //        if ((GetSettingsDataCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            GetSettingsDataCompleted(this, new GetSettingsDataCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetPrograms", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public Program[] GetPrograms([System.Xml.Serialization.XmlElementAttribute(IsNullable = true)] System.Nullable<bool> isEnabled, [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)] System.Nullable<int> minInstanceCount)
        //    {
        //        object[] results = Invoke("GetPrograms", new object[] {
        //                    isEnabled,
        //                    minInstanceCount});
        //        return ((Program[])(results[0]));
        //    }

        //    /// <remarks/>
        //    public void GetProgramsAsync(System.Nullable<bool> isEnabled, System.Nullable<int> minInstanceCount)
        //    {
        //        GetProgramsAsync(isEnabled, minInstanceCount, null);
        //    }

        //    /// <remarks/>
        //    public void GetProgramsAsync(System.Nullable<bool> isEnabled, System.Nullable<int> minInstanceCount, object userState)
        //    {
        //        if ((GetProgramsOperationCompleted == null))
        //        {
        //            GetProgramsOperationCompleted = new System.Threading.SendOrPostCallback(OnGetProgramsOperationCompleted);
        //        }
        //        InvokeAsync("GetPrograms", new object[] {
        //                    isEnabled,
        //                    minInstanceCount}, GetProgramsOperationCompleted, userState);
        //    }

        //    private void OnGetProgramsOperationCompleted(object arg)
        //    {
        //        if ((GetProgramsCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            GetProgramsCompleted(this, new GetProgramsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/GetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public Program GetProgram(string fileName, string fileProductName)
        //    {
        //        object[] results = Invoke("GetProgram", new object[] {
        //                    fileName,
        //                    fileProductName});
        //        return ((Program)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void GetProgramAsync(string fileName, string fileProductName)
        //    {
        //        GetProgramAsync(fileName, fileProductName, null);
        //    }

        //    /// <remarks/>
        //    public void GetProgramAsync(string fileName, string fileProductName, object userState)
        //    {
        //        if ((GetProgramOperationCompleted == null))
        //        {
        //            GetProgramOperationCompleted = new System.Threading.SendOrPostCallback(OnGetProgramOperationCompleted);
        //        }
        //        InvokeAsync("GetProgram", new object[] {
        //                    fileName,
        //                    fileProductName}, GetProgramOperationCompleted, userState);
        //    }

        //    private void OnGetProgramOperationCompleted(object arg)
        //    {
        //        if ((GetProgramCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            GetProgramCompleted(this, new GetProgramCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SetProgram", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public string SetProgram(Program p)
        //    {
        //        object[] results = Invoke("SetProgram", new object[] {
        //                    p});
        //        return ((string)(results[0]));
        //    }

        //    /// <remarks/>
        //    public void SetProgramAsync(Program p)
        //    {
        //        SetProgramAsync(p, null);
        //    }

        //    /// <remarks/>
        //    public void SetProgramAsync(Program p, object userState)
        //    {
        //        if ((SetProgramOperationCompleted == null))
        //        {
        //            SetProgramOperationCompleted = new System.Threading.SendOrPostCallback(OnSetProgramOperationCompleted);
        //        }
        //        InvokeAsync("SetProgram", new object[] {
        //                    p}, SetProgramOperationCompleted, userState);
        //    }

        //    private void OnSetProgramOperationCompleted(object arg)
        //    {
        //        if ((SetProgramCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            SetProgramCompleted(this, new SetProgramCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

        //    /// <remarks/>
        //    [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://x360ce.com/SignOut", RequestNamespace = "http://x360ce.com/", ResponseNamespace = "http://x360ce.com/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        //    public KeyValue[] SignOut()
        //    {
        //        object[] results = Invoke("SignOut", new object[0]);
        //        return ((KeyValue[])(results[0]));
        //    }

        //    /// <remarks/>
        //    public void SignOutAsync()
        //    {
        //        SignOutAsync(null);
        //    }

        //    /// <remarks/>
        //    public void SignOutAsync(object userState)
        //    {
        //        if ((SignOutOperationCompleted == null))
        //        {
        //            SignOutOperationCompleted = new System.Threading.SendOrPostCallback(OnSignOutOperationCompleted);
        //        }
        //        InvokeAsync("SignOut", new object[0], SignOutOperationCompleted, userState);
        //    }

        //    private void OnSignOutOperationCompleted(object arg)
        //    {
        //        if ((SignOutCompleted != null))
        //        {
        //            System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
        //            SignOutCompleted(this, new SignOutCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
        //        }
        //    }

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
