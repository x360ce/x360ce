using System;
using System.Data;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace x360ce.Engine
{
	/// <summary>
	/// Functions are provided only for compatibility with 16-bit versions of Windows. Applications should store initialization information in the registry.
	/// </summary>
	/// <remarks>
	/// INI files support only UTF16-little endian format.
	/// </remarks>
	public class Ini
    {

        #region Kernel32 Functions

        string[] _getPrivateProfileString(string section, string key, string defaultValue, int bufferLength)
		{
			char[] returnString = new char[bufferLength];
			int size = JocysCom.ClassLibrary.Win32.NativeMethods.GetPrivateProfileString(section, key, defaultValue, returnString, returnString.Length, this.File.FullName);
			string values = new string(returnString, 0, size);
			string[] list = values.Split(new[] { (char)0 }, StringSplitOptions.RemoveEmptyEntries);
			return list;
		}

		static string RemoveComments(string s)
		{
			// Remove comments.
			var cIndex = s.IndexOf(';');
			if (cIndex > -1) s = s.Substring(0, cIndex);
			cIndex = s.IndexOf('#');
			if (cIndex > -1) s = s.Substring(0, cIndex);
			return s.Trim(' ', '\t');
		}

		#endregion

		/// <summary>
		/// INI Constructor.
		/// </summary>
		public Ini(string fileName)
		{
			this.File = new System.IO.FileInfo(fileName);
		}

		#region Properties

		public System.IO.FileInfo File
		{
			get { return m_file; }
			set { m_file = value; }
		}
		System.IO.FileInfo m_file;

		public static DataTable NewTable()
		{
			DataTable table = new DataTable("Info");
			table.Locale = System.Globalization.CultureInfo.InvariantCulture;
			DataColumn sectionColumn = new DataColumn("Section", typeof(string), "", MappingType.Attribute);
			DataColumn keyColumn = new DataColumn("Key", typeof(string), "", MappingType.Attribute);
			DataColumn valueColumn = new DataColumn("Value", typeof(string), "", MappingType.Attribute);
			table.Columns.Add(sectionColumn);
			table.Columns.Add(keyColumn);
			table.Columns.Add(valueColumn);
			table.AcceptChanges();
			return table;
		}

		#endregion

		#region Get, Set and Remove

		/// <summary>
		/// Get value from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="defaultValue">If the key cannot be found in the initialization file then return default value.</param>
		public string GetValue(string section, string key, string defaultValue)
		{
			string[] list = _getPrivateProfileString(section, key, defaultValue, byte.MaxValue);
			if (list.Length > 0) return RemoveComments(list[0]);
			return null;
		}

		/// <summary>
		/// Get value from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		public string GetValue(string section, string key)
		{
			string[] list = _getPrivateProfileString(section, key, null, byte.MaxValue);
			if (list.Length > 0) return RemoveComments(list[0]);
			return "";
		}

		/// <summary>
		/// Set value to INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add.</param>
		/// <returns>Non-zero when written; zero when the file could not be written.</returns>
		public int SetValue(string section, string key, string value)
		{
			if (!EnsureUnicode())
				return 0;
			return JocysCom.ClassLibrary.Win32.NativeMethods.WritePrivateProfileString(section, key, value, this.File.FullName);
		}

		/// <summary>
		/// Makes sure the file is UTF-16 before Windows writes into it.
		/// </summary>
		/// <remarks>
		/// The profile functions keep whatever encoding a file already has and create a new file as
		/// ANSI, so a device name outside the code page would be lost. A missing file is created
		/// empty with the UTF-16 mark, and a file in another encoding is rewritten once. The file
		/// sits beside the program, so in a protected folder both can be refused; that is answered
		/// as "not written" rather than thrown, because the caller is a save, not a fault.
		/// </remarks>
		/// <returns>True when the file is UTF-16 and can be written.</returns>
		public bool EnsureUnicode()
		{
			var path = this.File.FullName;
			try
			{
				if (!System.IO.File.Exists(path))
				{
					System.IO.File.WriteAllText(path, "", Encoding.Unicode);
					return true;
				}
				using (var stream = System.IO.File.OpenRead(path))
				{
					var b0 = stream.ReadByte();
					var b1 = stream.ReadByte();
					if (b0 == 0xFF && b1 == 0xFE)
						return true;
				}
				string content;
				using (var reader = new System.IO.StreamReader(path, true))
					content = reader.ReadToEnd();
				System.IO.File.WriteAllText(path, content, Encoding.Unicode);
				return true;
			}
			catch (UnauthorizedAccessException) { return false; }
			catch (System.IO.IOException) { return false; }
		}

		/// <summary>
		/// Get sections from INI File.
		/// </summary>
		public string[] GetSections()
		{
			return _getPrivateProfileString(null, null, null, short.MaxValue);
		}

		/// <summary>
		/// Get section keys from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <returns>The array of values.</returns>
		public string[] GetKeys(string section)
		{
			return _getPrivateProfileString(section, null, null, short.MaxValue);
		}

		/// <summary>
		/// Remove value from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		public void RemoveValue(string section, string key)
		{
			JocysCom.ClassLibrary.Win32.NativeMethods.WritePrivateProfileString(section, key, null, this.File.FullName);
		}

		/// <summary>
		/// Remove section from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		public void RemoveSection(string section)
		{
			JocysCom.ClassLibrary.Win32.NativeMethods.WritePrivateProfileString(section, null, null, this.File.FullName);
		}

		#endregion
	}
}
