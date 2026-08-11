using Microsoft.Win32;
using System;
using System.Globalization;

namespace x360ce.App.Issues
{
	public enum CppRuntimeArchitecture
	{
		X86,
		X64,
	}

	public sealed class CppRuntimeRegistryValue
	{
		public object Installed { get; set; }
		public string Version { get; set; }
		public object Major { get; set; }
		public object Minor { get; set; }
		public object Build { get; set; }
		public object Revision { get; set; }
	}

	public interface ICppRuntimeRegistry
	{
		CppRuntimeRegistryValue Read(RegistryView view, CppRuntimeArchitecture architecture);
	}

	public sealed class WindowsCppRuntimeRegistry : ICppRuntimeRegistry
	{
		// Microsoft documents this component key for Visual C++ v14 detection.
		// https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files
		const string RuntimeKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes";

		public CppRuntimeRegistryValue Read(RegistryView view, CppRuntimeArchitecture architecture)
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
			using (var key = baseKey.OpenSubKey(RuntimeKey + "\\" + GetKeyName(architecture)))
			{
				if (key == null)
					return null;
				return new CppRuntimeRegistryValue
				{
					Installed = key.GetValue("Installed"),
					Version = key.GetValue("Version") as string,
					Major = key.GetValue("Major"),
					Minor = key.GetValue("Minor"),
					Build = key.GetValue("Bld"),
					Revision = key.GetValue("Rbld"),
				};
			}
		}

		static string GetKeyName(CppRuntimeArchitecture architecture) =>
			architecture == CppRuntimeArchitecture.X86 ? "x86" : "x64";
	}

	public sealed class CppRuntimeDetectionResult
	{
		public CppRuntimeArchitecture Architecture { get; internal set; }
		public bool IsApplicable { get; internal set; } = true;
		public bool IsInstalled { get; internal set; }
		public Version Version { get; internal set; }
		public RegistryView RegistryView { get; internal set; }
		public string ErrorMessage { get; internal set; }
	}

	public sealed class CppRuntimeDetector
	{
		readonly ICppRuntimeRegistry registry;
		readonly bool is64BitOperatingSystem;

		public CppRuntimeDetector()
			: this(new WindowsCppRuntimeRegistry(), Environment.Is64BitOperatingSystem)
		{
		}

		public CppRuntimeDetector(ICppRuntimeRegistry registry, bool is64BitOperatingSystem)
		{
			this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
			this.is64BitOperatingSystem = is64BitOperatingSystem;
		}

		public CppRuntimeDetectionResult Detect(CppRuntimeArchitecture architecture)
		{
			var result = new CppRuntimeDetectionResult { Architecture = architecture };
			if (architecture == CppRuntimeArchitecture.X64 && !is64BitOperatingSystem)
			{
				result.IsApplicable = false;
				return result;
			}

			var view = architecture == CppRuntimeArchitecture.X64
				? RegistryView.Registry64
				: RegistryView.Registry32;
			try
			{
				var value = registry.Read(view, architecture);
				if (value == null)
					return result;
				result.RegistryView = view;
				result.Version = GetVersion(value);
				result.IsInstalled = ToInt32(value.Installed) == 1 &&
					result.Version != null && result.Version.Major >= 14;
			}
			catch (Exception ex)
			{
				result.ErrorMessage = ex.GetType().Name + ": " + ex.Message;
			}
			return result;
		}

		static Version GetVersion(CppRuntimeRegistryValue value)
		{
			var text = value.Version?.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
					text = text.Substring(1);
				if (System.Version.TryParse(text, out var parsed))
					return parsed;
			}

			var major = ToInt32(value.Major);
			var minor = ToInt32(value.Minor);
			var build = ToInt32(value.Build);
			var revision = ToInt32(value.Revision);
			if (major < 0 || minor < 0 || build < 0 || revision < 0)
				return null;
			return new Version(major, minor, build, revision);
		}

		static int ToInt32(object value)
		{
			if (value == null)
				return -1;
			try
			{
				return Convert.ToInt32(value, CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return -1;
			}
		}
	}
}
