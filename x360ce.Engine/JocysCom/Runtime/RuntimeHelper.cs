using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	public static partial class RuntimeHelper
	{

		/*
		public static bool IsKnownType(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			return
				type == typeof(string)
				// Note: Every Primitive type (such as int, double, bool, char, etc.) is a ValueType. 
				|| type.IsValueType
				|| type.IsSerializable;
		}
		*/

		private static readonly HashSet<Type> numericTypes = new HashSet<Type>
		{
			typeof(int),  typeof(double),  typeof(decimal),
			typeof(long), typeof(short),   typeof(sbyte),
			typeof(byte), typeof(ulong),   typeof(ushort),
			typeof(uint), typeof(float),
		};

		public static bool IsNumeric(Type type)
		{
			return numericTypes.Contains(type);
		}

		/// <summary>Built-in types</summary>
		public static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string>
		{
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(char), "char" },
			{ typeof(decimal), "decimal" },
			{ typeof(double), "double" },
			{ typeof(float), "float" },
			{ typeof(int), "int" },
			{ typeof(long), "long" },
			{ typeof(object), "object" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(short), "short" },
			{ typeof(string), "string" },
			{ typeof(uint), "uint" },
			{ typeof(ulong), "ulong" },
			{ typeof(ushort), "ushort" },
			{ typeof(void), "void" }
		};

		public static string GetBuiltInTypeNameOrAlias(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			var elementType = type.IsArray
				? type.GetElementType()
				: type;
			// Lookup alias for type
			string alias;
			if (TypeAliases.TryGetValue(elementType, out alias))
				return alias + (type.IsArray ? "[]" : "");
			// Note: All Nullable<T> are value types.
			if (type.IsValueType)
			{
				var underType = Nullable.GetUnderlyingType(type);
				if (underType != null)
					return GetBuiltInTypeNameOrAlias(underType) + "?";
			}
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
			{
				var itemType = type.GetGenericArguments()[0];
				return string.Format("List<{0}>", GetBuiltInTypeNameOrAlias(itemType));
			}
			// Default to CLR type name
			return type.Name;
		}

		private static Type GetFirstArgumentOfGenericType(Type type)
		{
			return type.GetGenericArguments()[0];
		}

		public static bool IsNullableType(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			return type.IsGenericType
				? type.GetGenericTypeDefinition() == typeof(Nullable<>)
				: false;
		}

		public static BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		#region Copy Fields

		/// <summary>
		/// Get source intersecting fields.
		/// </summary>
		private static FieldInfo[] GetItersectingFields(object source, object target)
		{
			var targetNames = target.GetType().GetFields(DefaultBindingFlags).Select(x => x.Name).ToArray();
			var sourceFields = source
				.GetType()
				.GetFields(DefaultBindingFlags)
				.Where(x => targetNames.Contains(x.Name))
				.ToArray();
			return sourceFields;
		}

		/// <summary>Cache data for speed.</summary>
		/// <remarks>Cache allows for this class to work 20 times faster.</remarks>
		private static ConcurrentDictionary<Type, FieldInfo[]> Fields { get; } = new ConcurrentDictionary<Type, FieldInfo[]>();

		private static FieldInfo[] GetFields(Type t, bool cache = true)
		{
			var items = cache
				? Fields.GetOrAdd(t, x => t.GetFields(DefaultBindingFlags))
				: t.GetFields(DefaultBindingFlags);
			return items;
		}


		/// <summary>Cache data for speed.</summary>
		/// <remarks>Cache allows for this class to work 20 times faster.</remarks>
		private static ConcurrentDictionary<Type, PropertyInfo[]> Properties { get; } = new ConcurrentDictionary<Type, PropertyInfo[]>();

		private static PropertyInfo[] GetProperties(Type t, bool cache = true)
		{
			var items = cache
				? Properties.GetOrAdd(t, x => t.GetProperties(DefaultBindingFlags))
				: t.GetProperties(DefaultBindingFlags);
			return items;
		}

		public static void CopyFields(object source, object target, bool onlyNonByRef = false)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			// Get Field Info.
			var sourceFields = GetFields(source.GetType());
			var targetFields = GetFields(target.GetType());
			foreach (var sm in sourceFields)
			{
				var tm = targetFields.FirstOrDefault(x => x.Name == sm.Name);
				bool useJson;
				if (!CanCopy(sm.FieldType, tm.FieldType, onlyNonByRef, out useJson))
					continue;
				// Get source value.
				var sValue = sm.GetValue(source);
				if (useJson)
					sValue = Serialize(sValue);
				var update = true;
				// Get target value.
				var dValue = tm.GetValue(target);
				if (useJson)
					dValue = Serialize(dValue);
				// Update only if values are different.
				update = !Equals(sValue, dValue);
				if (update)
				{
					if (useJson)
						sValue = Deserialize(sValue as string, tm.FieldType);
					tm.SetValue(target, sValue);
				}
			}
		}

		#endregion

		#region Serializer

		/// <summary>Cache data for speed.</summary>
		/// <remarks>Cache allows for this class to work 20 times faster.</remarks>
		private static ConcurrentDictionary<Type, DataContractJsonSerializer> JsonSerializers = new ConcurrentDictionary<Type, DataContractJsonSerializer>();

		static DataContractJsonSerializer GetJsonSerializer(Type type, DataContractJsonSerializerSettings settings = null)
		{
			if (type == null)
				return null;
			return JsonSerializers.GetOrAdd(type, x => new DataContractJsonSerializer(type, settings));
		}

		// DataContractJsonSerializerSettings requires .NET 4.5
		static DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings()
		{
			IgnoreExtensionDataObject = true,
			// Simple dictionary format looks like this: { "Key1": "Value1", "Key2": "Value2" }
			UseSimpleDictionaryFormat = true,
		};


		private static string Serialize(object o)
		{
			if (o is null)
				return null;
			var serializer = GetJsonSerializer(o.GetType());
			var ms = new MemoryStream();
			lock (serializer) { serializer.WriteObject(ms, o); }
			var json = Encoding.UTF8.GetString(ms.ToArray());
			ms.Close();
			return json;
		}

		private static object Deserialize(string json, Type type)
		{
			if (json is null)
				return null;
			var serializer = GetJsonSerializer(type);
			var bytes = Encoding.UTF8.GetBytes(json);
			var ms = new MemoryStream(bytes);
			object o;
			lock (serializer) { o = serializer.ReadObject(ms); }
			ms.Close();
			return o;
		}

		#endregion

		#region Copy Properties

		/// <summary>
		/// Get information about different and intersecting properties.
		/// </summary>
		public static string GetPropertyDiffInfo<TSource, TTarget>()
		{
			var sType = typeof(TSource);
			var tType = typeof(TTarget);
			var sProperties = sType.GetProperties(DefaultBindingFlags);
			var tProperties = tType.GetProperties(DefaultBindingFlags);
			var sNames = sProperties.Select(x => x.Name).ToArray();
			var tNames = tProperties.Select(x => x.Name).ToArray();
			var targetOnly = tProperties.Where(x => !sNames.Contains(x.Name)).OrderBy(x => x.Name).ToArray();
			var sourceOnly = sProperties.Where(x => !tNames.Contains(x.Name)).OrderBy(x => x.Name).ToArray();
			var sourceSame = sProperties.Where(x => tNames.Contains(x.Name)).OrderBy(x => x.Name).ToArray();
			var sb = new StringBuilder();
			var targetOnlyLines = targetOnly.Select(x => string.Format("{0} {1}", GetBuiltInTypeNameOrAlias(x.PropertyType), x.Name));
			if (targetOnly.Length > 0)
			{
				sb.AppendFormat("// ---- Target Only [{0}] - {1}\r\n\t", targetOnly.Length, tType.Name);
				sb.Append(string.Join("\r\n\t", targetOnlyLines));
				sb.AppendLine();
			}
			var sourceOnlyLines = sourceOnly.Select(x => string.Format("{0} {1}", GetBuiltInTypeNameOrAlias(x.PropertyType), x.Name));
			if (sourceOnly.Length > 0)
			{
				sb.AppendFormat("// ---- Source Only [{0}] - {1}\r\n\t", sourceOnly.Length, sType.Name);
				sb.Append(string.Join("\r\n\t", sourceOnlyLines));
				sb.AppendLine();
			}
			if (sourceSame.Length > 0)
			{
				sb.AppendFormat("// ---- Intersects [{0}]:\r\n\t", sourceSame.Length);
				var intesectsLines = sourceSame.Select(s =>
					string.Format("{0} {1}{2}",
						GetBuiltInTypeNameOrAlias(s.PropertyType),
						s.Name,
						(GetBuiltInTypeNameOrAlias(tProperties.First(t => t.Name == s.Name).PropertyType) == GetBuiltInTypeNameOrAlias(s.PropertyType))
						? ""
						: " // " + GetBuiltInTypeNameOrAlias(tProperties.First(t => t.Name == s.Name).PropertyType)
					)
				);
				sb.Append(string.Join("\r\n\t", intesectsLines));
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>
		/// Retur true if can copy.
		/// </summary>
		public static bool CanCopy(Type source, Type target, bool onlyNonByRef, out bool useJson)
		{
			useJson = false;
			// If target property don't exists.
			if (target == null)
				return false;
			// If target property can't be assigned.
			if (!source.IsAssignableFrom(source))
				return false;
			// If only non reference properties can be compied.
			if (onlyNonByRef && source.IsByRef)
				return false;
			// Use JSON to clone referenced values.
			useJson = source.IsByRef;
			return true;
		}

		public static void CopyProperties(object source, object target, bool onlyNonByRef = false)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			// Get type of the destination object.
			var sourceProperties = GetProperties(source.GetType());
			var targetProperties = GetProperties(target.GetType());
			foreach (var sm in sourceProperties)
			{
				// Get destination property and skip if not found.
				var tm = targetProperties.FirstOrDefault(x => Equals(x.Name, sm.Name));
				if (!sm.CanRead || !tm.CanWrite)
					continue;
				bool useJson;
				if (!CanCopy(sm.PropertyType, tm.PropertyType, onlyNonByRef, out useJson))
					continue;
				// Get source value.
				var sValue = sm.GetValue(source, null);
				if (useJson)
					sValue = Serialize(sValue);
				var update = true;
				// If can read target value.
				if (tm.CanRead)
				{
					// Get target value.
					var dValue = tm.GetValue(target, null);
					if (useJson)
						dValue = Serialize(dValue);
					// Update only if values are different.
					update = !Equals(sValue, dValue);
				}
				if (update)
				{
					if (useJson)
						sValue = Deserialize(sValue as string, tm.PropertyType);
					tm.SetValue(target, sValue, null);
				}
			}
		}

		/// <summary>
		/// Returns true if all properties with the same name are equal.
		/// </summary>
		public static bool EqualProperties(object source, object target, bool onlyNonByRef = false)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			// Get type of the destination object.
			var sourceProperties = GetProperties(source.GetType());
			var targetProperties = GetProperties(target.GetType());
			foreach (var sm in sourceProperties)
			{
				// Get destination property and skip if not found.
				var tm = targetProperties.FirstOrDefault(x => Equals(x.Name, sm.Name));
				if (!sm.CanRead)
					continue;
				bool useJson;
				if (!CanCopy(sm.PropertyType, tm.PropertyType, onlyNonByRef, out useJson))
					continue;
				// Get source value.
				var sValue = sm.GetValue(source, null);
				if (useJson)
					sValue = Serialize(sValue);
				var update = true;
				// If can read target value.
				if (tm.CanRead)
				{
					// Get target value.
					var dValue = tm.GetValue(target, null);
					if (useJson)
						dValue = Serialize(dValue);
					// Update only if values are different.
					update = !Equals(sValue, dValue);
				}
				// If update needed then not equal.
				if (update)
					return false;
			}
			return true;
		}

		#endregion

		#region Convert: Object <-> Bytes

		// Note: Similar as "Structure <-> Bytes", but with ability to convert variable strings.

		public static byte[] ObjectToBytes<T>(T o)
		{
			using (var ms = new MemoryStream())
			{
				var flags = BindingFlags.Instance | BindingFlags.Public;
				var props = typeof(T).GetProperties(flags);
				using (var writer = new BinaryWriter(ms))
				{
					foreach (var p in props)
					{
						var value = p.GetValue(o);
						switch (value)
						{
							case bool v:
								writer.Write(v); break;
							case byte v:
								writer.Write(v); break;
							case byte[] v:
								writer.Write(v); break;
							case char[] v:
								writer.Write(v); break;
							case char v:
								writer.Write(v); break;
							case decimal v:
								writer.Write(v); break;
							case double v:
								writer.Write(v); break;
							case float v:
								writer.Write(v); break;
							case int v:
								writer.Write(v); break;
							case long v:
								writer.Write(v); break;
							case sbyte v:
								writer.Write(v); break;
							case short v:
								writer.Write(v); break;
							case string v:
								writer.Write(v); break;
							case uint v:
								writer.Write(v); break;
							case ulong v:
								writer.Write(v); break;
							case ushort v:
								writer.Write(v); break;
							default:
								break;
						}
					}
					ms.Flush();
					ms.Seek(0, SeekOrigin.Begin);
					return ms.ToArray();
				}
			}
		}

		public static T BytesToObject<T>(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				var o = Activator.CreateInstance<T>();
				var flags = BindingFlags.Instance | BindingFlags.Public;
				var props = typeof(T).GetProperties(flags);
				using (var reader = new BinaryReader(ms))
				{
					foreach (var p in props)
					{
						var typeCode = Type.GetTypeCode(p.PropertyType);
						object v;
						switch (typeCode)
						{
							case TypeCode.Boolean:
								v = reader.ReadBoolean();
								break;
							case TypeCode.Char:
								v = reader.ReadChar();
								break;
							case TypeCode.DBNull:
								v = DBNull.Value;
								break;
							case TypeCode.DateTime:
								v = new DateTime(reader.ReadInt64());
								break;
							case TypeCode.Decimal:
								v = reader.ReadDecimal();
								break;
							case TypeCode.Double:
								v = reader.ReadDouble();
								break;
							case TypeCode.Empty:
								v = null;
								break;
							case TypeCode.SByte:
								v = reader.ReadSByte();
								break;
							case TypeCode.Int16:
								v = reader.ReadInt16();
								break;
							case TypeCode.Int32:
								v = reader.ReadInt32();
								break;
							case TypeCode.Int64:
								v = reader.ReadInt64();
								break;
							case TypeCode.Single:
								v = reader.ReadSingle();
								break;
							case TypeCode.String:
								v = reader.ReadString();
								break;
							case TypeCode.Byte:
								v = reader.ReadByte();
								break;
							case TypeCode.UInt16:
								v = reader.ReadUInt16();
								break;
							case TypeCode.UInt32:
								v = reader.ReadUInt32();
								break;
							case TypeCode.UInt64:
								v = reader.ReadUInt64();
								break;
							default:
								throw new Exception("Non Serializable Object: " + p.PropertyType);
						}
						p.SetValue(o, v);
					}
				}
				return o;
			}
		}

		#endregion

		#region Convert: Structure <-> Bytes

		/// <summary>
		/// Convert structure to byte array (unmanaged block of memory).
		/// </summary>
		public static byte[] StructureToBytes<T>(T value) where T : struct
		{
			var size = Marshal.SizeOf(value);
			var bytes = new byte[size];
			var handle = default(GCHandle);
			try
			{
				handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
			}
			finally
			{
				if (handle.IsAllocated)
					handle.Free();
			}
			return bytes;
		}

		public static T BytesToStructure<T>(byte[] bytes) where T : struct
		{
			return (T)BytesToStructure(bytes, typeof(T));
		}

		/// <summary>
		/// Convert byte array (unmanaged block of memory) to structure.
		/// </summary>
		public static object BytesToStructure(byte[] bytes, Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			var value = type.IsValueType ? Activator.CreateInstance(type) : null;
			var handle = default(GCHandle);
			try
			{
				handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				value = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
			}
			finally
			{
				if (handle.IsAllocated)
					handle.Free();
			}
			return value;
		}

		#endregion

		#region Try Parse

		/// <summary>
		/// Tries to convert the specified string representation of a logical value to
		/// its type T equivalent. A return value indicates whether the conversion
		/// succeeded or failed.
		/// </summary>
		/// <typeparam name="T">The type to try and convert to.</typeparam>
		/// <param name="value">A string containing the value to try and convert.</param>
		/// <param name="type">target type</param>
		/// <param name="result">If the conversion was successful, the converted value of type T.</param>
		/// <returns>If value was converted successfully, true; otherwise false.</returns>
		public static bool TryParse(object value, Type t, out object result)
		{
			if (IsNullable(t))
				t = Nullable.GetUnderlyingType(t) ?? t;
			//var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
			//if (converter.IsValid(value))
			//{
			//	result = (T)converter.ConvertFromString(value);
			//	return true;
			//}
			if (t == typeof(string))
			{
				result = value;
				return true;
			}
			if (t.IsEnum)
			{
				var retValue = value is null ? false : Enum.IsDefined(t, value?.ToString());
				result = retValue ? Enum.Parse(t, value?.ToString()) : default;
				return retValue;
			}
			var tryParseMethod = t.GetMethod("TryParse",
				BindingFlags.Static | BindingFlags.Public, null,
				new[] { typeof(string), t.MakeByRefType() }, null);
			var parameters = new object[] { value, null };
			var retVal = (bool)tryParseMethod.Invoke(null, parameters);
			result = parameters[1];
			return retVal;
		}


		/// <summary>
		/// Tries to convert the specified string representation of a logical value to
		/// its type T equivalent. A return value indicates whether the conversion
		/// succeeded or failed.
		/// </summary>
		/// <typeparam name="T">The type to try and convert to.</typeparam>
		/// <param name="value">A string containing the value to try and convert.</param>
		/// <param name="result">If the conversion was successful, the converted value of type T.</param>
		/// <returns>If value was converted successfully, true; otherwise false.</returns>
		public static bool TryParse<T>(string value, out T result)
		{
			var t = typeof(T);
			object o;
			var success = TryParse(value, t, out o);
			result = (T)o;
			return success;
		}

		/// <summary>
		/// Tries to convert the specified string representation of a logical value to
		/// its type T equivalent. Returns default value if conversion failed.
		/// </summary>
		public static T TryParse<T>(string value, T defaultValue = default(T))
		{
			var result = default(T);
			return TryParse(value, out result)
				? result
				: defaultValue;
		}

		/// <summary>
		/// Tries to convert the specified string representation of a logical value to
		/// its type T equivalent. Returns default value if conversion failed.
		/// </summary>
		public static bool CanParse<T>(string value)
		{
			T result;
			return TryParse(value, out result);
		}

		public static bool IsNullable(Type t)
		{
			// Throw exception if type not supplied.
			if (t is null)
				throw new ArgumentNullException(nameof(t));
			// Special Handling - known cases where Exceptions would be thrown
			else if (t == typeof(void))
				throw new Exception("There is no Nullable version of void");
			// If this is not a value type, it is a reference type, so it is automatically nullable.
			// (NOTE: All forms of Nullable<T> are value types)
			if (!t.IsValueType)
				return true;
			// Return true if underlying Type exists (this is faster than line above).
			return Nullable.GetUnderlyingType(t) != null;
		}

		#endregion

		/// <summary>
		/// Convert year values from 0 to 99 to the years xx00 to yy99 with appropriate century. 
		/// </summary>
		/// <param name="year">A two-digit or four-digit integer that represents the year to convert.</param>
		/// <param name="twoDigitYearMax">The last year of a 100-year range that can be represented by a 2-digit year.</param>
		/// <returns>An integer that contains the four-digit representation of year.</returns>
		/// <remarks>
		/// Use C# solution from System.Globalization.Calendar.ToFourDigitYear(System.Int32 year).
		/// For example, if TwoDigitYearMax is set to 2029, the 100-year range is from 1930 to 2029:
		///   a 2-digit value of 30 is interpreted as 1930
		///   a 2-digit value of 29 is interpreted as 2029
		/// </remarks>
		public static int ToFourDigitYear(int year, int? twoDigitYearMax = null)
		{
			// If year is outside of the range then return.
			if (year < 0 && year > 99)
				return year;
			// Convert year from 2 to 4 digits with the correct century (-50/+50 rule).
			// JavaScript: By default, new Date(..) convert values from 0 to 99 to the years 1900 to 1999.
			//var twoDigitYearMax = new Date().getFullYear() + 50;
			var fullYearMax = twoDigitYearMax ?? DateTime.Now.Year + 50;
			var yearMax = fullYearMax % 100;
			var century = (fullYearMax - yearMax) / 100;
			var fullYear = (century - (year > yearMax ? 1 : 0)) * 100 + year;
			return fullYear;
		}

	}
}
