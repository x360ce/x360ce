using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Linq;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security.Controls
{
	public partial class UserList : System.Web.UI.UserControl
	{

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				//ShowColumn(UserColumnsEnum.CreateDate, false);
				//string roleName = (Request["Role"] == null) ? "" : Request["Role"].ToString();
				//RoleName = roleName;
				//// Can be "InRole" or "NotInRole".
				//string searchType = (Request["Filter"] == null) ? "" : Request["Filter"].ToString();
				////UserList1.ShowColumn(UserColumnsEnum.Edit, false);
				////UserList1.ShowColumn(UserColumnsEnum.Approved, false);
				////UserList1.ShowColumn(UserColumnsEnum.LastLoginDate, false);
				//// If role was not specified then show all users by inverting search.
				//bool invertSearch = (searchType == "Available") || String.IsNullOrEmpty(roleName);
				//InvertCheckBox.Checked = invertSearch;
			}
		}

		#region Public Events

		public event EventHandler<GridViewCommandEventArgs> UserSelected;
		//public event EventHandler<ItemActionsEventArgs> UsersDeleted;

		/// <summary>
		/// Rise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseUserSelected(GridViewCommandEventArgs e)
		{
			// If event was attached then fire it.
			if (UserSelected != null) UserSelected(UsersGridView, e);
		}

		/// <summary>
		/// Rise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseUsersDeleted(ItemActionsEventArgs e)
		{
			// If event was attached then fire it.
			//if (UsersDeleted != null) UsersDeleted(this, e);
		}

		#endregion

		#region "Hidden Server Buttons"

		protected void RefreshListButton_Click(object sender, EventArgs e)
		{
		}

		protected void DeleteSelectedButton_Click(object sender, EventArgs e)
		{
			//DeleteSelected();
		}

		protected void AddSelectedButton_Click(object sender, EventArgs e)
		{
			//AddSelected();
		}

		protected void RemoveSelectedButton_Click(object sender, EventArgs e)
		{
			//RemoveSelected();
		}


		#endregion

		#region Helper Functions
		private System.Collections.Generic.List<string> GetItemKeys(bool useTextBox)
		{
			System.Collections.Generic.List<string> list;
			list = new System.Collections.Generic.List<string>();
			//string userName;
			//int length;
			//if (useTextBox)
			//{
			//    string[] userNames = ItemsTextBox.Text.Replace(" ", "").Split(',');
			//    length = userNames.Length;
			//    for (int i = 0; i < length; i++)
			//    {
			//        list.Add(userNames[i]);
			//    }
			//}
			//else
			//{
			//    GridViewRow gridRow;
			//    System.Web.UI.WebControls.CheckBox checkBox;
			//    length = ItemsGridView.Rows.Count;
			//    for (int i = 0; i < length; i++)
			//    {
			//        gridRow = ItemsGridView.Rows[i];
			//        userName = gridRow.Cells[(int)UserColumnsEnum.UserName].Text;
			//        checkBox = (System.Web.UI.WebControls.CheckBox)gridRow.Cells[(int)UserColumnsEnum.Check].FindControl("ItemIsSelected");
			//        if (checkBox.Checked) list.Add(userName);
			//    }
			//}
			return list;
		}
		#endregion

		#region Refresh List

		public void RefreshList()
		{
			//ItemsGridView.DataBind();
		}

		public void RefreshList(string username, UserColumnsEnum sortColumn, SortDirection sortDirection)
		{
			//RefreshList();
			//ItemsGridView.Sort(sortColumn.ToString(), sortDirection);
		}

		#endregion

		#region Selected Actions
		/// <summary>
		/// Delete selected users and remove UserAdvanceds if there is no tickets attached to them.
		/// </summary>
		public string DeleteSelected()
		{
			string results = string.Empty;
			//System.Collections.Generic.List<string> list;
			//list = GetItemKeys(true);
			//int length = list.Count;
			//for (int i = 0; i < length; i++)
			//{
			//    string userName = list[i];
			//    MembershipUser user = Membership.GetUser(userName);
			//    bool allowToDelte = UserAdvancedEdit.AllowToDelte(user, true);
			//    if (allowToDelte)
			//    {
			//        System.Web.Security.Membership.DeleteUser(userName, true);

			//    }
			//    else
			//    {
			//        results = "UserAdvanceds who owns ticket actions can't be deleted.";
			//    }

			//}
			//// Clean list.
			//ItemsTextBox.Text = "";
			//RefreshList();
			//RiseUsersDeleted(new ItemActionsEventArgs(ItemActionsEnum.Delete, list));
			return results;
		}

		/// <summary>
		/// Remove selected users from role.
		/// </summary>
		public void RemoveSelected()
		{
			////Response.Write("Remove Selected<br />");
			//System.Collections.Generic.List<string> list;
			//list = GetItemKeys(true);
			//string roleName = ItemsDataSource.SelectParameters["RoleName"].DefaultValue;
			//int length = list.Count;
			//for (int i = 0; i < length; i++)
			//{
			//    string userName = list[i];
			//    //Response.Write("UserName: "+userName + "; RoleName: " + roleName);
			//    if (System.Web.Security.Roles.IsUserInRole(userName, roleName))
			//    {
			//        System.Web.Security.Roles.RemoveUserFromRole(userName, roleName);
			//        //Response.Write(" - Removed<br />");
			//    }
			//}
			//// Clean list.
			//ItemsTextBox.Text = "";
			//RefreshList();
		}

		/// <summary>
		/// Add selected users to role.
		/// </summary>
		public void AddSelected()
		{
			//    System.Collections.Generic.List<string> list;
			//    list = GetItemKeys(true);
			//    string roleName = ItemsDataSource.SelectParameters["RoleName"].DefaultValue;
			//    int length = list.Count;
			//    for (int i = 0; i < length; i++)
			//    {
			//        string userName = list[i];
			//        if (userName.Length > 0 && roleName.Length > 0)
			//        {
			//            if (!System.Web.Security.Roles.IsUserInRole(userName, roleName))
			//            {
			//                System.Web.Security.Roles.AddUserToRole(userName, roleName);
			//            }
			//        }
			//    }
			//    // Clean list.
			//    ItemsTextBox.Text = "";
			//    RefreshList();
		}

		#endregion

		#region Properties

		public bool ReadOnly
		{
			get { return m_readOnly; }
			set
			{
				m_readOnly = value;
				refreshButtons();
			}
		}
		private bool m_readOnly;

		public string RoleName
		{
			get { return this.m_roleName; }
			set
			{
				this.m_roleName = value;
				ItemNameLabel.Text = value;
				refreshButtons();
			}
		}
		private string m_roleName;

		private void refreshButtons()
		{
			//bool newDelete = String.IsNullOrEmpty(this.m_roleName);
			//RolePanel.Visible = !newDelete;
			//// Hide buttons by default.
			//AddRemoveCell.Style["display"] = "none";
			//NewDeleteCell.Style["display"] = "none";
			//if (newDelete && !ReadOnly)
			//{
			//    NewDeleteCell.Style.Remove("display");
			//}
			//if (!newDelete && !ReadOnly)
			//{
			//    AddRemoveCell.Style.Remove("display");
			//}
		}

		#endregion

		public void GetData(string applicationName, string roleName, int pageIndex, int pageSize, bool invert)
		{
			//ItemsTextBox.Text = "";
			//ApplicationNameTextBox.Text = applicationName;
			//InvertCheckBox.Checked = invert;
			//SearchFilterLabel.Text = (invert) ? "Not" : "";
			//RoleName = roleName;
			//// Set data source parameters.
			//ItemsDataSource.SelectParameters["ApplicationName"].DefaultValue = applicationName.ToString();
			//ItemsDataSource.SelectParameters["RoleName"].DefaultValue = roleName.ToString();
			//ItemsDataSource.SelectParameters["PageIndex"].DefaultValue = pageIndex.ToString();
			//ItemsDataSource.SelectParameters["PageSize"].DefaultValue = pageSize.ToString();
			//ItemsDataSource.SelectParameters["Invert"].DefaultValue = invert.ToString();
		}

		public void ShowColumn(UserColumnsEnum index, bool show)
		{
			//ItemsGridView.Columns[(int)index].Visible = show;
		}

		protected void ItemsGridView_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			var gridView = (GridView)sender;
			if (e.CommandName == "DeleteItem")
			{
				System.Web.Security.Membership.DeleteUser(e.CommandArgument.ToString());
				UsersGridView.DataBind();
			}
			if (e.CommandName == "SelectItem")
			{
				// If event was attached then...
				RiseUserSelected(e);
			}
		}

		public User[] users
		{
			get { return (User[])ViewState["_users"]; }
			set { ViewState["_users"] = value; }
		}

		protected string GetUserName(Guid userId){
			return users.First(x => x.UserId == userId).UserName;
		}

		protected void UsersEntityDataSource_Selected(object sender, EntityDataSourceSelectedEventArgs e)
		{
			var userIds = e.Results.Cast<Membership>().Select(x => x.UserId).ToArray();
			var db = new SecurityEntities();
			users = (from row in db.Users where userIds.Contains(row.UserId) select row).ToArray();
			db.Dispose();
			db = null;
		}

	}
}