using System;

namespace JocysCom.ClassLibrary.Security
{
	public static partial class TokenHelper
	{
		/// <summary>
		/// Get URL to page. If runs on website then host will be replaced with current request.
		/// Use AbsoluteUri property to get full URL string.
		/// </summary>
		/// <param name="token">Token.</param>
		/// <param name="page">Page name. Like "/Login.aspx" or "/LoginReset.aspx"</param>
		/// <returns>URL string.</returns>
		public static Uri GetUrl(string keyName, string token, Uri url = null)
		{
			var context = System.Web.HttpContext.Current;
			var u = (url == null || context != null)
				? System.Web.HttpContext.Current.Request.Url
				: url;
			var absolutePath = (url != null)
				? url.AbsolutePath
				: u.AbsolutePath;
			var port = u.IsDefaultPort ? "" : ":" + u.Port;
			var absoluteUri = string.Format("{0}://{1}{2}{3}?{4}={5}", u.Scheme, u.Host, port, absolutePath, keyName, token);
			return new Uri(absoluteUri);
		}

		/// <summary>
		/// Get full URL with specified path. Use AbsoluteUri property to get full URL string.
		/// </summary>
		public static Uri GetFullUrl(string absolutePath)
		{
			var context = System.Web.HttpContext.Current;
			if (context == null)
				return null;
			var u = context.Request.Url;
			var port = u.IsDefaultPort ? "" : ":" + u.Port;
			var absoluteUri = string.Format("{0}://{1}{2}{3}", u.Scheme, u.Host, port, absolutePath);
			return new Uri(absoluteUri);
		}

		/// <summary>
		/// Get URL to web application root. Use AbsoluteUri property to get full URL string.
		/// </summary>
		public static Uri GetApplicationUrl()
		{
			var context = System.Web.HttpContext.Current;
			if (context == null)
				return null;
			return GetFullUrl(context.Request.ApplicationPath);
		}
	}
}
