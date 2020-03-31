using System;
using System.Globalization;
using System.Net;
#if NETSTANDARD // If .NET Standard (Xamarin) preprocessor directive is set then...
using Xamarin.Forms;
#else
using System.Configuration;
#endif

namespace JocysCom.ClassLibrary.Configuration
{
	/// <summary>
	/// Parse application setting values.
	/// </summary>
	public partial class SettingsParser
	{

		public SettingsParser(string configPrefix = "")
		{
			ConfigPrefix = configPrefix;
		}

		public string ConfigPrefix { get; set; }
		public static SettingsParser Current { get; } = new SettingsParser();

		/// <summary>Parse all IConvertible types, like value types, with one function.</summary>
		public T Parse<T>(string name, T defaultValue = default(T)) where T : IConvertible
		{
			var v = GetValue(name);
			if (v == null)
				return defaultValue;
			return (T)System.Convert.ChangeType(v, typeof(T));
		}

#if NETSTANDARD // If .NET Standard (Xamarin) preprocessor directive is set then...

		string GetValue(string name) => 
			(string)Application.Current.Properties[ConfigPrefix + name];

#else

		string GetValue(string name) =>
			ConfigurationManager.AppSettings[ConfigPrefix + name];

#endif

		/// <summary>TimeSpan is structure. Default is value.</summary>
		public TimeSpan ParseTimeSpan(string name, TimeSpan defaultValue = default(TimeSpan))
		{
			var v = GetValue(name);
			if (v == null)
				return defaultValue;
			return TimeSpan.Parse(v, CultureInfo.InvariantCulture);
		}

		/// <summary>Enumeration is structure. Default is value.</summary>
		public T ParseEnum<T>(string name, T defaultValue = default(T), bool ignoreCase = false) where T : struct, IComparable, IFormattable, IConvertible
		{
			var v = GetValue(name);
			if (v == null)
				return defaultValue;
			return (T)Enum.Parse(typeof(T), v, ignoreCase);
		}

		/// <summary>IP Address is class. Default is null.</summary>
		public IPAddress ParseIPAddress(string name, IPAddress defaultValue = default(IPAddress))
		{
			var v = GetValue(name);
			if (v == null)
				return defaultValue;
			return IPAddress.Parse(v);
		}

	}
}
