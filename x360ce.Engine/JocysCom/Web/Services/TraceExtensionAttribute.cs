using System;
using System.Web.Services.Protocols;

namespace JocysCom.ClassLibrary.Web.Services
{

	// Create a SoapExtensionAttribute for our SOAP Extension that can be
	// applied to an XML Web service method.
	[AttributeUsage(AttributeTargets.Method)]
	public class TraceExtensionAttribute : SoapExtensionAttribute
	{

		public override Type ExtensionType
		{
			get { return typeof(TraceExtension); }
		}

		public override int Priority
		{
			get { return _Priority; }
			set { _Priority = value; }
		}
		private int _Priority;

	}
}
