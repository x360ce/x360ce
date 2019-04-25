using System;
using System.IO;
using System.Text;

namespace JocysCom.ClassLibrary.Data
{
	//  ValueType[0-5] + ValueSize[0-5] + ValueData[0-uint.Max]
	public class TsdItem
	{
		public int Tag { get; set; }
		//public int Size { get; set; }
		public byte[] Data { get; set; }

		byte[] ToBytes()
		{
			var ms = new MemoryStream();
			//var br = new System.IO.BinaryReader(ms);
			//var bw = new System.IO.BinaryWriter(ms);
			return null;
		}

		static Encoding CurrentEncoding = Encoding.UTF8;

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
		public static void Write7BitEncoded(Stream stream, uint value)
		{
			uint v = value;
			byte b = 0;
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
		public static TsdError Read7BitEncoded(Stream stream, out uint result)
		{
			result = 0;
			uint v = 0;
			var b = 0;
			var i = 0;
			do
			{
				// Read byte.
				b = stream.ReadByte();
				// if end of stream and no more bytes to read then...
				if (b == -1)
					return TsdError.Decoder7BitStreamIsTooShortError;
				if (i == 4 && b > 0xF)
					return TsdError.Decoder7BitNumberIsTooLargeError;
				// Add 7 bit value
				v |= (uint)(b & 0x7F) << (7 * i);
				i++;
			}
			// Continue if first bit is 1.
			while (b >> 7 == 1);
			result = v;
			return TsdError.None;
		}

		#endregion

		#region Convert Functions

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

		#endregion

		#region Bytes <-> Object

		/// <summary>
		/// Convert byte array to object.
		/// </summary>
		/// <remarks>byte[0] is empty/default value.</remarks>
		public static object BytesToObject(byte[] bytes, Type type)
		{
			// Return empty values for nullable types.
			if (bytes.Length == 0)
			{
				if (type == typeof(string)) return string.Empty;
				if (type == typeof(System.DBNull)) return DBNull.Value;
				bytes = null;
			}
			// Return null or default value.
			bool isNullable = IsNullable(type);
			if (bytes == null) return (isNullable) ? null : Activator.CreateInstance(type);
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
				case TypeCode.SByte: o = Convert.ToSByte(ReadSNumber(reader)); break;
				case TypeCode.Int16: o = Convert.ToInt16(ReadSNumber(reader)); break;
				case TypeCode.Int32: o = Convert.ToInt32(ReadSNumber(reader)); break;
				case TypeCode.Int64: o = Convert.ToInt64(ReadSNumber(reader)); break;
				case TypeCode.Single: o = reader.ReadSingle(); break;
				case TypeCode.String: o = CurrentEncoding.GetString(bytes); break;
				case TypeCode.Byte: o = Convert.ToByte(ReadUNumber(reader)); break;
				case TypeCode.UInt16: o = Convert.ToUInt16(ReadUNumber(reader)); break;
				case TypeCode.UInt32: o = Convert.ToUInt32(ReadUNumber(reader)); break;
				case TypeCode.UInt64: o = Convert.ToUInt64(ReadUNumber(reader)); break;
				case TypeCode.Object:
					if (typeU2.Equals(typeof(byte[])))
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
						throw new Exception("Non Serializable Object: " + typeU2.Name);
					}
					break;
				default: throw new Exception("Unknown Type: " + typeU2.Name);
			}
			if (typeU1.IsEnum) o = Enum.ToObject(typeU1, o);
			return o;
		}

		static T? ToNullable<T>(T value) where T : struct
		{
			return (T?)value;
		}

		public static bool IsNullOrDefault(object o)
		{
			if (o == null) return true;
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
		public static byte[] ObjectToBytes(object o, Type declaringType)
		{
			if (o == null) return null;
			Type type = declaringType; //  o.GetType();
			bool isNullable = IsNullable(type);
			Type typeU1 = isNullable ? Nullable.GetUnderlyingType(type) ?? type : type;
			Type typeU2 = typeU1.IsEnum ? Enum.GetUnderlyingType(typeU1) : typeU1;
			// If object is not nullable and supplied value is same as default value then...
			if (!isNullable && typeU2.IsValueType && Activator.CreateInstance(typeU1).Equals(o))
			{
				// Don't send data if default value is same.
				return null;
			}
			TypeCode typeCode = Type.GetTypeCode(typeU2);
			// CWE-404: Improper Resource Shutdown or Release
			// Note: Binary Writer will close underlying MemoryStream automatically.
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);
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
					if (typeU2.Equals(typeof(byte[])))
					{
						var bytes = (byte[])o;
						var value = new byte[bytes.Length];
						Array.Copy(bytes, value, value.Length);
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
						throw new Exception("Non Serializable Object: " + type.Name);
					}
					break;
				default:
					throw new Exception("Unknown Type: " + type.Name);
			}
			var result = objectBytes == null
				? stream.ToArray()
				: objectBytes;
			// Binary Writer will close underlying MemoryStream automatically.
			writer.Close();
			//stream.Close();
			return result;
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
