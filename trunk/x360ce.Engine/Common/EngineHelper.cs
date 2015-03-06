using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Win32;
using x360ce.Engine;

namespace x360ce.Engine
{
	public class EngineHelper
	{
		#region Manipulate XInput DLL

		/// <summary>
		/// Get information about XInput located on the disk.
		/// </summary>
		/// <returns></returns>
		public static FileInfo GetDefaultDll()
		{
			// Get XInput values.
			var values = Enum.GetValues(typeof(XInputMask)).Cast<XInputMask>().Where(x => x != XInputMask.None);
			// Get unique file names.
			var fileNames = values.Select(x => JocysCom.ClassLibrary.ClassTools.EnumTools.GetDescription(x)).Distinct();
			// Get information about XInput files located on the disk.
			var infos = fileNames.Select(x => new System.IO.FileInfo(x)).Where(x=>x.Exists).ToArray();
			FileInfo defaultDll = null;
			Version defaultVer = null;
			foreach (var info in infos)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(info.FullName);
				var ver = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
				// if first time in the loop of file with newer version was found then...
				if (defaultDll == null || ver > defaultVer)
				{
					// Pick file.
					defaultDll = info;
					defaultVer = ver;
				}
			}
			// Return newest file.
			return defaultDll;
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

		public static string GetXInputResoureceName(ProcessorArchitecture architecture = ProcessorArchitecture.None)
		{
			var assembly = Assembly.GetEntryAssembly();
			if (architecture == ProcessorArchitecture.None)
			{
				architecture = assembly.GetName().ProcessorArchitecture;
			}
			// There must be an easier way to check embedded non managed DLL version.
			var paString = architecture == ProcessorArchitecture.Amd64
				? "x64" : "x86";
			var name = string.Format("Resources.xinput_{1}.dll", assembly.GetName().Name, paString);
			var names = assembly.GetManifestResourceNames();
			var resourceName = names.FirstOrDefault(x => x.EndsWith(name));
			return resourceName;
		}

		public static Stream GetResource(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			foreach (var key in assembly.GetManifestResourceNames())
			{
				if (key.Contains(name)) return assembly.GetManifestResourceStream(key);
			}
			return null;
		}

		public static Dictionary<ProcessorArchitecture, Version> _embededVersions;

		public static Version GetEmbeddedDllVersion(ProcessorArchitecture architecture)
		{
			if (_embededVersions == null)
			{
				_embededVersions = new Dictionary<ProcessorArchitecture, Version>();
				ProcessorArchitecture[] archs = { ProcessorArchitecture.X86, ProcessorArchitecture.Amd64 };
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
					var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(tempFile);
					var v = new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
					System.IO.File.Delete(tempFile);
					_embededVersions.Add(a, v);
				}
			}
			return _embededVersions[architecture];
		}

		public static Version GetDllVersion(string fileName, out bool byMicrosoft)
		{
			var dllInfo = new System.IO.FileInfo(fileName);
			byMicrosoft = false;
			if (dllInfo.Exists)
			{
				var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(dllInfo.FullName);
				byMicrosoft = !string.IsNullOrEmpty(vi.CompanyName) && vi.CompanyName.Contains("Microsoft");
				return new Version(vi.FileMajorPart, vi.FileMinorPart, vi.FileBuildPart, vi.FilePrivatePart);
			}
			return new Version(0, 0, 0, 0);
		}

		#endregion

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
			switch (Assembly.GetEntryAssembly().GetName().ProcessorArchitecture)
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
