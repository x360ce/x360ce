// @under-test: App.v4/x360ce.App.v4.csproj, Engine/x360ce.Engine.csproj
// @area: migration   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace x360ce.Tests
{
	/// <summary>
	/// App.v4 no longer uses WPF. Initialising it committed around 90 MB that was never returned
	/// while the process ran, so nothing here may bring it back: this test fails on any XAML
	/// appearing anywhere in the repository.
	/// </summary>
	[TestClass]
	public class WpfSurfaceTest
	{

		/// <summary>
		/// Every XAML file still expected to exist, repository relative and forward slashed.
		/// </summary>
		/// <remarks>
		/// Empty, and meant to stay that way. Adding an entry here means reintroducing WPF, which
		/// costs about 90 MB for the life of the process.
		/// </remarks>
		static readonly string[] Expected = new string[0];

		static string[] ActualXamlFiles()
		{
			var root = Ui.RepoRoot.FullName;
			return Directory.GetFiles(root, "*.xaml", SearchOption.AllDirectories)
				.Where(x => !x.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
				.Where(x => !x.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
				.Select(x => x.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/'))
				.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}

		[TestMethod, TestCategory("migration"), TestCategory("smoke")]
		[Description("No XAML exists anywhere, so WPF cannot be initialised")]
		public void No_xaml_remains_anywhere_in_the_solution()
		{
			var actual = ActualXamlFiles();
			var unexpected = actual.Except(Expected, StringComparer.OrdinalIgnoreCase).ToArray();
			var missing = Expected.Except(actual, StringComparer.OrdinalIgnoreCase).ToArray();

			Assert.AreEqual(0, unexpected.Length,
				"XAML that is not on the expected list:" + Environment.NewLine + "  " +
				string.Join(Environment.NewLine + "  ", unexpected) + Environment.NewLine +
				"App.v4 was moved off WPF. Adding XAML back costs about 90 MB for the life of the " +
				"process. Delete it, or add it to Expected with a reason.");

			Assert.AreEqual(0, missing.Length,
				"Expected XAML is gone:" + Environment.NewLine + "  " +
				string.Join(Environment.NewLine + "  ", missing) + Environment.NewLine +
				"Remove it from Expected.");
		}

		[TestMethod, TestCategory("migration"), TestCategory("smoke")]
		[Description("No project still lists a XAML file that no longer exists")]
		public void No_project_references_a_deleted_xaml()
		{
			var root = Ui.RepoRoot.FullName;
			foreach (var project in Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
			{
				var folder = Path.GetDirectoryName(project);
				var text = File.ReadAllText(project);
				foreach (Match m in Regex.Matches(text, @"Include=""([^""]+\.xaml(?:\.cs)?)"""))
				{
					var relative = m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
					var full = Path.Combine(folder, relative);
					Assert.IsTrue(File.Exists(full),
						Path.GetFileName(project) + " still lists " + m.Groups[1].Value +
						", which is not on disk. Removing a control means removing its project entry too.");
				}
			}
		}

	}
}
