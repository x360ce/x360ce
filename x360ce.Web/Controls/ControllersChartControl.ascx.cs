using System;
using System.Collections.Generic;
using System.Data;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace x360ce.Web.Controls
{
    public partial class ControllersChartControl : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var db = new x360ce.Engine.Data.x360ceModelContainer();
            var data = ExecuteStoredProcedure(db, "x360ce_GetNewDeviceStats", null);
            var table = data.Tables[0];
            DateTime fromDate = new DateTime();
            DateTime toDate = new DateTime();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var date = (System.DateTime)row["Date"];
                var newDevices = (int)row["NewDevices"];
                var newDevicesSum = (int)row["NewDevicesSum"];
                this.Chart1.Series["Controllers"].Points.Add(newDevicesSum);
                if (i == 0) fromDate = date;
                toDate = date;
            }
            this.Chart1.Titles["Title1"].Text = string.Format("Controllers in Database • Monthly, {0:yyyy-MM} - {1:yyyy-MM}", fromDate, toDate);
        }

        public static DataSet ExecuteStoredProcedure(ObjectContext db, string storedProcedureName, IEnumerable<SqlParameter> parameters)
        {
            var connectionString = ((EntityConnection)db.Connection).StoreConnection.ConnectionString;
            var ds = new DataSet();
            using (var conn = new SqlConnection(connectionString))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = storedProcedureName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            cmd.Parameters.Add(parameter);
                        }
                    }
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(ds);
                    }
                }
            }
            return ds;
        }
    }
}