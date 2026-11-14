// @under-test: Engine/Common/Sdl/X360ceDatabase.cs, Engine/Common/Sdl/X360ceDatabaseServer.cs, Engine/Maps/PadSettingIni.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// x360ce's controller database, gamecontrollerdb_x360ce.windows.txt: each controller's most used complete setting as a
	/// row SDL's reader reads, written by the engine for the server and the update script, merged before SDL's mapping.
	/// </summary>
	/// <remarks>
	/// Tests\WebServices\popular-presets.xml is x360ce's public settings service's answer for the most popular controllers,
	/// recorded on 2026-11-14: the settings people use, as the server's database holds them.
	/// </remarks>
	[TestClass]
	public class X360ceDatabaseTest
	{
		static readonly XNamespace Ns = "http://x360ce.com/";

		/// <summary>The recorded presets: each controller, with its most used setting.</summary>
		static List<X360ceDatabaseEntry> Recorded()
		{
			var doc = XDocument.Load(Path.Combine(Ui.RepoRoot.FullName, @"Tests\WebServices\popular-presets.xml"));
			var settings = new Dictionary<Guid, PadSetting>();
			foreach (var e in doc.Descendants(Ns + "PadSetting"))
			{
				var ps = new PadSetting();
				foreach (var field in e.Elements())
				{
					var p = typeof(PadSetting).GetProperty(field.Name.LocalName);
					if (p == null)
						continue;
					if (p.PropertyType == typeof(string))
						p.SetValue(ps, field.Value, null);
					else if (p.PropertyType == typeof(Guid))
						p.SetValue(ps, Guid.Parse(field.Value), null);
				}
				settings[ps.PadSettingChecksum] = ps;
			}
			var entries = new List<X360ceDatabaseEntry>();
			foreach (var e in doc.Descendants(Ns + "Preset"))
			{
				PadSetting ps;
				if (!settings.TryGetValue(Guid.Parse((string)e.Element(Ns + "PadSettingChecksum")), out ps))
					continue;
				entries.Add(new X360ceDatabaseEntry
				{
					ProductGuid = Guid.Parse((string)e.Element(Ns + "ProductGuid")),
					ProductName = (string)e.Element(Ns + "ProductName"),
					Users = int.Parse((string)e.Element(Ns + "Users"), CultureInfo.InvariantCulture),
					Setting = ps,
				});
			}
			Assert.IsTrue(entries.Count >= 30, "The recorded answer holds only " + entries.Count + " presets with settings.");
			return entries;
		}

		static SdlMapping Row(string text)
		{
			SdlMapping row;
			Assert.IsTrue(SdlMapping.TryParse(text, out row), "Not read as a row: " + text);
			return row;
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Every setting x360ce.ini keeps has a key, and every key names one setting")]
		public void Each_setting_has_one_ini_key()
		{
			var keys = typeof(PadSetting).GetProperties().Where(p => p.PropertyType == typeof(string))
				.Select(p => new { p.Name, Key = typeof(SettingName).GetProperty(p.Name, BindingFlags.Public | BindingFlags.Static) })
				.Where(x => x.Key != null).Select(x => new { x.Name, Key = (string)x.Key.GetValue(null, null) }).ToArray();
			var twice = keys.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
			Assert.AreEqual(0, twice.Length, "Keys of two settings: " + string.Join(", ", twice));
			foreach (var k in keys)
				Assert.AreEqual(k.Name, PadSettingIni.NameOf(k.Key), k.Key);
			Assert.AreEqual("A", PadSettingIni.Key(nameof(PadSetting.ButtonA)));
			Assert.AreEqual(nameof(PadSetting.ForceEnable), PadSettingIni.NameOf("UseForceFeedback"));
			Assert.IsNull(PadSettingIni.NameOf(X360ceDatabase.UsersField));
			// What x360ce 3 writes, it reads back.
			var ps = new PadSetting { ButtonA = "2", LeftThumbAxisY = "-2", ForceEnable = "1", LeftMotorPeriod = "60" };
			var ini = new Dictionary<string, string>();
			PadSettingIni.Write(ps, (k, v) => ini[k] = v);
			Assert.AreEqual(PadSettingIni.Names.Length, ini.Count);
			Assert.AreEqual("2", ini["A"]);
			Assert.AreEqual(ps.CleanAndGetCheckSum(), PadSettingIni.Read(k => ini[k]).CleanAndGetCheckSum());
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Each recorded setting written as a row and read back keeps its checksum")]
		public void A_setting_written_as_a_row_keeps_its_checksum()
		{
			foreach (var entry in Recorded())
			{
				var expected = entry.Setting.CleanAndGetCheckSum();
				var text = X360ceDatabase.ToRow(entry).ToString();
				var back = X360ceDatabase.ToPadSetting(Row(text));
				Assert.AreEqual(expected, back.PadSettingChecksum, entry.ProductName + ": " + text);
				Assert.AreEqual(entry.Users, X360ceDatabase.Users(Row(text)));
			}
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The file holds one row for each controller, its most used, sorted by vendor and product, under a header that counts them")]
		public void The_file_holds_one_sorted_row_for_each_controller()
		{
			var entries = Recorded();
			var pad = entries[0];
			// A second, less used setting of the same controller, a GUID without vendor and product, and a setting that sets
			// nothing are left out.
			entries.Add(new X360ceDatabaseEntry { ProductGuid = pad.ProductGuid, ProductName = "Less used", Users = 1, Setting = new PadSetting { ButtonA = "9" } });
			entries.Add(new X360ceDatabaseEntry { ProductGuid = Guid.NewGuid(), ProductName = "No ids", Users = 999999, Setting = new PadSetting { ButtonA = "1" } });
			entries.Add(new X360ceDatabaseEntry { ProductGuid = new Guid("beef0e8f-0000-0000-0000-504944564944"), ProductName = "Sets nothing", Users = 11, Setting = new PadSetting() });
			var text = X360ceDatabase.Write(entries, "a test", new DateTime(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc), "A note.");
			StringAssert.StartsWith(text, "# x360ce's controller database");
			StringAssert.Contains(text, "# Made: 2026-11-14 from a test\n");
			StringAssert.Contains(text, "# A note.\n");
			Assert.AreEqual(new DateTime(2026, 11, 14), X360ceDatabase.MadeOn(text));
			var source = SdlSource.Parse(text, SdlSourceKind.BuiltIn);
			var products = entries.Where(x => x.ProductGuid.ToString().EndsWith("504944564944") && x.ProductName != "Sets nothing").Select(x => x.ProductGuid).Distinct().Count();
			Assert.AreEqual(products, source.Mappings.Count);
			StringAssert.Contains(text, "# Rows: " + products + "\n");
			var keys = source.Mappings.Select(x => (long)x.Guid.VendorId << 16 | (long)x.Guid.ProductId).ToArray();
			CollectionAssert.AreEqual(keys.OrderBy(x => x).ToArray(), keys, "The rows are not sorted by vendor and product.");
			Assert.IsFalse(text.Contains("Less used") || text.Contains("No ids") || text.Contains("Sets nothing"));
			Assert.IsTrue(source.Mappings.All(x => x.IsForWindows && X360ceDatabase.Users(x) > 0));
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Force feedback on at strength 0 is written at 100, and a missing motor period at x360ce 4's default")]
		public void Cleanup_puts_force_feedback_right()
		{
			var on = new PadSetting { ForceEnable = "1", ForceOverall = "0", LeftMotorPeriod = "", RightMotorPeriod = "0" };
			X360ceDatabase.Clean(on);
			Assert.AreEqual("100", on.ForceOverall);
			Assert.AreEqual(SettingName.DefaultLeftMotorPeriod, on.LeftMotorPeriod);
			Assert.AreEqual(SettingName.DefaultRightMotorPeriod, on.RightMotorPeriod);
			var kept = new PadSetting { ForceEnable = "1", ForceOverall = "40", LeftMotorPeriod = "120" };
			X360ceDatabase.Clean(kept);
			Assert.AreEqual("40", kept.ForceOverall);
			Assert.AreEqual("120", kept.LeftMotorPeriod);
			var off = new PadSetting { ForceOverall = "0" };
			X360ceDatabase.Clean(off);
			Assert.AreEqual("0", off.ForceOverall, "A setting with force feedback off was changed.");
			Assert.AreEqual("", off.LeftMotorPeriod ?? "");
			// The writer cleans what it writes.
			var text = X360ceDatabase.Write(new[] { new X360ceDatabaseEntry { ProductGuid = new Guid("c29b046d-0000-0000-0000-504944564944"), ProductName = "Wheel", Users = 5, Setting = new PadSetting { ForceEnable = "1", ForceOverall = "0" } } }, "a test", DateTime.UtcNow);
			StringAssert.Contains(text, "LeftMotorPeriod:160,");
			Assert.IsFalse(text.Contains("ForcePercent:"), "Strength 100 is the default and is not written.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("Settings merge key by key: a person's own first, then x360ce's database, then SDL's; a device with motors gets force feedback at the defaults when neither switches it on")]
		public void Settings_merge_key_by_key()
		{
			var own = new PadSetting { ButtonA = "5" };
			var x360ce = new PadSetting { ButtonA = "1", ButtonB = "2", ForceEnable = "1" };
			var sdl = new PadSetting { ButtonA = "3", ButtonB = "4", ButtonX = "6" };
			var merged = X360ceDatabase.Merge(own, x360ce, sdl);
			Assert.AreEqual("5", merged.ButtonA);
			Assert.AreEqual("2", merged.ButtonB);
			Assert.AreEqual("6", merged.ButtonX);
			Assert.AreEqual("1", merged.ForceEnable);
			Assert.AreEqual(merged.CleanAndGetCheckSum(), merged.PadSettingChecksum);

			// A device both databases know: x360ce's row, SDL's mapping where the row maps nothing.
			const string pidvid = "028e045e-0000-0000-0000-504944564944";
			var file = SdlDatabase.Merge(SdlSource.Parse("030000005e0400008e02000000000000,Xbox 360 Pad,users:9,A:2,UseForceFeedback:1,platform:Windows,", SdlSourceKind.BuiltIn));
			var sdlDb = SdlDatabase.Merge(SdlSource.Parse("030000005e0400008e02000000000000,Xbox 360 Controller,a:b0,b:b1,platform:Windows,", SdlSourceKind.BuiltIn));
			var device = new SdlDevice { VendorId = 0x045E, ProductId = 0x028E, ProductGuid = new Guid(pidvid), AxisMask = 0x1F, ButtonCount = 10, PovCount = 1 };
			string note;
			var both = X360ceDatabase.AutoSetting(device, false, true, out note, file, sdlDb);
			Assert.AreEqual("2", both.ButtonA, "SDL won over x360ce's database.");
			Assert.AreEqual("2", both.ButtonB, "SDL did not fill a box the row leaves empty.");
			Assert.AreEqual("1", both.ForceEnable);
			StringAssert.Contains(note, "x360ce's controller database");
			// A device only SDL knows: force feedback on at x360ce's defaults when it has motors.
			var none = SdlDatabase.Merge();
			var sdlOnly = X360ceDatabase.AutoSetting(device, true, true, out note, none, sdlDb);
			Assert.AreEqual("1", sdlOnly.ForceEnable);
			Assert.AreEqual(SettingName.DefaultLeftMotorPeriod, sdlOnly.LeftMotorPeriod);
			Assert.AreEqual("", X360ceDatabase.AutoSetting(device, false, true, out note, none, sdlDb).ForceEnable ?? "");
			Assert.IsNull(X360ceDatabase.AutoSetting(device, true, true, out note, none, none), "A device no database knows was given a setting.");
			// A row that leaves force feedback off, as the G27's does: a device with motors gets it on at x360ce's
			// defaults, and the row's own strengths and periods, set while nothing used them, are left out.
			var quiet = SdlDatabase.Merge(SdlSource.Parse("030000005e0400008e02000000000000,Xbox 360 Pad,users:9,A:2,LeftMotorStrength:0,RightMotorStrength:0,LeftMotorPeriod:60,RightMotorPeriod:120,platform:Windows,", SdlSourceKind.BuiltIn));
			var on = X360ceDatabase.AutoSetting(device, true, true, out note, quiet, sdlDb);
			Assert.AreEqual("1", on.ForceEnable, "A device with motors was left without force feedback.");
			Assert.AreEqual("", on.LeftMotorStrength ?? "", "The row's strength, set while force feedback was off, was kept.");
			Assert.AreEqual(SettingName.DefaultLeftMotorPeriod, on.LeftMotorPeriod);
			Assert.AreEqual(SettingName.DefaultRightMotorPeriod, on.RightMotorPeriod);
			Assert.AreEqual("2", on.ButtonA);
			Assert.AreEqual(on.CleanAndGetCheckSum(), on.PadSettingChecksum);
			var still = X360ceDatabase.AutoSetting(device, false, true, out note, quiet, sdlDb);
			Assert.AreEqual("", still.ForceEnable ?? "", "A device without motors was given force feedback.");
			// A row that switches force feedback on keeps its own values.
			var loud = SdlDatabase.Merge(SdlSource.Parse("030000005e0400008e02000000000000,Xbox 360 Pad,users:9,A:2,UseForceFeedback:1,LeftMotorPeriod:60,platform:Windows,", SdlSourceKind.BuiltIn));
			Assert.AreEqual("60", X360ceDatabase.AutoSetting(device, true, true, out note, loud, sdlDb).LeftMotorPeriod);
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The built-in file reads as the database: its header counts its rows, and every row is a controller's setting")]
		public void Built_in_file_is_the_database()
		{
			var text = X360ceDatabase.ReadBuiltIn();
			var said = text.Split('\n').First(x => x.StartsWith("# Rows: ")).Substring(8);
			var source = SdlSource.Parse(text, SdlSourceKind.BuiltIn);
			Assert.AreEqual(int.Parse(said, CultureInfo.InvariantCulture), source.Mappings.Count);
			Assert.IsTrue(source.Mappings.Count > 500, "The built-in database holds " + source.Mappings.Count + " rows.");
			Assert.IsNotNull(X360ceDatabase.MadeOn(text));
			foreach (var row in source.Mappings)
				Assert.IsTrue(X360ceDatabase.Users(row) > 0 && X360ceDatabase.ToPadSetting(row).PadSettingChecksum != Guid.Empty, row.ToString());
			var wheel = new SdlDevice { VendorId = 0x046D, ProductId = 0xC29B, ProductName = "Logitech G27 Racing Wheel USB" };
			Assert.IsNotNull(X360ceDatabase.Current.Find(wheel), "The G27 has no row.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The Most Popular lists show one preset for each row, the most used first, and load a copy of its setting by checksum")]
		public void Most_popular_lists_come_from_the_database()
		{
			X360ceDatabase.Reload();
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var presets = X360ceDatabase.Presets();
			Console.WriteLine("{0} presets in {1} ms.", presets.Length, watch.ElapsedMilliseconds);
			Assert.AreEqual(X360ceDatabase.Current.Mappings.Count, presets.Length);
			var users = presets.Select(x => x.Users).ToArray();
			CollectionAssert.AreEqual(users.OrderByDescending(x => x).ToArray(), users, "The most used are not first.");
			var g27 = presets.First(x => x.ProductName == "Logitech G27 Racing Wheel USB");
			var ps = X360ceDatabase.GetSetting(g27.PadSettingChecksum);
			Assert.AreEqual(g27.PadSettingChecksum, ps.CleanAndGetCheckSum(), "The setting is not the one the preset names.");
			ps.ButtonA = "99";
			Assert.AreNotEqual("99", X360ceDatabase.GetSetting(g27.PadSettingChecksum).ButtonA, "Changing a loaded setting changed the database's.");
			Assert.IsNull(X360ceDatabase.GetSetting(Guid.NewGuid()));
		}

		/// <summary>The settings database's answer (x360ce_GetControllerDatabase) made from the recorded presets.</summary>
		static DataSet RecordedAnswer()
		{
			var ds = new DataSet();
			var products = ds.Tables.Add();
			products.Columns.Add("ProductGuid", typeof(Guid));
			products.Columns.Add("ProductName", typeof(string));
			products.Columns.Add("PadSettingChecksum", typeof(Guid));
			products.Columns.Add("Users", typeof(int));
			var settings = ds.Tables.Add();
			var properties = typeof(PadSetting).GetProperties().Where(p => p.PropertyType == typeof(string) || p.Name == nameof(PadSetting.PadSettingChecksum)).ToArray();
			foreach (var p in properties)
				settings.Columns.Add(p.Name, p.PropertyType);
			foreach (var e in Recorded())
			{
				products.Rows.Add(e.ProductGuid, e.ProductName, e.Setting.PadSettingChecksum, e.Users);
				if (settings.Rows.Cast<DataRow>().Any(r => (Guid)r[nameof(PadSetting.PadSettingChecksum)] == e.Setting.PadSettingChecksum))
					continue;
				settings.Rows.Add(properties.Select(p => p.GetValue(e.Setting, null) ?? "").ToArray());
			}
			return ds;
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The server's cache serves the last good answer while it builds and after a failed build, and tries a failed build again only an hour on")]
		public void Server_cache_serves_the_last_answer_while_building_and_after_a_failure()
		{
			var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
			var gate = new System.Threading.ManualResetEventSlim(true);
			var fail = false;
			var calls = 0;
			var cache = new DailyCache<string>(served =>
			{
				var call = System.Threading.Interlocked.Increment(ref calls);
				gate.Wait();
				if (fail)
					throw new InvalidOperationException("The settings database is down.");
				return "built " + call;
			}, () => now);
			Assert.AreEqual("built 1", cache.Get());
			// A day on, a slow build: another request gets the last answer at once instead of waiting.
			now = now.AddDays(1);
			gate.Reset();
			var slow = System.Threading.Tasks.Task.Run(() => cache.Get());
			while (System.Threading.Volatile.Read(ref calls) < 2)
				System.Threading.Thread.Sleep(1);
			Assert.AreEqual("built 1", cache.Get(), "A request waited for another's build.");
			gate.Set();
			Assert.AreEqual("built 2", slow.Result);
			// A failed build keeps the last answer and is tried again only an hour on, not at every request.
			now = now.AddDays(1);
			fail = true;
			Assert.AreEqual("built 2", cache.Get());
			Assert.AreEqual("built 2", cache.Get());
			Assert.AreEqual(3, calls, "A failed build was tried again at the next request.");
			now = now.AddMinutes(59);
			Assert.AreEqual("built 2", cache.Get());
			Assert.AreEqual(3, calls);
			now = now.AddMinutes(1);
			fail = false;
			Assert.AreEqual("built 4", cache.Get());
			// Nothing built yet and the database down: nothing to serve, and no build at every request.
			var down = new DailyCache<string>(served => { System.Threading.Interlocked.Increment(ref calls); throw new InvalidOperationException("Down."); });
			var before = calls;
			Assert.IsNull(down.Get());
			Assert.IsNull(down.Get());
			Assert.AreEqual(before + 1, calls, "A failed first build was tried again at the next request.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("The server builds the file in memory at most once a day, keeps its ETag while the rows stay the same, builds one at a time, and answers 304 to a program that has it")]
		public void Server_builds_once_a_day_and_answers_304()
		{
			var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
			var fail = false;
			var users = 0;
			var server = new X360ceDatabaseServer(() =>
			{
				if (fail)
					throw new InvalidOperationException("The settings database is down.");
				var entries = X360ceDatabaseServer.Entries(RecordedAnswer());
				entries[0].Users += users;
				return X360ceDatabase.Write(entries, "a test", now);
			}, () => now);
			Assert.IsNotNull(server.Get(), "The first request after a start built nothing.");
			var file = server.Get();
			Assert.AreEqual(1, server.Builds);
			var source = SdlSource.Parse(System.Text.Encoding.UTF8.GetString(file.Bytes), SdlSourceKind.Downloaded);
			Assert.IsTrue(source.Mappings.Count >= 30, "The server's file holds " + source.Mappings.Count + " rows.");
			now = now.AddHours(23);
			Assert.AreSame(file, server.Get(), "The file was built again within the day.");
			Assert.AreEqual(1, server.Builds);
			// A day on, it is built again; when the settings database cannot be read, the older file is served.
			now = now.AddHours(2);
			fail = true;
			Assert.AreSame(file, server.Get());
			Assert.AreEqual(1, server.Builds);
			fail = false;
			Assert.AreSame(file, server.Get());
			Assert.AreEqual(1, server.Builds, "A failed build was tried again at the next request.");
			now = now.AddHours(1);
			// Built on another day with the same rows: the same file, ETag and date, so nobody is sent it again.
			Assert.AreSame(file, server.Get(), "A build with the same rows replaced the file.");
			Assert.AreEqual(2, server.Builds);
			// Rows that changed make a new file.
			now = now.AddDays(1);
			users = 1;
			var changed = server.Get();
			Assert.AreEqual(3, server.Builds);
			Assert.AreNotEqual(file.ETag, changed.ETag);
			Assert.IsTrue(changed.LastModified > file.LastModified);
			StringAssert.Contains(System.Text.Encoding.UTF8.GetString(changed.Bytes), "# Made: " + now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			// The ETag is the content's hash.
			var again = new X360ceDatabaseServer(() => System.Text.Encoding.UTF8.GetString(changed.Bytes), () => now).Get();
			Assert.AreEqual(changed.ETag, again.ETag);
			// One build at a time: requests that come together wait for the one build.
			var slow = new X360ceDatabaseServer(() => { System.Threading.Thread.Sleep(200); return "# x360ce's controller database\n"; });
			var answers = System.Threading.Tasks.Task.WhenAll(Enumerable.Range(0, 8).Select(i => System.Threading.Tasks.Task.Run(() => slow.Get()))).Result;
			Assert.AreEqual(1, slow.Builds, "Requests that came together built the file more than once.");
			Assert.IsTrue(answers.All(x => x == answers[0] && x != null));
			// The conditional request.
			file = changed;
			Assert.AreEqual(304, X360ceDatabaseServer.Status(file, file.ETag, null));
			Assert.AreEqual(304, X360ceDatabaseServer.Status(file, "\"other\", " + file.ETag, null));
			Assert.AreEqual(200, X360ceDatabaseServer.Status(file, "\"other\"", file.LastModified.ToString("R")));
			Assert.AreEqual(304, X360ceDatabaseServer.Status(file, null, file.LastModified.ToString("R")));
			Assert.AreEqual(200, X360ceDatabaseServer.Status(file, null, file.LastModified.AddSeconds(-1).ToString("R")));
			Assert.AreEqual(200, X360ceDatabaseServer.Status(file, null, null));
			StringAssert.StartsWith(file.ETag, "\"");
		}
	}
}
