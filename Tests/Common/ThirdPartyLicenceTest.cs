// @under-test: Resources/Licenses/README.md, Engine/Common/ThirdPartyLicences.cs, App.v4/Controls/AboutControl.cs, App.v3/Controls/AboutControl.cs, App.v4/x360ce.App.v4.csproj, App.v3/x360ce.App.v3.csproj
// @area: about   @layer: unit
using JocysCom.ClassLibrary.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The licences of the parts x360ce carries from other projects. Resources/Licenses/README.md lists each part, the
	/// programs that carry it and its licence file; each program embeds the list and those files, and its About page's
	/// License tab shows its own licence, then each part's name, address, licence and full text, in the list's order.
	/// </summary>
	/// <remarks>
	/// Version 3 cannot be referenced, because both programs build <c>x360ce.exe</c> in the namespace <c>x360ce.App</c>,
	/// so its About page is made in an application domain of its own, as <see cref="V3DirectInputLayoutTest"/> makes its
	/// pages.
	/// </remarks>
	[TestClass]
	public class ThirdPartyLicenceTest
	{
		/// <summary>The programs a part can name.</summary>
		static readonly string[] Programs = { "x360ce 4", "x360ce 3", "XInput library" };

		[TestMethod, TestCategory("about")]
		[Description("Each part in the list has its licence file, unchanged since its provenance was recorded, and names only known programs; a part of the XInput library names x360ce 3, which carries the library")]
		public void Each_part_has_its_licence_file_as_recorded()
		{
			var parts = List();
			Assert.IsTrue(parts.Count > 0, "The list of licences has no parts.");
			Assert.AreEqual(parts.Count, parts.Select(x => x.Name).Distinct().Count(), "A part is listed twice.");
			foreach (var part in parts)
			{
				var what = part.Name + " (" + part.File + ")";
				Assert.IsFalse(new[] { part.Name, part.Use, part.Licence, part.Provenance }.Any(string.IsNullOrEmpty), what + " leaves a column empty.");
				StringAssert.StartsWith(part.Address, "https://", part.Name + " has no address.");
				Assert.IsTrue(part.Programs.Length > 0 && part.Programs.All(Programs.Contains),
					part.Name + " names programs other than " + string.Join(", ", Programs) + ": " + string.Join(", ", part.Programs) + ".");
				if (part.Programs.Contains("XInput library"))
					Assert.IsTrue(part.Programs.Contains("x360ce 3"), part.Name + " is in the XInput library, which x360ce 3 carries, but does not name x360ce 3.");
				var path = Path.Combine(Ui.RepoRoot.FullName, part.File);
				Assert.IsTrue(File.Exists(path), what + ": the licence file is missing.");
				var hash = GitTextSha256(File.ReadAllBytes(path));
				StringAssert.Contains(part.Provenance, hash, what + " is not the file its provenance records, whose SHA-256 is not " + hash + ".");
			}
		}

		[TestMethod, TestCategory("about"), TestCategory("ui")]
		[Description("x360ce 4 carries the list and the licence of each part that names it, and its License tab shows its own licence, then each of those parts' names, addresses, licences and texts, in the list's order")]
		public void Version_4_carries_and_shows_the_licences_it_names()
		{
			var program = typeof(AboutControl).Assembly;
			string tab = null;
			string licence = null;
			Ui.OnUiThread(() =>
			{
				using (var about = new AboutControl())
				{
					about.CreateControl();
					tab = about.Controls.Find(nameof(AboutControl.LicenseTabPage), true).Single().Text;
					licence = about.Controls.Find(nameof(AboutControl.LicenseTextBox), true).Single().Text;
				}
			});
			AssertCarriedAndShown(program.GetName().Version, program.GetManifestResourceNames(), tab, licence);
		}

		[TestMethod, TestCategory("about"), TestCategory("ui"), Timeout(180000)]
		[Description("x360ce 3 carries the list and the licence of each part that names it, and its License tab shows its own licence, then each of those parts' names, addresses, licences and texts, in the list's order")]
		public void Version_3_carries_and_shows_the_licences_it_names()
		{
			var build = V3DirectInputLayoutTest.V3Build();
			if (build == null)
				Assert.Inconclusive("Version 3 is not built for a " + (Environment.Is64BitProcess ? "64" : "32") + "-bit process.");
			var copy = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "x360ce-tests", "v3-licences-" + Guid.NewGuid().ToString("N")));
			string[] seen;
			var domain = (AppDomain)null;
			try
			{
				V3DirectInputLayoutTest.CopyV3Build(build, copy.FullName);
				domain = AppDomain.CreateDomain(nameof(ThirdPartyLicenceTest), null, new AppDomainSetup
				{
					ApplicationBase = copy.FullName,
					ConfigurationFile = Path.Combine(copy.FullName, "x360ce.exe.config"),
				});
				var about = (V3About)domain.CreateInstanceFromAndUnwrap(typeof(V3About).Assembly.Location, typeof(V3About).FullName);
				seen = about.Read();
			}
			finally
			{
				if (domain != null)
					AppDomain.Unload(domain);
				try { copy.Delete(true); }
				catch (IOException) { }
				catch (UnauthorizedAccessException) { }
			}
			Assert.IsNull(seen[0], "Version 3's About page failed to load: " + seen[0]);
			AssertCarriedAndShown(new Version(seen[1]), seen.Skip(4).ToArray(), seen[2], seen[3]);
		}

		/// <summary>The list, as the repository holds it.</summary>
		static List<ThirdPartyLicences.Part> List()
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, ThirdPartyLicences.ListPath);
			Assert.IsTrue(File.Exists(path), ThirdPartyLicences.ListPath + " is missing.");
			return ThirdPartyLicences.Parse(File.ReadAllText(path));
		}

		/// <summary>
		/// Checks that a program carries the list and each licence file of the parts that name it, and that its License tab
		/// shows its own licence first, then each of those parts in the list's order: its name and address, its licence's name
		/// on the same line, and the licence's whole text under it.
		/// </summary>
		/// <param name="version">The program's version, whose major number names it in the list.</param>
		/// <param name="resources">The names of the resources the program carries.</param>
		/// <param name="tab">The License tab's title.</param>
		/// <param name="licence">The License tab's text.</param>
		static void AssertCarriedAndShown(Version version, string[] resources, string tab, string licence)
		{
			var program = "x360ce " + version.Major;
			// The tab names the program, not the process that hosts it: in a test, that is the test host.
			Assert.AreEqual("X360 Controller Emulator " + version.ToString(2) + " License", tab, program + "'s License tab is not named after it.");
			var own = ControlsHelper.NormalizeLineBreaks(File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v" + version.Major, "Documents", "License.txt")));
			Assert.IsTrue(licence.StartsWith(own, StringComparison.Ordinal), program + "'s own licence is not shown first, whole.");
			Assert.IsTrue(resources.Any(x => x.EndsWith("." + ThirdPartyLicences.ListPath.Replace('/', '.'), StringComparison.Ordinal)),
				program + " does not carry " + ThirdPartyLicences.ListPath + ".");
			var parts = List().Where(x => x.Programs.Contains(program)).ToList();
			Assert.IsTrue(parts.Count > 0, "No part in the list names " + program + ".");
			var at = own.Length;
			foreach (var part in parts)
			{
				Assert.IsTrue(resources.Any(x => x.EndsWith("." + part.File.Replace('/', '.'), StringComparison.Ordinal)),
					program + " does not carry " + part.Name + "'s licence, " + part.File + ".");
				var named = part.Name + " (" + part.Address + ")";
				var name = licence.IndexOf(named, at, StringComparison.Ordinal);
				Assert.IsTrue(name >= at, program + "'s License tab does not name " + named + " after the parts listed before it.");
				var lineEnd = licence.IndexOf("\r\n", name, StringComparison.Ordinal);
				var heading = licence.IndexOf(", " + part.Licence + ":", name, StringComparison.Ordinal);
				Assert.IsTrue(heading > name && heading < lineEnd, program + "'s License tab does not name " + part.Licence + " beside " + part.Name + ".");
				var text = ControlsHelper.NormalizeLineBreaks(File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, part.File)));
				Assert.IsTrue(licence.IndexOf(text, lineEnd, StringComparison.Ordinal) > 0, program + "'s License tab does not show " + part.Name + "'s licence in full under its name.");
				at = name + named.Length;
			}
		}

		/// <summary>The SHA-256 of a text file as Git stores it and its project publishes it: with Unix line endings.</summary>
		static string GitTextSha256(byte[] bytes)
		{
			var text = new List<byte>(bytes.Length);
			for (var i = 0; i < bytes.Length; i++)
				if (bytes[i] != 0x0D || i + 1 >= bytes.Length || bytes[i + 1] != 0x0A)
					text.Add(bytes[i]);
			using (var sha = SHA256.Create())
				return string.Concat(sha.ComputeHash(text.ToArray()).Select(x => x.ToString("x2")));
		}

		/// <summary>Runs in version 3's domain: makes its About page as the program does and reads what it shows.</summary>
		public sealed class V3About : MarshalByRefObject
		{
			/// <summary>A failure or null, the program's version, the License tab's title and text, then the names of the program's resources.</summary>
			public string[] Read()
			{
				string[] seen = null;
				Exception failure = null;
				var ui = new Thread(() =>
				{
					try { seen = Load(); }
					catch (Exception ex) { failure = ex; }
				});
				ui.SetApartmentState(ApartmentState.STA);
				ui.Start();
				if (!ui.Join(TimeSpan.FromSeconds(120)))
					return new[] { "The About page was not made within two minutes." };
				return failure != null ? new[] { failure.ToString() } : seen;
			}

			static string[] Load()
			{
				var program = Assembly.Load("x360ce");
				var type = program.GetType("x360ce.App.Controls.AboutControl", true);
				using (var about = (UserControl)Activator.CreateInstance(type))
				{
					about.CreateControl();
					var tab = about.Controls.Find("LicenseTabPage", true).Single().Text;
					var licence = about.Controls.Find("LicenseTextBox", true).Single().Text;
					return new[] { null, program.GetName().Version.ToString(), tab, licence }.Concat(program.GetManifestResourceNames()).ToArray();
				}
			}
		}
	}
}
