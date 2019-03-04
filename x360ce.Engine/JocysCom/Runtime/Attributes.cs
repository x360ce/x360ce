using System;
using System.Collections.Generic;
using System.ComponentModel;
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

	public partial class Attributes
	{

		#region Description Attribute

		/// <summary>Cache data for speed.</summary>
		static Dictionary<object, string> Descriptions = new Dictionary<object, string>();
		static object DescriptionsLock = new object();

		/// <summary>
		/// Get DescriptionAttribute value from object or enumeration value.
		/// </summary>
		/// <param name="o">Enumeration value or object</param>
		/// <returns>Description, class name, or enumeration property name.</returns>
		public static string GetDescription(object o)
		{
			if (o == null)
				return null;
			lock (DescriptionsLock)
			{
				var type = o.GetType();
				// If enumeration then use value as a key, otherwise use type string.
				var key = type.IsEnum
					? o
					: type.ToString();
				if (Descriptions.ContainsKey(key))
					return Descriptions[key];
				// Set default value.
				var value = type.IsEnum
					? string.Format("{0}", o)
					: type.FullName;
				// If enumeration then specify to get attribute from a field, otherwise from type.
				var ap = type.IsEnum
					? (ICustomAttributeProvider)type.GetField(Enum.GetName(type, o))
					: type;
				if (ap != null)
				{
					var attributes = ap.GetCustomAttributes(typeof(DescriptionAttribute), !type.IsEnum);
					if (attributes.Length > 0)
					{
						var da = (DescriptionAttribute)attributes[0];
						if (da != null)
							value = da.Description;
					}
				}
				Descriptions.Add(key, value);
				return value;
			}
		}

		#endregion

		#region DefaultValue

		/// <summary>Cache data for speed.</summary>
		static private Dictionary<object, object> DefaultValues = new Dictionary<object, object>();
		static object DefaultValuesLock = new object();

		public static string GetDefaultValue(object value)
		{
			var v = GetDefaultValue<object>(value);
			return v == null ? null : v.ToString();
		}

		public static T GetByDefaultValue<T>(string value)
		{
			var items = (T[])Enum.GetValues(typeof(T));
			foreach (var item in items)
			{
				var s = GetDefaultValue(item);
				if (string.Compare(s, value, true) == 0)
				{
					return item;
				}
			}
			return default(T);
		}

		public static T GetDefaultValue<T>(object value)
		{
			lock (DefaultValuesLock)
			{
				if (!DefaultValues.ContainsKey(value))
				{
					DefaultValueAttribute[] attributes;
					ICustomAttributeProvider p;
					if (value is ICustomAttributeProvider)
					{
						p = (ICustomAttributeProvider)value;
					}
					else
					{
						p = value.GetType().GetField(value.ToString());
					}
					attributes = (DefaultValueAttribute[])p.GetCustomAttributes(typeof(DefaultValueAttribute), false);
					var r = attributes.Length > 0 ? attributes[0].Value : null;
					DefaultValues.Add(value, r);
				}
			}
			return (T)DefaultValues[value];
		}

		#endregion

	}

}
