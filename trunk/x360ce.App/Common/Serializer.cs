using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace x360ce.App
{
    public class Serializer
    {
        #region Helper Functions

        /// <summary>
        /// Read file content in multiple attmempts.
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
                    // ReadAllBytes will lock file for writing, but leave open for other apps to read.
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
        /// Write file content in multiple attmempts.
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

        #region XML

        /// <summary>
        /// Reformat XML document.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static string XmlFormat(string xml)
        {
            XmlDocument xd = new XmlDocument();
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

        /// <summary>
        /// Serialize object to XML string.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <returns>XML string.</returns>
        public static string SerializeToXmlString(object o)
        {
            return SerializeToXmlString(o, Encoding.UTF8);
        }

        /// <summary>
        /// Serialize object to XML string.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <param name="encoding">The encoding to generate.</param>
        /// <param name="namespaces">Contains the XML namespaces and prefixes that the XmlSerializer  uses to generate qualified names in an XML-document instance.</param>
        /// <returns>XML string.</returns>
        public static string SerializeToXmlString(object o, Encoding encoding, bool omitXmlDeclaration = false)
        {
            if (o == null) return null;
            // Create serialization settings.
            encoding = encoding ?? Encoding.UTF8;
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Encoding = encoding;
            settings.Indent = true;
            // Serialize.
            XmlSerializer serializer = GetXmlSerializer(o.GetType());
            MemoryStream ms = new MemoryStream();
            XmlWriter xw = XmlTextWriter.Create(ms, settings);
            lock (serializer) { serializer.Serialize(xw, o); }
            xw.Flush();
            StreamReader tr = new StreamReader(ms);
            ms.Seek(0, SeekOrigin.Begin);
            string xml = tr.ReadToEnd();
            xw.Close();
            xw = null;
            return xml;
        }

        /// <summary>
        /// Serialize object to XML file.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <param name="filename">The file name to write to.</param>
        public static void SerializeToXmlFile(object o, string filename)
        {
            SerializeToXmlFile(o, filename, Encoding.UTF8);
        }

        /// <summary>
        /// Serialize object to XML file.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <param name="path">The file name to write to.</param>
        /// <param name="encoding">The encoding to generate.</param>
        public static void SerializeToXmlFile(object o, string path, Encoding encoding, bool omitXmlDeclaration = false, int attempts = 2, int waitTime = 500)
        {
            if (o == null)
            {
                WriteFile(path, new byte[0], attempts, waitTime);
                return;
            }
            // Create serialization settings.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = omitXmlDeclaration;
            settings.Encoding = encoding;
            settings.Indent = true;
            // Serialize in memory first, so file will be locked for shorter times.
            MemoryStream ms = new MemoryStream();
            XmlWriter xw = XmlTextWriter.Create(ms, settings);
            XmlSerializer serializer = GetXmlSerializer(o.GetType());
            lock (serializer) { serializer.Serialize(xw, o); }
            xw.Flush();
            byte[] bytes = ms.ToArray();
            xw.Close();
            xw = null;
            // Write serialized data into file.
            Serializer.WriteFile(path, bytes, attempts, waitTime);
        }

        /// <summary>
        /// Deserialize System.Collections.Generic.List to XML document.
        /// </summary>
        /// <param name="doc">Xml document representing object.</param>
        /// <param name="type">Type of object.</param>
        /// <returns>Xml document</returns>
        public static object DeserializeFromXml(XmlDocument doc, Type type)
        {
            if (doc == null) return null;
            var ms = new MemoryStream();
            doc.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            XmlSerializer serializer = GetXmlSerializer(type);
            object o;
            lock (serializer) { o = serializer.Deserialize(ms); }
            ms.Close();
            ms = null;
            return o;
        }

        /// <summary>
        /// Deserialize object from XML string.
        /// </summary>
        /// <param name="xml">Xml string representing object.</param>
        /// <param name="type">Type of object.</param>
        /// <returns>Object.</returns>
        public static object DeserializeFromXmlString(string xml, Type type)
        {
            return DeserializeFromXmlString(xml, type, Encoding.UTF8);
        }

        /// <summary>
        /// Deserialize object from XML string.
        /// </summary>
        /// <param name="xml">Xml string representing object.</param>
        /// <param name="type">Type of object.</param>
        /// <returns>Object.</returns>
        public static object DeserializeFromXmlString(string xml, Type type, Encoding encoding)
        {
            MemoryStream ms = new MemoryStream(encoding.GetBytes(xml));
            // Use stream reader to avoid error: There is no Unicode byte order mark. Cannot switch to Unicode.
            StreamReader sr = new StreamReader(ms, encoding);
            XmlSerializer serializer = GetXmlSerializer(type);
            object o;
            lock (serializer) { o = serializer.Deserialize(sr); }
            sr.Close();
            sr = null;
            return o;
        }

        /// <summary>
        /// Deserialize object from XML file.
        /// </summary>
        /// <param name="filename">The file name to read from.</param>
        /// <returns>Object.</returns>
        public static object DeserializeFromXmlFile(string filename, Type type)
        {
            return DeserializeFromXmlFile(filename, type, Encoding.UTF8);
        }

        /// <summary>
        /// Deserialize object from XML file.
        /// </summary>
        /// <param name="filename">The file name to read from.</param>
        /// <returns>Object.</returns>
        public static object DeserializeFromXmlFile(string filename, Type type, Encoding encoding, int attempts = 1, int waitTime = 500)
        {
            // Read full file content first, so file will be locked for shorter times.
            var bytes = Serializer.ReadFile(filename, attempts, waitTime);
            if (bytes == null) return null;
            // Read bytes.
            var ms = new MemoryStream(bytes);
            // Use stream reader to avoid error: There is no Unicode byte order mark. Cannot switch to Unicode.
            var sr = new StreamReader(ms, encoding);
            // Deserialize
            object o;
            XmlSerializer serializer = GetXmlSerializer(type);
            lock (serializer) { o = serializer.Deserialize(sr); }
            sr.Dispose();
            sr = null;
            ms = null;
            return o;
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
        /// Deserialize object from XML file.
        /// </summary>
        /// <param name="filename">The file name to read from.</param>
        /// <returns>Object.</returns>
        public static T DeserializeFromXmlFile<T>(string filename)
        {
            return (T)DeserializeFromXmlFile(filename, typeof(T));
        }

        /// <summary>
        /// Deserialize object from XML string.
        /// </summary>
        /// <param name="xml">Xml string representing object.</param>
        /// <param name="type">Type of object.</param>
        /// <returns>Object.</returns>
        public static T DeserializeFromXmlString<T>(string xml)
        {
            return (T)DeserializeFromXmlString(xml, typeof(T));
        }

        /// <summary>
        /// Deserialize object from XML string.
        /// </summary>
        /// <param name="xml">Xml string representing object.</param>
        /// <param name="type">Type of object.</param>
        /// <returns>Object.</returns>
        public static T DeserializeFromXmlString<T>(string xml, Encoding encoding)
        {
            return (T)DeserializeFromXmlString(xml, typeof(T), encoding);
        }


        #endregion


    }
}
