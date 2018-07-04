using JocysCom.ClassLibrary.Runtime;
using JocysCom.Web.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace x360ce.Web
{
	public class Global : System.Web.HttpApplication
	{

		// Order:
		// Application_Start
		// Application_BeginRequest
		// Application_AuthenticateRequest
		// Session_Start
		// Application_EndRequest
		// Session_End
		// Application_End
		// Application_Error

		protected void Application_Start(object sender, EventArgs e)
		{

			JocysCom.WebSites.Engine.Security.Check.CreateDefault();
		}

		protected void Session_Start(object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			// Increase website security.
			//The X - Frame - Options HTTP response header can be used To indicate whether Or Not 
			// a browser should be allowed To render a page In a <frame>, <iframe> or <object>.
			// Sites can use this To avoid clickjacking attacks, by ensuring that their content
			// Is Not embedded into other sites.
			// DENY - The Page cannot be displayed In a frame, regardless of the site attempting to do so.
			// SAMEORIGIN - The Page can only be displayed in a frame on the same origin as the page itself.
			// ALLOW-From uri - The Page can only be displayed in a frame on the specified origin. 
			HttpContext.Current.Response.AddHeader("x-frame-options", "SAMEORIGIN");
			// Deny physical path.
			if ((Request.Path.IndexOf((char)92) > -1 | System.IO.Path.GetFullPath(Request.PhysicalPath) != Request.PhysicalPath))
			{
				throw new HttpException((int)System.Net.HttpStatusCode.NotFound, "Not Found");
			}
		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{

		}

		protected void Application_Error(object sender, EventArgs e)
		{
			// Fires when an unhanded error occurs anywhere in the application
			if (LogHelper.Current.Smtp.ErrorNotifications)
			{
				var ex = Server.GetLastError().GetBaseException();
				//Context.ClearError();
				// Send exception by email
				LogHelper.Current.ProcessException(ex);
				//var query = HttpUtility.ParseQueryString("");
				//if (ex is FormatException)
				//{
				//	query["logoff"] = "1";
				//}
				//else if (ex is HttpException)
				//{
				//	query["message"] = ex.Message;
				//}
				//WebHelper.Redirect("~/CustomError.aspx", query);
			}
		}

		protected void Session_End(object sender, EventArgs e)
		{

		}

		protected void Application_End(object sender, EventArgs e)
		{

		}
	}
}
