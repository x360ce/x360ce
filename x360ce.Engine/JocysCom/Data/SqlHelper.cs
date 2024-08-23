using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
// Requires "System.Data.SqlClient" NuGet Package on .NET Core/Standard
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JocysCom.ClassLibrary.Data
{
	public partial class SqlHelper
	{

		static object _currentLock = new object();
		static SqlHelper _Current;

		// Current is a static method. A static method can't be virtual, since it's not related to an instance of the class.
		// use the 'new' keyword to override.
		public static SqlHelper Current
		{
			get
			{
				lock (_currentLock)
				{
					if (_Current is null) _Current = new SqlHelper();
					return _Current;
				}
			}
		}

#if NETSTANDARD // .NET Standard
	public int SetSessionUserCommentContext(IDbConnection connection, string comment = null) { return 0; }
#elif NETCOREAPP // .NET Core
		public int SetSessionUserCommentContext(IDbConnection connection, string comment = null) { return 0; }
#else // .NET Framework

		#region CONTEXT_INFO Property - Session Parameters / SetContext

		int? _SessionId;
		int? _UserId;
		object SessionParametersLock = new object();

		public virtual void GetSessionParameter(out int? sessionId, out int? userId)
		{
			lock (SessionParametersLock)
			{
				var hc = System.Web.HttpContext.Current;
				// If application is not website.
				if (hc is null)
				{
					sessionId = _SessionId;
					userId = _UserId;
				}
				else
				{
					sessionId = hc.Session is null ? null : (int?)hc.Session["_SessionId"];
					userId = hc.Session is null ? null : (int?)hc.Session["_UserId"];
				}
			}
		}

		public virtual void SetSessionParameters(int? sessionId, int? userId)
		{
			lock (SessionParametersLock)
			{
				var hc = System.Web.HttpContext.Current;
				// If application is not website.
				if (hc is null)
				{
					_SessionId = sessionId;
					_UserId = userId;
				}
				else
				{
					hc.Session["_SessionId"] = sessionId;
					hc.Session["_UserId"] = userId;
				}
			}
		}

		/*
			// CONTEXT_INFO Example:
			var conn = new System.Data.SqlClient.SqlConnection(cn);
			conn.Open();
			var mw = new System.IO.MemoryStream(128);
			var bw = new System.IO.BinaryWriter(mw);
			bw.Write((int)174);
			bw.Write("Test1");
			ClassLibrary.Data.SqlHelper.Current.SetContext(conn, mw.ToArray());
			var value = ClassLibrary.Data.SqlHelper.Current.GetContext(conn);
			conn.Close();
			var mr = new System.IO.MemoryStream(value);
			var br = new System.IO.BinaryReader(mr);
			var value1 = br.ReadInt32();
			var value2 = br.ReadString();
		*/

		/// <summary>Get SQL session context value.</summary>
		/// <param name="connection">Existing and open database connection.</param>
		/// <returns>Context value.</returns>
		public byte[] GetContext(IDbConnection connection)
		{
			var cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT CONTEXT_INFO()";
			cmd.CommandType = CommandType.Text;
			var bytes = (byte[])cmd.ExecuteScalar();
			return bytes;
		}

		/// <summary>Set SQL session context value.</summary>
		/// <param name="connection">Existing and open database connection.</param>
		/// <param name="value">Context value. 128 bytes max.</param>
		/// <returns>The number of rows affected.</returns>
		public int SetContext(IDbConnection connection, byte[] value)
		{
			var cmd = connection.CreateCommand();
			cmd.CommandText = "SET CONTEXT_INFO @value";
			cmd.CommandType = CommandType.Text;
			cmd.Parameters.Add(new SqlParameter("@value", value));
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Set current SessionId, UserID and ASCII Comment.
		/// </summary>
		/// <param name="connection">Existing and open database connection.</param>
		/// <param name="comment">Optional comment to pass into SQL.</param>
		/// <returns></returns>
		public int SetSessionUserCommentContext(IDbConnection connection, string comment = null)
		{
			int? sessionId;
			int? userId;
			GetSessionParameter(out sessionId, out userId);
			if (!sessionId.HasValue)
				return 0;
			if (!userId.HasValue)
				return 0;
			// Use the existing open connection to set the context info
			var data = new byte[128];
			if (!string.IsNullOrEmpty(comment))
			{
				var commentBytes = System.Text.Encoding.ASCII.GetBytes(comment);
				var commSize = Math.Min(commentBytes.Length, 116);
				AddValue(data, commSize, 4);
				Array.Copy(commentBytes, 0, data, 8, commSize);
			}
			AddValue(data, sessionId.Value, data.Length - 4);
			// Add profile_id.
			AddValue(data, userId.Value, 0);
			// Set context info.
			return SetContext(connection, data);
		}

		#endregion

#endif

		#region SESSION_CONTEXT Property (SQL Server 2016)

		/*
			// SESSION_CONTEXT Example:
			var conn = new System.Data.SqlClient.SqlConnection(cn);
			conn.Open();
			ClassLibrary.Data.SqlHelper.Current.SetSessionContext(conn, "Key1", 174);
			var value = (int)ClassLibrary.Data.SqlHelper.Current.GetSessionContext(conn, "Key1");
			conn.Close();
		*/

		/// <summary>
		/// SQL 2016. Maximum storage space - 256 kilobytes.
		/// </summary>
		/// <param name="connection">Existing and open database connection.</param>
		/// <param name="key">Key name. Up to 128 Unicode characters.</param>
		/// <param name="value">Any type.</param>
		/// <param name="read_only">If true then can's be cannot be changed unless session ends.</param>
		/// <remarks>SELECT SESSION_CONTEXT(N'ID');</remarks>
		/// <returns>The number of rows affected.</returns>
		public int SetSessionContext(IDbConnection connection, string key, object value, bool read_only = false)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));
			var cmd = connection.CreateCommand();
			cmd.CommandText = "sys.sp_set_session_context";
			cmd.CommandType = CommandType.StoredProcedure;
			var p = cmd.Parameters;
			p.Add(new SqlParameter("@key", key));
			p.Add(new SqlParameter("@value", value));
			p.Add(new SqlParameter("@read_only", read_only));
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// SQL 2016. Maximum storage space - 256 kilobytes.
		/// </summary>
		/// <param name="connection">Existing and open database connection.</param>
		/// <param name="key">Key name. Up to 128 Unicode characters.</param>
		/// <returns>Value.</returns>
		public object GetSessionContext(IDbConnection connection, string key)
		{
			if (connection is null)
				throw new ArgumentNullException(nameof(connection));
			var cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT SESSION_CONTEXT(@key);";
			cmd.CommandType = CommandType.Text;
			var p = cmd.Parameters;
			p.Add(new SqlParameter("@key", key));
			return cmd.ExecuteScalar();
		}

		#endregion

		#region Helper Methods

		public void AddValue(byte[] data, int id, int destinationIndex)
		{
			var bytes = BitConverter.GetBytes(id);
			Array.Reverse(bytes);
			Array.Copy(bytes, 0, data, destinationIndex, bytes.Length);
		}

		public static string GetProviderConnectionString(string connectionString, out bool isEntity)
		{
			isEntity = false;
#if NETSTANDARD // .NET Standard
#elif NETCOREAPP // .NET Core
			// EF Core does not support EF specific connection strings (metadata=res:... < this kind of connection strings).
#else // .NET Framework
			if (string.Compare(connectionString, "metadata=", true) == 0)
			{
				// Get connection string from entity connection string.
				var ecsb = new System.Data.EntityClient.EntityConnectionStringBuilder(connectionString);
				connectionString = ecsb.ProviderConnectionString;
				isEntity = true;
			}
#endif
			var builder = new SqlConnectionStringBuilder(connectionString);
			if (!builder.ContainsKey("Application Name") || ".Net SqlClient Data Provider".Equals(builder["Application Name"]))
			{
				var asm = Assembly.GetEntryAssembly();
				if (asm is null)
					asm = Assembly.GetExecutingAssembly();
				var appPrefix = asm.GetName().Name.Replace(".", "");
				var appName = string.Format("{0}", appPrefix);
				builder.Add("Application Name", appName);
				connectionString = builder.ToString();
			}
			return connectionString;
		}

		public static string GetConnectionString(string name)
		{
			bool isEntity;
			return GetConnectionString(name, out isEntity);
		}

		public static string GetConnectionString(string name, out bool isEntity)
		{
			isEntity = false;
			string connectionString = null;
#if !NETSTANDARD
			// Try to find entity connection.
			var cs = System.Configuration.ConfigurationManager.ConnectionStrings[name];
			connectionString = cs.ConnectionString;
#endif
			// If configuration section with not found then return.
			// EF Core does not support EF specific connection strings (metadata=res:... < this kind of connection strings).
#if NETFRAMEWORK // .NET Framework
			if (string.Compare(cs.ProviderName, "System.Data.EntityClient", true) == 0)
			{
				// Get connection string from entity connection string.
				var ecsb = new System.Data.EntityClient.EntityConnectionStringBuilder(cs.ConnectionString);
				connectionString = ecsb.ProviderConnectionString;
				isEntity = true;
			}
#endif
			if (connectionString is null)
				return null;
			var builder = new SqlConnectionStringBuilder(connectionString);
			if (!builder.ContainsKey("Application Name") || ".Net SqlClient Data Provider".Equals(builder["Application Name"]))
			{
				var asm = Assembly.GetEntryAssembly();
				if (asm is null) asm = Assembly.GetExecutingAssembly();
				var appPrefix = asm.GetName().Name.Replace(".", "");
				var appName = string.Format("{0}", appPrefix);
				builder.Add("Application Name", appName);
				connectionString = builder.ToString();
			}
			return connectionString;
		}

		/// <summary>Hide connection string password.</summary>
		/// <param name="text"></param>
		public static string FilterConnectionString(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;
			System.Text.RegularExpressions.Regex regex;
			regex = new System.Text.RegularExpressions.Regex("(Password|PWD)\\s*=([^;]*)([;]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			return regex.Replace(text, "$1=<hidden>$3");
		}

		public void SetReadUncommited(IDbConnection connection)
		{
			// Use the existing open connection to set the context info
			var cmd = connection.CreateCommand();
			cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery();
		}

		public string GetSqlOptions(string connectionString)
		{
			var options = "";
			using (var conn = new SqlConnection(connectionString))
			{
				var s = "";
				s += "WITH OPTION_VALUES AS (;\r\n";
				s += "\tSELECT;\r\n";
				s += "\t\toptionValues.[Id],;\r\n";
				s += "\t\toptionValues.[Name];\r\n";
				s += "\tFROM (VALUES;\r\n";
				s += "\t\t(1, 'DISABLE_DEF_CNST_CHK'),;\r\n";
				s += "\t\t(2, 'IMPLICIT_TRANSACTIONS'),;\r\n";
				s += "\t\t(4, 'CURSOR_CLOSE_ON_COMMIT'),;\r\n";
				s += "\t\t(8, 'ANSI_WARNINGS'),;\r\n";
				s += "\t\t(16, 'ANSI_PADDING'),;\r\n";
				s += "\t\t(32, 'ANSI_NULLS'),;\r\n";
				s += "\t\t(64, 'ARITHABORT'),;\r\n";
				s += "\t\t(128, 'ARITHIGNORE'),;\r\n";
				s += "\t\t(256, 'QUOTED_IDENTIFIER'),;\r\n";
				s += "\t\t(512, 'NOCOUNT'),;\r\n";
				s += "\t\t(1024, 'ANSI_NULL_DFLT_ON'),;\r\n";
				s += "\t\t(2048, 'ANSI_NULL_DFLT_OFF'),;\r\n";
				s += "\t\t(4096, 'CONCAT_NULL_YIELDS_NULL'),;\r\n";
				s += "\t\t(8192, 'NUMERIC_ROUNDABORT'),;\r\n";
				s += "\t\t(16384, 'XACT_ABORT');\r\n";
				s += "\t) AS optionValues([Id], [name]);\r\n";
				s += ");\r\n";
				s += "SELECT [Name];\r\n";
				s += "FROM OPTION_VALUES;\r\n";
				s += "WHERE (@@OPTIONS & [Id]) = [Id]\r\n";
				conn.Open();
				using (var optionsSqlCommand = new SqlCommand(s, conn))
				using (var sqlDataReader1 = optionsSqlCommand.ExecuteReader(CommandBehavior.SequentialAccess))
					while (sqlDataReader1.Read())
						options += $"{sqlDataReader1[0]}\r\n";
				// Dispose calls conn.Close() internally.
			}
			return options;
		}

		/// <summary>Cache data for speed.</summary>
		/// <remarks>Cache allows for this class to work 20 times faster.</remarks>
		private static ConcurrentDictionary<Type, PropertyInfo[]> Properties { get; } = new ConcurrentDictionary<Type, PropertyInfo[]>();

		private static PropertyInfo[] GetProperties(Type t, bool cache = true)
		{
			var properties = cache
				? Properties.GetOrAdd(t, x => t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
				: t.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			return properties;
		}

		#endregion

		#region Execute Methods

		public int ExecuteNonQuery(string connectionString, SqlCommand cmd, string comment = null, int? timeout = null)
		{
			//var sql = ToSqlCommandString(cmd);
			var cb = new SqlConnectionStringBuilder(connectionString);
			if (timeout.HasValue)
			{
				cmd.CommandTimeout = timeout.Value;
				cb.ConnectTimeout = timeout.Value;
			}
			var conn = new SqlConnection(cb.ConnectionString);
			cmd.Connection = conn;
			conn.Open();
			SetSessionUserCommentContext(conn, comment);
			int rv = cmd.ExecuteNonQuery();
			cmd.Dispose();
			// Dispose calls conn.Close() internally.
			conn.Dispose();
			return rv;
		}

		//[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		//public int ExecuteNonQuery(string connectionString, string cmdText, string comment = null, int? timeout = null)
		//{
		//	var cmd = new SqlCommand(cmdText);
		//	cmd.CommandType = CommandType.Text;
		//	return ExecuteNonQuery(connectionString, cmd, comment, timeout);
		//}

		public object ExecuteScalar(string connectionString, SqlCommand cmd, string comment = null)
		{
			//var sql = ToSqlCommandString(cmd);
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetSessionUserCommentContext(conn, comment);
			// Returns first column of the first row.
			var returnValue = cmd.ExecuteScalar();
			cmd.Dispose();
			// Dispose calls conn.Close() internally.
			conn.Dispose();
			return returnValue;
		}

		public IDataReader ExecuteReader(string connectionString, SqlCommand cmd, string comment = null)
		{
			//var sql = ToSqlCommandString(cmd);
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetSessionUserCommentContext(conn, comment);
			return cmd.ExecuteReader();
		}

		public T ExecuteDataSet<T>(string connectionString, SqlCommand cmd, string comment = null) where T : DataSet
		{
			//var sql = ToSqlCommandString(cmd);
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetSessionUserCommentContext(conn, comment);
			var adapter = new SqlDataAdapter(cmd);
			var ds = Activator.CreateInstance<T>();
			int rowsAffected = ds.GetType() == typeof(DataSet)
				? adapter.Fill(ds)
				: adapter.Fill(ds, ds.Tables[0].TableName);
			adapter.Dispose();
			cmd.Dispose();
			// Dispose calls conn.Close() internally.
			conn.Dispose();
			return ds;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public DataSet ExecuteDataset(string connectionString, CommandType commandType, string cmdText, string comment = null, params SqlParameter[] commandParameters)
		{
			var cmd = new SqlCommand(cmdText);
			cmd.CommandType = commandType;
			if (commandParameters != null && commandParameters.Length > 0)
			{
				cmd.Parameters.AddRange(commandParameters);
			}
			return ExecuteDataSet<DataSet>(connectionString, cmd, comment);
		}

		public DataSet ExecuteDataSet(string connectionString, SqlCommand cmd, string comment = null)
		{
			return ExecuteDataSet<DataSet>(connectionString, cmd, comment);
		}

		public DataTable ExecuteDataTable(string connectionString, SqlCommand cmd, string comment = null)
		{
			var ds = ExecuteDataSet(connectionString, cmd, comment);
			if (ds != null && ds.Tables.Count > 0) return ds.Tables[0];
			return null;
		}

		public DataRow ExecuteDataRow(string connectionString, SqlCommand cmd, string comment = null)
		{
			var table = ExecuteDataTable(connectionString, cmd, comment);
			if (table != null && table.Rows.Count > 0) return table.Rows[0];
			return null;
		}

		public List<T> ExecuteData<T>(string connectionString, SqlCommand cmd, string comment = null)
		{
			var list = new List<T>();
			var props = typeof(T).GetProperties().ToDictionary(x => x.Name, x => x);
			var reader = ExecuteReader(connectionString, cmd, comment);
			while (reader.Read())
			{
				var item = Activator.CreateInstance<T>();
				for (int i = 0; i < reader.FieldCount; i++)
				{
					var name = reader.GetName(i);
					var value = reader.GetValue(i);
					var property = props[name];
					if (property != null)
						property.SetValue(item, reader.IsDBNull(i) ? null : value, null);
				}
				list.Add(item);
			}
			return null;
		}

		#endregion

		#region Optimize

		public static bool Optimize = true;

		/// <summary>
		/// Optimize CommandBehavior for SqlDataReader.
		/// SequentialAccess requires to read results in strict sequence.
		/// var v0 = reader.GetString(0);
		/// var v1 = reader.GetDateTime(1);
		/// var v3 = reader.GetDouble(2);
		/// var v4 = reader.GetDouble(3);
		/// </summary>
		public static CommandBehavior OptimizeBehavior(CommandBehavior behavior)
		{
			if (!Optimize)
				return behavior;
			// Automatically closes the associated SqlConnection when the SqlDataReader is closed, freeing up resources.
			behavior |= CommandBehavior.CloseConnection;
			// Enables the SqlDataReader to load data as a stream, improving performance when processing large amounts of data sequentially.
			behavior |= CommandBehavior.SequentialAccess;
			return behavior;
		}

		/// <summary>
		/// Optimize SQL Query.
		/// </summary>
		public static string OptimizeQuery(string query)
		{
			if (!Optimize)
				return query;
			// Reduce the locking overhead for your query by allowing it to read uncommitted data.
			// Achieves a similar result as using the (NOLOCK) hint in the query. 
			query = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; " + query;
			// Disable the tracking of time statistics for a query.
			////query = "SET STATISTICS TIME OFF; " + query;
			// Prevents sending row count messages to the client.
			query = "SET NOCOUNT ON; " + query;
			// Disable tracking of IO statistics for a query.
			//query = "SET STATISTICS IO OFF; " + query;
			// Force to recompile the execution plan each time the query is executed,
			// which can improve performance for frequently changing data or varying input parameters.
			//query += " OPTION (RECOMPILE)";
			return query;
		}

		/// <summary>
		/// Optimize SQL connection string for SqlDataReader.
		/// </summary>
		public static string OptimizeConnection(string connectionString)
		{
			if (!Optimize)
				return connectionString;
			var builder = new SqlConnectionStringBuilder(connectionString)
			{
				// Increase default packet size, potentially improving performance for queries
				// that return large amounts of data by reducing the number of round-trips between client and server.
				//PacketSize = 4096 * 2,
				// Disable support for multiple active result sets, improving performance when only one result set is needed.
				// Note: Setting `MultipleActiveResultSets = false` can have a significant impact on reducing random query delays.
				MultipleActiveResultSets = false,
				// Allow SQL Server to optimize for read-only queries.
				ApplicationIntent = ApplicationIntent.ReadOnly,
			};
			return builder.ConnectionString;
		}

		#endregion

		#region Error

		public static void SetErrorParameters(SqlParameterCollection p)
		{
			var ec = p.Add("@error_code", SqlDbType.Int).Direction = ParameterDirection.InputOutput;
			var em = p.Add("@error_message", SqlDbType.NVarChar, 255).Direction = ParameterDirection.InputOutput;
		}

		public static void GetErrorParameters(SqlParameterCollection p, out int error_code, out string error_message)
		{
			error_code = (int)p["@error_code"].Value;
			error_message = (string)p["@error_message"].Value;
		}

		#endregion

		#region Add Range

		/// <summary>
		/// Add an array of parameters to a SQL command in an IN statement.
		/// Example:
		///	    var cmd = new SqlCommand("SELECT * FROM table WHERE value IN (@values)");
		///     SqlHelper.AddArrayParameters(cmd, "@values", new int[] { 1, 2, 3 });
		/// </summary>
		/// <param name="cmd">The SQL command object to add parameters to.</param>
		/// <param name="paramName">Parameter name inside SQL command.</param>
		/// <param name="values">The array of strings that need to be added as parameters.</param>
		/// <returns>Array of added parameters.</returns>
		/// <remarks>
		/// An array cannot be simply added as a single parameter to a SQL command.
		/// New SQL parameter will be created for each array value.
		/// </remarks>
		public static SqlParameter[] AddArrayParameters<T>(SqlCommand cmd, string paramName, params T[] values)
		{
			if (cmd is null)
				throw new ArgumentNullException(nameof(cmd));
			var parameters = new List<SqlParameter>();
			for (int i = 0; i < values.Length; i++)
			{
				var name = string.Format("{0}_{1}", paramName, i);
				var param = cmd.Parameters.AddWithValue(name, values[i]);
				parameters.Add(param);
			}
			var rx = new System.Text.RegularExpressions.Regex(paramName + "\\b");
			var paramNames = string.Join(", ", parameters.Select(x => x.ParameterName));
			cmd.CommandText = rx.Replace(cmd.CommandText, paramNames);
			return parameters.ToArray();
		}

		#endregion

		#region Convert Table To/From List

		/// <summary>
		/// Convert DataTable to List of objects. Can be used to convert DataTable to list of framework entities. 
		/// </summary>
		public static List<T> ConvertToList<T>(DataTable table)
		{
			if (table is null) return null;
			var list = new List<T>();
			var props = GetProperties(typeof(T));
			var columns = table.Columns.Cast<DataColumn>().ToArray();
			foreach (DataRow row in table.Rows)
			{
				var item = Convert<T>(row, props, columns);
				list.Add(item);
			}
			return list;
		}

		/// <summary>Convert DataRow to object.</summary>
		/// <param name="propsCache">Optional for cache reasons.</param>
		/// <param name="columnsCache">Optional for cache reasons.</param>
		public static T Convert<T>(DataRow row, PropertyInfo[] propsCache = null, DataColumn[] columnsCache = null)
		{
			var props = propsCache ?? GetProperties(typeof(T));
			var columns = columnsCache ?? row.Table.Columns.Cast<DataColumn>().ToArray();
			var item = Activator.CreateInstance<T>();
			foreach (var prop in props)
			{
				var column = columns.FirstOrDefault(x => prop.Name.Equals(x.ColumnName, StringComparison.OrdinalIgnoreCase));
				if (column is null)
					continue;
				if (!prop.CanWrite)
					continue;
				if (row.IsNull(column.ColumnName))
					continue;
				var value = row[column.ColumnName];
				// If type must be converted then...
				var columnType = row[column.ColumnName].GetType();
				if (columnType != prop.PropertyType)
				{
					// Get type if nullable.
					var underType = Nullable.GetUnderlyingType(prop.PropertyType);
					if (underType != null)
					{
						if (columnType == typeof(string) && Equals(value, ""))
							value = null;
					}
					var t = underType ?? prop.PropertyType;
					if (value != null)
						value = t.IsEnum
						? Enum.Parse(t, (string)value)
						: System.Convert.ChangeType(value, t);
				}
				prop.SetValue(item, value, null);
			}
			return item;
		}

		/// <summary>
		/// Convert IEnumerable to DataTable.
		/// Table can be used to pass data into stored procedures.
		/// </summary>
		/// <param name="source">List to convert.</param>
		/// <param name="propertyNames">
		/// Properties to include.
		/// For example: nameof(class1.property1), nameof(class1.property2)
		/// </param>
		/// <remarks>
		/// LIST:
		///	class1[] data = new {
		///		{ P1 = "A", P2 = "B", P3 = "C" }
		///		{ P1 = "E", P2 = "F", P3 = "G" }
		/// };
		/// WHERE:
		/// public class1 {
		///		public string P1 { get;set; }
		///		public string P2 { get;set; }
		///		public string P3 { get;set; }
		/// }
		/// TABLE:
		/// P1,  P2,  P3
		/// "A", "B", "C"
		/// "E", "F", "G"
		/// </remarks>
		public static DataTable ConvertToTable<T>(IEnumerable<T> source, params string[] propertyNames)
		{
			if (source is null)
				return null;
			var table = new DataTable();
			var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
			if (propertyNames.Any())
				props = props.Where(x => propertyNames.Contains(x.Name)).ToArray();
			foreach (var prop in props)
			{
				var underType = Nullable.GetUnderlyingType(prop.PropertyType);
				var columnType = underType ?? prop.PropertyType;
				//var descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();
				//var columnName = descriptionAttribute?.Description ?? prop.Name;
				//table.Columns.Add(columnName, columnType);
				table.Columns.Add(prop.Name, columnType);
			}
			var values = new object[props.Length];
			foreach (T item in source)
			{
				for (int i = 0; i < props.Length; i++)
					values[i] = props[i].GetValue(item, null);
				table.Rows.Add(values);
			}
			return table;
		}

		/// <summary>
		/// Convert object with IEnumerable propertied to DataTable.
		/// Table can be used to pass data into stored procedures.
		/// </summary>
		/// <param name="list">List to convert.</param>
		/// <param name="columns">
		/// Properties to include.
		/// For example: nameof(class1.property1), nameof(class1.property2)
		/// </param>
		/// <remarks>
		/// OBJECT:
		///	var data = new class1(){
		///		P1 = new[] { "A", "E" },
		///		P2 = new[] { "B", "F" },
		///		P3 = new[] { "C", "G" },
		///	}
		/// WHERE:
		/// public class1 {
		///		public string[] P1 { get;set; }
		///		public string[] P2 { get;set; }
		///		public string[] P3 { get;set; }
		/// }
		/// TABLE:
		/// P1,  P2,  P3
		/// "A", "B", "C"
		/// "E", "F", "G"
		/// </remarks>
		public static DataTable ConvertObjectToTable<T>(T source, params string[] propertyNames)
		{
			if (source == null)
				return null;

			var table = new DataTable();
			var typeInfo = typeof(T);

			// Get all properties of the specified type T.
			var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
			if (propertyNames.Any())
				props = props.Where(x => propertyNames.Contains(x.Name)).ToArray();

			// Filter properties to be included.
			var propertiesToInclude = props
				// Get properties that are IEnumerable. Exclude "string" type that is an enumerable of char.
				.Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
				.ToArray();

			// Populate DataTable columns
			foreach (var property in propertiesToInclude)
			{
				// Determine the type of elements in the IEnumerable
				var elementType = GetElementType(property.PropertyType);
				var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
				var columnName = descriptionAttribute?.Description ?? property.Name;
				table.Columns.Add(columnName, elementType ?? typeof(object));
			}

			// Determine maximum row count based on the largest list
			int maxRowCount = propertiesToInclude
				.Select(p => (IEnumerable)p.GetValue(source))
				.Where(l => l != null)
				.Select(l => l.Cast<object>().Count())
				.DefaultIfEmpty(0)
				.Max();

			// Populate DataTable with property values
			for (int rowIndex = 0; rowIndex < maxRowCount; rowIndex++)
			{
				var row = table.NewRow();
				for (int columnIndex = 0; columnIndex < propertiesToInclude.Length; columnIndex++)
				{
					var property = propertiesToInclude[columnIndex];
					var values = (IEnumerable)property.GetValue(source);
					if (values != null)
					{
						var valueList = values.Cast<object>().ToList();
						row[columnIndex] = rowIndex < valueList.Count ? valueList[rowIndex] : DBNull.Value;
					}
					else
					{
						row[columnIndex] = DBNull.Value;
					}
				}
				table.Rows.Add(row);
			}
			return table;
		}

		/// <summary>
		/// Helper method to determine the element type of an IEnumerable property.
		/// </summary>
		private static Type GetElementType(Type enumerableType)
		{
			if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				return enumerableType.GetGenericArguments()[0];
			var iEnumerableInterface = enumerableType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
			return iEnumerableInterface?.GetGenericArguments()[0];
		}

		#endregion

		#region SqlCommand to T-SQL

		/// <summary>
		/// There is no easy way to create SQL string from SqlCommand, because execution does not generate any SQL.
		/// </summary>
		public static string ToSqlCommandString(SqlCommand cmd)
		{
			var sql = new StringBuilder();
			var FirstParam = true;
			if (cmd.Connection != null)
				sql.AppendLine("USE " + cmd.Connection.Database + ";");
			switch (cmd.CommandType)
			{
				case CommandType.StoredProcedure:
					sql.AppendLine("DECLARE @return_value int;");
					foreach (SqlParameter sp in cmd.Parameters)
					{
						if (sp.Direction == ParameterDirection.InputOutput || sp.Direction == ParameterDirection.Output)
						{
							sql.Append("DECLARE " + sp.ParameterName + "\t" + sp.SqlDbType.ToString() + "; SET " + sp.ParameterName + " = ");
							sql.AppendLine((sp.Direction == ParameterDirection.Output ? "NULL" : ParameterValueForSQL(sp)) + ";");
						}
					}
					sql.AppendLine("EXEC [" + cmd.CommandText + "]");
					foreach (SqlParameter sp in cmd.Parameters)
					{
						if (sp.Direction != ParameterDirection.ReturnValue)
						{
							sql.Append((FirstParam) ? "\t" : "\t, ");
							if (FirstParam) FirstParam = false;
							if (sp.Direction == ParameterDirection.Input) sql.AppendLine(sp.ParameterName + " = " + ParameterValueForSQL(sp));
							else sql.AppendLine(sp.ParameterName + " = " + sp.ParameterName + " OUTPUT");
						}
					}
					sql.AppendLine(";");
					sql.AppendLine("SELECT '@return_value' = CONVERT(VarChar, @return_value);");
					foreach (SqlParameter sp in cmd.Parameters)
					{
						if (sp.Direction == ParameterDirection.InputOutput || sp.Direction == ParameterDirection.Output)
						{
							sql.AppendLine("SELECT '" + sp.ParameterName + "' = CONVERT(VarChar, " + sp.ParameterName + ");");
						}
					}
					break;
				case CommandType.Text:
					sql.AppendLine(cmd.CommandText);
					break;
			}
			return sql.ToString();
		}


		/// <summary>
		/// Get Parameter value in string for SQL.
		/// </summary>
		static string ParameterValueForSQL(SqlParameter sp)
		{
			string retval = "";
			switch (sp.SqlDbType)
			{
				case SqlDbType.Char:
				case SqlDbType.NChar:
				case SqlDbType.NText:
				case SqlDbType.NVarChar:
				case SqlDbType.Text:
				case SqlDbType.Time:
				case SqlDbType.VarChar:
				case SqlDbType.Xml:
					retval = "'" + string.Format("{0}", sp.Value).Replace("'", "''") + "'";
					break;
				case SqlDbType.Date:
					retval = string.Format("'{0:yyyy-MM-dd}'", sp.Value);
					break;
				case SqlDbType.DateTime:
					retval = string.Format("'{0:yyyy-MM-dd HH:mm:ss.FFF}'", sp.Value);
					break;
				case SqlDbType.DateTime2:
					retval = string.Format("'{0:yyyy-MM-dd HH:mm:ss.FFFFFFF}'", sp.Value);
					break;
				case SqlDbType.DateTimeOffset:
					retval = string.Format("'{0:yyyy-MM-dd HH:mm:ss.FFFFFFFzzz}'", sp.Value);
					break;
				case SqlDbType.Bit:
					retval = ToBooleanOrDefault(sp.Value, false) ? "1" : "0";
					break;
				default:
					retval = string.Format("{0}", sp.Value).Replace("'", "''");
					break;
			}
			return retval;
		}

		/// <summary>
		/// Get boolean value from object value.
		/// </summary>
		static bool ToBooleanOrDefault(object o, bool defaultValue)
		{
			if (o != null)
			{
				var s = o.ToString().ToLower();
				if (new[] { "yes", "true", "ok", "y" }.Contains(s)) return true;
				if (new[] { "no", "false", "n" }.Contains(s)) return false;
				bool parsed;
				if (bool.TryParse(s, out parsed)) return parsed;
			}
			return defaultValue;
		}

		#endregion

	}
}
