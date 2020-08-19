using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Services.Description;
using x360ce.Engine.Data;
using System.Collections.Generic;
using JocysCom.ClassLibrary.Web.Services;

namespace x360ce.Engine
{

	[WebServiceBinding(Name = "x360ceSoap", Namespace = ns)]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(StructuralObject))]
	//[System.Xml.Serialization.XmlIncludeAttribute(typeof(EntityKeyMember[]))]
	public partial class WebServiceClient : SoapHttpClientBase, IWebService
	{

		const string ns = "http://x360ce.com/";

		#region Method: SignIn

		public event EventHandler<SoapHttpClientEventArgs> SignInCompleted;

		public void SignInAsync(string username, string password, object userState = null)
		{
			InvokeAsync("SignIn", SignInCompleted, userState, new object[] { username, password });
		}

		[SoapDocumentMethod(ns + "SignIn",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudMessage SignIn(string username, string password)
		{
			return Invoke<CloudMessage>("SignIn", username, password);
		}

		#endregion

		#region Method: SearchSettings

		public event EventHandler<SoapHttpClientEventArgs> SearchSettingsCompleted;

		[SoapDocumentMethod(ns + "SearchSettings",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SearchResult SearchSettings(SearchParameter[] args)
		{
			return Invoke<SearchResult>("SearchSettings", args);
		}

		public void SearchSettingsAsync(SearchParameter[] args, object userState = null)
		{
			InvokeAsync("SearchSettings", SearchSettingsCompleted, userState, new object[] { args });
		}

		#endregion

		#region Method: SaveSetting

		public event EventHandler<SoapHttpClientEventArgs> SaveSettingCompleted;

		[SoapDocumentMethod(ns + "SaveSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string SaveSetting(UserSetting s, PadSetting ps)
		{
			return Invoke<string>("SaveSetting", s, ps);
		}

		public void SaveSettingAsync(UserSetting s, PadSetting ps, object userState = null)
		{
			InvokeAsync("SaveSetting", SaveSettingCompleted, userState, new object[] { s, ps });
		}

		#endregion

		#region Method: DeleteSetting

		public event EventHandler<SoapHttpClientEventArgs> DeleteSettingCompleted;

		[SoapDocumentMethod(ns + "DeleteSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public string DeleteSetting(UserSetting s)
		{
			return Invoke<string>("DeleteSetting", s);
		}

		public void DeleteSettingAsync(UserSetting s, object userState = null)
		{
			InvokeAsync("DeleteSetting", DeleteSettingCompleted, userState, new object[] { s });
		}

		#endregion

		#region Method: LoadSetting

		public event EventHandler<SoapHttpClientEventArgs> LoadSettingCompleted;

		[SoapDocumentMethod(ns + "LoadSetting",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SearchResult LoadSetting(System.Guid[] checksum)
		{
			return Invoke<SearchResult>("LoadSetting", checksum);
		}

		public void LoadSettingAsync(System.Guid[] checksum, object userState = null)
		{
			InvokeAsync("LoadSetting", LoadSettingCompleted, userState, new object[] { checksum });
		}

		#endregion

		#region Method: GetPrograms

		public event EventHandler<SoapHttpClientEventArgs> GetProgramsCompleted;

		[SoapDocumentMethod(ns + "GetPrograms",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public List<Program> GetPrograms(EnabledState isEnabled, int minInstanceCount)
		{
			return Invoke<List<Program>>("GetPrograms", isEnabled, minInstanceCount);
		}

		public void GetProgramsAsync(EnabledState isEnabled, int minInstanceCount, object userState = null)
		{
			InvokeAsync("GetPrograms", GetProgramsCompleted, userState, new object[] { isEnabled, minInstanceCount });
		}

		#endregion

		#region Method: GetVendors

		public event EventHandler<SoapHttpClientEventArgs> GetVendorsCompleted;

		[SoapDocumentMethod(ns + "GetVendors",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public List<Vendor> GetVendors()
		{
			return Invoke<List<Vendor>>("GetVendors");
		}

		public void GetVendorssAsync(object userState = null)
		{
			InvokeAsync("GetVendors", GetVendorsCompleted, userState);
		}

		#endregion

		#region Method: GetSettingsData

		public event EventHandler<SoapHttpClientEventArgs> GetSettingsDataCompleted;

		[SoapDocumentMethod(ns + "GetSettingsData",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public SettingsData GetSettingsData()
		{
			return Invoke<SettingsData>("GetSettingsData");
		}

		public void GetSettingsDatasAsync(object userState = null)
		{
			InvokeAsync("GetSettingsData", GetSettingsDataCompleted, userState);
		}

		#endregion

		#region Method: SignOut

		public event EventHandler<SoapHttpClientEventArgs> SignOutCompleted;

		[SoapDocumentMethod(ns + "SignOut",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudMessage SignOut()
		{
			return Invoke<CloudMessage>("SignOut");
		}

		public void SignOutsAsync(object userState = null)
		{
			InvokeAsync("SignOut", SignOutCompleted, userState);
		}

		#endregion

		#region Method: Execute

		public event EventHandler<SoapHttpClientEventArgs> ExecuteCompleted;

		[SoapDocumentMethod(ns + "Execute",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public CloudMessage Execute(CloudMessage command)
		{
			return Invoke<CloudMessage>("Execute", command);
		}

		public void ExecuteAsync(CloudMessage command, object userState = null)
		{
			InvokeAsync("Execute", ExecuteCompleted, userState, new object[] { command });
		}

		#endregion
	}

}
