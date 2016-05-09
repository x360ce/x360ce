using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Configuration
{
	public partial class AssemblyInfo
	{

		public AssemblyInfo()
		{
			_assembly = Assembly.GetEntryAssembly();
		}

		public AssemblyInfo(string strValFile)
		{
			_assembly = Assembly.LoadFile(strValFile);
		}

		public AssemblyInfo(Assembly assembly)
		{
			_assembly = assembly;
		}

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

		public string GetTitle(bool showBuild = true, bool showBuildDate = true, bool showArchitecture = true, bool showDescription = true)
		{
			var s = string.Format("{0} {1} {2}", Company, Product, Version.ToString(3));
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
			if (showBuildDate)
			{
				s += string.Format(" (Build: {0:yyyy-MM-dd})", BuildDateTime);
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
			return s.Trim();
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

	}
}
