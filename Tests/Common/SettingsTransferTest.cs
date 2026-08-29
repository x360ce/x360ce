// @under-test: Engine/Common/SettingsTransfer.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Carrying settings from one folder to another must never be how somebody loses
	/// them.
	/// </summary>
	/// <remarks>
	/// Deleting is the one step that cannot be undone, so what is tested here is
	/// mostly when it does NOT happen: a copy that did not arrive, a file that is
	/// already there and different, a folder that is not there at all. A user
	/// reported settings that could not be saved at all, and moving them is the way
	/// out of that, so the way out has to be safer than the problem.
	/// </remarks>
	[TestClass]
	public class SettingsTransferTest
	{

		private string _root;
		private string _source;
		private string _target;

		[TestInitialize]
		public void Setup()
		{
			_root = Path.Combine(Path.GetTempPath(), "x360ce-transfer-" + Guid.NewGuid().ToString("N"));
			_source = Path.Combine(_root, "from", "Settings");
			_target = Path.Combine(_root, "to", "Settings");
			Directory.CreateDirectory(_source);
		}

		[TestCleanup]
		public void Cleanup()
		{
			// A junction has to be taken out on its own, before the tree it sits in.
			// Deleting it as part of a recursive sweep is refused, and following it
			// would reach the folder it points at rather than the link.
			foreach (var folder in Directory.Exists(_root)
				? Directory.GetDirectories(_root, "*", SearchOption.AllDirectories)
				: new string[0])
			{
				var info = new DirectoryInfo(folder);
				if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
					info.Delete();
			}
			try { Directory.Delete(_root, true); }
			catch (IOException) { /* left behind rather than failing a passing test */ }
		}

		private void Given(string name, string content)
			=> File.WriteAllText(Path.Combine(_source, name), content);

		private static string[] Names(string folder)
			=> Directory.Exists(folder)
				? Directory.GetFiles(folder, "*.xml").Select(Path.GetFileName).OrderBy(x => x).ToArray()
				: new string[0];

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A copy leaves the originals where they are")]
		public void A_copy_leaves_the_originals_where_they_are()
		{
			Given("x360ce.Options.xml", "<options />");
			Given("x360ce.UserGames.xml", "<games />");

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Copy);

			Assert.IsTrue(result.Success, result.Problem);
			Assert.AreEqual(2, result.Copied);
			Assert.AreEqual(2, result.Verified);
			Assert.AreEqual(0, result.Removed, "A copy removed something.");
			CollectionAssert.AreEqual(Names(_source), Names(_target), "The two folders do not match.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A move removes the originals only after every copy is verified")]
		public void A_move_removes_the_originals_only_after_every_copy_is_verified()
		{
			Given("x360ce.Options.xml", "<options />");
			Given("x360ce.UserGames.xml", "<games />");

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Move);

			Assert.IsTrue(result.Success, result.Problem);
			Assert.AreEqual(2, result.Verified);
			Assert.AreEqual(2, result.Removed);
			Assert.AreEqual(0, Names(_source).Length, "The originals are still there.");
			CollectionAssert.AreEqual(
				new[] { "x360ce.Options.xml", "x360ce.UserGames.xml" }, Names(_target));
			Assert.AreEqual("<games />", File.ReadAllText(Path.Combine(_target, "x360ce.UserGames.xml")),
				"What arrived is not what was sent.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A different file of the same name stops the transfer and changes nothing")]
		public void A_different_file_of_the_same_name_stops_the_transfer_and_changes_nothing()
		{
			// Somebody has used the chosen folder before, with different settings.
			// Overwriting theirs, or deleting these, would both be somebody's loss.
			Given("x360ce.Options.xml", "<options mine=\"true\" />");
			Directory.CreateDirectory(_target);
			File.WriteAllText(Path.Combine(_target, "x360ce.Options.xml"), "<options theirs=\"true\" />");

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Move);

			Assert.IsFalse(result.Success, "The transfer went ahead over somebody else's file.");
			StringAssert.Contains(result.Problem, "x360ce.Options.xml");
			Assert.AreEqual(0, result.Removed, "It deleted originals despite refusing.");
			Assert.AreEqual("<options mine=\"true\" />",
				File.ReadAllText(Path.Combine(_source, "x360ce.Options.xml")), "The original changed.");
			Assert.AreEqual("<options theirs=\"true\" />",
				File.ReadAllText(Path.Combine(_target, "x360ce.Options.xml")), "The other file was overwritten.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("An identical file already in place counts as arrived")]
		public void An_identical_file_already_in_place_counts_as_arrived()
		{
			// Re-running a transfer that was interrupted must finish it, not refuse it.
			Given("x360ce.Options.xml", "<options />");
			Directory.CreateDirectory(_target);
			File.WriteAllText(Path.Combine(_target, "x360ce.Options.xml"), "<options />");

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Move);

			Assert.IsTrue(result.Success, result.Problem);
			Assert.AreEqual(0, result.Copied, "It copied a file that was already there.");
			Assert.AreEqual(1, result.Verified);
			Assert.AreEqual(1, result.Removed);
			Assert.AreEqual(0, Names(_source).Length);
		}

		[TestMethod, TestCategory("settings")]
		[Description("Nothing to carry is not a failure")]
		public void Nothing_to_carry_is_not_a_failure()
		{
			Directory.Delete(_source, true);

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Move);

			Assert.IsTrue(result.Success, result.Problem);
			Assert.AreEqual(0, result.Copied);
			Assert.AreEqual(0, result.Removed);
		}

		[TestMethod, TestCategory("settings")]
		[Description("Carrying a folder to itself does nothing")]
		public void Carrying_a_folder_to_itself_does_nothing()
		{
			Given("x360ce.Options.xml", "<options />");

			var result = SettingsTransfer.Run(_source, _source, SettingsTransferMode.Move);

			Assert.IsTrue(result.Success, result.Problem);
			Assert.AreEqual(0, result.Removed, "It emptied the folder it was asked to fill.");
			Assert.AreEqual(1, Names(_source).Length);
		}

		[TestMethod, TestCategory("settings")]
		[Description("A link leaves the old path reading the new folder")]
		public void A_link_leaves_the_old_path_reading_the_new_folder()
		{
			// The reason to link at all: a version that only knows the old path keeps
			// working, and there is still only one copy of the settings.
			Given("x360ce.Options.xml", "<options />");

			var result = SettingsTransfer.Run(_source, _target, SettingsTransferMode.Link);

			if (!result.Success)
				Assert.Inconclusive("This machine would not make a junction: " + result.Problem);
			Assert.AreEqual(1, result.Verified);
			Assert.IsTrue(File.Exists(Path.Combine(_target, "x360ce.Options.xml")),
				"The file is not in the folder that now holds it.");
			Assert.IsTrue(File.Exists(Path.Combine(_source, "x360ce.Options.xml")),
				"The old path does not reach the settings, so an older version would see none.");
			// One copy, not two: writing through the old path is writing the new file.
			File.WriteAllText(Path.Combine(_source, "x360ce.Options.xml"), "<options changed=\"true\" />");
			Assert.AreEqual("<options changed=\"true\" />",
				File.ReadAllText(Path.Combine(_target, "x360ce.Options.xml")),
				"The two paths are separate copies rather than one folder.");
		}

	}
}
