using System;
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
			Assembly =
				Assembly.GetEntryAssembly() ??
				FindEntryAssembly1() ??
				FindEntryAssembly2() ??
				Assembly.GetCallingAssembly() ??
				Assembly.GetExecutingAssembly();
		}

		public static object _EntryLock = new object();
		public static AssemblyInfo Entry
		{
			get
			{
				lock (_EntryLock)
				{
					if (_Entry is null)
						_Entry = new AssemblyInfo();
					return _Entry;
				}
			}
			set
			{
				lock (_EntryLock)
				{
					_Entry = value;
				}
			}
		}
		static AssemblyInfo _Entry;

		public AssemblyInfo(string strValFile)
		{
			Assembly = Assembly.LoadFile(strValFile);
		}

		public AssemblyInfo(Assembly assembly)
		{
			Assembly = assembly;
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

		public Assembly Assembly { get; set; }

		DateTime? _BuildDateTime;
		object BuildDateTimeLock = new object();

		public DateTime BuildDateTime
		{
			get
			{
				lock (BuildDateTimeLock)
				{
					if (!_BuildDateTime.HasValue)
						_BuildDateTime = GetBuildDateTime(Assembly);
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
						_FullTitle = GetTitle();
					return _FullTitle;
				}
			}
		}

		public string RunMode
		{
			get
			{
				if (_RunMode is null)
					// TODO: Standardize configuration provider XML, JSON, INI, Registry, etc...
					// https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration-providers
					//_RunMode = SettingsParser.Current.Parse("RunMode", "");
					return "";
				return _RunMode;
			}
		}
		public string _RunMode;

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
					case 5: s += " RTM"; break;    // Release to Manufacturing (RTM)
					default: break;                // General Availability (GA) - Gold
				}
			}

			var haveRunMode = !string.IsNullOrEmpty(RunMode);
			// If run mode is not specified then assume live.
			var nonLive = haveRunMode && string.Compare(RunMode, "LIVE", true) != 0;
			if (showBuildDate || (showRunMode && nonLive))
			{
				s += " (";
				if (showRunMode && nonLive)
				{
					s += string.Format("{0}", RunMode);
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
				switch (RuntimeInformation.ProcessArchitecture)
				{
					case Architecture.X64:
					case Architecture.Arm64:
						s += " 64-bit";
						break;
					case Architecture.X86:
					case Architecture.Arm:
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

#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework

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
			// if (WinAPI.IsVista && WinAPI.IsElevated() && WinAPI.IsInAdministratorRole) Text += " (Administrator)";
#endif
			return s.Trim();
		}

#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework

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

		private static string GetInformation(int WTSInfoClass)
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

#endif

		/// <summary>
		/// Read build time from the file. This won't work with deterministic builds.
		/// </summary>
		/// <remarks>
		/// The C# compiler (Roslyn) supports deterministic builds since Visual Studio 2015.
		/// This means that compiling assemblies under the same conditions (permalink)
		/// would produce byte-for-byte equivalent binaries.
		/// </remarks>
		public static DateTime GetBuildDateTime(string filePath)
		{
			// Constants related to the Windows PE file format.
			const int PE_HEADER_OFFSET = 60; // 0x3C
			const int LINKER_TIMESTAMP_OFFSET = 8;
			// Read header from file
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
					s.Close();
			}
			// Read the linker TimeStamp
			var offset = BitConverter.ToInt32(b, PE_HEADER_OFFSET);
			var secondsSince1970 = BitConverter.ToInt32(b, offset + LINKER_TIMESTAMP_OFFSET);
			var dt = GetDateTime(secondsSince1970);
			return dt;
		}


		/// <summary>
		/// Read build time from the assembly. Workaround is required to work with deterministic builds.
		/// </summary>
		/// <remarks>
		/// You have two options:
		/// 
		/// Option 1: Disable Deterministic build by adding
		/// 
		///     &gt;Deterministic&lt;False&gt;/Deterministic&lt; inside a &gt;PropertyGroup&lt section  of .csproj
		///
		/// Option 2:
		/// 
		///     Create "Resources\BuildDate.txt" and set its "Build Action: Embedded Resource"
		///     Add to pre-build event to work with latest .NET builds:
		///     
		///     PowerShell.exe -Command "New-Item -ItemType Directory -Force -Path \"$(ProjectDir)Resources\" | Out-Null"
		///     PowerShell.exe -Command "(Get-Date).ToString(\"o\") | Out-File \"$(ProjectDir)Resources\BuildDate.txt\""
		///
		/// Note:
		/// The C# compiler (Roslyn) supports deterministic builds since Visual Studio 2015.
		/// This means that compiling assemblies under the same conditions (permalink)
		/// would produce byte-for-byte equivalent binaries.
		/// </remarks>
		public static DateTime GetBuildDateTime(Assembly assembly, TimeZoneInfo tzi = null)
		{
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));
			var names = assembly.GetManifestResourceNames();
			var dt = default(DateTime);
			foreach (var name in names)
			{
				if (!name.EndsWith("BuildDate.txt"))
					continue;
				var stream = assembly.GetManifestResourceStream(name);
				using (var reader = new StreamReader(stream))
				{
					var date = reader.ReadToEnd();
					dt = DateTime.Parse(date);
					dt = TimeZoneInfo.ConvertTime(dt, tzi ?? TimeZoneInfo.Local);
					return dt;
				}
			}
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
#else // .NET Framework

			// Constants related to the Windows PE file format.
			const int PE_HEADER_OFFSET = 60;
			const int LINKER_TIMESTAMP_OFFSET = 8;
			// Discover the base memory address where our assembly is loaded
			var entryModule = assembly.ManifestModule;
			var hMod = Marshal.GetHINSTANCE(entryModule);
			if (hMod == IntPtr.Zero - 1)
				throw new Exception("Failed to get HINSTANCE.");
			// Read the linker TimeStamp
			var offset = Marshal.ReadInt32(hMod, PE_HEADER_OFFSET);
			var secondsSince1970 = Marshal.ReadInt32(hMod, offset + LINKER_TIMESTAMP_OFFSET);
			dt = GetDateTime(secondsSince1970);
#endif
			return dt;
		}

		/// <summary>
		/// Convert the TimeStamp to a DateTime
		/// </summary>
		static DateTime GetDateTime(int secondsSince1970, TimeZoneInfo tzi = null)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var linkTimeUtc = epoch.AddSeconds(secondsSince1970);
			return TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tzi ?? TimeZoneInfo.Local);
		}

		public string AssemblyPath
		{
			get
			{
				var codeBase = Assembly.Location;
				if (string.IsNullOrEmpty(codeBase))
					return codeBase;
				var uri = new UriBuilder(codeBase);
				var path = Uri.UnescapeDataString(uri.Path);
				return path;
			}
		}

		public string AssemblyFullName { get { return Assembly.GetName().FullName.ToString(); } }
		public string AssemblyName { get { return Assembly.GetName().Name.ToString(); } }
		public string CodeBase { get { return Assembly.Location; } }

		public string Company { get { return GetAttribute<AssemblyCompanyAttribute>(a => a.Company); } }
		public string Product { get { return GetAttribute<AssemblyProductAttribute>(a => a.Product); } }
		public string Copyright { get { return GetAttribute<AssemblyCopyrightAttribute>(a => a.Copyright); } }
		public string Trademark { get { return GetAttribute<AssemblyTrademarkAttribute>(a => a.Trademark); } }
		public string Title { get { return GetAttribute<AssemblyTitleAttribute>(a => a.Title); } }
		public string Description { get { return GetAttribute<AssemblyDescriptionAttribute>(a => a.Description); } }
		public string Configuration { get { return GetAttribute<AssemblyConfigurationAttribute>(a => a.Configuration); } }
		public string FileVersion { get { return GetAttribute<AssemblyFileVersionAttribute>(a => a.Version); } }
		public string ProductGuid { get { return GetAttribute<GuidAttribute>(a => a.Value); } }

		public Version Version { get { return Assembly.GetName().Version; } }

		string GetAttribute<T>(Func<T, string> value) where T : Attribute
		{
			T attribute = (T)Attribute.GetCustomAttribute(Assembly, typeof(T));
			return attribute is null
				? ""
				: value.Invoke(attribute);
		}

		public string GetAppDataPath(bool userLevel = false, string format = "", params object[] args)
		{
			// Get writable application folder.
			var specialFolder = userLevel
				? Environment.SpecialFolder.ApplicationData
				: Environment.SpecialFolder.CommonApplicationData;
			var folder = string.Format("{0}\\{1}\\{2}",
				Environment.GetFolderPath(specialFolder),
				Company,
				Product);
			// Get file name.
			var file = string.Format(format, args);
			var path = Path.Combine(folder, file);
			return path;
		}

		public FileInfo GetAppDataFile(bool userLevel = false, string format = "", params object[] args)
		{
			var path = GetAppDataPath(userLevel, format, args);
			return new FileInfo(path);
		}

	}
}
