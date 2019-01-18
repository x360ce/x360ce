using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.XPath;

namespace JocysCom.ClassLibrary.Web.Services
{
	public class HttpLogModule : IHttpModule
	{

		// If you are running Web Application in Classic mode:
		// <system.web>
		//   <httpModules>
		//     <add name="ClientHttpModule" type="JocysCom.ClassLibrary.Web.Services.HttpLogModule,JocysCom.ClassLibrary" />
		//   </httpModules>
		// </system.web>
		// 
		// If you are running Web Server in Integrated mode:
		// <system.webServer>
		//   <modules>
		//     <add name="ClientHttpModule" type="JocysCom.ClassLibrary.Web.Services.HttpLogModule,JocysCom.ClassLibrary" />
		//   </modules>
		// </system.webServer>

		// <system.diagnostics>
		//   <sources>
		//     <source name="HttpLogModule" switchValue="Verbose">
		//       <listeners>
		//         <add name="HttpPostLogs" />
		//       </listeners>
		//     </source>
		//  </sources >
		//<sharedListeners >
		//  <!-- Files can be opened with SvcTraceViewer.exe.make sure that IIS_IUSRS group has write permissions on this folder. -->
		//  <add name="HttpPostLogs" type="JocysCom.ClassLibrary.Diagnostics.RollingXmlWriterTraceListener, JocysCom.ClassLibrary" initializeData="c:\inetpub\logs\LogFiles\HttpPostListener.svclog" />
		//</sharedListeners>

		void AddLog(params object[] data)
		{
			// Add source
			var source = new TraceSource(GetType().Name);
			//source.Listeners.Add(_Listener);
			source.Switch.Level = SourceLevels.All;
			source.TraceData(TraceEventType.Information, 38, data);
			source.Flush();
			source.Close();
		}

		void AddXml(string xml)
		{
			using (var sr = new StringReader(xml))
			{
				using (var tr = new XmlTextReader(sr))
				{
					var doc = new XPathDocument(tr);
					var nav = doc.CreateNavigator();
					AddLog(nav);
				}
			}
		}

		public HttpLogModule()
		{
		}

		public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			context.BeginRequest += new EventHandler(context_BeginRequest);
		}
	
		private void context_BeginRequest(object sender, EventArgs e)
		{
			if (sender != null && sender is HttpApplication)
			{
				var request = (sender as HttpApplication).Request;
				var response = (sender as HttpApplication).Response;
				if (request != null)
				{
					var settings = new XmlWriterSettings();
					settings.Indent = true;
					settings.IndentChars = ("\t");
					settings.OmitXmlDeclaration = true;
					var sb = new StringBuilder();
					// Create the XmlWriter object and write some content.
					var writer = XmlWriter.Create(sb, settings);
					writer.WriteStartElement("Data");
					writer.WriteElementString("HttpMethod", request.HttpMethod);
					writer.WriteElementString("Form", request.Form.ToString());
					writer.WriteEndElement();
					writer.Flush();
					AddXml(sb.ToString());
					writer.Close();
					// Add to IIS log.
					//if (!string.IsNullOrWhiteSpace(body))
					//	response.AppendToLog(body);
				}
			}
		}

	}
}
