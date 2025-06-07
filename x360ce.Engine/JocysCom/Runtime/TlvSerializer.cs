using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace JocysCom.ClassLibrary.Runtime
{
	// TLV Message bytes:
	// ObjectType[0-5] + ObjectLength[0-5] + 
	//     // ObjectValue[0-int.Max]
	// 	   [PropertyTag[0-5] + PropertyLength[0-5] + PropertyValue[0-int.Max]
	//	   ...
	//	   [PropertyTag[0-5] + PropertyLength[0-5] + PropertyValue[0-int.Max]
	//
	public class TlvSerializer : TlvSerializer<int>
	{
		public TlvSerializer(Dictionary<Type, int> types = null) : base(types) { }

	}

	public class TlvSerializer<Te>
		// Declare TE as same as Enum.
		where Te : struct, IComparable, IFormattable, IConvertible
	{

		//public static TlvSerializer<T> Create<T>(Dictionary<Type, T> types = null) where T : struct
		//{
		//	return new TlvSerializer<T>(types);
		//}

		public TlvSerializer(Dictionary<Type, Te> types = null)
		{
			Types = types;
			MemberInfos = new Dictionary<Type, List<MemberInfo>>();
			MemberTags = new Dictionary<Type, List<int>>();
		}

		/// <summary>Map integer/enumeration to type of serializable object </summary>
		Dictionary<Type, Te> Types;
		/// <summary>Cache properties of type which will be serialized.</summary>
		Dictionary<Type, List<MemberInfo>> MemberInfos;
		Dictionary<Type, List<int>> MemberTags;

		static Encoding CurrentEncoding = Encoding.UTF8;

		#region Serialize / Deserialize

		/// <summary>
		///  Deserializes the TLV bytes contained by the specified System.IO.Stream.
		/// </summary>
		/// <param name="stream">The System.IO.Stream that contains the TLV bytes to deserialize.</param>
		/// <returns>The System.Object being deserialized.</returns>
		public TlvSerializerError Deserialize(Stream stream, out object result)
		{
			result = null;
			int typeI;
			byte[] value;
			// Read header.
			var error = ReadTlv(stream, out typeI, out value);
			if (error != TlvSerializerError.None)
				return error;
			// Get type from type number.
			var typeE = (Te)(object)typeI;
			if (!Types.ContainsValue(typeE))
				return TlvSerializerError.EnumIdNotFound;
			var typeT = Types.First(x => x.Value.Equals(typeE)).Key;
			var o = Activator.CreateInstance(typeT);
			// Read properties from stream.
			var infos = MemberInfos[typeT];
			var tags = MemberTags[typeT];
			int tag;
			UpdateMembersCache(typeT);
			var membersStream = new MemoryStream(value);
			byte[] mBytes;
			object mValue;
			while (membersStream.Position < value.Length)
			{
				error = ReadTlv(membersStream, out tag, out mBytes);
				if (error != TlvSerializerError.None)
					return error;
				var index = tags.IndexOf(tag);
				var info = infos[index];
				switch (info.MemberType)
				{
					case MemberTypes.Field:
						var fi = (FieldInfo)info;
						var fiStatus = BytesToObject(mBytes, fi.FieldType, out mValue);
						if (fiStatus != TlvSerializerError.None)
						{
							return fiStatus;
						}
						fi.SetValue(o, mValue);
						break;
					case MemberTypes.Property:
						var pi = (PropertyInfo)info;
						var piStatus = BytesToObject(mBytes, pi.PropertyType, out mValue);
						if (piStatus != TlvSerializerError.None)
						{
							return piStatus;
						}
						pi.SetValue(o, mValue);
						break;
					default:
						break;
				}
			}
			result = o;
			return TlvSerializerError.None;
		}

		public TlvSerializerError ReadTlv(Stream stream, out int tag, out byte[] value)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			value = null;
			TlvSerializerError error;
			// Read header bytes.
			error = Read7BitEncoded(stream, out tag);
			if (error != TlvSerializerError.None)
				return error;
			int length;
			error = Read7BitEncoded(stream, out length);
			if (error != TlvSerializerError.None)
				return error;
			value = new byte[length];
			stream.Read(value, 0, length);
			return TlvSerializerError.None;
		}

		/// <summary>
		/// Serializes the specified System.Object and writes the TLV bytes to the specified System.IO.Stream.
		/// </summary>
		/// <param name="stream">The System.IO.Stream used to write the TLV bytes.</param>
		/// <param name="o">The System.Object to serialize.</param>
		public TlvSerializerError Serialize(Stream stream, object o)
		{
			if (o is null)
				return TlvSerializerError.None;
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			var typeT = o.GetType();
			// Write Object Type.
			if (!Types.ContainsKey(typeT))
				return TlvSerializerError.TypeIdNotFound;
			var typeE = Types.First(x => x.Key == typeT).Value;
			Write7BitEncoded(stream, (int)(object)typeE);
			UpdateMembersCache(typeT);
			var infos = MemberInfos[typeT];
			var tags = MemberTags[typeT];
			var membersStream = new MemoryStream();
			for (int i = 0; i < infos.Count; i++)
			{
				var info = infos[i];
				var tag = tags[i];
				object mValue = null;
				Type mType = null;
				byte[] mBytes = null;
				switch (info.MemberType)
				{
					case MemberTypes.Field:
						var fi = (FieldInfo)info;
						mType = fi.FieldType;
						mValue = fi.GetValue(o);
						break;
					case MemberTypes.Property:
						var pi = (PropertyInfo)info;
						mType = pi.PropertyType;
						mValue = pi.GetValue(o, null);
						break;
					default:
						break;
				}
				var status2 = ObjectToBytes(mValue, mType, out mBytes);
				if (status2 != TlvSerializerError.None)
					return status2;
				// Do not serialize null objects.
				if (mBytes is null)
					continue;
				// Write Member Tag.
				Write7BitEncoded(membersStream, tag);
				// Write Member Length.
				Write7BitEncoded(membersStream, mBytes.Length);
				// Write Member Value.
				membersStream.Write(mBytes, 0, mBytes.Length);
			}
			var data = membersStream.ToArray();
			// Write Object Length.
			Write7BitEncoded(stream, data.Length);
			// Write Object Value.
			stream.Write(data, 0, data.Length);
			return TlvSerializerError.None;
		}

		#endregion

		#region 7-bit Encoder/Decoder

		/// <summary>
		/// Write an Int32, 7 bits at a time.
		/// </summary>
		/// <param name="value"></param>
		/// <remarks>
		/// Decimal: 1,259,551,277
		/// Remaining integer                 encoded bytes
		/// 1001011000100110011101000101101
		/// 100101100010011001110100          00101101
		/// 10010110001001100                 10101101 01110100
		/// 1001011000                        10101101 11110100 01001100
		/// 100                               10101101 11110100 11001100 01011000
		/// 0                                 10101101 11110100 11001100 11011000 00000100
		/// 
		/// </remarks>
		public static void Write7BitEncoded(Stream stream, int value)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			int v = value;
			byte b;
			do
			{
				// Store 7 bits.
				b = (byte)(v & 0x7F);
				// Shift by 7 bits.
				v >>= 7;
				// If more bits left then...
				if (v > 0)
					// set first bit to 1.
					b |= 0x80;
				stream.WriteByte(b);
			}
			// Continue if more bits left.
			while (v > 0);
		}

		/// <summary>
		/// Read an Int32, 7 bits at a time.
		/// </summary>
		public static TlvSerializerError Read7BitEncoded(Stream stream, out int result)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			result = 0;
			int v = 0;
			int b;
			var i = 0;
			do
			{
				// Read byte.
				b = stream.ReadByte();
				// if end of stream and no more bytes to read then...
				if (b == -1)
					return TlvSerializerError.Decoder7BitStreamIsTooShortError;
				if (i == 4 && b > 0xF)
					return TlvSerializerError.Decoder7BitNumberIsTooLargeError;
				// Add 7 bit value
				v |= (b & 0x7F) << (7 * i);
				i++;
			}
			// Continue if first bit is 1.
			while (b >> 7 == 1);
			result = v;
			return TlvSerializerError.None;
		}

		#endregion

		#region Convert Functions

		public static bool IsNullable(Type t)
		{
			// Throw exception if type not supplied.
			if (t is null) throw new ArgumentNullException(nameof(t));
			// Special Handling - known cases where Exceptions would be thrown
			else if (t == typeof(void)) throw new Exception("There is no Nullable version of void");
			// If this is not a value type, it is a reference type, so it is automatically nullable.
			// (NOTE: All forms of Nullable<T> are value types)
			if (!t.IsValueType) return true;
			// Return true if underlying Type exists (this is faster than line above).
			return Nullable.GetUnderlyingType(t) != null;
		}

		/// <summary>
		/// Returns value if property have fixed size (used by BinaryWriter/BinaryReader).
		/// Can't use Marshal.SizeOf(type), because it returns size of unmanaged type:
		/// 4 for boolean on 32-bit system and 8 on 64-bit system.
		/// </summary>
		public static int[] GetPropertySizes(Type type, out TypeCode typeCode)
		{
			var underType = GetUnderlyingType(type);
			typeCode = Type.GetTypeCode(underType);
			switch (typeCode)
			{
				case TypeCode.Boolean: return new[] { 1 };
				case TypeCode.Char: return new[] { 1, 2 };
				case TypeCode.DateTime: return new[] { 8 };
				case TypeCode.Decimal: return new[] { 16 };
				case TypeCode.Double: return new[] { 8 };
				case TypeCode.Single: return new[] { 4 };
				case TypeCode.SByte: return new[] { 1 };
				case TypeCode.Int16: return new[] { 1, 2 };
				case TypeCode.Int32: return new[] { 1, 2, 4 };
				case TypeCode.Int64: return new[] { 1, 2, 4, 8 };
				case TypeCode.Byte: return new[] { 1 };
				case TypeCode.UInt16: return new[] { 1, 2 };
				case TypeCode.UInt32: return new[] { 1, 2, 4 };
				case TypeCode.UInt64: return new[] { 1, 2, 4, 8 };
			}
			return new int[0];
		}

		static Type GetUnderlyingType(Type type)
		{
			if (IsNullable(type)) type = Nullable.GetUnderlyingType(type) ?? type;
			if (type.IsEnum) type = Enum.GetUnderlyingType(type);
			return type;
		}

		static Type GetNullableType(Type type)
		{
			// Use Nullable.GetUnderlyingType() to remove the Nullable<T> wrapper if type is already nullable.
			type = Nullable.GetUnderlyingType(type) ?? type;
			return (type.IsValueType) ? typeof(Nullable<>).MakeGenericType(type) : type;
		}

		/// <summary>
		/// Get properties of type to serialize.
		/// </summary>
		/// <param name="type">Type of properties.</param>
		/// <returns>Array of properties.</returns>
		void UpdateMembersCache(Type type)
		{
			if (MemberInfos.ContainsKey(type))
				return;
			var orders = new Dictionary<int, MemberInfo>();
			var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
				.Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property).ToArray();
			for (int i = 0; i < members.Length; i++)
			{
				var pi = members[i];
				var attributes = pi.GetCustomAttributes(typeof(DataMemberAttribute), false);
				// If property/Field is marked with DataMember attribute then...
				if (attributes.Length > 0)
				{
					var attribute = (DataMemberAttribute)attributes[0];
					if (attribute.Order > -1)
					{
						if (orders.ContainsKey(attribute.Order))
						{
							var message = string.Format("Order property on DataMemberAttribute[Order={0}]{1}.{2} must be unique for TLV serialization to work!",
								attribute.Order, type.Name, pi.MemberType);
							throw new Exception();
						}
						orders.Add(attribute.Order, pi);
					}
				}
			}
			// If no members with DataMemberAttribute found then...
			if (orders.Count == 0)
			{
				// Add all.
				MemberInfos.Add(type, members.ToList());
				var tags = new int[members.Length];
				for (int i = 0; i < members.Length; i++)
					tags[i] = i;
				MemberTags.Add(type, tags.ToList());
			}
			else
			{
				var dataMembers = orders.OrderBy(x => x.Key);
				var oInfos = dataMembers.Select(x => x.Value).ToArray();
				var oTags = dataMembers.Select(x => x.Key).ToArray();
				MemberInfos.Add(type, oInfos.ToList());
				MemberTags.Add(type, oTags.ToList());
			}
		}

		#endregion

		#region Bytes <-> Object

		/// <summary>
		/// Convert byte array to object.
		/// </summary>
		/// <remarks>byte[0] is empty/default value.</remarks>
		public TlvSerializerError BytesToObject(byte[] bytes, Type type, out object result)
		{
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			// Return empty values for nullable types.
			if (bytes.Length == 0)
			{
				if (type == typeof(string))
				{
					result = string.Empty;
					return TlvSerializerError.None;
				}
				if (type == typeof(System.DBNull))
				{
					result = DBNull.Value;
					return TlvSerializerError.None;
				}
				bytes = null;
			}
			// Return null or default value.
			bool isNullable = IsNullable(type);
			if (bytes is null)
			{
				result = isNullable
					? null
					: Activator.CreateInstance(type);
				return TlvSerializerError.None;
			}
			Type typeU1 = isNullable ? Nullable.GetUnderlyingType(type) ?? type : type;
			Type typeU2 = typeU1.IsEnum ? Enum.GetUnderlyingType(typeU1) : typeU1;
			TypeCode typeCode = Type.GetTypeCode(typeU2);
			MemoryStream stream = new MemoryStream(bytes);
			BinaryReader reader = new BinaryReader(stream);
			object o;
			switch (typeCode)
			{
				case TypeCode.Boolean: o = reader.ReadBoolean(); break;
				case TypeCode.Char: o = reader.ReadChar(); break;
				case TypeCode.DBNull: o = DBNull.Value; break;
				case TypeCode.DateTime: o = new DateTime(reader.ReadInt64()); break;
				case TypeCode.Decimal: o = reader.ReadDecimal(); break;
				case TypeCode.Double: o = reader.ReadDouble(); break;
				case TypeCode.Empty: o = null; break;
				case TypeCode.SByte: o = System.Convert.ToSByte(ReadSNumber(reader)); break;
				case TypeCode.Int16: o = System.Convert.ToInt16(ReadSNumber(reader)); break;
				case TypeCode.Int32: o = System.Convert.ToInt32(ReadSNumber(reader)); break;
				case TypeCode.Int64: o = System.Convert.ToInt64(ReadSNumber(reader)); break;
				case TypeCode.Single: o = reader.ReadSingle(); break;
				case TypeCode.String: o = CurrentEncoding.GetString(bytes); break;
				case TypeCode.Byte: o = System.Convert.ToByte(ReadUNumber(reader)); break;
				case TypeCode.UInt16: o = System.Convert.ToUInt16(ReadUNumber(reader)); break;
				case TypeCode.UInt32: o = System.Convert.ToUInt32(ReadUNumber(reader)); break;
				case TypeCode.UInt64: o = System.Convert.ToUInt64(ReadUNumber(reader)); break;
				case TypeCode.Object:
					if (typeU2.Equals(typeof(object)))
					{
						// bytes[0] will be sent.
						o = new object();
					}
					else if (typeU2.Equals(typeof(byte[])))
					{
						byte[] bo = new byte[bytes.Length];
						Array.Copy(bytes, bo, bo.Length);
						o = bo;
					}
					//else if (IsISocketMessage(type))
					//{
					//	var item = (Network.Sockets.ISocketMessage)Activator.CreateInstance(type);
					//	string error;
					//	item.FromBytes(bytes, 0, out error);
					//	if (!string.IsNullOrEmpty(error)) throw new Exception(error);
					//	o = item;
					//}
					//else if (IsISocketArrayMessage(type))
					//{
					//	int startIndex = 0;
					//	var list = new List<Network.Sockets.ISocketMessage>();
					//	Network.Sockets.ISocketMessage item;
					//	Type itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
					//	while (startIndex < bytes.Length)
					//	{
					//		item = (Network.Sockets.ISocketMessage)Activator.CreateInstance(itemType);
					//		string error;
					//		startIndex += item.FromBytes(bytes, startIndex, out error);
					//		if (!string.IsNullOrEmpty(error)) throw new Exception(error);
					//		list.Add(item);
					//	}
					//	if (type.IsArray)
					//	{
					//		object oArray = Activator.CreateInstance(type, new object[] { list.Count });
					//		//MethodInfo resize = type.GetMethod("Resize");
					//		//resize.Invoke(oList, new object[] { list.Count });
					//		Array.Copy(list.ToArray(), (Array)oArray, list.Count);
					//		o = oArray;
					//	}
					//	else
					//	{
					//		var oList = Activator.CreateInstance(type);
					//		var add = type.GetMethod("Add");
					//		foreach (var listItem in list)
					//		{
					//			add.Invoke(oList, new object[] { listItem });
					//		}
					//		o = oList;
					//	}
					//}
					else
					{
						var ms2 = new MemoryStream();
						object o2;
						var status2 = Deserialize(ms2, out o2);
						if (status2 != TlvSerializerError.None)
						{
							result = null;
							return status2;
						}
						o = o2;
					}
					break;
				default: throw new Exception("Unknown Type: " + typeU2.Name);
			}
			if (typeU1.IsEnum)
				o = Enum.ToObject(typeU1, o);
			result = o;
			return TlvSerializerError.None;
		}

		static T? ToNullable<T>(T value) where T : struct
		{
			return (T?)value;
		}

		public static bool IsNullOrDefault(object o)
		{
			if (o is null) return true;
			Type type = o.GetType();
			bool isNullable = IsNullable(type);
			Type typeU1 = isNullable ? Nullable.GetUnderlyingType(type) ?? type : type;
			Type typeU2 = typeU1.IsEnum ? Enum.GetUnderlyingType(typeU1) : typeU1;
			// If object is not nullable and supplied value is same as default value then...
			if (!isNullable && typeU2.IsValueType && Activator.CreateInstance(typeU1).Equals(o))
			{
				// Don't send data if default value is same.
				return true;
			}
			return false;
		}

		/// <summary>
		/// Convert object to byte array.
		/// </summary>
		public TlvSerializerError ObjectToBytes(object o, Type declaringType, out byte[] bytes)
		{
			bytes = null;
			if (o is null)
				return TlvSerializerError.None;
			var type = declaringType; //  o.GetType();
			var isNullable = IsNullable(type);
			var typeU1 = isNullable ? Nullable.GetUnderlyingType(type) ?? type : type;
			var typeU2 = typeU1.IsEnum ? Enum.GetUnderlyingType(typeU1) : typeU1;
			// If object can't be null and supplied value is the same as default value then don't send any data.
			if (!isNullable && typeU2.IsValueType && Activator.CreateInstance(typeU1).Equals(o))
				return TlvSerializerError.None;
			var typeCode = Type.GetTypeCode(typeU2);
			// SUPPRESS: CWE-404: Improper Resource Shutdown or Release
			// Note: Binary Writer will close underlying MemoryStream automatically.
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);
			byte[] objectBytes = null;
			switch (typeCode)
			{
				case TypeCode.Boolean: writer.Write((Boolean)o); break;
				case TypeCode.Byte: writer.Write((Byte)o); break;
				case TypeCode.Char: writer.Write((Char)o); break;
				case TypeCode.DBNull: break; // byte[0] will be returned.
				case TypeCode.DateTime: writer.Write(((DateTime)o).Ticks); break; // Ticks type is Int64.
				case TypeCode.Decimal: writer.Write((Decimal)o); break;
				case TypeCode.Double: writer.Write((Double)o); break;
				case TypeCode.Empty: break; // byte[0] will be returned.
				case TypeCode.Int16: WriteSNumber(writer, (Int16)o); break;
				case TypeCode.Int32: WriteSNumber(writer, (Int32)o); break;
				case TypeCode.Int64: WriteSNumber(writer, (Int64)o); break;
				case TypeCode.SByte: writer.Write((SByte)o); break;
				case TypeCode.Single: writer.Write((Single)o); break;
				case TypeCode.String: writer.Write(CurrentEncoding.GetBytes((string)o)); break;
				case TypeCode.UInt16: WriteUNumber(writer, (UInt16)o); break;
				case TypeCode.UInt32: WriteUNumber(writer, (UInt32)o); break;
				case TypeCode.UInt64: WriteUNumber(writer, (UInt64)o); break;
				case TypeCode.Object:
					if (typeU2.Equals(typeof(object)))
					{
						// bytes[0] will be sent.
					}
					else if (typeU2.Equals(typeof(byte[])))
					{
						var bytes2 = (byte[])o;
						var value = new byte[bytes2.Length];
						Array.Copy(bytes2, value, value.Length);
						objectBytes = value;
					}
					//else if (IsISocketMessage(type))
					//{
					//	var item = (Network.Sockets.ISocketMessage)o;
					//	objectBytes = item.ToBytes();
					//}
					//else if (IsISocketArrayMessage(type))
					//{
					//	var array = (IEnumerable<Network.Sockets.ISocketMessage>)o;
					//	var bytes = new byte[0];
					//	foreach (Network.Sockets.ISocketMessage item in array)
					//	{
					//		byte[] itemBytes = item.ToBytes();
					//		var destinationIndex = bytes.Length;
					//		Array.Resize(ref bytes, bytes.Length + itemBytes.Length);
					//		Array.Copy(itemBytes, 0, bytes, destinationIndex, itemBytes.Length);
					//	}
					//	objectBytes = bytes;
					//}
					else
					{
						var mso = new MemoryStream();
						var status2 = Serialize(mso, o);
						if (status2 != TlvSerializerError.None)
						{
							mso.Dispose();
							return status2;
						}
						objectBytes = mso.ToArray();
						mso.Dispose();
						//return TlvSerializerError.NonSerializableObject;
						//throw new Exception("Non Serializable Object: " + type.Name);
					}
					break;
				default:
					return TlvSerializerError.UnknownType;
					//throw new Exception("Unknown Type: " + type.Name);
			}
			var result = objectBytes is null
				? stream.ToArray()
				: objectBytes;
			// Binary Writer will close underlying MemoryStream automatically.
			writer.Close();
			//stream.Close();
			bytes = result;
			return TlvSerializerError.None;
		}

		//public static bool IsISocketMessage(Type t)
		//{
		//	Type[] interfaces = t.GetInterfaces();
		//	foreach (Type type in interfaces)
		//	{
		//		if (type == typeof(Network.Sockets.ISocketMessage))
		//			return true;
		//	}
		//	return false;
		//}

		//static bool IsISocketArrayMessage(Type t)
		//{
		//	Type[] interfaces = t.GetInterfaces();
		//	foreach (Type interfaceType in interfaces)
		//	{
		//		if (interfaceType == typeof(System.Collections.IEnumerable))
		//		{
		//			if (t.IsArray && IsISocketMessage(t.GetElementType()))
		//				return true;
		//			else if (IsISocketMessage(t.GetGenericArguments()[0]))
		//				return true;
		//		}
		//	}
		//	return false;
		//}

		/// <summary>
		/// Reads signed numbers from byte arrays smaller than required.
		/// </summary>
		static object ReadSNumber(BinaryReader reader)
		{
			var length = reader.BaseStream.Length;
			if (length == 1) return reader.ReadSByte();
			if (length == 2) return reader.ReadInt16();
			if (length == 4) return reader.ReadInt32();
			else return reader.ReadInt64();
		}

		/// <summary>
		/// Reads unsigned numbers from byte arrays smaller than required.
		/// </summary>
		static object ReadUNumber(BinaryReader reader)
		{
			long length = reader.BaseStream.Length;
			if (length == 1) return reader.ReadByte();
			if (length == 2) return reader.ReadUInt16();
			if (length == 4) return reader.ReadUInt32();
			else return reader.ReadUInt64();
		}

		/// <summary>
		/// WriteUNumber
		/// </summary>
		static void WriteUNumber(BinaryWriter writer, UInt64 v)
		{
			if (v <= Byte.MaxValue) writer.Write((Byte)v);
			else if (v <= UInt16.MaxValue) writer.Write((UInt16)v);
			else if (v <= UInt32.MaxValue) writer.Write((UInt32)v);
			else writer.Write(v);
		}

		/// <summary>
		/// WriteSNumber
		/// </summary>
		static void WriteSNumber(BinaryWriter writer, Int64 v)
		{
			if (v >= SByte.MinValue && v <= SByte.MaxValue) writer.Write((SByte)v);
			else if (v >= Int16.MinValue && v <= Int16.MaxValue) writer.Write((Int16)v);
			else if (v >= Int32.MinValue && v <= Int32.MaxValue) writer.Write((Int32)v);
			else writer.Write(v);
		}

		#endregion

	}
}
