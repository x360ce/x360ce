// @under-test: Engine/Data/PadSetting.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The checksum that names a set of controller settings, and whether adding a setting to the
	/// program renames every set of settings ever saved.
	/// </summary>
	/// <remarks>
	/// A saved set of settings is known by the checksum of its own contents, and that checksum is the
	/// key it is stored under - here, in every shared preset, and in the database behind the cloud.
	/// Change what goes into the checksum and every stored set answers to a name nobody will ask for
	/// again. Settings people spent time on would still be there and would never be found.
	///
	/// So a new setting has to be invisible until somebody uses it. The way that is arranged is worth
	/// stating, because it is the whole safety of the thing: a value equal to its default is left out
	/// of the checksum entirely, and what remains is sorted before it is measured. Left out means the
	/// line cannot appear; sorted means a line that does appear cannot push another one anywhere.
	/// Two new settings, both off, therefore cannot change any answer that already exists.
	///
	/// These measure that against every preset the program ships, which is real data of the same shape
	/// as the cloud holds, rather than examples written to agree with the code.
	/// </remarks>
	[TestClass]
	public class PadSettingChecksumTest
	{
		/// <summary>The settings added after presets were first shared, which must stay invisible until used.</summary>
		static readonly string[] NewSettings = { "ForcePassThrough", "ForcePassThroughIndex", "ForceSpringEnable", "ForceSpringStrength", "WheelRange" };

		#region The presets this program ships, as settings

		/// <summary>The INI key each PadSetting property is written under.</summary>
		/// <remarks>
		/// Taken from the map the program itself uses, so a preset is read here exactly as the program
		/// reads it. The names differ in places - what the code calls GamePadType a file calls
		/// ControllerType - and a copy of that list would drift.
		/// </remarks>
		static Dictionary<string, string> KeyByProperty()
		{
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var settingType = typeof(PadSetting);
			foreach (var p in typeof(SettingName).GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				var target = settingType.GetProperty(p.Name);
				if (target == null || target.PropertyType != typeof(string) || !target.CanWrite)
					continue;
				var key = p.GetValue(null, null) as string;
				if (!string.IsNullOrEmpty(key))
					map[p.Name] = key;
			}
			return map;
		}

		/// <summary>The values in one section of an INI file.</summary>
		static Dictionary<string, string> ReadSection(string path)
		{
			var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			// The files are UTF-16 with a mark at the front, which the reader honours.
			foreach (var raw in File.ReadAllLines(path))
			{
				var line = raw.Trim();
				if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("["))
					continue;
				var at = line.IndexOf('=');
				if (at < 1)
					continue;
				values[line.Substring(0, at).Trim()] = line.Substring(at + 1).Trim();
			}
			return values;
		}

		/// <summary>Every preset the program ships, read into settings.</summary>
		static List<KeyValuePair<string, PadSetting>> Presets()
		{
			var folder = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Presets");
			var map = KeyByProperty();
			var list = new List<KeyValuePair<string, PadSetting>>();
			foreach (var file in Directory.GetFiles(folder, "*.ini").OrderBy(x => x))
			{
				var values = ReadSection(file);
				if (values.Count == 0)
					continue;
				var ps = new PadSetting();
				var filled = 0;
				foreach (var pair in map)
				{
					string value;
					if (!values.TryGetValue(pair.Value, out value) || value.Length == 0)
						continue;
					typeof(PadSetting).GetProperty(pair.Key).SetValue(ps, value, null);
					filled++;
				}
				if (filled > 0)
					list.Add(new KeyValuePair<string, PadSetting>(Path.GetFileNameWithoutExtension(file), ps));
			}
			return list;
		}

		/// <summary>A copy, so a checksum taken from one does not disturb the next.</summary>
		static PadSetting Copy(PadSetting from)
		{
			var to = new PadSetting();
			foreach (var p in typeof(PadSetting).GetProperties())
				if (p.PropertyType == typeof(string) && p.CanRead && p.CanWrite)
					// The properties refuse a null, and an unset one reads as null rather than empty.
					p.SetValue(to, p.GetValue(from, null) ?? "", null);
			return to;
		}

		#endregion

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The presets this program ships can be read, so the rest measures real data")]
		public void The_presets_this_program_ships_can_be_read()
		{
			// Said out loud rather than assumed. Every test below is worthless if the corpus is empty,
			// and an empty corpus passes every one of them without a word.
			var presets = Presets();
			Assert.IsTrue(presets.Count >= 15,
				"Only " + presets.Count + " preset(s) could be read, so the tests below are measuring " +
				"almost nothing. They are the real data this rests on.");
			foreach (var preset in presets)
				Assert.AreNotEqual(Guid.Empty, preset.Value.CleanAndGetCheckSum(),
					"Preset " + preset.Key + " read as entirely default, so nothing was actually read " +
					"out of it.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A new setting left alone is absent from the checksum of every shipped preset")]
		public void A_new_setting_left_alone_is_absent_from_every_preset()
		{
			// The one that matters. A line that never enters the measurement cannot change what comes
			// out of it, so this holds for every set of settings that exists, not only these.
			foreach (var preset in Presets())
			{
				var lines = new List<string>();
				preset.Value.CleanAndGetCheckSum(lines);
				foreach (var name in NewSettings)
					Assert.IsFalse(lines.Any(x => x.StartsWith(name + "=", StringComparison.Ordinal)),
						"Preset " + preset.Key + " puts " + name + " into its checksum while it is " +
						"switched off, so every set of settings ever saved has just been renamed and " +
						"none of them will be found again.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Off, empty and never set are one and the same to the checksum")]
		public void Off_empty_and_never_set_are_the_same()
		{
			// Three ways of saying the same nothing. A file written by an older version has no value at
			// all, one written by this one says "0", and both have to answer to the name the settings
			// were stored under.
			foreach (var preset in Presets())
			{
				var never = Copy(preset.Value).CleanAndGetCheckSum();
				foreach (var name in NewSettings)
					foreach (var quiet in new[] { "", "0" })
					{
						var ps = Copy(preset.Value);
						typeof(PadSetting).GetProperty(name).SetValue(ps, quiet, null);
						Assert.AreEqual(never, ps.CleanAndGetCheckSum(),
							"Preset " + preset.Key + " answers to a different name once " + name +
							" is written as \"" + quiet + "\", which is what it means when it is off.");
					}
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Settings with everything at its default have no name at all")]
		public void Settings_with_everything_default_have_no_name()
		{
			// Empty settings are stored under nothing, and adding a setting must not give them a name.
			Assert.AreEqual(Guid.Empty, new PadSetting().CleanAndGetCheckSum());
			foreach (var name in NewSettings)
			{
				var ps = new PadSetting();
				typeof(PadSetting).GetProperty(name).SetValue(ps, "0", null);
				Assert.AreEqual(Guid.Empty, ps.CleanAndGetCheckSum(),
					name + " switched off gives empty settings a name, so the empty set is no longer " +
					"recognised as empty.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A new setting actually used does change the name")]
		public void A_new_setting_actually_used_does_change_the_name()
		{
			// The other half. Invisible while off is only right if it counts once somebody turns it on -
			// otherwise two different sets of settings would share one name and one would overwrite the
			// other.
			foreach (var preset in Presets().Take(5))
			{
				var before = Copy(preset.Value).CleanAndGetCheckSum();
				var on = Copy(preset.Value);
				on.ForcePassThrough = "1";
				Assert.AreNotEqual(before, on.CleanAndGetCheckSum(),
					"Preset " + preset.Key + " keeps its name with force feedback pass-through turned " +
					"on, so those settings would be stored over the ones without it.");
				var place = Copy(preset.Value);
				place.ForcePassThrough = "1";
				place.ForcePassThroughIndex = "3";
				Assert.AreNotEqual(on.CleanAndGetCheckSum(), place.CleanAndGetCheckSum(),
					"Preset " + preset.Key + " keeps its name when the place the force is sent to " +
					"changes, so both would be stored as one.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The same settings always give the same name")]
		public void The_same_settings_always_give_the_same_name()
		{
			// It is a key, so it has to be the same every time it is asked for - including the second
			// time, after the first has tidied defaults away.
			foreach (var preset in Presets())
			{
				var first = preset.Value.CleanAndGetCheckSum();
				var second = preset.Value.CleanAndGetCheckSum();
				var third = Copy(preset.Value).CleanAndGetCheckSum();
				Assert.AreEqual(first, second, "Preset " + preset.Key + " changes its name when asked twice.");
				Assert.AreEqual(first, third, "Preset " + preset.Key + " changes its name when copied.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Two sets of settings share a name only when they are the same settings")]
		public void Two_sets_share_a_name_only_when_they_are_the_same_settings()
		{
			// A set of settings is known by its contents and nothing else, so two that hold the same
			// values are one set stored once, on purpose. Two of the shipped presets are exactly that:
			// the Driving Force GT and the G27 differ only in the name of the wheel, which belongs to
			// the preset and not to the settings, so they share a name and should.
			//
			// What must never happen is the other way round - two sets that differ being given one
			// name, because then one is stored over the other and somebody loses what they set up.
			var byName = new Dictionary<Guid, KeyValuePair<string, string>>();
			foreach (var preset in Presets())
			{
				var lines = new List<string>();
				var sum = preset.Value.CleanAndGetCheckSum(lines);
				var content = string.Join(", ", lines.OrderBy(x => x).ToArray());
				KeyValuePair<string, string> other;
				if (byName.TryGetValue(sum, out other))
					Assert.AreEqual(other.Value, content,
						"Presets " + other.Key + " and " + preset.Key + " hold different settings and " +
						"are stored under one name, so whichever is saved second replaces the first.");
				else
					byName[sum] = new KeyValuePair<string, string>(preset.Key, content);
			}
			Assert.IsTrue(byName.Count > 1, "Every preset read as the same settings, so this measured nothing.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The name does not depend on the order the settings were filled in")]
		public void The_name_does_not_depend_on_the_order_they_were_filled_in()
		{
			// The values are sorted before they are measured, which is what lets a setting be added in
			// the middle of the list without moving anything. Worth holding, because the day it stops
			// being true is the day adding a setting quietly renames everything.
			var properties = typeof(PadSetting).GetProperties()
				.Where(x => x.PropertyType == typeof(string) && x.CanRead && x.CanWrite).ToArray();
			foreach (var preset in Presets().Take(5))
			{
				var forwards = Copy(preset.Value).CleanAndGetCheckSum();
				var backwards = new PadSetting();
				foreach (var p in properties.Reverse())
					p.SetValue(backwards, p.GetValue(preset.Value, null) ?? "", null);
				Assert.AreEqual(forwards, backwards.CleanAndGetCheckSum(),
					"Preset " + preset.Key + " is given a different name depending on the order its " +
					"values were written, so the same settings would be stored twice.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The name does not depend on the computer's language")]
		public void The_name_does_not_depend_on_the_computers_language()
		{
			// The server names what it stores, and a program on a computer in another language must
			// arrive at the same name for the same settings. Sorting by the computer's language gave
			// ten languages another order: Welsh and Albanian read "th" as one letter, Lithuanian,
			// Latvian and Western Frisian sort "Y" with "I", Azerbaijani puts "X" after "H" and
			// Hawaiian puts vowels first. Every language Windows knows is tried.
			var mapping = new PadSetting
			{
				AxisToDPadDeadZone = "100", AxisToDPadEnabled = "1",
				ButtonA = "1", ButtonB = "2", ButtonX = "3", ButtonY = "4", ButtonBack = "7", ButtonStart = "8",
				ButtonBDeadZone = "100", LeftThumbAxisX = "1", LeftThumbAxisY = "2", LeftThumbAntiDeadZoneX = "2000",
				DPad = "1", LeftTrigger = "a-6", RightTrigger = "a-2",
			};
			var saved = System.Threading.Thread.CurrentThread.CurrentCulture;
			try
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
				var expected = Copy(mapping).CleanAndGetCheckSum();
				var differ = new List<string>();
				foreach (var culture in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures))
				{
					System.Threading.Thread.CurrentThread.CurrentCulture = culture;
					if (Copy(mapping).CleanAndGetCheckSum() != expected)
						differ.Add(culture.Name);
				}
				Assert.AreEqual(0, differ.Count,
					"The same settings are named differently on computers set to " + string.Join(", ", differ) +
					", so the server stores them twice and cannot find what those computers ask for.");
			}
			finally
			{
				System.Threading.Thread.CurrentThread.CurrentCulture = saved;
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The change script that renames stored settings measures them as the program does")]
		public void The_change_script_measures_as_the_program_does()
		{
			// The database renames its rows in SQL, from a list of settings, their defaults and their
			// order written into the script. A setting added here and not there, or put in another
			// order, would rename every stored row to a checksum no program asks for.
			var path = Path.Combine(Ui.RepoRoot.FullName, "Data", "Change Scripts", "2026-09-23_Rename_PadSettings_To_Current_Checksum.sql");
			var rows = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path),
				@"\(\s*(\d+), '(\w+)', p\.\[(\w+)\], '(\d+)'\)")
				.Cast<System.Text.RegularExpressions.Match>()
				.Select(m => new { Rank = int.Parse(m.Groups[1].Value), Name = m.Groups[2].Value, Column = m.Groups[3].Value, Default = m.Groups[4].Value })
				.ToList();
			// The names the program measures, found by giving every setting a value no default has.
			var all = new PadSetting();
			foreach (var p in typeof(PadSetting).GetProperties())
				if (p.PropertyType == typeof(string) && p.CanWrite)
					p.SetValue(all, "7", null);
			var lines = new List<string>();
			all.CleanAndGetCheckSum(lines);
			var names = lines.Select(x => x.Substring(0, x.IndexOf('='))).OrderBy(x => x, StringComparer.Ordinal).ToArray();
			CollectionAssert.AreEqual(names, rows.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
				"The script and the program measure different settings.");
			Assert.IsTrue(rows.All(x => x.Name == x.Column), "A line of the script reads another column than it names.");
			// The order: the program sorts whole lines, and no name is the start of another followed
			// by "=", so the order of "Name=" is the order of the lines.
			var order = rows.Select(x => x.Name + "=").OrderBy(x => x, StringComparer.InvariantCulture).ToArray();
			CollectionAssert.AreEqual(order, rows.OrderBy(x => x.Rank).Select(x => x.Name + "=").ToArray(),
				"The script puts the lines in another order than the program.");
			CollectionAssert.AreEqual(Enumerable.Range(1, rows.Count).ToArray(), rows.Select(x => x.Rank).ToArray(),
				"The ranks are not 1, 2, 3 in the order written.");
			// The defaults: a setting at its script default must be left out, and any other value kept.
			foreach (var row in rows)
			{
				var property = typeof(PadSetting).GetProperty(row.Name);
				var atDefault = new PadSetting();
				property.SetValue(atDefault, row.Default, null);
				var kept = new List<string>();
				atDefault.CleanAndGetCheckSum(kept);
				Assert.AreEqual(0, kept.Count, row.Name + " at the script's default " + row.Default + " is measured by the program.");
				var changed = new PadSetting();
				property.SetValue(changed, row.Default == "0" ? "1" : "0", null);
				kept.Clear();
				changed.CleanAndGetCheckSum(kept);
				Assert.AreEqual(1, kept.Count, row.Name + " away from the script's default " + row.Default + " is not measured by the program.");
			}
			// And the whole measurement, made the script's way, against every shipped preset.
			using (var md5 = System.Security.Cryptography.MD5.Create())
			{
				foreach (var preset in Presets())
				{
					var ps = Copy(preset.Value);
					var text = string.Join("\r\n", rows.OrderBy(x => x.Rank)
						.Select(x => new { x.Name, x.Default, Value = (string)typeof(PadSetting).GetProperty(x.Name).GetValue(ps, null) ?? "" })
						.Where(x => x.Value.Length > 0 && x.Value != x.Default)
						.Select(x => x.Name + "=" + x.Value));
					var script = text.Length == 0 ? Guid.Empty : new Guid(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(text)));
					Assert.AreEqual(ps.CleanAndGetCheckSum(), script, "Preset " + preset.Key + " is named differently by the script.");
				}
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Every setting in the checksum can be read as text")]
		public void Every_setting_in_the_checksum_can_be_read_as_text()
		{
			// The checksum reads each value as text. A setting stored as a number or a yes-or-no would
			// be read the same way and would stop the program at the point it tried, which is why the
			// two new ones are text like every other - however much they look like a number and a
			// yes-or-no.
			var lines = new List<string>();
			var ps = new PadSetting();
			foreach (var p in typeof(PadSetting).GetProperties())
				if (p.PropertyType == typeof(string) && p.CanWrite)
					p.SetValue(ps, "1", null);
			ps.CleanAndGetCheckSum(lines);
			foreach (var name in NewSettings)
			{
				var property = typeof(PadSetting).GetProperty(name);
				Assert.IsNotNull(property, name + " is not a setting at all.");
				Assert.AreEqual(typeof(string), property.PropertyType,
					name + " is not text, and the checksum reads every value as text, so taking one " +
					"would stop the program.");
				Assert.IsTrue(lines.Any(x => x.StartsWith(name + "=", StringComparison.Ordinal)),
					name + " never reaches the checksum even when it is set, so two different sets of " +
					"settings would share one name.");
			}
		}

	}
}
