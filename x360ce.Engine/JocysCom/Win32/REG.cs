using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Win32
{
	/// <summary>
	/// Registry property value types.
	/// </summary>
	/// <remarks>
	/// If data has the REG_SZ, REG_MULTI_SZ, or REG_EXPAND_SZ type, the string
	/// may not have been stored with the proper terminating null characters.
	/// Therefore, when reading a string from the registry, you must ensure that
	/// the string is properly terminated before using it; otherwise, it may
	/// overwrite a buffer. (Note that REG_MULTI_SZ strings should have two
	/// terminating null characters.)
	///
	/// When writing a string to the registry, you must specify the length of the
	/// string, including the terminating null character (\0). A common error is
	/// to use the strlen function to determine the length of the string, but to
	/// forget that strlen returns only the number of characters in the string,
	/// not including the terminating null. Therefore, the length of the string
	/// should be calculated as follows: strlen( string ) + 1
	///
	/// A REG_MULTI_SZ string ends with a string of length 0. Therefore, it is
	/// not possible to include a zero-length string in the sequence. An empty
	/// sequence would be defined as follows: \0.
	/// </remarks>
	public enum REG : uint
	{
		/// <summary>No defined value type.</summary>
		REG_NONE = 0,
		/// <summary>A null-terminated string.</summary>
		REG_SZ = 1,
		/// <summary>A null-terminated string that contains unexpanded references to environment variables (for example, "%PATH%").</summary>
		REG_EXPAND_SZ = 2,
		/// <summary>Binary data in any form.</summary>
		REG_BINARY = 3,
		/// <summary>A 32-bit number.</summary>
		REG_DWORD = 4,
		/// <summary>A 32-bit number in little-endian format.</summary>
		REG_DWORD_LITTLE_ENDIAN = 4,
		/// <summary>A 32-bit number in big-endian format.</summary>
		REG_DWORD_BIG_ENDIAN = 5,
		/// <summary>A null-terminated Unicode string that contains the target path of a symbolic link.</summary>
		REG_LINK = 6,
		/// <summary>A sequence of null-terminated strings, terminated by an empty string (\0).</summary>
		REG_MULTI_SZ = 7,
		/// <summary></summary>
		REG_RESOURCE_LIST = 8,
		/// <summary></summary>
		REG_FULL_RESOURCE_DESCRIPTOR = 9,
		/// <summary></summary>
		REG_RESOURCE_REQUIREMENTS_LIST = 10,
		/// <summary>A 64-bit number.</summary>
		REG_QWORD = 11,
		/// <summary>A 64-bit number in little-endian format.</summary>
		REG_QWORD_LITTLE_ENDIAN = 11
	}

}
