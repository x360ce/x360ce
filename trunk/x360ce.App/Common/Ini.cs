using System;
using System.Data;
using System.Configuration;
using System.Runtime.InteropServices;

namespace x360ce.App
{
	/// <summary>
	/// Functions are provided only for compatibility with 16-bit versions of Windows. Applications should store initialization information in the registry.
	/// </summary>
	public class Ini
	{
	
		#region Kernel32 Functions

		/// <summary>
		/// Retrieves a string from the specified section in an initialization file. http://msdn2.microsoft.com/en-us/library/ms724353.aspx
		/// </summary>
		/// <param name="lpAppName">The name of the section containing the key name. If this parameter is NULL, the GetPrivateProfileString function copies all section names in the file to the supplied buffer.</param>
		/// <param name="lpKeyName">The name of the key whose associated string is to be retrieved. If this parameter is NULL, all key names in the section specified by the lpAppName parameter are copied to the buffer specified by the lpReturnedString parameter.</param>
		/// <param name="lpDefault">A default string. If the lpKeyName key cannot be found in the initialization file, GetPrivateProfileString copies the default string to the lpReturnedString buffer. If this parameter is NULL, the default is an empty string, "". Avoid specifying a default string with trailing blank characters. The function inserts a null character in the lpReturnedString buffer to strip any trailing blanks.</param>
		/// <param name="lpReturnedString">[out] A pointer to the buffer that receives the retrieved string.</param>
		/// <param name="nSize">The size of the buffer pointed to by the lpReturnedString parameter, in characters.</param>
		/// <param name="lpFileName">The name of the initialization file. If this parameter does not contain a full path to the file, the system searches for the file in the Windows directory.</param>
		/// <returns>The return value is the number of characters copied to the buffer, not including the terminating null character.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"),
		DllImport("kernel32", SetLastError = true)]
		internal static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, byte[] lpReturnedString, int nSize, string lpFileName);

		/// <summary>
		/// Copies a string into the specified section of an initialization file. http://msdn2.microsoft.com/en-us/library/ms725501.aspx
		/// </summary>
		/// <param name="lpAppName">The name of the section to which the string will be copied. If the section does not exist, it is created. The name of the section is case-independent; the string can be any combination of uppercase and lowercase letters.</param>
		/// <param name="lpKeyName">The name of the key to be associated with a string. If the key does not exist in the specified section, it is created. If this parameter is NULL, the entire section, including all entries within the section, is deleted.</param>
		/// <param name="lpString">A null-terminated string to be written to the file. If this parameter is NULL, the key pointed to by the lpKeyName parameter is deleted.</param>
		/// <param name="lpFileName">The name of the initialization file. If the file was created using Unicode characters, the function writes Unicode characters to the file. Otherwise, the function writes ANSI characters.</param>
		/// <returns>If the function successfully copies the string to the initialization file, the return value is nonzero. If the function fails, or if it flushes the cached version of the most recently accessed initialization file, the return value is zero.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"),
		DllImport("kernel32", SetLastError = true)]
		internal static extern int WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

		/// <summary>
		/// The size of the buffer for return value. The maximum buffer size is 32767 characters.
		/// </summary>
		public short BufferLength
		{
			get { return bufferLength; }
			set { bufferLength = value; }
		}
		private short bufferLength = 256;

		private string _getPrivateProfileString(string section, string key, string defaultValue)
		{
			string results = defaultValue;
			byte[] bytes = new byte[bufferLength];
			int size = GetPrivateProfileString(section, key, defaultValue, bytes, bufferLength, this.File.FullName);
			results = System.Text.Encoding.GetEncoding(1252).GetString(bytes, 0, size).TrimEnd((char)0);
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
			return WritePrivateProfileString(section, key, value, this.File.FullName);
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
			WritePrivateProfileString(section, key, null, this.File.FullName);
		}

		/// <summary>
		/// Remove section from INI File.
		/// </summary>
		/// <param name="section">The name of the section.</param>
		public void RemoveSection(string section)
		{
			WritePrivateProfileString(section, null, null, this.File.FullName);
		}

		#endregion
	}
}
