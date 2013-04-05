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
            public string FileName { get; set; }
            //public Guid InstanceGuid { get; set; }        
        }

        protected void Page_Load(object sender, EventArgs e)
        {
               if (!IsPostBack)
            {
                var db = new Data.x360ceModelContainer();
                var rows = db.ExecuteStoreQuery<GameItem>("exec x360ce_GetMostPopularGames");
                GamesGridView.DataSource = rows;
                GamesGridView.DataBind();
            }
        }
    }
}