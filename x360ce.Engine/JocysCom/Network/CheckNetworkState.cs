using System.ComponentModel;

namespace JocysCom.ClassLibrary.Network
{
	public enum CheckNetworkState
	{
		///<summary>Network state was not checked or unknown.</summary>
		[Description("Network state was not checked or unknown.")]
		None = 0,
		///<summary>HTTP check for known reliable website failed.</summary>
		[Description("HTTP check for known and reliable website failed.")]
		PublicHttp = 1,
		///<summary>HTTPS check for known reliable website failed.</summary>
		[Description("HTTPS check for known and reliable website failed.")]
		PublicHttps = 2,
		///<summary>Public DNS Server check failed.</summary>
		[Description("Public DNS Server check failed.")]
		PublicDns = 3,
		///<summary>Remote Web Server failed to respond properly.</summary>
		[Description("Remote Web Server failed to respond properly.")]
		RemoteWebServer = 4,
		///<summary>Remote Web Service failed to respond properly.</summary>
		[Description("Remote Web Service failed to respond properly.")]
		RemoteWebService = 5,
		///<summary>All tests were successful.</summary>
		[Description("All tests were successful.")]
		OK = 6
	}
}
