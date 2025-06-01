using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Summary description for PathConvert.
	/// </summary>
	public static partial class PathHelper
	{
		const string nullOrEmpty = "Input cannot be null or empty";

		public static void ValidateFileName(string name)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			var invalid = name.Intersect(System.IO.Path.GetInvalidFileNameChars());
			if (invalid.Any())
				throw new ArgumentException(string.Format("Invalid file name chars found: {0}", string.Join(", ", invalid)), nameof(name));
		}

		/// <summary>
		/// Method used to protect from
		/// SUPPRESS: CWE-78: Improper Neutralization of Special Elements used in an OS Command ('OS Command Injection')
		/// https://cwe.mitre.org/data/definitions/78.html
		/// </summary>
		/// <param name="path"></param>
		public static void ValidatePath(string path)
		{
			var d = System.IO.Path.GetDirectoryName(path);
			var invalid = d.Intersect(System.IO.Path.GetInvalidPathChars());
			if (invalid.Any())
				throw new ArgumentException(string.Format("Invalid path chars found: {0}", string.Join(", ", invalid)), nameof(path));
			var name = System.IO.Path.GetFileName(path);
			ValidateFileName(name);
		}

		#region Detect Type of Path

		/// <summary>
		/// Virtual path starts from "/" or "\" 
		/// </summary>
		public static bool IsPathVirtual(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException(nullOrEmpty, nameof(path));
			var newPath = path.Trim();
			return
				newPath.StartsWith("/") ||
				newPath.StartsWith("\\");
		}

		/// <summary>
		/// A physical path is how the OS locates the resource.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool IsPathPhysical(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException(nullOrEmpty, nameof(path));
			var newPath = path.Trim();
			return
				newPath.Contains(':') ||
				newPath.Contains("\\");
		}

		#endregion

		// Get Physical path from any type of path.
		public static string GetPathPhysical(string relativeTo, string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			string physicalPath;
			// If path is Physical. 
			if (IsPathPhysical(path))
			{
				// Return current path
				physicalPath = path;
			}
			else
			{
				// Convert to physical.
				physicalPath = relativeTo + "/" + path;
				// Convert "/" separator to "\".
				physicalPath = physicalPath.Replace("/", "\\");
				// Remove double "\\".
				physicalPath = physicalPath.Replace("\\\\", "\\");
			}
			// Return results.
			return physicalPath;
		}

		// Get Rooted path.
		public static string GetPathRooted(string relativeTo, string path)
		{
			if (path is null) throw new ArgumentNullException(nameof(path));
			//if path already rooted then...
			if (System.IO.Path.IsPathRooted(path))
				return path;
			// Combine path.
#if NETCOREAPP
			var combined = System.IO.Path.GetFullPath(path, relativeTo);
#else
			var combined = System.IO.Path.Combine(relativeTo, path);
#endif
			// Fix dot notations.
			combined = Path.GetFullPath(combined);
			return combined;
		}

		/// <summary>
		///    root     parent
		/// ----------- --------
		///             ..\..\..
		/// c:\r1\r2\r3\p1\p2\p3\\
		/// c:\r1\r2\r3\a1\file.ext
		///             ----------- 
		///             append
		/// Important: if relative is directory then path must end with '\' or '/'.
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <param name="pathTo"></param>
		/// <returns></returns>
		public static string GetRelativePath(string relativeTo, string path, bool addCurrentDir = true)
		{
			if (string.IsNullOrEmpty(relativeTo))
				throw new ArgumentException(nullOrEmpty, nameof(relativeTo));
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException(nullOrEmpty, nameof(path));
			var containsPrimarySeparator = relativeTo.Contains(Path.DirectorySeparatorChar);
			relativeTo = Path.GetFullPath(relativeTo);
			path = Path.GetFullPath(path);
			var relativeToUri = new Uri(relativeTo);
			var pathUri = new Uri(path);
			var relativeUri = relativeToUri.MakeRelativeUri(pathUri).ToString();
			var relativePath = Uri.UnescapeDataString(relativeUri);
			// Add current folder prefix if output does not contain separators.
			if (addCurrentDir && !relativePath.Contains(Path.DirectorySeparatorChar) && !relativePath.Contains(Path.AltDirectorySeparatorChar))
				relativePath = "." + Path.AltDirectorySeparatorChar + relativePath;
			// Make sure that output contains same separator type as input.
			if (containsPrimarySeparator)
				relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return relativePath;
		}

		// Get Relative path from any type of path.
		public static string GetPathVirtual(string relativeTo, string path)
		{
			if (path is null) throw new ArgumentNullException(nameof(path));
			// If path is not physical then just return.
			if (!IsPathPhysical(path))
				return path;
			// Convert to virtual path by cropping root part.
			var virtualPath = "/" + path.Substring(relativeTo.Length);
			// Convert "\" separator to "/".
			virtualPath = virtualPath.Replace("\\", "/");
			// Remove double "//".
			virtualPath = virtualPath.Replace("//", "/");
			// Return results.
			return virtualPath;
		}

		// Get parent folder from path.
		public static string GetParentFolderPhysical(string relativeTo, string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			// Get physical path.
			var physicalPath = GetPathPhysical(relativeTo, path);
			// Get folder only.
			var folderPath = System.IO.Path.GetDirectoryName(physicalPath);
			// Return results.
			return folderPath;
		}

		// Get parent folder from path.
		public static string GetParentFolderVirtual(string relativeTo, string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			// Get physical parent folder path.
			var folderPath = GetParentFolderPhysical(relativeTo, path);
			// Convert to virtual path.
			var virtualPath = GetPathVirtual(relativeTo, folderPath);
			// Return results.
			return virtualPath;
		}

		public static string GetFileName(string relativeTo, string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));
			// Make sure that path is physical.
			var physicalPath = GetPathPhysical(relativeTo, path);
			return System.IO.Path.GetFileName(physicalPath);
		}


		#region Path Converter

		static object SpecialFoldersLock = new object();

		static Dictionary<string, string> _SpecialFolders;

		public static Dictionary<string, string> SpecialFolders
		{
			get
			{
				lock (SpecialFoldersLock)
				{
					if (_SpecialFolders is null)
					{
						var keys = (System.Environment.SpecialFolder[])Enum.GetValues(typeof(System.Environment.SpecialFolder));
						var items = new List<KeyValuePair<string, string>>();
						foreach (var key in keys)
						{
							var item = new KeyValuePair<string, string>($"{key}", System.Environment.GetFolderPath(key));
							// Make sure all values are not empty and unique.
							if (!string.IsNullOrEmpty(item.Key) && !string.IsNullOrEmpty(item.Value))
								items.Add(item);
						}
						_SpecialFolders = new Dictionary<string, string>();
						// Order folders descending.
						var list = items.OrderByDescending(x => x.Value).ToArray();
						foreach (var listItem in list)
						{
							// If list doesn't contains key then...
							if (!_SpecialFolders.ContainsKey(listItem.Key))
								_SpecialFolders.Add(listItem.Key, listItem.Value);
						}
					}
				}
				return _SpecialFolders;
			}
		}

		#endregion

	}
}
