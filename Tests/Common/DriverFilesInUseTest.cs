// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>Unpacking the virtual driver package into a folder where a file is still in use.</summary>
	/// <remarks>
	/// The package goes to the same folder every time, and a file from the last time can still be in use.
	/// A file that already holds the packed bytes is left as it is. Windows refuses to write over one that
	/// has to change while it is in use (0x80070020), and installing, repairing or removing the driver then
	/// stops as a setup that did not run, rather than raising the refusal from the button.
	/// </remarks>
	[TestClass]
	public class DriverFilesInUseTest
	{
		static readonly byte[] Setup = Encoding.ASCII.GetBytes("setup program, as packed");
		string _folder;

		[TestInitialize]
		public void Before()
		{
			_folder = Path.Combine(Path.GetTempPath(), "x360ce-driver-" + Guid.NewGuid().ToString("N"));
		}

		[TestCleanup]
		public void After()
		{
			if (Directory.Exists(_folder))
				Directory.Delete(_folder, true);
		}

		string SetupPath { get { return Path.Combine(_folder, "Win10Setup", "setup.exe"); } }

		/// <summary>A package holding one setup program in a folder, as the embedded one does.</summary>
		static MemoryStream Package(byte[] setup)
		{
			var ms = new MemoryStream();
			var zip = ZipStorer.Create(ms, "", true);
			zip.AddStream(ZipStorer.Compression.Deflate, "Win10Setup/setup.exe", new MemoryStream(setup), new DateTime(2024, 1, 1), "");
			zip.Close();
			ms.Position = 0;
			return ms;
		}

		/// <summary>Holds the setup the way a running program holds its own file: others may read it, nobody may write it.</summary>
		FileStream InUse()
		{
			return new FileStream(SetupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A package unpacks into a folder that is not there yet")]
		public void A_package_unpacks_into_a_new_folder()
		{
			VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			CollectionAssert.AreEqual(Setup, File.ReadAllBytes(SetupPath));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A file in use that already holds the packed bytes is left as it is")]
		public void An_unchanged_file_in_use_is_left_alone()
		{
			VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			using (InUse())
				VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			CollectionAssert.AreEqual(Setup, File.ReadAllBytes(SetupPath));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A changed file is written, and refused while in use rather than skipped")]
		public void A_changed_file_is_written_and_refused_while_in_use()
		{
			var newer = Encoding.ASCII.GetBytes("setup program, newer");
			VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			using (InUse())
			{
				try
				{
					VirtualDriverInstaller.ExtractZip(Package(newer), _folder);
					Assert.Fail("A file that has to change was skipped because it was in use; the old setup would run.");
				}
				catch (IOException) { }
			}
			VirtualDriverInstaller.ExtractZip(Package(newer), _folder);
			CollectionAssert.AreEqual(newer, File.ReadAllBytes(SetupPath));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A stale file of the same length and date is rewritten, and refused while in use")]
		public void A_stale_file_of_the_same_length_is_rewritten()
		{
			VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			var stale = Encoding.ASCII.GetBytes("setup program, as PACKED");
			Assert.AreEqual(Setup.Length, stale.Length);
			var time = File.GetLastWriteTime(SetupPath);
			File.WriteAllBytes(SetupPath, stale);
			File.SetLastWriteTime(SetupPath, time);
			using (InUse())
			{
				try
				{
					VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
					Assert.Fail("A stale file of the same length was taken as correct; the old setup would run.");
				}
				catch (IOException) { }
			}
			VirtualDriverInstaller.ExtractZip(Package(Setup), _folder);
			CollectionAssert.AreEqual(Setup, File.ReadAllBytes(SetupPath));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The driver setup treats a refused unpack as a setup that did not run")]
		public void A_refused_unpack_is_a_setup_that_did_not_run()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput", "VirtualDriverInstaller.cs"));
			var head = Ui.Between(source, "static bool RunModernSetup()", "var setup = GetModernSetupPath();");
			StringAssert.Matches(head, new Regex(@"if \(!ExtractViGemBusFiles\(\)\)\s*return false;"),
				"RunModernSetup runs the setup after the package could not be unpacked.");
			var extract = Ui.Between(source, "static bool ExtractViGemFiles(string source, string target)",
				"public static void ExtractZip(");
			StringAssert.Matches(extract, new Regex(
				@"catch \(Exception ex\) when \(ex is IOException \|\| ex is UnauthorizedAccessException\)\s*\{[^}]*return false;"),
				"A refused unpack leaves ExtractViGemFiles as an exception and reaches the Uninstall button.");
			Assert.IsFalse(source.Contains("bool overwrite"),
				"The unpack takes an overwrite switch it does not use; a file is written only when it differs.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The elevated copy reports whether installing or removing the driver worked")]
		public void The_elevated_copy_reports_whether_install_and_uninstall_worked()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Program.AdminCommands.cs"));
			foreach (var command in new[] { "InstallViGEmBus", "UninstallViGEmBus" })
			{
				var block = Ui.Between(source, "AdminCommand." + command + ".ToString()", "return true;");
				StringAssert.Contains(block, "Environment.ExitCode = ",
					command + " exits 0 whatever happened, so the caller records Done for a driver it did not change.");
				StringAssert.Contains(block, "(int)AdminResult.Done", command);
				StringAssert.Contains(block, "(int)AdminResult.Failed", command);
			}
		}
	}
}
