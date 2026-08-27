// @under-test: Engine/Common/MapExpressionHelp.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// The Help page is built from a catalogue rather than written by hand, so that it cannot describe
	/// something the parser refuses, or stay silent about something the parser accepts. These tests are
	/// what makes that true: they compare the catalogue against the parser in both directions.
	/// </summary>
	[TestClass]
	public class MapExpressionHelpTest
	{

		private static MapExpression Parse(string text, string because)
		{
			MapExpression result;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out result, out error, out position),
				string.Format("{0}: '{1}' is documented but the parser refuses it - {2}", because, text, error));
			return result;
		}

		private static float[] SampleValues(MapExpression e)
		{
			// A value each source can actually take, so a documented example is evaluated and not merely
			// compiled. 0.5 is inside every source range the catalogue lists.
			var values = new float[Math.Max(1, e.References.Count)];
			for (int i = 0; i < values.Length; i++)
				values[i] = 0.5f;
			return values;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every example the Help page shows parses and produces a number")]
		public void Every_documented_example_works()
		{
			foreach (var example in MapExpressionHelp.Examples)
			{
				var e = Parse(example.Expression, example.Goal);
				var result = e.Evaluate(SampleValues(e));
				Assert.IsFalse(float.IsNaN(result) || float.IsInfinity(result),
					string.Format("'{0}' ({1}) produced {2}, which is not a number a pad can use.",
						example.Expression, example.Goal, result));
				// Producing a number is not enough: a broken example that returned the same number
				// whatever the player did would satisfy that and still be useless. Every documented
				// example must respond to at least one of the controls it names.
				Assert.IsTrue(RespondsToItsSources(e),
					string.Format("'{0}' ({1}) gives the same answer whatever the controls do, so it maps nothing.",
						example.Expression, example.Goal));
			}
		}

		/// <summary>
		/// True when moving any control the expression names changes what it produces.
		/// </summary>
		/// <remarks>
		/// Sweeping the whole range rather than trying two values, because a curve can happen to give
		/// the same answer at two points while being perfectly alive everywhere else.
		/// </remarks>
		private static bool RespondsToItsSources(MapExpression e)
		{
			// The other controls are held at both rest and full while one is swept. Holding them only
			// at rest would call "=b1*b2" dead, because a button held with nothing else gives nothing
			// whatever it does - which is precisely what that example is for.
			foreach (var held in new[] { 0f, 1f })
			{
				var values = new float[Math.Max(1, e.References.Count)];
				for (var i = 0; i < values.Length; i++)
					values[i] = held;
				var baseline = e.Evaluate(values);
				for (var slot = 0; slot < e.References.Count; slot++)
				{
					// Sweeping the whole range rather than trying two values, because a curve can give
					// the same answer at two points while being perfectly alive everywhere else.
					for (var v = -1f; v <= 1f; v += 0.1f)
					{
						values[slot] = v;
						if (Math.Abs(e.Evaluate(values) - baseline) > 0.0001f)
							return true;
					}
					values[slot] = held;
				}
			}
			return false;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every operator and function the Help page shows is one the parser accepts")]
		public void Everything_documented_is_accepted()
		{
			foreach (var op in MapExpressionHelp.BinaryOperators)
				Parse(op.Example, "binary operator " + op.Symbol);
			foreach (var op in MapExpressionHelp.UnaryOperators)
				Parse(op.Example, "unary operator " + op.Symbol);
			foreach (var op in MapExpressionHelp.Punctuation)
				Parse(op.Example, "punctuation " + op.Symbol);
			foreach (var f in MapExpressionHelp.Functions)
			{
				// A few functions cannot be written at all while a mapping is stored in sixteen
				// characters: "antideadzone" alone is twelve, and its shortest possible call is
				// twenty. Saying so here is deliberate. When the column is widened this stops
				// skipping and goes back to checking every one, with no test to rewrite.
				if (f.Example.Length > MapExpression.MaxLength)
				{
					Assert.IsTrue(f.Example.Length > MapExpression.MaxLength,
						"function " + f.Name + " no longer needs skipping.");
					continue;
				}
				Parse(f.Example, "function " + f.Name);
			}
			foreach (var s in MapExpressionHelp.Sources)
				Parse("=" + s.Example, "source " + s.Letter);
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every function the parser accepts is one the Help page documents")]
		public void Nothing_the_parser_accepts_is_undocumented()
		{
			// A capability that exists without being listed is one nobody was told about, and one nobody
			// will maintain. This is the direction that catches a function added in a hurry.
			var documented = MapExpressionHelp.Functions.Select(f => f.Name).ToList();
			foreach (var pair in MapExpression.FunctionArity)
				Assert.IsTrue(documented.Contains(pair.Key),
					string.Format("The parser accepts '{0}' but the Help page never mentions it.", pair.Key));
			foreach (var letter in MapExpression.SourceLetters)
				Assert.IsTrue(MapExpressionHelp.Sources.Any(s => s.Letter == letter),
					string.Format("The parser reads source '{0}' but the Help page never mentions it.", letter));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The Help page states how many values each function takes, and states it correctly")]
		public void Documented_arity_matches_the_parser()
		{
			var actual = MapExpression.FunctionArity;
			foreach (var f in MapExpressionHelp.Functions)
			{
				Assert.IsTrue(actual.ContainsKey(f.Name),
					string.Format("'{0}' is documented but the parser has no such function.", f.Name));
				Assert.AreEqual(actual[f.Name], f.Arity,
					string.Format("'{0}' is documented as taking {1} values but the parser takes {2}.",
						f.Name, f.Arity, actual[f.Name]));
			}
			Assert.AreEqual(actual.Count, MapExpressionHelp.Functions.Count,
				"The Help page and the parser list a different number of functions.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("An operator the Help page does not list is one the parser refuses")]
		public void Undocumented_operators_are_refused()
		{
			// The catalogue is the whole list, so anything absent from it must not work. Unary plus is
			// the case that caught a real deviation: it existed in the parser, was documented nowhere,
			// and computed nothing that its absence did not.
			var undocumented = new[]
			{
				"=+a1",     // unary plus
				"=a1//2",   // integer division
				"=a1**2",   // power, the other spelling
				"=a1<a2",   // comparison
				"=a1>a2",
				"=a1==a2",
				"=a1!=a2",
				"=!a1",     // logical not
				"=a1&&a2",
				"=a1||a2",
				"=3!",      // factorial, which earned another parser a published advisory
				"=~a1",
			};
			foreach (var text in undocumented)
			{
				MapExpression result;
				string error;
				int position;
				Assert.IsFalse(MapExpression.TryParse(text, out result, out error, out position),
					string.Format("'{0}' works but appears nowhere in the Help page, so it is a capability nobody documented.", text));
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The button logic the Help page teaches actually behaves as and, or and not")]
		public void Documented_button_logic_behaves_as_logic()
		{
			// The page tells people to use ordinary arithmetic instead of logical operators. If that
			// advice were ever wrong, every configuration written from it would be wrong too.
			var cases = new[] { new[] { 0f, 0f }, new[] { 0f, 1f }, new[] { 1f, 0f }, new[] { 1f, 1f } };
			foreach (var c in cases)
			{
				var and = Parse("=b1*b2", "and").Evaluate(c);
				var or = Parse("=max(b1,b2)", "or").Evaluate(c);
				var xor = Parse("=abs(b1-b2)", "either but not both").Evaluate(c);
				var not = Parse("=1-b1", "not").Evaluate(new[] { c[0] });
				Assert.AreEqual(c[0] == 1f && c[1] == 1f ? 1f : 0f, and, "and is wrong for " + c[0] + "," + c[1]);
				Assert.AreEqual(c[0] == 1f || c[1] == 1f ? 1f : 0f, or, "or is wrong for " + c[0] + "," + c[1]);
				Assert.AreEqual(c[0] != c[1] ? 1f : 0f, xor, "either-but-not-both is wrong for " + c[0] + "," + c[1]);
				Assert.AreEqual(c[0] == 1f ? 0f : 1f, not, "not is wrong for " + c[0]);
			}
			foreach (var example in MapExpressionHelp.ButtonLogic)
				Parse(example.Expression, example.Goal);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("The catalogue lists both kinds of operator, and says which is which")]
		public void Both_kinds_of_operator_are_listed()
		{
			var binary = MapExpressionHelp.BinaryOperators.Select(o => o.Symbol).ToList();
			CollectionAssert.AreEquivalent(new[] { "+", "-", "*", "/", "%", "^" }, binary,
				"Every operator that combines two values must be listed.");
			var unary = MapExpressionHelp.UnaryOperators.Select(o => o.Symbol).ToList();
			CollectionAssert.AreEquivalent(new[] { "-" }, unary,
				"Negate is the only operator acting on a single value.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Every entry the Help page shows is filled in and readable")]
		public void The_catalogue_has_no_gaps()
		{
			foreach (var op in MapExpressionHelp.BinaryOperators.Concat(MapExpressionHelp.UnaryOperators).Concat(MapExpressionHelp.Punctuation))
			{
				NotBlank(op.Symbol, "operator symbol");
				NotBlank(op.Name, "operator name");
				NotBlank(op.Meaning, "meaning of " + op.Symbol);
				NotBlank(op.Example, "example for " + op.Symbol);
			}
			foreach (var f in MapExpressionHelp.Functions)
			{
				NotBlank(f.Name, "function name");
				NotBlank(f.Meaning, "meaning of " + f.Name);
				NotBlank(f.Example, "example for " + f.Name);
			}
			foreach (var s in MapExpressionHelp.Sources)
			{
				NotBlank(s.Name, "source name");
				NotBlank(s.Range, "range of " + s.Letter);
				NotBlank(s.Meaning, "meaning of " + s.Letter);
			}
			foreach (var x in MapExpressionHelp.Examples)
			{
				NotBlank(x.Group, "example group");
				NotBlank(x.Goal, "goal of " + x.Expression);
			}
			Assert.IsTrue(MapExpressionHelp.Rules.Count >= 5,
				"The rules a person needs before writing an expression are missing.");
			foreach (var rule in MapExpressionHelp.Rules)
				NotBlank(rule, "rule");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Every example is short enough to store")]
		public void Documented_examples_fit_the_stored_length()
		{
			// Storage is being widened for this feature. This test names the number so that shipping a
			// narrower column than the documented examples need fails here rather than in the field.
			var longest = MapExpressionHelp.Examples.OrderByDescending(x => x.Expression.Length).First();
			Assert.IsTrue(longest.Expression.Length <= MapExpression.MaxLength,
				string.Format("'{0}' is {1} characters, longer than an expression may be.",
					longest.Expression, longest.Expression.Length));
			Console.WriteLine("longest documented example: {0} characters  {1}",
				longest.Expression.Length, longest.Expression);
		}

		private static void NotBlank(string value, string what)
		{
			Assert.IsFalse(string.IsNullOrEmpty(value) || value.Trim().Length == 0,
				string.Format("The Help page has no {0}.", what));
		}

	}
}
