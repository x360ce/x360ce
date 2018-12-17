using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Runtime.Serialization;
using System.Data.Objects.DataClasses;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.Runtime
{
	public partial class RuntimeHelper
	{

		public static bool IsKnownType(Type type)
		{
			return
				type == typeof(string)
				|| type.IsPrimitive
				|| type.IsSerializable;
		}

		static readonly HashSet<Type> numericTypes = new HashSet<Type>
		{
			typeof(int),  typeof(double),  typeof(decimal),
			typeof(long), typeof(short),   typeof(sbyte),
			typeof(byte), typeof(ulong),   typeof(ushort),
			typeof(uint), typeof(float),   //typeof(BigInteger)
		};

		public static bool IsNumeric(Type type)
		{
			return numericTypes.Contains(type);
		}

		//public static bool IsNumeric<T>(T item)
		//{
		//	return item == null
		//	? false
		//	: numericTypes.Contains(item.GetType());
		//}

		private static Type GetFirstArgumentOfGenericType(Type type)
		{
			return type.GetGenericArguments()[0];
		}

		public static bool IsNullableType(Type type)
		{
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
			string[] dFieldNames = dest.GetType().GetFields(DefaultBindingFlags).Select(x => x.Name).ToArray();
			FieldInfo[] itersectingFields = source
				.GetType()
				.GetFields(DefaultBindingFlags)
				.Where(x => dFieldNames.Contains(x.Name))
				.ToArray();
			return itersectingFields;
		}

		public static void CopyFields(object source, object dest)
		{
			// Get type of the destination object.
			Type destType = dest.GetType();
			// Copy fields.
			FieldInfo[] sourceItersectingFields = GetItersectingFields(source, dest);
			foreach (FieldInfo sfi in sourceItersectingFields)
			{
				if (IsKnownType(sfi.FieldType))
				{
					FieldInfo dfi = destType.GetField(sfi.Name, DefaultBindingFlags);
					dfi.SetValue(dest, sfi.GetValue(source));
				}
			}
		}

		#endregion

		#region Copy Properties

		static object PropertiesReadLock = new object();
		static Dictionary<Type, PropertyInfo[]> PropertiesReadList = new Dictionary<Type, PropertyInfo[]>();
		static object PropertiesWriteLock = new object();
		static Dictionary<Type, PropertyInfo[]> PropertiesWriteList = new Dictionary<Type, PropertyInfo[]>();

		/// <summary>
		/// Get properties which exists on both objects.
		/// </summary>
		private static PropertyInfo[] GetItersectingProperties(object source, object dest)
		{
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
			// Get type of the destination object.
			Type destType = dest.GetType();
			// Copy properties.
			PropertyInfo[] sourceItersectingProperties = GetItersectingProperties(source, dest);
			foreach (PropertyInfo spi in sourceItersectingProperties)
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

		#region Copy Properties with DataMember attribute

		static object DataMembersLock = new object();
		static Dictionary<Type, PropertyInfo[]> DataMembers = new Dictionary<Type, PropertyInfo[]>();
		static Dictionary<Type, PropertyInfo[]> DataMembersNoKey = new Dictionary<Type, PropertyInfo[]>();

		static PropertyInfo[] GetDataMemberProperties<T>(T item, bool skipKey = false)
		{
			PropertyInfo[] ps;
			Type t = item == null ? typeof(T) : item.GetType();
			lock (DataMembersLock)
			{
				var cache = skipKey ? DataMembersNoKey : DataMembers;
				if (cache.ContainsKey(t))
				{
					ps = cache[t];
				}
				else
				{
					var items = t.GetProperties(DefaultBindingFlags | BindingFlags.DeclaredOnly)
						.Where(p => p.CanRead && p.CanWrite)
						.Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)));
					if (skipKey)
					{
						var keys = items
							.Where(p => Attribute.IsDefined(p, typeof(EdmScalarPropertyAttribute)))
							.Where(p => ((EdmScalarPropertyAttribute)Attribute.GetCustomAttribute(p, typeof(EdmScalarPropertyAttribute))).EntityKeyProperty);
						items = items.Except(keys);
					}
					ps = items
						// Order properties by name so list will change less with the code changed (important for checksums)
						.OrderBy(x => x.Name)
						.ToArray();
					cache.Add(t, ps);
				}
			}
			return ps;
		}

		/// <summary>
		/// Copy properties with [DataMemberAttribute].
		/// Only members declared at the level of the supplied type's hierarchy should be copied.
		/// Inherited members are not copied.
		/// </summary>
		public static void CopyDataMembers<T>(T source, T dest, bool skipKey = false)
		{
			Type t = source == null ? typeof(T) : source.GetType();
			PropertyInfo[] ps = GetDataMemberProperties(source, skipKey);
			foreach (PropertyInfo p in ps)
			{
				var sValue = p.GetValue(source, null);
				var dValue = p.GetValue(dest, null);
				// If values are different then...
				if (!Equals(sValue, dValue))
					p.SetValue(dest, sValue, null);
			}
		}

		/// <summary>
		/// Get string representation of [DataMemberAttribute].
		/// Inherited members are not included.
		/// </summary>
		public static string GetDataMembersString<T>(T item, bool skipKey = true, bool skipTime = true)
		{
			Type t = item == null ? typeof(T) : item.GetType();
			PropertyInfo[] ps = GetDataMemberProperties(item, skipKey);
			StringBuilder sb = new StringBuilder();
			foreach (PropertyInfo p in ps)
			{
				var value = p.GetValue(item, null);
				if (value == null)
					continue;
				var defaultValue = p.PropertyType.IsValueType ? Activator.CreateInstance(p.PropertyType) : null;
				if (Equals(defaultValue, value))
					continue;
				if (skipTime && p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
					continue;
				if (p.PropertyType == typeof(string) && string.IsNullOrWhiteSpace((string)value))
					continue;
				if (p.Name.ToLower().Contains("checksum"))
					continue;
				sb.AppendFormat("{0}={1}", p.Name, value);
				sb.AppendLine();
			}
			return sb.ToString();
		}

		#endregion

		public static object CloneObject(object o)
		{
			Type t = o.GetType();
			PropertyInfo[] properties = t.GetProperties();
			object dest = t.InvokeMember("", BindingFlags.CreateInstance, null, o, null);
			foreach (PropertyInfo pi in properties)
			{
				if (pi.CanWrite) pi.SetValue(dest, pi.GetValue(o, null), null);
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

		/// <summary>
		/// Get change state by comparing old and new values.
		/// </summary>
		/// <param name="type">Type of values.</param>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		/// <returns></returns>
		public static ChangeState GetValueChangeSet(Type type, object oldValue, object newValue)
		{
			ChangeState state = new ChangeState();
			state.ValueType = type;
			state.oldValue = oldValue;
			state.newValue = newValue;
			state.State = System.Data.EntityState.Unchanged;
			if (oldValue != newValue) state.State = System.Data.EntityState.Modified;
			bool oldIsEmpty = oldValue == null || oldValue == Activator.CreateInstance(type);
			bool newIsEmpty = newValue == null || newValue == Activator.CreateInstance(type);
			if (oldIsEmpty && !newIsEmpty) state.State = System.Data.EntityState.Added;
			if (newIsEmpty && !oldIsEmpty) state.State = System.Data.EntityState.Deleted;
			return state;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item1"></param>
		/// <param name="item2"></param>
		/// <returns></returns>
		public static System.Data.EntityState GetClassChangeState(object item1, object item2)
		{
			List<ChangeState> list = RuntimeHelper.CompareProperties(item1, item2);
			EntityState state = EntityState.Unchanged;
			List<EntityState> states = list.Select(x => x.State).Distinct().ToList();
			states.Remove(EntityState.Unchanged);
			if (states.Count == 0) return state;
			if (states.Count == 1) return states[0];
			return EntityState.Modified;
		}

		/// <summary>
		/// Compare all properties of two objects and return change state for every property and field.
		/// </summary>
		/// <param name="source">item1</param>
		/// <param name="dest">item2</param>
		/// <returns></returns>
		public static List<ChangeState> CompareProperties(object source, object dest)
		{
			object oldValue;
			object newValue;
			ChangeState state;
			List<ChangeState> list = new List<ChangeState>();
			Type dstType = dest.GetType();
			FieldInfo[] itersectingFields = GetItersectingFields(source, dest);
			foreach (FieldInfo fi in itersectingFields)
			{
				FieldInfo dp = dstType.GetField(fi.Name);
				if (IsKnownType(fi.FieldType))
				{
					oldValue = fi.GetValue(source);
					newValue = fi.GetValue(dest);
					state = GetValueChangeSet(fi.FieldType, oldValue, newValue);
					list.Add(state);
				}
			}
			PropertyInfo[] itersectingProperties = GetItersectingProperties(source, dest);
			foreach (PropertyInfo pi in itersectingProperties)
			{
				PropertyInfo dp = dstType.GetProperty(pi.Name);
				if (IsKnownType(pi.PropertyType))
				{
					oldValue = pi.GetValue(source, null);
					newValue = pi.GetValue(dest, null);
					state = GetValueChangeSet(pi.PropertyType, oldValue, newValue);
					list.Add(state);
				}
			}
			return list;
		}

		#region Convert: Object <-> Bytes

		// Note: Similar as "Structure <-> Bytes", but with ability to convert variable strings.

		public static byte[] ObjectToBytes<T>(T o)
		{
			using (var ms = new MemoryStream())
			{
				var flags = BindingFlags.Instance | BindingFlags.Public;
				var props = typeof(T).GetProperties(flags);
				var writer = new BinaryWriter(ms);
				foreach (var p in props)
				{
					var value = p.GetValue(o);
					writer.Write((dynamic)value);
				}
				ms.Flush();
				ms.Seek(0, SeekOrigin.Begin);
				return ms.ToArray();
			}
		}

		public static T BytesToObject<T>(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				var o = Activator.CreateInstance<T>();
				var flags = BindingFlags.Instance | BindingFlags.Public;
				var props = typeof(T).GetProperties(flags);
				var reader = new BinaryReader(ms);
				foreach (var p in props)
				{
					var typeCode = Type.GetTypeCode(p.PropertyType);
					object v;
					switch (typeCode)
					{
						case TypeCode.Boolean: v = reader.ReadBoolean(); break;
						case TypeCode.Char: v = reader.ReadChar(); break;
						case TypeCode.DBNull: v = DBNull.Value; break;
						case TypeCode.DateTime: v = new DateTime(reader.ReadInt64()); break;
						case TypeCode.Decimal: v = reader.ReadDecimal(); break;
						case TypeCode.Double: v = reader.ReadDouble(); break;
						case TypeCode.Empty: v = null; break;
						case TypeCode.SByte: v = reader.ReadSByte(); break;
						case TypeCode.Int16: v = reader.ReadInt16(); break;
						case TypeCode.Int32: v = reader.ReadInt32(); break;
						case TypeCode.Int64: v = reader.ReadInt64(); break;
						case TypeCode.Single: v = reader.ReadSingle(); break;
						case TypeCode.String: v = reader.ReadString(); break;
						case TypeCode.Byte: v = reader.ReadByte(); break;
						case TypeCode.UInt16: v = reader.ReadUInt16(); break;
						case TypeCode.UInt32: v = reader.ReadUInt32(); break;
						case TypeCode.UInt64: v = reader.ReadUInt64(); break;
						default: throw new Exception("Non Serializable Object: " + p.PropertyType);
					}
					p.SetValue(o, v);
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
		/// <param name="result">If the conversion was successful, the converted value of type T.</param>
		/// <returns>If value was converted successfully, true; otherwise false.</returns>
		public static bool TryParse<T>(string value, out T result)
		{
			var t = typeof(T);
			if (IsNullable(t))
				t = Nullable.GetUnderlyingType(t) ?? t;
			//var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
			//if (converter.IsValid(value))
			//{
			//	result = (T)converter.ConvertFromString(value);
			//	return true;
			//}
			if (t.IsEnum)
			{
				var retValue = value == null ? false : Enum.IsDefined(t, value);
				result = retValue ? (T)Enum.Parse(t, value) : default(T);
				return retValue;
			}
			var tryParseMethod = t.GetMethod("TryParse",
				BindingFlags.Static | BindingFlags.Public, null,
				new[] { typeof(string), t.MakeByRefType() }, null);
			var parameters = new object[] { value, null };
			var retVal = (bool)tryParseMethod.Invoke(null, parameters);
			result = (T)parameters[1];
			return retVal;
		}

		/// <summary>
		/// Tries to convert the specified string representation of a logical value to
		/// its type T equivalent. Returns default value if conversion failed.
		/// </summary>
		public static T TryParse<T>(string value, T defaultValue = default(T))
		{
			T result = default(T);
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
			if (t == null) throw new ArgumentNullException("t");
			// Special Handling - known cases where Exceptions would be thrown
			else if (t == typeof(void)) throw new Exception("There is no Nullable version of void");
			// If this is not a value type, it is a reference type, so it is automatically nullable.
			// (NOTE: All forms of Nullable<T> are value types)
			if (!t.IsValueType) return true;
			// Return true if underlying Type exists (this is faster than line above).
			return Nullable.GetUnderlyingType(t) != null;
		}


		#endregion

	}
}
