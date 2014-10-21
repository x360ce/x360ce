using System;
using System.Data;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace x360ce.App
{
	/// <summary>
	/// Functions are provided only for compatibility with 16-bit versions of Windows. Applications should store initialization information in the registry.
	/// </summary>
	public class Ini
	{

		#region Kernel32 Functions

		string[] _getPrivateProfileString(string section, string key, string defaultValue, int bufferLength)
		{
			char[] returnString = new char[bufferLength];
			int size = Win32.NativeMethods.GetPrivateProfileString(section, key, defaultValue, returnString, returnString.Length, this.File.FullName);
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
			if (list.Length > 0) return list[0];
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
			if (list.Length > 0) return list[0];
			return "";
		}

		/// <summary>
		/// Set value to INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		/// <param name="value">The value of the element to add.</param>
		public int SetValue(string section, string key, string value)
		{
			return Win32.NativeMethods.WritePrivateProfileString(section, key, value, this.File.FullName);
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
			Win32.NativeMethods.WritePrivateProfileString(section, key, null, this.File.FullName);
		}

		/// <summary>
		/// Remove section from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		public void RemoveSection(string section)
		{
			Win32.NativeMethods.WritePrivateProfileString(section, null, null, this.File.FullName);
		}

		///// <summary>
		///// Get all sections (tagged with brackets).
		///// </summary>
		///// <remarks>
		///// The API GetPriveProfileString fills an internal buffer separated by \0.
		///// In order to get a generic list we have to split that buffer with the token \0.
		///// </remarks>
		///// <returns>List of values.</returns>
		//public List<string> GetSections()
		//{
		//	var sb = new StringBuilder(ushort.MaxValue);
		//	Win32.NativeMethods.GetPrivateProfileString(null, null, null, sb, ushort.MaxValue, this.File.FullName);
		//	List<string> result = new List<string>(sb.ToString().Split((char)0));
		//	// The last 2 values can be removed, because they are always empty.
		//	result.RemoveRange(result.Count - 2, 2);
		//	return result;
		//}

		///// <summary>
		///// Get all keys by section.
		///// </summary>
		///// <param name="section">The name of the section.</param>
		///// <returns>List of values.</returns>
		//private static List<string> GetKeys(string section)
		//{
		//	var sb = new StringBuilder(short.MaxValue);
		//	Win32.NativeMethods.GetPrivateProfileString(section, null, null, sb, short.MaxValue, this.File.FullName);
		//	List<string> result = new List<string>(sb.ToString().Split((char)0));
		//	// The last 2 values can be removed, because they are always empty.
		//	result.RemoveRange(result.Count - 2, 2);
		//	return result;
		//}

		#endregion
	}
}
