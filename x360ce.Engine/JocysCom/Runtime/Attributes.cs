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
	//	string message,
	//	[CallerMemberName] string memberName = "",
	//	[CallerFilePath] string sourceFilePath = "",
	//	[CallerLineNumber] int sourceLineNumber = 0)
	//{
	//	Trace.WriteLine("message: " + message);
	//	Trace.WriteLine("member name: " + memberName);
	//	Trace.WriteLine("source file path: " + sourceFilePath);
	//	Trace.WriteLine("source line number: " + sourceLineNumber);
	//}

	public static partial class Attributes
	{

		#region Get DescriptionAttribute Value

		/// <summary>Cache data for speed.</summary>
		/// <remarks>Cache allows for this class to work 20 times faster.</remarks>
		private static ConcurrentDictionary<object, string> Descriptions = new ConcurrentDictionary<object, string>();

		/// <summary>
		/// Get DescriptionAttribute value from object or enumeration value.
		/// </summary>
		/// <param name="o">Enumeration value or object</param>
		/// <returns>Description, class name, or enumeration property name.</returns>
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

		private static ConcurrentDictionary<object, object> DefaultValues = new ConcurrentDictionary<object, object>();

		/// <summary>
		/// Return default value.
		/// </summary>
		/// <param name="value">Can be enum value or MemberInfo, PropertyInfo...
		///	Enum.Value
		/// typeof(ClassName)
		/// </param>
		public static string GetDefaultValue(object value)
		{
			var v = GetDefaultValue<object>(value);
			return v?.ToString();
		}

		/// <summary>
		/// Some enums can be decorated with DefaultValue attribute:
		///   [Description("Favourite"), DefaultValue("F")]
		///   Favourite,
		/// This function will get original Enum value by string default value.
		/// </summary>
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
		/// Return default attribute value.
		/// </summary>
		/// <typeparam name="T">Source Type</typeparam>
		/// <typeparam name="TResult">Return Type</typeparam>
		/// <param name="memberName">Member name i.e. property or field name.</param>
		public static TResult GetDefaultValue<T, TResult>(string memberName)
		{
			var member = typeof(T).GetMember(memberName);
			return GetDefaultValue<TResult>(member[0]);
		}

		public static T GetDefaultValue<T>(object value)
		{
			if (!_UseDefaultValuesCache)
				_GetDefaultValue<T>(value);
			return (T)DefaultValues.GetOrAdd(value, x => _GetDefaultValue<T>(x));
		}

		public static bool _UseDefaultValuesCache = true;

		/// <summary>
		/// </summary>
		/// <param name="value"></param>
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
		/// Assign property values from their [DefaultValueAttribute] value.
		/// </summary>
		/// <param name="o">Object to reset properties on.</param>
		public static void ResetPropertiesToDefault(object o, bool onlyIfNull = false, string[] exclude = null)
		{
			if (o is null)
				return;
			var type = o.GetType();
			var properties = type.GetProperties();
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
		/// Get a dictionary where the key is an enum and the value is the value of the description attribute.
		/// </summary>
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
		/// Find attribute including interfaces.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="methodInfo">Method info.</param>
		/// <returns></returns>
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
