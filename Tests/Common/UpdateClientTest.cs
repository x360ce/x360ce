// @under-test: App.v4/Common/Update/UpdateClient.cs, App.v4/Common/Update/UpdateRelease.cs
// @area: update   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The updater reads the list of the newest GitHub releases, picks one by its title, and trusts no
	/// download it did not check against what GitHub publishes for that release. Every answer the network
	/// can give is played back here through the client's transport seam, so the outcomes are pinned
	/// without a connection.
	/// </summary>
	[TestClass]
	public class UpdateClientTest
	{
		static readonly Version Local = new Version("4.22.8.0");

		static readonly byte[] Zip = Encoding.ASCII.GetBytes("not really a zip, but bytes with a hash");
		static readonly byte[] Tampered = Encoding.ASCII.GetBytes("not really a zip, but bytes with a hasX");

		static string ZipUrl(string version)
		{
			return "https://github.com/x360ce/x360ce/releases/download/" + version + "/x360ce.zip";
		}

		/// <summary>The x360ce.zip asset of a release, as the GitHub releases list shows it.</summary>
		static string ZipAsset(string version, bool withDigest = true)
		{
			return string.Format(
				"{{\"name\":\"x360ce.zip\",\"size\":{0},\"digest\":{1},\"browser_download_url\":\"{2}\"}}",
				Zip.Length, withDigest ? "\"sha256:" + UpdateRelease.Sha256Of(Zip) + "\"" : "null", ZipUrl(version));
		}

		/// <summary>The assets of a v3 release, which carries no x360ce.zip.</summary>
		const string V3Assets =
			"{\"name\":\"x360ce_x64.zip\",\"size\":10,\"digest\":null,\"browser_download_url\":\"https://example.invalid/x360ce_x64.zip\"},"
			+ "{\"name\":\"x360ce_x86.zip\",\"size\":10,\"digest\":null,\"browser_download_url\":\"https://example.invalid/x360ce_x86.zip\"}";

		static string Release(string title, string assets, bool prerelease = false, bool draft = false)
		{
			return string.Format(
				"{{\"name\":\"{0}\",\"prerelease\":{1},\"draft\":{2},\"html_url\":\"https://github.com/x360ce/x360ce/releases/tag/{3}\",\"assets\":[{4}]}}",
				title, prerelease ? "true" : "false", draft ? "true" : "false", title.Replace(' ', '-'), assets);
		}

		static string List(params string[] releases)
		{
			return "[" + string.Join(",", releases) + "]";
		}

		/// <summary>A transport that answers from a script and remembers what was asked.</summary>
		class Fake
		{
			public readonly List<UpdateRequest> Requests = new List<UpdateRequest>();
			public Func<UpdateRequest, UpdateResponse> Answer;
			public UpdateResponse Send(UpdateRequest r)
			{
				Requests.Add(r);
				return Answer(r);
			}
		}

		static UpdateResponse Ok(string body, string etag = null)
		{
			return new UpdateResponse { StatusCode = 200, Body = Encoding.UTF8.GetBytes(body), ETag = etag };
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("Release titles of the fixed form give their version; anything else is ignored")]
		public void Titles_are_parsed_on_the_real_form_only()
		{
			Version v;
			Assert.IsTrue(UpdateRelease.TryParseTitle("X360CE 4.21.30.0", out v));
			Assert.AreEqual(new Version("4.21.30.0"), v);
			Assert.IsFalse(UpdateRelease.TryParseTitle("X360CE 4.21", out v));
			Assert.IsFalse(UpdateRelease.TryParseTitle("Release 4.21.30.0", out v));
			Assert.IsFalse(UpdateRelease.TryParseTitle("X360CE 4.21.30.0 beta", out v));
			Assert.IsFalse(UpdateRelease.TryParseTitle(null, out v));
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A GitHub asset digest gives its SHA-256 only in the form GitHub writes it")]
		public void Digest_is_read_only_in_its_known_form()
		{
			var hex = UpdateRelease.Sha256Of(Zip);
			Assert.AreEqual(hex, UpdateRelease.Sha256OfDigest("sha256:" + hex));
			Assert.AreEqual(hex, UpdateRelease.Sha256OfDigest("sha256:" + hex.ToUpperInvariant()));
			Assert.IsNull(UpdateRelease.Sha256OfDigest(null));
			Assert.IsNull(UpdateRelease.Sha256OfDigest(hex), "No algorithm named.");
			Assert.IsNull(UpdateRelease.Sha256OfDigest("sha512:" + hex));
			Assert.IsNull(UpdateRelease.Sha256OfDigest("sha256:" + hex.Substring(1)), "Too short.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The newest full release of this major version is chosen by its title, with its own zip, size and SHA-256")]
		public void Newest_release_of_this_major_version_is_chosen()
		{
			var releases = List(
				Release("X360CE 4.26.0.0", ZipAsset("4.26.0.0"), prerelease: true),
				Release("X360CE 4.25.99.0", ZipAsset("4.25.99.0"), draft: true),
				Release("X360CE 5.0.0.0", ZipAsset("5.0.0.0")),
				Release("X360CE 3.6.0.0", V3Assets),
				Release("Notes", ""),
				Release("X360CE 4.21.30.0", ZipAsset("4.21.30.0")),
				Release("X360CE 4.23.2.0", ZipAsset("4.23.2.0")));
			var fake = new Fake { Answer = r => Ok(releases, "\"etag-2\"") };
			var check = new UpdateClient(Local, fake.Send).Check("\"etag-1\"");
			Assert.AreEqual(UpdateOutcome.Available, check.Outcome, check.Error);
			Assert.AreEqual(new Version("4.23.2.0"), check.Version, "Pre-releases, drafts and other major versions are invisible; the highest version wins, not the first listed.");
			Assert.AreEqual(UpdateClient.ReleasesUrl, fake.Requests[0].Url);
			Assert.AreEqual("\"etag-1\"", fake.Requests[0].ETag);
			Assert.AreEqual(ZipUrl("4.23.2.0"), check.Release.Url, "The zip of the release chosen, never \"latest\".");
			Assert.AreEqual(Zip.Length, check.Release.Size);
			Assert.AreEqual(UpdateRelease.Sha256Of(Zip), check.Release.Sha256);
			Assert.AreEqual("https://github.com/x360ce/x360ce/releases/tag/X360CE-4.23.2.0", check.Release.Page);
			Assert.IsNull(check.ETag, "A newer release found is never hidden by a later 304.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("An equal or older newest release says current and keeps the list's tag for the next look")]
		public void Equal_or_older_release_is_current_and_keeps_the_tag()
		{
			var fake = new Fake { Answer = r => Ok(List(Release("X360CE 4.22.8.0", ZipAsset("4.22.8.0"))), "\"etag-1\"") };
			var check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Current, check.Outcome, check.Error);
			Assert.AreEqual(Local, check.Version);
			Assert.AreEqual("\"etag-1\"", check.ETag);

			fake.Answer = r => Ok(List(Release("X360CE 4.21.30.0", V3Assets)));
			Assert.AreEqual(UpdateOutcome.Current, new UpdateClient(Local, fake.Send).Check(null).Outcome, "An older release is not looked at further.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("304 means current, sends the stored tag, and downloads nothing")]
		public void Not_modified_is_current_without_a_download()
		{
			var fake = new Fake { Answer = r => new UpdateResponse { StatusCode = 304 } };
			var check = new UpdateClient(Local, fake.Send).Check("\"etag-1\"");
			Assert.AreEqual(UpdateOutcome.Current, check.Outcome);
			Assert.AreEqual(1, fake.Requests.Count);
			Assert.AreEqual("\"etag-1\"", fake.Requests[0].ETag);
			Assert.AreEqual(UpdateClient.ReleasesUrl, fake.Requests[0].Url);
			Assert.AreEqual("\"etag-1\"", check.ETag);
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A newer release without x360ce.zip and its SHA-256 is not offered, and neither is an older one in its place")]
		public void Release_without_a_verifiable_zip_is_not_offered()
		{
			var fake = new Fake { Answer = r => Ok(List(Release("X360CE 4.23.2.0", V3Assets), Release("X360CE 4.23.1.0", ZipAsset("4.23.1.0")))) };
			var check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Failed, check.Outcome);
			StringAssert.Contains(check.Error, "4.23.2.0");
			StringAssert.Contains(check.Error, UpdateClient.ZipName);

			fake.Answer = r => Ok(List(Release("X360CE 4.23.2.0", ZipAsset("4.23.2.0", withDigest: false))));
			Assert.AreEqual(UpdateOutcome.Failed, new UpdateClient(Local, fake.Send).Check(null).Outcome, "No digest, nothing to check the zip against.");

			fake.Answer = r => Ok(List(Release("X360CE 3.6.0.0", V3Assets), Release("Notes", "")));
			check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Failed, check.Outcome, "No release of this major version is not the same as nothing newer.");
			StringAssert.Contains(check.Error, "4.x");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("An unreachable service is a quiet outcome, never an exception")]
		public void Unreachable_service_fails_quietly()
		{
			var fake = new Fake { Answer = r => { throw new WebException("The remote name could not be resolved"); } };
			var check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Failed, check.Outcome);
			StringAssert.Contains(check.Error, "resolved");
			fake.Answer = r => new UpdateResponse { StatusCode = 403 };
			Assert.AreEqual(UpdateOutcome.Failed, new UpdateClient(Local, fake.Send).Check(null).Outcome);
			fake.Answer = r => Ok("<html>rate limited</html>");
			Assert.AreEqual(UpdateOutcome.Failed, new UpdateClient(Local, fake.Send).Check(null).Outcome);
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The zip comes from the release chosen; one whose hash differs from GitHub's is refused, a matching one is returned")]
		public void Download_comes_from_the_chosen_release_and_is_checked()
		{
			var fake = new Fake { Answer = r => Ok(List(Release("X360CE 4.23.2.0", ZipAsset("4.23.2.0")))) };
			var client = new UpdateClient(Local, fake.Send);
			var check = client.Check(null);
			Assert.AreEqual(UpdateOutcome.Available, check.Outcome, check.Error);

			fake.Answer = r => new UpdateResponse { StatusCode = 200, Body = Tampered };
			try { client.Download(check); Assert.Fail("A tampered download was accepted."); }
			catch (InvalidDataException) { }
			Assert.AreEqual(ZipUrl("4.23.2.0"), fake.Requests[1].Url);
			Assert.AreEqual(UpdateClient.DownloadTimeoutMs, fake.Requests[1].TimeoutMs);

			fake.Answer = r => new UpdateResponse { StatusCode = 200, Body = Zip };
			CollectionAssert.AreEqual(Zip, client.Download(check));

			try { client.Download(new UpdateCheck { Outcome = UpdateOutcome.Current }); Assert.Fail("A download without a release was attempted."); }
			catch (InvalidOperationException) { }
			Assert.AreEqual(3, fake.Requests.Count, "Nothing chosen, nothing fetched.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A file whose version differs from the one announced is not what was released")]
		public void Unpacked_version_must_match_the_announcement()
		{
			var check = new UpdateCheck { Version = new Version("4.23.0.0") };
			Assert.IsTrue(UpdateClient.Announced(check, new Version("4.23.0.0")));
			Assert.IsFalse(UpdateClient.Announced(check, new Version("4.23.1.0")));
			Assert.IsTrue(UpdateClient.Announced(null, new Version("4.23.1.0")), "Nothing announced, nothing to contradict.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The signature check refuses a file that carries no trusted signature")]
		public void Signature_check_refuses_an_unsigned_file()
		{
			X509Certificate2 certificate;
			Exception error;
			// The test assembly is built here and signed by nobody.
			Assert.IsFalse(CertificateHelper.IsSignedAndTrusted(typeof(UpdateClientTest).Assembly.Location, out certificate, out error));
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("Off means no probe; on means one probe a day at a random moment within the first hour")]
		public void Probe_delay_respects_the_option_and_the_day()
		{
			var now = new DateTime(2026, 10, 1, 9, 0, 0);
			var random = new Random(1);
			Assert.IsNull(UpdateClient.NextProbeDelay(false, DateTime.MinValue, now, random), "Off: never.");
			Assert.IsNull(UpdateClient.NextProbeDelay(true, now.AddHours(-3), now, random), "Looked this morning: not again today.");
			Assert.IsNotNull(UpdateClient.NextProbeDelay(true, now.AddDays(-2), now, random));
			Assert.IsNotNull(UpdateClient.NextProbeDelay(true, DateTime.MinValue, now, random), "Never looked: look.");
			Assert.IsNotNull(UpdateClient.NextProbeDelay(true, now.AddDays(3), now, random), "A clock set back is not a reason to wait years.");
			for (var i = 0; i < 1000; i++)
			{
				var delay = UpdateClient.NextProbeDelay(true, DateTime.MinValue, now, random).Value;
				Assert.IsTrue(delay >= TimeSpan.Zero && delay < UpdateClient.SpreadWindow, "Delay outside the window: " + delay);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("network")]
		[Description("The real transport reaches GitHub, and the release it finds publishes its zip with a SHA-256")]
		public void Live_probe_reaches_github()
		{
			var check = new UpdateClient(Local).Check(null);
			Assert.AreNotEqual(UpdateOutcome.Failed, check.Outcome, check.Error);
			Assert.IsNotNull(check.Version);
			if (check.Outcome == UpdateOutcome.Available)
			{
				StringAssert.EndsWith(check.Release.Url, "/" + UpdateClient.ZipName);
				Assert.IsNotNull(check.Release.Sha256);
			}
			Console.WriteLine("{0}: {1} ({2}, etag: {3})", check.Outcome, check.Version, check.Release == null ? null : check.Release.Url, check.ETag);
		}

		[TestMethod, TestCategory("update")]
		[Description("The request names the program and its version and nothing else about the sender")]
		public void Requests_identify_only_the_program()
		{
			Assert.AreEqual("x360ce/4.22.8.0", new UpdateClient(Local).UserAgent);
		}
	}
}
