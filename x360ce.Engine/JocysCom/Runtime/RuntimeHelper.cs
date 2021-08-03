using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	public static partial class RuntimeHelper
	{

		public static bool IsKnownType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return
				type == typeof(string)
				|| type.IsPrimitive
				|| type.IsSerializable;
		}

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
			if (type == null)
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
			if (type == null)
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
		private static FieldInfo[] GetItersectingFields(object source, object dest)
		{
			var dFieldNames = dest.GetType().GetFields(DefaultBindingFlags).Select(x => x.Name).ToArray();
			var itersectingFields = source
				.GetType()
				.GetFields(DefaultBindingFlags)
				.Where(x => dFieldNames.Contains(x.Name))
				.ToArray();
			return itersectingFields;
		}

		public static void CopyFields(object source, object dest)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			// Get type of the destination object.
			var destType = dest.GetType();
			// Copy fields.
			var sourceItersectingFields = GetItersectingFields(source, dest);
			foreach (var sfi in sourceItersectingFields)
			{
				if (IsKnownType(sfi.FieldType))
				{
					var dfi = destType.GetField(sfi.Name, DefaultBindingFlags);
					dfi.SetValue(dest, sfi.GetValue(source));
				}
			}
		}

		#endregion

		#region Copy Properties

		private static readonly object PropertiesReadLock = new object();
		private static readonly Dictionary<Type, PropertyInfo[]> PropertiesReadList = new Dictionary<Type, PropertyInfo[]>();
		private static readonly object PropertiesWriteLock = new object();
		private static readonly Dictionary<Type, PropertyInfo[]> PropertiesWriteList = new Dictionary<Type, PropertyInfo[]>();

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
		/// Get properties which exists on both objects.
		/// </summary>
		static PropertyInfo[] GetItersectingProperties(object source, object dest)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			// Properties to read.
			PropertyInfo[] sProperties;
			lock (PropertiesReadLock)
			{
				var sType = source.GetType();
				if (PropertiesReadList.ContainsKey(sType))
				{
					sProperties = PropertiesReadList[sType];
				}
				else
				{
					sProperties = sType.GetProperties(DefaultBindingFlags)
						.Where(p => p.CanRead)
						.ToArray();
					PropertiesReadList.Add(sType, sProperties);
				}
			}
			// Properties to write.
			PropertyInfo[] dProperties;
			lock (PropertiesWriteLock)
			{
				var dType = dest.GetType();
				if (PropertiesWriteList.ContainsKey(dType))
				{
					dProperties = PropertiesWriteList[dType];
				}
				else
				{
					dProperties = dType.GetProperties(DefaultBindingFlags)
						.Where(p => p.CanWrite)
						.ToArray();
					PropertiesWriteList.Add(dType, dProperties);
				}
			}
			var dPropertyNames = dProperties.Select(x => x.Name).ToArray();
			var itersectingProperties = sProperties
				.Where(x => dPropertyNames.Contains(x.Name))
				.ToArray();
			return itersectingProperties;
		}

		public static void CopyProperties(object source, object dest)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			// Get type of the destination object.
			var destType = dest.GetType();
			// Copy properties.
			var sourceItersectingProperties = GetItersectingProperties(source, dest);
			foreach (var spi in sourceItersectingProperties)
			{
				// Skip if can't read.
				if (!spi.CanRead)
					continue;
				if (!IsKnownType(spi.PropertyType))
					continue;
				// Get destination type.
				var dpi = destType.GetProperty(spi.Name, DefaultBindingFlags);
				// Skip if can't write.
				if (!dpi.CanWrite)
					continue;
				// Get source value.
				var sValue = spi.GetValue(source, null);
				var update = true;
				// If can read destination.
				if (dpi.CanRead)
				{
					// Get destination value.
					var dValue = dpi.GetValue(dest, null);
					// Update only if values are different.
					update = !Equals(sValue, dValue);
				}
				if (update)
					dpi.SetValue(dest, sValue, null);
			}
		}

		#endregion

		public static object CloneObject(object o)
		{
			if (o == null)
				throw new ArgumentNullException(nameof(o));
			var t = o.GetType();
			var properties = t.GetProperties();
			var dest = t.InvokeMember("", BindingFlags.CreateInstance, null, o, null);
			foreach (var pi in properties)
			{
				if (pi.CanWrite)
					pi.SetValue(dest, pi.GetValue(o, null), null);
			}
			return dest;
		}

		/// <summary>
		/// Assign property values from their [DefaultValueAttribute] value.
		/// </summary>
		/// <param name="o">Object to reset properties on.</param>
		public static void ResetPropertiesToDefault(object o, bool onlyIfNull = false)
		{
			if (o == null)
				return;
			var type = o.GetType();
			var properties = type.GetProperties();
			foreach (var p in properties)
			{
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
			if (type == null)
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
		public static bool TryParse(string value, Type t, out object result)
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
				var retValue = value == null ? false : Enum.IsDefined(t, value);
				result = retValue ? Enum.Parse(t, value) : default;
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
			if (t == null)
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

		public class DetectTypeItem
		{
			public string Name { get; set; }
			public Type Type { get; set; }
			public int MinLength { get; set; }
			public int MaxLength { get; set; }
			public int DecimalPrecision { get; set; }
			public int DecimalScale { get; set; }
			public bool IsAscii { get; set; }
			public bool IsRequired { get; set; }
			public List<string> Log { get; set; }
		}


		public static DetectTypeItem DetectType(string[] values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));
			var log = new List<string>();
			var type = typeof(string);
			var sizeMin = int.MaxValue;
			var sizeMax = 0;
			var decimalPrecision = 0;
			var decimalScale = 0;
			var isAscii = true;
			var haveEmpty = false;
			// Order matters. Strictest on the top. First available type will be returned.
			// If all values can be parsed to Int16 then it can be parsed to Int32 and Int64 too.
			var tcs = new TypeCode[]
			{
				TypeCode.Boolean,
				TypeCode.Byte,
				TypeCode.SByte,
				TypeCode.Int16,
				TypeCode.Int32,
				TypeCode.Int64,
				TypeCode.UInt16,
				TypeCode.UInt32,
				TypeCode.UInt64,
				TypeCode.Single,
				TypeCode.Char,
				TypeCode.DateTime,
				TypeCode.Double,
				TypeCode.Decimal,
				TypeCode.String,
				// TypeCode.DBNull,
				// TypeCode.Empty,
				// TypeCode.Object,
			}.ToList();
			// All available types.
			var available = new Dictionary<TypeCode, Type>();
			tcs.ForEach(x => available.Add(x, Type.GetType(nameof(System) + "." + x)));
			//Convert.ChangeType(value, colType);
			for (int i = 0; i < values.Length; i++)
			{
				var value = values[i];
				if (string.IsNullOrEmpty(value))
				{
					haveEmpty = true;
					continue;
				}
				// Determine string limits.
				sizeMin = Math.Min(sizeMin, value.Length);
				sizeMax = Math.Max(sizeMax, value.Length);
				isAscii &= value.All(x => x < 128);
				// Get available types to test against.
				var availableTypeCodes = available.Keys.ToArray();
				// If only string was left.
				if (availableTypeCodes.Length == 1 && availableTypeCodes[0] == TypeCode.String)
					break;
				// Test against available types.
				foreach (var tc in availableTypeCodes)
				{
					switch (tc)
					{
						case TypeCode.Boolean:
							bool resultBool;
							if (!bool.TryParse(value, out resultBool))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Byte:
							byte resultByte;
							if (!byte.TryParse(value, out resultByte))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Char:
							char resultChar;
							if (!char.TryParse(value, out resultChar))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.DateTime:
							DateTime resultDateTime;
							if (!DateTime.TryParse(value, out resultDateTime))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Decimal:
							decimal resultDecimal;
							if (!decimal.TryParse(value, out resultDecimal))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							var d = (System.Data.SqlTypes.SqlDecimal)resultDecimal;
							decimalPrecision = Math.Max(decimalPrecision, d.Precision);
							decimalScale = Math.Max(decimalScale, d.Scale);
							break;
						case TypeCode.Double:
							double resultDouble;
							if (!double.TryParse(value, out resultDouble))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Int16:
							short resultShort;
							if (!short.TryParse(value, out resultShort))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Int32:
							int resultInt;
							if (!int.TryParse(value, out resultInt))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Int64:
							long resultLong;
							if (!long.TryParse(value, out resultLong))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.SByte:
							sbyte resultSByte;
							if (!sbyte.TryParse(value, out resultSByte))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.Single:
							float resultFloat;
							if (!float.TryParse(value, out resultFloat))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.UInt16:
							ushort resultUShort;
							if (!ushort.TryParse(value, out resultUShort))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.UInt32:
							uint resultUInt;
							if (!uint.TryParse(value, out resultUInt))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						case TypeCode.UInt64:
							ulong resultULong;
							if (!ulong.TryParse(value, out resultULong))
							{
								log.Add(string.Format($"Removed {tc,-8} at {i,4} line. Value: {value}"));
								available.Remove(tc);
							}
							break;
						default:
							break;
					}
				}
			}
			type = available.FirstOrDefault().Value;
			var item = new DetectTypeItem()
			{
				Type = type,
				MinLength = sizeMin,
				MaxLength = sizeMax,
				IsAscii = isAscii,
				DecimalPrecision = decimalPrecision,
				DecimalScale = decimalScale,
				IsRequired = !haveEmpty,
				Log = log,
			};
			return item;
		}

	}
}
