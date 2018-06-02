using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Configuration
{
	public partial class AssemblyInfo
	{

		public AssemblyInfo()
		{
			_assembly =
				Assembly.GetEntryAssembly() ??
				FindEntryAssembly1() ??
				FindEntryAssembly2() ??
				Assembly.GetCallingAssembly() ??
				Assembly.GetExecutingAssembly();
		}

		public static AssemblyInfo _Entry;
		public static object _EntryLock = new object();
		public static AssemblyInfo Entry
		{
			get
			{
				lock (_EntryLock)
				{
					if (_Entry == null)
						_Entry = new AssemblyInfo();
					return _Entry;
				}
			}
		}

		public AssemblyInfo(string strValFile)
		{
			_assembly = Assembly.LoadFile(strValFile);
		}

		public AssemblyInfo(Assembly assembly)
		{
			_assembly = assembly;
		}

		#region Entry assembly

		/// <summary>
		/// Assembly.GetEntryAssembly() returns null in web applications. Mark assembly as the entry assembly
		/// by adding this attribute inside Properties\AssemblyInfo.cs file:
		/// [assembly: JocysCom.ClassLibrary.Configuration.AssemblyInfo.EntryAssembly]
		/// </summary>
		[AttributeUsage(AttributeTargets.Assembly)]
		public sealed class EntryAssemblyAttribute : Attribute { }

		// Method 1 better works on multiple assemblies marked as entry.
		Assembly FindEntryAssembly1()
		{
			var frames = new StackTrace().GetFrames();
			Array.Reverse(frames);
			foreach (var frame in frames)
			{
				var declaringType = frame.GetMethod().DeclaringType;
				var assembly = Assembly.GetAssembly(declaringType);
				var attribute = assembly.GetCustomAttributes(typeof(EntryAssemblyAttribute), false);
				if (attribute.Length > 0)
					return assembly;
			}
			return null;
		}


		// Find on current domain.
		Assembly FindEntryAssembly2()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var attribute = assembly.GetCustomAttributes(typeof(EntryAssemblyAttribute), false);
				if (attribute.Length > 0)
					return assembly;
			}
			return null;
		}

		#endregion

		public Assembly Assembly
		{
			get { return _assembly; }
			set { _assembly = value; }
		}
		private Assembly _assembly;

		DateTime? _BuildDateTime;
		object BuildDateTimeLock = new object();

		public DateTime BuildDateTime
		{
			get
			{
				lock (BuildDateTimeLock)
				{
					if (!_BuildDateTime.HasValue)
					{
						//_BuildDateTime = GetBuildDateTime(AssemblyPath);
						_BuildDateTime = GetBuildDateTime(_assembly.Location);
					}
					return _BuildDateTime.Value;
				}
			}
		}

		object FullTitleLock = new object();
		string _FullTitle;
		public string FullTitle
		{
			get
			{
				lock (FullTitleLock)
				{
					if (string.IsNullOrEmpty(_FullTitle))
					{
						_FullTitle = GetTitle();
					}
					return _FullTitle;
				}
			}
		}

		public string GetTitle(bool showBuild = true, bool showRunMode = true, bool showBuildDate = true, bool showArchitecture = true, bool showDescription = true, int versionNumbers = 3)
		{
			var s = string.Format("{0} {1} {2}", Company, Product, Version.ToString(versionNumbers));
			if (showBuild)
			{
				// Version = major.minor.build.revision
				switch (Version.Build)
				{
					case 0: s += " Alpha"; break;  // Alpha Release (AR)
					case 1: s += " Beta 1"; break; // Master Beta (MB)
					case 2: s += " Beta 2"; break; // Feature Complete (FC)
					case 3: s += " Beta 3"; break; // Technical Preview (TP)
					case 4: s += " RC"; break;     // Release Candidate (RC)
					case 5: s += " RTM"; break; // Release to Manufacturing (RTM)
					default: break;// General Availability (GA) - Gold
				}
			}
			var runMode = ConfigurationManager.AppSettings["RunMode"];
			var haveRunMode = !string.IsNullOrEmpty(runMode);
			// If run mode is not specified then assume live.
			var nonLive = haveRunMode && string.Compare(runMode, "LIVE", true) != 0;
			if (showBuildDate || (showRunMode && nonLive))
			{
				s += " (";
				if (showRunMode && nonLive)
				{
					s += string.Format("{0}", runMode);
					if (showBuildDate) s += " ";
				}
				if (showBuildDate)
				{
					s += string.Format("Build: {0:yyyy-MM-dd}", BuildDateTime);
				}
				s += ")";
			}
			if (showArchitecture)
			{
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
			}
			if (showDescription && !string.IsNullOrEmpty(Description) && !s.Contains(Description))
			{
				s += " - " + Description;
			}
			// Add elevated tag.
			var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var isElevated = identity.Owner != identity.User;
			// Add running user.
			string windowsDomain = GetWindowsDomainName();
			string windowsUser = GetWindowsUserName();
			string processDomain = Environment.UserDomainName;
			string processUser = Environment.UserName;
			if (string.Compare(windowsDomain, processDomain, true) != 0 || string.Compare(windowsUser, processUser, true) != 0)
				s += string.Format(" ({0}\\{1})", processDomain, processUser);
			else if (isElevated)
				s += " (Administrator)";
			// if (WinAPI.IsVista && WinAPI.IsElevated() && WinAPI.IsInAdministratorRole) this.Text += " (Administrator)";
			return s.Trim();
		}

		internal partial class NativeMethods
		{
			[DllImport("wtsapi32.dll")]
			internal static extern bool WTSQuerySessionInformationW(
				IntPtr hServer,
				int SessionId,
				int WTSInfoClass,
				out IntPtr ppBuffer,
				out IntPtr pBytesReturned
			);

		}

		public string GetWindowsDomainName() { return GetInformation(7); }

		public string GetWindowsUserName() { return GetInformation(5); }

		string GetInformation(int WTSInfoClass)
		{
			// Use current context.
			var WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
			var p = System.Diagnostics.Process.GetCurrentProcess();
			IntPtr AnswerBytes;
			IntPtr AnswerCount;
			// Get domain name.
			var success = NativeMethods.WTSQuerySessionInformationW(
				WTS_CURRENT_SERVER_HANDLE,
				p.SessionId,
				WTSInfoClass,
				out AnswerBytes,
				out AnswerCount
			);
			return Marshal.PtrToStringUni(AnswerBytes);
		}

		public static DateTime GetBuildDateTime(string filePath)
		{
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;
			byte[] b = new byte[2048];
			Stream s = null;
			try
			{
				s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
				s.Read(b, 0, 2048);
			}
			finally
			{
				if (s != null)
				{
					s.Close();
				}
			}
			int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
			int secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			dt = dt.AddSeconds(secondsSince1970);
			dt = dt.ToLocalTime();
			return dt;
		}

		public string AssemblyPath
		{
			get
			{
				string codeBase = _assembly.CodeBase;
				UriBuilder uri = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uri.Path);
				return path;
			}
		}

		public string AssemblyFullName { get { return _assembly.GetName().FullName.ToString(); } }
		public string AssemblyName { get { return _assembly.GetName().Name.ToString(); } }
		public string CodeBase { get { return _assembly.CodeBase; } }

		public string Company { get { return GetAttribute<AssemblyCompanyAttribute>(a => a.Company); } }
		public string Product { get { return GetAttribute<AssemblyProductAttribute>(a => a.Product); } }
		public string Copyright { get { return GetAttribute<AssemblyCopyrightAttribute>(a => a.Copyright); } }
		public string Trademark { get { return GetAttribute<AssemblyTrademarkAttribute>(a => a.Trademark); } }
		public string Title { get { return GetAttribute<AssemblyTitleAttribute>(a => a.Title); } }
		public string Description { get { return GetAttribute<AssemblyDescriptionAttribute>(a => a.Description); } }
		public string Configuration { get { return GetAttribute<AssemblyConfigurationAttribute>(a => a.Configuration); } }
		public string FileVersion { get { return GetAttribute<AssemblyFileVersionAttribute>(a => a.Version); } }
		public string ProductGuid { get { return GetAttribute<GuidAttribute>(a => a.Value); } }

		public Version Version { get { return _assembly.GetName().Version; } }

		string GetAttribute<T>(Func<T, string> value) where T : Attribute
		{
			T attribute = (T)Attribute.GetCustomAttribute(_assembly, typeof(T));
			return value.Invoke(attribute);
		}

		public FileInfo GetAppDataFile(bool userLevel, string format, params object[] args)
		{
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var folder = string.Format("{0}\\{1}\\{2}",
				Environment.GetFolderPath(specialFolder),
				Company,
				Product);
			var file = string.Format(format, args);
			var path = Path.Combine(folder, file);
			return new FileInfo(path);
		}

	}
}
