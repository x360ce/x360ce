using System;
using System.ComponentModel;

namespace JocysCom.WebSites.Engine.Security
{
	[Flags]
	public enum UserFieldName
	{
		[Description("First Name")]
		FirstName = 1,
		[Description("Last Name")]
		LastName = 2,
		[Description("Email")]
		Email = 4,
		[Description("User Name")]
		UserName = 8,
		[Description("Password")]
		Password = 16,
		[Description("Birthday")]
		Birthday = 32,
		[Description("Gender")]
		Gender = 64,
		[Description("Terms of Use")]
		Terms = 128,
		[Description("News")]
		News = 256,
	}
}
