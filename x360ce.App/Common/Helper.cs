using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Drawing;
using SharpDX.DirectInput;
using System.Text.RegularExpressions;
//using x360ce.App.x360ce.Engine.Data;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Win32;
using x360ce.App.Win32;

namespace x360ce.App
{
    public class Helper
    {
        #region Manipulate XInput DLL

        // Will be set to default values.
        public const string dllFile0 = "xinput9_1_0.dll";
        public const string dllFile1 = "xinput1_1.dll";
        public const string dllFile2 = "xinput1_2.dll";
        public const string dllFile3 = "xinput1_3.dll";
        public const string dllFile4 = "xinput1_4.dll";

        public const string dllFile3ce32 = "Resources.xinput1_3_32.dll";
        public const string dllFile3ce64 = "Resources.xinput1_3_64.dll";

        public static FileInfo[] GetDllInfos()
        {
            var files = new string[] { dllFile0, dllFile1, dllFile2, dllFile3, dllFile4 };
            var infos = files.Select(x => new System.IO.FileInfo(x)).ToArray();
            return infos;
        }

        public static FileInfo GetDefaultDll()
        {
            var files = GetDllInfos();
            // Make sure new version is on top.
            Array.Reverse(files);
            return files.FirstOrDefault(x => x.Exists);
        }

        public static Guid GetFileChecksum(string fileName)
        {
            var file = new FileStream(fileName, FileMode.Open);
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
            return new Guid(retVal);
        }

        public static x360ce.Engine.Data.Program[] GetLocalFiles(string path = ".")
        {
            var programs = new List<x360ce.Engine.Data.Program>();
            //var files = new List<System.Diagnostics.FileVersionInfo>();
            var fullNames = Directory.GetFiles(path, "*.exe");
            var list = new List<FileInfo>();
            foreach (var fullName in fullNames)
            {
                // Don't add x360ce App.
                if (fullName.EndsWith("\\x360ce.exe")) continue;
                var program = x360ce.Engine.Data.Program.FromDisk(fullName);
                if (program != null) programs.Add(program);
            }
            return programs.ToArray();
        }

        /// <summary></summary>
        /// <returns>True if file exists.</returns>
        public static bool CreateDllFile(bool create, string file)
        {
            if (create)
            {
                // If file don't exist exists then...
                var present = GetDefaultDll();
                if (present == null)
                {
                    MainForm.Current.CreateFile(GetXInputResoureceName(), Helper.dllFile3);
                }
                else if (!System.IO.File.Exists(file))
                {
                    present.CopyTo(file, true);
                }
            }
            else
            {
                if (System.IO.File.Exists(file))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch (Exception) { }
                }
            }
            return System.IO.File.Exists(file);
        }

        public static string GetXInputResoureceName()
        {
            // There must be an easier way to check embedded non managed DLL version.
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = dllFile3ce32;
            if (assembly.GetName().ProcessorArchitecture == ProcessorArchitecture.Amd64) resourceName = dllFile3ce64;
            return typeof(MainForm).Namespace + "." + resourceName;
        }

        public static Stream GetResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var key in assembly.GetManifestResourceNames())
            {
                if (key.Contains(name)) return assembly.GetManifestResourceStream(key);
            }
            return null;
        }

        public static Version _embededVersion;

        public static Version GetEmbeddedDllVersion()
        {
            if (_embededVersion != null) return _embededVersion;
            string tempPath = Path.GetTempPath();
            FileStream sw = null;
            var tempFile = Path.Combine(Path.GetTempPath(), "xinput.tmp.dll");
            sw = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
            var buffer = new byte[1024];
            var assembly = Assembly.GetExecutingAssembly();
            var sr = assembly.GetManifestResourceStream(GetXInputResoureceName());
            while (true)
            {
                var count = sr.Read(buffer, 0, buffer.Length);
                if (count == 0) break;
                sw.Write(buffer, 0, count);
            }
            sr.Close();
            sw.Close();
            var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(tempFile);
            var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            System.IO.File.Delete(tempFile);
            _embededVersion = v;
            return v;
        }

        #endregion

        #region Colors

        /// <summary>
        /// Make bitmap gray scale
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static void GrayScale(Bitmap b)
        {
            int w = b.Width;
            int h = b.Height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color p = b.GetPixel(x, y);
                    byte c = (byte)(.299 * p.R + .587 * p.G + .114 * p.B);
                    b.SetPixel(x, y, Color.FromArgb(p.A, c, c, c));
                }
            }
        }

        /// <summary>
        /// Make bitmap gray scale
        /// </summary>
        /// <param name="b"></param>
        /// <param name="alpha">256 max</param>
        /// <returns></returns>
        public static void Transparent(Bitmap b, int alpha)
        {
            int w = b.Width;
            int h = b.Height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color p = b.GetPixel(x, y);
                    int a = (int)((float)p.A * (float)alpha / byte.MaxValue);
                    if (a >= byte.MaxValue) a = byte.MaxValue;
                    if (a <= byte.MinValue) a = byte.MinValue;
                    b.SetPixel(x, y, Color.FromArgb(a, p.R, p.G, p.B));
                }
            }
        }

        #endregion

        // Use special function or comparison fails.
        public static bool IsSameDevice(Device device, Guid instanceGuid)
        {
            return instanceGuid.Equals(device == null ? Guid.Empty : device.Information.InstanceGuid);
        }

        public static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        /// <summary>Enable double buffering to make redraw faster.</summary>
        public static void EnableDoubleBuffering(DataGridView grid)
        {
            typeof(DataGridView).InvokeMember("DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, grid, new object[] { true });
        }


        #region Comparisons

        static Regex _GuidRegex;
        public static Regex GuidRegex
        {
            get
            {
                if (_GuidRegex == null)
                {
                    _GuidRegex = new Regex(
                "^[A-Fa-f0-9]{32}$|" +
                "^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
                "^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$");
                }
                return _GuidRegex;
            }

        }

        public static bool IsGuid(string s)
        {
            return string.IsNullOrEmpty(s)
                ? false
                : GuidRegex.IsMatch(s);
        }

        /// <summary>
        /// Gets a value that determines what the friendly name of the file is.
        /// </summary>
        /// <param name="fileExtension">File extension.</param>
        public static string GetFileDescription(string fileExtension)
        {
            var progId = GetProgId(fileExtension);
            if (string.IsNullOrEmpty(progId)) return string.Empty;
            var key = Registry.ClassesRoot;
            key = key.OpenSubKey(progId);
            if (key == null) return null;
            var val = key.GetValue("", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (val == null) return string.Empty;
            return val.ToString();
        }

        /// <summary>
        /// Gets a value that indicates the name of the associated application with the behavior to handle this extension.
        /// </summary>
        /// <param name="fileExtension">File extension.</param>
        public static string GetProgId(string fileExtension)
        {
            var key = Registry.ClassesRoot;
            key = key.OpenSubKey(fileExtension);
            if (key == null) return null;
            var val = key.GetValue("", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (val == null) return string.Empty;
            return val.ToString();
        }

        #endregion

        #region Serialize Methods

        static Dictionary<Type, XmlSerializer> _XmlSerializers;
        static Dictionary<Type, XmlSerializer> XmlSerializers
        {
            get
            {
                return _XmlSerializers = _XmlSerializers ?? new Dictionary<Type, XmlSerializer>();
            }
            set { _XmlSerializers = value; }
        }

        static XmlSerializer GetXmlSerializer(Type type)
        {
            lock (XmlSerializers)
            {
                if (!XmlSerializers.ContainsKey(type))
                {
                    XmlSerializers.Add(type, new XmlSerializer(type, new Type[] { typeof(string) }));
                }
            }
            return XmlSerializers[type];
        }

        /// <summary>
        /// Serialize object to XML file.
        /// </summary>
        /// <param name="o">The object to serialize.</param>
        /// <param name="filename">The file name to write to.</param>
        /// <param name="encoding">The encoding to generate.</param>
        public static void SerializeToXmlFile(object o, string filename, Encoding encoding)
        {
            var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            var xw = new XmlTextWriter(fs, encoding);
            xw.Formatting = System.Xml.Formatting.Indented;
            XmlSerializer serializer = GetXmlSerializer(o.GetType());
            lock (serializer)
            {
                try { serializer.Serialize(xw, o); }
                catch (Exception) { fs.Close(); throw; }
                xw.Flush();
                fs.Close();
            }
        }

        /// <summary>
        /// Deserialize object from XML file.
        /// </summary>
        /// <param name="filename">The file name to read from.</param>
        /// <returns>Object.</returns>
        public static object DeserializeFromXmlFile(string filename, Type type)
        {
            // FileStream allows to deserialize without locking the file. FileShare.ReadWrite is the key.
            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XmlSerializer serializer = GetXmlSerializer(type);
            object o = null;
            lock (serializer)
            {
                try { o = serializer.Deserialize(fs); }
                catch (Exception)
                {
                    fs.Close();
                    // copy file.
                    try
                    {
                        System.IO.File.Copy(filename, filename + "." + DateTime.Now.ToString("yyyyMMdd_hhmmss.ffffff"));
                        System.IO.File.Delete(filename);
                    }
                    catch (Exception) { }
                }
                return o;
            }
        }

        #endregion

        #region Application Info

        public static string GetProductFullName()
        {
            Version v = new Version(Application.ProductVersion);
            var s = string.Format("{0} {1} {2}", Application.CompanyName, Application.ProductName, v.ToString(3));
            // Version = major.minor.build.revision
            switch (v.Build)
            {
                case 0: s += " Alpha"; break;  // Alpha Release (AR)
                case 1: s += " Beta 1"; break; // Master Beta (MB)
                case 2: s += " Beta 2"; break; // Feature Complete (FC)
                case 3: s += " Beta 3"; break; // Technical Preview (TP)
                case 4: s += " RC"; break;     // Release Candidate (RC)
                case 5: s += " RTM"; break;    // Release to Manufacturing (RTM)
                default: break;                // General Availability (GA) - Gold
            }
            DateTime buildDate = GetBuildDateTime(Application.ExecutablePath);
            s += buildDate.ToString(" (yyyy-MM-dd)");
            switch (Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture) 
            {
                case ProcessorArchitecture.Amd64:
                case ProcessorArchitecture.IA64:
                    s += " 64-bit";
                    break;
                case ProcessorArchitecture.X86:
                    s += " 32-bit";
                    break;
                default: // Default is MSIL: Any CPU, show nothing/
                    break;
            }

            return s;
        }

        public static DateTime GetBuildDateTime(string filePath)
        {
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;
            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }
            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.ToLocalTime();
            return dt;
        }

        #endregion
    }
}
