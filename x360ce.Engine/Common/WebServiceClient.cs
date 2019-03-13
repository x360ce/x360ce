using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.ComponentModel;
using System.Web.Services.Description;
using x360ce.Engine.Data;
using System.Collections.Generic;
using System.Net;
using JocysCom.ClassLibrary.Web.Services;

namespace x360ce.Engine
{

	/// <remarks/>
	[WebServiceBinding(Name = "x360ceSoap", Namespace = ns)]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
	public partial class WebServiceClient : SoapHttpClientBase, IWebService
	{

		const string ns = "http://x360ce.com/";

		#region Method: SignIn

		public event EventHandler<ResultEventArgs> SignInCompleted;

		public void SignInAsync(string username, string password, object userState = null)
		{
			InvokeAsync("SignIn", SignInCompleted, userState, new object[] { username, password });
		}

		[SoapDocumentMethod(ns + "SignIn",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudMessage SignIn(string username, string password)
		{
			object[] results = Invoke("SignIn", new object[] { username, password });
			return (CloudMessage)results[0];
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
		public string SaveSetting(UserSetting s, PadSetting ps)
		{
			object[] results = Invoke("SaveSetting", new object[] { s, ps });
			return (string)results[0];
		}

		public void SaveSettingAsync(UserSetting s, PadSetting ps, object userState = null)
		{
			InvokeAsync("SaveSetting", SaveSettingCompleted, userState, new object[] { s, ps });
		}

		#endregion

		#region Method: DeleteSetting

		public event EventHandler<ResultEventArgs> DeleteSettingCompleted;

		[SoapDocumentMethod(ns + "DeleteSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string DeleteSetting(UserSetting s)
		{
			object[] results = Invoke("DeleteSetting", new object[] { s });
			return (string)results[0];
		}

		public void DeleteSettingAsync(UserSetting s, object userState = null)
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

		#region Method: SignOut

		public event EventHandler<ResultEventArgs> SignOutCompleted;

		[SoapDocumentMethod(ns + "SignOut",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudMessage SignOut()
		{
			object[] results = Invoke("SignOut", new object[] { });
			return (CloudMessage)results[0];
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
		public CloudMessage Execute(CloudMessage command)
		{
			object[] results = Invoke("Execute", new object[] { command });
			return (CloudMessage)results[0];
		}

		public void ExecuteAsync(CloudMessage command, object userState = null)
		{
			InvokeAsync("Execute", ExecuteCompleted, userState, new object[] { command });
		}

		#endregion
	}

}
