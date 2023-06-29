using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Schema;

namespace JocysCom.ClassLibrary.Runtime
{

	public class XmlValidator
	{
		public List<XmlSchemaException> Exceptions;
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

		public bool IsValid(string xml, XmlSchema xd, bool reportWarnings = false)
		{
			// Create the schema object
			var sc = new XmlSchemaSet();
			sc.Add(xd);
			return IsValid(xml, sc, reportWarnings);
		}

		public bool IsValid(string xml, XmlSchemaSet sc, bool reportWarnings = false)
		{
			_xml = xml;
			// Create reader settings
			var settings = new System.Xml.XmlReaderSettings();
			// Attach event handler which will be fired when validation error occurs
			settings.ValidationEventHandler += XmlReader_ValidationCallBack;
			// Set validation type to schema
			settings.ValidationType = System.Xml.ValidationType.Schema;
			if (reportWarnings)
				settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
			//settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema
			//settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation
			// Add to the collection of schemas in readerSettings
			settings.Schemas = sc;
			//settings.ProhibitDtd = False
			// Create object of XmlReader using XmlReaderSettings
			var xmlMs = new System.IO.StringReader(xml);
			var reader = System.Xml.XmlReader.Create(xmlMs, settings);
			Exceptions = new List<XmlSchemaException>();
			// Parse the file. 
			while (reader.Read()) { }
			reader.Dispose();
			return Exceptions.Count == 0;
		}

		public virtual bool IsValid(string xml, string xsdResource, Type xsdResourceType, bool reportWarnings = false)
		{
			Exceptions = new List<XmlSchemaException>();
			_xml = xml;
			_xsd = xsdResource;
			System.IO.StreamReader streamReader = null;
			if (xsdResourceType != null)
			{
				var asm = System.Reflection.Assembly.GetAssembly(xsdResourceType);
				var resourceName = asm.GetManifestResourceNames().First(x => x.EndsWith(xsdResource));
				var stream = asm.GetManifestResourceStream(resourceName);
				streamReader = new System.IO.StreamReader(stream);
			}
			else
			{
				streamReader = new System.IO.StreamReader(xsdResource);
			}
			var xsdString = streamReader.ReadToEnd();
			streamReader.Dispose();
			var ms = new System.IO.StringReader(xsdString);
			var xd = XmlSchema.Read(ms, new ValidationEventHandler(XmlSchema_ValidationCallBack));
			var isValid = IsValid(xml, xd, reportWarnings);
			ms.Dispose();
			return isValid;
		}

		public XmlSchemaSet ExportSchemaSet(Type tp, params Type[] knownTypes)
		{
			var exporter = new XsdDataContractExporter();
			// Use the ExportOptions to add the Possessions type to the  
			// collection of KnownTypes.  
			if (knownTypes.Length > 0)
			{
				var eo = new ExportOptions();
				foreach (var kt in knownTypes)
					eo.KnownTypes.Add(kt);
				exporter.Options = eo;
			}
			if (exporter.CanExport(tp))
				exporter.Export(tp);
			return exporter.Schemas;
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
				foreach (var kt in knownTypes)
					eo.KnownTypes.Add(kt);
				exporter.Options = eo;
			}
			if (exporter.CanExport(type))
			{
				exporter.Export(type);

				var XmlNameValue = exporter.GetRootElementName(type);
				var nameSpace = XmlNameValue.Namespace;
				XmlSchema mainSchema = null;
				foreach (XmlSchema schema in exporter.Schemas.Schemas(nameSpace))
				{
					mainSchema = schema;
				}
				var i = 0;
				foreach (XmlSchema schema in exporter.Schemas.Schemas())
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

		// Display any validation errors.
		private void XmlSchema_ValidationCallBack(object sender, ValidationEventArgs e)
		{
			if (e.Exception != null)
				ProcessException(e.Exception);
			//Console.WriteLine("    Validation Error: {0}" & vbCrLf, e.Message)
		}

		// Display any validation errors.
		private void XmlReader_ValidationCallBack(object sender, ValidationEventArgs e)
		{
			if (e.Exception != null)
				ProcessException(e.Exception);
		}

		public static void RethrowWithNoStackTraceLoss(Exception ex)
		{
			var remoteStackTraceString = typeof(Exception).GetField("_remoteStackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			remoteStackTraceString.SetValue(ex, ex.StackTrace);
			throw ex;
		}

		private void ProcessException(XmlSchemaException ex)
		{
			// If stack traces is empty then...
			if (ex.StackTrace is null)
			{
				try
				{
					throw new XmlSchemaException(ex.Message, ex, ex.LineNumber, ex.LinePosition);
				}
				catch (XmlSchemaException ex1)
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
				ex.Data.Add("XML Path", _xmlPath);
			if (_xml.Length > 8192)
			{
				ex.Data.Add("XML Size", _xml.Length.ToString("#,##0"));
				_xml = _xml.Substring(0, 8192);
			}
			ex.Data.Add("XML", _xml);
		}

	}
}
