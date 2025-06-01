using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Contains configuration information related to the assembly,
	/// including application data paths and utility methods for path expansion
	/// and parameterization.
	/// </summary>
	public partial class AssemblyInfo
	{
		/// <summary>Path to per-user application data; defaults to Entry.GetAppDataPath(true) if not set.</summary>
		public string AppUserData
		{
			get => _AppUserData ?? Entry.GetAppDataPath(true);
			set => _AppUserData = value;
		}
		string _AppUserData;

		/// <summary>Path to common application data; defaults to Entry.GetAppDataPath(false) if not set.</summary>
		public string AppCommonData
		{
			get => _AppCommonData ?? Entry.GetAppDataPath(false);
			set => _AppCommonData = value;
		}
		string _AppCommonData;

		/// <summary>Full path of the current process's main module file; defaults to Process.GetCurrentProcess().MainModule.FileName if not set.</summary>
		public string ModuleFileName
		{
			get => _ModuleFileName ?? System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			set => _ModuleFileName = value;
		}
		string _ModuleFileName;

		/// <summary>Directory of the module file or null if ModuleFileName is empty.</summary>
		public string ModuleDirectory
			=> string.IsNullOrEmpty(ModuleFileName) ? null : Path.GetDirectoryName(ModuleFileName);

		/// <summary>File name without extension of the module file or null if ModuleFileName is empty.</summary>
		public string ModuleBaseName
			=> string.IsNullOrEmpty(ModuleFileName) ? null : Path.GetFileNameWithoutExtension(ModuleFileName);

		/// <summary>Base path of the module file, combining ModuleDirectory and ModuleBaseName, or null if ModuleFileName is empty.</summary>
		public string ModuleBasePath
			=> string.IsNullOrEmpty(ModuleFileName) ? null : Path.Combine(ModuleDirectory, ModuleBaseName);


		#region Parametrize and Expand

		/// <summary>Expands %ENV_VAR% and {token} placeholders in the path using environment variables and custom token replacement.</summary>
		public static string ExpandPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;
			// Variables are quoted with '%' (percent) sign.
			path = Environment.ExpandEnvironmentVariables(path);
			// Variables are quoted with '{' and '}' sign.
			path = JocysCom.ClassLibrary.Text.Helper.Replace(path, Entry, false);
			return path;
		}

		/// <summary>
		/// Replaces {token} placeholders in the path with their values
		/// and optionally substitutes absolute paths with %ENV_VAR% tokens.
		/// </summary>
		public static string ParameterizePath(string path, bool useEnvironmentVariables = false)
		{
			if (string.IsNullOrEmpty(path))
				return path;
			// Variables are quoted with '{' and '}' sign
			path = JocysCom.ClassLibrary.Text.Helper.Replace(Entry, path, false);
			if (useEnvironmentVariables)
				path = ReplaceWithEnvironmentVariables(path);
			return path;
		}

		/// <summary>Replaces absolute path segments in the input string with %ENV_VAR% environment variable tokens.</summary>
		/// <remarks>
		/// Variables containing invalid path characters or unrooted values are ignored.
		/// Values are ordered by descending length to match longest paths first and avoid partial overlaps.
		/// </remarks>
		public static string ReplaceWithEnvironmentVariables(string input)
		{
			var invalidPathChars = Path.GetInvalidPathChars();
			var kvs = Environment.GetEnvironmentVariables()
				.Cast<DictionaryEntry>()
				.ToDictionary(x => x.Key, x => $"{x.Value}")
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
				.Where(kv => kv.Value.IndexOfAny(invalidPathChars) == -1)
				.Where(kv => Path.IsPathRooted(kv.Value))
				.OrderByDescending(kv => kv.Value.Length)
				.ToList();
			foreach (var kv in kvs)
				if (input.Contains(kv.Value))
					input = input.Replace(kv.Value, $"%{kv.Key}%");
			return input;
		}



		//public static string GetExpandedPath(string path)
		//{
		//	path = ExpandPath(path);
		//	path = IO.PathHelper.ConvertFromSpecialFoldersPattern(path, "{", "}");
		//	return path;
		//}

		//public static string GetParametrizedPath(string path)
		//{
		//	path = IO.PathHelper.ConvertToSpecialFoldersPattern(path, "{", "}");
		//	path = ParameterizePath(path);
		//	return path;
		//}

		#endregion

	}
}