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
			// The array is one argument. Passed bare it would spread into the params list.
			return Invoke<SearchResult>("SearchSettings", new object[] { args });
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

		#region Method: GetServerInfo

		[SoapDocumentMethod(ns + "GetServerInfo",
			RequestNamespace = ns, ResponseNamespace = ns,
			Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
		public ServerInfo GetServerInfo()
		{
			return Invoke<ServerInfo>("GetServerInfo");
		}

		/// <summary>Asks the service at an address whether it is there and working, and says what it found in one line.</summary>
		/// <remarks>
		/// Three answers are possible. A service from 4.23 on reports its version and whether its
		/// database answers. An older service does not know the question, so a small request every
		/// version answers is made instead, which proves the address without a version. Anything
		/// else is not an x360ce service, or not reachable, and the reason is passed on.
		/// </remarks>
		/// <param name="url">The service address to try.</param>
		/// <param name="working">True when the service answered and, where it can say, its database did too.</param>
		public static string Probe(string url, out bool working)
		{
			var ws = new WebServiceClient { Url = url, Timeout = 10000 };
			var watch = System.Diagnostics.Stopwatch.StartNew();
			try
			{
				var info = ws.GetServerInfo();
				working = string.IsNullOrEmpty(info.Error);
				return working
					? string.Format("Service {0} answers in {1} ms; database '{2}' answers, server time {3:HH:mm:ss} UTC.",
						info.Version, watch.ElapsedMilliseconds, info.Database, info.DatabaseUtcTime)
					: string.Format("Service {0} answers in {1} ms, but its database does not: {2}",
						info.Version, watch.ElapsedMilliseconds, info.Error);
			}
			catch (SoapException)
			{
				// Asked a question it does not know: a service older than 4.23, or not ours at all.
			}
			catch (System.Net.WebException ex)
			{
				working = false;
				return string.Format("No answer from {0}: {1}", url, ex.Message);
			}
			catch (InvalidOperationException ex)
			{
				// The reply was not SOAP: a web page, an error page, something else at that address.
				working = false;
				return string.Format("Not an x360ce service at {0}: {1}", url, ex.Message);
			}
			try
			{
				ws.GetVendors();
				working = true;
				return string.Format("Service answers in {0} ms. It is older than 4.23 and does not report its version.", watch.ElapsedMilliseconds);
			}
			catch (Exception ex) when (ex is SoapException || ex is System.Net.WebException || ex is InvalidOperationException)
			{
				working = false;
				return string.Format("Not an x360ce service at {0}: {1}", url, ex.Message);
			}
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
