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

namespace JocysCom.Web.Security.Controls
{
	public partial class UserAdvancedEdit : System.Web.UI.UserControl
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				CleanForm();
				//EmployeeGuidTextBox.Text = Request["EmployeeGuid"] == null ? "" : Request["EmployeeGuid"].ToString();
			}
			//    if (EmployeeGuidTextBox.Text.Length > 0)
			//    {
			//        EmployeeFormView.ChangeMode(FormViewMode.Edit);
			//    }
			//    else
			//    {
			//        EmployeeFormView.ChangeMode(FormViewMode.Insert);
			//    }
			//    UpdatePage();

		}

		public void CleanForm()
		{
			//RecordEnabledCheckBox.Checked = true;
			//EmployeeIdTextBox.Text = "";
			//FirstNameTextBox.Text = "";
			//LastNameTextBox.Text = "";
			//MessageLabel.Visible = false;
			//MessageLabel.Text = string.Empty;
		}

		private void PrepareToUpdate()
		{
			CleanForm();
		}

		public void LoadByUser(string userName)
		{
			PrepareToUpdate();
			//// Get user.
			//MembershipUser user = Membership.GetUser(userName);
			//UserNameLabel.Text = "'" + user.UserName + "'";
			//DataProvider.CompanyTableAdapters.EmployeesTableAdapter eta;
			//eta = new DataProvider.CompanyTableAdapters.EmployeesTableAdapter();
			//DataProvider.Company.EmployeesDataTable et;
			//Guid EmployeeGuid = new Guid(user.ProviderUserKey.ToString());
			//et = eta.GetEmployeeByGuid(EmployeeGuid);
			//DataProvider.Company.EmployeesRow row;
			//if (et.Rows.Count > 0)
			//{
			//    row = et[0];
			//    EmployeeIdTextBox.Text = row.employeeId.ToString();
			//    FirstNameTextBox.Text = row.firstName;
			//    LastNameTextBox.Text = row.lastName;
			//    DepartmentDropDownList.SelectedValue = row.department.ToString();
			//    EmployeeLevelDropDownList.SelectedValue = row.employeeLevel.ToString();
			//    RecordEnabledCheckBox.Checked = row.RecordEnabled;
			//}
			//else
			//{
			//    MessageLabel.Visible = true;
			//    MessageLabel.Text = "Employee by user[GUID={" + EmployeeGuid.ToString() + "};Name='" + userName + "'] was not found! Please check that your 'Employee GUID' is same as 'User Id (GUID)'";
			//}
		}

		public void CreateByUser(MembershipUser user)
		{
			//DataProvider.CompanyTableAdapters.EmployeesTableAdapter eta;
			//eta = new DataProvider.CompanyTableAdapters.EmployeesTableAdapter();
			//int departmentId = int.Parse(DepartmentDropDownList.SelectedValue);
			//int EmployeeLevel = int.Parse(EmployeeLevelDropDownList.SelectedValue);
			//// Get values from user.
			//string password = user.GetPassword();
			//Guid EmployeeGuid = new Guid(user.ProviderUserKey.ToString());
			//// Calculate Password Hash and data.
			//byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
			//Guid passwordHash = new Guid(ResApp.Current.Settings.MacProvider.ComputeHash(bytes));
			//byte[] data = ResApp.Current.Settings.RsaProvider.Encrypt(bytes, false);
			//string passwordData = Convert.ToBase64String(data);
			//eta.InsertEmployee(-1, EmployeeGuid, FirstNameTextBox.Text, LastNameTextBox.Text,
			//    user.UserName, password, user.Email, departmentId,
			//    EmployeeLevel, passwordHash, passwordData, RecordEnabledCheckBox.Checked);
		}

		public void SaveByUser(MembershipUser user)
		{
			//DataProvider.CompanyTableAdapters.EmployeesTableAdapter eta;
			//eta = new DataProvider.CompanyTableAdapters.EmployeesTableAdapter();
			//int EmployeeId = int.Parse(EmployeeIdTextBox.Text);
			//int departmentId = int.Parse(DepartmentDropDownList.SelectedValue);
			//int EmployeeLevel = int.Parse(EmployeeLevelDropDownList.SelectedValue);
			//// Get values from user.
			//string password = user.GetPassword();
			//Guid EmployeeGuid = new Guid(user.ProviderUserKey.ToString());
			//// Calculate Password Hash and data.
			//byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
			//Guid passwordHash = new Guid(ResApp.Current.Settings.MacProvider.ComputeHash(bytes));
			//byte[] data = ResApp.Current.Settings.RsaProvider.Encrypt(bytes, false);
			//string passwordData = Convert.ToBase64String(data);
			//eta.UpdateEmployee(EmployeeId, EmployeeGuid, FirstNameTextBox.Text, LastNameTextBox.Text,
			//    user.UserName, password, user.Email, departmentId,
			//    EmployeeLevel, passwordHash, passwordData, RecordEnabledCheckBox.Checked);
		}

		public static bool AllowToDelte(MembershipUser user, bool deleteUser)
		{
			bool allowToDelete = true;
			////-------------------------------------------------
			//// Custom function
			////-------------------------------------------------
			//DataProvider.CompanyTableAdapters.EmployeesTableAdapter eta;
			//eta = new DataProvider.CompanyTableAdapters.EmployeesTableAdapter();
			//// Try to gey Employee info.
			//DataProvider.Company.EmployeesDataTable et;
			//et = eta.GetEmployeesByNameAndFilter(user.UserName, "", "None");
			//// If Employee was found then we can proceed.
			//if (et.Rows.Count > 0)
			//{
			//    int EmployeeId = et[0].employeeId;
			//    DataProvider.CompanyTableAdapters.QueriesTableAdapter qta;
			//    qta = new DataProvider.CompanyTableAdapters.QueriesTableAdapter();
			//    DataProvider.Company.QueriesDataTable qt;
			//    qt = qta.EmployeeOwnsObjects(EmployeeId);
			//    if (qt.Rows.Count > 0)
			//    {
			//        // If Employee doesn't own any objects inside tickets or actions table then allow tp remove user.
			//        allowToDelete = (qt[0].Owns == 0);
			//        if (allowToDelete && deleteUser) eta.DeleteEmployee(EmployeeId);
			//    }
			//}
			////-------------------------------------------------
			return allowToDelete;
		}

		protected void DepartmentDropDownList_Load(object sender, EventArgs e)
		{
			//DepartmentDropDownList.Items.Clear();
			//BindControls.BindDepartments(DepartmentDropDownList, true, -1);
			//if (DepartmentDropDownList.Items.Count == 2)
			//{
			//    DepartmentDropDownList.SelectedIndex = 1;
			//}
		}

	}
}