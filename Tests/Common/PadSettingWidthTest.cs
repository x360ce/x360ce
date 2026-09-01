// @under-test: Data/dbo/Tables/x360ce_PadSettings.sql, Engine/Data/x360ceModel.edmx
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// How much a setting is allowed to hold, said in three places, and whether they agree.
	/// </summary>
	/// <remarks>
	/// The width of a settings column is written down three times: in the table, in the storage half
	/// of the model, and in the half the program sees. Nothing checks that they match, and they fail
	/// in the least helpful way when they do not. The program half being the wider of the two means
	/// the program accepts a value the database then refuses, and the refusal arrives at whatever
	/// happens to be saving at the time rather than at whatever set the value.
	///
	/// This matters most while the columns are being brought down to the size of what they actually
	/// hold. Every one narrowed is three edits, and this says so when it was one or two.
	/// </remarks>
	[TestClass]
	public class PadSettingWidthTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
		}

		/// <summary>The part of the model describing one thing, so another is not read by mistake.</summary>
		static string Section(string edmx, string entityType)
		{
			var at = edmx.IndexOf("<EntityType Name=\"" + entityType + "\"", StringComparison.Ordinal);
			Assert.IsTrue(at >= 0, "The model no longer describes " + entityType + ".");
			var end = edmx.IndexOf("</EntityType>", at, StringComparison.Ordinal);
			return edmx.Substring(at, end - at);
		}

		/// <summary>The width each property allows, however the attributes happen to be ordered.</summary>
		/// <remarks>
		/// The model writes the same element two ways - the name before the type in some places and
		/// after it in others - so each element is read whole and its attributes picked out by name.
		/// Reading them in a fixed order finds only half of them, and silently: the missing half looks
		/// exactly like a column nobody declared.
		/// </remarks>
		static Dictionary<string, int> WidthsIn(string section)
		{
			var widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (Match element in Regex.Matches(section, "<Property[^>]*/>"))
			{
				var name = Regex.Match(element.Value, @"Name=""(?<v>\w+)""");
				var width = Regex.Match(element.Value, @"MaxLength=""(?<v>\d+)""");
				if (name.Success && width.Success)
					widths[name.Groups["v"].Value] = int.Parse(width.Groups["v"].Value);
			}
			return widths;
		}

		/// <summary>What the table allows each settings column to hold.</summary>
		static Dictionary<string, int> TableWidths()
		{
			var widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var sql = Read(Path.Combine("Data", "dbo", "Tables", "x360ce_PadSettings.sql"));
			foreach (Match m in Regex.Matches(sql, @"\[(?<name>\w+)\]\s+VARCHAR \((?<width>\d+)\)"))
				widths[m.Groups["name"].Value] = int.Parse(m.Groups["width"].Value);
			return widths;
		}

		/// <summary>What the storage half of the model allows.</summary>
		static Dictionary<string, int> StorageWidths(string edmx)
		{
			return WidthsIn(Section(edmx, "x360ce_PadSettings"));
		}

		/// <summary>What the half the program sees allows.</summary>
		static Dictionary<string, int> ProgramWidths(string edmx)
		{
			return WidthsIn(Section(edmx, "PadSetting"));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Every settings column is allowed to hold the same amount in all three places")]
		public void Every_column_allows_the_same_amount_everywhere()
		{
			var edmx = Read(Path.Combine("Engine", "Data", "x360ceModel.edmx"));
			var table = TableWidths();
			var storage = StorageWidths(edmx);
			var program = ProgramWidths(edmx);

			// Said out loud, because every check below passes on an empty list without a word.
			Assert.IsTrue(table.Count > 50, "Only " + table.Count + " column(s) were read from the table.");
			Assert.IsTrue(program.Count > 50, "Only " + program.Count + " setting(s) were read from the model.");

			var wrong = new List<string>();
			foreach (var column in table)
			{
				int declared;
				if (!storage.TryGetValue(column.Key, out declared))
					wrong.Add(column.Key + ": the table has it, the storage half of the model does not");
				else if (declared != column.Value)
					wrong.Add(string.Format("{0}: table allows {1}, storage half of the model allows {2}",
						column.Key, column.Value, declared));
				if (!program.TryGetValue(column.Key, out declared))
					// Only a warning in principle - a column the program never sees is allowed - but the
					// program half is generated from the same table, so in practice it means a missed edit.
					wrong.Add(column.Key + ": the table has it, the half the program sees does not");
				else if (declared != column.Value)
					wrong.Add(string.Format("{0}: table allows {1}, the program is told {2}{3}",
						column.Key, column.Value, declared,
						declared > column.Value
							? " - so the program accepts a value the database refuses"
							: " - so the program refuses a value already stored"));
			}
			Assert.AreEqual(0, wrong.Count,
				"A settings column is allowed to hold different amounts depending on which file is "
				+ "asked. Narrowing one is three edits:" + Environment.NewLine
				+ string.Join(Environment.NewLine, wrong.ToArray()));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A setting holding one character is not given a value with two")]
		public void A_setting_holding_one_character_is_not_given_a_longer_value()
		{
			// The force feedback pass-through settings hold a yes-or-no and a single digit, and the
			// column is that size. The trap is close by: the list of players this program already
			// offers elsewhere is built from an enum whose "any" member is 255, which is three
			// characters. Bind one of these to that list and the value is cut short by the database,
			// a long way from whatever chose it.
			var names = new Dictionary<string, string>
			{
				{ "ForcePassThrough", SettingName.ForcePassThrough },
				{ "ForcePassThroughIndex", SettingName.ForcePassThroughIndex },
			};
			var edmx = Read(Path.Combine("Engine", "Data", "x360ceModel.edmx"));
			var table = TableWidths();
			foreach (var name in names)
			{
				int width;
				Assert.IsTrue(table.TryGetValue(name.Key, out width),
					name.Key + " is not a column in the settings table.");
				Assert.AreEqual(1, width, name.Key + " no longer holds a single character.");
			}
			// And the defaults have to fit in it, or a setting could not be written at all.
			foreach (var name in names)
			{
				var setting = new x360ce.Engine.Data.PadSetting();
				typeof(x360ce.Engine.Data.PadSetting).GetProperty(name.Key).SetValue(setting, "0", null);
				Assert.AreEqual(Guid.Empty, setting.CleanAndGetCheckSum(),
					name.Key + " counts as set when it holds its default.");
			}
			Assert.IsTrue(ProgramWidths(edmx)["ForcePassThrough"] == 1
				&& StorageWidths(edmx)["ForcePassThrough"] == 1,
				"The model still lets the program write more than the column holds.");
		}

	}
}
