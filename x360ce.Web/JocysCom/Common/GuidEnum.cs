using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		///	    <para>Guid attribute = GuidEnum.Attribute(myEnum)</para>
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static Guid Attribute<E>(E e)
		{
			GuidValueAttribute[] attrs =
			typeof(E).GetField(e.ToString()).GetCustomAttributes(
			typeof(GuidValueAttribute), false
			) as GuidValueAttribute[];
			return attrs[0].Value;
		}

		private static Regex _GuidRegex;
		public static Regex GuidRegex
		{
			get
			{
				if (_GuidRegex == null)
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
			if (string.IsNullOrEmpty(s)) return false;
			Match match = GuidRegex.Match(s);
			return match.Success;
		}

		/// <summary>
		/// Get enumeration by name:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.Parse("Gallery")</para>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static E Parse<E>(string value)
		{
			if (value == null) throw new ArgumentNullException("value");
			return (E)Enum.Parse(typeof(E), value);
		}

		public static E TryParse<E>(string value, E defaultValue, bool asString)
		{
			if (value == null) throw new ArgumentNullException("value");
			E results = defaultValue;
			try
			{
				if (char.IsLetter(value[0]) || !asString)
				{
					results = (E)Enum.Parse(typeof(E), value);
				}
			}
			catch (Exception) { }
			return results;
		}

		/// <summary>
		/// Get enumeration by name:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.Parse("Gallery")</para>
		/// </summary>
		/// <param name="value"></param>
		/// <param name="ignoreCase"></param>
		/// <returns></returns>
		public static E Parse<E>(string value, bool ignoreCase)
		{
			if (value == null) throw new ArgumentNullException("value");
			return (E)Enum.Parse(typeof(E), value, ignoreCase);
		}

		/// <summary>
		/// Get enumeration by attribute:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.ParseByAttribute("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")</para>
		/// </summary>
		/// <typeparam name="E"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static E ParseByAttribute<E>(string value)
		{
			return ParseByAttribute<E>(new Guid(value));
		}

		/// <summary>
		/// Get enumeration by attribute:
		///     <para>myEnum = GuidEnum&lt;MyEnum&gt;.ParseByAttribute(new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"))</para>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static E ParseByAttribute<E>(Guid value)
		{
			E result = default(E);
			foreach (string name in Enum.GetNames(typeof(E)))
			{
				E e = (E)Enum.Parse(typeof(E), name);
				if (Attribute(e) == value)
				{
					result = e;
					break;
				}
			}
			return result;
		}

		/// <summary>
		/// Gets the System.Type with the specified name, performing a case-sensitive search.
		/// </summary>
		/// <param name="typeName">The name of the type to get.</param>
		/// <param name="resolve">True to resolve partial type name.</param>
		/// <returns>The System.Type with the specified name, if found; otherwise, null.</returns>
		public static Type FindType(string typeName, bool resolve)
		{
			System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Type t = Type.GetType(typeName);
			// Search for object in other assemblies.
			for (int a = 0; a < assemblies.Length && t == null; a++) t = assemblies[a].GetType(typeName);

			if (resolve)
			{
				for (int a = 0; a < assemblies.Length && t == null; a++)
				{
					Type[] types = assemblies[a].GetTypes();
					for (int i = 0; i < types.Length; i++)
					{
						if (types[i].FullName.EndsWith("." + typeName)) t = types[i];
					}
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
			SortedDictionary<string, object> list = new SortedDictionary<string, object>();
			if (type == null) throw new ArgumentNullException("type");
			string[] names = Enum.GetNames(type);
			foreach (string name in names) list.Add(name, name);
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
			SortedDictionary<string, object> list = new SortedDictionary<string, object>();
			if (enumType == null) throw new Exception(typeName);
			string[] names = Enum.GetNames(enumType);
			foreach (string name in names) list.Add(name, Enum.Parse(enumType, name));
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
			Type enumType = FindType(typeName, resolve);
			SortedDictionary<string, Guid> list = new SortedDictionary<string, Guid>();
			if (enumType == null) throw new Exception(typeName);
			string[] names = Enum.GetNames(enumType);
			foreach (string name in names) list.Add(name, Attribute(Enum.Parse(enumType, name)));
			return list;
		}

	}
}
