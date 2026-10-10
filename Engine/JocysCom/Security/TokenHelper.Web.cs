#nullable disable

#if NETFRAMEWORK // .NET Framework
using System.Web;
#else
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
#endif
using System;

namespace JocysCom.ClassLibrary.Security
{
	public static partial class TokenHelper
	{
		/// <summary>
		/// Get URL to page. If runs on website then host will be replaced with current request.
		/// Use AbsoluteUri property to get full URL string.
		/// </summary>
		/// <param name="keyName">Key name for the token parameter.</param>
		/// <param name="token">Token.</param>
		/// <param name="url">Base URL. If null, current request URL will be used.</param>
		/// <returns>URL string.</returns>
		public static Uri GetUrl(string keyName, string token, Uri url = null)
		{
			var u = url ?? GetRequestUrl();
			if (u is null)
				return null;
			var port = u.IsDefaultPort ? "" : ":" + u.Port;
			var absoluteUri = string.Format("{0}://{1}{2}{3}?{4}={5}", u.Scheme, u.Host, port, u.AbsolutePath, keyName, token);
			return new Uri(absoluteUri);
		}

		/// <summary>
		/// Get full URL with specified path. Use AbsoluteUri property to get full URL string.
		/// </summary>
		public static Uri GetFullUrl(string absolutePath)
		{
			var u = GetRequestUrl();
			if (u is null)
				return null;
			var port = u.IsDefaultPort ? "" : ":" + u.Port;
			var absoluteUri = string.Format("{0}://{1}{2}{3}", u.Scheme, u.Host, port, absolutePath);
			return new Uri(absoluteUri);
		}

		/// <summary>
		/// Get URL to web application root. Use AbsoluteUri property to get full URL string.
		/// </summary>
		public static Uri GetApplicationUrl()
		{
			var path = GetApplicationPath();
			if (path is null)
				return null;
			return GetFullUrl(path);
		}

#if NETFRAMEWORK // .NET Framework

		public static string GetApplicationPath()
			=> System.Web.HttpContext.Current?.Request?.ApplicationPath;

		public static Uri GetRequestUrl()
			=> System.Web.HttpContext.Current?.Request?.Url;

#else

		/*
			// Call InitializeParser to initialize parser in .NET Core.
			public class Startup
			{
				public void Configure()
				{
					JocysCom.ClassLibrary.Configuration.TokenHelper.Configure(SiteHelper.ApplicationPath);
				}
			}
		*/

		private static string _ApplicationPath;

		/// <summary>
		/// Use this method to initialize configuration in .NET core.
		/// </summary>
		/// <param name="configuration"></param>
		public static void Configure(string applicationPath)
			=> _ApplicationPath = applicationPath;

		public static string GetApplicationPath()
			=> _ApplicationPath;

		public static Uri GetRequestUrl()
			=> new Uri(_ApplicationPath);

#endif

	}


}
