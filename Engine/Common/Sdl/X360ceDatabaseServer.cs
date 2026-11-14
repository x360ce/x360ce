using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JocysCom.ClassLibrary.Data;
using x360ce.Engine.Data;

namespace x360ce.Engine
{
	/// <summary>The controller database as the server serves it: its bytes, its ETag and when its rows last changed.</summary>
	public sealed class X360ceDatabaseFile
	{
		public byte[] Bytes;
		/// <summary>The SHA-1 of the bytes, quoted as HTTP writes an ETag.</summary>
		public string ETag;
		/// <summary>When the rows last changed, in UTC, to the second as HTTP dates go.</summary>
		public DateTime LastModified;
	}

	/// <summary>
	/// How x360ce's server serves its controller database (Web\ControllerDatabase.ashx): built from the settings database
	/// and kept in memory as <see cref="DailyCache{T}"/> keeps it; answered with ETag and Last-Modified, and with 304 when
	/// the asker already has the file.
	/// </summary>
	/// <remarks>
	/// A build whose rows are the ones already served keeps the file as it was, its "# Made:" date, ETag and Last-Modified
	/// too, so a program that has it is not sent it again. Nothing is written to disk.
	/// </remarks>
	public sealed class X360ceDatabaseServer
	{
		/// <param name="build">Writes the file's text: <see cref="X360ceDatabase.Write"/> over <see cref="Entries"/>.</param>
		/// <param name="utcNow">The clock; the real one when null.</param>
		public X360ceDatabaseServer(Func<string> build, Func<DateTime> utcNow = null)
		{
			var clock = utcNow ?? (() => DateTime.UtcNow);
			Cache = new DailyCache<X360ceDatabaseFile>(served =>
			{
				var text = build();
				return served != null && WithoutDate(text) == WithoutDate(Encoding.UTF8.GetString(served.Bytes))
					? served
					: Made(text, clock());
			}, clock);
		}

		readonly DailyCache<X360ceDatabaseFile> Cache;

		/// <summary>How many times the file was built.</summary>
		public int Builds { get { return Cache.Builds; } }

		/// <summary>The file, built first when there is none or it is a day old; null when there is none and it cannot be built.</summary>
		public X360ceDatabaseFile Get()
		{
			return Cache.Get();
		}

		/// <summary>The file served for a text built at a time.</summary>
		static X360ceDatabaseFile Made(string text, DateTime now)
		{
			var bytes = new UTF8Encoding(false).GetBytes(text);
			string etag;
			using (var sha1 = SHA1.Create())
				etag = "\"" + BitConverter.ToString(sha1.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() + "\"";
			var lastModified = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
			return new X360ceDatabaseFile { Bytes = bytes, ETag = etag, LastModified = lastModified };
		}

		/// <summary>The text without its "# Made:" line, which changes with every build.</summary>
		static string WithoutDate(string text)
		{
			return string.Join("\n", text.Split('\n').Where(x => !x.StartsWith(X360ceDatabase.MadePrefix, StringComparison.Ordinal)));
		}

		/// <summary>The status a request gets: 304 when it names the file it has, by its ETag or, without one, by its date; 200 otherwise.</summary>
		/// <param name="file">The file served.</param>
		/// <param name="ifNoneMatch">The request's If-None-Match header, or null.</param>
		/// <param name="ifModifiedSince">The request's If-Modified-Since header, or null.</param>
		public static int Status(X360ceDatabaseFile file, string ifNoneMatch, string ifModifiedSince)
		{
			if (!string.IsNullOrWhiteSpace(ifNoneMatch))
				return ifNoneMatch.Split(',').Any(x => x.Trim() == file.ETag || x.Trim() == "*") ? 304 : 200;
			DateTime since;
			if (!string.IsNullOrWhiteSpace(ifModifiedSince) && DateTime.TryParse(ifModifiedSince, CultureInfo.InvariantCulture,
				DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out since) && file.LastModified <= since)
				return 304;
			return 200;
		}

		/// <summary>The controllers the settings database gives (x360ce_GetControllerDatabase): each with its most used setting.</summary>
		public static List<X360ceDatabaseEntry> Entries(DataSet ds)
		{
			var settings = SqlHelper.ConvertToList<PadSetting>(ds.Tables[1]).ToDictionary(x => x.PadSettingChecksum);
			var entries = new List<X360ceDatabaseEntry>();
			foreach (DataRow row in ds.Tables[0].Rows)
			{
				PadSetting setting;
				if (!settings.TryGetValue((Guid)row["PadSettingChecksum"], out setting))
					continue;
				entries.Add(new X360ceDatabaseEntry
				{
					ProductGuid = (Guid)row["ProductGuid"],
					ProductName = row["ProductName"] as string ?? "",
					Users = Convert.ToInt32(row["Users"], CultureInfo.InvariantCulture),
					Setting = setting,
				});
			}
			return entries;
		}
	}
}
