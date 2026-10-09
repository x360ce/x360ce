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

	public class ItemActionsEventArgs : System.EventArgs
	{

		#region Properties

		public System.Collections.Generic.List<string> Items
		{ get { return m_items; } set { m_items = value; } }
		private System.Collections.Generic.List<string> m_items;

		public ItemActionsEnum Action
		{ get { return m_action; } set { m_action = value; } }
		private ItemActionsEnum m_action;

		#endregion

		public ItemActionsEventArgs(ItemActionsEnum action, System.Collections.Generic.List<string> items)
		{
			this.m_action = action;
			this.m_items = items;
		}

	}
}
