using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace x360ce.App
{
	/// <summary>One request the update client makes. Only the address, the last ETag and a time limit.</summary>
	public class UpdateRequest
	{
		public string Url;
		/// <summary>ETag of the manifest last seen; sent as If-None-Match so an unchanged one costs no download.</summary>
		public string ETag;
		public int TimeoutMs = UpdateClient.TimeoutMs;
	}

	public class UpdateResponse
	{
		public int StatusCode;
		public string ETag;
		public byte[] Body;
	}

	public enum UpdateOutcome
	{
		/// <summary>This version is the newest, or nothing has changed since the last look.</summary>
		Current,
		/// <summary>A newer version exists; the check says which and, when the manifest was read, how to verify it.</summary>
		Available,
		/// <summary>Nothing could be learned. Silent at start, one line in the update window.</summary>
		Failed,
	}

	public class UpdateCheck
	{
		public UpdateOutcome Outcome;
		public Version Version;
		/// <summary>Null when the version came from the release titles rather than a manifest.</summary>
		public UpdateManifest Manifest;
		public string ETag;
		public string Error;
	}

	/// <summary>
	/// Asks GitHub Releases whether a newer version exists and fetches it. The release page is
	/// the single source of truth: the manifest attached to the newest release says what the
	/// version is and what the zip must hash to, and the release titles are the fallback for a
	/// release published without one. Requests carry only the program's name and version.
	/// </summary>
	/// <remarks>
	/// The transport is a function so every outcome can be exercised without a network: a
	/// test hands in the answers, the program hands in <see cref="Http"/>.
	/// </remarks>
	public class UpdateClient
	{
		public const string ManifestUrl = "https://github.com/x360ce/x360ce/releases/latest/download/latest.json";
		public const string ZipUrl = "https://github.com/x360ce/x360ce/releases/latest/download/x360ce.zip";
		public const string ReleasesUrl = "https://api.github.com/repos/x360ce/x360ce/releases?per_page=10";
		public const int TimeoutMs = 10000;
		/// <summary>The download is given longer than the probe; the zip is a few megabytes.</summary>
		public const int DownloadTimeoutMs = 120000;

		public UpdateClient(Version localVersion, Func<UpdateRequest, UpdateResponse> transport = null)
		{
			LocalVersion = localVersion;
			Transport = transport ?? Http;
		}

		public readonly Version LocalVersion;
		public readonly Func<UpdateRequest, UpdateResponse> Transport;

		/// <summary>The one thing a request says about the sender.</summary>
		public string UserAgent
		{
			get { return "x360ce/" + LocalVersion; }
		}

		/// <summary>Looks for a newer version. Never throws: a failure is an outcome.</summary>
		public UpdateCheck Check(string etag)
		{
			try
			{
				var response = Transport(new UpdateRequest { Url = ManifestUrl, ETag = etag });
				if (response.StatusCode == 304)
					return new UpdateCheck { Outcome = UpdateOutcome.Current, Version = LocalVersion, ETag = etag };
				if (response.StatusCode == 404)
					return CheckTitles();
				if (response.StatusCode != 200)
					return Failed("The manifest request answered " + response.StatusCode + ".");
				var manifest = UpdateManifest.Parse(Encoding.UTF8.GetString(response.Body ?? new byte[0]));
				var version = manifest.ParsedVersion;
				return new UpdateCheck
				{
					Outcome = version > LocalVersion ? UpdateOutcome.Available : UpdateOutcome.Current,
					Version = version,
					Manifest = manifest,
					ETag = response.ETag ?? etag,
				};
			}
			catch (Exception ex)
			{
				return Failed(ex.Message);
			}
		}

		/// <summary>Releases published before manifests existed are still found by their titles.</summary>
		UpdateCheck CheckTitles()
		{
			var response = Transport(new UpdateRequest { Url = ReleasesUrl });
			if (response.StatusCode != 200)
				return Failed("The releases list answered " + response.StatusCode + ".");
			var titles = ReleaseTitles(response.Body);
			var highest = UpdateManifest.HighestTitle(titles);
			if (highest == null)
				return Failed("No release title carries a version.");
			return new UpdateCheck
			{
				Outcome = highest > LocalVersion ? UpdateOutcome.Available : UpdateOutcome.Current,
				Version = highest,
			};
		}

		[DataContract]
		class Release
		{
			[DataMember(Name = "name")]
			public string Name { get; set; }
			[DataMember(Name = "prerelease")]
			public bool Prerelease { get; set; }
			[DataMember(Name = "draft")]
			public bool Draft { get; set; }
		}

		/// <summary>Titles of the full releases in a GitHub releases answer, newest first as GitHub lists them.</summary>
		public static string[] ReleaseTitles(byte[] json)
		{
			var serializer = new DataContractJsonSerializer(typeof(Release[]));
			using (var stream = new MemoryStream(json ?? new byte[0]))
			{
				var releases = (Release[])serializer.ReadObject(stream) ?? new Release[0];
				return releases.Where(x => !x.Prerelease && !x.Draft).Select(x => x.Name).ToArray();
			}
		}

		static UpdateCheck Failed(string error)
		{
			return new UpdateCheck { Outcome = UpdateOutcome.Failed, Error = error };
		}

		/// <summary>
		/// Fetches the newest zip and, when the check carried a manifest, refuses bytes that are
		/// not the ones the manifest describes.
		/// </summary>
		/// <exception cref="InvalidDataException">The download is not the file the release says it published.</exception>
		public byte[] Download(UpdateCheck check)
		{
			var response = Transport(new UpdateRequest { Url = ZipUrl, TimeoutMs = DownloadTimeoutMs });
			if (response.StatusCode != 200 || response.Body == null)
				throw new WebException("The download answered " + response.StatusCode + ".");
			var manifest = check == null ? null : check.Manifest;
			if (manifest != null && !manifest.Describes(response.Body))
				throw new InvalidDataException(string.Format(
					"The downloaded file is not the one the release describes ({0:N0} bytes, SHA-256 {1}).",
					response.Body.LongLength, UpdateManifest.Sha256Of(response.Body)));
			return response.Body;
		}

		/// <summary>Whether the program a release unpacked to is the version the release announced.</summary>
		public static bool Announced(UpdateCheck check, Version unpacked)
		{
			return check == null || check.Version == null || check.Version == unpacked;
		}

		/// <summary>How long a day is, for "at most once a day".</summary>
		public static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);
		/// <summary>The start-up probe lands somewhere in this window, so clients starting together do not arrive together.</summary>
		public static readonly TimeSpan SpreadWindow = TimeSpan.FromHours(1);

		/// <summary>
		/// When the start-up probe should run: null for never (checking is off, or a day has not
		/// passed since the last look), otherwise a delay chosen at random inside the window.
		/// </summary>
		public static TimeSpan? NextProbeDelay(bool enabled, DateTime lastCheck, DateTime now, Random random)
		{
			if (!enabled)
				return null;
			if (lastCheck > now - CheckInterval && lastCheck <= now)
				return null;
			return TimeSpan.FromMilliseconds(random.NextDouble() * SpreadWindow.TotalMilliseconds);
		}

		/// <summary>The real transport: one HTTPS request over TLS 1.2 that says nothing about the machine.</summary>
		public UpdateResponse Http(UpdateRequest r)
		{
			// Windows 7 and 8.1 do not offer TLS 1.2 to .NET Framework unless asked, and GitHub
			// accepts nothing older.
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			var request = (HttpWebRequest)WebRequest.Create(r.Url);
			request.Method = "GET";
			request.UserAgent = UserAgent;
			request.Accept = "application/json, application/octet-stream";
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
				// 304 and 404 arrive as exceptions here, and both are answers, not failures.
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
	}
}
