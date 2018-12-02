using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
					if (_Current == null) _Current = new SqlHelper();
					return _Current;
				}
			}
		}

		#region Session Parameters / SetContext

		int? _SessionId;
		int? _UserId;
		object SessionParametersLock = new object();

		public virtual void GetSessionParameter(out int? sessionId, out int? userId)
		{
			lock (SessionParametersLock)
			{
				var hc = System.Web.HttpContext.Current;
				// If application is not website.
				if (hc == null)
				{
					sessionId = _SessionId;
					userId = _UserId;
				}
				else
				{
					sessionId = hc.Session == null ? null : (int?)hc.Session["_SessionId"];
					userId = hc.Session == null ? null : (int?)hc.Session["_UserId"];
				}
			}
		}

		public virtual void SetSessionParameters(int? sessionId, int? userId)
		{
			lock (SessionParametersLock)
			{
				var hc = System.Web.HttpContext.Current;
				// If application is not website.
				if (hc == null)
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

		public void SetContext(IDbConnection connection, string comment = null)
		{
			int? sessionId;
			int? userId;
			GetSessionParameter(out sessionId, out userId);
			if (!sessionId.HasValue) return;
			if (!userId.HasValue) return;
			// use the existing open connection to set the context info
			var command = connection.CreateCommand();
			command.CommandText = "SET CONTEXT_INFO @ContextUserID";
			command.CommandType = CommandType.Text;
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
			var p = new SqlParameter("@ContextUserID", data);
			command.Parameters.Add(p);
			command.ExecuteNonQuery();
		}

		#endregion

		#region Helper Methods

		public void AddValue(byte[] data, int id, int destinationIndex)
		{
			var bytes = BitConverter.GetBytes(id);
			Array.Reverse(bytes);
			Array.Copy(bytes, 0, data, destinationIndex, bytes.Length);
		}

		public string GetConnectionString(string name)
		{
			// Try to find entity connection.
			var cs = ConfigurationManager.ConnectionStrings[name];
			// If configuration section with not found then return.
			if (cs == null)
				return null;
			var connectionString = cs.ConnectionString;
			if (string.Compare(cs.ProviderName, "System.Data.EntityClient", true) == 0)
			{
				// Get connection string from entity connection string.
				var ecsb = new System.Data.EntityClient.EntityConnectionStringBuilder(cs.ConnectionString);
				connectionString = ecsb.ProviderConnectionString;
			}
			var builder = new SqlConnectionStringBuilder(connectionString);
			if (!builder.ContainsKey("Application Name") || ".Net SqlClient Data Provider".Equals(builder["Application Name"]))
			{
				var asm = Assembly.GetEntryAssembly();
				if (asm == null) asm = Assembly.GetExecutingAssembly();
				var appPrefix = asm.GetName().Name.Replace(".", "");
				var appName = string.Format("{0}", appPrefix);
				builder.Add("Application Name", appName);
				connectionString = builder.ToString();
			}
			return connectionString;
		}

		public void SetReadUncommited(IDbConnection connection)
		{
			// Use the existing open connection to set the context info
			var cmd = connection.CreateCommand();
			cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery();
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
			SetContext(conn, comment);
			int rv = cmd.ExecuteNonQuery();
			cmd.Dispose();
			// Dispose calls conn.Close() internally.
			conn.Dispose();
			return rv;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public int ExecuteNonQuery(string connectionString, string cmdText, string comment = null, int? timeout = null)
		{
			var cmd = new SqlCommand(cmdText);
			cmd.CommandType = CommandType.Text;
			return ExecuteNonQuery(connectionString, cmd, comment, timeout);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
		public int ExecuteNonQuery(string connectionString, CommandType commandType, string cmdText, string comment = null, params SqlParameter[] commandParameters)
		{
			var cmd = new SqlCommand(cmdText);
			cmd.CommandType = commandType;
			if (commandParameters != null && commandParameters.Length > 0)
			{
				cmd.Parameters.AddRange(commandParameters);
			}
			return ExecuteNonQuery(connectionString, cmd, comment);
		}

		public object ExecuteScalar(string connectionString, SqlCommand cmd, string comment = null)
		{
			//var sql = ToSqlCommandString(cmd);
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
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
			SetContext(conn, comment);
			return cmd.ExecuteReader();
		}

		public T ExecuteDataSet<T>(string connectionString, SqlCommand cmd, string comment = null) where T : DataSet
		{
			//var sql = ToSqlCommandString(cmd);
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			var adapter = new SqlDataAdapter(cmd);
			var ds = Activator.CreateInstance<T>();
			int rowsAffected = ds.GetType() == typeof(DataSet)
				? adapter.Fill(ds)
				: adapter.Fill(ds, ds.Tables[0].TableName);
			adapter.Dispose();
			cmd.Dispose();
			// Dispose calls conn.Close() internally.
			conn.Dispose();
			return (T)ds;
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
			if (table == null) return null;
			var list = new List<T>();
			var props = typeof(T).GetProperties();
			foreach (DataRow row in table.Rows)
			{
				var item = Activator.CreateInstance<T>();
				foreach (var prop in props)
				{
					foreach (DataColumn column in table.Columns)
					{
						if (!prop.CanWrite) continue;
						if (string.Compare(prop.Name, column.ColumnName, true) != 0) continue;
						if (row.IsNull(column.ColumnName)) continue;
						prop.SetValue(item, row[column.ColumnName], null);
					}
				}
				list.Add(item);
			}
			return list;
		}

		/// <summary>
		/// Convert List to DataTable. Can be used to pass data into stored procedures. 
		/// </summary>
		public static DataTable ConvertToTable<T>(IEnumerable<T> list)
		{
			if (list == null) return null;
			var table = new DataTable();
			var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
			foreach (var prop in props)
			{
				table.Columns.Add(prop.Name, prop.PropertyType);
			}
			var values = new object[props.Length];
			foreach (T item in list)
			{
				for (int i = 0; i < props.Length; i++)
				{
					values[i] = props[i].GetValue(item, null);
				}
				table.Rows.Add(values);
			}
			return table;
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
					retval = "'" + sp.Value.ToString().Replace("'", "''") + "'";
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
					retval = sp.Value.ToString().Replace("'", "''");
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
