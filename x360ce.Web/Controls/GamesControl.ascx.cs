using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace x360ce.Web.Controls
{
    public partial class GamesControl : System.Web.UI.UserControl
    {

        public class GameItem
        {
            public int InstanceCount { get; set; }
            public string FileProductName { get; set; }
            //public Guid InstanceGuid { get; set; }        
        }

        protected void Page_Load(object sender, EventArgs e)
        {
               if (!IsPostBack)
            {
                var db = new Data.x360ceModelContainer();
                var rows = db.Programs.OrderByDescending(x=>x.InstanceCount).Take(20).ToArray();
                GamesGridView.DataSource = rows;
                GamesGridView.DataBind();
            }
        }
    }
}