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
			var cn = SqlHelper.Current.GetConnectionString("x360ceModelContainer");
			var table = SqlHelper.Current.ExecuteDataTable(cn, cmd);
			return table;
		}

		public static DataTable GetTopGames()
		{
			var cmd = new SqlCommand("x360ce_GetTopGames");
			cmd.CommandType = CommandType.StoredProcedure;
			var cn = SqlHelper.Current.GetConnectionString("x360ceModelContainer");
			var table = SqlHelper.Current.ExecuteDataTable(cn, cmd);
			return table;
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
			var cn = SqlHelper.Current.GetConnectionString("x360ceModelContainer");
			var ds = SqlHelper.Current.ExecuteDataSet(cn, cmd);
			return ds;
		}

	}
}
