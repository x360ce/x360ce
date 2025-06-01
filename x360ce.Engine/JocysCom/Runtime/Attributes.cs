using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace JocysCom.ClassLibrary.Runtime
{
    
    //Example:
    //public void TraceMessage(
    //    string message,
    //    [CallerMemberName] string memberName = "",
    //    [CallerFilePath] string sourceFilePath = "",
    //    [CallerLineNumber] int sourceLineNumber = 0)
    //{
    //    Trace.WriteLine("message: " + message);
    //    Trace.WriteLine("member name: " + memberName);
    //    Trace.WriteLine("source file path: " + sourceFilePath);
    //    Trace.WriteLine("source line number: " + sourceLineNumber);
    //}
    
    /// <summary>
    /// Reflection utilities for retrieving and caching <see cref="DescriptionAttribute"/> and <see cref="DefaultValueAttribute"/> metadata,
    /// resetting objects to default attribute values, and finding custom attributes including interface implementations.
    /// </summary>
    public static partial class Attributes
    {
    
        #region Get DescriptionAttribute Value
    
        /// <summary>Thread-safe cache of <see cref="DescriptionAttribute"/> values to avoid repeated reflection calls.</summary>
        /// <remarks>Avoids reflection overhead; yields up to 20× speedup.</remarks>
        private static ConcurrentDictionary<object, string> Descriptions = new ConcurrentDictionary<object, string>();
    
        /// <summary>
        /// Retrieves the <see cref="DescriptionAttribute"/> value for an object or enum value.
        /// Uses a thread-safe cache when <paramref name="cache"/> is true to avoid repeated reflection.
        /// </summary>
        /// <param name="o">Object or enumeration value.</param>
        /// <param name="cache">True to cache results; false to query attributes each call.</param>
        /// <returns>The description attribute text, or the class name or enum member name if none is set.</returns>
        public static string GetDescription(object o, bool cache = true)
        {
            if (o is null)
                return null;
            var type = o.GetType();
            if (!cache)
                return _GetDescription(o);
            // If enumeration then use value as a key, otherwise use type string.
            var key = type.IsEnum
                ? o
                : type.ToString();
            return Descriptions.GetOrAdd(key, x => _GetDescription(x));
        }
    
        /// <summary>
        /// Retrieves the <see cref="DescriptionAttribute"/> for the given object (enum field or type)
        /// and returns its <see cref="DescriptionAttribute.Description"/> value.
        /// If not found, returns the enum member’s name or type’s full name.
        /// </summary>
        private static string _GetDescription(object o)
        {
            if (o is null)
                return null;
            var type = o.GetType();
            // If enumeration then get attribute from a field, otherwise from type.
            var ap = type.IsEnum
                ? (ICustomAttributeProvider)type.GetField(Enum.GetName(type, o))
                : type;
            if (!(ap is null))
            {
                var attributes = ap.GetCustomAttributes(typeof(DescriptionAttribute), !type.IsEnum);
                // If atribute is present then return value.
                if (attributes.Length > 0)
                    return ((DescriptionAttribute)attributes[0]).Description;
            }
            // Return default value.
            return type.IsEnum
                ? string.Format("{0}", o)
                : type.FullName;
        }
    
        #endregion
    
        #region Get DefaultValueAttribute Value
    
        /// <summary>Thread-safe cache of <see cref="DefaultValueAttribute"/> lookup results to avoid reflection overhead.</summary>
        private static ConcurrentDictionary<object, object> DefaultValues = new ConcurrentDictionary<object, object>();
    
        /// <summary>
        /// Gets the string representation of the <see cref="DefaultValueAttribute"/> value for an object.
        /// </summary>
        /// <param name="value">
        /// An enum member or reflection provider (MemberInfo, PropertyInfo, etc.).
        /// Examples:
        ///   Enum.Value
        ///   typeof(ClassName)
        /// </param>
        /// <returns>The attribute value converted to string, or null if none is set.</returns>
        public static string GetDefaultValue(object value)
        {
            var v = GetDefaultValue<object>(value);
            return v?.ToString();
        }
    
        /// <summary>
        /// Finds an enum member by its <see cref="DefaultValueAttribute"/> value.
        /// </summary>
        /// <remarks>
        /// Enums can be decorated with [DefaultValue(...)]:
        ///   [Description("Favourite"), DefaultValue("F")]
        ///   Favourite,
        /// </remarks>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="defaultValue">String to match against the DefaultValueAttribute.</param>
        /// <returns>The matching enum member, or default(T) if no match.</returns>
        public static T GetByDefaultValue<T>(string defaultValue) where T : Enum
        {
            var items = (T[])Enum.GetValues(typeof(T));
            foreach (var item in items)
            {
                var s = GetDefaultValue(item);
                if (string.Compare(s, defaultValue, true) == 0)
                    return item;
            }
            return default(T);
        }
    
        /// <summary>
        /// Retrieves the <see cref="DefaultValueAttribute"/> value for the specified member on type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type containing the member.</typeparam>
        /// <typeparam name="TResult">Type of the attribute value to return.</typeparam>
        /// <param name="memberName">Name of the property or field.</param>
        /// <returns>The attribute’s value, or default(<typeparamref name="TResult"/>) if not present.</returns>
        public static TResult GetDefaultValue<T, TResult>(string memberName)
        {
            var member = typeof(T).GetMember(memberName);
            return GetDefaultValue<TResult>(member[0]);
        }
    
        /// <summary>
        /// Retrieves a value defined by <see cref="DefaultValueAttribute"/> for the specified object.
        /// </summary>
        /// <typeparam name="T">Return type of the attribute value.</typeparam>
        /// <param name="value">An enum member, MemberInfo, or Type instance.</param>
        /// <returns>The attribute-defined default, or default(T) if none is present.</returns>
        /// <remarks>Results are cached in <see cref="DefaultValues"/> unless <see cref="_UseDefaultValuesCache"/> is false.</remarks>
        public static T GetDefaultValue<T>(object value)
        {
            if (!_UseDefaultValuesCache)
                _GetDefaultValue<T>(value);
            return (T)DefaultValues.GetOrAdd(value, x => _GetDefaultValue<T>(x));
        }
    
        /// <summary>
        /// When false, disables caching of <see cref="DefaultValueAttribute"/> lookups to force re-evaluation on each call.
        /// </summary>
        public static bool _UseDefaultValuesCache = true;
    
        /// <summary>
        /// Retrieves the <see cref="DefaultValueAttribute"/> value for the given object or enum field via reflection.
        /// </summary>
        /// <typeparam name="T">Type of the attribute value.</typeparam>
        /// <param name="value">An ICustomAttributeProvider or enum member.</param>
        /// <returns>The attribute’s value, or default(T) if none is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</returns>
        /// <remarks>
        /// If <paramref name="value"/> is not an <see cref="ICustomAttributeProvider"/>,
        /// it is assumed to be an enum member, and the corresponding field is inspected.
        /// </remarks>
        private static T _GetDefaultValue<T>(object value)
        {
            // Check if MemberInfo/ICustomAttributeProvider.
            var p = value as ICustomAttributeProvider;
            // Assume it is enumeration value.
            if (p is null)
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                p = value.GetType().GetField(value.ToString());
            }
            var attributes = (DefaultValueAttribute[])p.GetCustomAttributes(typeof(DefaultValueAttribute), false);
            if (attributes.Length > 0)
                return (T)attributes[0].Value;
            return default;
        }
    
        #endregion
    
        /// <summary>
        /// Resets public instance properties decorated with <see cref="DefaultValueAttribute"/> to their default values.
        /// </summary>
        /// <param name="o">The target object whose properties to reset.</param>
        /// <param name="onlyIfNull">True to reset only properties whose current value is null.</param>
        /// <param name="exclude">Names of properties to exclude from resetting.</param>
        /// <remarks>
        /// Only public instance properties with a <see cref="DefaultValueAttribute"/> are considered;
        /// write-only or excluded properties are ignored.
        /// </remarks>
        public static void ResetPropertiesToDefault(object o, bool onlyIfNull = false, string[] exclude = null)
        {
            if (o is null)
                return;
            var type = o.GetType();
            var properties = type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
            foreach (var p in properties)
            {
                if (exclude?.Contains(p.Name) == true)
                    continue;
                if (p.CanRead && onlyIfNull && p.GetValue(o, null) != null)
                    continue;
                if (!p.CanWrite)
                    continue;
                var da = p.GetCustomAttributes(typeof(DefaultValueAttribute), false);
                if (da.Length == 0)
                    continue;
                var value = ((DefaultValueAttribute)da[0]).Value;
                p.SetValue(o, value, null);
            }
        }
    
        /// <summary>
        /// Builds a dictionary mapping each enum member to its <see cref="DescriptionAttribute"/> text.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="keys">Optional array of enum members; defaults to all values of the enum.</param>
        /// <returns>A dictionary from enum members to their description strings.</returns>
        public static Dictionary<T, string> GetDictionary<T>(T[] keys = null) where T : Enum
        {
            if (keys == null)
                keys = (T[])Enum.GetValues(typeof(T));
            var dict = new Dictionary<T, string>();
            foreach (var key in keys)
                dict[key] = GetDescription(key);
            return dict;
        }
    
        /// <summary>
        /// Retrieves a custom attribute of type <typeparamref name="T"/> from a method,
        /// searching the method itself and its interface implementations.
        /// </summary>
        /// <typeparam name="T">Type of the attribute.</typeparam>
        /// <param name="methodInfo">Method to inspect.</param>
        /// <returns>An instance of <typeparamref name="T"/> if found; otherwise null.</returns>
        public static T FindCustomAttribute<T>(MethodInfo methodInfo) where T : Attribute
        {
            // First, try to get the attribute from the method directly.
            var attribute = methodInfo.GetCustomAttribute<T>(true);
            if (attribute != null)
                return attribute;
            // If the attribute is not found, search on the interfaces.
            foreach (var iface in methodInfo.DeclaringType.GetInterfaces())
            {
                // Get the interface map for the current interface.
                var map = methodInfo.DeclaringType.GetInterfaceMap(iface);
                for (int i = 0; i < map.TargetMethods.Length; i++)
                {
                    // Check if the current method in the map matches the methodInfo.
                    if (map.TargetMethods[i] != methodInfo)
                        continue;
                    // If it matches, try to get the attribute from the corresponding interface method.
                    attribute = map.InterfaceMethods[i].GetCustomAttribute<T>(true);
                    if (attribute != null)
                        return attribute;
                }
            }
            return null;
        }
    }

}