using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Collections.Specialized;
using x360ce.Engine;

namespace x360ce.App.Html2Rtf
{
	public class Converter
	{

		public static string Html2Rtf(string html)
		{
			return Html2Rtf(html, null);
		}


		//•	page-start-number: Page start number (default: 1)
		//•	page-setup-paper-width: Paper width in TWIPS (default: 11907 TWIPS = 21 cm, i.e. A4 format)
		//•	page-setup-paper-height: Paper height in TWIPS (default: 16840 TWIPS = 29.7 cm, i.e. A4 format)
		//•	page-setup-margin-top: Top margin in TWIPS (default: 1440 TWIPS = 1 inch = 2.54 cm)
		//•	page-setup-margin-bottom: Bottom margin in TWIPS (default: 1440 TWIPS = 1 inch = 2.54 cm)
		//•	page-setup-margin-left: Left margin in TWIPS (default: 1134 TWIPS = 2 cm)
		//•	page-setup-margin-right: Right margin in TWIPS (default: 1134 TWIPS = 2 cm)
		//•	font-size-default: Default font size in TWIPS (default: 20 TWIPS = 10 pt.)
		//•	font-name-default: Default font name (default: 'Times New Roman')
		//•	font-name-fixed: Default font name for fixed-width text, like PRE or CODE (default: 'Courier New')
		//•	font-name-barcode: Barcode font name (default: '3 of 9 Barcode')
		//•	header-font-size-default: Header default font size in TWIPS (default: 14 TWIPS = 7 pt.)
		//•	header-distance-from-edge: Default distance between top of page and top of header, in TWIPS (default: 720 TWIPS = 1.27 cm)
		//•	header-indentation-left: Header left indentation in TWIPS (default: 0)
		//•	footer-font-size-default: Footer default font size in TWIPS (default: 14 TWIPS = 7 pt.)
		//•	footer-distance-from-edge: Default distance between bottom of page and bottom of footer, in TWIPS (default: 720 TWIPS = 1.27 cm)
		//•	use-default-footer: Boolean flag: 1 to use default footer (page number and date) or 0 no footer (default: 1)
		//•	document-protected: Boolean flag: 1 protected (cannot be modified) or 0 unprotected (default: 1)
		//•	normalize-space: Boolean flag: 1 spaces are normalized and trimmed, or 0 no normalization no trim (default: 0)
		//•	my-normalize-space: Boolean flag: 1 spaces are normalized (NOT TRIMMED), or 0 no normalization (default: 1)


		public static string Html2Rtf(string html, NameValueCollection parameters)
		{
			// Load data.
			var xml = new System.Xml.XmlDocument();
			xml.Load(EngineHelper.GetResource("Help.htm"));
			xml.DocumentElement.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
			xml.DocumentElement.SetAttribute("xmlns:xhtml2rtf", "http://www.lutecia.info/download/xmlns/xhtml2rtf");
			//xml.DocumentElement.SetAttribute("SelectionLanguage", "XPath");
			//xml.DocumentElement.SetAttribute("SelectionNamespaces", "xmlns='http://www.w3.org/1999/xhtml' xmlns:xhtml='http://www.w3.org/1999/xhtml'");
			xml.Load(new StringReader(xml.OuterXml));
			// Load style sheet.
			var xslDoc = new System.Xml.XmlDocument();
			xslDoc.Load(EngineHelper.GetResource("xhtml2rtf.xsl"));
			//xslDoc.DocumentElement.SetAttribute("SelectionLanguage", "XPath");
			//xslDoc.DocumentElement.SetAttribute("SelectionNamespaces", "xmlns:xhtml='http://www.w3.org/1999/xhtml' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'");
			//xslDoc.Load(new StringReader(xslDoc.OuterXml));
			// Create namespace manager.
			XmlNamespaceManager man = new XmlNamespaceManager(xslDoc.NameTable);
			man.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
			
			// Set parameters in stylesheet
			if (parameters != null)
			{
				foreach (var name in parameters.AllKeys)
				{
					var value = parameters[name];
					var xmlParam = xslDoc.DocumentElement.SelectSingleNode("//xsl:param[@name='" + name + "']", man);
					if (xmlParam != null)
					{
						var xmlParamValue = xmlParam.SelectSingleNode("@select", man);
						if (xmlParamValue != null)
						{
							xmlParamValue.InnerText = value;
						}
					}
				}
			}
			// Load the String into a TextReader
			System.IO.StringReader tr = new System.IO.StringReader(xslDoc.OuterXml);
			// Use that TextReader as the Source for the XmlTextReader
			System.Xml.XmlReader xr = new System.Xml.XmlTextReader(tr);
			// Create a new XslTransform class
			var xsl = new System.Xml.Xsl.XslCompiledTransform(true);
			// Load the XmlReader StyleSheet into the XslTransform class
			xsl.Load(xr, new XsltSettings(false, true), (XmlResolver)null);
			// Create output
			var sb = new System.Text.StringBuilder();
			var tw = new System.IO.StringWriter(sb);
			xsl.Transform(xml, (XsltArgumentList)null, tw);
			// Return RTF document.
			return sb.ToString();
		}

	}
}
