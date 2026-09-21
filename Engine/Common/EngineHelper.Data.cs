using JocysCom.ClassLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public partial class EngineHelper
	{

		public static DataTable GetTopControllers()
		{
			var cmd = new SqlCommand("x360ce_GetTopControllers");
			cmd.CommandType = CommandType.StoredProcedure;
			var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
			var table = SqlHelper.Current.ExecuteDataTable(cn, cmd);
			return table;
		}

		public static DataTable GetTopGames()
		{
			var cmd = new SqlCommand("x360ce_GetTopGames");
			cmd.CommandType = CommandType.StoredProcedure;
			var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
			var table = SqlHelper.Current.ExecuteDataTable(cn, cmd);
			return table;
		}

		/// <summary>One row per month with the running total of controllers, refreshed by the procedure itself.</summary>
		public static DataTable GetNewDeviceStats()
		{
			var cmd = new SqlCommand("x360ce_GetNewDeviceStats");
			cmd.CommandType = CommandType.StoredProcedure;
			var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
			var table = SqlHelper.Current.ExecuteDataTable(cn, cmd);
			return table;
		}

		/// <summary>What the service says about itself: its version, its clock, and whether its database answers.</summary>
		/// <remarks>
		/// A database that does not answer is reported in the answer rather than as a fault, because
		/// the point of asking is to find that out; the caller is the Test button, not a game.
		/// </remarks>
		public static ServerInfo GetServerInfo()
		{
			var info = new ServerInfo
			{
				Version = typeof(EngineHelper).Assembly.GetName().Version.ToString(),
				UtcTime = DateTime.UtcNow,
				Database = "",
				Error = "",
			};
			try
			{
				var cmd = new SqlCommand("SELECT DB_NAME() AS [Name], GETUTCDATE() AS [Utc]");
				var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
				var row = SqlHelper.Current.ExecuteDataTable(cn, cmd).Rows[0];
				info.Database = (string)row["Name"];
				info.DatabaseUtcTime = DateTime.SpecifyKind((DateTime)row["Utc"], DateTimeKind.Utc);
			}
			catch (Exception ex)
			{
				info.Error = ex.Message;
			}
			return info;
		}

		public static DataTable GetDataTable(IEnumerable<SearchParameter> list)
		{
			DataTable table = new DataTable();
			table.Columns.Add("ProductGuid", typeof(Guid));
			table.Columns.Add("InstanceGuid", typeof(Guid));
			table.Columns.Add("FileName", typeof(string));
			table.Columns.Add("FileProductName", typeof(string));
			foreach (var item in list)
			{
				table.Rows.Add(item.ProductGuid, item.InstanceGuid, item.FileName, item.FileProductName);
			}
			return table;
		}

		public static DataSet GetPresets(SearchParameter[] args, int? MaxRecords = null, int? MaxPerProduct = null, int? MaxPerProductFile = null)
		{
			var p = SqlHelper.ConvertToTable(args);
			var cmd = new SqlCommand("x360ce_GetPresets");
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddWithValue("@args", p);
			if (MaxRecords.HasValue) cmd.Parameters.AddWithValue("@MaxRecords", MaxRecords);
			if (MaxPerProduct.HasValue) cmd.Parameters.AddWithValue("@MaxPerProduct", MaxPerProduct);
			if (MaxPerProductFile.HasValue) cmd.Parameters.AddWithValue("@MaxPerProductFile", MaxPerProductFile);
			var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
			var ds = SqlHelper.Current.ExecuteDataSet(cn, cmd);
			return ds;
		}

		public static DataSet GetSettings(SearchParameter[] args)
		{
			var p = SqlHelper.ConvertToTable(args);
			var cmd = new SqlCommand("x360ce_GetUserInstances");
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddWithValue("@args", p);
			var cn = SqlHelper.GetConnectionString("x360ceModelContainer");
			var ds = SqlHelper.Current.ExecuteDataSet(cn, cmd);
			return ds;
		}

	}
}
