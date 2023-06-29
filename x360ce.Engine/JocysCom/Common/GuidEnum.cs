using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JocysCom.WebSites.Engine
{
	/// <summary>
	/// Provides Enumeration class with Guid value stored as attribute.
	/// </summary>
	public static class GuidEnum
	{
		/// <summary>
		/// Get attribute by enumeration:
		///	Guid attribute = GuidEnum.Attribute(myEnum)
		/// </summary>
		/// <typeparam name="TE">Enum Type</typeparam>
		/// <param name="e">You can use 'dynamic' to supply unknown enum value.
		/// dynamic en = Enum.Parse(enumType, name);
		/// </param>
		/// <returns></returns>
		public static Guid Attribute<TE>(TE e)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			var attrs =
				typeof(TE).GetField(e.ToString()).GetCustomAttributes(
				typeof(GuidValueAttribute), false
			) as GuidValueAttribute[];
			return attrs[0].Value;
		}

		private static Regex _GuidRegex;
		public static Regex GuidRegex
		{
			get
			{
				if (_GuidRegex is null)
				{
					_GuidRegex = new Regex(
						"^[A-Fa-f0-9]{32}$|" +
						"^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
						"^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$");
				}
				return _GuidRegex;
			}

		}

		public static bool IsGuid(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;
			var match = GuidRegex.Match(s);
			return match.Success;
		}

		/// <summary>
		/// Get enumeration by name:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.Parse("Gallery")</para>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TE Parse<TE>(string value)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			return (TE)Enum.Parse(typeof(TE), value);
		}

		public static TE TryParse<TE>(string value, TE defaultValue, bool asString)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			var results = defaultValue;
			try
			{
				if (char.IsLetter(value[0]) || !asString)
					results = (TE)Enum.Parse(typeof(TE), value);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception) { }
#pragma warning restore CA1031 // Do not catch general exception types
			return results;
		}

		/// <summary>
		/// Get enumeration by name:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.Parse("Gallery")</para>
		/// </summary>
		/// <param name="value"></param>
		/// <param name="ignoreCase"></param>
		/// <returns></returns>
		public static TE Parse<TE>(string value, bool ignoreCase)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			return (TE)Enum.Parse(typeof(TE), value, ignoreCase);
		}

		/// <summary>
		/// Get enumeration by attribute:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.ParseByAttribute("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")</para>
		/// </summary>
		/// <typeparam name="TE"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TE ParseByAttribute<TE>(string value)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			return ParseByAttribute<TE>(new Guid(value));
		}

		/// <summary>
		/// Get enumeration by attribute:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.ParseByAttribute(new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"))</para>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TE ParseByAttribute<TE>(Guid value)
			// Declare TE as same as Enum.
			where TE : struct, IComparable, IFormattable, IConvertible
		{
			foreach (string name in Enum.GetNames(typeof(TE)))
			{
				var e = (TE)Enum.Parse(typeof(TE), name);
				if (Attribute(e) == value)
					return e;
			}
			return default(TE);
		}

		/// <summary>
		/// Gets the System.Type with the specified name, performing a case-sensitive search.
		/// </summary>
		/// <param name="typeName">The name of the type to get.</param>
		/// <param name="resolve">True to resolve partial type name.</param>
		/// <returns>The System.Type with the specified name, if found; otherwise, null.</returns>
		public static Type FindType(string typeName, bool resolve)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var t = Type.GetType(typeName);
			// Search for object in other assemblies.
			for (var a = 0; a < assemblies.Length && t is null; a++)
				t = assemblies[a].GetType(typeName);
			if (resolve)
			{
				for (var a = 0; a < assemblies.Length && t is null; a++)
				{
					var types = assemblies[a].GetTypes();
					for (var i = 0; i < types.Length; i++)
						if (types[i].FullName.EndsWith("." + typeName)) t = types[i];
				}
			}
			return t;
		}

		/// <summary>
		/// Creates a System.Collections.Generic.Dictionary from an System.Enum.
		/// </summary>
		/// <param name="typeName">The name of the type to get.</param>
		/// <param name="resolve">True to resolve partial type name.</param>
		/// <returns>A System.Collections.Generic.Dictionary.</returns>
		/// <remarks>
		/// Method can be used to bind enumeration to DropDownLists.
		/// </remarks>
		public static SortedDictionary<string, object> ToNameDictionary(string typeName, bool resolve)
		{
			Type enumType = FindType(typeName, resolve);
			return ToNameDictionary(enumType);
		}

		/// <summary>
		/// Creates a System.Collections.Generic.Dictionary from an System.Enum.
		/// </summary>
		/// <param name="type">Enumeration type.</param>
		/// <returns>A System.Collections.Generic.Dictionary.</returns>
		public static SortedDictionary<string, object> ToNameDictionary(Type type)
		{
			var list = new SortedDictionary<string, object>();
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			var names = Enum.GetNames(type);
			foreach (string name in names)
				list.Add(name, name);
			return list;
		}

		/// <summary>
		/// Creates a System.Collections.Generic.Dictionary from an System.Enum.
		/// </summary>
		/// <param name="typeName">The name of the type to get.</param>
		/// <param name="resolve">True to resolve partial type name.</param>
		/// <returns>A System.Collections.Generic.Dictionary.</returns>
		public static SortedDictionary<string, object> ToValueDictionary(string typeName, bool resolve)
		{
			Type enumType = FindType(typeName, resolve);
			var list = new SortedDictionary<string, object>();
			if (enumType is null)
				throw new ArgumentNullException(typeName);
			var names = Enum.GetNames(enumType);
			foreach (string name in names)
				list.Add(name, Enum.Parse(enumType, name));
			return list;
		}

		/// <summary>
		/// Creates a System.Collections.Generic.Dictionary from an System.Enum.
		/// </summary>
		/// <param name="typeName">The name of the type to get.</param>
		/// <param name="resolve">True to resolve partial type name.</param>
		/// <returns>A System.Collections.Generic.Dictionary.</returns>
		public static SortedDictionary<string, Guid> ToAttributeDictionary(string typeName, bool resolve)
		{
			var enumType = FindType(typeName, resolve);
			var list = new SortedDictionary<string, Guid>();
			if (enumType is null)
				throw new ArgumentNullException(typeName);
			var names = Enum.GetNames(enumType);
			foreach (string name in names)
			{
				dynamic en = Enum.Parse(enumType, name);
				var guid = Attribute(en);
				list.Add(name, guid);
			}
			return list;
		}

	}
}
