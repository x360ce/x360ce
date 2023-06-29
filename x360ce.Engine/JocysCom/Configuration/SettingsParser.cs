using System;
using System.Linq;
#if NETCOREAPP // .NET Core
#elif NETSTANDARD // .NET Standard
using System.Globalization;
using System.Net;
using System.Collections.Generic;
using System.IO;
#else // .NET Framework...
using System.Configuration;
#endif
#if __MOBILE__
	using Xamarin.Forms;
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
		public T Parse<T>(string name, T defaultValue = default(T))
		{
			if (_GetValue is null)
				return defaultValue;
			var v = _GetValue(ConfigPrefix + name);
			return ParseValue<T>(v, defaultValue);
		}

		public static object ParseValue(Type t, string v, object defaultValue = null)
		{
			if (t is null)
				throw new ArgumentNullException(nameof(t));
			if (v is null)
				return defaultValue;
			if (typeof(System.Drawing.Color).IsAssignableFrom(t))
				return System.Drawing.Color.FromName(v);
			// Get Parse method with string parameter.
			var m = t.GetMethod("Parse", new[] { typeof(string) });
			if (m != null)
				return m.Invoke(null, new[] { v });
			//if (typeof(IPAddress).IsAssignableFrom(t))
			//	return IPAddress.Parse(v);
			//if (typeof(TimeSpan).IsAssignableFrom(t))
			//	return TimeSpan.Parse(v, CultureInfo.InvariantCulture);
			if (t.IsEnum)
				return Enum.Parse(t, v, true);
			// If type can be converted then convert.
			if (typeof(IConvertible).IsAssignableFrom(t))
				return System.Convert.ChangeType(v, t);
			return defaultValue;
		}

		public static T TryParseValue<T>(string v, T defaultValue = default(T))
		{
			try
			{
				return (T)ParseValue(typeof(T), v, defaultValue);
			}
			catch (Exception)
			{
				return defaultValue;
			}
		}

		public static T ParseValue<T>(string v, T defaultValue = default(T))
		{
			return (T)ParseValue(typeof(T), v, defaultValue);
		}

#if NETSTANDARD // If .NET Standard (Xamarin) preprocessor directive is set then...

		string GetValue(string name)
		{
			if (_GetValue != null)
				return _GetValue(ConfigPrefix + name);
#if __MOBILE__

			var p = Application.Current.Properties;
			var key = ConfigPrefix + name;
			if (!p.Keys.Contains(key))
			{
				if (EmbeddedAppSettings is null)
					ReadEmbeddedSettings();
				if (EmbeddedAppSettings.Keys.Contains(key))
					return EmbeddedAppSettings[key];
				return null;
			}
			return (string)p[key];
#else
			return null;
#endif
		}

#if __MOBILE__

		public static Dictionary<string, string> EmbeddedAppSettings;

		public static void ReadEmbeddedSettings()
		{
			EmbeddedAppSettings = new Dictionary<string, string>();
			var assembly = typeof(SettingsParser).Assembly;
			var names = assembly.GetManifestResourceNames();
			var name = names.FirstOrDefault(x => x.EndsWith("App.config"));
			if (string.IsNullOrEmpty(name))
				return;
			var stream = assembly.GetManifestResourceStream(name);
			using (var reader = new StreamReader(stream))
			{
				var doc = XDocument.Parse(reader.ReadToEnd());
				var items = doc
					.Element("configuration")
					.Element("appSettings")
					.Elements("add").ToList();
				foreach (var item in items)
				{
					var k = item.Attribute("key").Value;
					var v = item.Attribute("value").Value;
					EmbeddedAppSettings.Add(k, v);
				}
			}
		}

#endif

#elif NETCOREAPP // if .NET Core preprocessor directive is set then...

		public static Func<string, string> _GetValue;

#else // NETFRAMEWORK - .NET Framework...

		public static Func<string, string> _GetValue = (name)
			=> ConfigurationManager.AppSettings[name];

#endif

	}
}
