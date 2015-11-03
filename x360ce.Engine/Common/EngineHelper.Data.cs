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

	}
}
