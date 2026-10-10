using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace x360ce.App
{
	/// <summary>One request the update client makes. Only the address, the last ETag and a time limit.</summary>
	public class UpdateRequest
	{
		public string Url;
		/// <summary>ETag of the releases list last seen; sent as If-None-Match so an unchanged list costs no download.</summary>
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
		/// <summary>A newer version exists; the check says which, where its zip is and how to verify it.</summary>
		Available,
		/// <summary>Nothing could be learned. Silent at start, one line in the update window.</summary>
		Failed,
	}

	public class UpdateCheck
	{
		public UpdateOutcome Outcome;
		public Version Version;
		/// <summary>For a newer version, the release chosen and what it publishes about its zip.</summary>
		public UpdateRelease Release;
		/// <summary>The releases list's tag, kept only when nothing newer was found, so a newer release found once is not hidden by a 304 later.</summary>
		public string ETag;
		public string Error;
	}

	/// <summary>
	/// Asks GitHub Releases whether a newer version exists and fetches it. One request reads the
	/// newest releases; the newest full release of this program's major version, by its title, is
	/// the candidate, and its own x360ce.zip is what is downloaded and checked against the size
	/// and SHA-256 GitHub publishes for it. Requests carry only the program's name and version.
	/// </summary>
	/// <remarks>
	/// The transport is a function so every outcome can be exercised without a network: a
	/// test hands in the answers, the program hands in <see cref="Http"/>.
	///
	/// Pre-releases and drafts are skipped, and so is any other major version: v3 is published
	/// beside v4 as a full release, and GitHub's "latest release" can be either, so nothing here
	/// asks for "latest". A future major version is offered only when this rule changes.
	/// </remarks>
	public class UpdateClient
	{
		public const string ReleasesUrl = "https://api.github.com/repos/x360ce/x360ce/releases?per_page=10";
		/// <summary>The asset every v4 release carries.</summary>
		public const string ZipName = "x360ce.zip";
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
				var response = Transport(new UpdateRequest { Url = ReleasesUrl, ETag = etag });
				if (response.StatusCode == 304)
					return new UpdateCheck { Outcome = UpdateOutcome.Current, Version = LocalVersion, ETag = etag };
				if (response.StatusCode != 200)
					return Failed("The releases list answered " + response.StatusCode + ".");
				var newest = NewestRelease(response.Body, LocalVersion.Major);
				if (newest == null)
					return Failed("No release title carries a version " + LocalVersion.Major + ".x.");
				var version = newest.Version;
				if (version <= LocalVersion)
					return new UpdateCheck { Outcome = UpdateOutcome.Current, Version = version, ETag = response.ETag };
				if (newest.Url == null || newest.Sha256 == null || newest.Size <= 0)
					return Failed("Release " + version + " publishes no " + ZipName + " with its size and SHA-256.");
				return new UpdateCheck { Outcome = UpdateOutcome.Available, Version = version, Release = newest };
			}
			catch (Exception ex)
			{
				return Failed(ex.Message);
			}
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
			[DataMember(Name = "html_url")]
			public string Page { get; set; }
			[DataMember(Name = "assets")]
			public Asset[] Assets { get; set; }
		}

		[DataContract]
		class Asset
		{
			[DataMember(Name = "name")]
			public string Name { get; set; }
			[DataMember(Name = "size")]
			public long Size { get; set; }
			[DataMember(Name = "digest")]
			public string Digest { get; set; }
			[DataMember(Name = "browser_download_url")]
			public string Url { get; set; }
		}

		/// <summary>
		/// The newest full release of one major version in a GitHub releases answer, by the version its title
		/// carries, with what it publishes about its zip; null when no title of that major version is found.
		/// </summary>
		/// <remarks>A release without the zip, or one whose zip has no digest, comes back without an address or a hash.</remarks>
		public static UpdateRelease NewestRelease(byte[] json, int major)
		{
			var serializer = new DataContractJsonSerializer(typeof(Release[]));
			Release[] releases;
			using (var stream = new MemoryStream(json ?? new byte[0]))
				releases = (Release[])serializer.ReadObject(stream) ?? new Release[0];
			Release newest = null;
			Version newestVersion = null;
			foreach (var release in releases)
			{
				Version version;
				if (release.Prerelease || release.Draft || !UpdateRelease.TryParseTitle(release.Name, out version) || version.Major != major)
					continue;
				if (newestVersion == null || version > newestVersion)
				{
					newest = release;
					newestVersion = version;
				}
			}
			if (newest == null)
				return null;
			var zip = (newest.Assets ?? new Asset[0]).FirstOrDefault(x => string.Equals(x.Name, ZipName, StringComparison.OrdinalIgnoreCase));
			return new UpdateRelease
			{
				Version = newestVersion,
				Page = newest.Page,
				Url = zip == null ? null : zip.Url,
				Size = zip == null ? 0 : zip.Size,
				Sha256 = zip == null ? null : UpdateRelease.Sha256OfDigest(zip.Digest),
			};
		}

		static UpdateCheck Failed(string error)
		{
			return new UpdateCheck { Outcome = UpdateOutcome.Failed, Error = error };
		}

		/// <summary>
		/// Fetches the zip of the release the check chose, from that release, and refuses bytes that are not the
		/// ones the release describes.
		/// </summary>
		/// <exception cref="InvalidOperationException">The check chose no release to download.</exception>
		/// <exception cref="InvalidDataException">The download is not the file the release says it published.</exception>
		public byte[] Download(UpdateCheck check)
		{
			var release = check == null ? null : check.Release;
			if (release == null || string.IsNullOrEmpty(release.Url))
				throw new InvalidOperationException("No release was chosen to download.");
			var response = Transport(new UpdateRequest { Url = release.Url, TimeoutMs = DownloadTimeoutMs });
			if (response.StatusCode != 200 || response.Body == null)
				throw new WebException("The download answered " + response.StatusCode + ".");
			if (!release.Describes(response.Body))
				throw new InvalidDataException(string.Format(
					"The downloaded file is not the one the release describes ({0:N0} bytes, SHA-256 {1}).",
					response.Body.LongLength, UpdateRelease.Sha256Of(response.Body)));
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
