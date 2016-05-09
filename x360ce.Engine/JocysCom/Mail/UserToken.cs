using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

namespace JocysCom.ClassLibrary.Mail
{
	/// <summary>
	/// User state - unique identifier for the asynchronous task.
	/// </summary>
	public class UserToken
	{
		public UserToken()
		{
		}

		public Guid UserId;
		public MailMessage Message;
		public System.IO.DirectoryInfo TempDirectory;
	}

}
