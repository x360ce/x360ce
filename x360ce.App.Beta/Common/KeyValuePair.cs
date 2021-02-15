using System;
using System.Runtime.InteropServices;

namespace x360ce.App
{

	/// <summary>
	/// Key/value pair that can be set or retrieved.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public class KeyValuePair
	{
		/// <summary>
		/// Initializes an instance of the key/value pair type with the specified key and value.
		/// </summary>
		/// <param name="key">
		/// The string defined in each key/value pair. 
		/// </param>
		/// <param name="value">
		/// The definition associated with <paramref name="key" />. 
		/// </param>
		public KeyValuePair(string key, string value)
		{
			Key = key;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the key in the key/value pair.
		/// </summary>
		/// <returns>
		/// The key in the key/value pair.
		/// </returns>
		public string Key { get; set; }

		/// <summary>
		/// Gets or sets the value in the key/value pair.
		/// </summary>
		/// <returns>
		/// The value in the key/value pair.
		/// </returns>
		public string Value { get; set; }

	}

}
