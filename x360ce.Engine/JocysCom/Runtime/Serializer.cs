using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Xml.Schema;

namespace JocysCom.ClassLibrary.Runtime
{
	public class Serializer
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

		#region Bytes

		static object ByteSerializersLock = new object();
		static Dictionary<Type, NetDataContractSerializer> ByteSerializers;
		static NetDataContractSerializer GetByteSerializer(Type type)
		{
			lock (ByteSerializersLock)
			{
				if (ByteSerializers == null) ByteSerializers = new Dictionary<Type, NetDataContractSerializer>();
				if (!ByteSerializers.ContainsKey(type))
				{
					ByteSerializers.Add(type, new NetDataContractSerializer());
				}
			}
			return ByteSerializers[type];
		}

		/// <summary>
		/// Serialize object to byte array.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <returns>Byte array.</returns>
		public static byte[] SerializeToBytes(object o)
		{
			if (o == null) return null;
			NetDataContractSerializer serializer = GetByteSerializer(o.GetType());
			MemoryStream ms = new MemoryStream();
			lock (serializer) { serializer.Serialize(ms, o); }
			byte[] bytes = ms.ToArray();
			ms.Close();
			ms = null;
			return bytes;
		}

		/// <summary>
		/// Deserialize object from byte array.
		/// </summary>
		/// <param name="bytes">Byte array representing object. </param>
		/// <returns>Object.</returns>
		public static object DeserializeFromBytes(byte[] bytes, Type type)
		{
			if (bytes == null) return null;
			NetDataContractSerializer serializer = GetByteSerializer(type);
			MemoryStream ms = new MemoryStream(bytes);
			object o;
			lock (serializer) { o = serializer.Deserialize(ms); }
			ms.Close();
			ms = null;
			return o;
		}

		/// <summary>
		/// Deserialize object from byte array.
		/// </summary>
		/// <param name="bytes">Byte array representing object. </param>
		/// <returns>Object.</returns>
		public static T DeserializeFromBytes<T>(byte[] bytes)
		{
			return (T)DeserializeFromBytes(bytes, typeof(T));
		}

		#endregion

		#region JSON

		static object JsonSerializersLock = new object();
		static Dictionary<Type, DataContractJsonSerializer> JsonSerializers;
		static DataContractJsonSerializer GetJsonSerializer(Type type)
		{
			lock (JsonSerializersLock)
			{
				if (JsonSerializers == null) JsonSerializers = new Dictionary<Type, DataContractJsonSerializer>();
				if (!JsonSerializers.ContainsKey(type))
				{
					JsonSerializers.Add(type, new DataContractJsonSerializer(type));
				}
			}
			return JsonSerializers[type];
		}

		/// <summary>
		/// Serialize object to JSON string.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <returns>JSON string.</returns>
		public static string SerializeToJson(object o)
		{
			return SerializeToJson(o, Encoding.UTF8);
		}

		/// <summary>
		/// Serialize object to JSON string.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>JSON string.</returns>
		public static string SerializeToJson(object o, Encoding encoding)
		{
			if (o == null) return null;
			DataContractJsonSerializer serializer = GetJsonSerializer(o.GetType());
			MemoryStream ms = new MemoryStream();
			lock (serializer) { serializer.WriteObject(ms, o); }
			string json = encoding.GetString(ms.ToArray());
			ms.Close();
			ms = null;
			return json;
		}

		/// <summary>
		/// Deserialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <returns>The deserialized object.</returns>
		public static object DeserializeFromJson(string json, Type type)
		{
			return DeserializeFromJson(json, type, Encoding.UTF8);
		}

		/// <summary>
		/// Deserialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>The deserialized object.</returns>
		public static object DeserializeFromJson(string json, Type type, Encoding encoding)
		{
			if (json == null) return null;
			DataContractJsonSerializer serializer = GetJsonSerializer(type);
			MemoryStream ms = new MemoryStream(encoding.GetBytes(json));
			object o;
			lock (serializer) { o = serializer.ReadObject(ms); }
			ms.Close();
			ms = null;
			return o;
		}

		/// <summary>
		/// Deserialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <returns>The deserialized object.</returns>
		public T DeserializeFromJson<T>(string json)
		{
			return (T)DeserializeFromJson(json, typeof(T), Encoding.UTF8);
		}

		/// <summary>
		/// Deserialize object from JSON string.
		/// </summary>
		/// <param name="json">JSON string representing object.</param>
		/// <param name="encoding">JSON string encoding.</param>
		/// <returns>The deserialized object.</returns>
		public T DeserializeFromJson<T>(string json, Encoding encoding)
		{
			return (T)DeserializeFromJson(json, typeof(T), encoding);
		}

		#endregion

		#region XML

		/// <summary>
		/// Reformat XML document.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static string XmlFormat(string xml)
		{
			XmlDocument xd = new XmlDocument();
			xd.XmlResolver = null;
			xd.LoadXml(xml);
			StringBuilder sb = new StringBuilder();
			XmlWriterSettings xws = new XmlWriterSettings();
			xws.Indent = true;
			xws.CheckCharacters = true;
			XmlWriter xw = XmlTextWriter.Create(sb, xws);
			xd.WriteTo(xw);
			xw.Close();
			return sb.ToString();
		}

		static object XmlSerializersLock = new object();
		static Dictionary<Type, XmlSerializer> XmlSerializers;
		static XmlSerializer GetXmlSerializer(Type type)
		{
			lock (XmlSerializersLock)
			{
				if (XmlSerializers == null) XmlSerializers = new Dictionary<Type, XmlSerializer>();
				if (!XmlSerializers.ContainsKey(type))
				{
					Type[] extraTypes = new Type[] { typeof(string) };
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
		/// <returns>Xml document</returns>
		public static XmlDocument SerializeToXml(object o)
		{
			if (o == null) return null;
			XmlSerializer serializer = GetXmlSerializer(o.GetType());
			MemoryStream ms = new MemoryStream();
			lock (serializer) { serializer.Serialize(ms, o); }
			ms.Seek(0, SeekOrigin.Begin);
			XmlDocument doc = new XmlDocument();
			doc.Load(ms);
			ms.Close();
			ms = null;
			return doc;
		}

		static T SeriallizeToXml<T>(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null)
		{
			if (o == null) return default(T);
			// Create serialization settings.
			encoding = encoding ?? Encoding.UTF8;
			var settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = omitXmlDeclaration;
			settings.Encoding = encoding;
			settings.Indent = true;
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
		public static string SerializeToXmlString(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null)
		{
			return SeriallizeToXml<string>(o, encoding, omitXmlDeclaration, comment);
		}

		/// <summary>
		/// Serialize object to XML file.
		/// </summary>
		/// <param name="o">The object to serialize.</param>
		/// <param name="path">The file name to write to.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		public static void SerializeToXmlFile(object o, string path, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null, int attempts = 2, int waitTime = 500)
		{
			var bytes = (o == null)
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
		public static byte[] SerializeToXmlBytes(object o, Encoding encoding = null, bool omitXmlDeclaration = false, string comment = null, int attempts = 2, int waitTime = 500)
		{
			var bytes = (o == null)
				? new byte[0]
				: SeriallizeToXml<byte[]>(o, encoding, omitXmlDeclaration, comment);
			return bytes;
		}

		#endregion

		#region XML: Deserialize

		/// <summary>
		/// Deserialize System.Collections.Generic.List to XML document.
		/// </summary>
		/// <param name="doc">Xml document representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <returns>Xml document</returns>
		public static object DeserializeFromXml(XmlDocument doc, Type type)
		{
			if (doc == null)
				return null;
			return DeserializeFromXmlString(doc.OuterXml, type);
		}

		/// <summary>
		/// Deserialize object from XML bytes. XML bytes can contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">Xml string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">Encoding to use (default is UTF8) if Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlBytes(byte[] bytes, Type type, Encoding encoding = null)
		{
			using (var ms = new MemoryStream(bytes))
			{
				// Use stream reader (inherits from TextReader) to avoid encoding errors.
				// Use specified encoding if Byte Order Mark (BOM) is missing.
				using (var sr = new StreamReader(ms, encoding ?? Encoding.UTF8, true))
				{
					// Settings used to protect from
					// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
					// https://cwe.mitre.org/data/definitions/611.html
					var settings = new XmlReaderSettings();
					settings.DtdProcessing = DtdProcessing.Ignore;
					settings.XmlResolver = null;
					using (var reader = XmlReader.Create(sr, settings))
					{
						object o;
						var serializer = GetXmlSerializer(type);
						lock (serializer) { o = serializer.Deserialize(reader); }
						return o;
					}
				}
			}
		}

		/// <summary>
		/// Deserialize object from XML string. XML string must not contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">Xml string representing object.</param>
		/// <param name="type">Type of object.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlString(string xml, Type type)
		{
			// Note: If you are getting deserialization error in XML document(1,1) then there is a chance that
			// you are trying to deserialize string which contains Byte Order Mark (BOM) which must not be there.
			// Probably you used "var xml = System.Text.Encoding.GetString(bytes)" directly on file content.
			// You should use "StreamReader" on file content, because this method will strip BOM properly
			// when converting bytes to string.
			using (var sr = new StringReader(xml))
			{
				// Settings used to protect from
				// CWE-611: Improper Restriction of XML External Entity Reference('XXE')
				// https://cwe.mitre.org/data/definitions/611.html
				var settings = new XmlReaderSettings();
				settings.DtdProcessing = DtdProcessing.Ignore;
				settings.XmlResolver = null;
				using (var reader = XmlReader.Create(sr, settings))
				{
					object o;
					var serializer = GetXmlSerializer(type);
					lock (serializer) { o = serializer.Deserialize(reader); }
					return o;
				}
			}
		}

		/// <summary>
		/// Deserialize object from XML file.
		/// </summary>
		/// <param name="filename">The file name to read from.</param>
		/// <param name="type">Type of object.</param>
		/// <param name="encoding">Encoding to use (default is UTF8) if file Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static object DeserializeFromXmlFile(string filename, Type type, Encoding encoding = null, int attempts = 1, int waitTime = 500)
		{
			// Read full file content first, so file will be locked for shorter period of time.
			var bytes = ReadFile(filename, attempts, waitTime);
			if (bytes == null || bytes.Length == 0)
				return null;
			return DeserializeFromXmlBytes(bytes, type, encoding);
		}

		/// <summary>
		/// Deserialize object from XML Document.
		/// </summary>
		/// <param name="doc">Xml document representing object.</param>
		/// <returns>Xml document</returns>
		public static T DeserializeFromXml<T>(XmlDocument doc)
		{
			return (T)DeserializeFromXml(doc, typeof(T));
		}

		/// <summary>
		/// Deserialize object from XML string.
		/// </summary>
		/// <param name="xml">Xml string representing object.</param>
		/// <param name="encoding">The encoding to use (default is UTF8).</param>
		/// <returns>Object.</returns>
		public static T DeserializeFromXmlString<T>(string xml)
		{
			return (T)DeserializeFromXmlString(xml, typeof(T));
		}

		/// <summary>
		/// Deserialize object from XML file.
		/// </summary>
		/// <param name="filename">The file name to read from.</param>
		/// <param name="encoding">Specified encoding will be used if file Byte Order Mark (BOM) is missing.</param>
		/// <returns>Object.</returns>
		public static T DeserializeFromXmlFile<T>(string filename, Encoding encoding = null, int attempts = 1, int waitTime = 500)
		{
			return (T)DeserializeFromXmlFile(filename, typeof(T), encoding, attempts, waitTime);
		}

		/// <summary>
		/// Deserialize object from XML bytes. XML bytes can contain Byte Order Mark (BOM).
		/// </summary>
		/// <param name="xml">Xml string representing object.</param>
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
			if (o == null)
			{
				WriteFile(path, new byte[0], attempts, waitTime);
				return;
			}
			encoding = encoding ?? Encoding.UTF8;
			// Create serialization settings.
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = omitXmlDeclaration;
			settings.Encoding = encoding;
			settings.Indent = true;
			// Serialize in memory first, so file will be locked for shorter times.
			MemoryStream ms = new MemoryStream();
			XmlWriter xw = XmlWriter.Create(ms, settings);
			XmlSerializer serializer = GetXmlSerializer(o.GetType());
			try
			{
				XsdDataContractExporter exporter = new XsdDataContractExporter();
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
				xw = null;
				ms = null;
				throw;
			}
			xw.Flush();
			byte[] bytes = ms.ToArray();
			xw.Close();
			// CA2202: Do not dispose objects multiple times
			//ms.Close();
			xw = null;
			ms = null;
			// Write serialized data into file.
			WriteFile(path, bytes, attempts, waitTime);
		}

		#endregion

	}

}
