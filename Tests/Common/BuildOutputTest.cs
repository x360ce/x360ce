// @under-test: App.v3/x360ce.App.v3.csproj, App.v4/x360ce.App.v4.csproj, Engine/x360ce.Engine.csproj
// @area: build   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The files embedded into the applications come from the build rather than from source
	/// control, so the path each one is taken from decides what ends up inside a signed
	/// executable. A path naming one configuration outright is served to every configuration:
	/// a Debug build then either stops because the Release output is not there, or, once an
	/// earlier Release build has left it on disk, silently embeds Release binaries while the
	/// pre-build event copies the Debug ones next to the executable.
	/// </summary>
	[TestClass]
	public class BuildOutputTest
	{

		static readonly string[] Applications =
		{
			"App.v3/x360ce.App.v3.csproj",
			"App.v4/x360ce.App.v4.csproj",
		};

		/// <summary>Configuration names that must never be written into a source path.</summary>
		static readonly string[] ConfigurationNames = { "Debug", "Release" };

		[TestMethod, TestCategory("build"), TestCategory("smoke")]
		[Description("Embedded build output is taken from the configuration being built")]
		public void Embedded_files_come_from_the_configuration_being_built()
		{
			var checkedPaths = 0;
			foreach (var relative in Applications)
			{
				var path = Path.Combine(Ui.RepoRoot.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
				Assert.IsTrue(File.Exists(path), relative + " not found; the test no longer covers it.");
				foreach (var include in LiteralIncludes(path))
				{
					checkedPaths++;
					foreach (var name in ConfigurationNames)
					{
						Assert.IsFalse(NamesFolder(include, name),
							relative + " embeds " + include + ", which names the " + name + " folder. " +
							"Use $(Configuration) so every configuration embeds its own build output.");
					}
					Assert.IsTrue(include.IndexOf("$(Configuration)", StringComparison.Ordinal) >= 0,
						relative + " embeds " + include + " from a fixed folder. " +
						"A path into the build output has to carry $(Configuration).");
				}
			}
			Assert.IsTrue(checkedPaths > 0,
				"No embedded build output was found in either application, so this test proves nothing. " +
				"Either the item name changed or the files are no longer embedded.");
		}

		/// <summary>
		/// The Include values of GeneratedResource items that name a file on disk. Items taking
		/// their value from another item list, such as @(ReferencePath), carry no path of their
		/// own and are already resolved by the build.
		/// </summary>
		static IEnumerable<string> LiteralIncludes(string projectPath)
		{
			// Matched by local name: an SDK-style project file carries no namespace, the old
			// format carried the MSBuild one, and the items mean the same in both.
			return XDocument.Load(projectPath)
				.Descendants()
				.Where(x => x.Name.LocalName == "GeneratedResource")
				.Select(x => (string)x.Attribute("Include"))
				.Where(x => !string.IsNullOrEmpty(x) && x.IndexOf("@(", StringComparison.Ordinal) < 0);
		}

		/// <summary>True when the path contains the named folder as a whole segment.</summary>
		static bool NamesFolder(string path, string folder)
		{
			return path.Replace('/', '\\')
				.Split('\\')
				.Any(segment => segment.Equals(folder, StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod, TestCategory("build"), TestCategory("critical")]
		[Description("The engine and the programs carry no data-model metadata and no Entity Framework")]
		public void The_engine_and_the_programs_carry_no_Entity_Framework()
		{
			// The data classes are generated from the database (Engine\Data\Generate-Models.ps1). The context that
			// queries it, and Entity Framework with it, belong to the web site alone.
			var engine = typeof(x360ce.Engine.EngineHelper).Assembly;
			var metadata = engine.GetManifestResourceNames()
				.Where(x => x.EndsWith(".csdl", StringComparison.Ordinal) || x.EndsWith(".ssdl", StringComparison.Ordinal) || x.EndsWith(".msl", StringComparison.Ordinal))
				.ToArray();
			Assert.AreEqual(0, metadata.Length, "The engine embeds model metadata: " + string.Join(", ", metadata));
			foreach (var assembly in new[] { engine, typeof(x360ce.App.MainForm).Assembly })
			{
				var references = assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
				foreach (var name in new[] { "EntityFramework", "EntityFramework.SqlServer" })
					Assert.IsFalse(references.Contains(name), assembly.GetName().Name + " references " + name + ".");
			}
		}

	}
}
