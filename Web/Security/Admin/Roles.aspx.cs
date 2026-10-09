using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Security;

namespace JocysCom.Web.Security.Admin
{
	public partial class Roles : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				// If user name was submitted then...
				if (Request["Role"] == null)
				{
					RoleList1.DataBind();
				}
				else
				{
					// Show user edit form.
					//LoadByRole(Request["Role"].ToString());
				}
			}

			RoleList1.RoleSelected += new EventHandler<GridViewCommandEventArgs>(RoleList1_RoleSelected);
			RoleList1.RolesDeleted += new EventHandler<ItemActionsEventArgs>(RoleList1_RolesDeleted);
			RoleEdit1.Updated += new EventHandler<RoleEditEventArgs>(RoleEdit1_Updated);
			//RoleList1.AllowCreate = true;
			//RoleList1.AllowEdit = true;
			//RoleList1.AllowDelete = true;
			//UserList1.ShowColumn(UserColumnsEnum.Edit, false);
			//UserList1.ShowColumn(UserColumnsEnum.IsApproved, false);
		}

		void RoleEdit1_Updated(object sender, RoleEditEventArgs e)
		{
			//RoleList1.RefreshList();
		}

		void RoleList1_RolesDeleted(object sender, ItemActionsEventArgs e)
		{
			// If one of the deleted users is now open in edit form then we need to close it.
			//if (e.Items.Contains(RoleEdit1.UserName)){
			//RoleEdit1.Visible = false;
			//UsersList1.Visible = false;
			//}
		}

		void RoleList1_RoleSelected(object sender, GridViewCommandEventArgs e)
		{
			Guid roleId = new Guid(e.CommandArgument.ToString());
			LoadByRole(roleId);
			RoleEdit1.Visible = true;
			UserList1.Visible = true;
		}

		protected void SelectRoleButton_Click(object sender, EventArgs e)
		{
			//RoleList1.RefreshList(SelectRoleTextBox.Text, RoleColumnsEnum.RoleName, SortDirection.Ascending);
			//LoadByRole(SelectRoleTextBox.Text);
		}

		public void LoadByRole(Guid roleId)
		{
			//UserList1.GetData("/", roleName, 0, 10000, false);
			RoleEdit1.LoadRole(roleId);
		}

	}
}