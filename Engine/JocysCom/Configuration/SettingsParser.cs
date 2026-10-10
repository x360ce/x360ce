#nullable disable

using System;
using System.Linq;
#if NETFRAMEWORK // .NET Framework...
using System.Configuration;
#endif
#if __MOBILE__
    using Xamarin.Forms;
#endif

namespace JocysCom.ClassLibrary.Configuration
{
    /// <summary>
    /// Parse application setting values.
    /// Parses app settings across .NET Framework, .NET Standard, .NET Core, and Xamarin; converts raw strings to target types.
    /// </summary>
    public partial class SettingsParser
    {

        public SettingsParser(string configPrefix = "")
        {
            ConfigPrefix = configPrefix;
        }

        public string ConfigPrefix { get; set; }
        public static SettingsParser Current { get; } = new SettingsParser();

        /// <summary>
        /// Parse all IConvertible types, like value types, with one function.
        /// Retrieves the setting identified by <paramref name="name"/> (prefixed with <see cref="ConfigPrefix"/>)
        /// and converts it to <typeparamref name="T"/>. Falls back to <paramref name="defaultValue"/> if missing or conversion fails.
        /// </summary>
        /// <typeparam name="T">Target type for conversion.</typeparam>
        /// <param name="name">Setting key without prefix.</param>
        /// <param name="defaultValue">Value returned if missing or conversion fails.</param>
        /// <returns>Converted value or <paramref name="defaultValue"/>.</returns>
        public T Parse<T>(string name, T defaultValue = default(T))
        {
            if (_GetValue is null)
                return defaultValue;
            var v = _GetValue(ConfigPrefix + name);
            return ParseValue<T>(v, defaultValue);
        }

        /// <summary>Converts a string to the specified <paramref name="t"/> type.</summary>
        /// <remarks>
        /// Supports System.Drawing.Color (FromName), static Parse(string), enums (case-insensitive), and IConvertible via Convert.ChangeType.
        /// Returns <paramref name="defaultValue"/> if <paramref name="v"/> is null or no converter is found.
        /// </remarks>
        /// <param name="t">Target type; must not be null.</param>
        /// <param name="v">Input string value.</param>
        /// <param name="defaultValue">Fallback value.</param>
        /// <returns>Converted object or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="t"/> is null.</exception>
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
            //    return IPAddress.Parse(v);
            //if (typeof(TimeSpan).IsAssignableFrom(t))
            //    return TimeSpan.Parse(v, CultureInfo.InvariantCulture);
            if (t.IsEnum)
                return Enum.Parse(t, v, true);
            // If type can be converted then convert.
            if (typeof(IConvertible).IsAssignableFrom(t))
                return System.Convert.ChangeType(v, t);
            return defaultValue;
        }

        /// <summary>Attempts to convert the string to <typeparamref name="T"/>, returning <paramref name="defaultValue"/> on error.</summary>
        /// <typeparam name="T">Target type to parse.</typeparam>
        /// <param name="v">Input string.</param>
        /// <param name="defaultValue">Fallback value.</param>
        /// <returns>Parsed value or <paramref name="defaultValue"/>.</returns>
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

#if NETFRAMEWORK // .NET Framework...

        /// <summary>Delegate to fetch configuration values by key; defaults to ConfigurationManager.AppSettings in .NET Framework.</summary>
        public static Func<string, string> _GetValue = (name)
            => ConfigurationManager.AppSettings[name];

#else // .NET (Core/5+)

        /// <summary>Delegate to fetch configuration values by key; initialize via InitializeParser in .NET Core.</summary>
        public static Func<string, string> _GetValue;

#endif

    }
}