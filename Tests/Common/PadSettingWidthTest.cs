// @under-test: Data/dbo/Tables/x360ce_PadSettings.sql, Engine/Data/Model/dbo/PadSetting.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// How much a setting is allowed to hold, said in two places, and whether they agree.
	/// </summary>
	/// <remarks>
	/// The width of a settings column is written down twice: in the table file of the SQL project, and in the
	/// [StringLength] of the class generated from the live database (Engine\Data\Generate-Models.ps1). Nothing else
	/// checks that they match, and they fail in the least helpful way when they do not. The class being the wider of
	/// the two means the program accepts a value the database then refuses, and the refusal arrives at whatever
	/// happens to be saving at the time rather than at whatever set the value.
	///
	/// This matters most while the columns are being brought down to the size of what they actually hold. Every one
	/// narrowed is a change script, the table file and the classes generated again, and this says so when a step is
	/// missing.
	/// </remarks>
	[TestClass]
	public class PadSettingWidthTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
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

		/// <summary>What the class generated from the database allows each setting to hold.</summary>
		static Dictionary<string, int> ProgramWidths()
		{
			var widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in typeof(x360ce.Engine.Data.PadSetting).GetProperties())
			{
				var length = (StringLengthAttribute)Attribute.GetCustomAttribute(p, typeof(StringLengthAttribute));
				if (length != null)
					widths[p.Name] = length.MaximumLength;
			}
			return widths;
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Every settings column is allowed to hold the same amount in the table and in the class")]
		public void Every_column_allows_the_same_amount_everywhere()
		{
			var table = TableWidths();
			var program = ProgramWidths();

			// Said out loud, because every check below passes on an empty list without a word.
			Assert.IsTrue(table.Count > 50, "Only " + table.Count + " column(s) were read from the table.");
			Assert.IsTrue(program.Count > 50, "Only " + program.Count + " setting(s) were read from the class.");

			var wrong = new List<string>();
			foreach (var column in table)
			{
				int declared;
				if (!program.TryGetValue(column.Key, out declared))
					// The class is generated from the database, so a column it lacks is one the table file has and the
					// database does not, or one added since the classes were generated.
					wrong.Add(column.Key + ": the table has it, the class does not");
				else if (declared != column.Value)
					wrong.Add(string.Format("{0}: table allows {1}, the program is told {2}{3}",
						column.Key, column.Value, declared,
						declared > column.Value
							? " - so the program accepts a value the database refuses"
							: " - so the program refuses a value already stored"));
			}
			Assert.AreEqual(0, wrong.Count,
				"A settings column is allowed to hold different amounts depending on which file is asked. "
				+ "The database is the source: change it with a change script, then the table file, then run "
				+ "Engine\\Data\\Generate-Models.ps1:" + Environment.NewLine
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
			Assert.AreEqual(1, ProgramWidths()["ForcePassThrough"],
				"The class still lets the program write more than the column holds.");
		}

	}
}
