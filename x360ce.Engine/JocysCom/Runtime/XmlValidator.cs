using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace JocysCom.ClassLibrary.Runtime
{

	public class XmlValidator
	{
		public List<System.Xml.Schema.XmlSchemaException> Exceptions;
		// Parameters for exception.
		public string _xmlPath;
		private string _xml;
		private string _xsd;

		public bool IsValid(Type type, string xml, bool reportWarnings, params Type[] knownTypes)
		{
			var xss = ExportSchemaSet(type, knownTypes);
			return IsValid(xml, xss, reportWarnings);
		}

		public bool IsValid<T>(string xml, bool reportWarnings, params Type[] knownTypes)
		{
			var xss = ExportSchemaSet<T>(knownTypes);
			return IsValid(xml, xss, reportWarnings);
		}

		public XmlSchemaSet ExportSchemaSet(Type tp, params Type[] knownTypes)
		{
			var exporter = new XsdDataContractExporter();
			// Use the ExportOptions to add the Possessions type to the  
			// collection of KnownTypes.  
			if (knownTypes.Length > 0)
			{
				var eo = new ExportOptions();
				foreach (Type kt in knownTypes)
					eo.KnownTypes.Add(kt);
				exporter.Options = eo;
			}
			if (exporter.CanExport(tp))
			{
				exporter.Export(tp);
			}
			return exporter.Schemas;
			//System.IO.MemoryStream ms = new System.IO.MemoryStream();
			//if (exporter.CanExport(tp))
			//{
			//	exporter.Export(tp);
			//	var XmlNameValue = exporter.GetRootElementName(tp);
			//	string nameSpace = XmlNameValue.Namespace;
			//	foreach (System.Xml.Schema.XmlSchema schema in exporter.Schemas.Schemas(nameSpace))
			//	{
			//		foreach (System.Xml.Schema.XmlSchema sc in exporter.Schemas.Schemas())
			//		{
			//			//var import = new XmlSchemaImport();
			//			var include = new XmlSchemaInclude();
			//			include.Schema = sc;
			//			schema.Includes.Add(include);
			//		}
			//		schema.Write(ms);
			//	}
			//}
			//var xd = new XmlSchema();
			//ms.Position = 0;
			//xd = XmlSchema.Read(ms, new ValidationEventHandler(XmlSchema_ValidationCallBack));
			//return xd;
		}

		public XmlSchemaSet ExportSchemaSet<T>(params Type[] knownTypes)
		{
			return ExportSchemaSet(typeof(T), knownTypes);
		}

		public void ExportXsdToFile(Type type, string filename, System.Text.Encoding encoding, params Type[] knownTypes)
		{
			var exporter = new XsdDataContractExporter();
			// Use the ExportOptions to add the Possessions type to the  
			// collection of KnownTypes.  
			if (knownTypes.Length > 0)
			{
				var eo = new ExportOptions();
				foreach (Type kt in knownTypes)
					eo.KnownTypes.Add(kt);
				exporter.Options = eo;
			}
			if (exporter.CanExport(type))
			{
				exporter.Export(type);

				var XmlNameValue = exporter.GetRootElementName(type);
				string nameSpace = XmlNameValue.Namespace;
				XmlSchema mainSchema = null;
				foreach (System.Xml.Schema.XmlSchema schema in exporter.Schemas.Schemas(nameSpace))
				{
					mainSchema = schema;
				}
				var i = 0;
				foreach (System.Xml.Schema.XmlSchema schema in exporter.Schemas.Schemas())
				{
					if (schema == mainSchema)
					{
						var file = new System.Xml.XmlTextWriter(filename, encoding);
						file.Formatting = System.Xml.Formatting.Indented;
						schema.Write(file);
						file.Flush();
						file.Close();
					}
					else
					{
						var dir = System.IO.Path.GetDirectoryName(filename);
						var nam = System.IO.Path.GetFileNameWithoutExtension(filename);
						var ext = System.IO.Path.GetExtension(filename);
						var newName = System.IO.Path.Combine(dir, nam + "." + (i++) + ext);
						var file = new System.Xml.XmlTextWriter(newName, encoding);
						file.Formatting = System.Xml.Formatting.Indented;
						schema.Write(file);
						file.Flush();
						file.Close();
					}
				}
			}
		}

		public void ExportXsdToFile<T>(string filename, System.Text.Encoding encoding, params Type[] knownTypes)
		{
			ExportXsdToFile(typeof(T), filename, encoding, knownTypes);
		}

		public bool IsValid(string xml, System.Xml.Schema.XmlSchemaSet sc, bool reportWarnings = false)
		{
			_xml = xml;
			// Create reader settings
			System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings();
			// Attach event handler whic will be fired when validation error occurs
			settings.ValidationEventHandler += XmlReader_ValidationCallBack;
			// Set validation type to schema
			settings.ValidationType = System.Xml.ValidationType.Schema;
			if (reportWarnings)
			{
				settings.ValidationFlags |= System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings;
			}
			//settings.ValidationFlags = settings.ValidationFlags Or System.Xml.Schema.XmlSchemaValidationFlags.ProcessInlineSchema
			//settings.ValidationFlags = settings.ValidationFlags Or System.Xml.Schema.XmlSchemaValidationFlags.ProcessSchemaLocation
			// Add to the collection of schemas in readerSettings
			settings.Schemas = sc;
			//settings.ProhibitDtd = False
			// Create object of XmlReader using XmlReaderSettings
			System.IO.StringReader xmlMs = new System.IO.StringReader(xml);
			System.Xml.XmlReader reader = System.Xml.XmlReader.Create(xmlMs, settings);
			Exceptions = new List<System.Xml.Schema.XmlSchemaException>();
			// Parse the file. 
			while (reader.Read()) { }
			return Exceptions.Count == 0;
		}

		public bool IsValid(string xml, System.Xml.Schema.XmlSchema xd, bool reportWarnings = false)
		{
			// Create the schema object
			var sc = new System.Xml.Schema.XmlSchemaSet();
			sc.Add(xd);
			return IsValid(xml, sc, reportWarnings);
		}

		public bool IsValid(string xml, string xsdResource, Type xsdResourceType, bool reportWarnings = false)
		{
			Exceptions = new List<System.Xml.Schema.XmlSchemaException>();
			_xml = xml;
			_xsd = xsdResource;
			System.IO.StreamReader streamReader = null;
			if (xsdResourceType != null)
			{
				var asm = System.Reflection.Assembly.GetAssembly(xsdResourceType);
				var resourceName = asm.GetManifestResourceNames().First(x => x.EndsWith(xsdResource));
				System.IO.Stream stream = asm.GetManifestResourceStream(resourceName);
				streamReader = new System.IO.StreamReader(stream);
			}
			else
			{
				streamReader = new System.IO.StreamReader(xsdResource);
			}
			var xsdString = streamReader.ReadToEnd();
			var ms = new System.IO.StringReader(xsdString);
			var xd = System.Xml.Schema.XmlSchema.Read(ms, new System.Xml.Schema.ValidationEventHandler(XmlSchema_ValidationCallBack));
			return IsValid(xml, xd, reportWarnings);
		}

		// Display any validation errors.
		private void XmlSchema_ValidationCallBack(object sender, System.Xml.Schema.ValidationEventArgs e)
		{
			if (e.Exception != null)
				ProcessException(e.Exception);
			//Console.WriteLine("    Validation Error: {0}" & vbCrLf, e.Message)
		}

		// Display any validation errors.
		private void XmlReader_ValidationCallBack(object sender, System.Xml.Schema.ValidationEventArgs e)
		{
			if (e.Exception != null)
				ProcessException(e.Exception);
		}

		public static void RethrowWithNoStackTraceLoss(Exception ex)
		{
			System.Reflection.FieldInfo remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(ex, ex.StackTrace);
			throw ex;
		}

		private void ProcessException(System.Xml.Schema.XmlSchemaException ex)
		{
			// If stack traces is empty then...
			if (ex.StackTrace == null)
			{
				try
				{
					throw new System.Xml.Schema.XmlSchemaException(ex.Message, ex, ex.LineNumber, ex.LinePosition);
				}
				catch (System.Xml.Schema.XmlSchemaException ex1)
				{
					//Dim remoteStackTraceString As Reflection.FieldInfo = GetType(Exception).GetField("_remoteStackTraceString", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
					// to the current InnerException.StackTrace
					//remoteStackTraceString.SetValue(ex, New StackTrace(True).ToString())
					ex = ex1;
				}
			}
			Exceptions.Add(ex);
			ex.Data.Add("XSD", _xsd);
			if (!string.IsNullOrEmpty(_xmlPath))
			{
				ex.Data.Add("XML Path", _xmlPath);
			}
			if (_xml.Length > 8192)
			{
				ex.Data.Add("XML Size", _xml.Length.ToString("#,##0"));
				_xml = _xml.Substring(0, 8192);
			}
			ex.Data.Add("XML", _xml);
		}

	}
}
