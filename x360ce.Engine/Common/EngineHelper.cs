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
				if (string.IsNullOrEmpty(_AppDataPath))
				{
					// Apply default path.
					_AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\X360CE";
					var fi = new FileInfo(".\\x360ce\\x360ce.Options.xml");
					// If local configuration was found then use it.
					if (fi.Exists)
					{
						_AppDataPath = fi.Directory.FullName;
					}
					else
					{
						var args = Environment.GetCommandLineArgs();
						// Requires System.Configuration.Installl reference.
						var ic = new System.Configuration.Install.InstallContext(null, args);
						if (ic.Parameters.ContainsKey("Profile"))
						{
							var name = ic.Parameters["Profile"].Trim(' ', '"', '\'');
							if (string.IsNullOrEmpty(name))
							{
								// Name is invalid.
							}
							else
							{
								var path = Environment.ExpandEnvironmentVariables(name);
								// Get invalid path and file name chars.
								var ipc = Path.GetInvalidPathChars();
								var ifc = Path.GetInvalidFileNameChars();
								// If path is valid file name then...
								if (!name.ToCharArray().Any(x => ifc.Contains(x)))
								{
									// Use Profiles sub-folder.
									_AppDataPath += "\\Profiles\\" + name;
								}
								// If name is valid path then...
								else if (!name.ToCharArray().Any(x => ipc.Contains(x)))
								{
									var di = new DirectoryInfo(path);
									path = di.FullName;
									var winFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
									// If path is not inside windows folder then...
									if (!path.StartsWith(winFolder, StringComparison.OrdinalIgnoreCase))
									{
										_AppDataPath = path;
									}
								}
							}
						}
					}
				}
				return _AppDataPath;
			}
			set
			{
				_AppDataPath = value;
			}
		}
		static string _AppDataPath;

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
			var sp = !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem
				? Environment.SpecialFolder.SystemX86
				: Environment.SpecialFolder.System;
			var sysFolder = System.Environment.GetFolderPath(sp);
			var file = Directory.GetFiles(sysFolder, "xinput1_?.dll")
				.OrderByDescending(x => x).First();
			var info = new FileInfo(file);
			return info;
		}

		private static Assembly GetAssembly()
		{
			return Assembly.GetEntryAssembly();
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static Stream GetResourceStream(string name, Assembly assembly = null)
		{
			var path = GetResourcePath(name, assembly);
			if (path == null)
				return null;
			var asm = assembly ?? GetAssembly();
			if (asm == null)
				return null;
			var sr = asm.GetManifestResourceStream(path);
			return sr;
		}

		private static byte[] GetResourceBytes(string name, Assembly assembly = null)
		{
			var stream = GetResourceStream(name, assembly);
			if (stream == null)
				return null;
			var bytes = new byte[stream.Length];
			stream.Read(bytes, 0, bytes.Length);
			stream.Dispose();
			return bytes;
		}

		public static string GetResourceString(string name, Assembly assembly = null)
		{
			var stream = GetResourceStream(name, assembly);
			if (stream == null)
				return null;
			var sr = new StreamReader(stream);
			var s = sr.ReadToEnd();
			sr.Dispose();
			return s;
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static string GetResourcePath(string name, Assembly assembly = null)
		{
			var asm = assembly ?? GetAssembly();
			if (asm == null)
				return null;
			var names = asm.GetManifestResourceNames()
				.Where(x => x.EndsWith(name));
			var a = Environment.Is64BitProcess ? ".x64." : ".x86.";
			// Try to get by architecture first.
			var path = names.FirstOrDefault(x => x.Contains(a));
			if (!string.IsNullOrEmpty(path))
				return path;
			// Return first found.
			return names.FirstOrDefault();
		}

		public static Dictionary<ProcessorArchitecture, Version> _embededVersions;
		static object EmbededVersionsLock = new object();


		#endregion

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
					JocysCom.ClassLibrary.Controls.ControlsHelper.OpenPath(fixedPath);
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
			return ai.GetTitle(true, true, true, true, false, 4);
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

		public static Exception ExtractFile(string name, out FileInfo fi)
		{
			fi = null;
			try
			{
				// Extract file from Embedded resource.
				var chName = x360ce.Engine.EngineHelper.GetResourceChecksumFile(name);
				var fileName = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", chName);
				fi = new FileInfo(fileName);
				if (!fi.Exists)
				{
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					var sr = GetResourceStream(name);
					if (sr == null)
						return new Exception("Resource not found.");
					FileStream sw = null;
					sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
					var buffer = new byte[1024];
					while (true)
					{
						var count = sr.Read(buffer, 0, buffer.Length);
						if (count == 0)
							break;
						sw.Write(buffer, 0, count);
					}
					sr.Close();
					sw.Close();
				}
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return ex;
			}
			return null;
		}

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
			var hash = JocysCom.ClassLibrary.Security.CRC32Helper.GetHashAsString(bytes);
			// Put file into sub folder because file name must match with LoadLibrary() argument. 
			var newName = string.Format("{0}.{1:X8}\\{0}", name, hash);
			return newName;
		}

		static Guid UpdateChecksum(IChecksum item, System.Security.Cryptography.MD5CryptoServiceProvider md5)
		{
			string s = JocysCom.ClassLibrary.Runtime.RuntimeHelper.GetDataMembersString(item);
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
