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
			if (cs == null) return null;
			string connectionString;
			if (cs.ProviderName == "System.Data.EntityClient")
			{
				// Use entity connection.
				var e = new System.Data.EntityClient.EntityConnection(cs.ConnectionString);
				connectionString = e.StoreConnection.ConnectionString;
			}
			else
			{
				// Use classic connection.
				connectionString = cs.ConnectionString;
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
			var conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			return cmd.ExecuteReader();
		}

		public T ExecuteDataSet<T>(string connectionString, SqlCommand cmd, string comment = null) where T : DataSet
		{
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
	}
}
