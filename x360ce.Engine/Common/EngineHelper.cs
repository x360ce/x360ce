using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.Serialization;
using JocysCom.ClassLibrary.Runtime;

namespace x360ce.Engine
{
    public partial class EngineHelper
    {
        #region Manipulate XInput DLL

        public static string AppDataPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\X360CE";
            }
        }

        /// <summary>
        /// Get information about XInput located on the disk.
        /// </summary>
        /// <returns></returns>
        public static FileInfo GetDefaultDll(bool useMicrosoft = false)
        {
            FileInfo defaultDll = null;
            if (!useMicrosoft)
            {
                // Get XInput values.
                var values = Enum.GetValues(typeof(XInputMask)).Cast<XInputMask>().Where(x => x != XInputMask.None);
                // Get unique file names.
                var fileNames = values.Select(x => Attributes.GetDescription(x)).Distinct();
                // Get information about XInput files located on the disk.
                var infos = fileNames.Select(x => new FileInfo(x)).Where(x => x.Exists).ToArray();
                Version defaultVer = null;
                foreach (var info in infos)
                {
                    var vi = FileVersionInfo.GetVersionInfo(info.FullName);
                    var ver = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
                    // if first time in the loop of file with newer version was found then...
                    if (defaultDll == null || ver > defaultVer)
                    {
                        // Pick file.
                        defaultDll = info;
                        defaultVer = ver;
                    }
                }
            }
            // If custom XInput DLL was not found then...
            if (defaultDll == null)
            {
                var info = GetMsXInputLocation();
                var vi = FileVersionInfo.GetVersionInfo(info.FullName);
                var ver = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
                defaultDll = info;
            }
            // Return newest file.
            return defaultDll;
        }

        /// <summary>
        /// Get path to Microsoft's XInput library.
        /// </summary>
        /// <returns></returns>
        public static FileInfo GetMsXInputLocation()
        {
            // If this is 32 bit process on 64-bit OS then
            var sp = !IsApp64bit() && Environment.Is64BitOperatingSystem
                ? Environment.SpecialFolder.SystemX86
                : Environment.SpecialFolder.System;
            var sysFolder = System.Environment.GetFolderPath(sp);
            var msx = System.IO.Path.Combine(sysFolder, "xinput1_3.dll");
            var info = new FileInfo(msx);
            return info;
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
                // Don't add x360ce Application.
                if (fullName.EndsWith("\\x360ce.exe")) continue;
                var program = x360ce.Engine.Data.Program.FromDisk(fullName);
                if (program != null) programs.Add(program);
            }
            return programs.ToArray();
        }

        public static string GetXInputResoureceName(ProcessorArchitecture architecture = ProcessorArchitecture.None)
        {
            return GetResourcePath("xinput.dll");
        }

        /// <summary>
        /// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
        /// </summary>
        public static Stream GetResourceStream(string name)
        {
            var path = GetResourcePath(name);
            if (path == null)
                return null;
            var assembly = Assembly.GetEntryAssembly();
            var sr = assembly.GetManifestResourceStream(path);
            return sr;
        }

        public static byte[] GetResourceBytes(string name)
        {
            var sr = GetResourceStream(name);
            if (sr == null)
                return null;
            byte[] bytes = new byte[sr.Length];
            sr.Read(bytes, 0, bytes.Length);
            sr.Dispose();
            return bytes;
        }

        public static bool IsApp64bit()
        {
            var assembly = Assembly.GetEntryAssembly();
            var architecture = assembly.GetName().ProcessorArchitecture;
            // There must be an easier way to check embedded non managed DLL version.
            return
                architecture == ProcessorArchitecture.Amd64 ||
                architecture == ProcessorArchitecture.IA64;
        }

        /// <summary>
        /// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
        /// </summary>
        public static string GetResourcePath(string name)
        {
            var assembly = Assembly.GetEntryAssembly();
            var names = assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(name));
            var a = IsApp64bit() ? ".x64." : ".x86.";
            // Try to get by architecture first.
            var path = names.FirstOrDefault(x => x.Contains(a));
            if (!string.IsNullOrEmpty(path))
                return path;
            // Return first found.
            return names.FirstOrDefault();
        }

        public static Dictionary<ProcessorArchitecture, Version> _embededVersions;
        static object EmbededVersionsLock = new object();

        public static Version GetEmbeddedDllVersion(ProcessorArchitecture architecture)
        {
            lock (EmbededVersionsLock)
            {
                if (_embededVersions == null)
                {
                    _embededVersions = new Dictionary<ProcessorArchitecture, Version>();
                    ProcessorArchitecture[] archs = { ProcessorArchitecture.X86, ProcessorArchitecture.Amd64, ProcessorArchitecture.MSIL };
                    foreach (var a in archs)
                    {
                        string tempPath = Path.GetTempPath();
                        FileStream sw = null;
                        var tempFile = Path.Combine(Path.GetTempPath(), "xinput_" + a.ToString() + ".tmp.dll");
                        sw = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
                        var buffer = new byte[1024];
                        var assembly = Assembly.GetEntryAssembly();
                        var resourceName = GetXInputResoureceName(architecture);
                        var sr = assembly.GetManifestResourceStream(resourceName);
                        while (true)
                        {
                            var count = sr.Read(buffer, 0, buffer.Length);
                            if (count == 0) break;
                            sw.Write(buffer, 0, count);
                        }
                        sr.Close();
                        sw.Close();
                        var vi = FileVersionInfo.GetVersionInfo(tempFile);
                        var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
                        File.Delete(tempFile);
                        _embededVersions.Add(a, v);
                    }
                }
            }
            return _embededVersions[architecture];
        }

        public static Version GetDllVersion(string fileName, out bool byMicrosoft)
        {
            var dllInfo = new FileInfo(fileName);
            byMicrosoft = false;
            if (dllInfo.Exists)
            {
                var vi = FileVersionInfo.GetVersionInfo(dllInfo.FullName);
                byMicrosoft = !string.IsNullOrEmpty(vi.CompanyName) && vi.CompanyName.Contains("Microsoft");
                return new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
            }
            return new Version(0, 0, 0, 0);
        }

        public static bool? IsCustomLibrarry(string fileName)
        {
            var fi = new FileInfo(fileName);
            if (!fi.Exists)
                return null;
            var vi = FileVersionInfo.GetVersionInfo(fi.FullName);
            if (string.IsNullOrEmpty(vi.InternalName))
                return false;
            return string.Compare(vi.InternalName, "X360CE", true) == 0;
        }

        #endregion

        public static void CopyProperties<T>(T source, T dest)
        {
            Type t = typeof(T);
            var pis = t.GetProperties().Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute))).ToArray();
            foreach (PropertyInfo pi in pis)
            {
                if (pi.CanWrite && pi.CanRead)
                {
                    pi.SetValue(dest, pi.GetValue(source, null), null);
                }
            }
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

        public static void BrowsePath(string path)
        {
            var exists = File.Exists(path);
            string fixedPath = path;
            if (!exists)
            {
                // Try to get parent folder.
                var dirInfo = new DirectoryInfo(path);
                var newInfo = dirInfo;
                // If root folder exist then continue...
                if (dirInfo.Root.Exists)
                {
                    // Go to parent if folder doesn't exist.
                    while (!exists)
                    {
                        if (newInfo.Exists)
                        {
                            fixedPath = newInfo.FullName;
                            exists = true;
                        }
                        else
                        {
                            newInfo = newInfo.Parent;
                        }
                    }
                }
            }
            if (exists)
            {
                var attributes = File.GetAttributes(fixedPath);
                var isDirectory = attributes.HasFlag(FileAttributes.Directory);
                if (isDirectory)
                {
                    OpenPath(fixedPath);
                }
                else
                {
                    string argument = @"/select, " + fixedPath;
                    Process.Start("explorer.exe", argument);
                }
            }
            else
            {
                MessageBox.Show("Path not found!", "Path not found!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Open file with associated program.
        /// </summary>
        /// <param name="path">file to open.</param>
        public static void OpenPath(string path, string arguments = null)
        {
            try
            {
                var fi = new FileInfo(path);
                // Brings up the "Windows cannot open this file" dialog if association not found.
                var psi = new ProcessStartInfo(path);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = fi.Directory.FullName;
                psi.ErrorDialog = true;
                if (arguments != null) psi.Arguments = arguments;
                Process.Start(psi);
            }
            catch (Exception) { }
        }

        public static string[] GetFiles(string path, string searchPattern, bool allDirectories = false)
        {
            var dir = new DirectoryInfo(path);
            var fis = new List<FileInfo>();
            GetFiles(dir, ref fis, searchPattern, false);
            return fis.Select(x => x.FullName).ToArray();
        }

        public static void GetFiles(DirectoryInfo di, ref List<FileInfo> fileList, string searchPattern, bool allDirectories)
        {
            try
            {
                if (allDirectories)
                {
                    foreach (DirectoryInfo subDi in di.GetDirectories())
                    {
                        GetFiles(subDi, ref fileList, searchPattern, allDirectories);
                    }
                }
            }
            catch { }
            try
            {
                // Add only different files.
                var files = di.GetFiles(searchPattern);
                for (int i = 0; i < files.Length; i++)
                {
                    var fullName = files[i].FullName;
                    if (!fileList.Any(x => x.FullName == fullName))
                    {
                        fileList.Add(files[i]);
                    }
                }
            }
            catch { }
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

        /// <summary>
        /// Remove multiple spaces and trim
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string FixName(string s, string defaultName)
        {
            s = (s ?? "").Replace("[ \t\n\r\u00A0]+", " ").Trim();
            return string.IsNullOrEmpty(s) ? defaultName : s;
        }

        #endregion

        #region Application Info

        public static string GetProductFullName()
        {
            var ai = new JocysCom.ClassLibrary.Configuration.AssemblyInfo();
            return ai.GetTitle(true, true, true, true, false);
        }

        public static string GetProcessorArchitectureDescription(ProcessorArchitecture architecture)
        {
            switch (architecture)
            {
                case ProcessorArchitecture.Amd64:
                case ProcessorArchitecture.IA64:
                    return "64-bit";
                case ProcessorArchitecture.MSIL:
                    return "Any CPU";
                case ProcessorArchitecture.X86:
                    return "32-bit";
                default:
                    return "Unknown";

            }
        }

        #endregion

        #region Compression

        public static byte[] EmptyGzip = { 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static byte[] Compress(byte[] bytes)
        {
            int numRead;
            var srcStream = new MemoryStream(bytes);
            var dstStream = new MemoryStream();
            srcStream.Position = 0;
            var stream = new GZipStream(dstStream, CompressionMode.Compress);
            byte[] buffer = new byte[0x1000];
            while (true)
            {
                numRead = srcStream.Read(buffer, 0, buffer.Length);
                if (numRead == 0) break;
                stream.Write(buffer, 0, numRead);
            }
            stream.Close();
            return dstStream.ToArray();
        }

        public static byte[] Decompress(byte[] bytes)
        {
            int numRead;
            var srcStream = new MemoryStream(bytes);
            var dstStream = new MemoryStream();
            srcStream.Position = 0;
            var stream = new GZipStream(srcStream, CompressionMode.Decompress);
            var buffer = new byte[0x1000];
            while (true)
            {
                numRead = stream.Read(buffer, 0, buffer.Length);
                if (numRead == 0) break;
                dstStream.Write(buffer, 0, numRead);
            }
            dstStream.Close();
            return dstStream.ToArray();
        }


        #endregion

        #region MD5

        static System.Security.Cryptography.MD5 HashProvider;
        static object HashProviderLock = new object();

        /// <summary>
        /// Computes the MD5 hash value for the specified text. Use UTF-8 encoding to get bytes.
        /// </summary>
        /// <param name="text">The input to compute the hash code for.</param>
        /// <returns>The computed hash code as GUID.</returns>
        public static Guid ComputeMd5Hash(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return ComputeMd5Hash(bytes);
        }

        /// <summary>
        /// Computes the MD5 hash value for the specified text.
        /// </summary>
        /// <param name="text">The input to compute the hash code for.</param>
        /// <param name="encoding">Encoding to get bytes.</param>
        /// <returns>The computed hash code as GUID.</returns>
        public static Guid ComputeMd5Hash(string text, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(text);
            return ComputeMd5Hash(bytes);
        }

        /// <summary>
        /// Computes the MD5 hash value for the specified byte array.
        /// </summary>
        /// <param name="bytes">The input to compute the hash code for.</param>
        /// <returns>The computed hash code as GUID.</returns>
        /// <remarks>
        /// One instance of the MD5 Crypto Service Provider
        /// can't operate properly with multiple simultaneous threads.
        /// Use lock to solve this problem.
        /// </remarks>
        public static Guid ComputeMd5Hash(byte[] bytes)
        {
            byte[] hash;
            lock (HashProviderLock)
            {
                HashProvider = HashProvider ?? new System.Security.Cryptography.MD5CryptoServiceProvider();
                hash = HashProvider.ComputeHash(bytes);
            }
            return new Guid(hash);
        }

        #endregion

        /// <summary>
        /// Get first 8 numbers of GUID.
        /// </summary>
        /// <remarks>Instance GUID or Setting GUID (MD5 checksum) is always random.</remarks>
        public static string GetID(Guid guid)
        {
            return guid.ToString("N").Substring(0, 8).ToUpper();
        }

        #region Get Key Name

        /// <summary>
        /// Remove diacritic marks (accent marks) from characters.
        /// éèàçùö =>eeacuo
        /// http://blogs.msdn.com/michkap/archive/2007/05/14/2629747.aspx
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveDiacriticMarks(string s)
        {
            string stFormD = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        static readonly Regex RxAllowedInKey = new Regex("[^A-Z0-9 ]+", RegexOptions.IgnoreCase);
        static readonly Regex RxMultiSpace = new Regex("[ \u00A0]+");
        static TextInfo Culture = new CultureInfo("en-US", false).TextInfo;
        static char[] BasicChars = new char[] { '-', ' ', ',', '\u00A0' };

        public static string GetKey(string input, bool capitalize, string separator = "_")
        {
            // Filter accents: Hélan => Helan
            string s = RemoveDiacriticMarks(input);
            // Convert to upper-case and keep only allowed chars.
            s = RxAllowedInKey.Replace(s, " ");
            // Replace multiple spaces.
            s = RxMultiSpace.Replace(s, " ").Trim();
            // Trim basic chars.
            s = s.Trim(BasicChars);
            if (capitalize)
            {
                s = Culture.ToTitleCase(s.ToLower());
            }
            s = s.Replace(" ", separator);
            return s;
        }

        #endregion

        /// <summary>
        /// Get file name inside the folder with CRC32 checksum prefix.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetResourceChecksumFile(string name)
        {
            var bytes = GetResourceBytes(name);
            if (bytes == null)
                return null;
            var hash = JocysCom.ClassLibrary.Security.CRC32.GetHashAsString(bytes);
            // Put file into sub folder because file name must match with LoadLibrary() argument. 
            var newName = string.Format("{0}.{1:X8}\\{0}", name, hash);
            return newName;
        }

        static Guid UpdateChecksum(IChecksum item, System.Security.Cryptography.MD5CryptoServiceProvider md5)
        {
            string s = JocysCom.ClassLibrary.Runtime.Helper.GetDataMembersString(item);
            var bytes = Encoding.Unicode.GetBytes(s);
            var cs = new Guid(md5.ComputeHash(bytes));
            if (item.Checksum != cs)
                item.Checksum = cs;
            return cs;
        }

        /// <summary>
        /// Update checksums of objects and return total checksum.
        /// </summary>
        /// <remarks>Last GUID will be summary checksum.</remarks>
        public static List<Guid> UpdateChecksums<T>(T[] list) where T : IChecksum
        {
            var result = new List<Guid>();
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            for (int i = 0; i < list.Length; i++)
            {
                var checksum = UpdateChecksum(list[i], md5);
                result.Add(checksum);
            }
            if (list.Length > 0)
            {   // Order to make sure that it won't influence total checksum.
                result = result.OrderBy(x => x).ToList();
                int size = 16;
                var total = new byte[list.Length * size];
                for (int i = 0; i < list.Length; i++)
                {
                    Array.Copy(list[i].Checksum.ToByteArray(), 0, total, i * size, size);
                }
                result.Add(new Guid(md5.ComputeHash(total)));
            }
            return result;
        }
    }
}
