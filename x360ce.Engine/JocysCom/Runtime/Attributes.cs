using System;
using System.Collections.Generic;
using System.ComponentModel;

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
		#region Description

		/// <summary>Cache data for speed.</summary>
		static private Dictionary<object, string> Descriptions = new Dictionary<object, string>();
		static object DescriptionsLock = new object();

		public static string GetDescription(object value)
		{
			lock (DescriptionsLock)
			{
				if (!Descriptions.ContainsKey(value))
				{
					var fi = value.GetType().GetField(value.ToString());
					var r = string.Format("{0}", value);
					if (fi != null)
					{
						DescriptionAttribute[] attributes = (DescriptionAttribute[])(fi.GetCustomAttributes(typeof(DescriptionAttribute), false));
						if (attributes.Length > 0) r = attributes[0].Description;
					}
					Descriptions.Add(value, r);
				}
			}
			return Descriptions[value].ToString();
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
					DefaultValueAttribute[] attributes = (DefaultValueAttribute[])(value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DefaultValueAttribute), false));
					var r = ((attributes.Length > 0) ? (attributes[0].Value) : null).ToString();
					DefaultValues.Add(value, r);
				}
			}
			return (T)DefaultValues[value];
		}

		#endregion

	}

}
