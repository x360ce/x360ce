using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using JocysCom.ClassLibrary;

namespace JocysCom.WebSites.Engine.Security.Data
{
	public partial class User
	{

		private static Regex _emailRegex;
		public static Regex EmailRegex
		{
			set { _emailRegex = value; }
			get { return _emailRegex = _emailRegex ?? new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.IgnoreCase); }
		}

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
			results.Add(new ValidationField() { Name = UserFieldName.UserName, Value = username, Message = u });
			results.Add(new ValidationField() { Name = UserFieldName.Password, Value = password, Message = p });
			//results.Add(new ValidationField() { Name = "Validation", Value = string.Empty, Message = v });
			return results.ToArray();
		}

		public static List<string> DisallowedNames = new List<string>();

		/// <summary>
		/// Test user registration info.
		/// </summary>
		/// <param name="value">Validation parameters.</param>
		/// <returns>Results array for each parameter.</returns>
		public static ValidationField[] ValidateMemberRegistration(
			string firstName,
			string lastName,
			string email,
			string userName,
			string password,
			string birthday,
			string gender,
			bool? terms,
			bool? news,
			UserFieldName requiredFields = 0
		)
		{
			var results = new List<ValidationField>();
			var values = new List<ValidationField>()
			{
				new ValidationField(UserFieldName.FirstName, firstName ?? ""),
				new ValidationField(UserFieldName.LastName,lastName ?? ""),
				new ValidationField(UserFieldName.Email, email ?? ""),
				new ValidationField(UserFieldName.UserName, userName ?? ""),
				new ValidationField(UserFieldName.Password, password ?? ""),
				new ValidationField(UserFieldName.Birthday, birthday ?? ""),
				new ValidationField(UserFieldName.Gender, gender ?? ""),
				new ValidationField(UserFieldName.Terms, terms),
				new ValidationField(UserFieldName.News, news),
			};
			var allValues = (UserFieldName[])Enum.GetValues(typeof(UserFieldName));
			if (requiredFields == 0)
			{
				requiredFields = (UserFieldName)allValues.Select(x => (int)x).Sum();
			}
			foreach (var item in allValues)
			{
				var description = ClassLibrary.ClassTools.EnumTools.GetDescription(item);
				if (requiredFields.HasFlag(item))
				{
					var field = values.FirstOrDefault(x => x.Name == item);
					bool isEmpy = field.Value is string
						? string.IsNullOrEmpty((string)field.Value)
						: field.Value == null;
					if (isEmpy)
					{
						results.Add(new ValidationField(item, field.Value, "<b>" + description + "</b> is required."));
					}
					else if (field.Value is string)
					{
						var value = (string)field.Value;
						if (DisallowedNames.Contains(value))
						{
							results.Add(new ValidationField(item, value, "This <b>" + description + "</b> is not allowed."));
						}
						if ((item == UserFieldName.FirstName || item == UserFieldName.LastName) && Regex.Match(value, "[a-zA-Z]{1,20}").Value != value)
						{
							results.Add(new ValidationField(item, value, "<b>" + description + "</b> must be between 1-20 characters<br/> and contain only letters."));
						}
						if (item == UserFieldName.UserName)
						{
							if (Regex.Match(value, "[a-zA-Z0-9_]{3,20}").Value != value)
							{
								results.Add(new ValidationField(item, value, "<b>" + description + "</b> must be between 3-20 characters<br />and contain only letters or numbers."));
							}
							else if (User.GetUser(value) != null)
							{
								results.Add(new ValidationField(item, value, "" + description + " is already taken."));
							}
						}
						if (item == UserFieldName.Email)
						{
							if (!EmailRegex.Match(email).Success)
							{
								results.Add(new ValidationField(item, value, "" + description + " is not valid."));
							}
							else if (User.GetUserByEmail(email) != null)
							{
								results.Add(new ValidationField(item, value, "" + description + " is already taken."));
							}
						}
						if (item == UserFieldName.Password)
						{
							if (value.Length < 6)
							{
								results.Add(new ValidationField(item, value, "" + description + " must be at least 6 characters."));
							}
							if (password == firstName)
							{
								results.Add(new ValidationField(item, value, "" + description + " can't be same as First Name."));
							}
							if (password == lastName)
							{
								results.Add(new ValidationField(item, value, "" + description + " can't be same as Last Name."));
							}
							if (password == userName)
							{
								results.Add(new ValidationField(item, value, "" + description + " can't be same as User Name."));
							}
							if (password == email)
							{
								results.Add(new ValidationField(item, value, "" + description + " can't be same as Email."));
							}
							if (!string.IsNullOrEmpty(firstName) && value.Contains(firstName))
							{
								results.Add(new ValidationField(item, value, "" + description + " can't contain First Name."));
							}
							if (!string.IsNullOrEmpty(lastName) && value.Contains(lastName))
							{
								results.Add(new ValidationField(item, value, "" + description + " can't contain Last Name."));
							}
							if (!string.IsNullOrEmpty(userName) && value.Contains(userName))
							{
								results.Add(new ValidationField(item, value, "" + description + " can't contain User Name."));
							}
						}
						if (item == UserFieldName.Birthday)
						{
							DateTime bd;
							if (!DateTime.TryParse(birthday, out bd))
							{
								results.Add(new ValidationField(item, value, description + " is not valid."));
							}

						}
						if (item == UserFieldName.Gender)
						{
							if (gender != "M" && gender != "F")
							{
								results.Add(new ValidationField(item, value, description + " is not valid."));
							}
						}
					}
					else
					{
						var bValue = (bool?)field.Value;
						if (item == UserFieldName.Terms)
						{
							if (!bValue.Value)
							{
								results.Add(new ValidationField(item, bValue, "You must accept <b>" + description + "</b>."));
							}
						}
					}
				}
			}
			return results.ToArray();
		}


		public static void SendPasswordResetKey(User user)
		{

		}

		public static void SendPasswordResetKey(string username, string password, TimeUnitType unit)
		{
			//Uri u = System.Web.HttpContext.Current.Request.Url;
			//string resetUrl = GetUrl(username, password, unit);
			//string template = Helper.GetTranslation(TranslationKey.PasswordResetTemplate);
			//string body = template.Replace("{Username}", user.FullName).Replace("{Host}", u.Host).Replace("{ResetKey}", resetUrl);
			//string subject = string.Format("Reset your {0} password", u.Host);
			//Engine.Mail.Current.Send(user.Membership.Email, user.FullName, subject, body, JocysCom.ClassLibrary.Mail.MailTextType.Plain);
		}

	}
}
