// @under-test: .github/scripts/Publish-Wiki.ps1, docs
// @area: documents   @layer: integration
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace x360ce.Tests
{
	/// <summary>
	/// The wiki is written from docs by the script the publish workflow runs, so the two are held to one contract here:
	/// every page of docs is published with its links working in the wiki, and a link to a page, heading or file that
	/// docs does not hold stops the publish instead of reaching readers.
	/// </summary>
	[TestClass]
	public class PublishWikiScriptTest
	{
		const string Commit = "0123456789abcdef0123456789abcdef01234567";

		static string Script
		{
			get { return Path.Combine(Ui.RepoRoot.FullName, ".github", "scripts", "Publish-Wiki.ps1"); }
		}

		static string NewFolder()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-wiki-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			return folder;
		}

		static int Publish(string docs, string wiki, out string output)
		{
			return Ui.RunScript(Script, "-Docs \"" + docs + "\" -Wiki \"" + wiki + "\" -Commit " + Commit, out output);
		}

		static void Write(string folder, string name, string text)
		{
			var path = Path.Combine(folder, name);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text);
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("The docs folder publishes with no broken link: every page, the sidebar in its order, the footer and the old page names")]
		public void The_docs_publish_with_no_broken_link()
		{
			var wiki = NewFolder();
			try
			{
				var docs = Path.Combine(Ui.RepoRoot.FullName, "docs");
				string output;
				Assert.AreEqual(0, Publish(docs, wiki, out output), output);
				foreach (var page in Directory.GetFiles(docs, "*.md"))
					Assert.IsTrue(File.Exists(Path.Combine(wiki, Path.GetFileName(page))), Path.GetFileName(page) + " was not published.");
				var sidebar = File.ReadAllLines(Path.Combine(wiki, "_Sidebar.md"));
				Assert.AreEqual("- [Home](Home)", sidebar[0], "The sidebar does not start where docs/.order does.");
				var footer = File.ReadAllText(Path.Combine(wiki, "_Footer.md"));
				StringAssert.Contains(footer, "https://github.com/x360ce/x360ce/tree/master/docs", "The footer does not say the wiki's source is docs.");
				StringAssert.Contains(footer, Commit, "The footer does not name the commit published.");
				foreach (var line in File.ReadAllLines(Path.Combine(docs, ".moved")).Where(x => x.Trim().Length > 0))
					Assert.IsTrue(File.Exists(Path.Combine(wiki, line.Split('=')[0].Trim() + ".md")), "The old page " + line + " was not written.");
				Assert.IsFalse(Directory.Exists(Path.Combine(wiki, ".attachments")), "Files were copied into the wiki instead of linked.");
			}
			finally
			{
				Directory.Delete(wiki, true);
			}
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("A link to another page goes to its wiki page, a file to the repository and a picture to its raw file, all at the commit published")]
		public void Links_work_in_the_wiki()
		{
			var docs = NewFolder();
			var wiki = NewFolder();
			try
			{
				Write(docs, "Home.md", "# Home\n\nSee [the other](Other.md#a-heading), [a preset](.attachments/a.xml), ![a picture](.attachments/a.png), " +
					"[the site](https://www.x360ce.com) and [below](#home).\n");
				Write(docs, "Other.md", "# Other\n\n## A Heading\n");
				Write(docs, ".attachments/a.xml", "<a />");
				Write(docs, ".attachments/a.png", "");
				Write(docs, "plans/Unshipped.md", "# Unshipped\n");
				string output;
				Assert.AreEqual(0, Publish(docs, wiki, out output), output);
				var home = File.ReadAllText(Path.Combine(wiki, "Home.md"));
				StringAssert.Contains(home, "[the other](Other#a-heading)", "A page link does not go to the wiki page.");
				StringAssert.Contains(home, "[a preset](https://github.com/x360ce/x360ce/blob/" + Commit + "/docs/.attachments/a.xml)", "A file link does not go to the repository.");
				StringAssert.Contains(home, "![a picture](https://raw.githubusercontent.com/x360ce/x360ce/" + Commit + "/docs/.attachments/a.png)", "A picture is not served from the repository.");
				StringAssert.Contains(home, "[the site](https://www.x360ce.com) and [below](#home)", "An address or an anchor was changed.");
				Assert.IsFalse(File.Exists(Path.Combine(wiki, "Unshipped.md")), "A page in a sub-folder of docs was published.");
			}
			finally
			{
				Directory.Delete(docs, true);
				Directory.Delete(wiki, true);
			}
		}

		[TestMethod, TestCategory("documents"), TestCategory("critical")]
		[Description("A link to a page, heading or file docs does not hold stops the publish and names it; the wiki keeps its git folder and loses pages docs dropped")]
		public void A_broken_link_stops_the_publish()
		{
			var docs = NewFolder();
			var wiki = NewFolder();
			try
			{
				Write(docs, "Home.md", "# Home\n\n[gone](Gone.md), [no heading](Home.md#nowhere), ![no picture](.attachments/none.png)\n");
				Write(docs, ".moved", "Old-Page = Missing\n");
				Write(wiki, ".git/config", "kept");
				Write(wiki, "Dropped.md", "# Dropped\n");
				string output;
				Assert.AreNotEqual(0, Publish(docs, wiki, out output), "The publish went on past broken links.");
				foreach (var named in new[] { "Gone", "Home#nowhere", ".attachments/none.png", "Old-Page" })
					StringAssert.Contains(output, named, "The broken link to " + named + " was not named.");
				Assert.IsTrue(File.Exists(Path.Combine(wiki, ".git", "config")), "The wiki's git folder was removed.");
				Assert.IsFalse(File.Exists(Path.Combine(wiki, "Dropped.md")), "A page docs no longer holds was left in the wiki.");
			}
			finally
			{
				Directory.Delete(docs, true);
				Directory.Delete(wiki, true);
			}
		}
	}
}
