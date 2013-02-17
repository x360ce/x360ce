using System;
using System.Data;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;

namespace x360ce.App
{
	/// <summary>
	/// Functions are provided only for compatibility with 16-bit versions of Windows. Applications should store initialization information in the registry.
	/// </summary>
	public class Ini
	{
	
		#region Kernel32 Functions

		/// <summary>
		/// The size of the buffer for return value. The maximum buffer size is 32767 characters.
		/// </summary>
		public short BufferLength
		{
			get { return bufferLength; }
			set { bufferLength = value; }
		}
		short bufferLength = 256;

		string _getPrivateProfileString(string section, string key, string defaultValue)
		{
			string results = defaultValue;

			var sb = new StringBuilder(bufferLength);
			byte[] bytes = new byte[bufferLength];
			int size = Win32.NativeMethods.GetPrivateProfileString(section, key, defaultValue, sb, sb.Capacity, this.File.FullName);
			results = sb.ToString();
			// remove comments.
			var cIndex = results.IndexOf(';');
			if (cIndex > -1) results = results.Substring(0, cIndex);
			cIndex = results.IndexOf('#');
			if (cIndex > -1) results = results.Substring(0, cIndex);
			return results.Trim(' ', '\t');
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
			return _getPrivateProfileString(section, key, defaultValue);
		}

		/// <summary>
		/// Get value from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <param name="key">The key of the element to add.</param>
		public string GetValue(string section, string key)
		{
			return _getPrivateProfileString(section, key, "");
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
		/// Get values in a section from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		/// <returns>The array of values.</returns>
		public string[] GetValues(string section)
		{
			return _getPrivateProfileString(section, null, null).Split((char)0);
		}

		/// <summary>
		/// Get sections from INI File.
		/// </summary>
		public string[] GetSections()
		{
			return _getPrivateProfileString(null, null, null).Split((char)0);
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

		#endregion
	}
}
