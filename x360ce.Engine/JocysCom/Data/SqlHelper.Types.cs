using System;
using System.Data.SqlClient;

namespace JocysCom.ClassLibrary.Data
{
	public partial class SqlHelper
	{
		public static bool HaveSize(string sqlType)
		{
			var name = sqlType.ToLower();
			return name.Contains("char") || name.Contains("binary");
		}

		public static bool IsUnicode(string sqlType)
		{
			var name = sqlType.ToLower();
			return name.Contains("nchar") || name.Contains("nvarchar");
		}

		public static string ToSystemTypeVB(string sqlType)
		{
			var t = ToSystemType(sqlType);
			if (t == typeof(DateTime)) return "Date";
			if (t == typeof(Int16)) return "Short";
			if (t == typeof(Int32)) return "Integer";
			if (t == typeof(Int64)) return "Long";
			if (t == typeof(UInt16)) return "UShort";
			if (t == typeof(UInt32)) return "UInteger";
			if (t == typeof(UInt64)) return "ULong";
			return t.Name;
		}

		public static string ToSystemTypeCS(string sqlType)
		{
			var t = ToSystemType(sqlType);
			var name = ClassLibrary.Runtime.RuntimeHelper.GetBuiltInTypeNameOrAlias(t);
			return name;
		}

		public static Type ToSystemType(string sqlTypeSring)
		{
			var sqlType = (SqlDataType)Enum.Parse(typeof(SqlDataType), sqlTypeSring, true);
			var t = typeof(String);
			switch (sqlType)
			{
				case SqlDataType.BigInt:
					t = typeof(Int64);
					break;
				case SqlDataType.Bit:
					t = typeof(Boolean);
					break;
				case SqlDataType.Char:
				case SqlDataType.VarChar:
				case SqlDataType.VarCharMax:
					t = typeof(String);
					break;
				case SqlDataType.Date:
				case SqlDataType.DateTime:
				case SqlDataType.DateTime2:
				case SqlDataType.SmallDateTime:
					t = typeof(DateTime);
					break;
				case SqlDataType.DateTimeOffset:
					t = typeof(DateTimeOffset);
					break;
				case SqlDataType.Time:
					t = typeof(TimeSpan);
					break;
				case SqlDataType.Decimal:
				case SqlDataType.Money:
				case SqlDataType.SmallMoney:
					t = typeof(Decimal);
					break;
				case SqlDataType.Int:
					t = typeof(Int32);
					break;
				case SqlDataType.NChar:
				case SqlDataType.NText:
				case SqlDataType.NVarChar:
				case SqlDataType.NVarCharMax:
				case SqlDataType.Text:
					t = typeof(String);
					break;
				case SqlDataType.Real:
				case SqlDataType.Numeric:
				case SqlDataType.Float:
					t = typeof(Double);
					break;
				case SqlDataType.Timestamp:
				case SqlDataType.Binary:
					t = typeof(Byte[]);
					break;
				case SqlDataType.TinyInt:
					t = typeof(Byte);
					break;
				case SqlDataType.SmallInt:
					t = typeof(Int16);
					break;
				case SqlDataType.UniqueIdentifier:
					t = typeof(Guid);
					break;
				case SqlDataType.UserDefinedDataType:
				case SqlDataType.UserDefinedType:
				case SqlDataType.Variant:
				case SqlDataType.Image:
					t = typeof(Object);
					break;
				default:
					t = typeof(String);
					break;
			}
			return t;
		}

		public static SqlDataType GetSqlDataType(TypeCode code, int min = 0, int max = 0, bool isUnicode = false)
		{
			var v = min != max;
			switch (code)
			{
				case TypeCode.Empty: return SqlDataType.None;
				case TypeCode.Object: return v ? SqlDataType.VarBinary : SqlDataType.Binary;
				case TypeCode.DBNull: return SqlDataType.None;
				case TypeCode.Boolean: return SqlDataType.Bit;
				case TypeCode.Char: return isUnicode ? SqlDataType.NChar : SqlDataType.Char;
				case TypeCode.SByte: return SqlDataType.None;
				case TypeCode.Byte: return SqlDataType.TinyInt;
				case TypeCode.Int16: return SqlDataType.SmallInt;
				case TypeCode.UInt16: return SqlDataType.SmallInt;
				case TypeCode.Int32: return SqlDataType.Int;
				case TypeCode.UInt32: return SqlDataType.Int;
				case TypeCode.Int64: return SqlDataType.BigInt;
				case TypeCode.UInt64: return SqlDataType.BigInt;
				case TypeCode.Single: return SqlDataType.Real;
				case TypeCode.Double: return SqlDataType.Float;
				case TypeCode.Decimal: return SqlDataType.Money;
				case TypeCode.DateTime: return SqlDataType.DateTime;
				case TypeCode.String: return v
						? isUnicode ? SqlDataType.NVarChar : SqlDataType.VarChar 
						: isUnicode ? SqlDataType.NChar : SqlDataType.Char;
				default: return SqlDataType.None;
			}
		}

		//public static SqlDataType GetSqlDataType(Type giveType)
		//{
		//	// Allow nullable types to be handled
		//	giveType = Nullable.GetUnderlyingType(giveType) ?? giveType;
		//	return GetSqlDataType(giveType);
		//}

		//public static SqlDataType GetSqlDataType<T>()
		//{
		//	return GetSqlDataType(typeof(T));
		//}

		public enum SqlDataType
		{
			BigInt = 1,
			Binary = 2,
			Bit = 3,
			Char = 4,
			Date = 36,
			DateTime = 6,
			DateTime2 = 39,
			DateTimeOffset = 38,
			Decimal = 7,
			Float = 8,
			Geography = 43,
			Geometry = 42,
			HierarchyId = 41,
			Image = 9,
			Int = 10,
			Money = 11,
			NChar = 12,
			None = 0,
			NText = 13,
			Numeric = 35,
			NVarChar = 14,
			NVarCharMax = 15,
			Real = 16,
			SmallDateTime = 17,
			SmallInt = 18,
			SmallMoney = 19,
			SysName = 34,
			Text = 20,
			Time = 37,
			Timestamp = 21,
			TinyInt = 22,
			UniqueIdentifier = 23,
			UserDefinedDataType = 24,
			UserDefinedTableType = 40,
			UserDefinedType = 25,
			VarBinary = 28,
			VarBinaryMax = 29,
			VarChar = 30,
			VarCharMax = 31,
			Variant = 32,
			Xml = 33
		}


	}

}
