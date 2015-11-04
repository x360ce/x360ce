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

		public static DataSet GetPresets(Guid? productGuid = null, string fileName = null, int maxRecords = 50, int maxPerProductFile = 2)
		{
			var cmd = new SqlCommand("x360ce_GetPresets");
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.AddWithValue("@ProductGuid", productGuid);
			cmd.Parameters.AddWithValue("@FileName", fileName);
			cmd.Parameters.AddWithValue("@MaxRecords", maxRecords);
			cmd.Parameters.AddWithValue("@MaxPerProductFile", maxPerProductFile);
			var cn = SqlHelper.Current.GetConnectionString("x360ceModelContainer");
			var ds = SqlHelper.Current.ExecuteDataSet(cn, cmd);
			return ds;
		}


	}
}
