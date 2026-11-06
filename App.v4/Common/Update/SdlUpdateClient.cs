using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using x360ce.Engine;

namespace x360ce.App
{
	/// <summary>What pressing an [Update SDL Database] button did.</summary>
	public enum SdlUpdateOutcome
	{
		/// <summary>A newer copy was saved to the settings folder.</summary>
		Saved,
		/// <summary>The copy in use is the one at the address, or newer; nothing was saved.</summary>
		UpToDate,
		/// <summary>Nothing was saved: the address, the answer or the file was not right.</summary>
		Failed,
	}

	/// <summary>
	/// Fetches SDL's controller database into the settings folder from the address Options > Internet sets, only when the
	/// person asks.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A GitHub raw address, https://raw.githubusercontent.com/{owner}/{repository}/{branch}/{path}, of any fork, branch or
	/// file: one request asks GitHub for the newest commit that changed the file on that branch, and one fetches the file as
	/// it was at that commit. Any other http or https address, a mirror or a person's own server: one request fetches the file
	/// as it is; the SHA-1 of its bytes stands for the commit, and the answer's Last-Modified, else the time of the download,
	/// for its date. Anything else is said and not fetched.
	/// </para>
	/// <para>
	/// The requests go through <see cref="UpdateClient.Http"/>, which says only the program's name and version. The file is
	/// kept only when it reads as a database with Windows rows, and is saved with its commit and the address it came from on
	/// top (<see cref="SdlSource.WithHeader"/>), so the program knows how new it is and where it came from, and passes it
	/// over once the built-in copy is newer. A test hands in the answers.
	/// </para>
	/// </remarks>
	public class SdlUpdateClient
	{
		/// <summary>SDL_GameControllerDB's own file on its main branch: where the database comes from unless the person names another.</summary>
		public const string DefaultUrl = "https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt";

		/// <summary>Where GitHub serves a repository's files as they are.</summary>
		const string GitHubRawHost = "raw.githubusercontent.com";

		/// <summary>A file with fewer Windows rows than this is not the database, whatever answered.</summary>
		public const int MinimumWindowsRows = 100;

		public SdlUpdateClient(Func<UpdateRequest, UpdateResponse> transport = null)
		{
			Transport = transport ?? new UpdateClient(typeof(SdlUpdateClient).Assembly.GetName().Version).Http;
		}

		public readonly Func<UpdateRequest, UpdateResponse> Transport;

		[DataContract]
		class Commit
		{
			[DataMember(Name = "sha")]
			public string Sha { get; set; }
			[DataMember(Name = "commit")]
			public CommitDetail Detail { get; set; }
		}

		[DataContract]
		class CommitDetail
		{
			[DataMember(Name = "committer")]
			public CommitPerson Committer { get; set; }
		}

		[DataContract]
		class CommitPerson
		{
			[DataMember(Name = "date")]
			public string Date { get; set; }
		}

		/// <summary>
		/// Fetches the database at <paramref name="address"/> unless the one in use is that copy or newer, and saves it to
		/// <paramref name="path"/>.
		/// </summary>
		/// <param name="address">Where to fetch it from: the address Options > Internet sets.</param>
		/// <param name="path">Where to save it: the settings folder's gamecontrollerdb.txt.</param>
		/// <param name="current">The database in use, whose version is compared.</param>
		/// <param name="outcome">Whether a newer file was saved, the one in use was up to date, or it failed.</param>
		/// <returns>What happened, in a sentence. Never throws: a failure is said.</returns>
		public string Update(string address, string path, SdlDatabase current, out SdlUpdateOutcome outcome)
		{
			outcome = SdlUpdateOutcome.Failed;
			Uri uri;
			if (!Uri.TryCreate((address ?? "").Trim(), UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
				return "The SDL controller database's address, \"" + address + "\", is not an http or https address, so nothing was fetched. Options > Internet sets it.";
			try
			{
				var version = current == null ? null : current.Version;
				string repository, branch, file, sha, sender;
				DateTime date;
				byte[] body;
				if (TryReadGitHubAddress(uri, out repository, out branch, out file))
				{
					sender = "GitHub";
					var url = string.Format(CultureInfo.InvariantCulture, "https://api.github.com/repos/{0}/commits?path={1}&sha={2}&per_page=1", repository, file, branch);
					var response = Transport(new UpdateRequest { Url = url });
					if (response.StatusCode != 200)
						return "GitHub answered " + response.StatusCode + " when asked for the newest SDL controller database.";
					Commit[] commits;
					var serializer = new DataContractJsonSerializer(typeof(Commit[]));
					using (var stream = new MemoryStream(response.Body ?? new byte[0]))
						commits = (Commit[])serializer.ReadObject(stream) ?? new Commit[0];
					var commit = commits.FirstOrDefault();
					if (commit == null || commit.Sha == null || commit.Sha.Length != 40 || !commit.Sha.All(Uri.IsHexDigit)
						|| commit.Detail == null || commit.Detail.Committer == null
						|| !DateTime.TryParse(commit.Detail.Committer.Date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out date))
						return "GitHub's answer names no commit of the SDL controller database.";
					sha = commit.Sha.ToLowerInvariant();
					if (UpToDate(version, sha, date))
					{
						outcome = SdlUpdateOutcome.UpToDate;
						return UpToDateMessage(version);
					}
					var answer = Transport(new UpdateRequest { Url = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/{2}/{3}", GitHubRawHost, repository, sha, file), TimeoutMs = UpdateClient.DownloadTimeoutMs });
					if (answer.StatusCode != 200 || answer.Body == null)
						return "GitHub answered " + answer.StatusCode + " when asked for the SDL controller database's file.";
					body = answer.Body;
				}
				else
				{
					sender = uri.Host;
					var answer = Transport(new UpdateRequest { Url = uri.AbsoluteUri, TimeoutMs = UpdateClient.DownloadTimeoutMs });
					if (answer.StatusCode != 200 || answer.Body == null)
						return sender + " answered " + answer.StatusCode + " when asked for the SDL controller database.";
					body = answer.Body;
					using (var sha1 = SHA1.Create())
						sha = BitConverter.ToString(sha1.ComputeHash(body)).Replace("-", "").ToLowerInvariant();
					date = answer.LastModified ?? DateTime.UtcNow;
					if (UpToDate(version, sha, date))
					{
						outcome = SdlUpdateOutcome.UpToDate;
						return UpToDateMessage(version);
					}
				}
				// A byte order mark would stand before the header line once saved.
				var text = Encoding.UTF8.GetString(body).TrimStart('﻿');
				var source = SdlSource.Parse(text, SdlSourceKind.Downloaded);
				if (source.Mappings.Count < MinimumWindowsRows)
					return string.Format(CultureInfo.InvariantCulture, "The file {0} sent holds {1} Windows rows, so it is not the SDL controller database. Nothing was saved.", sender, source.Mappings.Count);
				Save(path, SdlSource.WithHeader(text, sha, date, uri.AbsoluteUri));
				outcome = SdlUpdateOutcome.Saved;
				return string.Format(CultureInfo.InvariantCulture, "Saved the SDL controller database of {0:yyyy-MM-dd}, {1}, {2} Windows rows, from {3}.", date, sha.Substring(0, 7), source.Mappings.Count, sender);
			}
			catch (Exception ex)
			{
				return "The SDL controller database could not be updated: " + ex.Message;
			}
		}

		/// <summary>Whether the database in use is the copy named, or newer: a copy older than the built-in one would be passed over.</summary>
		static bool UpToDate(SdlSource version, string sha, DateTime date)
		{
			return version != null && (version.Sha == sha || version.Date.Value >= date);
		}

		static string UpToDateMessage(SdlSource version)
		{
			return string.Format(CultureInfo.InvariantCulture, "The SDL controller database is up to date: {0:yyyy-MM-dd}, {1}.", version.Date.Value, version.ShortSha);
		}

		/// <summary>
		/// Reads a GitHub raw address, https://raw.githubusercontent.com/{owner}/{repository}/{branch}/{path}, also with
		/// refs/heads/ or refs/tags/ before the branch as GitHub's Raw button writes it; false for any other address.
		/// </summary>
		/// <param name="uri">The address.</param>
		/// <param name="repository">"{owner}/{repository}".</param>
		/// <param name="branch">The branch, tag or commit named.</param>
		/// <param name="file">The file's path in the repository, as the address writes it.</param>
		static bool TryReadGitHubAddress(Uri uri, out string repository, out string branch, out string file)
		{
			repository = branch = file = null;
			if (!string.Equals(uri.Host, GitHubRawHost, StringComparison.OrdinalIgnoreCase))
				return false;
			var parts = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			var at = parts.Length > 4 && parts[2] == "refs" && (parts[3] == "heads" || parts[3] == "tags") ? 4 : 2;
			if (parts.Length < at + 2)
				return false;
			repository = parts[0] + "/" + parts[1];
			branch = parts[at];
			file = string.Join("/", parts, at + 1, parts.Length - at - 1);
			return true;
		}

		/// <summary>Writes the file whole or not at all: to a file beside it first, which then takes its place.</summary>
		static void Save(string path, string text)
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
}
