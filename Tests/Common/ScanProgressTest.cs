// @under-test: App.v4/Controls/PadTabPages/GamesControl.cs, Engine/Common/XInputMaskScanner.cs
// @area: games   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using x360ce.App;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>What the scan reports while it runs, including in a folder that has nothing to scan.</summary>
	/// <remarks>
	/// The scan runs on a worker and posts its reports to the games list, so it never waits for the window.
	/// A report the list fails to show is reported on the interface thread, where it failed, and never
	/// thrown on the worker, a thread-pool thread, where an unhandled exception ends the program.
	/// </remarks>
	[TestClass]
	public class ScanProgressTest
	{
		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A folder with no programs reports its update without an index into an empty list")]
		public void An_empty_folder_reports_without_failing()
		{
			var text = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 1,
				State = XInputMaskScannerState.FileUpdate,
				Files = new List<FileInfo>(),
				FileIndex = 0,
				Message = "Scan file 0 of 0. Please wait...",
			});
			StringAssert.Contains(text, "Scan file 0 of 0");
			Assert.IsFalse(text.Contains("Current File"), "There is no current file in an empty folder.");
			var folders = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 0,
				State = XInputMaskScannerState.DirectoryUpdate,
				Directories = new List<DirectoryInfo>(),
				DirectoryIndex = 0,
				Message = "Searching",
			});
			StringAssert.Contains(folders, "Skipped = 0");
			Assert.IsFalse(folders.Contains("Current Folder"));
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A file being scanned is named with its size")]
		public void The_current_file_is_named()
		{
			var file = new FileInfo(typeof(XInputMaskScanner).Assembly.Location);
			var text = GamesGridUserControl.ProgressText(new XInputMaskScannerEventArgs
			{
				Level = 1,
				State = XInputMaskScannerState.FileUpdate,
				Files = new List<FileInfo> { file },
				FileIndex = 0,
				Message = "Scan file 1 of 1. Please wait...",
			});
			StringAssert.Contains(text, "Current File");
			StringAssert.Contains(text, file.FullName);
		}

		/// <summary>The list's own handler for the scan's reports, as the scan attaches it.</summary>
		static EventHandler<XInputMaskScannerEventArgs> Handler(GamesGridUserControl panel)
		{
			var method = typeof(GamesGridUserControl).GetMethod("Scanner_Progress", BindingFlags.Instance | BindingFlags.NonPublic);
			return (EventHandler<XInputMaskScannerEventArgs>)Delegate.CreateDelegate(
				typeof(EventHandler<XInputMaskScannerEventArgs>), panel, method);
		}

		/// <summary>Runs this interface thread's messages until the worker is done, for a report the worker waits on.</summary>
		static void PumpUntil(Task work)
		{
			var deadline = DateTime.UtcNow.AddSeconds(10);
			while (!work.IsCompleted && DateTime.UtcNow < deadline)
				Application.DoEvents();
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("The scan reads a game folder without waiting for the window, and the window then shows how far it got")]
		public void The_scan_does_not_wait_for_the_window()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				// A program file in a game folder: what the reported scan was reading (GetMasks, Level 1).
				File.Copy(typeof(XInputMaskScanner).Assembly.Location, Path.Combine(folder, "game.exe"));
				Ui.OnUiThreadWatched(() =>
				{
					using (var form = new Form { ShowInTaskbar = false })
					using (var panel = new GamesGridUserControl())
					{
						form.Controls.Add(panel);
						form.Show();
						var scanner = new XInputMaskScanner();
						scanner.Progress += Handler(panel);
						// This thread answers nothing while the worker reads, as a window busy drawing does not.
						var scan = Task.Run(() => scanner.GetMasks(folder, SearchOption.TopDirectoryOnly, false));
						SpinWait.SpinUntil(() => scan.IsCompleted, TimeSpan.FromSeconds(10));
						Assert.IsTrue(scan.IsCompleted, "The scan waited for the window to answer; it runs only as fast as the window draws.");
						Assert.IsFalse(scan.IsFaulted, "Reporting progress failed on the worker, which ends the program: " + scan.Exception);
						var label = panel.Controls.Find("ScanProgressLevel1Label", true).Single();
						var deadline = DateTime.UtcNow.AddSeconds(10);
						while (!label.Text.Contains("Scan file 1 of 1") && DateTime.UtcNow < deadline)
							Application.DoEvents();
						StringAssert.Contains(label.Text, "Scan file 1 of 1", "The progress the scan reported never reached the window.");
					}
				}, "Showing a report");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A game found after the games list has closed stops the scan instead of failing on the worker")]
		public void A_game_found_after_the_list_closed_stops_the_scan()
		{
			Ui.OnUiThreadWatched(() =>
			{
				var panel = new GamesGridUserControl();
				var handler = Handler(panel);
				panel.Dispose();
				var scanner = new XInputMaskScanner();
				var file = new FileInfo(typeof(XInputMaskScanner).Assembly.Location);
				var found = new XInputMaskScannerEventArgs
				{
					State = XInputMaskScannerState.GameFound,
					Game = new UserGame { FileName = file.Name, FullPath = file.FullName },
					GameFileInfo = file,
				};
				var scan = Task.Run(() => handler(scanner, found));
				// A game found is applied on this thread before the scan goes on.
				PumpUntil(scan);
				Assert.IsTrue(scan.IsCompleted, "The scan never came back from reporting a game.");
				Assert.IsFalse(scan.IsFaulted, "Reporting a game after the list closed failed on the worker, which ends the program: " + scan.Exception);
				Assert.IsTrue(scanner.IsStopping, "The list has closed, yet the scan goes on.");
			}, "Showing a report");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A listed game added again by hand, with no default settings and no product name, moves to where it was found")]
		public void A_game_with_no_default_settings_is_updated()
		{
			Ui.OnUiThreadWatched(() =>
			{
				using (var form = new Form { ShowInTaskbar = false })
				using (var panel = new GamesGridUserControl())
				{
					form.Controls.Add(panel);
					form.Show();
					var file = new FileInfo(typeof(XInputMaskScanner).Assembly.Location);
					var game = new UserGame { FileName = file.Name, FileProductName = "", FullPath = Path.Combine(@"C:\Moved", file.Name) };
					// Added by hand: no default settings name the file, so the scan reports no program.
					var updated = new XInputMaskScannerEventArgs
					{
						State = XInputMaskScannerState.GameUpdated,
						Game = game,
						GameFileInfo = file,
						Program = null,
					};
					var scan = Task.Run(() => Handler(panel)(new XInputMaskScanner(), updated));
					PumpUntil(scan);
					Assert.IsTrue(scan.IsCompleted, "The scan never came back from updating a game.");
					Assert.IsFalse(scan.IsFaulted, "Updating a game with no default settings failed: " + scan.Exception);
					Assert.AreEqual(file.FullName, game.FullPath, "The game was not moved to where it was found.");
				}
			}, "Showing a report");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A scan report is posted to the window and runs none of the window's other messages")]
		public void A_report_is_posted_and_runs_no_other_messages()
		{
			var source = File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Controls", "PadTabPages", "GamesControl.cs"));
			var start = source.IndexOf("void Scanner_Progress(");
			var end = source.IndexOf("public static string ProgressText(");
			Assert.IsTrue(start > 0 && end > start, "The scan's handlers moved; update this test.");
			var handlers = source.Substring(start, end - start);
			Assert.IsFalse(handlers.Contains("Application.DoEvents()"), "A report runs every waiting message of the window from inside itself.");
			Assert.IsFalse(Regex.IsMatch(handlers, @"(?<!Begin)Invoke\("), "A report is sent to the window; a failure there is thrown again on the worker.");
			StringAssert.Contains(handlers, "ControlsHelper.BeginInvoke(() => ShowScanProgress(scanner, e))");
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A closed list stops the Step 2 loop after at most one game report, not after every listed program")]
		public void A_closed_list_stops_the_scan_after_one_program()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				var source = typeof(XInputMaskScanner).Assembly.Location;
				File.Copy(source, Path.Combine(folder, "game1.exe"));
				File.Copy(source, Path.Combine(folder, "game2.exe"));
				Ui.OnUiThreadWatched(() =>
				{
					var panel = new GamesGridUserControl();
					var handler = Handler(panel);
					panel.Dispose();
					var scanner = new XInputMaskScanner();
					var reportCount = 0;
					scanner.Progress += (s, ea) =>
					{
						if (ea.State == XInputMaskScannerState.GameFound || ea.State == XInputMaskScannerState.GameUpdated)
							Interlocked.Increment(ref reportCount);
						handler(s, ea);
					};
					var programs = new List<Program>
					{
						new Program { FileName = "game1.exe" },
						new Program { FileName = "game2.exe" },
					};
					var scan = Task.Run(() => scanner.ScanGames(new[] { folder }, new List<UserGame>(), programs));
					PumpUntil(scan);
					Assert.IsTrue(scan.IsCompleted, "The scan never came back after the list closed.");
					Assert.IsFalse(scan.IsFaulted, "The scan failed after the list closed: " + scan.Exception);
					Assert.IsTrue(reportCount <= 1, "A closed list should stop the Step 2 loop after at most one game report; found " + reportCount + ".");
				}, "Showing a report");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A game found while another listed game has no path does not fail")]
		public void A_game_is_found_while_a_listed_game_has_no_path()
		{
			// FullPath is left unset: the entity's setter refuses an explicit null, but its default,
			// unset backing field already reads back as null, which is what a game with no path is.
			var nopath = new UserGame { GameId = Guid.NewGuid(), FileName = "nopath.exe" };
			SettingsManager.UserGames.Items.Add(nopath);
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			UserGame found = null;
			try
			{
				var copy = new FileInfo(Path.Combine(folder, "found.exe"));
				File.Copy(typeof(XInputMaskScanner).Assembly.Location, copy.FullName);
				copy.Refresh();
				Ui.OnUiThreadWatched(() =>
				{
					using (var form = new Form { ShowInTaskbar = false })
					using (var panel = new GamesGridUserControl())
					{
						form.Controls.Add(panel);
						form.Show();
						var scanner = new XInputMaskScanner();
						var game = new UserGame { GameId = Guid.NewGuid(), FileName = copy.Name, FullPath = copy.FullName };
						var reported = new XInputMaskScannerEventArgs
						{
							State = XInputMaskScannerState.GameFound,
							Game = game,
							GameFileInfo = copy,
						};
						var scan = Task.Run(() => Handler(panel)(scanner, reported));
						PumpUntil(scan);
						Assert.IsTrue(scan.IsCompleted, "The scan never came back from reporting the game.");
						Assert.IsFalse(scan.IsFaulted, "Reporting a game while a listed game has no path failed: " + scan.Exception);
						Assert.IsTrue(SettingsManager.UserGames.Items.Contains(game), "The found game was not added.");
						found = game;
					}
				}, "Showing a report");
			}
			finally
			{
				SettingsManager.UserGames.Items.Remove(nopath);
				if (found != null)
					SettingsManager.UserGames.Items.Remove(found);
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("Enabling a listed game skips a listed game with no path instead of failing on it")]
		public void Enabling_a_game_skips_a_listed_game_with_no_path()
		{
			// Disabled, with a path: this is the row the click below enables, which is what runs
			// the otherGames query and its x.FullPath != null filter.
			var toEnable = new UserGame
			{
				GameId = Guid.NewGuid(),
				FileName = "other.exe",
				FullPath = Path.Combine(Path.GetTempPath(), "other.exe"),
				IsEnabled = false,
				ProcessorArchitecture = (int)System.Reflection.ProcessorArchitecture.X86,
			};
			// Enabled, with no path (left unset, see A_game_is_found_while_a_listed_game_has_no_path):
			// sits elsewhere in the list while toEnable is clicked, so the query above reaches it.
			var noPath = new UserGame
			{
				GameId = Guid.NewGuid(),
				FileName = "nopath.exe",
				IsEnabled = true,
				ProcessorArchitecture = (int)System.Reflection.ProcessorArchitecture.Amd64,
			};
			SettingsManager.UserGames.Items.Add(toEnable);
			SettingsManager.UserGames.Items.Add(noPath);
			try
			{
				Ui.OnUiThread(() =>
				{
					using (var form = new Form { ShowInTaskbar = false })
					using (var panel = new GamesGridUserControl())
					{
						form.Controls.Add(panel);
						form.Show();
						Application.DoEvents();
						var grid = (DataGridView)panel.Controls.Find("GamesDataGridView", true).Single();
						var row = grid.Rows.Cast<DataGridViewRow>().First(r => ReferenceEquals(r.DataBoundItem, toEnable));
						var column = grid.Columns["IsEnabledColumn"];
						var method = typeof(GamesGridUserControl).GetMethod("GamesDataGridView_CellClick", BindingFlags.Instance | BindingFlags.NonPublic);
						var args = new DataGridViewCellEventArgs(column.Index, row.Index);
						method.Invoke(panel, new object[] { grid, args });
						Assert.IsTrue(toEnable.IsEnabled, "The click did not enable the game.");
						Assert.IsTrue(noPath.IsEnabled, "A listed game with no path should not be touched by enabling another game.");
					}
				});
			}
			finally
			{
				SettingsManager.UserGames.Items.Remove(toEnable);
				SettingsManager.UserGames.Items.Remove(noPath);
			}
		}

		[TestMethod, TestCategory("games"), TestCategory("critical")]
		[Description("A listed game or program with no file name is skipped instead of ending the scan")]
		public void A_listed_entry_with_no_file_name_is_skipped()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-scan-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(folder);
			try
			{
				File.Copy(typeof(XInputMaskScanner).Assembly.Location, Path.Combine(folder, "game.exe"));
				var scanner = new XInputMaskScanner();
				UserGame added = null;
				scanner.Progress += (s, e) =>
				{
					if (e.State == XInputMaskScannerState.GameFound)
						added = e.Game;
				};
				// A games or programs list entry with no file name is skipped on the worker, and the real,
				// named file after it is still reached. The entry's FileName and FullPath are left unset:
				// the entity's setter refuses an explicit null, but an unset field reads back as null.
				var games = new List<UserGame> { new UserGame() };
				var programs = new List<Program>
				{
					new Program(),
					new Program { FileName = "game.exe" },
				};
				Exception failure = null;
				try
				{
					scanner.ScanGames(new[] { folder }, games, programs);
				}
				catch (Exception ex)
				{
					failure = ex;
				}
				Assert.IsNull(failure, "A listed entry with no file name ended the scan: " + failure);
				Assert.IsNotNull(added, "The named file was not found because the scan stopped at the entry with no file name.");
			}
			finally
			{
				Directory.Delete(folder, true);
			}
		}
	}
}
