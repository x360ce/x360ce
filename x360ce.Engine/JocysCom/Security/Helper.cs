using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace JocysCom.ClassLibrary.Security
{
	public class Helper
	{

		static int securityHashSize = 16;
		static int unlockHashSize = 6;

		/// <summary>
		/// Get current time unit value.
		/// </summary>
		/// <param name="unit">Time unit type.</param>
		/// <returns>Time unit value.</returns>
		public static double GetTimeUnitValue(TimeUnitType unit)
		{
			switch (unit)
			{
				case TimeUnitType.Seconds: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalSeconds);
				case TimeUnitType.Minutes: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalMinutes);
				case TimeUnitType.Hours: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalHours);
				case TimeUnitType.Days: return Math.Floor(DateTime.Now.Subtract(DateTime.MinValue).TotalDays);
			}
			return 0;
		}

		/// <summary>
		/// Generate password reset/token key which is unique to user. Reset key won't change until user logs in or change password.
		/// </summary>
		/// <param name="id">ID</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <returns></returns>
		public static string GetSecurityToken<T1, T2>(T1 id, T2 password, TimeUnitType unit, string hmacHashKey = null)
		{
			double u = GetTimeUnitValue(unit);
			return GetSecurityToken(id, password, u, hmacHashKey);
		}

		/// <summary>
		/// Get security token.
		/// </summary>
		/// <param name="id">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unitValue">Unit value.</param>
		/// <returns></returns>
		public static string GetSecurityToken<T1, T2>(T1 id, T2 password, double unitValue, string hmacHashKey = null)
		{
			byte[] idBytes = ObjectToBytes(id);
			byte[] passwordBytes = ObjectToBytes(password);
			byte[] unitBytes = ObjectToBytes(unitValue);
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);
			writer.Write(idBytes);
			writer.Write(passwordBytes);
			writer.Write(unitBytes);
			byte[] bytes = stream.ToArray();
			byte[] tokenPrefixBytesFull = Encryption.Current.ComputeHash(bytes).ToByteArray();
			string tokenPrefix = BytesToHex(tokenPrefixBytesFull).Substring(0, securityHashSize).ToUpper();
			byte[] tokenIdBytes = ExclusiveORValue(tokenPrefix, idBytes, hmacHashKey);
			string tokenId = BytesToHex(tokenIdBytes);
			string token = string.Format("{0}{1}", tokenPrefix, tokenId);
			return token;
		}

		/// <summary>
		/// Check if token key is valid.
		/// </summary>
		/// <param name="token">Token to check.</param>
		/// <param name="userId">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <param name="count">How many units in past mus be checked.</param>
		/// <returns>True if token is valid, False if not valid.</returns>
		public static bool CheckSecurityToken<T1, T2>(string token, T1 id, T2 password, TimeUnitType unit = TimeUnitType.None, int count = 0)
		{
			// Time which passed.
			double u = GetTimeUnitValue(unit);
			// If there is no expiry then...
			if (u == 0) return (token == GetSecurityToken(id, password, u));
			// Check keys for last units. (-5 solves the issue when token generator time is inaccurate and is set up to 5 [seconds|minutes|hours|days] in future).
			for (int i = -5; i < count; i++)
			{
				// If resetKey matches to key for given day then...
				if (token == GetSecurityToken(id, password, u - i)) return true;
			}
			return false;
		}

		#region Unlock Token (Decimal)

		/// <summary>Get decimal unlock code.</summary>
		/// <param name="value">Value which will be used to make token.</param>
		/// <param name="unitType">Time unit type. Can be minutes, hours or days.</param>
		/// <returns></returns>
		public static string GetUnlockToken(string value, TimeUnitType unitType, string hmacHashKey = null)
		{
			var u = GetTimeUnitValue(unitType);
			return GetUnlockTokenByValue(value, u, hmacHashKey);
		}

		/// <summary>Get decimal unlock code.</summary>
		/// <param name="value">Value to hash.</param>
		/// <param name="unitValue">Time value which equals to total Seconds, Minutes, Hours or Days passed from zero date to specific point in time.</param>
		/// <returns></returns>
		private static string GetUnlockTokenByValue(string value, double unitValue, string hmacHashKey = null)
		{
			string passString = string.Format("{0}_{1}", value, unitValue);
			var hash = Encryption.Current.ComputeHash(passString, hmacHashKey).ToByteArray();
			var numb = BitConverter.ToUInt32(hash, 0);
			var text = numb.ToString().PadRight(unlockHashSize, '0').Substring(0, unlockHashSize);
			return text;
		}

		/// <summary>
		/// Check if token is valid.
		/// </summary>
		/// <param name="token">Token to check.</param>
		/// <param name="userId">User id (Integer or GUID).</param>
		/// <param name="password">Password or secure key/hash.</param>
		/// <param name="unit">Time unit type.</param>
		/// <param name="count">How many units in past mus be checked.</param>
		/// <returns>True if token is valid, False if not valid.</returns>
		public static bool CheckUnlockToken(string token, string value, TimeUnitType unit, int count, string hmacHashKey = null)
		{
			// Time which passed.
			double u = GetTimeUnitValue(unit);
			// Check keys for last units and return if token match.
			for (int i = 0; i < count; i++) if (token == GetUnlockTokenByValue(value, u - i, hmacHashKey)) return true;
			// -5 solves the issue when token generator time is inaccurate and is set up to 5 [seconds|minutes|hours|days] in future
			for (int i = -5; i < 0; i++) if (token == GetUnlockTokenByValue(value, u - i, hmacHashKey)) return true;
			return false;
		}

		#endregion

		/// <summary>
		/// Get URL to page.
		/// </summary>
		/// <param name="token">Token.</param>
		/// <param name="page">Page name. Like "/Login.aspx" or "/LoginReset.aspx"</param>
		/// <returns>Url.</returns>
		public static string GetUrl(string token, string absolutePath = null, string keyName = "Key")
		{
			Uri u = System.Web.HttpContext.Current.Request.Url;
			var port = u.IsDefaultPort ? "" : ":" + u.Port.ToString();
			if (absolutePath == null) absolutePath = u.AbsolutePath;
			return string.Format("{0}://{1}{2}{3}?{4}={5}", u.Scheme, u.Host, port, absolutePath, keyName, token);
		}

		/// <summary>
		/// Get Id from token.
		/// </summary>
		/// <typeparam name="T">Id type (Integer or GUID).</typeparam>
		/// <param name="token">Token.</param>
		/// <returns>Id.</returns>
		public static T GetId<T>(string token, string hmacHashKey = null)
		{
			string tokenPrefix = token.Substring(0, securityHashSize);
			string tokenId = token.Substring(securityHashSize);
			var tokenIdBytes = HexToBytes(tokenId);
			var value = ExclusiveORValue(tokenPrefix, tokenIdBytes, hmacHashKey);
			var id = BytesToObject<T>(value);
			return id;
		}

		#region Object To/From Bytes

		public static byte[] ExclusiveORValue(string tokenPrefix, byte[] tokenId, string hmacHashKey = null)
		{
			var keyBytes = Encryption.Current.ComputeHash(tokenPrefix, hmacHashKey).ToByteArray();
			var value = ExclusiveOR(tokenId, keyBytes);
			return value;
		}

		public static byte[] ExclusiveOR(byte[] value, byte[] key)
		{
			byte[] result = new byte[value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				result[i] = (byte)(value[i] ^ key[i % key.Length]);
			}
			return result;
		}

		static string ObjectToHex<T>(T o)
		{
			var bytes = ObjectToBytes<T>(o);
			return BytesToHex(bytes);
		}

		static string BytesToHex(byte[] bytes)
		{
			var hexList = bytes.Select(x => x.ToString("X2"));
			return string.Join("", hexList);
		}

		static byte[] HexToBytes(string hex)
		{
			int offset = 0;
			if ((hex.Length % 2) != 0) return new byte[0];
			byte[] bytes = new byte[(hex.Length - offset) / 2];
			for (int i = 0; i < bytes.Length; i++)
			{
				bytes[i] = byte.Parse(hex.Substring(offset, 2), System.Globalization.NumberStyles.HexNumber);
				offset += 2;
			}
			return bytes;
		}

		static T HexToObject<T>(string hex)
		{
			var bytes = HexToBytes(hex);
			var o = BytesToObject<T>(bytes);
			return o;
		}

		/// <summary>
		/// Convert object to byte array.
		/// </summary>
		public static byte[] ObjectToBytes<T>(T value)
		{
			if (value == null) return new byte[0];
			var o = (object)value;
			var type = typeof(T);
			TypeCode typeCode = Type.GetTypeCode(type);
			MemoryStream stream = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(stream);
			switch (typeCode)
			{
				case TypeCode.Boolean: writer.Write((Boolean)o); break;
				case TypeCode.Byte: writer.Write((Byte)o); break;
				case TypeCode.Char: writer.Write((Char)o); break;
				case TypeCode.DateTime: writer.Write(((DateTime)o).Ticks); break; // Ticks type is Int64.
				case TypeCode.Decimal: writer.Write((Decimal)o); break;
				case TypeCode.Double: writer.Write((Double)o); break;
				case TypeCode.Int16: writer.Write((Int16)o); break;
				case TypeCode.Int32: writer.Write((Int32)o); break;
				case TypeCode.Int64: writer.Write((Int64)o); break;
				case TypeCode.SByte: writer.Write((SByte)o); break;
				case TypeCode.Single: writer.Write((Single)o); break;
				case TypeCode.String: writer.Write(Encoding.UTF8.GetBytes((string)o)); break;
				case TypeCode.UInt16: writer.Write((UInt16)o); break;
				case TypeCode.UInt32: writer.Write((UInt32)o); break;
				case TypeCode.UInt64: writer.Write((UInt64)o); break;
				default:
					if (type == typeof(byte[])) writer.Write((byte[])o);
					else if (type == typeof(Guid)) writer.Write(((Guid)o).ToByteArray());
					break;
			}
			byte[] result = stream.ToArray();
			return result;
		}

		/// <summary>
		/// Convert byte array to object.
		/// </summary>
		/// <remarks>byte[0] is empty/default value.</remarks>
		public static T BytesToObject<T>(byte[] bytes)
		{
			if (bytes == null) return default(T);
			Type type = typeof(T);
			// Return empty value.
			if (bytes.Length == 0)
			{
				if (type == typeof(string)) return (T)(object)string.Empty;
				return default(T);
			}
			TypeCode typeCode = Type.GetTypeCode(type);
			MemoryStream stream = new MemoryStream(bytes);
			BinaryReader reader = new BinaryReader(stream);
			object o = default(T);
			switch (typeCode)
			{
				case TypeCode.Boolean: o = reader.ReadBoolean(); break;
				case TypeCode.Char: o = reader.ReadChar(); break;
				case TypeCode.DateTime: o = new DateTime(reader.ReadInt64()); break;
				case TypeCode.Decimal: o = reader.ReadDecimal(); break;
				case TypeCode.Double: o = reader.ReadDouble(); break;
				case TypeCode.SByte: o = reader.ReadSByte(); break;
				case TypeCode.Int16: o = reader.ReadInt16(); break;
				case TypeCode.Int32: o = reader.ReadInt32(); break;
				case TypeCode.Int64: o = reader.ReadInt64(); break;
				case TypeCode.Single: o = reader.ReadSingle(); break;
				case TypeCode.String: o = Encoding.UTF8.GetString(bytes); break;
				case TypeCode.Byte: o = reader.ReadByte(); break;
				case TypeCode.UInt16: o = reader.ReadUInt16(); break;
				case TypeCode.UInt32: o = reader.ReadUInt32(); break;
				case TypeCode.UInt64: o = reader.ReadUInt64(); break;
				default:
					if (type == typeof(byte[])) o = bytes.ToArray();
					else if (type == typeof(Guid)) o = (new Guid(bytes));
					break;
			}
			return (T)o;
		}

		#endregion

		//public static void SendPasswordResetKey(string username, string password, TimeUnitType unit)
		//{
		//    Uri u = System.Web.HttpContext.Current.Request.Url;
		//    string resetUrl = GetUrl(username, password, unit);
		//    string template = Helper.GetTranslation(TranslationKey.PasswordResetTemplate);
		//    string body = template.Replace("{Username}", user.FullName).Replace("{Host}", u.Host).Replace("{ResetKey}", resetUrl);
		//    string subject = string.Format("Reset your {0} password", u.Host);
		//    Engine.Mail.Current.Send(user.Membership.Email, user.FullName, subject, body, JocysCom.ClassLibrary.Mail.MailTextType.Plain);
		//}

	}
}
