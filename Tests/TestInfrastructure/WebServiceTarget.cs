using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The web service the compatibility tests talk to, and the two ways of talking to it.
	/// </summary>
	/// <remarks>
	/// The programs reach the service through <see cref="WebServiceClient"/>, so the tests do
	/// too; a second SOAP stack would prove something about itself rather than about the
	/// program. Three operations of the service are called by no program and have no client
	/// method (<c>GetProgram</c>, <c>GetProgramsDefault</c>, <c>SetProgram</c>); those are sent as
	/// raw SOAP so the whole WSDL is still covered.
	///
	/// The target comes from <c>X360CE_WEBSERVICE_URL</c>. Without it the tests are inconclusive
	/// rather than failing, so <c>Run-Tests.ps1</c> stays green on a machine with no site. The
	/// same tests are pointed at the live service to prove they describe what the programs
	/// depend on, and at a local site to prove the local site matches.
	/// </remarks>
	public static class WebServiceTarget
	{
		public const string UrlVariable = "X360CE_WEBSERVICE_URL";

		public const string SoapNamespace = "http://x360ce.com/";

		/// <summary>The most common controller in the presets, used where any product will do.</summary>
		public static readonly Guid Xbox360ProductGuid = new Guid("028e045e-0000-0000-0000-504944564944");

		/// <summary>The product the owner's crash report was about; two presets live on both databases.</summary>
		public static readonly Guid LogitechG27ProductGuid = new Guid("c29b046d-0000-0000-0000-504944564944");

		public static string Url
		{
			get { return Environment.GetEnvironmentVariable(UrlVariable); }
		}

		/// <summary>True when the target is the public site, where writes are not welcome.</summary>
		public static bool IsLive
		{
			get
			{
				var url = Url;
				return !string.IsNullOrEmpty(url) && !new Uri(url).IsLoopback;
			}
		}

		/// <summary>Stops the test as inconclusive when no target is configured.</summary>
		public static string Require()
		{
			var url = Url;
			if (string.IsNullOrWhiteSpace(url))
				Assert.Inconclusive("Skipped: set " + UrlVariable + " to a x360ce.asmx address to run.");
			return url;
		}

		/// <summary>A client set up exactly as the programs set theirs up.</summary>
		public static WebServiceClient Client()
		{
			var ws = new WebServiceClient();
			ws.Url = Require();
			ws.Timeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;
			return ws;
		}

		/// <summary>
		/// Sends one wrapped document/literal call and returns the body of the reply, or the
		/// SOAP fault text when the service answers with one.
		/// </summary>
		public static XmlDocument Soap(string operation, string innerXml, out string fault)
		{
			var url = Require();
			var envelope =
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns=\"" + SoapNamespace + "\">" +
				"<soap:Body><" + operation + ">" + innerXml + "</" + operation + "></soap:Body></soap:Envelope>";
			var bytes = Encoding.UTF8.GetBytes(envelope);
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "text/xml; charset=utf-8";
			request.Headers["SOAPAction"] = SoapNamespace + operation;
			request.Timeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds;
			using (var stream = request.GetRequestStream())
				stream.Write(bytes, 0, bytes.Length);
			string text;
			try
			{
				using (var response = (HttpWebResponse)request.GetResponse())
				using (var reader = new StreamReader(response.GetResponseStream()))
					text = reader.ReadToEnd();
			}
			catch (WebException ex)
			{
				// ASMX answers a SOAP fault with HTTP 500 and the fault in the body.
				if (ex.Response == null)
					throw;
				using (var reader = new StreamReader(ex.Response.GetResponseStream()))
					text = reader.ReadToEnd();
			}
			var doc = new XmlDocument();
			doc.LoadXml(text);
			var faultNode = doc.GetElementsByTagName("faultstring");
			fault = faultNode.Count > 0 ? faultNode[0].InnerText : null;
			return doc;
		}

		/// <summary>Downloads the WSDL the target publishes.</summary>
		public static XmlDocument Wsdl()
		{
			var url = Require();
			using (var client = new WebClient())
			{
				var doc = new XmlDocument();
				doc.LoadXml(client.DownloadString(url + "?WSDL"));
				return doc;
			}
		}
	}
}
