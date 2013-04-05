using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace x360ce.Web.Controls
{
    public partial class ControllersControl : System.Web.UI.UserControl
    {

        public class ControllerItem
        {
            public int InstanceCount { get; set; }
            public string ProductName { get; set; }
            //public Guid InstanceGuid { get; set; }        
        }

        protected void Page_Load(object sender, EventArgs e)
        {
               if (!IsPostBack)
            {
                var db = new Data.x360ceModelContainer();
                var rows = db.ExecuteStoreQuery<ControllerItem>("exec x360ce_GetMostPopularControllers");
                ControllersGridView.DataSource = rows;
                ControllersGridView.DataBind();
            }
        }
    }
}