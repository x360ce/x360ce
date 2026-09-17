// @under-test: App.v4/Common/Update/UpdateClient.cs, App.v4/Common/Update/UpdateManifest.cs
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
	/// The updater reads one small file from the newest GitHub release and trusts nothing it
	/// did not verify. Every answer the network can give is played back here through the
	/// client's transport seam, so the outcomes are pinned without a connection.
	/// </summary>
	[TestClass]
	public class UpdateClientTest
	{
		static readonly Version Local = new Version("4.22.8.0");

		static string ManifestJson(string version, byte[] file)
		{
			return string.Format(
				"{{\"version\":\"{0}\",\"file\":\"x360ce.zip\",\"size\":{1},\"sha256\":\"{2}\",\"published\":\"2026-10-01T12:00:00Z\",\"notes\":\"https://example.invalid/notes\"}}",
				version, file.Length, UpdateManifest.Sha256Of(file));
		}

		static readonly byte[] Zip = Encoding.ASCII.GetBytes("not really a zip, but bytes with a hash");

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
		[Description("A manifest is read whole, and one missing any field it needs is refused")]
		public void Manifest_parses_and_a_broken_one_is_refused()
		{
			var m = UpdateManifest.Parse(ManifestJson("4.23.1.0", Zip));
			Assert.AreEqual(new Version("4.23.1.0"), m.ParsedVersion);
			Assert.AreEqual("x360ce.zip", m.File);
			Assert.AreEqual(Zip.Length, m.Size);
			Assert.IsTrue(m.Describes(Zip));
			Assert.IsFalse(m.Describes(Encoding.ASCII.GetBytes("tampered")));
			foreach (var broken in new[] { "", "{}", "{\"version\":\"soon\"}", "<html>", "{\"version\":\"4.23.1.0\",\"file\":\"x\",\"size\":1,\"sha256\":\"short\"}" })
			{
				try { UpdateManifest.Parse(broken); Assert.Fail("Accepted: " + broken); }
				catch (FormatException) { }
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("Release titles of the fixed form give their version; anything else is ignored, and the highest wins")]
		public void Titles_are_parsed_on_the_real_form_only()
		{
			Version v;
			Assert.IsTrue(UpdateManifest.TryParseTitle("X360CE 4.21.30.0", out v));
			Assert.AreEqual(new Version("4.21.30.0"), v);
			Assert.IsFalse(UpdateManifest.TryParseTitle("X360CE 4.21", out v));
			Assert.IsFalse(UpdateManifest.TryParseTitle("Release 4.21.30.0", out v));
			Assert.IsFalse(UpdateManifest.TryParseTitle("X360CE 4.21.30.0 beta", out v));
			Assert.IsFalse(UpdateManifest.TryParseTitle(null, out v));
			var highest = UpdateManifest.HighestTitle(new[] { "X360CE 4.20.43.0", "X360CE 4.21.30.0", "Notes", "X360CE 4.9.99.0" });
			Assert.AreEqual(new Version("4.21.30.0"), highest);
			Assert.IsNull(UpdateManifest.HighestTitle(new[] { "nothing" }));
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A newer manifest says available, an equal or older one says current")]
		public void Version_compare_decides_the_outcome()
		{
			var fake = new Fake { Answer = r => Ok(ManifestJson("4.23.0.0", Zip), "\"etag-1\"") };
			var check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Available, check.Outcome);
			Assert.AreEqual(new Version("4.23.0.0"), check.Version);
			Assert.AreEqual("\"etag-1\"", check.ETag, "The tag is kept for the next look.");
			Assert.IsNotNull(check.Manifest);

			fake.Answer = r => Ok(ManifestJson("4.22.8.0", Zip));
			Assert.AreEqual(UpdateOutcome.Current, new UpdateClient(Local, fake.Send).Check(null).Outcome);
			fake.Answer = r => Ok(ManifestJson("4.21.30.0", Zip));
			Assert.AreEqual(UpdateOutcome.Current, new UpdateClient(Local, fake.Send).Check(null).Outcome);
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
			Assert.AreEqual(UpdateClient.ManifestUrl, fake.Requests[0].Url);
			Assert.AreEqual("\"etag-1\"", check.ETag);
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A release without a manifest is found by the titles of the full releases")]
		public void Missing_manifest_falls_back_to_release_titles()
		{
			const string releases = "[{\"name\":\"X360CE 4.23.5.0\",\"prerelease\":true,\"draft\":false},"
				+ "{\"name\":\"X360CE 4.23.2.0\",\"prerelease\":false,\"draft\":false},"
				+ "{\"name\":\"X360CE 4.21.30.0\",\"prerelease\":false,\"draft\":false}]";
			var fake = new Fake
			{
				Answer = r => r.Url == UpdateClient.ManifestUrl ? new UpdateResponse { StatusCode = 404 } : Ok(releases)
			};
			var check = new UpdateClient(Local, fake.Send).Check(null);
			Assert.AreEqual(UpdateOutcome.Available, check.Outcome);
			Assert.AreEqual(new Version("4.23.2.0"), check.Version, "The pre-release is invisible.");
			Assert.IsNull(check.Manifest, "Nothing to verify the zip against; the signature and version checks remain.");
			Assert.AreEqual(UpdateClient.ReleasesUrl, fake.Requests[1].Url);
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
		[Description("A download whose hash differs from the manifest is refused; a matching one is returned")]
		public void Download_is_checked_against_the_manifest()
		{
			var manifest = UpdateManifest.Parse(ManifestJson("4.23.0.0", Zip));
			var check = new UpdateCheck { Outcome = UpdateOutcome.Available, Version = manifest.ParsedVersion, Manifest = manifest };
			var fake = new Fake { Answer = r => new UpdateResponse { StatusCode = 200, Body = Encoding.ASCII.GetBytes("not really a zip, but bytes with a hasX") } };
			try { new UpdateClient(Local, fake.Send).Download(check); Assert.Fail("A tampered download was accepted."); }
			catch (InvalidDataException) { }
			Assert.AreEqual(UpdateClient.ZipUrl, fake.Requests[0].Url);

			fake.Answer = r => new UpdateResponse { StatusCode = 200, Body = Zip };
			CollectionAssert.AreEqual(Zip, new UpdateClient(Local, fake.Send).Download(check));
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
		[Description("The real transport reaches GitHub and comes back with an answer, not an exception")]
		public void Live_probe_reaches_github()
		{
			var check = new UpdateClient(Local).Check(null);
			Assert.AreNotEqual(UpdateOutcome.Failed, check.Outcome, check.Error);
			Assert.IsNotNull(check.Version);
			Console.WriteLine("{0}: {1} (manifest: {2}, etag: {3})", check.Outcome, check.Version, check.Manifest != null, check.ETag);
		}

		[TestMethod, TestCategory("update")]
		[Description("The request names the program and its version and nothing else about the sender")]
		public void Requests_identify_only_the_program()
		{
			Assert.AreEqual("x360ce/4.22.8.0", new UpdateClient(Local).UserAgent);
		}
	}
}
