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
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Data.EntityClient;
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security.Controls
{
	[ParseChildren(false), ControlValueProperty("DirectoryPath"), DefaultProperty("DirectoryPath")]
	public partial class RoleList : UserControl
	{

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				var s = new List<string>();
				ItemNameLabel.Text = (Request["User"] == null) ? "" : Request["User"].ToString();
				// Can be "InRole" or "NotInRole".
				SearchFilterLabel.Text = (Request["Filter"] == null) ? "" : Request["Filter"].ToString();
				////UserList1.ShowColumn(UserColumnsEnum.Edit, false);
				////UserList1.ShowColumn(UserColumnsEnum.Approved, false);
				////UserList1.ShowColumn(UserColumnsEnum.LastLoginDate, false);
				//// If role was not specified then show all users by inverting search.
				//bool invertSearch = (searchType == "Available") || String.IsNullOrEmpty(userName);
				//InvertCheckBox.Checked = invertSearch;
			}
		}

		public void SetUserAndFilter(Guid userId, string filter)
		{
			SearchUserId.Text = userId.ToString();
			SearchFilterLabel.Text = filter;
			RolesGridView.DataBind();
		}

		#region Public Events

		public event EventHandler<GridViewCommandEventArgs> RoleSelected;
		public event EventHandler<ItemActionsEventArgs> RolesDeleted;

		/// <summary>
		/// Rise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseRoleSelected(GridViewCommandEventArgs e)
		{
			// If event was attached then fire it.
			if (RoleSelected != null) RoleSelected(RolesGridView, e);
		}

		/// <summary>
		/// Rise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseRolesDeleted(ItemActionsEventArgs e)
		{
			// If event was attached then fire it.
			if (RolesDeleted != null) RolesDeleted(this, e);
		}

		#endregion

		#region Helper Functions
		System.Collections.Generic.List<string> GetItemKeys(bool useTextBox)
		{
			System.Collections.Generic.List<string> list;
			list = new System.Collections.Generic.List<string>();

			int length;
			//if (useTextBox)
			//{
				string[] userNames = ItemsTextBox.Text.Replace(" ", "").Split(new char[]{','},  StringSplitOptions.RemoveEmptyEntries);
				length = userNames.Length;
				for (int i = 0; i < length; i++)
				{
					list.Add(userNames[i]);
				}
			//}
			//else
			//{
			//string roleName;
			//    GridViewRow gridRow;
			//    System.Web.UI.WebControls.CheckBox checkBox;
			//    length = ItemsGridView.Rows.Count;
			//    for (int i = 0; i < length; i++)
			//    {
			//        gridRow = ItemsGridView.Rows[i];
			//        roleName = gridRow.Cells[(int)RoleColumnsEnum.RoleName].Text;
			//        checkBox = (System.Web.UI.WebControls.CheckBox)gridRow.Cells[(int)RoleColumnsEnum.Check].FindControl("ItemIsSelected");
			//        if (checkBox.Checked) list.Add(roleName);
			//    }
			//}
			return list;
		}
		#endregion

		#region Selected Actions

		/// <summary>
		/// Delete selected roles from user.
		/// </summary>
		public string DeleteSelected()
		{
			string results = string.Empty;
			//    System.Collections.Generic.List<string> list;
			//    list = GetItemKeys(true);
			//    int length = list.Count;
			//    for (int i = 0; i < length; i++)
			//    {
			//        string roleName = list[i];

			//        // Remove all members first.
			//        string[] users = System.Web.Security.Roles.GetUsersInRole(roleName);
			//        for (int u = 0; u < users.Length; u++)
			//        {
			//            System.Web.Security.Roles.RemoveUserFromRole(users[u], roleName);
			//        }
			//        // Now we can delete role.
			//        System.Web.Security.Roles.DeleteRole(roleName);
			//    }
			//    // Clean list.
			//    ItemsTextBox.Text = "";
			//    RefreshList();
			//    RiseRolesDeleted(new ItemActionsEventArgs(ItemActionsEnum.Delete, list));
			return results;
		}

		/// <summary>
		/// Remove selected users from role.
		/// </summary>
		public void RemoveSelected()
		{
			//System.Collections.Generic.List<string> list;
			//list = GetItemKeys(true);
			//string userName = ItemsDataSource.SelectParameters["UserName"].DefaultValue;
			//int length = list.Count;
			//for (int i = 0; i < length; i++)
			//{
			//    string roleName = list[i];
			//    //Response.Write(userName + " " + roleName);
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


		protected void AddRoleButton_Click(object sender, EventArgs e) { AddSelected(); }


		/// <summary>
		/// Add selected roles to user.
		/// </summary>
		public void AddSelected()
		{
			System.Collections.Generic.List<string> list;
			list = GetItemKeys(true);
			var user = User.GetUser(new Guid(SearchUserId.Text));
			int length = list.Count;
			for (int i = 0; i < length; i++)
			{
				string roleName = list[i];
				if (user.UserName.Length > 0 && roleName.Length > 0)
				{
					if (!Roles.IsUserInRole(user.UserName, roleName))
					{
						Roles.AddUserToRole(user.UserName, roleName);
					}
				}
			}
			// Clean list.
			ItemsTextBox.Text = "";
			RolesGridView.DataBind();
		}

		#endregion

		#region Permissions

		public bool m_allowCreate;
		public bool AllowCreate
		{
			set
			{
				this.m_allowCreate = value;
			}
			get { return this.m_allowCreate; }
		}

		[DefaultValue(true)]
		public bool AllowEdit
		{
			set { ViewState["_allowEdit"] = value; }
			get { return (bool?)ViewState["_allowEdit"] ?? true; }
		}

		[DefaultValue(true)]
		public bool AllowDelete
		{
			set { ViewState["_allowDelete"] = value; }
			get { return (bool?)ViewState["_allowDelete"] ?? true; }
		}

		#endregion

		protected void RolesGridView_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			GridView gridView = (GridView)sender;
			if (e.CommandName == "DeleteItem")
			{
				//System.Web.Security.Roles.DeleteRole(e.CommandArgument.ToString());
				RolesGridView.DataBind();
			}
			if (e.CommandName == "SelectItem")
			{
				// If event was attached then...
				if (RoleSelected != null) RoleSelected(sender, e);
			}
		}

		public void ShowColumn(RoleColumnsEnum index, bool show)
		{
			RolesGridView.Columns[(int)index].Visible = show;
		}

		protected void RolesDataSource_QueryCreated(object sender, QueryCreatedEventArgs e)
		{
			if (!string.IsNullOrEmpty(SearchUserId.Text))
			{
				var userId = new Guid(SearchUserId.Text);
				var db = new SecurityEntities();
				if (SearchFilterLabel.Text == "InRole")
				{
					e.Query = db.Users.Where(x => x.UserId == userId).SelectMany(x => x.Roles).OrderBy(x => x.RoleName);
				}
				else if (SearchFilterLabel.Text == "NotInRole")
				{
					var q1 = db.Users.Where(x => x.UserId == userId).SelectMany(x => x.Roles);
					e.Query = db.Roles.Except(q1).OrderBy(x => x.RoleName);
				}

			}
		}

	}
}