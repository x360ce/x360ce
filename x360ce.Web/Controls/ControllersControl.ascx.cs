using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using x360ce.Engine.Data;

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
                var db = new x360ceModelContainer();
                var rows = db.Products.OrderByDescending(x=>x.InstanceCount).Take(20).ToArray();
                ControllersGridView.DataSource = rows;
                ControllersGridView.DataBind();
            }
        }
    }
}