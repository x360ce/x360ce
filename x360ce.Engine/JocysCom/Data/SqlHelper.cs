using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JocysCom.ClassLibrary.Data
{
	public class SqlHelper
	{

		#region "Basic Data"

		static object _currentLock = new object();
		static SqlHelper _Current;
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

		int? _SessionId;
		int? _UserId;
		object SessionParametersLock = new object();

		public void GetSessionParameter(out int? sessionId, out int? userId)
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

		public void SetSessionParameters(int? sessionId, int? userId)
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

		public void AddValue(byte[] data, int id, int destinationIndex)
		{
			var bytes = BitConverter.GetBytes(id);
			Array.Reverse(bytes);
			Array.Copy(bytes, 0, data, destinationIndex, bytes.Length);
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

		public int ExecuteNonQuery(string connectionString, string sql, string comment = null)
		{
			var cmd = new SqlCommand(sql);
			cmd.CommandType = CommandType.Text;
			return ExecuteNonQuery(connectionString, cmd, -1, comment);
		}

		public int ExecuteNonQuery(string connectionString, SqlCommand cmd, string comment = null)
		{
			return ExecuteNonQuery(connectionString, cmd, -1, comment);
		}

		public int ExecuteNonQuery(string connectionString, SqlCommand cmd, int timeout, string comment = null)
		{
			SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder(connectionString);
			if (timeout > -1)
			{
				cmd.CommandTimeout = timeout;
				cb.ConnectTimeout = timeout;
			}
			SqlConnection conn = new SqlConnection(cb.ConnectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			int rowsAffected = cmd.ExecuteNonQuery();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
			return rowsAffected;
		}

		public object ExecuteScalar(string connectionString, SqlCommand cmd, string comment = null)
		{
			SqlConnection conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			// Returns first column of the first row.
			object returnValue = cmd.ExecuteScalar();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
			return returnValue;
		}

		public IDataReader ExecuteReader(string connectionString, SqlCommand cmd, string comment = null)
		{
			SqlConnection conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			return cmd.ExecuteReader();
		}

		public DataSet ExecuteDataSet(string connectionString, SqlCommand cmd, string comment = null)
		{
			int rowsAffected = 0;
			return ExecuteDataSet(connectionString, cmd, ref rowsAffected, comment);
		}

		public DataSet ExecuteDataSet(string connectionString, SqlCommand cmd, ref int rowsAffected, string comment = null)
		{
			SqlConnection conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			SqlDataAdapter adapter = new SqlDataAdapter(cmd);
			DataSet ds = new DataSet();
			rowsAffected = adapter.Fill(ds);
			adapter.Dispose();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
			return ds;
		}

		public DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText, string comment = null, params SqlParameter[] commandParameters)
		{
			var cmd = new SqlCommand(commandText);
			cmd.CommandType = commandType;
			cmd.Parameters.AddRange(commandParameters);
			return ExecuteDataSet(connectionString, cmd, comment);
		}

		public int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, string comment = null, params SqlParameter[] commandParameters)
		{
			var cmd = new SqlCommand(commandText);
			cmd.CommandType = commandType;
			cmd.Parameters.AddRange(commandParameters);
			return ExecuteNonQuery(connectionString, cmd, comment);
		}

		public int ExecuteFill<T>(string connectionString, SqlCommand cmd, T dataSet, string comment = null) where T : DataSet
		{
			SqlConnection conn = new SqlConnection(connectionString);
			cmd.Connection = conn;
			conn.Open();
			SetContext(conn, comment);
			SqlDataAdapter adapter = new SqlDataAdapter(cmd);
			int rowsAffected = adapter.Fill(dataSet, dataSet.Tables[0].TableName);
			adapter.Dispose();
			cmd.Dispose();
			conn.Close();
			conn.Dispose();
			return rowsAffected;
		}

		public DataTable ExecuteDataTable(string connectionString, SqlCommand cmd, ref int rowsAffected, string comment = null)
		{
			DataSet ds = ExecuteDataSet(connectionString, cmd, ref rowsAffected, comment);
			if (ds.Tables.Count == 0) return null;
			return ds.Tables[0];
		}

		public DataTable ExecuteDataTable(string connectionString, SqlCommand cmd, string comment = null)
		{
			int rowsAffected = 0;
			return ExecuteDataTable(connectionString, cmd, ref rowsAffected, comment);
		}

		public DataRow ExecuteDataRow(string connectionString, SqlCommand cmd, string comment = null)
		{
			int rowsAffected = 0;
			return ExecuteDataRow(connectionString, cmd, ref rowsAffected, comment);
		}

		public DataRow ExecuteDataRow(string connectionString, SqlCommand cmd, ref int rowsAffected, string comment = null)
		{
			DataTable table = ExecuteDataTable(connectionString, cmd, ref rowsAffected, comment);
			if (table != null && table.Rows.Count > 0)
			{
				return table.Rows[0];
			}
			return null;
		}

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

		#endregion
	}
}
