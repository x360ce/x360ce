// @under-test: App.v3/x360ce.App.v3.csproj, App.v4/x360ce.App.v4.csproj
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

		/// <summary>
		/// The names the connection strings in the engine's configuration ask for, each of which
		/// must be embedded under exactly that name or the data model cannot open.
		/// </summary>
		static readonly string[] ModelMetadata =
		{
			"Data.x360ceModel.csdl", "Data.x360ceModel.ssdl", "Data.x360ceModel.msl",
			"JocysCom.Security.Data.SecurityModel.csdl", "JocysCom.Security.Data.SecurityModel.ssdl", "JocysCom.Security.Data.SecurityModel.msl",
		};

		[TestMethod, TestCategory("build"), TestCategory("critical")]
		[Description("The engine embeds its data-model metadata under the names its connection strings use")]
		public void Engine_embeds_the_model_metadata_by_name()
		{
			var engine = typeof(x360ce.Engine.EngineHelper).Assembly;
			var names = engine.GetManifestResourceNames();
			foreach (var name in ModelMetadata)
				Assert.IsTrue(names.Contains(name), "Missing embedded resource: " + name + ". Present: " + string.Join(", ", names.Where(x => x.EndsWith("dl", StringComparison.Ordinal))));
			// Each one is the model section it claims to be, not an empty placeholder.
			foreach (var name in ModelMetadata)
				using (var stream = engine.GetManifestResourceStream(name))
					Assert.IsTrue(stream != null && stream.Length > 1000, name + " is empty.");
		}

	}
}
