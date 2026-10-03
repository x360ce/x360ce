using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public class SearchParameterCollection : List<SearchParameter>, IEnumerable<SqlDataRecord>
	{

		IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
		{
			var sqlRow = new SqlDataRecord(
				new SqlMetaData("ProductGuid", SqlDbType.UniqueIdentifier),
				new SqlMetaData("InstanceGuid", SqlDbType.UniqueIdentifier),
				new SqlMetaData("FileName", SqlDbType.NVarChar, 128),
				new SqlMetaData("FileProductName", SqlDbType.NVarChar, 256)
			);
			foreach (SearchParameter cust in this)
			{
				sqlRow.SetGuid(0, cust.ProductGuid);
				sqlRow.SetGuid(1, cust.InstanceGuid);
				sqlRow.SetString(2, cust.FileName ?? "");
				sqlRow.SetString(3, cust.FileProductName ?? "");
				yield return sqlRow;
			}
		}

		public static SqlParameter GetSqlParameter(IEnumerable<SearchParameter> list, string parameterName = "args")
		{
			var args = new SearchParameterCollection();
			args.AddRange(list);
			var p = new SqlParameter
			{
				ParameterName = parameterName,
				Value = args,
				TypeName = "[dbo].[x360ce_SearchParameterTableType]",
				SqlDbType = SqlDbType.Structured
			};
			return p;
		}

	}
}
