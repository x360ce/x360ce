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
	public partial class Users : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
            //if (!Page.IsPostBack)
            //{
            //    // If user name was submited then...
            //    if (Request["User"] != null)
            //    {
            //        // Show user edit form.
            //        LoadByUser(Request["User"].ToString());
            //    }
            //}

            //UserList1.ShowColumn(UserColumnsEnum.IsApproved, false);
            //RoleList1.ShowColumn(RoleColumnsEnum.Edit, false);
            //if (!Page.IsPostBack)
            //{
            //    UpdateRolesList();
            //}
            UserList1.UserSelected += new EventHandler<GridViewCommandEventArgs>(UserList1_UserSelected);
            //UserList1.UsersDeleted += new EventHandler<ItemActionsEventArgs>(UserList1_UsersDeleted);
            //UserEdit1.Updated += new EventHandler<UserEditEventArgs>(UserEdit1_Updated);
		}

		void UserList1_UsersDeleted(object sender, ItemActionsEventArgs e)
		{
            //// If one of the deleted users is now open in edit form then we need to close it.
            //if (e.Items.Contains(UserEdit1.UserName)){
            //    UserEdit1.Visible = false;
            //    RoleList1.Visible = false;
            //    UserAdvancedEdit1.Visible = false;
            //}
		}

		void UserList1_UserSelected(object sender, GridViewCommandEventArgs e)
		{
            var userId = new Guid(e.CommandArgument.ToString());
            LoadByUser(userId);
            UserEdit1.Visible = true;
            //RoleList1.Visible = true;
            //UserAdvancedEdit1.Visible = true;
		}

		void UserEdit1_Updated(object sender, UserEditEventArgs e)
        {
        //    // User was just updated. We need update UserAdvanced info too.
        //    UserList1.RefreshList();
        //    UserAdvancedEdit1.SaveByUser(e.User);
		}

		protected void btnAdd_Click(object sender, EventArgs e)
		{
			//// If role does not exist.
			//if (!Roles.RoleExists(tbxRole.Text))
			//{
			//    Roles.CreateRole(tbxRole.Text);
			//    tbxRole.Text = "";
			//    UpdateRolesList();
			//}
		}

		protected void UpdateRolesList()
		{
            //UserList1.GetData("/", "", 0, 10000, true);
		}
		

		public void LoadByUser(Guid userId)
		{
            //MembersPanel.Visible = true;
            //RoleList1.GetData("/", username, 0, 10000, false);
			RoleList1.SetUserAndFilter(userId, "InRole");
			UserEdit1.LoadUser(userId);
            //UserAdvancedEdit1.LoadByUser(username);
		}

		protected void SelectUserButton_Click(object sender, EventArgs e)
        {
        //    UserList1.RefreshList(SelectUserTextBox.Text, UserColumnsEnum.CreateDate, SortDirection.Descending);
        //    LoadByUser(SelectUserTextBox.Text);
		}

	}
}