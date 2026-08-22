using Microsoft.Win32;
using System;

namespace x360ce.App.Issues
{
	/// <summary>
	/// Detect the Visual C++ v14x runtime.
	/// </summary>
	/// <remarks>
	/// Uses the component key Microsoft documents for redistributable detection:
	/// https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files
	/// Matching display names in Add/Remove Programs is unreliable, because the name
	/// carries a year label that changes with every release and is localized.
	/// All 14.x runtimes are backwards compatible, so any of them is enough.
	/// </remarks>
	internal static class CppRuntimeDetector
	{

		const string RuntimeKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes";

		/// <summary>Installed runtime version, or null when not installed.</summary>
		/// <param name="x64">True for the 64-bit runtime, false for the 32-bit runtime.</param>
		public static Version GetInstalledVersion(bool x64)
		{
			var view = x64 ? RegistryView.Registry64 : RegistryView.Registry32;
			var name = x64 ? "x64" : "x86";
			try
			{
				using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
				using (var key = baseKey.OpenSubKey(RuntimeKey + "\\" + name))
				{
					if (key == null)
						return null;
					if (GetInt(key, "Installed") != 1)
						return null;
					var major = GetInt(key, "Major");
					// Runtimes before 14.0 are a different, incompatible product line.
					if (major < 14)
						return null;
					return new Version(major, GetInt(key, "Minor"), GetInt(key, "Bld"), GetInt(key, "Rbld"));
				}
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return null;
			}
		}

		static int GetInt(RegistryKey key, string name)
		{
			var value = key.GetValue(name);
			if (value == null)
				return 0;
			try
			{
				return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				return 0;
			}
		}

	}
}
