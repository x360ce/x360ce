// @under-test: Data/dbo/Tables/x360ce_PadSettings.sql, Engine/Data/x360ceModel.edmx, Engine/Data/x360ceModel.csdl, Engine/Data/x360ceModel.ssdl, Engine/Data/x360ceModel.msl, Engine/Common/MapExpression.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// A mapping is written by the parser, described by the model, and kept in a column, and all three
	/// have to agree about how long one may be. These tests are what makes them agree.
	/// </summary>
	/// <remarks>
	/// The failure this prevents is silent. A parser cap larger than the column lets an expression pass
	/// every check and then be cut short on the way to storage, so the person who wrote it is told
	/// nothing and finds out later, in a game, through a control that no longer does what they set. A
	/// model that still says sixteen fails at the same point for the same reason. Nothing at run time
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
		[Description("Both layers of the model describe the mapping columns at their real width")]
		public void Model_describes_the_columns_at_their_real_width()
		{
			var model = XDocument.Parse(Read("Engine/Data/x360ceModel.edmx"));
			// The storage layer names the table and the conceptual layer names the class. Both carry
			// their own length, and either one being wrong fails the save.
			foreach (var entityName in new[] { "x360ce_PadSettings", "PadSetting" })
			{
				var entity = model.Descendants()
					.FirstOrDefault(x => x.Name.LocalName == "EntityType"
						&& (string)x.Attribute("Name") == entityName);
				Assert.IsNotNull(entity, "The model has no entity named " + entityName + ".");
				foreach (var column in MappingColumns())
				{
					var property = entity.Elements()
						.FirstOrDefault(x => x.Name.LocalName == "Property"
							&& (string)x.Attribute("Name") == column);
					Assert.IsNotNull(property, entityName + " has no property named " + column + ".");
					var length = (string)property.Attribute("MaxLength");
					Assert.AreEqual(MapExpression.MaxLength.ToString(), length,
						entityName + "." + column + " is described as " + length + " characters, but the " +
						"parser accepts " + MapExpression.MaxLength + ".");
				}
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
		[Description("The model the engine runs from loads, and both of its halves carry every column of the table")]
		public void The_embedded_model_loads_and_carries_every_column()
		{
			// The engine runs from the three files embedded in it, not from the .edmx beside them, and
			// Entity Framework reads them together on the first query. A mapping naming a property or a
			// column the other two lack fails there, in a program that is saving; here it fails in a test.
			var engine = typeof(EngineHelper).Assembly;
			Func<string, System.Xml.XmlReader> open = name => System.Xml.XmlReader.Create(engine.GetManifestResourceStream(name));
			var conceptual = new System.Data.Metadata.Edm.EdmItemCollection(new[] { open("Data.x360ceModel.csdl") });
			var storage = new System.Data.Metadata.Edm.StoreItemCollection(new[] { open("Data.x360ceModel.ssdl") });
			new System.Data.Mapping.StorageMappingItemCollection(conceptual, storage, new[] { open("Data.x360ceModel.msl") });
			var table = storage.GetItems<System.Data.Metadata.Edm.EntityType>().Single(x => x.Name == "x360ce_PadSettings");
			var entity = conceptual.GetItems<System.Data.Metadata.Edm.EntityType>().Single(x => x.Name == "PadSetting");
			var columns = Regex.Matches(Read("Data/dbo/Tables/x360ce_PadSettings.sql"), @"^\s+\[(?<name>\w+)\]\s", RegexOptions.Multiline)
				.Cast<Match>().Select(x => x.Groups["name"].Value).ToArray();
			Assert.IsTrue(columns.Length > 50, "Only " + columns.Length + " column(s) were read from the table.");
			var missing = columns.Where(x => !table.Properties.Contains(x)).Select(x => "storage: " + x)
				.Concat(columns.Where(x => !entity.Properties.Contains(x)).Select(x => "program: " + x)).ToArray();
			Assert.AreEqual(0, missing.Length, "The table has columns the embedded model does not describe, "
				+ "so the program never reads or writes them:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
			// Nothing sets this one, and its column refuses null, so a new setting has to start it empty.
			Assert.AreEqual("", new x360ce.Engine.Data.PadSetting().ButtonBig, "A new setting would save null into ButtonBig.");
		}

	}
}
