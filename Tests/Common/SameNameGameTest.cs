// @under-test: App.v4/Common/SettingsManager.cs, App.v4/Controls/PadTabPages/GamesControl.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Two games with one file name: kept as one entry unless the person asks for two.
	/// </summary>
	/// <remarks>
	/// Every copy of a program shares one configuration, keyed by file name; that rule serves the
	/// program's own entry, the scanner and a game that was moved. The mods of one game all carry
	/// its file, and adding a second one replaced the first. The question is asked only when the
	/// listed file is still there in another folder; a missing file is the same game moved.
	/// </remarks>
	[TestClass]
	public class SameNameGameTest
	{
		string folderA;
		string folderB;
		UserGame listed;

		[TestInitialize]
		public void Before()
		{
			var root = Path.Combine(Path.GetTempPath(), "x360ce-games-" + Guid.NewGuid().ToString("N"));
			folderA = Path.Combine(root, "Half-Life 2");
			folderB = Path.Combine(root, "SMOD");
			Directory.CreateDirectory(folderA);
			Directory.CreateDirectory(folderB);
			// Real program files, because the scanner reads their headers.
			var source = typeof(XInputMaskScanner).Assembly.Location;
			File.Copy(source, Path.Combine(folderA, "hl2.exe"));
			File.Copy(source, Path.Combine(folderB, "hl2.exe"));
			listed = new UserGame { GameId = Guid.NewGuid(), FileName = "hl2.exe", FileProductName = "Half-Life 2", FullPath = Path.Combine(folderA, "hl2.exe") };
			SettingsManager.UserGames.Items.Add(listed);
		}

		[TestCleanup]
		public void After()
		{
			SettingsManager.UserGames.Items.Remove(listed);
			Directory.Delete(Path.GetDirectoryName(folderA), true);
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		public void The_same_name_in_another_folder_is_a_question_while_the_listed_file_is_still_there()
		{
			Assert.AreSame(listed, SettingsManager.OtherGameWithSameName(Path.Combine(folderB, "hl2.exe")));
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		public void The_same_name_in_another_folder_is_the_moved_game_once_the_listed_file_is_gone()
		{
			File.Delete(listed.FullPath);
			Assert.IsNull(SettingsManager.OtherGameWithSameName(Path.Combine(folderB, "hl2.exe")), "A moved game was taken for a second one.");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		public void The_same_file_again_is_no_question()
		{
			Assert.IsNull(SettingsManager.OtherGameWithSameName(listed.FullPath));
			Assert.IsNull(SettingsManager.OtherGameWithSameName(Path.Combine(folderA, "HL2.EXE")), "Case alone made a second game.");
		}

		/// <summary>What the scan of one file reports, given the games it is shown.</summary>
		static XInputMaskScannerState ScanOf(string file, params UserGame[] shown)
		{
			var scanner = new XInputMaskScanner();
			var outcome = XInputMaskScannerState.None;
			scanner.Progress += (sender, e) =>
			{
				if (e.State == XInputMaskScannerState.GameFound || e.State == XInputMaskScannerState.GameUpdated)
					outcome = e.State;
			};
			scanner.ScanGames(new[] { Path.GetDirectoryName(file) }, new List<UserGame>(shown), new List<Program>(), Path.GetFileName(file));
			return outcome;
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		public void Use_existing_updates_the_listed_game_and_add_separate_makes_a_new_one()
		{
			var second = Path.Combine(folderB, "hl2.exe");
			// Shown the listed game, the scanner updates it: one entry, as always.
			Assert.AreEqual(XInputMaskScannerState.GameUpdated, ScanOf(second, listed));
			// Shown a list without it, as "Add Separate" does, the scanner finds a new game.
			Assert.AreEqual(XInputMaskScannerState.GameFound, ScanOf(second));
		}

		[TestMethod, TestCategory("games")]
		public void Two_entries_under_one_name_are_told_apart_by_folder_and_one_alone_is_not()
		{
			Assert.AreEqual("hl2.exe - Half-Life 2", SettingsManager.DisplayNameInList(listed), "A game with no twin was renamed.");
			var second = new UserGame { GameId = Guid.NewGuid(), FileName = "hl2.exe", FileProductName = "Half-Life 2", FullPath = Path.Combine(folderB, "hl2.exe") };
			SettingsManager.UserGames.Items.Add(second);
			try
			{
				Assert.AreEqual("hl2.exe - Half-Life 2 (Half-Life 2)", SettingsManager.DisplayNameInList(listed));
				Assert.AreEqual("hl2.exe - Half-Life 2 (SMOD)", SettingsManager.DisplayNameInList(second));
			}
			finally
			{
				SettingsManager.UserGames.Items.Remove(second);
			}
		}
	}
}
