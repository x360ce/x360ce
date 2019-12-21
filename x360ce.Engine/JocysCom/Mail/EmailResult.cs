using System.ComponentModel;

namespace JocysCom.ClassLibrary.Mail
{
	public enum EmailResult
	{
		[Description("OK")]
		OK = 0,
		[Description("The email address is compulsory")]
		Empty = 1,
		[Description("The email address cannot end with a semicolon")]
		Semicolon = 2,
		[Description("The email address format is invalid")]
		Invalid = 3,
	}
}
