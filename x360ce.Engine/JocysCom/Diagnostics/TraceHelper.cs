using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace JocysCom.ClassLibrary.Diagnostics
{
	public class TraceHelper
	{

		public static void AddLog(string sourceName, params object[] data)
		{
			// Add source
			var source = new TraceSource(sourceName);
			//source.Listeners.Add(_Listener);
			source.Switch.Level = SourceLevels.All;
			source.TraceData(TraceEventType.Information, 0, data);
			source.Flush();
			source.Close();
		}

		public static void AddLog(string sourceName, NameValueCollection collection)
		{
			// Write Data.
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = ("\t");
			settings.OmitXmlDeclaration = true;
			var sb = new StringBuilder();
			// Create the XmlWriter object and write some content.
			var writer = XmlWriter.Create(sb, settings);
			writer.WriteStartElement("Data");
			foreach (var key in collection.AllKeys)
			{
				var keyString = string.Format("{0}", key);
				var valString = string.Format("{0}", collection[key]);
				writer.WriteElementString(keyString, valString);
			}
			writer.WriteEndElement();
			writer.Flush();
			AddXml(sourceName, sb.ToString());
			writer.Close();

		}

		static void AddXml(string sourceName, string xml)
		{
			using (var sr = new StringReader(xml))
			{
				using (var tr = new XmlTextReader(sr))
				{
					// Settings used to protect from
					// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
					// https://cwe.mitre.org/data/definitions/611.html
					var settings = new XmlReaderSettings();
					settings.DtdProcessing = DtdProcessing.Ignore;
					settings.XmlResolver = null;
					using (var xr = XmlReader.Create(tr, settings))
					{
						var doc = new XPathDocument(tr);
						var nav = doc.CreateNavigator();
						AddLog(sourceName, nav);
					}
				}
			}
		}

	}
}
