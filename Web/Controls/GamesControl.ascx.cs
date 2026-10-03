using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using x360ce.Engine.Data;

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
				var table = Engine.EngineHelper.GetTopGames();
				GamesListView.DataSource = table;
                GamesListView.DataBind();
            }
        }
    }
}