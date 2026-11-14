using System;
using System.Web;
using x360ce.Engine;

namespace x360ce.Web
{
	/// <summary>
	/// x360ce's controller database, gamecontrollerdb_x360ce.windows.txt: for each controller, the setting most people use
	/// with it. Built from the settings database at most once a day, and at the first request after the site starts, and
	/// served from memory, with ETag and Last-Modified, and 304 to a program that already has it.
	/// scripts\Update-X360ceDatabase.ps1 and both programs' Update buttons fetch it.
	/// </summary>
	public class ControllerDatabase : IHttpHandler
	{
		static readonly X360ceDatabaseServer Server = new X360ceDatabaseServer(Build);

		public bool IsReusable => true;

		public void ProcessRequest(HttpContext context)
		{
			var file = Server.Get();
			if (file == null)
			{
				context.Response.StatusCode = 503;
				return;
			}
			var cache = context.Response.Cache;
			cache.SetCacheability(HttpCacheability.Public);
			cache.SetETag(file.ETag);
			cache.SetLastModified(file.LastModified);
			cache.SetMaxAge(TimeSpan.FromHours(1));
			var status = X360ceDatabaseServer.Status(file, context.Request.Headers["If-None-Match"], context.Request.Headers["If-Modified-Since"]);
			if (status == 304)
			{
				context.Response.StatusCode = 304;
				context.Response.SuppressContent = true;
				return;
			}
			context.Response.ContentType = "text/plain";
			context.Response.Charset = "utf-8";
			context.Response.BinaryWrite(file.Bytes);
		}

		/// <summary>The file's text, from the settings database.</summary>
		static string Build()
		{
			var entries = X360ceDatabaseServer.Entries(EngineHelper.GetControllerDatabase());
			return X360ceDatabase.Write(entries, "x360ce's server", DateTime.UtcNow);
		}
	}
}
