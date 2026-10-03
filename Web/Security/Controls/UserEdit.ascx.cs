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
// Required for cryptography.
using System.Security.Cryptography;
using System.Data.Common;
using System.Configuration.Provider;
using System.Web.Configuration;
using System.ComponentModel;

namespace JocysCom.Web.Security.Controls
{
	public partial class UserEdit : System.Web.UI.UserControl
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				PrepareToCreate();
			}
		}

		#region Events

		// Define a public event members.
		public event EventHandler<UserEditEventArgs> Updated;
		public event EventHandler<UserEditEventArgs> Created;

		/// <summary>
		/// Rise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseUpdated(UserEditEventArgs e)
		{
			// If event was attached then fire it.
			if (Updated != null) Updated(this, e);
		}

		/// <summary>
		/// Raise the event within the method.  
		/// </summary>
		/// <param name="e"></param>
		protected virtual void RiseCreated(UserEditEventArgs e)
		{
			// If event was attached then fire it.
			if (Created != null) Created(this, e);
		}

		#endregion

		#region Properties

		[Browsable(true), Category("Layout")]
		public bool ShowUserId
		{
			get { return UserIdRow.Visible; }
			set { UserIdRow.Visible = value; }
		}

		[Browsable(true), Category("Layout")]
		public bool ShowPasswordQuestion {
			get { return PasswordQuestionRow.Visible; }
			set { PasswordQuestionRow.Visible = value; }
		}

		[Browsable(true), Category("Layout")]
		public bool ShowPasswordAnswer
		{
			get { return PasswordAnswerRow.Visible; }
			set { PasswordAnswerRow.Visible = value; }
		}

		[Browsable(true), Category("Layout")]
		public bool ShowComment
		{
			get { return CommentRow.Visible; }
			set { CommentRow.Visible = value; }
		}


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

		public string UserName
		{
			get { return UserNameTextBox.Text; }
			set { UserNameTextBox.Text = value; }
		}

		public bool AllowUpdateUserGuid
		{
			get { return m_allowUpdateUserGuid; }
			set { m_allowUpdateUserGuid = value; }
		}
		private bool m_allowUpdateUserGuid;
	

		#endregion


		public void PrepareForm()
		{
			CreateStatusLabel.Text = string.Empty;
		}

		#region Create

		protected void CreateUserButton_Click(object sender, EventArgs e)
		{
			CreateUser();
		}

		public void PrepareToCreate()
		{
			PrepareForm();
			// Reset form to default values.
			ProviderNameTextBox.ReadOnly = true;
			UserIdTextBox.Text = Guid.NewGuid().ToString();
			IsOnlineCheckBox.Enabled = false;
			CreateDateRow.Visible = false;
			LastLoginDateDateRow.Visible = false;
			LastActivityDateRow.Visible = false;
			LastPasswordChangeDateRow.Visible = false;
			LastLockoutDateDateRow.Visible = false;
			UpdateUserButton.Visible = false;
			// Clean other values.
			ProviderNameTextBox.Text = "";
			UserIdTextBox.Text = Guid.NewGuid().ToString();
			UserNameTextBox.Text = "";
			PasswordTextBox.Text = "";
			PasswordConfirmTextBox.Text = "";
			EmailTextBox.Text = "";
			PasswordQuestionTextBox.Text = "";
			PasswordAnswerTextBox.Text = "";
			CommentTextBox.Text = "";
			IsLockedOutCheckBox.Enabled = false;
			IsApprovedCheckBox.Checked = true;
		}

		public void CreateUser()
		{
			MembershipUser user = null;
			CreatedUserTextBox.Text = "";
			string userName = UserNameTextBox.Text;
			Guid userId = new Guid(UserIdTextBox.Text);
			try
			{
				MembershipCreateStatus mcStatus;
				user = Membership.CreateUser(userName, PasswordTextBox.Text, EmailTextBox.Text, null, null, IsApprovedCheckBox.Checked, userId, out mcStatus);
			}
			catch (Exception ex)
			{
				CreateStatusLabel.ForeColor = System.Drawing.Color.Red;
				CreateStatusLabel.Text = ex.Message;
			}
			if (user != null)
			{
				user.Comment = CommentTextBox.Text;
				user.IsApproved = IsApprovedCheckBox.Checked;
				if ((PasswordQuestionTextBox.Text.Length > 0) && (PasswordAnswerTextBox.Text.Length > 0))
				{
					user.ChangePasswordQuestionAndAnswer(PasswordTextBox.Text, PasswordQuestionTextBox.Text, PasswordAnswerTextBox.Text);
				}
				CreateStatusLabel.ForeColor = System.Drawing.Color.Green;
				CreateStatusLabel.Text = "User '" + user.UserName + "' was created.";
				CreatedUserTextBox.Text = userName;
				switch (PostCreateMode)
				{
					case PostCreateModeEnum.None:
						break;
					case PostCreateModeEnum.Create:
						PrepareToCreate();
						break;
					case PostCreateModeEnum.Update:
						PrepareToUpdate();
						LoadUser(userName);
						break;
					default:
						break;
				}
				RiseCreated(new UserEditEventArgs(user));
			}
		}

		#endregion

		#region Update

		protected void UpdateUserButton_Click(object sender, EventArgs e)
		{
			UpdateUser();
		}

		private void PrepareToUpdate()
		{
			PrepareForm();
			UserIdTextBox.ReadOnly = !AllowUpdateUserGuid;
		}

		public void LoadUser(Guid userId)
		{
			LoadUser(Membership.GetUser((object)userId));
		}

		public void LoadUser(string username)
		{
			LoadUser(Membership.GetUser(username));
		}

		public void LoadUser(MembershipUser user)
		{
			PrepareToUpdate();
			UserNameLabel.Text = "'" + user.UserName + "'";
			string dateFormat = "yyyy-MM-dd HH:mm:sszzz (ddd)";
			ProviderNameTextBox.Text = user.ProviderName;
			UserIdTextBox.Text = user.ProviderUserKey.ToString();
			UserNameTextBox.Text = user.UserName;
			// Load password
			string password = user.GetPassword();
			// We need this double text/value set in order [Show Password] checkbox to work properly.
			PasswordTextBox.Text = password;
			PasswordTextBox.Attributes.Add("value", password);
			// We need this double text/value set in order [Show Password] checkbox to work properly.
			PasswordConfirmTextBox.Text = password;
			PasswordConfirmTextBox.Attributes.Add("value", password);
			EmailTextBox.Text = user.Email;
			PasswordQuestionTextBox.Text = user.PasswordQuestion;
			CommentTextBox.Text = user.Comment;
			CreateDateLabel.Text = user.CreationDate.ToString(dateFormat);
			LastLoginDateLabel.Text = user.LastLoginDate.ToString(dateFormat);
			LastActivityDateLabel.Text = user.LastActivityDate.ToString(dateFormat);
			LastPasswordChangeLablel.Text = user.LastPasswordChangedDate.ToString(dateFormat);
			LastLockoutDateLabel.Text = user.LastLockoutDate.ToString(dateFormat);
			IsLockedOutCheckBox.Checked = user.IsLockedOut;
			IsLockedOutCheckBox.Enabled = user.IsLockedOut;
			IsApprovedCheckBox.Checked = user.IsApproved;
			IsOnlineCheckBox.Checked = user.IsOnline;
			//FirstNameTextBox.Text = user.Comment
			CreateDateRow.Visible = (user.CreationDate.Year > 1754);
			LastLoginDateDateRow.Visible = (user.LastLoginDate.Year > 1754);
			LastActivityDateRow.Visible = (user.LastActivityDate.Year > 1754);
			LastPasswordChangeDateRow.Visible = (user.LastPasswordChangedDate.Year > 1754);
			LastLockoutDateDateRow.Visible = (user.LastLockoutDate.Year > 1754);
			UserNameTextBox.ReadOnly = (UserNameTextBox.Text.Length > 0);
			UpdateUserButton.Visible = true;
			CreateUserButton.Visible = false;
		}

		public void UpdateUser()
		{
			string userName = UserNameTextBox.Text;
			MembershipUser user = Membership.GetUser(userName);
			if (PasswordTextBox.Text != user.GetPassword())
			{
				user.ChangePassword(user.GetPassword(), PasswordTextBox.Text);
				Membership.UpdateUser(user);
			}
			if (PasswordQuestionTextBox.Text.Length > 0 && PasswordAnswerTextBox.Text.Length > 0)
			{
				user.ChangePasswordQuestionAndAnswer(user.GetPassword(), PasswordQuestionTextBox.Text, PasswordAnswerTextBox.Text);
				Membership.UpdateUser(user);
			}
			user.Comment = CommentTextBox.Text;
			user.Email = EmailTextBox.Text;
			user.IsApproved = IsApprovedCheckBox.Checked;
			Membership.UpdateUser(user);
			//user.LastActivityDate
			//user.LastLoginDate
			if (IsLockedOutCheckBox.Checked != user.IsLockedOut)
			{
				user.UnlockUser();
				Membership.UpdateUser(user);
			}
			LoadUser(userName);
			RiseUpdated(new UserEditEventArgs(userName));
		}

		#endregion

		protected void ShowPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			string password = PasswordTextBox.Text;
			string passwordConfirm = PasswordConfirmTextBox.Text;
			if (ShowPasswordCheckBox.Checked)
			{
				PasswordTextBox.TextMode = TextBoxMode.SingleLine;
				PasswordConfirmTextBox.TextMode = TextBoxMode.SingleLine;
			}
			else
			{
				PasswordTextBox.TextMode = TextBoxMode.Password;
				PasswordConfirmTextBox.TextMode = TextBoxMode.Password;
			}
			PasswordTextBox.Attributes.Add("value", password);
			PasswordConfirmTextBox.Attributes.Add("value", passwordConfirm);
		}

	}
}