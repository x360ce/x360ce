// @under-test: Engine/Maps/SettingsConverter.cs, Engine/Common/MapExpression.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Expressions are shared through the same database that older versions read. Those versions know
	/// nothing about expressions, so the whole feature rests on them refusing a function cleanly rather
	/// than misreading it as a mapping. These tests pin that, because it is the claim that protects
	/// every user who has not updated.
	/// </summary>
	[TestClass]
	public class MapExpressionCompatibilityTest
	{

		/// <summary>Expressions as they will actually be stored, including the documented examples.</summary>
		private static readonly string[] Expressions =
		{
			"=a1*2",
			"=a1*abs(a1)",
			"=sign(a1)*a1",
			"=max(a1,0)",
			"=-min(a1,0)",
			"=a1-a2",
			"=a1*(0.5+a2*0.5)",
			"=clamp(a1*2,0,1)",
			"=a1-0.05",
		};

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The old parser refuses an expression rather than reading a source number out of it")]
		public void An_expression_is_not_mistaken_for_a_mapping()
		{
			// This is what makes an older version safe: TryParseIniValue fails, the index stays zero,
			// and zero already means unmapped everywhere in the engine. Were it to succeed and yield
			// some index, an older version would silently drive the wrong control from a shared
			// configuration, which is worse than ignoring it.
			foreach (var text in Expressions)
			{
				MapType type;
				int index;
				var parsed = SettingsConverter.TryParseIniValue(text, out type, out index);
				Assert.IsFalse(parsed,
					string.Format("'{0}' was read as a mapping by the old parser, giving {1} {2}.", text, type, index));
				Assert.AreEqual(0, index,
					string.Format("'{0}' left a source number behind, which an older version would act on.", text));
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The two parsers never both claim the same value")]
		public void A_value_belongs_to_exactly_one_parser()
		{
			// An overlap in either direction is a defect. A value both accept would mean the same
			// configuration behaves differently depending on which version opened it, silently.
			var mappings = new[] { "1", "12", "a1", "-2", "a-3", "s2", "x4", "h5", "p1", "d2" };
			foreach (var text in mappings)
			{
				MapType type;
				int index;
				Assert.IsTrue(SettingsConverter.TryParseIniValue(text, out type, out index),
					string.Format("'{0}' is a stored mapping the old parser should still read.", text));
				Assert.IsFalse(MapExpression.IsExpression(text),
					string.Format("'{0}' is a plain mapping but is being treated as an expression.", text));
			}
			foreach (var text in Expressions)
			{
				Assert.IsTrue(MapExpression.IsExpression(text),
					string.Format("'{0}' should be recognised as an expression.", text));
				MapType type;
				int index;
				Assert.IsFalse(SettingsConverter.TryParseIniValue(text, out type, out index),
					string.Format("'{0}' is claimed by both parsers.", text));
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Converting an expression for display yields nothing rather than a wrong mapping")]
		public void An_expression_has_no_display_form_in_the_old_parser()
		{
			// An older version shows this in a dropdown. Empty means the control shows nothing, which
			// is the visible symptom of a mapping it cannot represent - and the reason a save in that
			// version is expected to destroy the value.
			foreach (var text in Expressions)
				Assert.AreEqual("", SettingsConverter.FromIniValue(text),
					string.Format("'{0}' produced a display value in the old parser.", text));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A value the parser refuses is left exactly as it was found, never rewritten")]
		public void A_refused_expression_is_not_altered()
		{
			// Whatever else happens, reading must not change the stored text. A parser that trimmed,
			// lower-cased, or normalised on the way through would corrupt a configuration it could not
			// even use.
			foreach (var text in new[] { "=a1*2", "=NOT VALID", "=", "=((((", "=аbs(1)" })
			{
				var before = string.Copy(text);
				MapExpression result;
				string error;
				int position;
				MapExpression.TryParse(text, out result, out error, out position);
				Assert.AreEqual(before, text, "Parsing altered the value it was given.");
				MapType type;
				int index;
				SettingsConverter.TryParseIniValue(text, out type, out index);
				Assert.AreEqual(before, text, "The old parser altered the value it was given.");
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("An expression that parses keeps its text, so it can be written back unchanged")]
		public void A_parsed_expression_remembers_its_own_text()
		{
			// The stored text is what must go back to the database, not something rebuilt from the
			// tree. Rebuilding would rewrite everyone's expressions into this version's spelling.
			foreach (var text in Expressions)
			{
				MapExpression result;
				string error;
				int position;
				Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position), error);
				Assert.AreEqual(text, result.Text,
					"A parsed expression must carry the text it was written as.");
			}
		}

	}
}
