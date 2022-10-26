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
using JocysCom.WebSites.Engine.Security;
using System.Web.UI.HtmlControls;
using System.Security.Cryptography;

namespace JocysCom.Web.Security.Controls
{
	public sealed class ProfileCreateEventArgs : EventArgs
	{
		public User User { get; set; }
	}

	public partial class CreateUser : UserControl
	{

		#region Server Validate


		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				var fields = SecurityContext.Current.RequiredFields | SecurityContext.Current.OptionalFields;
				FirstNameRow.Style["display"] = fields.HasFlag(UserFieldName.FirstName) ? "" : "none";
				LastNameRow.Style["display"] = fields.HasFlag(UserFieldName.LastName) ? "" : "none";
				EmailRow.Style["display"] = fields.HasFlag(UserFieldName.Email) ? "" : "none";
				UserNameRow.Style["display"] = fields.HasFlag(UserFieldName.UserName) ? "" : "none";
				PasswordRow.Style["display"] = fields.HasFlag(UserFieldName.Password) ? "" : "none";
				BirthdayRow.Style["display"] = fields.HasFlag(UserFieldName.Birthday) ? "" : "none";
				GenderRow.Style["display"] = fields.HasFlag(UserFieldName.Gender) ? "" : "none";
				TermsRow.Style["display"] = fields.HasFlag(UserFieldName.Terms) ? "" : "none";
				NewsRow.Style["display"] = fields.HasFlag(UserFieldName.News) ? "" : "none";
				var en = SecurityContext.Current.AllowUsersToSignUp;
				HelperFunctions.EnableControl(this, en, en ? null : "Sign Up Disabled");
				HeadPanel.Visible = ShowHead;
				var values = (UserFieldName[])Enum.GetValues(typeof(UserFieldName));
				foreach (var item in values)
				{
					if (!SecurityContext.Current.RequiredFields.HasFlag(item))
					{
						continue;
					}
					var id = string.Format("{0}Status", item);
					var div = HelperFunctions.FindControl<HtmlGenericControl>(this, id);
					if (div != null)
					{
						div.Attributes["class"] = "SWUI_Table_Result0Changed";
					}
				}
			}
		}

		protected void AllCustomValidator_ServerValidate(object source, ServerValidateEventArgs args)
		{
			var result =
				User.ValidateMemberRegistration(
					FirstNameTextBox.Text,
					LastNameTextBox.Text,
					EmailTextBox.Text,
					UserName.Text,
					PasswordTextBox.Text,
					string.Format("{0}-{1}-{2}",
					YearDropDownList.SelectedValue,
					MonthDropDownList.SelectedValue,
					DayDropDownList.SelectedValue),
					GenderDropDownList.SelectedValue,
					TermsCheckBox.Checked,
					NewsCheckBox.Checked,
					SecurityContext.Current.RequiredFields
				);

			var errors = result.Where(x => !string.IsNullOrEmpty(x.Message)).Select(x => x.Message);
			var valid = errors.Count() == 0;
			args.IsValid = valid;
			HelperFunctions.FindControl<Label>(this, "ErrorLabel").Text = valid
				? "" : string.Join("<br />\r\n", errors);
			HelperFunctions.FindControl<Panel>(this, "ErrorPanel").Style["display"] = valid ? "none" : "block";
			var values = (UserFieldName[])Enum.GetValues(typeof(UserFieldName));
			foreach (var item in values)
			{
				var id = string.Format("{0}Status", item);
				var div = HelperFunctions.FindControl<HtmlGenericControl>(this, id);
				if (div != null)
				{
					if (result.Any(x => x.Name == item))
					{
						div.Attributes["class"] = "SWUI_Table_Result0Changed";
					}
					else
					{
						if (SecurityContext.Current.RequiredFields.HasFlag(item))
						{
							div.Attributes["class"] = "SWUI_Table_Result1";
						}
						else
						{
							div.Attributes["class"] = "";
						}
					}
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
		/// Generates a pseudorandom password that cannot be predicted.
		/// </summary>
		/// <param name="length">
		/// 	The length of the password to be generated,
		/// 	if left to zero - a random password-length will be generated.
		/// </param>
		/// <param name="charlist">A list of characters from which the password can consist of.</param>
		/// <returns>A string representing a securely-generated password.</returns>
		public string NewPassword(uint length = 0, string charlist = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!\"£$%^&*()_+=-{}[]:@~;'#/,.<>?\\")
		{
			var rnd = new Random();
			string password = string.Empty;
			for (int i = 0; i < 12; i++)
				password += charlist[rnd.Next(charlist.Length)];
			return password;
		}

		private User RegisterMember()
		{
			/* --- Create a new user --------- */
			//Page.Validate("MemberRegistration");
			if (!Page.IsValid) return null;
			// Create ASP.NET User
			var password = PasswordTextBox.Text;
			PasswordTextBox.Text = string.IsNullOrEmpty(password)
								? NewPassword()
								: password;
			MembershipCreateStatus status;
			MembershipUser member = SecurityClassesDataContext.Current.CreateUser(UserName.Text, password, EmailTextBox.Text,
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
				var fields = SecurityContext.Current.RequiredFields | SecurityContext.Current.OptionalFields;
				if (fields.HasFlag(UserFieldName.Birthday))
				{
					user.DateBirth = new DateTime(
						int.Parse(YearDropDownList.SelectedValue),
						int.Parse(MonthDropDownList.SelectedValue),
						int.Parse(DayDropDownList.SelectedValue));
				}

				user.FirstName = FirstNameTextBox.Text;
				user.LastName = LastNameTextBox.Text;
				user.Gender = (string)GenderDropDownList.SelectedValue;
				SecurityClassesDataContext.Current.SaveChanges();
				var role = SecurityContext.Current.DefaultRole;
				if (!string.IsNullOrEmpty(role))
				{
					if (!Roles.RoleExists(role))
					{
						Roles.CreateRole(role);
						JocysCom.WebSites.Engine.Security.Check.UpdateRole(role, "Default Role.");
					}
					// Add role to the user.
					System.Web.Security.Roles.AddUserToRole(user.UserName, role);
				}
				// LogIn user to website.
				FormsAuthentication.SetAuthCookie(user.UserName, false);
				if (CreatedUser != null)
				{
					CreatedUser(sender, new ProfileCreateEventArgs { User = user });
				}
			}
			catch (Exception ex)
			{
				//if something goes wrong invalidate registration
				SecurityClassesDataContext.Current.DeleteUser(user.UserName, true);
				System.Web.Security.FormsAuthentication.SignOut();
				Session.Abandon();
				HelperFunctions.FindControl<Label>(this, "ErrorLabel").Text = ex.Message;
				HelperFunctions.FindControl<Panel>(this, "ErrorPanel").Style["display"] = "block";
				return;
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
				if (value < DateTime.Now.AddYears(-120)) return;
				if (value > DateTime.Now) return;
				try
				{
					YearDropDownList.SelectedValue = value.Year.ToString();
					MonthDropDownList.SelectedValue = value.Month.ToString();
					DayDropDownList.SelectedValue = value.Day.ToString();
				}
				catch { }
			}
		}

		#endregion
	}
}
