using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace x360ce.Engine
{
	/// <summary>One request an update makes: the address, what the asker already has, and a time limit.</summary>
	public class UpdateRequest
	{
		public string Url;
		/// <summary>ETag of the copy already kept; sent as If-None-Match so an unchanged file costs no download.</summary>
		public string ETag;
		public int TimeoutMs = HttpFile.TimeoutMs;
	}

	public class UpdateResponse
	{
		public int StatusCode;
		public string ETag;
		/// <summary>When the server says the file last changed, in UTC; null when it does not say.</summary>
		public DateTime? LastModified;
		public byte[] Body;
	}

	/// <summary>The transport both programs' updates share: one HTTPS request that says nothing about the machine, and a file saved whole.</summary>
	public static class HttpFile
	{
		/// <summary>How long a request may take.</summary>
		public const int TimeoutMs = 10000;

		/// <summary>How long a download may take.</summary>
		public const int DownloadTimeoutMs = 120000;

		/// <summary>The real transport: one HTTPS request over TLS 1.2, naming only the program and its version.</summary>
		public static UpdateResponse Get(UpdateRequest r, string userAgent)
		{
			// Windows 7 and 8.1 do not offer TLS 1.2 to .NET Framework unless asked, and GitHub
			// accepts nothing older.
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			var request = (HttpWebRequest)WebRequest.Create(r.Url);
			request.Method = "GET";
			request.UserAgent = userAgent;
			request.Accept = "application/json, application/octet-stream, text/plain";
			request.AllowAutoRedirect = true;
			request.Timeout = r.TimeoutMs;
			request.ReadWriteTimeout = r.TimeoutMs;
			if (!string.IsNullOrEmpty(r.ETag))
				request.Headers[HttpRequestHeader.IfNoneMatch] = r.ETag;
			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{
				// A 304 and the error statuses arrive as exceptions here; they are answers, and the caller words them.
				response = ex.Response as HttpWebResponse;
				if (response == null)
					throw;
			}
			using (response)
			{
				var result = new UpdateResponse
				{
					StatusCode = (int)response.StatusCode,
					ETag = response.Headers[HttpResponseHeader.ETag],
				};
				// Read from the header itself: HttpWebResponse.LastModified gives the time now when the server says nothing.
				DateTime lastModified;
				if (DateTime.TryParse(response.Headers[HttpResponseHeader.LastModified], CultureInfo.InvariantCulture,
					DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out lastModified))
					result.LastModified = lastModified;
				if (result.StatusCode == 200)
					using (var stream = response.GetResponseStream())
					using (var memory = new MemoryStream())
					{
						stream.CopyTo(memory);
						result.Body = memory.ToArray();
					}
				return result;
			}
		}

		/// <summary>Writes a file whole or not at all: to a file beside it first, which then takes its place.</summary>
		public static void Save(string path, string text)
		{
			var folder = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(folder))
				Directory.CreateDirectory(folder);
			var temp = path + ".tmp";
			File.WriteAllText(temp, text, new UTF8Encoding(false));
			if (File.Exists(path))
				File.Replace(temp, path, null);
			else
				File.Move(temp, path);
		}
	}

	/// <summary>What pressing an [Update ... Database] button did.</summary>
	public enum DatabaseUpdateOutcome
	{
		/// <summary>A newer copy was saved to the settings folder.</summary>
		Saved,
		/// <summary>The copy kept is the server's; nothing was saved.</summary>
		UpToDate,
		/// <summary>Nothing was saved: the address, the answer or the file was not right.</summary>
		Failed,
	}

	/// <summary>
	/// Fetches x360ce's controller database from x360ce's server into the settings folder, only when the person asks: with
	/// the ETag of the copy kept, so an unchanged file is not sent again.
	/// </summary>
	public class X360ceUpdateClient
	{
		/// <summary>Where x360ce's server serves the database (Web\ControllerDatabase.ashx).</summary>
		public const string DefaultUrl = "https://www.x360ce.com/x360ce/ControllerDatabase.ashx";

		/// <summary>The line the kept copy ends with: the server's ETag of it.</summary>
		public const string ETagPrefix = "# ETag: ";

		/// <summary>A file with fewer rows than this is not the database, whatever answered.</summary>
		public const int MinimumRows = 100;

		/// <param name="transport">Makes a request; <see cref="HttpFile.Get"/> in the programs, the answers in a test.</param>
		public X360ceUpdateClient(Func<UpdateRequest, UpdateResponse> transport)
		{
			Transport = transport;
		}

		public readonly Func<UpdateRequest, UpdateResponse> Transport;

		/// <summary>
		/// Fetches the database from x360ce's server into the settings folder off the caller's thread, as the programs'
		/// buttons do: what happened, in a sentence, and the outcome.
		/// </summary>
		/// <param name="userAgent">The program and its version, the only thing the request says about the machine.</param>
		public static Task<KeyValuePair<string, DatabaseUpdateOutcome>> UpdateSettingsCopy(string userAgent)
		{
			var client = new X360ceUpdateClient(r => HttpFile.Get(r, userAgent));
			return Task.Run(() =>
			{
				DatabaseUpdateOutcome outcome;
				var text = client.Update(DefaultUrl, X360ceDatabase.SettingsFilePath, out outcome);
				return new KeyValuePair<string, DatabaseUpdateOutcome>(text, outcome);
			});
		}

		/// <summary>Fetches the database unless the copy kept at <paramref name="path"/> is the server's, and saves it there.</summary>
		/// <param name="address">The server's file.</param>
		/// <param name="path">The settings folder's copy, <see cref="X360ceDatabase.SettingsFilePath"/>.</param>
		/// <param name="outcome">Whether a newer file was saved, the copy kept was up to date, or it failed.</param>
		/// <returns>What happened, in a sentence. Never throws: a failure is said.</returns>
		public string Update(string address, string path, out DatabaseUpdateOutcome outcome)
		{
			outcome = DatabaseUpdateOutcome.Failed;
			Uri uri;
			if (!Uri.TryCreate((address ?? "").Trim(), UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
				return "x360ce's controller database's address, \"" + address + "\", is not an http or https address, so nothing was fetched.";
			try
			{
				var answer = Transport(new UpdateRequest { Url = uri.AbsoluteUri, ETag = KeptETag(path), TimeoutMs = HttpFile.DownloadTimeoutMs });
				if (answer.StatusCode == 304)
				{
					outcome = DatabaseUpdateOutcome.UpToDate;
					return "x360ce's controller database is up to date.";
				}
				if (answer.StatusCode != 200 || answer.Body == null)
					return uri.Host + " answered " + answer.StatusCode + " when asked for x360ce's controller database.";
				var text = Encoding.UTF8.GetString(answer.Body).TrimStart('﻿').Replace("\r\n", "\n");
				var rows = SdlSource.Parse(text, SdlSourceKind.Downloaded).Mappings.Count;
				if (!text.StartsWith("# x360ce's controller database", StringComparison.Ordinal) || rows < MinimumRows)
					return string.Format(CultureInfo.InvariantCulture, "The file {0} sent is not x360ce's controller database: {1} rows. Nothing was saved.", uri.Host, rows);
				if (!string.IsNullOrEmpty(answer.ETag))
					text = text.TrimEnd('\n') + "\n" + ETagPrefix + answer.ETag + "\n";
				HttpFile.Save(path, text);
				X360ceDatabase.Reload();
				outcome = DatabaseUpdateOutcome.Saved;
				var made = X360ceDatabase.MadeOn(text);
				return string.Format(CultureInfo.InvariantCulture, "Saved x360ce's controller database{0}, {1} rows, from {2}.",
					made.HasValue ? " of " + made.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "", rows, uri.Host);
			}
			catch (Exception ex)
			{
				return "x360ce's controller database could not be updated: " + ex.Message;
			}
		}

		/// <summary>The ETag the kept copy was saved with; null when there is none.</summary>
		static string KeptETag(string path)
		{
			if (!File.Exists(path))
				return null;
			var line = File.ReadAllLines(path).LastOrDefault(x => x.StartsWith(ETagPrefix, StringComparison.Ordinal));
			return line == null ? null : line.Substring(ETagPrefix.Length).Trim();
		}
	}
}
