using System;
using System.Web;
using System.Web.Caching;
using x360ce.Engine;

namespace x360ce.Web
{
	/// <summary>
	/// The website's statistics as SVG pictures, for this site's pages and any page that shows them:
	/// <c>Statistics.ashx?name=NewDevices</c> (controllers in the database, by month), <c>TopGames</c> and
	/// <c>TopControllers</c>. Each is read from the database and drawn at most once an hour.
	/// </summary>
	public class Statistics : IHttpHandler
	{
		static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

		public bool IsReusable => true;

		public void ProcessRequest(HttpContext context)
		{
			var svg = GetSvg(context.Request.QueryString["name"]);
			if (svg == null)
			{
				context.Response.StatusCode = 404;
				return;
			}
			context.Response.ContentType = "image/svg+xml";
			context.Response.Charset = "utf-8";
			context.Response.Cache.SetCacheability(HttpCacheability.Public);
			context.Response.Cache.SetMaxAge(MaxAge);
			context.Response.Write(svg);
		}

		/// <summary>The picture with the name, from the cache when it was drawn within the hour; null for an unknown name.</summary>
		static string GetSvg(string name)
		{
			var key = typeof(Statistics).FullName + "." + name;
			var svg = (string)HttpRuntime.Cache[key];
			if (svg != null)
				return svg;
			switch (name)
			{
				case "NewDevices":
					svg = DeviceStatsSvg.Render(EngineHelper.GetNewDeviceStats());
					break;
				case "TopGames":
					svg = TopListSvg.Render(EngineHelper.GetTopGames(), "FileProductName", "Top Games");
					break;
				case "TopControllers":
					svg = TopListSvg.Render(EngineHelper.GetTopControllers(), "ProductName", "Top Controllers");
					break;
				default:
					return null;
			}
			HttpRuntime.Cache.Insert(key, svg, null, DateTime.UtcNow.Add(MaxAge), Cache.NoSlidingExpiration);
			return svg;
		}
	}
}
