// @under-test: Data/dbo/Tables/x360ce_PadSettings.sql, Engine/Data/Model/dbo/PadSetting.cs, Engine/Common/MapExpression.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A mapping is written by the parser, described by the class, and kept in a column, and all three
	/// have to agree about how long one may be. These tests are what makes them agree.
	/// </summary>
	/// <remarks>
	/// The failure this prevents is silent. A parser cap larger than the column lets an expression pass
	/// every check and then be cut short on the way to storage, so the person who wrote it is told
	/// nothing and finds out later, in a game, through a control that no longer does what they set. A
	/// class that still says sixteen fails at the same point for the same reason. Nothing at run time
	/// notices any of this, so it has to be noticed here.
	///
	/// The backup schema under Change Scripts is deliberately not checked. It is written out from the
	/// live database rather than read into it, so it cannot drift from a schema it is a copy of, and it
	/// is not in the repository for a test to find.
	/// </remarks>
	[TestClass]
	public class PadSettingsSchemaTest
	{

		/// <summary>
		/// The columns that hold a mapping, taken from the parser's own side of the contract.
		/// </summary>
		/// <remarks>
		/// Every value of <see cref="MapCode"/> names a column, and only those columns hold a mapping;
		/// the rest of the table holds numbers. Listing them here by hand would be a fourth place to
		/// keep in step, which is the very problem these tests exist to catch.
		/// </remarks>
		private static IEnumerable<string> MappingColumns()
		{
			return Enum.GetNames(typeof(MapCode)).Where(x => x != "None");
		}

		private static string Read(string relativePath)
		{
			var path = Path.Combine(Ui.RepoRoot.FullName, relativePath);
			Assert.IsTrue(File.Exists(path), "Missing: " + relativePath);
			return File.ReadAllText(path);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The parser's length cap is the width of the column an expression is stored in")]
		public void Length_cap_matches_the_column_it_is_stored_in()
		{
			var sql = Read("Data/dbo/Tables/x360ce_PadSettings.sql");
			foreach (var column in MappingColumns())
			{
				var declaration = new Regex(@"\[" + column + @"\]\s+VARCHAR \((\d+)\)", RegexOptions.IgnoreCase);
				var match = declaration.Match(sql);
				Assert.IsTrue(match.Success, "No VARCHAR column named " + column + " in the table.");
				Assert.AreEqual(MapExpression.MaxLength, int.Parse(match.Groups[1].Value),
					"Column " + column + " and MapExpression.MaxLength disagree. An expression that " +
					"passes validation would be cut short on its way into this column.");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The class generated from the database describes the mapping columns at their real width")]
		public void Model_describes_the_columns_at_their_real_width()
		{
			// The web service's context saves through the class's [StringLength], so a wrong one fails the save.
			foreach (var column in MappingColumns())
			{
				var property = typeof(x360ce.Engine.Data.PadSetting).GetProperty(column);
				Assert.IsNotNull(property, "PadSetting has no property named " + column + ".");
				var length = (StringLengthAttribute)Attribute.GetCustomAttribute(property, typeof(StringLengthAttribute));
				Assert.AreEqual(MapExpression.MaxLength, length?.MaximumLength,
					"PadSetting." + column + " is described as " + length?.MaximumLength + " characters, but the " +
					"parser accepts " + MapExpression.MaxLength + ".");
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The busiest formula the application writes for itself fits in the column")]
		public void The_formula_the_application_writes_fits_in_the_column()
		{
			// Switching a fully tuned row over to a formula writes this. If the longest thing the
			// application produces on its own does not fit, the limit is wrong however consistent it is.
			var seeded = MapExpressionSeed.FromSettings("a1",
				deadZone: 8000f, antiDeadZone: 9830f, linear: 100f,
				destinationMax: MapExpressionSeed.ThumbMax);
			Assert.IsTrue(seeded.Length <= MapExpression.MaxLength,
				"A row tuned in every way seeds a " + seeded.Length + " character formula, over the " +
				MapExpression.MaxLength + " allowed: " + seeded);
			// And it has to be an expression the parser will take back, not merely a short string.
			MapExpression parsed;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(seeded, out parsed, out error, out position),
				"The application seeded a formula it cannot itself read: " + error);
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("The class generated from the database carries every column of the table")]
		public void The_generated_class_carries_every_column()
		{
			// The web service's context maps the class by its [Table] and its properties, and the checksum and the
			// copies between records read the ones marked [DataMember]. A column the class lacks is one the table file
			// has and the database does not, or one added since Engine\Data\Generate-Models.ps1 last ran.
			var type = typeof(x360ce.Engine.Data.PadSetting);
			Assert.AreEqual("x360ce_PadSettings", ((TableAttribute)Attribute.GetCustomAttribute(type, typeof(TableAttribute)))?.Name);
			var columns = Regex.Matches(Read("Data/dbo/Tables/x360ce_PadSettings.sql"), @"^\s+\[(?<name>\w+)\]\s", RegexOptions.Multiline)
				.Cast<Match>().Select(x => x.Groups["name"].Value).ToArray();
			Assert.IsTrue(columns.Length > 50, "Only " + columns.Length + " column(s) were read from the table.");
			var missing = columns.Where(x =>
			{
				var p = type.GetProperty(x);
				return p == null || !Attribute.IsDefined(p, typeof(DataMemberAttribute)) || Attribute.IsDefined(p, typeof(NotMappedAttribute));
			}).ToArray();
			Assert.AreEqual(0, missing.Length, "The table has columns the class does not map, "
				+ "so the program never reads or writes them:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
			// Nothing sets this one, and its column refuses null, so a new setting has to start it empty.
			Assert.AreEqual("", new x360ce.Engine.Data.PadSetting().ButtonBig, "A new setting would save null into ButtonBig.");
		}

	}
}
