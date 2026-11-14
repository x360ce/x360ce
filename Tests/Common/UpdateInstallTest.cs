// @under-test: App.v4/Controls/OptionsUpdateUserControl.cs, App.v4/Common/CertificateHelper.cs, App.v4/MainForm.Update.cs
// @area: update   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;

namespace x360ce.Tests
{
	/// <summary>
	/// What the Update page does with a release once it is downloaded: unpacks the program, checks its signature and its
	/// version, and leaves nothing beside the program when it stops. Played with local files through the client's
	/// transport, so no network is used.
	/// </summary>
	/// <remarks>
	/// The signature decides as Windows does, without a revocation lookup. The page used to write the whole download to
	/// disk, read it back, and build a certificate chain online whose answer was thrown away, all on the window's thread,
	/// and to leave the zip and the unpacked program beside the program.
	/// </remarks>
	[TestClass]
	public class UpdateInstallTest
	{
		const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

		/// <summary>The longest the window's thread may be held at once while a release is checked.</summary>
		const int LongestHoldMs = 100;

		/// <summary>Signed by Microsoft, with its signature inside the file as x360ce's is, and a version written as numbers only, as x360ce's is.</summary>
		static string SignedFile()
		{
			var signed = typeof(Assert).Assembly.Location;
			if (CertificateHelper.GetSigner(signed) == null)
				Assert.Inconclusive("The test framework's library carries no signature to read: " + signed);
			return signed;
		}

		/// <summary>Built here and signed by nobody.</summary>
		static string UnsignedFile()
		{
			return typeof(UpdateInstallTest).Assembly.Location;
		}

		/// <summary>A release's zip: the program inside, beside room that makes it about as large as the real download.</summary>
		static byte[] ReleaseZip(string program)
		{
			using (var stream = new MemoryStream())
			{
				using (var zip = ZipStorer.Create(stream, null, true))
				{
					using (var file = File.OpenRead(program))
						zip.AddStream(ZipStorer.Compression.Deflate, "x360ce.exe", file, DateTime.Now);
					using (var room = new MemoryStream(new byte[20 * 1024 * 1024]))
						zip.AddStream(ZipStorer.Compression.Store, "room.bin", room, DateTime.Now);
				}
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Gives the page a release to download, unpack and check, beside a program in a folder of its own, and returns the
		/// page's log once it has finished. The program has the same version as the one unpacked, so nothing is installed.
		/// </summary>
		/// <param name="longestHoldMs">The longest the window's thread was held at once meanwhile.</param>
		static string[] Check(string folder, byte[] zip, out long longestHoldMs)
		{
			string[] lines = null;
			long longest = 0;
			Ui.OnUiThread(() =>
			{
				using (var page = new OptionsUpdateUserControl())
				{
					var handle = page.Handle;
					typeof(OptionsUpdateUserControl).GetField(nameof(OptionsUpdateUserControl.processFileName), Private).SetValue(page, Path.Combine(folder, "x360ce.exe"));
					var button = (Button)page.Controls.Find(nameof(OptionsUpdateUserControl.CheckButton), true).Single();
					var log = (TextBox)page.Controls.Find(nameof(OptionsUpdateUserControl.LogTextBox), true).Single();
					((CheckBox)page.Controls.Find(nameof(OptionsUpdateUserControl.CheckDigitalSignatureCheckBox), true).Single()).Checked = true;
					// Kept on: a version that differs would replace the program and restart the process, the test host.
					((CheckBox)page.Controls.Find(nameof(OptionsUpdateUserControl.CheckVersionCheckBox), true).Single()).Checked = true;
					var release = new UpdateRelease { Url = "https://example.invalid/x360ce.zip", Size = zip.Length, Sha256 = UpdateRelease.Sha256Of(zip) };
					var check = new UpdateCheck { Outcome = UpdateOutcome.Available, Release = release };
					var client = new UpdateClient(new Version("4.0.0.0"), r => new UpdateResponse { StatusCode = 200, Body = zip });
					// As Check now leaves it once a newer release is found.
					button.Enabled = false;
					typeof(OptionsUpdateUserControl).GetMethod(nameof(OptionsUpdateUserControl.Step2Download), Private).Invoke(page, new object[] { client, check });
					var until = DateTime.UtcNow.AddSeconds(60);
					var watch = new Stopwatch();
					while (!button.Enabled && DateTime.UtcNow < until)
					{
						watch.Restart();
						Application.DoEvents();
						longest = Math.Max(longest, watch.ElapsedMilliseconds);
						Thread.Sleep(1);
					}
					Assert.IsTrue(button.Enabled, "The check did not finish: " + log.Text);
					lines = log.Lines;
				}
			});
			longestHoldMs = longest;
			return lines;
		}

		static string NewFolder(string program)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce.Tests", "update-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			File.Copy(program, Path.Combine(folder, "x360ce.exe"));
			return folder;
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A signed program passes the signature check and an unsigned one fails it, as Windows decides, without a network")]
		public void The_signature_decides_as_windows_does()
		{
			AssertTrusted(SignedFile(), true);
			AssertTrusted(UnsignedFile(), false);
		}

		/// <summary>Asks the page's signature check about a file, timed.</summary>
		static void AssertTrusted(string file, bool trusted)
		{
			Exception error;
			var watch = Stopwatch.StartNew();
			var answer = CertificateHelper.IsSignedAndTrusted(file, out error);
			Console.WriteLine("{0}: {1} in {2} ms{3}", Path.GetFileName(file), answer, watch.ElapsedMilliseconds, error == null ? "" : " (" + error.Message + ")");
			Assert.AreEqual(trusted, answer, file);
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A signed release is unpacked and checked off the window's thread, and leaves nothing beside the program")]
		public void A_signed_release_is_checked_off_the_window_thread_and_leaves_nothing()
		{
			var signed = SignedFile();
			var folder = NewFolder(signed);
			try
			{
				long longest;
				var lines = Check(folder, ReleaseZip(signed), out longest);
				Console.WriteLine(string.Join(Environment.NewLine, lines));
				Console.WriteLine("Longest hold of the window's thread: {0} ms", longest);
				Assert.IsTrue(lines.Any(x => x.StartsWith("Check Digital Signature... Done")), "The signed program did not pass: " + string.Join(" | ", lines));
				CollectionAssert.Contains(lines, "Versions are the same. Skip Update.");
				CollectionAssert.AreEqual(new[] { "x360ce.exe" }, Directory.GetFiles(folder).Select(Path.GetFileName).ToArray(), "Files were left beside the program.");
				Assert.IsTrue(longest <= LongestHoldMs, "The window's thread was held for " + longest + " ms at once while the release was checked.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("An unsigned release fails the signature check off the window's thread, and leaves nothing beside the program")]
		public void An_unsigned_release_fails_and_leaves_nothing()
		{
			var unsigned = UnsignedFile();
			var folder = NewFolder(unsigned);
			try
			{
				long longest;
				var lines = Check(folder, ReleaseZip(unsigned), out longest);
				Console.WriteLine(string.Join(Environment.NewLine, lines));
				Console.WriteLine("Longest hold of the window's thread: {0} ms", longest);
				Assert.IsTrue(lines.Any(x => x.StartsWith("Check Digital Signature... Failed")), "The unsigned program was not refused: " + string.Join(" | ", lines));
				Assert.IsFalse(lines.Any(x => x.StartsWith("Current version")), "The version of a refused program was read.");
				CollectionAssert.AreEqual(new[] { "x360ce.exe" }, Directory.GetFiles(folder).Select(Path.GetFileName).ToArray(), "Files were left beside the program.");
				Assert.IsTrue(longest <= LongestHoldMs, "The window's thread was held for " + longest + " ms at once while the release was checked.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The program an update replaced is deleted when the new one starts, once the old one has let go of it, with what older versions left")]
		public void The_replaced_program_is_deleted_at_the_next_start()
		{
			var folder = NewFolder(UnsignedFile());
			try
			{
				var program = Path.Combine(folder, "x360ce.exe");
				var replaced = program + ".bak";
				File.WriteAllText(replaced, "the program before the update");
				// Older versions left the download and the program unpacked from it.
				File.WriteAllText(program + ".zip", "a download");
				File.WriteAllText(program + ".tmp", "an unpacked program");
				// The copy that installed the update may still be closing, and holds the file until it has.
				System.Threading.Tasks.Task deleting;
				using (File.Open(replaced, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					deleting = OptionsUpdateUserControl.DeleteLeftovers(program);
					Assert.IsFalse(deleting.Wait(300), "The delete gave up while the file was held.");
					Assert.IsTrue(File.Exists(replaced));
				}
				Assert.IsTrue(deleting.Wait(10000), "The delete did not finish once the file was let go of.");
				CollectionAssert.AreEqual(new[] { "x360ce.exe" }, Directory.GetFiles(folder).Select(Path.GetFileName).ToArray(), "Files were left beside the program.");
				Assert.IsTrue(OptionsUpdateUserControl.DeleteLeftovers(program).Wait(1000), "With nothing to delete, the delete waits.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}
	}
}
