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
using JocysCom.WebSites.Engine.Security.Data;

namespace JocysCom.Web.Security.Controls
{

	public partial class RoleEdit : System.Web.UI.UserControl
	{
        protected void Page_Load(object sender, EventArgs e)
        {
			if (!Page.IsPostBack)
			{
				PrepareToCreate();
			}
        }

        #region Properties

        private PostCreateModeEnum m_postCreateMode;

        public PostCreateModeEnum PostCreateMode
        {
            get
            {
                if (m_postCreateMode == PostCreateModeEnum.None)
                {
                    m_postCreateMode = PostCreateModeEnum.Create;
                }
                return m_postCreateMode;
            }
            set { m_postCreateMode = value; }
        }

        public bool AllowUpdateRoleGuid
        {
            get { return m_allowUpdateRoleGuid; }
            set { m_allowUpdateRoleGuid = value; }
        }
        private bool m_allowUpdateRoleGuid;

        #endregion

        #region Events

        // Define a public event members.
        public event EventHandler<RoleEditEventArgs> Updated;
        public event EventHandler<RoleEditEventArgs> Created;

        /// <summary>
        /// Rise the event within the method.  
        /// </summary>
        /// <param name="e"></param>
        protected virtual void RiseUpdated(RoleEditEventArgs e)
        {
            // If event was attached then fire it.
            if (Updated != null) Updated(this, e);
        }

        /// <summary>
        /// Raise the event within the method.  
        /// </summary>
        /// <param name="e"></param>
        protected virtual void RiseCreated(RoleEditEventArgs e)
        {
            // If event was attached then fire it.
            if (Created != null) Created(this, e);
        }

        #endregion

        #region Create

        protected void CreateRoleButton_Click(object sender, EventArgs e)
        {
            CreateRole();
        }

        public void PrepareForm()
        {
            RoleNameLabel.Text = "";
        }

        public void PrepareToCreate()
        {
			PrepareForm();
			ProviderNameTextBox.Text = System.Web.Security.Roles.Provider.Name;
			// Find application guid.
			ApplicationIdTextBox.Text = SecurityEntities.ApplicationId.ToString();
			// Generate new guid for role.
			RoleIdTextBox.Text = Guid.NewGuid().ToString();
			// Empty fields.
			RoleNameTextBox.Text = string.Empty;
			DescriptionTextBox.Text = string.Empty;
			// Set other input properties.
			ProviderNameTextBox.ReadOnly = true;
			ApplicationIdTextBox.ReadOnly = true;
			RoleIdTextBox.ReadOnly = false;
			RoleNameTextBox.ReadOnly = false;
			CreateRoleButton.Visible = true;
			UpdateRoleButton.Visible = false;
        }

		public void CreateRole()
		{
			CreatedRoleTextBox.Text = "";
			var db = new SecurityEntities();
			var role = new Role()
			{
				ApplicationId = new Guid(ApplicationIdTextBox.Text),
				Description = DescriptionTextBox.Text,
				LoweredRoleName = RoleNameTextBox.Text.ToLower(),
				RoleId = new Guid(RoleIdTextBox.Text),
				RoleName = RoleNameTextBox.Text,
			};
			db.Roles.AddObject(role);
			db.SaveChanges();
			switch (PostCreateMode)
			{
				case PostCreateModeEnum.None:
					break;
				case PostCreateModeEnum.Create:
					PrepareToCreate();
					break;
				case PostCreateModeEnum.Update:
					PrepareToUpdate();
					LoadRole(role.RoleId);
					break;
				default:
					break;
			}
			CreateStatusLabel.ForeColor = System.Drawing.Color.Green;
			CreateStatusLabel.Text = "Role '" + role.RoleName + "' was created.";
			CreatedRoleTextBox.Text = role.RoleName;
			RiseCreated(new RoleEditEventArgs(role));
		}

        #endregion

        #region Update

        protected void UpdateRoleButton_Click(object sender, EventArgs e)
        {
            UpdateRole();
        }

        private void PrepareToUpdate()
        {
			PrepareForm();
			ProviderNameTextBox.Text = System.Web.Security.Roles.Provider.Name;
			// Set other input properties.
			ProviderNameTextBox.ReadOnly = true;
			ApplicationIdTextBox.ReadOnly = true;
			RoleIdTextBox.ReadOnly = AllowUpdateRoleGuid;
			RoleNameTextBox.ReadOnly = true;
			CreateRoleButton.Visible = false;
			UpdateRoleButton.Visible = true;

        }

        public void LoadRole(Guid roleId)
        {
			PrepareToUpdate();
			// Load rest
			var role = Role.GetRole(roleId);
			// If role was found then...
			if (role != null)
			{
				RoleNameLabel.Text = "'" + role.RoleName + "'";
				ApplicationIdTextBox.Text = role.ApplicationId.ToString();
				RoleIdTextBox.Text = role.RoleId.ToString();
				RoleNameTextBox.Text = role.RoleName;
				DescriptionTextBox.Text = role.Description;
			}
			RoleNameTextBox.ReadOnly = (RoleNameTextBox.Text.Length > 0);
			ApplicationIdTextBox.ReadOnly = (ApplicationIdTextBox.Text.Length > 0);
			RoleIdTextBox.ReadOnly = (RoleIdTextBox.Text.Length > 0);
			UpdateRoleButton.Visible = true;
			CreateRoleButton.Visible = false;
        }

        public void UpdateRole()
        {
			var db = new SecurityEntities();
			var role = Role.GetRole(CreatedRoleTextBox.Text, db);
			role.ApplicationId = new Guid(ApplicationIdTextBox.Text);
			role.Description = DescriptionTextBox.Text;
			role.LoweredRoleName = RoleNameTextBox.Text.ToLower();
			role.RoleName = RoleNameTextBox.Text;
			db.SaveChanges();
			RiseUpdated(new RoleEditEventArgs(RoleNameTextBox.Text));
        }

        #endregion


	}
}