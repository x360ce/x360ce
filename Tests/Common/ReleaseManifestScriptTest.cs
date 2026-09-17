// @under-test: Documents/App_5_Manifest.ps1
// @area: update   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using x360ce.App;

namespace x360ce.Tests
{
	/// <summary>
	/// The manifest the release script writes is the file the updater reads, so the two are
	/// held to one contract here: the script's output must parse in the program, carry the
	/// version of the program inside the zip, and refuse a zip whose version is not the one
	/// being released.
	/// </summary>
	[TestClass]
	public class ReleaseManifestScriptTest
	{
		static string Script
		{
			get { return Path.Combine(Ui.RepoRoot.FullName, "Documents", "App_5_Manifest.ps1"); }
		}

		/// <summary>A release-shaped zip holding the program built for these tests.</summary>
		static string ZipOfBuiltProgram(string folder)
		{
			var exe = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "bin", "Debug", "x360ce.exe");
			Assert.IsTrue(File.Exists(exe), "The program must be built first: " + exe);
			var zip = Path.Combine(folder, "x360ce.zip");
			using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
				archive.CreateEntryFromFile(exe, "x360ce.exe");
			return zip;
		}

		static int Run(string arguments, out string output)
		{
			var host = "pwsh";
			try { Process.Start(new ProcessStartInfo(host, "-v") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true }).WaitForExit(); }
			catch (Exception) { host = "powershell"; }
			var info = new ProcessStartInfo(host, "-NoProfile -NonInteractive -File \"" + Script + "\" " + arguments)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using (var p = Process.Start(info))
			{
				output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
				p.WaitForExit();
				return p.ExitCode;
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The script's manifest parses in the program and names the version of the exe in the zip")]
		public void Manifest_written_by_the_script_is_the_one_the_program_reads()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-manifest-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				var zip = ZipOfBuiltProgram(folder);
				var version = FileVersionInfo.GetVersionInfo(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "bin", "Debug", "x360ce.exe")).FileVersion;
				string output;
				var code = Run("-ZipPath \"" + zip + "\" -ExpectedVersion " + version, out output);
				Assert.AreEqual(0, code, output);
				var manifest = UpdateManifest.Parse(File.ReadAllText(Path.Combine(folder, "latest.json")));
				Assert.AreEqual(new Version(version), manifest.ParsedVersion);
				Assert.AreEqual("x360ce.zip", manifest.File);
				Assert.IsTrue(manifest.Describes(File.ReadAllBytes(zip)), "Size and hash must be those of the zip.");
				StringAssert.Contains(output, "X360CE " + version, "The release title is printed in the fixed form.");
			}
			finally { Directory.Delete(folder, true); }
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A zip carrying a version other than the one being released stops the script")]
		public void Wrong_version_in_the_zip_fails_the_release()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-manifest-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				var zip = ZipOfBuiltProgram(folder);
				string output;
				var code = Run("-ZipPath \"" + zip + "\" -ExpectedVersion 0.0.0.1", out output);
				Assert.AreNotEqual(0, code, "A wrong version must fail.");
				StringAssert.Contains(output, "Rebuild before publishing");
				Assert.IsFalse(File.Exists(Path.Combine(folder, "latest.json")), "Nothing is written for a wrong version.");
			}
			finally { Directory.Delete(folder, true); }
		}
	}
}
