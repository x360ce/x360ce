// @under-test: Engine/Common/Sdl/SdlDatabase.cs, App.v4/Common/AutoMapHelper.cs, App.v4/Common/SettingsManager.XML.cs, App.v4/Common/Update/SdlUpdateClient.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// SDL's controller database as the program carries it, merges it with a newer copy and the environment, and uses it:
	/// Auto Preset from SDL first, Copy and Paste Preset in SDL's format, and the update button.
	/// </summary>
	[TestClass]
	public class SdlDatabaseTest
	{
		const string XboxGuid = "030000005e0400008e02000000000000";

		static SdlSource Source(SdlSourceKind kind, params string[] lines)
		{
			return SdlSource.Parse(string.Join("\n", lines), kind);
		}

		/// <summary>An Xbox 360 controller as DirectInput reports it: X, Y, Z, Rx and Ry, ten buttons and a hat.</summary>
		static UserDevice XboxPad()
		{
			return new UserDevice
			{
				CapType = (int)DeviceType.Gamepad,
				DevVendorId = 0x045E,
				DevProductId = 0x028E,
				ProductName = "Controller (XBOX 360 For Windows)",
				DiAxeMask = 0x1F,
				CapButtonCount = 10,
				CapPovCount = 1,
				DeviceObjects = new[]
				{
					new DeviceObjectItem((int)JoystickOffset.Buttons0, ObjectGuid.Button, 0, DeviceObjectTypeFlags.PushButton, 0, "Button 0") { DiIndex = 0 },
					new DeviceObjectItem((int)JoystickOffset.X, ObjectGuid.XAxis, ObjectAspect.Position, DeviceObjectTypeFlags.AbsoluteAxis, 0, "X Axis") { DiIndex = 0 },
				},
			};
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The built-in database is read from the engine's resource, with its commit, and holds the Windows rows")]
		public void Built_in_database_loads_from_the_resource()
		{
			// The resource is the engine's, which both programs carry; make sure it is loaded where resources are looked for.
			Assert.IsNotNull(typeof(SdlDatabase).Assembly);
			var text = SdlDatabase.ReadBuiltIn();
			Assert.IsTrue(text.Length > 500000, "The built-in database is missing or cut short.");
			var source = SdlSource.Parse(text, SdlSourceKind.BuiltIn);
			Assert.AreEqual("07af1d50187e94c861c9f9f5d8a55b148f11306b", source.Sha);
			Assert.AreEqual(new DateTime(2026, 11, 2, 16, 46, 32, DateTimeKind.Utc), source.Date.Value);
			Assert.AreEqual(DateTimeKind.Utc, source.Date.Value.Kind);
			Assert.IsTrue(source.Mappings.Count > 800, "The built-in database holds " + source.Mappings.Count + " Windows rows.");
			Assert.IsTrue(source.Mappings.All(x => x.IsForWindows), "A row for another platform was read.");
			Assert.IsTrue(source.Mappings.Any(x => x.Guid.Form == SdlGuidForm.XInput), "The xinput row is missing.");
			StringAssert.StartsWith(source.Describe(), "built in, 2026-11-02, 07af1d5, ");
			Assert.IsNull(source.Address, "The line \"# Source:\" SDL_GameControllerDB writes was read as the address the copy was downloaded from.");
			StringAssert.Contains(SdlDatabase.ReadLicence(), "Sam Lantinga", "The database's licence is not carried with it.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Sources merge in order, a later one winning a row with the same GUID, the CRC and version aside")]
		public void Later_sources_win_rows_with_the_same_guid()
		{
			var builtIn = Source(SdlSourceKind.BuiltIn,
				"# Commit: 07af1d50187e94c861c9f9f5d8a55b148f11306b 2026-11-02T16:46:32Z",
				XboxGuid + ",Built in,a:b0,platform:Windows,",
				"03000000300f00000a01000000000000,Only built in,a:b0,platform:Windows,",
				XboxGuid + ",Linux row,a:b0,platform:Linux,");
			var file = Source(SdlSourceKind.File,
				// The name's CRC and a version: the same controller.
				"030012345e0400008e02000001000000,From the file,a:b1,platform:Windows,");
			var environment = Source(SdlSourceKind.Environment,
				XboxGuid + ",From the environment,a:b2,");
			var merged = SdlDatabase.Merge(builtIn, file, environment);
			Assert.AreEqual(2, merged.Mappings.Count, "A row for another platform, or a row twice, was kept.");
			var xbox = merged.Find(new SdlDevice { VendorId = 0x045E, ProductId = 0x028E });
			Assert.AreEqual("From the environment", xbox.Name);
			Assert.AreEqual(SdlSourceKind.Environment, merged.SourceOf(xbox).Kind);
			Assert.AreEqual("From the file", SdlDatabase.Merge(builtIn, file).Find(new SdlDevice { VendorId = 0x045E, ProductId = 0x028E }).Name);
			Assert.AreEqual(builtIn, merged.Version, "The built-in copy, the only one with a date, is not the version.");
			StringAssert.StartsWith(merged.Describe(), "built in, 2026-11-02, 07af1d5, 2 Windows rows; ");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The settings folder's file is used when a person put it there or it is newer than the built-in copy")]
		public void Settings_file_is_used_when_newer_or_put_there()
		{
			var builtIn = Source(SdlSourceKind.BuiltIn, "# Commit: 07af1d50187e94c861c9f9f5d8a55b148f11306b 2026-11-02T16:46:32Z", XboxGuid + ",Built in,a:b0,platform:Windows,");
			var row = XboxGuid + ",File,a:b1,platform:Windows,";
			var own = SdlDatabase.ParseSettingsFile(row, "own.txt", builtIn);
			Assert.AreEqual(SdlSourceKind.File, own.Kind, "A file with no commit line is a person's own, and always used.");
			var newer = SdlDatabase.ParseSettingsFile(SdlSource.WithHeader(row, new string('a', 40), new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)), "new.txt", builtIn);
			Assert.AreEqual(SdlSourceKind.Downloaded, newer.Kind);
			Assert.AreEqual(new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), newer.Date.Value);
			Assert.AreEqual(newer, SdlDatabase.Merge(builtIn, newer).Version, "A newer download is not the version.");
			var older = SdlDatabase.ParseSettingsFile(SdlSource.WithHeader(row, new string('b', 40), new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)), "old.txt", builtIn);
			Assert.IsNull(older, "A download older than the built-in copy was used.");
			// The commit line is replaced, not added to.
			var twice = SdlSource.WithHeader(SdlSource.WithHeader(row, new string('a', 40), DateTime.UtcNow), new string('c', 40), new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc));
			Assert.AreEqual(1, twice.Split('\n').Count(x => x.StartsWith(SdlSource.HeaderPrefix)));
			Assert.AreEqual(new string('c', 40), SdlSource.Parse(twice, SdlSourceKind.File).Sha);
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A downloaded copy names the address it came from under its commit line, and a copy saved without one still reads")]
		public void Saved_header_names_the_address_and_older_copies_still_read()
		{
			var builtIn = Source(SdlSourceKind.BuiltIn, "# Commit: 07af1d50187e94c861c9f9f5d8a55b148f11306b 2026-11-02T16:46:32Z", XboxGuid + ",Built in,a:b0,platform:Windows,");
			var row = XboxGuid + ",File,a:b1,platform:Windows,";
			var date = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
			var text = SdlSource.WithHeader(row, new string('a', 40), date, Mirror);
			var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
			StringAssert.StartsWith(lines[0], SdlSource.HeaderPrefix, "The commit line is not first, where 4.29 builds read it.");
			Assert.AreEqual(SdlSource.AddressPrefix + Mirror, lines[1]);
			var source = SdlDatabase.ParseSettingsFile(text, "new.txt", builtIn);
			Assert.AreEqual(SdlSourceKind.Downloaded, source.Kind);
			Assert.AreEqual(Mirror, source.Address);
			StringAssert.StartsWith(source.Describe(), "downloaded from " + Mirror + ", 2026-12-01, aaaaaaa, 1 Windows row");
			// A copy an earlier 4.29 build saved: the commit line only.
			var older = SdlDatabase.ParseSettingsFile("# Commit: " + new string('b', 40) + " 2026-12-01T00:00:00Z\r\n" + row, "old.txt", builtIn);
			Assert.AreEqual(SdlSourceKind.Downloaded, older.Kind, "A copy saved before the address was kept is no longer read as downloaded.");
			Assert.AreEqual(new string('b', 40), older.Sha);
			Assert.IsNull(older.Address);
			StringAssert.StartsWith(older.Describe(), "downloaded, 2026-12-01, bbbbbbb, ");
			// Downloaded again from elsewhere: the lines are replaced, not added to.
			var again = SdlSource.WithHeader(text, new string('c', 40), date, SdlUpdateClient.DefaultUrl);
			Assert.AreEqual(1, again.Split('\n').Count(x => x.StartsWith(SdlSource.AddressPrefix)));
			Assert.AreEqual(SdlUpdateClient.DefaultUrl, SdlSource.Parse(again, SdlSourceKind.File).Address);
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The program's database merges the built-in copy, the settings folder's file and SDL_GAMECONTROLLERCONFIG")]
		public void Load_merges_the_three_sources()
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-sdl-" + Guid.NewGuid().ToString("N"));
			var appData = EngineHelper.AppDataPath;
			var variable = Environment.GetEnvironmentVariable(SdlDatabase.EnvironmentVariable);
			Assert.IsNotNull(typeof(AutoMapHelper).Assembly);
			try
			{
				EngineHelper.AppDataPath = folder;
				Directory.CreateDirectory(Path.Combine(folder, "Settings"));
				File.WriteAllText(SdlDatabase.SettingsFilePath, XboxGuid + ",From the file,a:b1,platform:Windows,\n03000000300f00000a01000000000000,Only in the file,a:b0,platform:Windows,");
				Environment.SetEnvironmentVariable(SdlDatabase.EnvironmentVariable, XboxGuid + ",From Steam,a:b2,");
				var database = SdlDatabase.Load();
				CollectionAssert.AreEqual(new[] { SdlSourceKind.BuiltIn, SdlSourceKind.File, SdlSourceKind.Environment }, database.Sources.Select(x => x.Kind).ToArray());
				Assert.AreEqual("From Steam", database.Find(new SdlDevice { VendorId = 0x045E, ProductId = 0x028E }).Name);
				Assert.AreEqual("Only in the file", database.Find(new SdlDevice { VendorId = 0x0F30, ProductId = 0x010A }).Name);
				Assert.IsTrue(database.Mappings.Count > 800, "The built-in rows were lost in the merge.");
			}
			finally
			{
				Environment.SetEnvironmentVariable(SdlDatabase.EnvironmentVariable, variable);
				EngineHelper.AppDataPath = appData;
				Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A device's row is found by its ids or its product GUID; among several, the name nearest the device's")]
		public void Rows_are_found_by_ids_and_name()
		{
			var database = SdlDatabase.Merge(Source(SdlSourceKind.BuiltIn,
				"03000000790000000600000000000000,G-Shark GS-GP702,a:b2,platform:Windows,",
				"05000000790000000600000000000000,Generic USB Joystick,a:b1,platform:Windows,",
				"7900060000000000000000504944564944,Too long a GUID,a:b0,platform:Windows,",
				"79000600000000000000504944564944,Old Pad,a:b3,platform:Windows,"));
			var device = new SdlDevice { VendorId = 0x0079, ProductId = 0x0006, ProductName = "Generic   USB  Joystick" };
			var all = database.FindAll(device);
			Assert.AreEqual(3, all.Count, "A row of the device was missed, or a GUID too long was read.");
			Assert.AreEqual("Generic USB Joystick", all[0].Name, "The row whose name shares the most words with the device's is not first.");
			Assert.AreEqual("Old Pad", database.Find(new SdlDevice { ProductGuid = RawInputDevice.GetProductGuid(0x0079, 0x0006) }).Name, "An old row was not found by the product GUID it starts with.");
			Assert.AreEqual("Generic USB Joystick", database.Find(new SdlDevice { VendorId = 0x0079, ProductId = 0x0006, ProductName = "Generic USB Joystick" }).Name);
			Assert.IsNull(database.Find(new SdlDevice { VendorId = 0x1234, ProductId = 0x5678 }));
			Assert.IsNull(database.Find(null));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Auto Preset maps a pad from x360ce's database, then SDL's, and guesses only for a device neither has a row for")]
		public void Auto_preset_takes_sdl_first_and_guesses_otherwise()
		{
			var database = SdlDatabase.Merge(Source(SdlSourceKind.BuiltIn,
				XboxGuid + ",Xbox 360 Controller,a:b0,b:b1,back:b6,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,leftshoulder:b4,leftstick:b8,lefttrigger:+a2,leftx:a0,lefty:a1,rightshoulder:b5,rightstick:b9,righttrigger:-a2,rightx:a3,righty:a4,start:b7,x:b2,y:b3,misc1:b10,platform:Windows,"));
			string note;
			// x360ce's own database first, SDL's where its row maps nothing.
			var x360ce = SdlDatabase.Merge(Source(SdlSourceKind.BuiltIn, XboxGuid + ",Xbox 360 Pad,users:9,A:2,UseForceFeedback:1,platform:Windows,"));
			var both = AutoMapHelper.GetAutoPreset(XboxPad(), out note, database, x360ce);
			StringAssert.Contains(note, "x360ce's controller database");
			Assert.AreEqual("2", both.ButtonA, "SDL won over x360ce's database.");
			Assert.AreEqual("x3", both.LeftTrigger, "SDL did not fill what x360ce's row leaves empty.");
			Assert.AreEqual("1", both.ForceEnable);
			var none = SdlDatabase.Merge();
			// A DirectInput device not read yet, as when it is first put on a tab, has no objects: x360ce's row, which needs
			// none, fills it; SDL's row and the guess wait until it has been read.
			var unread = XboxPad();
			unread.DeviceObjects = null;
			var early = AutoMapHelper.GetAutoPreset(unread, out note, database, x360ce);
			Assert.AreEqual("2", early.ButtonA, "x360ce's row waited for the device's objects.");
			Assert.AreEqual("1", early.ForceEnable);
			Assert.IsTrue(string.IsNullOrEmpty(early.LeftTrigger), "SDL's row was read without the device's axes.");
			Assert.AreEqual(Guid.Empty, AutoMapHelper.GetAutoPreset(unread, out note, database, none).PadSettingChecksum,
				"A device only SDL knows was filled before its axes were known.");
			var ps = AutoMapHelper.GetAutoPreset(XboxPad(), out note, database, none);
			StringAssert.Contains(note, "SDL's controller database");
			StringAssert.Contains(note, "misc1:b10", "What was left out is not said.");
			Assert.AreEqual("x3", ps.LeftTrigger);
			Assert.AreEqual("x-3", ps.RightTrigger);
			Assert.AreEqual("a-5", ps.RightThumbAxisY);
			Assert.AreEqual("p1", ps.DPad);
			Assert.AreEqual(ps.CleanAndGetCheckSum(), ps.PadSettingChecksum);
			// A pad SDL does not know is guessed, as before.
			var unknown = XboxPad();
			unknown.DevVendorId = 0x1234;
			var guessed = AutoMapHelper.GetAutoPreset(unknown, out note, database, none);
			StringAssert.Contains(note, "own guess");
			Assert.AreEqual("1", guessed.ButtonA, "The guess did not map the first button.");
			Assert.AreEqual("a1", guessed.LeftThumbAxisX);
			// Keyboards, mice and plugin devices are always guessed.
			foreach (var type in new[] { DeviceType.Keyboard, DeviceType.Mouse })
			{
				var other = XboxPad();
				other.CapType = (int)type;
				AutoMapHelper.GetAutoPreset(other, out note, database, x360ce);
				StringAssert.Contains(note, "own guess", type + " was mapped from a database.");
			}
			var plugin = XboxPad();
			plugin.InputSourceType = (int)InputSourceType.Plugin;
			AutoMapHelper.GetAutoPreset(plugin, out note, database, x360ce);
			StringAssert.Contains(note, "own guess", "A plugin device was mapped from a database.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Copy Preset as SDL writes the device's row; Paste Preset reads a row, or a whole file, for the selected device")]
		public void Copy_and_paste_preset_in_sdl_format()
		{
			var pad = XboxPad();
			var ps = new PadSetting { ButtonA = "1", ButtonB = "2", LeftTrigger = "x3", RightTrigger = "x-3", LeftThumbAxisX = "a1", LeftThumbAxisY = "a-2", DPad = "p1", ButtonGuide = "=b1*b2" };
			var text = SettingsManager.PadSettingToText(ps, SettingsManager.PresetFormat.Sdl, pad);
			Assert.AreEqual(XboxGuid + ",Controller (XBOX 360 For Windows),a:b0,b:b1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,dpup:h0.1,lefttrigger:+a2,leftx:a0,lefty:a1,righttrigger:-a2,platform:Windows,", text);
			string note;
			var back = SettingsManager.PadSettingFromText(text, pad, out note);
			StringAssert.Contains(note, "Controller (XBOX 360 For Windows)");
			foreach (var code in SdlConverter.MappingBoxes.Where(x => x != MapCode.ButtonGuide))
				Assert.AreEqual(SdlConverter.GetValue(ps, code), SdlConverter.GetValue(back, code), code.ToString());
			Assert.AreEqual("", SdlConverter.GetValue(back, MapCode.ButtonGuide), "A formula was carried by SDL's format.");
			Assert.AreEqual(back.CleanAndGetCheckSum(), back.PadSettingChecksum);
			// A whole file: comments, another platform, another pad, then the device's own row.
			var file = string.Join(Environment.NewLine, new[]
			{
				"# Game Controller DB for SDL",
				XboxGuid + ",Linux,a:b5,platform:Linux,",
				"03000000300f00000a01000000000000,Other Pad,a:b3,platform:Windows,",
				XboxGuid + ",Own Row,a:b1,platform:Windows,",
			});
			Assert.AreEqual("2", SettingsManager.PadSettingFromText(file, pad, out note).ButtonA, "The device's own row was not chosen.");
			Assert.AreEqual("4", SettingsManager.PadSettingFromText(file, null, out note).ButtonA, "With no device, the first Windows row was not chosen.");
			var linux = Assert.ThrowsExactly<InvalidDataException>(() => SettingsManager.PadSettingFromText(XboxGuid + ",Linux,a:b5,platform:Linux,", pad, out note));
			StringAssert.Contains(linux.Message, "Windows");
			StringAssert.Contains(SettingsManager.NotAPresetMessage, "SDL");
		}

		/// <summary>A mirror of the database: any address that is not GitHub's.</summary>
		const string Mirror = "https://example.org/sdl/gamecontrollerdb.txt";

		/// <summary>The built-in copy as the update compares with it.</summary>
		static SdlDatabase BuiltIn()
		{
			return SdlDatabase.Merge(Source(SdlSourceKind.BuiltIn, "# Commit: 07af1d50187e94c861c9f9f5d8a55b148f11306b 2026-11-02T16:46:32Z", XboxGuid + ",Built in,a:b0,platform:Windows,"));
		}

		/// <summary>A database of so many Windows rows.</summary>
		static string Rows(int count)
		{
			return string.Join("\n", Enumerable.Range(1, count).Select(i => string.Format("03000000{0:x4}00000a01000000000000,Pad {0},a:b0,platform:Windows,", i)));
		}

		static UpdateResponse Ok(string body, DateTime? lastModified = null)
		{
			return new UpdateResponse { StatusCode = 200, Body = Encoding.UTF8.GetBytes(body), LastModified = lastModified };
		}

		/// <summary>The answers at a table of addresses, each address asked noted; 404 anywhere else.</summary>
		static Func<UpdateRequest, UpdateResponse> Answers(List<string> asked, Dictionary<string, UpdateResponse> answers)
		{
			return r =>
			{
				asked.Add(r.Url);
				UpdateResponse answer;
				return answers.TryGetValue(r.Url, out answer) ? answer : new UpdateResponse { StatusCode = 404 };
			};
		}

		/// <summary>GitHub's answers for a file of a repository on a branch: its newest commit, and the file at that commit.</summary>
		static Func<UpdateRequest, UpdateResponse> GitHub(string repository, string branch, string file, string sha, string date, string text, List<string> asked)
		{
			return Answers(asked, new Dictionary<string, UpdateResponse>
			{
				{ CommitsUrl(repository, branch, file), Ok("[{\"sha\":\"" + sha + "\",\"commit\":{\"committer\":{\"name\":\"GitHub\",\"date\":\"" + date + "\"}}}]") },
				{ FileUrl(repository, sha, file), Ok(text) },
			});
		}

		static string CommitsUrl(string repository, string branch, string file)
		{
			return "https://api.github.com/repos/" + repository + "/commits?path=" + file + "&sha=" + branch + "&per_page=1";
		}

		static string FileUrl(string repository, string sha, string file)
		{
			return "https://raw.githubusercontent.com/" + repository + "/" + sha + "/" + file;
		}

		static string Sha1Of(string text)
		{
			using (var sha = SHA1.Create())
				return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
		}

		/// <summary>Hands the test where to save the database, in a folder of its own that is removed after.</summary>
		static void InTempFolder(Action<string> test)
		{
			var folder = Path.Combine(Path.GetTempPath(), "x360ce-sdl-" + Guid.NewGuid().ToString("N"));
			try
			{
				test(Path.Combine(folder, SdlDatabase.FileName));
			}
			finally
			{
				if (Directory.Exists(folder))
					Directory.Delete(folder, true);
			}
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("The update button fetches the newest commit's file, keeps it only when it holds Windows rows, and saves it with its commit and address")]
		public void Update_fetches_checks_and_saves_the_newest_file()
		{
			var builtIn = BuiltIn();
			var sha = "0123456789abcdef0123456789abcdef01234567";
			var rows = Rows(150);
			const string repository = "mdqinc/SDL_GameControllerDB";
			const string file = "gamecontrollerdb.txt";
			InTempFolder(path =>
			{
				var asked = new List<string>();
				SdlUpdateOutcome outcome;
				var message = new SdlUpdateClient(GitHub(repository, "master", file, sha, "2026-12-01T10:00:00Z", rows, asked)).Update(SdlUpdateClient.DefaultUrl, path, builtIn, out outcome);
				Assert.AreEqual(SdlUpdateOutcome.Saved, outcome, message);
				CollectionAssert.AreEqual(new[] { CommitsUrl(repository, "master", file), FileUrl(repository, sha, file) }, asked);
				StringAssert.Contains(message, "2026-12-01");
				var written = SdlDatabase.ParseSettingsFile(File.ReadAllText(path), path, builtIn.Version);
				Assert.AreEqual(SdlSourceKind.Downloaded, written.Kind);
				Assert.AreEqual(sha, written.Sha);
				Assert.AreEqual(SdlUpdateClient.DefaultUrl, written.Address);
				Assert.AreEqual(150, written.Mappings.Count);
				// The same commit again: nothing is fetched.
				asked.Clear();
				message = new SdlUpdateClient(GitHub(repository, "master", file, sha, "2026-12-01T10:00:00Z", rows, asked)).Update(SdlUpdateClient.DefaultUrl, path, SdlDatabase.Merge(builtIn.Sources[0], written), out outcome);
				Assert.AreEqual(SdlUpdateOutcome.UpToDate, outcome, message);
				StringAssert.Contains(message, "up to date");
				Assert.AreEqual(1, asked.Count, "The file was fetched although the database is up to date.");
				// A refusal is said, not thrown.
				message = new SdlUpdateClient(r => new UpdateResponse { StatusCode = 403 }).Update(SdlUpdateClient.DefaultUrl, path, builtIn, out outcome);
				Assert.AreEqual(SdlUpdateOutcome.Failed, outcome);
				StringAssert.Contains(message, "403");
			});
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A GitHub address of another fork, branch or file asks that repository for the newest commit of that file on that branch, and fetches the file at it")]
		public void A_fork_or_branch_address_asks_its_own_repository()
		{
			var builtIn = BuiltIn();
			var sha = "fedcba9876543210fedcba9876543210fedcba98";
			var rows = Rows(120);
			var addresses = new[]
			{
				new { Address = "https://raw.githubusercontent.com/someone/SDL_GameControllerDB/my-branch/data/gamecontrollerdb.txt", Repository = "someone/SDL_GameControllerDB", Branch = "my-branch", File = "data/gamecontrollerdb.txt" },
				// GitHub's Raw button writes the branch after refs/heads/.
				new { Address = "https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/refs/heads/master/gamecontrollerdb.txt", Repository = "mdqinc/SDL_GameControllerDB", Branch = "master", File = "gamecontrollerdb.txt" },
			};
			foreach (var a in addresses)
				InTempFolder(path =>
				{
					var asked = new List<string>();
					SdlUpdateOutcome outcome;
					var message = new SdlUpdateClient(GitHub(a.Repository, a.Branch, a.File, sha, "2026-12-02T10:00:00Z", rows, asked)).Update(a.Address, path, builtIn, out outcome);
					Assert.AreEqual(SdlUpdateOutcome.Saved, outcome, a.Address + ": " + message);
					CollectionAssert.AreEqual(new[] { CommitsUrl(a.Repository, a.Branch, a.File), FileUrl(a.Repository, sha, a.File) }, asked, a.Address);
					var written = SdlDatabase.ParseSettingsFile(File.ReadAllText(path), path, builtIn.Version);
					Assert.AreEqual(sha, written.Sha);
					Assert.AreEqual(a.Address, written.Address);
				});
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("Any other http or https address is fetched once as a file, saved with the SHA-1 of its bytes and its date, and is up to date the next time")]
		public void A_plain_address_is_fetched_once_and_named_by_its_hash()
		{
			var builtIn = BuiltIn();
			var rows = Rows(130);
			// After the built-in copy's commit, before the download.
			var modified = new DateTime(2026, 11, 3, 8, 30, 0, DateTimeKind.Utc);
			InTempFolder(path =>
			{
				var asked = new List<string>();
				var mirror = Answers(asked, new Dictionary<string, UpdateResponse> { { Mirror, Ok(rows, modified) } });
				SdlUpdateOutcome outcome;
				var message = new SdlUpdateClient(mirror).Update(Mirror, path, builtIn, out outcome);
				Assert.AreEqual(SdlUpdateOutcome.Saved, outcome, message);
				CollectionAssert.AreEqual(new[] { Mirror }, asked, "The address was not asked once, and only it.");
				var written = SdlDatabase.ParseSettingsFile(File.ReadAllText(path), path, builtIn.Version);
				Assert.AreEqual(SdlSourceKind.Downloaded, written.Kind);
				Assert.AreEqual(Sha1Of(rows), written.Sha, "The copy is not named by the SHA-1 of its bytes.");
				Assert.AreEqual(modified, written.Date.Value, "The copy is not dated by the answer's Last-Modified.");
				Assert.AreEqual(Mirror, written.Address);
				Assert.AreEqual(130, written.Mappings.Count);
				StringAssert.StartsWith(written.Describe(), "downloaded from " + Mirror + ", 2026-11-03, " + Sha1Of(rows).Substring(0, 7) + ", ");
				// The same file again: up to date, and the saved copy stays as it was.
				asked.Clear();
				var before = File.ReadAllText(path);
				message = new SdlUpdateClient(mirror).Update(Mirror, path, SdlDatabase.Merge(builtIn.Sources[0], written), out outcome);
				Assert.AreEqual(SdlUpdateOutcome.UpToDate, outcome, message);
				StringAssert.Contains(message, "up to date");
				Assert.AreEqual(1, asked.Count);
				Assert.AreEqual(before, File.ReadAllText(path));
				// An answer that does not say when the file changed: dated by the download.
				var start = DateTime.UtcNow.AddSeconds(-1);
				var changed = rows + "\n# changed";
				message = new SdlUpdateClient(Answers(asked, new Dictionary<string, UpdateResponse> { { Mirror, Ok(changed) } })).Update(Mirror, path, SdlDatabase.Merge(builtIn.Sources[0], written), out outcome);
				Assert.AreEqual(SdlUpdateOutcome.Saved, outcome, message);
				var newer = SdlDatabase.ParseSettingsFile(File.ReadAllText(path), path, builtIn.Version);
				Assert.AreEqual(Sha1Of(changed), newer.Sha);
				Assert.IsTrue(newer.Date.Value >= start && newer.Date.Value <= DateTime.UtcNow, "Dated " + newer.Date.Value.ToString("o") + ", not by the download.");
			});
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("An address that is not an absolute http or https address is said so, and nothing is fetched")]
		public void An_address_that_is_not_http_fetches_nothing()
		{
			var builtIn = BuiltIn();
			foreach (var address in new[] { "ftp://example.org/gamecontrollerdb.txt", @"C:\gamecontrollerdb.txt", "file:///C:/gamecontrollerdb.txt", "gamecontrollerdb.txt", "", null })
				InTempFolder(path =>
				{
					var asked = new List<string>();
					SdlUpdateOutcome outcome;
					var message = new SdlUpdateClient(Answers(asked, new Dictionary<string, UpdateResponse>())).Update(address, path, builtIn, out outcome);
					Assert.AreEqual(SdlUpdateOutcome.Failed, outcome, "\"" + address + "\": " + message);
					Assert.AreEqual(0, asked.Count, "\"" + address + "\" was fetched: " + string.Join(", ", asked));
					StringAssert.Contains(message, "http", "The answer does not say what address is wanted.");
					Assert.IsFalse(File.Exists(path));
				});
		}

		[TestMethod, TestCategory("update"), TestCategory("critical")]
		[Description("A file with fewer Windows rows than the database holds is saved from no source, and leaves the copy there as it was")]
		public void A_file_that_is_not_the_database_is_saved_from_no_source()
		{
			var builtIn = BuiltIn();
			var sha = new string('9', 40);
			const string repository = "mdqinc/SDL_GameControllerDB";
			const string file = "gamecontrollerdb.txt";
			foreach (var text in new[] { "<html>Not found</html>", Rows(SdlUpdateClient.MinimumWindowsRows - 1) })
				InTempFolder(path =>
				{
					var sources = new[]
					{
						new { Address = SdlUpdateClient.DefaultUrl, Transport = GitHub(repository, "master", file, sha, "2026-12-05T10:00:00Z", text, new List<string>()) },
						new { Address = Mirror, Transport = Answers(new List<string>(), new Dictionary<string, UpdateResponse> { { Mirror, Ok(text) } }) },
					};
					foreach (var s in sources)
					{
						SdlUpdateOutcome outcome;
						var message = new SdlUpdateClient(s.Transport).Update(s.Address, path, builtIn, out outcome);
						Assert.AreEqual(SdlUpdateOutcome.Failed, outcome, s.Address + ": " + message);
						StringAssert.Contains(message, "Nothing was saved", s.Address);
						Assert.IsFalse(File.Exists(path), s.Address + ": a file that is not the database was saved.");
					}
					// Over a copy already there.
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					var good = SdlSource.WithHeader(Rows(150), new string('1', 40), new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), Mirror);
					File.WriteAllText(path, good);
					foreach (var s in sources)
					{
						SdlUpdateOutcome outcome;
						new SdlUpdateClient(s.Transport).Update(s.Address, path, builtIn, out outcome);
						Assert.AreEqual(good, File.ReadAllText(path), s.Address + ": a file that is not the database replaced the saved one.");
					}
				});
		}
	}
}
