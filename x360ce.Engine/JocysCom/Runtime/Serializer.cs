using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace JocysCom.ClassLibrary.Runtime
{
	public static class Serializer
	{

		#region Helper Functions

		/// <summary>
		/// Read file content in multiple attempts.
		/// </summary>
		/// <param name="path">The file to open for reading.</param>
		/// <param name="attempts">Number of attempts to read from the file. Default 2 times.</param>
		/// <param name="waitTime">Wait time between attempts. Default 500ms.</param>
		/// <returns>A byte array containing the contents of the file.</returns>
		public static byte[] ReadFile(string path, int attempts = 2, int waitTime = 500)
		{
			while (true)
			{
				attempts -= 1;
				try
				{
					// CWE-73: External Control of File Name or Path
					// Note: False Positive. File path is not externally controlled by the user.
					return System.IO.File.ReadAllBytes(path);
				}
				catch (Exception)
				{
					if (attempts > 0)
					{
						new System.Threading.ManualResetEvent(false).WaitOne(waitTime);
						continue;
					}
					throw;
				}
			}
		}

		/// <summary>
		/// Write file content in multiple attempts.
		/// </summary>
		/// <param name="path">The file to open for writing.</param>
		/// <param name="bytes">The bytes to write to the file.</param>
		/// <param name="attempts">Number of attempts to write into the file. Default 2 times.</param>
		/// <param name="waitTime">Wait time between attempts. Default 500ms.</param>
		public static void WriteFile(string path, byte[] bytes, int attempts = 2, int waitTime = 500)
		{
			while (true)
			{
				attempts -= 1;
				try
				{
					// WriteAllBytes will lock file for writing and reading.
					// CWE-73: External Control of File Name or Path
					// Note: False Positive. File path is not externally controlled by the user.
					System.IO.File.WriteAllBytes(path, bytes);
					return;
				}
				catch (Exception)
				{
					if (attempts > 0)
					{
						new System.Threading.ManualResetEvent(false).WaitOne(waitTime);
						continue;
					}
					throw;
				}
			}
		}

		#endregion

		#region JSON

		// Notes: Use [DataMember(EmitDefaultValue = false, IsRequired = false)] attribute
		// if you don't want to serialize default values.

		static object JsonSerializersLock = new object();
		static Dictionary<Type, DataContractJsonSerializer> JsonSerializers;
		public static DataContractJsonSerializer GetJsonSerializer(Type type, DataContractJsonSerializerSettings settings = null)
		{
			lock (JsonSerializersLock)
			{
				if (JsonSerializers is null) JsonSerializers = new Dictionary<Type, DataContractJsonSerializer>();
				if (!JsonSerializers.ContainsKey(type))
				{
					// Simple dictionary format looks like this: { "Key1": "Value1", "Key2": "Value2" }
					// DataContractJsonSerializerSettings requires .NET 4.5
					if (settings is null)
					{
						settings = new DataContractJsonSerializerSettings();
						settings.IgnoreExtensionDataObject = true;
						settings.UseSimpleDictionaryFormat = true;
					}
					var serializer = new DataContractJsonSerializer(type, settings);
					JsonSerializers.Add(type, serializer);
				}
			}
			return JsonSerializers[type];
		}

		public static Func<object, Encoding, string> _SerializeToJson;
		public static Func<string, Type, Encoding, object> _DeserializeFromJson;

#if NETCOREAPP
		private static System.Text.Json.JsonSerializerOptions GetJsonOptions()
		{
			var options = new System.Text.Json.JsonSerializerOptions();
#if NETCOREAPP2_1 || NETCOREAPP3_0 || NETCOREAPP3_1
			options.IgnoreNullValues = true;
#else
			options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
#endif
			options.PropertyNamingPolicy = null;
			options.WriteIndented = true;
			options.MaxDepth = 64;
			// Add support for de-serializing enumeration strings.
			options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
			return options;
		}
#endif

		/// <summary>
		/// Serialize object to JSON string.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>JSON string.</returns>
		public static string SerializeToJson(object o, Encoding encoding = null)
		{
			if (_SerializeToJson != null)
				return _SerializeToJson(o, encoding);
			if (o is null)
				return null;
#if NETCOREAPP
			var options = GetJsonOptions();
			var json = System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), options);
			return json;
#else
			var serializer = GetJsonSerializer(o.GetType());
			var ms = new MemoryStream();
			lock (serializer) { serializer.WriteObject(ms, o); }
			if (encoding is null)
				encoding = Encoding.UTF8;
			var json = encoding.GetString(ms.ToArray());
			ms.Close();
			return json;
#endif
		}

		/// <summary>
		/// De-serialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>The de-serialized object.</returns>
		public static object DeserializeFromJson(string json, Type type, Encoding encoding = null)
		{
			if (_DeserializeFromJson != null)
				return _DeserializeFromJson(json, type, encoding);
			if (json is null)
				return null;
#if NETCOREAPP
			var options = GetJsonOptions();
			var o = System.Text.Json.JsonSerializer.Deserialize(json, type, options);
			return o;
#else
			var serializer = GetJsonSerializer(type);
			if (encoding is null)
				encoding = Encoding.UTF8;
			var bytes = encoding.GetBytes(json);
			var ms = new MemoryStream(bytes);
			object o;
			lock (serializer) { o = serializer.ReadObject(ms); }
			ms.Close();
			return o;
#endif
		}

		/// <summary>
		/// De-serialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>The de-serialized object.</returns>
		public static T DeserializeFromJson<T>(string json, Encoding encoding = null)
		{
			return (T)DeserializeFromJson(json, typeof(T), encoding);
		}

		// Created by: https://stackoverflow.com/users/17211/vince-panuccio
		// https://stackoverflow.com/questions/4580397/json-formatter-in-c
		public static string FormatJson(string json, string indent = "\t")
		{
			var indentation = 0;
			var quoteCount = 0;
			var escapeCount = 0;
			var result =
				from ch in json ?? string.Empty
				let escaped = (ch == '\\' ? escapeCount++ : escapeCount > 0 ? escapeCount-- : escapeCount) > 0
				let quotes = ch == '"' && !escaped ? quoteCount++ : quoteCount
				let unquoted = quotes % 2 == 0
				let colon = ch == ':' && unquoted ? ": " : null
				let nospace = char.IsWhiteSpace(ch) && unquoted ? string.Empty : null
				let lineBreak = ch == ',' && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, indentation)) : null
				let openChar = (ch == '{' || ch == '[') && unquoted ? ch + Environment.NewLine + string.Concat(Enumerable.Repeat(indent, ++indentation)) : ch.ToString()
				let closeChar = (ch == '}' || ch == ']') && unquoted ? Environment.NewLine + string.Concat(Enumerable.Repeat(indent, --indentation)) + ch : ch.ToString()
				select colon ?? nospace ?? lineBreak ?? (openChar.Length > 1 ? openChar : closeChar);
			return string.Concat(result);
		}

		//public static string FormatJson(string json, bool indent = true)
		//{
		//	using document = JsonDocument.Parse(json);
		//	using var stream = new MemoryStream();
		//	using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = indent });
		//	document.WriteTo(writer);
		//	writer.Flush();
		//	return Encoding.UTF8.GetString(stream.ToArray());
		//}

		#endregion

		#region XML

		/// <summary>
		/// Reformat XML document.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static string XmlFormat(string xml)
		{
			var xd = new XmlDocument();
			xd.XmlResolver = null;
			xd.LoadXml(xml);
			var sb = new StringBuilder();
			var xws = new XmlWriterSettings();
			xws.Indent = true;
			xws.CheckCharacters = true;
			// XmlReader normalizes all newlines and converts '\r\n' to '\n'.
			// This requires to save NewLines with  option which
			// "Entitize" option replace '\r' with '&#xD;' in text node values.
			xws.NewLineHandling = NewLineHandling.Entitize;
			var xw = XmlWriter.Create(sb, xws);
			xd.WriteTo(xw);
			xw.Close();
			return sb.ToString();
		}

		static object XmlSerializersLock = new object();
		static Dictionary<Type, XmlSerializer> XmlSerializers;
		public static XmlSerializer GetXmlSerializer(Type type)
		{
			lock (XmlSerializersLock)
			{
				if (XmlSerializers is null)
					XmlSerializers = new Dictionary<Type, XmlSerializer>();
				if (!XmlSerializers.ContainsKey(type))
				{
					var extraTypes = new Type[] { typeof(string) };
					XmlSerializers.Add(type, new XmlSerializer(type, extraTypes));
				}
			}
			return XmlSerializers[type];
		}

		#endregion

		#region XML: Serialize

		/// <summary>
		/// Serialize object to XML document.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <returns>XML document</returns>
		public static XmlDocument SerializeToXml(object o)
		{
			if (o is null)
				return null;
			var serializer = GetXmlSerializer(o.GetType());
			var ms = new MemoryStream();
			lock (serializer) { serializer.Serialize(ms, o); }
			ms.Seek(0, SeekOrigin.Begin);
			var doc = new XmlDocument();
			doc.XmlResolver = null;
			// Settings used to protect from:
			// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
			// https://cwe.mitre.org/data/definitions/611.html
			var settings = new XmlReaderSettings();
			settings.DtdProcessing = DtdProcessing.Ignore;
			settings.XmlResolver = null;
			settings.CloseInput = false;
			var reader = XmlReader.Create(ms, settings);
			doc.Load(reader);
			reader.Dispose();
			ms.Close();
			return doc;
		}

		static T SeriallizeToXml<T>(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null, bool indent = true)
		{
			if (o is null)
				return default(T);
			// Create serialization settings.
			encoding = encoding ?? Encoding.UTF8;
			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = omitXmlDeclaration;
			settings.Encoding = encoding;
			settings.Indent = indent;
			// XmlReader normalizes all newlines and converts '\r\n' to '\n'.
			// This requires to save NewLines with  option which
			// "Entitize" option replace '\r' with '&#xD;' in text node values.
			settings.NewLineHandling = NewLineHandling.Entitize;
			// Serialize.
			var serializer = GetXmlSerializer(o.GetType());
			// Serialize in memory first, so file will be locked for shorter times.
			var ms = new MemoryStream();
			var xw = XmlWriter.Create(ms, settings);
			try
			{
				lock (serializer)
				{
					if (!string.IsNullOrEmpty(comment))
					{
						xw.WriteStartDocument();
						xw.WriteComment(comment);
					}
					if (omitXmlDeclaration)
					{
						//Create our own namespaces for the output
						var ns = new XmlSerializerNamespaces();
						//Add an empty namespace and empty value
						ns.Add("", "");
						serializer.Serialize(xw, o, ns);
					}
					else
					{
						serializer.Serialize(xw, o);
					}
					if (!string.IsNullOrEmpty(comment))
					{
						xw.WriteEndDocument();
					}
					// Make sure that all data flushed into memory stream.
					xw.Flush();
				}
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				// This will close underlying MemoryStream too.
				xw.Close();
			}
			// ToArray will return all bytes from memory stream despite it being closed.
			// Bytes will start with Byte Order Mark(BOM) and are ready to write into file.
			var xmlBytes = ms.ToArray();
			// If string must be returned then...
			if (typeof(T) == typeof(string))
			{
				// Use StreamReader to remove Byte Order Mark(BOM).
				var ms2 = new MemoryStream(xmlBytes);
				var sr = new StreamReader(ms2, true);
				var xmlString = sr.ReadToEnd();
				// This will close underlying MemoryStream too.
				sr.Close();
				return (T)(object)xmlString;
			}
			else
			{
				return (T)(object)xmlBytes;
			}
		}

		/// <summary>
		/// Serialize object to XML string.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		/// <param name="namespaces">Contains the XML namespaces and prefixes that the XmlSerializer  uses to generate qualified names in an XML-document instance.</param>
		/// <returns>XML string.</returns>
		public static string SerializeToXmlString(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null, bool indent = true)
		{
			return SeriallizeToXml<string>(o, encoding, omitXmlDeclaration, comment, indent);
		}

		/// <summary>
		/// Serialize object to XML file.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="path">The file name to write to.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		public static void SerializeToXmlFile(object o, string path, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null, int attempts = 2, int waitTime = 500)
		{
			var bytes = (o is null)
				? new byte[0]
				: SeriallizeToXml<byte[]>(o, encoding, omitXmlDeclaration, comment);
			// Write serialized data into file.
			WriteFile(path, bytes, attempts, waitTime);
		}

		/// <summary>
		/// Serialize object to XML bytes with Byte Order Mark (BOM).
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="path">The file name to write to.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		public static byte[] SerializeToXmlBytes(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null)
		{
			var bytes = (o is null)
				? new byte[0]
				: SeriallizeToXml<byte[]>(o, encoding, omitXmlDeclaration, comment);
			return bytes;
		}

		#endregion

		#region XML: De-serialize

		/// <summary>
		/// De-serialize System.Collections.Generic.List to XML document.
		/// </summary>
		/// <param name="doc">XML document representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <returns>XML document</returns>
		public static object DeserializeFromXml(XmlDocument doc, Type type)
		{
			if (doc is null)
				return null;
			return DeserializeFromXmlString(doc.OuterXml, type);
		}

		/// <summary>
		/// De-serialize object from XML bytes. XML bytes can contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">XML string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">Encoding to use (default is UTF8) if Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlBytes(byte[] bytes, Type type, Encoding encoding = null)
		{
			var ms = new MemoryStream(bytes);
			// Use stream reader (inherits from TextReader) to avoid encoding errors.
			// Use specified encoding if Byte Order Mark (BOM) is missing.
			var sr = new StreamReader(ms, encoding ?? Encoding.UTF8, true);
			// Settings used to protect from
			// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
			// https://cwe.mitre.org/data/definitions/611.html
			var settings = new XmlReaderSettings();
			settings.DtdProcessing = DtdProcessing.Ignore;
			settings.XmlResolver = null;
			settings.IgnoreWhitespace = true;
			// Stream 'ms' and 'sr' will be disposed by the reader.
			using (var reader = XmlReader.Create(sr, settings))
			{
				object o;
				var serializer = GetXmlSerializer(type);
				lock (serializer) { o = serializer.Deserialize(reader); }
				return o;
			}
		}

		// Example how to add the missing namespaces.
		// 
		// Create a new NameTable
		//var nameTable = new NameTable();
		// Create a new NamespaceManager
		//var nsMgr = new XmlNamespaceManager(nameTable);
		// Add namespaces used in the XML
		//nsMgr.AddNamespace("xlink", "urn:http://namespaceurl.com");
		// Create the XmlParserContext using the previous declared XmlNamespaceManager
		//var inputContext = new XmlParserContext(null, nsMgr, null, XmlSpace.None);

		/// <summary>
		/// De-serialize object from XML string. XML string must not contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">XML string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="inputContext">You can use inputContext to add missing namespaces.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlString(string xml, Type type, XmlParserContext inputContext = null, bool ignoreNamespaces = false)
		{
			// Note: If you are getting de-serialization error in XML document(1,1) then there is a chance that
			// you are trying to de-serialize string which contains Byte Order Mark (BOM) which must not be there.
			// Probably you used "var xml = System.Text.Encoding.GetString(bytes)" directly on file content.
			// You should use "StreamReader" on file content, because this method will strip BOM properly
			// when converting bytes to string.
			// Settings used to protect from
			// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
			// https://cwe.mitre.org/data/definitions/611.html
			var settings = new XmlReaderSettings();
			settings.DtdProcessing = DtdProcessing.Ignore;
			settings.XmlResolver = null;
			object o;
			var serializer = GetXmlSerializer(type);
			// Stream 'sr' will be disposed by the reader.
			using (var sr = new StringReader(xml))
			{
				if (ignoreNamespaces)
				{
					using (var tr = new XmlTextReader(sr))
					{
						// Ignore namespaces.
						tr.Namespaces = false;
						using (var reader = XmlReader.Create(tr, settings))
							lock (serializer) { o = serializer.Deserialize(reader); }
					}
					return o;
				}
				using (var reader = XmlReader.Create(sr, settings, inputContext))
					lock (serializer) { o = serializer.Deserialize(reader); }
				return o;
			}
		}

		/// <summary>
		/// De-serialize object from XML file.
		/// </summary>
		/// <param name="filename">The file name to read from.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">Encoding to use (default is UTF8) if file Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlFile(string filename, Type type, Encoding encoding = null, int attempts = 1, int waitTime = 500)
		{
			// Read full file content first, so file will be locked for shorter period of time.
			var bytes = ReadFile(filename, attempts, waitTime);
			if (bytes is null || bytes.Length == 0)
				return null;
			return DeserializeFromXmlBytes(bytes, type, encoding);
		}

		/// <summary>
		/// De-serialize object from XML Document.
		/// </summary>
		/// <param name="doc">XML document representing object.</param>
		/// <returns>XML document</returns>
		public static T DeserializeFromXml<T>(XmlDocument doc)
		{
			return (T)DeserializeFromXml(doc, typeof(T));
		}

		/// <summary>
		/// De-serialize object from XML string.
		/// </summary>
		/// <param name="xml">XML string representing object.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		/// <returns>Object.</returns>
		public static T DeserializeFromXmlString<T>(string xml)
		{
			return (T)DeserializeFromXmlString(xml, typeof(T));
		}

		/// <summary>
		/// De-serialize object from XML file.
		/// </summary>
		/// <param name="filename">The file name to read from.</param>
		/// <param name="encoding">Specified encoding will be used if file Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static T DeserializeFromXmlFile<T>(string filename, Encoding encoding = null, int attempts = 1, int waitTime = 500)
		{
			return (T)DeserializeFromXmlFile(filename, typeof(T), encoding, attempts, waitTime);
		}

		/// <summary>
		/// De-serialize object from XML bytes. XML bytes can contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">XML string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">The encoding to use (default is UTF8) if Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static T DeserializeFromXmlBytes<T>(byte[] bytes, Encoding encoding = null)
		{
			return (T)DeserializeFromXmlBytes(bytes, typeof(T), encoding);
		}

		#endregion

		#region XSD: Serialize

		/// <summary>
		/// Serialize object schema to XSD file.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="path">The file name to write to.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		public static void SerializeToXsdFile(object o, string path, Encoding encoding = null, bool omitXmlDeclaration = false, int attempts = 2, int waitTime = 500)
		{
			if (o is null)
			{
				WriteFile(path, new byte[0], attempts, waitTime);
				return;
			}
			encoding = encoding ?? Encoding.UTF8;
			// Create serialization settings.
			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = omitXmlDeclaration;
			settings.Encoding = encoding;
			settings.Indent = true;
			// XmlReader normalizes all newlines and converts '\r\n' to '\n'.
			// This requires to save NewLines with  option which
			// "Entitize" option replace '\r' with '&#xD;' in text node values.
			settings.NewLineHandling = NewLineHandling.Entitize;
			// Serialize in memory first, so file will be locked for shorter times.
			var ms = new MemoryStream();
			var xw = XmlWriter.Create(ms, settings);
			var serializer = GetXmlSerializer(o.GetType());
			try
			{
				var exporter = new XsdDataContractExporter();
				if (exporter.CanExport(o.GetType()))
				{
					exporter.Export(o.GetType());
					//Console.WriteLine("number of schemas: {0}", exporter.Schemas.Count);
					XmlSchemaSet schemas = exporter.Schemas;
					XmlQualifiedName XmlNameValue = exporter.GetRootElementName(o.GetType());
					string nameSpace = XmlNameValue.Namespace;
					foreach (XmlSchema schema in schemas.Schemas(nameSpace))
					{
						schema.Write(xw);
					}
				}
			}
			catch (Exception)
			{
				xw.Close();
				// CA2202: Do not dispose objects multiple times
				//ms.Close();
				throw;
			}
			xw.Flush();
			byte[] bytes = ms.ToArray();
			xw.Close();
			// CA2202: Do not dispose objects multiple times
			//ms.Close();
			// Write serialized data into file.
			WriteFile(path, bytes, attempts, waitTime);
		}

		#endregion

	}

}
