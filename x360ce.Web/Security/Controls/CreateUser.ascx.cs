using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;
using JocysCom.WebSites.Engine;
using System.Web.Security;
using User = JocysCom.WebSites.Engine.Security.Data.User;
using SecurityClassesDataContext = JocysCom.WebSites.Engine.Security.Data.SecurityEntities;

namespace JocysCom.Web.Security.Controls
{
	public sealed class ProfileCreateEventArgs : EventArgs
	{
		public User User { get; set; }
	}

	public partial class CreateUser : UserControl
	{
		private User.ValidationField[] _validationResults;

		public User.ValidationField[] ValidationResults
		{
			get
			{
				return _validationResults =
					_validationResults ??
					User.ValidateMemberRegistration(
						FirstNameTextBox.Text,
						LastNameTextBox.Text,
						EmailTextBox.Text,
						UserName.Text,
						(PasswordTextBoxCustomValidator.Enabled) ? PasswordTextBox.Text : "automatic",
						string.Format("{0}-{1}-{2}",
										YearDropDownList.SelectedValue,
										MonthDropDownList.SelectedValue,
										DayDropDownList.SelectedValue),
						GenderDropDownList.SelectedValue,
						TermsCheckBox.Checked,
						NewsCheckBox.Checked);
			}
		}

		#region Server Validate


		protected void Page_Load(object sender, EventArgs e)
		{
			var en = SecurityContext.Current.AllowUsersToRegister; // || HttpContext.Current.Request.IsLocal;
			HelperFunctions.EnableControl(this, en, en ? null : "Sign Up Disabled");
			HeadPanel.Visible = ShowHead;
		}

		protected void AllCustomValidator_ServerValidate(object source, ServerValidateEventArgs args)
		{
			User.ValidationField[] result = ValidationResults;
			args.IsValid = true;
			foreach (User.ValidationField item in result)
			{
				if (!string.IsNullOrEmpty(item.Message))
				{
					//ErrorPanelLabel.Text = item.Message; 
					args.IsValid = false;
					break;
				}
			}
		}

		#endregion

		public event EventHandler<ProfileCreateEventArgs> CreatedUser;

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
			if (!IsPostBack)
			{
				if (EnableReturnUrl)
				{
					ReturnUrl = Request.ServerVariables["HTTP_REFERER"];
				}
				FillBirthdayDropDownLists();
			}
			TermsLink.HRef = "/terms.htm";
			System.Web.Security.FormsAuthentication.SignOut();
			Session.Abandon();
		}

		/// <summary>
		/// Generate easy to remember password.
		/// </summary>
		/// <returns></returns>
		public string NewPassword()
		{
			var rnd = new Random();
			string chars = "qwxzQWZX";
			;
			string volves = "aeiouyAEIOUY".Replace(chars, "");
			string consonants = "bcdfghjklmnpqrstvwxzBCDFGHJKLMNPQRSTVWXZ".Replace(chars, "");
			string password = string.Empty;

			for (int i = 0; i < 8; i++)
			{
				string choice = (i % 2 == 0) ? consonants : volves;
				password += choice[rnd.Next(choice.Length)].ToString();
			}
			return password;
		}

		private User RegisterMember()
		{
			/* --- Create a new user --------- */
			Page.Validate("MemberRegistration");
			if (!Page.IsValid) return null;
			// Create ASP.NET User
			UserPassword = string.IsNullOrEmpty(UserPassword)
								? NewPassword()
								: UserPassword;
			MembershipCreateStatus status;
			MembershipUser member = SecurityClassesDataContext.Current.CreateUser(UserUsername, UserPassword, UserEmail,
					"Password Question", "Password Answer", true, out status);
			// If creation failed then exit.
			if (status != MembershipCreateStatus.Success) return null;
			return User.GetUser((Guid)member.ProviderUserKey);
		}

		protected void SignUpLinkButton_Click(object sender, EventArgs e)
		{
			/* --- Register member --- */
			User user = RegisterMember();
			if (user == null) return;
			try
			{
				user.DateBirth = new DateTime(
					int.Parse(YearDropDownList.SelectedValue),
					int.Parse(MonthDropDownList.SelectedValue),
					int.Parse(DayDropDownList.SelectedValue));
				user.FirstName = UserFirstName;
				user.LastName = UserLastName;
				user.Gender = UserGender;
				SecurityClassesDataContext.Current.SaveChanges();
				// Add role to the user.
				System.Web.Security.Roles.AddUserToRole(user.UserName, "SocialUsers");
				// LogIn user to website.
				FormsAuthentication.SetAuthCookie(user.UserName, false);
				if (CreatedUser != null)
					CreatedUser(sender, new ProfileCreateEventArgs { User = user });
			}
			catch (Exception)
			{
				//if something goes wrong invalidate registration
				SecurityClassesDataContext.Current.DeleteUser(user.UserName, true);
				System.Web.Security.FormsAuthentication.SignOut();
				Session.Abandon();
				throw;
			}
			// Need to be outside the try catch or the redirect will sire an error
			if (!string.IsNullOrEmpty(RedirectUrl))
			{
				string url = RedirectUrl;
				if (!string.IsNullOrEmpty(ReturnUrl))
				{
					url += string.Format("?ReturnUrl={0}",
										 HttpUtility.UrlEncode(ReturnUrl));
				}

				Response.Redirect(url, true);
			}
		}

		public void FillBirthdayDropDownLists()
		{
			DropDownList yd = YearDropDownList;
			yd.Items.Clear();
			yd.Items.Add("Year:");
			for (int i = 0; i < 120; i++)
			{
				yd.Items.Add(DateTime.Now.AddYears(-i).ToString("yyyy"));
			}
			DropDownList md = MonthDropDownList;
			md.Items.Clear();
			md.Items.Add("Month:");
			for (int i = 1; i <= 12; i++)
			{
				md.Items.Add(i.ToString());
			}
			DropDownList dd = DayDropDownList;
			dd.Items.Clear();
			dd.Items.Add("Day:");
			for (int i = 1; i <= 31; i++)
			{
				dd.Items.Add(i.ToString());
			}
		}

		#region Layout

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowFirstName
		{
			get { return string.IsNullOrEmpty(FirstNameRow.Style["display"]); }
			set { FirstNameRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowLastName
		{
			get { return string.IsNullOrEmpty(LastNameRow.Style["display"]); }
			set { LastNameRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowEmail
		{
			get { return string.IsNullOrEmpty(EmailRow.Style["display"]); }
			set { EmailRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowUsername
		{
			get { return string.IsNullOrEmpty(UsernameRow.Style["display"]); }
			set { UsernameRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowPassword
		{
			get { return string.IsNullOrEmpty(PasswordRow.Style["display"]); }
			set
			{
				PasswordRow.Style["display"] = value ? "" : "none";
				PasswordTextBoxCustomValidator.Enabled = value;
			}
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowBirthday
		{
			get { return string.IsNullOrEmpty(BirthdayRow.Style["display"]); }
			set { BirthdayRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowGender
		{
			get { return string.IsNullOrEmpty(GenderRow.Style["display"]); }
			set { GenderRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowTerms
		{
			get { return string.IsNullOrEmpty(TermsRow.Style["display"]); }
			set { TermsRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowNews
		{
			get { return string.IsNullOrEmpty(NewsRow.Style["display"]); }
			set { NewsRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowSignUp
		{
			get { return string.IsNullOrEmpty(SignUpRow.Style["display"]); }
			set { SignUpRow.Style["display"] = value ? "" : "none"; }
		}

		[Category("Layout"), EditorBrowsable, DefaultValue(true)]
		public bool ShowHead
		{
			get { return (bool?)ViewState["ShowHead"] ?? true; }
			set { ViewState["ShowHead"] = value; }
		}

		#endregion

		#region Behaviour

		[Category("Behavior"), EditorBrowsable, DefaultValue("")]
		public string RedirectUrl
		{
			get { return RedirectUrlTextBox.Text; }
			set { RedirectUrlTextBox.Text = value; }
		}

		[Category("Behavior"), EditorBrowsable, DefaultValue("")]
		public string ReturnUrl
		{
			get { return ReturnUrlTextBox.Text; }
			set { ReturnUrlTextBox.Text = value; }
		}

		[Category("Behavior"), EditorBrowsable, DefaultValue(true)]
		public bool EnableReturnUrl
		{
			get { return (bool?)ViewState["EnableReturnUrl"] ?? true; }
			set { ViewState["EnableReturnUrl"] = value; }
		}

		#endregion

		#region User

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserFirstName
		{
			get { return FirstNameTextBox.Text; }
			set { FirstNameTextBox.Text = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserLastName
		{
			get { return LastNameTextBox.Text; }
			set { LastNameTextBox.Text = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserUsername
		{
			get { return UserName.Text; }
			set { UserName.Text = value; }
		}


		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserEmail
		{
			get { return EmailTextBox.Text; }
			set { EmailTextBox.Text = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserPassword
		{
			get { return PasswordTextBox.Text; }
			set { PasswordTextBox.Text = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public DateTime UserBirthday
		{
			get
			{
				DateTime birthday;
				DateTime.TryParse(
					string.Format("{0}-{1}-{2}",
								  YearDropDownList.SelectedValue,
								  MonthDropDownList.SelectedValue,
								  DayDropDownList.SelectedValue
						), out birthday);
				return birthday;
			}
			set
			{
				if (value < DateTime.Now.AddYears(-120))
					return;

				if (value > DateTime.Now)
					return;

				try
				{
					YearDropDownList.SelectedValue = value.Year.ToString();
				}
				catch
				{
				}
				try
				{
					MonthDropDownList.SelectedValue = value.Month.ToString();
				}
				catch
				{
				}
				try
				{
					DayDropDownList.SelectedValue = value.Day.ToString();
				}
				catch
				{
				}
			}
		}

		[Category("User"), EditorBrowsable, DefaultValue("")]
		public string UserGender
		{
			get { return GenderDropDownList.SelectedValue; }
			set { GenderDropDownList.SelectedValue = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue(false)]
		public bool UserTerms
		{
			get { return TermsCheckBox.Checked; }
			set { TermsCheckBox.Checked = value; }
		}

		[Category("User"), EditorBrowsable, DefaultValue(false)]
		public bool UserNews
		{
			get { return NewsCheckBox.Checked; }
			set { NewsCheckBox.Checked = value; }
		}

		#endregion
	}
}