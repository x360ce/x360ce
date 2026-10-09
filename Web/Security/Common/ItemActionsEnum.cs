using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace JocysCom.Web.Security
{
	public enum ItemActionsEnum
	{
		None = 0,
		New = 1,
		Delete = 2,
		Add = 3,
		Remove = 4,
	}
}
