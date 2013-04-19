using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace JocysCom.WebSites.Engine.Security.Data
{
	public partial class User
	{

		public LinkItem ToLinkItem()
		{
			return new LinkItem()
			{
				Id = UserId,
				Name = FullName,
				Type = ItemType.User,
			};
		}
		
		[XmlIgnore]
		public string FullName
		{
			get
			{
				return string.Format("{0} {1}", this.FirstName, this.LastName);
			}
		}

		public static User GetUser(Guid userId)
		{
			var db = SecurityEntities.Current;
			return db.Users.Single(x => x.UserId == userId);
		}

		public static User GetUser(string username)
		{
			var db = SecurityEntities.Current;
			return db.Users.Single(x => x.UserName == username);
		}

		/// <summary>
		/// All shared queries must go here.
		/// </summary>
		/// <param name="queryName"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IQueryable<User> GetQuery(string queryName, OrderedDictionary parameters)
		{
			// Get parameters.
			string[] qa = queryName.Split('/');
			string p0 = qa[0];
			string p1 = (qa.Length > 1) ? qa[1] : string.Empty;
			string p2 = (qa.Length > 2) ? qa[2] : string.Empty;
			// Set predefined query.
			IQueryable<User> query = null;
			Guid userId = parameters.Contains("UserId") ? (Guid)parameters["UserId"] : Guid.Empty;
			var db = SecurityEntities.Current;
			UserQueryName qne = GuidEnum.TryParse<UserQueryName>(p0, UserQueryName.None, true);
			switch (qne)
			{
				case UserQueryName.All:
					query = from row in db.Users select row;
					break;
				default:
					throw new NotImplementedException(string.Format("{0} QueryName not supported", queryName));
				//break;
			}
			// Add search condition.
			if (parameters != null)
			{
				// Apply search filter.
				string searchValue;
				searchValue = parameters.Contains("SearchName") ? (string)parameters["SearchName"] : string.Empty;
				if (!string.IsNullOrEmpty(searchValue))
				{
					searchValue = searchValue.Trim();
					Guid uid;
					if (Guid.TryParse(searchValue, out uid))
					{
						query = query.Where(x => x.UserId == uid);
					}
					else
					{
						// we cant use FullText index inside linq so just extend command timout in order for 
						// search not to fail.
						if (db.CommandTimeout < 120) db.CommandTimeout = 120;
						query = query.Where(x =>
							x.UserName == searchValue);
					}
				}
			}
			return query;
		}

		public static User GetUserByEmail(string email)
		{
			var db = SecurityEntities.Current;
			return db.Users.FirstOrDefault(x => x.Membership.Email == email);
		}

		[Serializable]
		public struct ValidationField
		{
			public string Name;
			public object Value;
			public string Message;
		}

		public static ValidationField[] ValidateUser(
		string username,
		string password)
		{
			string u = string.Empty; // username
			string p = string.Empty; // password
			string v = string.Empty; //Validation

			// Required fields.
			if (string.IsNullOrEmpty(username)) u = "<b>Username</b> is required.";
			if (string.IsNullOrEmpty(password)) p = "<b>Password</b> is required.";
			// Username.
			if (string.IsNullOrEmpty(u) && Regex.Match(username, "[a-zA-Z0-9_]{3,20}").Value != username)
			{
				u = "<b>Username</b> must be between 3-20 characters<br />and contain only letters or numbers.";
			}

			//if(!System.Web.Security.Membership.ValidateUser(username, password))
			//    v = "user name or password are not valid";

			List<ValidationField> results = new List<ValidationField>();
			results.Add(new ValidationField() { Name = "UserName", Value = username, Message = u });
			results.Add(new ValidationField() { Name = "Password", Value = password, Message = p });
			//results.Add(new ValidationField() { Name = "Validation", Value = string.Empty, Message = v });
			return results.ToArray();
		}

		/// <summary>
		/// Test user registration info.
		/// </summary>
		/// <param name="value">Validation parameters.</param>
		/// <returns>Results array for each parameter.</returns>
		public static ValidationField[] ValidateMemberRegistration(
			string firstName,
			string lastName,
			string email,
			string username,
			string password,
			string birthday,
			string gender,
			bool? terms,
			bool? news
		)
		{
			string f = string.Empty; // First Name
			string l = string.Empty; // Last Name
			string e = string.Empty; // Email
			string u = string.Empty; // Username
			string p = string.Empty; // Password
			string b = string.Empty; // Birthday
			string g = string.Empty; // Gender
			string t = string.Empty; // Terms
			string n = string.Empty; // Promotions
			// Required fields.
			if (string.IsNullOrEmpty(firstName)) f = "<b>First Name</b> is required.";
			if (string.IsNullOrEmpty(lastName)) l = "<b>Last Name</b> is required.";
			if (string.IsNullOrEmpty(email)) e = "<b>Your Email</b> is required.";
			if (string.IsNullOrEmpty(username)) u = "<b>Username</b> is required.";
			if (string.IsNullOrEmpty(password)) p = "<b>Password</b> is required.";
			// First Name
			if (string.IsNullOrEmpty(f) && firstName.Length > 0)
			{
				//if (Engine.DataBase.Data.Resource.IsMatch(firstName, AppContext.Current.Resources.DisallowedNames))
				//{
				//    f = "This first name is not allowed.";
				//}
			}
			// Last Name
			if (string.IsNullOrEmpty(l) && lastName.Length > 0)
			{
				//if (Engine.DataBase.Data.Resource.IsMatch(lastName, AppContext.Current.Resources.DisallowedNames))
				//{
				//    l = "This last name is not allowed.";
				//}
			}
			// Username.
			if (string.IsNullOrEmpty(u) && Regex.Match(username, "[a-zA-Z0-9_]{3,20}").Value != username)
			{
				u = "<b>Username</b> must be between 3-20 characters<br />and contain only letters or numbers.";
			}
			if (string.IsNullOrEmpty(u) && User.GetUser(username) != null)
			{
				u = "<b>Username</b> is already taken.";
			}
			//if (string.IsNullOrEmpty(u) && Engine.Forums.Transforms.IsMatch(
			//        username, Engine.Forums.Transforms.DisallowedNames))
			//{
			//    u = "<b>Username</b> is not available.";
			//}
			// Email
			if (string.IsNullOrEmpty(e) && !Engine.Mail.EmailRegex.Match(email).Success)
			{
				e = "<b>Your Email</b> address is not valid.";
			}

			if (string.IsNullOrEmpty(e) && User.GetUserByEmail(email) != null)
			{
				e = "<b>Your Email</b> address is used already.";
			}
			// Password
			if (string.IsNullOrEmpty(p) && password.Length < 6)
			{
				p = "<b>Password</b> must be at least 6 characters.";
			}
			// Pasword: Is same as...
			//if (string.IsNullOrEmpty(p) && password == firstName)
			//{
			//    p = "<b>Password</b> can't be same as <b>First Name</b>.";
			//}
			//if (string.IsNullOrEmpty(p) && password == lastName)
			//{
			//    p = "<b>Password</b> can't be same as <b>Last Name</b>.";
			//}
			//if (string.IsNullOrEmpty(p) && password == username)
			//{
			//    p = "<b>Password</b> can't be same as <b>Username</b>.";
			//}
			if (string.IsNullOrEmpty(p) && password == email)
			{
				p = "<b>Password</b> can't be same as <b>Your Email</b>.";
			}
			// Password: Contains...
			//if (string.IsNullOrEmpty(p) && password.Contains(firstName))
			//{
			//    p = "<b>Password</b> can't contain <b>First Name</b> inside.";
			//}
			//if (string.IsNullOrEmpty(p) && password.Contains(lastName))
			//{
			//    p = "<b>Password</b> can't contain <b>Last Name</b> inside.";
			//}
			//if (string.IsNullOrEmpty(p) && password.Contains(username))
			//{
			//    p = "<b>Password</b> can't contain <b>Username</b> inside.";
			//}
			// Birthday
			DateTime bd;
			if (string.IsNullOrEmpty(birthday) || !DateTime.TryParse(birthday, out bd))
			{
				b = "Indicate your <b>Birthday</b> to register.";
			}
			// Gender
			if (gender != "M" && gender != "F")
			{
				g = "Indicate your <b>Gender</b> to register.";
			}
			// Terms
			if (terms.HasValue && terms.Value == false)
			{
				t = "You must accept <b>Terms of use</b>.";
			}
			List<ValidationField> results = new List<ValidationField>();
			results.Add(new ValidationField() { Name = "FirstName", Value = firstName, Message = f });
			results.Add(new ValidationField() { Name = "LastName", Value = lastName, Message = l });
			results.Add(new ValidationField() { Name = "Email", Value = email, Message = e });
			results.Add(new ValidationField() { Name = "Username", Value = username, Message = u });
			results.Add(new ValidationField() { Name = "Password", Value = password, Message = p });
			results.Add(new ValidationField() { Name = "Birthday", Value = birthday, Message = b });
			results.Add(new ValidationField() { Name = "Gender", Value = gender, Message = g });
			results.Add(new ValidationField() { Name = "Terms", Value = terms, Message = t });
			results.Add(new ValidationField() { Name = "News", Value = news, Message = n });
			return results.ToArray();
		}


	}
}
