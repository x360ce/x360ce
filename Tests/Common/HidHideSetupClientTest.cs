// @under-test: App.v4/Common/Update/HidHideSetupClient.cs, App.v4/Common/Update/UpdateRelease.cs, App.v4/Common/CertificateHelper.cs, App.v4/Controls/OptionsUserControl.cs, docs/Help.HidHide.md
// @area: update   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// HID Hide is installed from the latest release its authors publish, and its setup runs only when it is the file the
	/// release describes and is signed by them. Every answer the network can give is played back through the client's
	/// transport, so nothing here goes online, and no setup is ever run: the checks before it are what is tested.
	/// </summary>
	[TestClass]
	public class HidHideSetupClientTest
	{
		/// <summary>A copy of the real setup of HID Hide 1.5.230, kept for offline checks. Never run: it installs a driver.</summary>
		const string RealSetup = @"D:\Temp\x360ce-hidhide\HidHide_1.5.230_x64.exe";

		const string SetupUrl = "https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe";

		/// <summary>The latest release as GitHub answers for it, with the setups named.</summary>
		static string Latest(string tag, params string[] assets)
		{
			return string.Format("{{\"name\":\"HidHide {0}\",\"tag_name\":\"{0}\",\"prerelease\":false,\"draft\":false,"
				+ "\"html_url\":\"https://github.com/nefarius/HidHide/releases/tag/{0}\",\"assets\":[{1}]}}", tag, string.Join(",", assets));
		}

		static string Asset(string name, long size, string digest = null)
		{
			return string.Format("{{\"name\":\"{0}\",\"size\":{1},\"digest\":{2},\"browser_download_url\":\"https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/{0}\"}}",
				name, size, digest == null ? "null" : "\"" + digest + "\"");
		}

		static UpdateResponse Ok(byte[] body)
		{
			return new UpdateResponse { StatusCode = 200, Body = body };
		}

		static string NewFolder()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-hidhide-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			return folder;
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The setup is chosen by the processor Windows runs on; one the release does not carry is none")]
		public void Setup_is_chosen_for_the_processor()
		{
			var today = new[] { "HidHide_1.5.230_x64.exe" };
			Assert.AreEqual("HidHide_1.5.230_x64.exe", HidHideSetupClient.SetupFor(today, "x64"));
			Assert.IsNull(HidHideSetupClient.SetupFor(today, "arm64"), "The x64 setup was offered to an ARM64 computer.");
			Assert.IsNull(HidHideSetupClient.SetupFor(today, "x86"), "The x64 setup was offered to 32-bit Windows.");
			var later = new[] { "HidHide_2.0.0_arm64.exe", "HidHide_2.0.0_x64.exe", "HidHide_2.0.0_x64.exe.sha256", "Source.zip" };
			Assert.AreEqual("HidHide_2.0.0_arm64.exe", HidHideSetupClient.SetupFor(later, "arm64"));
			Assert.AreEqual("HidHide_2.0.0_x64.exe", HidHideSetupClient.SetupFor(later, "x64"));
			Assert.IsNull(HidHideSetupClient.SetupFor(new[] { "Other_1.0_x64.exe" }, "x64"), "Another program's setup was taken.");
			CollectionAssert.Contains(new[] { "x64", "arm64", "x86" }, HidHideSetupClient.MachineArchitecture());
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The latest release gives its version, page, and this processor's setup with its size and, when GitHub shows it, its SHA-256")]
		public void Latest_release_gives_the_setup_for_the_processor()
		{
			var hash = UpdateRelease.Sha256Of(new byte[] { 1, 2, 3 });
			var asked = new List<UpdateRequest>();
			var client = new HidHideSetupClient(r => { asked.Add(r); return Ok(Encoding.UTF8.GetBytes(Latest("v1.5.230.0", Asset("HidHide_1.5.230_x64.exe", 8078016), Asset("HidHide_1.5.230_arm64.exe", 3, "sha256:" + hash)))); });
			string error;
			var release = client.Latest("x64", out error);
			Assert.IsNotNull(release, error);
			Assert.AreEqual(HidHideSetupClient.LatestUrl, asked[0].Url);
			Assert.AreEqual(new Version(1, 5, 230, 0), release.Version);
			Assert.AreEqual("https://github.com/nefarius/HidHide/releases/tag/v1.5.230.0", release.Page);
			Assert.AreEqual(SetupUrl, release.Url);
			Assert.AreEqual(8078016, release.Size);
			Assert.IsNull(release.Sha256, "GitHub shows no digest for this upload.");
			release = client.Latest("arm64", out error);
			Assert.AreEqual(hash, release.Sha256);
			release = client.Latest("x86", out error);
			Assert.IsNotNull(release, "A release without a setup for the processor is still the latest release.");
			Assert.IsNull(release.Url);
			var refusal = client.Install(release, null);
			StringAssert.Contains(refusal, "no setup for this computer's processor");
			Assert.AreEqual(3, asked.Count, "A release without a setup was downloaded from.");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A failed answer is said, never thrown")]
		public void Latest_release_fails_quietly()
		{
			string error;
			Assert.IsNull(new HidHideSetupClient(r => new UpdateResponse { StatusCode = 403 }).Latest("x64", out error));
			StringAssert.Contains(error, "403");
			Assert.IsNull(new HidHideSetupClient(r => Ok(Encoding.UTF8.GetBytes("<html>rate limited</html>"))).Latest("x64", out error));
			Assert.IsNotNull(error);
			Assert.IsNull(new HidHideSetupClient(r => Ok(Encoding.UTF8.GetBytes(Latest("latest")))).Latest("x64", out error));
			StringAssert.Contains(error, "no HID Hide version");
			Assert.IsNull(new HidHideSetupClient(r => { throw new System.Net.WebException("The remote name could not be resolved"); }).Latest("x64", out error));
			StringAssert.Contains(error, "resolved");
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A download of another size, or another SHA-256 where GitHub shows one, is refused and nothing is written")]
		public void Wrong_size_or_digest_is_refused()
		{
			var data = Encoding.ASCII.GetBytes("not a setup");
			var client = new HidHideSetupClient(r => Ok(data));
			var folder = NewFolder();
			try
			{
				var release = new UpdateRelease { Version = new Version(1, 5, 230, 0), Url = SetupUrl, Size = data.Length + 1 };
				var ex = Refused(() => client.Download(release, folder));
				StringAssert.Contains(ex.Message, "bytes");
				release.Size = data.Length;
				release.Sha256 = UpdateRelease.Sha256Of(Encoding.ASCII.GetBytes("another file"));
				ex = Refused(() => client.Download(release, folder));
				StringAssert.Contains(ex.Message, "SHA-256");
				release.Url = "http://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe";
				ex = Refused(() => client.Download(release, folder));
				StringAssert.Contains(ex.Message, "HTTPS");
				Assert.AreEqual(0, Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length, "A refused download was left on disk.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A setup signed by anybody else, or by nobody, is refused and deleted")]
		public void Wrong_signer_is_refused()
		{
			var folder = NewFolder();
			try
			{
				// Signed, but by Microsoft.
				var signed = typeof(Uri).Assembly.Location;
				if (x360ce.App.CertificateHelper.GetSigner(signed) == null)
					Assert.Inconclusive("System.dll on this machine carries no signature to read: " + signed);
				var ex = Refused(() => Download(folder, File.ReadAllBytes(signed)));
				StringAssert.Contains(ex.Message, "Microsoft Corporation");
				StringAssert.Contains(ex.Message, HidHideSetupClient.Publisher);
				// Signed by nobody: the test library is built here.
				ex = Refused(() => Download(folder, File.ReadAllBytes(typeof(HidHideSetupClientTest).Assembly.Location)));
				StringAssert.Contains(ex.Message, "no signature");
				Assert.AreEqual(0, Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Length, "A refused setup was left on disk.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The real setup, signed by its authors and matching the release, is accepted; it is not run")]
		public void Right_signer_is_accepted()
		{
			if (!File.Exists(RealSetup))
				Assert.Inconclusive("The copy of HID Hide's setup is not on this computer: " + RealSetup);
			var data = File.ReadAllBytes(RealSetup);
			var folder = NewFolder();
			try
			{
				var client = new HidHideSetupClient(r => Ok(data));
				var release = new UpdateRelease { Version = new Version(1, 5, 230, 0), Url = SetupUrl, Size = data.Length, Sha256 = UpdateRelease.Sha256Of(data) };
				var path = client.Download(release, folder);
				Assert.AreEqual("HidHide_1.5.230_x64.exe", Path.GetFileName(path), "The setup is not saved under its own name.");
				CollectionAssert.AreEqual(data, File.ReadAllBytes(path));
				Assert.IsNull(HidHideSetupClient.SignatureRefusal(path));
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("An install that is refused says why, leaves nothing in its folder, and lets the next one start")]
		public void Refused_install_says_why_and_cleans_up()
		{
			var folder = NewFolder();
			try
			{
				var data = Encoding.ASCII.GetBytes("not a setup");
				var steps = new List<string>();
				var client = new HidHideSetupClient(r => Ok(data)) { TempFolder = folder };
				var release = new UpdateRelease { Version = new Version(1, 5, 230, 0), Url = SetupUrl, Size = data.Length };
				for (var i = 0; i < 2; i++)
				{
					var result = client.Install(release, steps.Add);
					StringAssert.StartsWith(result, "HID Hide's setup was refused: ", "The second install was not let start.");
					StringAssert.Contains(result, "no signature");
				}
				Assert.IsTrue(steps.Any(x => x.StartsWith("Downloading HID Hide 1.5.230.0", StringComparison.Ordinal)), string.Join(" | ", steps));
				Assert.AreEqual(0, Directory.GetFileSystemEntries(folder).Length, "The download's folder was left behind.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update")]
		[Description("An installed version is outdated only when it is older than the release's, or not known")]
		public void Installed_version_is_compared_with_the_release()
		{
			var latest = new Version(1, 5, 230, 0);
			Assert.IsFalse(HidHideSetupClient.Outdated("1.5.230.0", latest));
			Assert.IsFalse(HidHideSetupClient.Outdated("1.5.230", latest), "Three parts and four are the same version.");
			Assert.IsFalse(HidHideSetupClient.Outdated("1.6.0.0", latest));
			Assert.IsTrue(HidHideSetupClient.Outdated("1.5.212.0", latest));
			Assert.IsTrue(HidHideSetupClient.Outdated(null, latest), "A version not recorded may be older.");
			Assert.IsTrue(HidHideSetupClient.Outdated("unknown", latest));
			Assert.IsFalse(HidHideSetupClient.Outdated("1.5.212.0", null), "Nothing found, nothing newer.");
			// The release's tag, as Latest reads it.
			Version tag;
			Assert.IsTrue(JocysCom.ClassLibrary.Helper.TryParseVersion("v1.5.230.0", out tag));
			Assert.AreEqual(latest, tag);
			Assert.IsFalse(JocysCom.ClassLibrary.Helper.TryParseVersion("latest", out tag));
		}

		[TestMethod, TestCategory("update")]
		[Description("What the setup's exit code means is said in words; a finished setup leaves the saying to what is installed")]
		public void Exit_codes_are_explained()
		{
			Assert.IsNull(HidHideSetupClient.ExplainExit(0));
			StringAssert.Contains(HidHideSetupClient.ExplainExit(-1), "administrator");
			StringAssert.Contains(HidHideSetupClient.ExplainExit(1602), "cancelled");
			StringAssert.Contains(HidHideSetupClient.ExplainExit(3010), "Restart Windows");
			StringAssert.Contains(HidHideSetupClient.ExplainExit(1603), "1603");
		}

		[TestMethod, TestCategory("update")]
		[Description("The organisation is read from a certificate's subject, quoted or not, and only when it names one")]
		public void Organisation_is_read_from_the_subject()
		{
			Assert.AreEqual(HidHideSetupClient.Publisher, x360ce.App.CertificateHelper.OrganisationOf(
				"CN=Nefarius Software Solutions e.U., O=Nefarius Software Solutions e.U., L=Wels, C=AT, SERIALNUMBER=505039f, OID.2.5.4.15=Private Organization"));
			Assert.AreEqual("Contoso, Ltd.", x360ce.App.CertificateHelper.OrganisationOf("CN=Contoso, O=\"Contoso, Ltd.\", C=US"));
			Assert.IsNull(x360ce.App.CertificateHelper.OrganisationOf("CN=Nefarius Software Solutions e.U., OU=\"O=Nefarius Software Solutions e.U.\""), "An organisational unit is not the organisation.");
			Assert.IsNull(x360ce.App.CertificateHelper.OrganisationOf("CN=Someone, O=One, O=Two"), "Two organisations are not one.");
			Assert.IsNull(x360ce.App.CertificateHelper.OrganisationOf("CN=Someone"));
			Assert.IsNull(x360ce.App.CertificateHelper.OrganisationOf(null));
		}

		[TestMethod, TestCategory("ui"), TestCategory("critical")]
		[Description("The HID Hide page shows its help in a box, as the HID Guardian page does, and keeps a link to the release page")]
		public void The_hid_hide_page_shows_its_help()
		{
			var help = AppHelper.ReadHelp(AppHelper.HelpHidHideResource);
			StringAssert.StartsWith(help.TrimStart('\uFEFF'), "# HID Hide", "The program does not carry docs/Help.HidHide.md.");
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUserControl())
				{
					page.CreateControl();
					var box = Find<RichTextBox>(page, nameof(OptionsUserControl.HidHideHelpRichTextBox));
					StringAssert.Contains(box.Text, "hides original controllers from games", "The help box is empty.");
					StringAssert.Contains(box.Text, "Install HID Hide");
					Assert.AreEqual(nameof(OptionsUserControl.HidHideTabPage), box.Parent.Parent.Name, "The help box is not on the HID Hide page.");
					Assert.AreEqual(DockStyle.Fill, box.Dock);
					Assert.IsTrue(box.ReadOnly);
					Assert.IsNotNull(Find<LinkLabel>(page, nameof(OptionsUserControl.HidHideDownloadLinkLabel)));
					Assert.IsNotNull(Find<Button>(page, nameof(OptionsUserControl.HidHideInstallButton)));
				}
			});
		}

		/// <summary>Downloads bytes as a release that describes them, without a digest, so only the signature can refuse them.</summary>
		static string Download(string folder, byte[] data)
		{
			var release = new UpdateRelease { Version = new Version(1, 5, 230, 0), Url = SetupUrl, Size = data.Length };
			return new HidHideSetupClient(r => Ok(data)).Download(release, folder);
		}

		static InvalidDataException Refused(Func<string> download)
		{
			try
			{
				var path = download();
				Assert.Fail("Accepted: " + path);
				return null;
			}
			catch (InvalidDataException ex)
			{
				return ex;
			}
		}

		static T Find<T>(Control parent, string name) where T : Control
		{
			var found = parent.Controls.Find(name, true).OfType<T>().FirstOrDefault();
			Assert.IsNotNull(found, name + " is not on the Options page.");
			return found;
		}
	}
}
