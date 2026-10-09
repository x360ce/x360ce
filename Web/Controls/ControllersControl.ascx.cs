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
				var table = Engine.EngineHelper.GetTopControllers();
                ControllersListView.DataSource = table;
                ControllersListView.DataBind();
            }
        }

        public const int CropTextDefauldMaxLength = 128;

        public static string CropText(object s, int maxLength)
        {
            if (s == null) return string.Empty;
            return CropText(s.ToString(), maxLength);
        }

        public static string CropText(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s) || maxLength == -1) return string.Empty;
            if (maxLength == 0) return s;
            if (maxLength == 0) maxLength = CropTextDefauldMaxLength;
            if (s.Length > maxLength)
            {
                s = s.Substring(0, maxLength - 3);
                // Find last separator and crop there...
                int ls = s.LastIndexOf(' ');
                if (ls > 0) s = s.Substring(0, ls);
                s += "...";
            }
            return s;
        }

    }
}