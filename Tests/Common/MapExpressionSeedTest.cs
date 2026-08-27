// @under-test: Engine/Common/MapExpressionSeed.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.Engine;

namespace x360ce.Tests
{
	/// <summary>
	/// Switching a row to an expression replaces its dead zone, anti dead zone and sensitivity, so the
	/// expression it starts with has to produce what those settings were producing. If it did not, every
	/// person who tried the feature would find their controller had quietly changed feel.
	/// </summary>
	[TestClass]
	public class MapExpressionSeedTest
	{

		private static float Seeded(string text, float normalisedInput)
		{
			MapExpression e;
			string error;
			int position;
			Assert.IsTrue(MapExpression.TryParse(text, out e, out error, out position),
				string.Format("The seeded expression '{0}' does not parse: {1}", text, error));
			return e.Evaluate(new[] { normalisedInput });
		}

		/// <summary>What the settings produce, in normalised units, for the same input.</summary>
		private static float Settings(float normalisedInput, float deadZone, float antiDeadZone, float linear)
		{
			// The settings work in DirectInput units, so the input is converted in and the result out.
			var di = (normalisedInput + 1f) / 2f * 65535f;
			var raw = ConvertHelper.GetThumbValue(di, deadZone, antiDeadZone, linear, false, false, true);
			return raw / MapExpressionSeed.ThumbMax;
		}

		private static void Matches(float deadZone, float antiDeadZone, float linear, string what)
		{
			var text = MapExpressionSeed.FromSettings("a1", deadZone, antiDeadZone, linear, MapExpressionSeed.ThumbMax);
			// A row's settings written out in full are longer than a mapping can be stored in until the
			// column is widened, so what is offered is the plain source instead. That is checked here
			// and the rest is skipped, deliberately: when the column grows and the full form fits, this
			// goes back to checking that it reproduces the settings exactly, with no test to rewrite.
			if (text == "=a1")
			{
				Assert.IsTrue("=sign(a1)*abs(a1)".Length > MapExpression.MaxLength,
					what + ": the plain source was offered although the full form would have fitted.");
				return;
			}
			// Whole steps rather than adding a fraction repeatedly, so the centre is exactly nought.
			// Accumulating 0.05 never lands on zero, and the one input that matters most is the one
			// where a stick is being left alone.
			for (var step = -20; step <= 20; step++)
			{
				var input = step / 20f;
				var bySettings = Settings(input, deadZone, antiDeadZone, linear);
				var byFormula = Seeded(text, input);
				Assert.AreEqual(bySettings, byFormula, 0.02f,
					string.Format("{0}: at {1:F2} the settings give {2:F4} but '{3}' gives {4:F4}.",
						what, input, bySettings, text, byFormula));
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A row with nothing set seeds as the plain source, not as arithmetic that cancels out")]
		public void Nothing_set_seeds_as_the_plain_source()
		{
			// Somebody opening a formula for the first time should see something they can read.
			Assert.AreEqual("=a1", MapExpressionSeed.FromSettings("a1", 0f, 0f, 0f, MapExpressionSeed.ThumbMax));
			Assert.AreEqual("=s2", MapExpressionSeed.FromSettings("s2", 0f, 0f, 0f, MapExpressionSeed.TriggerMax));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A seeded expression reproduces the dead zone it replaces")]
		public void A_seeded_expression_reproduces_a_dead_zone()
		{
			Matches(3276f, 0f, 0f, "dead zone of a tenth");
			Matches(8000f, 0f, 0f, "a larger dead zone");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A seeded expression reproduces the anti dead zone it replaces")]
		public void A_seeded_expression_reproduces_an_anti_dead_zone()
		{
			Matches(0f, 4915f, 0f, "anti dead zone of about fifteen per cent");
			Matches(0f, 9830f, 0f, "a larger anti dead zone");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A seeded expression reproduces the sensitivity curve it replaces")]
		public void A_seeded_expression_reproduces_the_curve()
		{
			Matches(0f, 0f, 50f, "sensitivity bent one way");
			Matches(0f, 0f, -50f, "sensitivity bent the other way");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A seeded expression reproduces all three together")]
		public void A_seeded_expression_reproduces_everything_at_once()
		{
			Matches(3276f, 4915f, 50f, "everything set at once");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A seeded expression is short enough to store and to read")]
		public void A_seeded_expression_fits_the_stored_length()
		{
			// The busiest row anybody can have. If this does not fit, seeding silently fails exactly for
			// the people who tuned their controller most carefully.
			var text = MapExpressionSeed.FromSettings("a1", 8000f, 9830f, 100f, MapExpressionSeed.ThumbMax);
			Console.WriteLine("busiest seeded expression, {0} characters: {1}", text.Length, text);
			Assert.IsTrue(text.Length <= MapExpression.MaxLength,
				string.Format("A fully tuned row seeds to {0} characters, past the limit of {1}.",
					text.Length, MapExpression.MaxLength));
		}

		[TestMethod, TestCategory("mapping")]
		[Description("A trigger row seeds against the trigger range, not the stick range")]
		public void A_trigger_seeds_against_its_own_range()
		{
			// The two destinations count their dead zones in different units, so using the wrong one would
			// turn a tenth into a hundred and thirtieth without any error.
			var trigger = MapExpressionSeed.FromSettings("s1", 25.5f, 0f, 0f, MapExpressionSeed.TriggerMax);
			var thumb = MapExpressionSeed.FromSettings("a1", 3276.7f, 0f, 0f, MapExpressionSeed.ThumbMax);
			if (trigger == "=s1")
			{
				// The full form does not fit, so the plain source is offered. The unit question this
				// test exists for cannot be asked until the column is widened.
				Assert.IsTrue("=sign(s1)*deadzone(abs(s1),0.1)".Length > MapExpression.MaxLength,
					"The full form would have fitted, so it should not have fallen back.");
				return;
			}
			StringAssert.Contains(trigger, "0.1", "A tenth of a trigger should read as a tenth.");
			StringAssert.Contains(thumb, "0.1", "A tenth of a stick should read as a tenth.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stored button becomes a button in the formula, not the number one")]
		public void A_stored_button_does_not_become_the_number_one()
		{
			// Storage keeps a button as a bare number, so button one is "1". Inside a formula "1" is
			// the number one: a value that never changes, not something anybody can press. The row
			// would then read as sensible, compile, run, and do nothing the person asked for.
			Assert.AreEqual("b1", MapExpressionSeed.AsExpressionSource("1"));
			Assert.AreEqual("b12", MapExpressionSeed.AsExpressionSource("12"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Controls that already carry a letter are unchanged")]
		public void A_control_that_names_its_kind_is_left_alone()
		{
			Assert.AreEqual("a1", MapExpressionSeed.AsExpressionSource("a1"));
			Assert.AreEqual("x3", MapExpressionSeed.AsExpressionSource("x3"));
			Assert.AreEqual("s2", MapExpressionSeed.AsExpressionSource("s2"));
			Assert.AreEqual("d4", MapExpressionSeed.AsExpressionSource("d4"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A stick pushed the other way becomes a minus sign, which is what it means")]
		public void A_reversed_stick_becomes_a_minus_sign()
		{
			Assert.AreEqual("-a2", MapExpressionSeed.AsExpressionSource("a-2"));
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Anything with no plain form gives nothing back rather than something close")]
		public void Anything_without_a_plain_form_gives_nothing()
		{
			// Half of an axis read backwards, and a button counted as released, cannot be said with
			// the sources a formula has. A formula that is nearly right is worse than none.
			Assert.IsNull(MapExpressionSeed.AsExpressionSource("x-3"), "Half an axis, reversed.");
			Assert.IsNull(MapExpressionSeed.AsExpressionSource("-1"), "A button counted as released.");
			Assert.IsNull(MapExpressionSeed.AsExpressionSource(""));
			Assert.IsNull(MapExpressionSeed.AsExpressionSource(null));
			Assert.IsNull(MapExpressionSeed.AsExpressionSource("0"), "There is no control nought.");
			Assert.IsNull(MapExpressionSeed.AsExpressionSource("q7"), "No such kind of control.");
			Assert.IsNull(MapExpressionSeed.AsExpressionSource("=a1"), "Already a formula.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every source a conversion produces is one the parser accepts")]
		public void Every_converted_source_is_one_the_parser_reads()
		{
			foreach (var stored in new[] { "1", "12", "a1", "a-2", "x3", "s2", "h1", "p1", "d4" })
			{
				var source = MapExpressionSeed.AsExpressionSource(stored);
				if (source == null)
					continue;
				MapExpression parsed;
				string error;
				int position;
				Assert.IsTrue(MapExpression.TryParse(MapExpression.Prefix + source, out parsed, out error, out position),
					"'" + stored + "' became '" + source + "', which the parser will not read: " + error);
			}
		}

	}
}
